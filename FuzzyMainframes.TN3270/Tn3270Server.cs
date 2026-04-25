/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * Portions of this code may have been adapted or originated from another MIT
 * licensed project and will be explicitly noted in the comments as needed.
 *
 * MIT License
 *
 * Copyright (c) 2020-2026 by Robert J. Lawrence (roblthegreat) and other
 * FuzzyMainframes.TN3270 contributors.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FuzzyMainframes.TN3270;

/// <summary>
///     TCP listener that accepts TN3270 client connections, performs the
///     telnet-level negotiation, and hands each connection to a user-supplied
///     callback for the per-turn screen logic. One thread per connection;
///     blocking I/O inside the connection thread.
/// </summary>
/// <remarks>
///     <para>
///         The server registers <see cref="CodePagesEncodingProvider" /> in its
///         constructor so that IBM-prefixed EBCDIC code pages
///         (<c>IBM01047</c>, <c>IBM037</c>, …) resolve through
///         <see cref="Encoding.GetEncoding(string)" />. Each accepted connection
///         is given a freshly-constructed <see cref="ICodepage" /> from the
///         factory, so concurrent clients on different code pages don't
///         interfere.
///     </para>
///     <para>
///         Per-connection behaviour lives in <see cref="ITn3270ConnectionHandler" />.
///     </para>
/// </remarks>
public class Tn3270Server
{
    /// <summary>
    ///     Bind to <c>0.0.0.0:<paramref name="port" /></c> with the default
    ///     CP1047 (<c>IBM01047</c>) EBCDIC code page.
    /// </summary>
    /// <param name="port">TCP port to listen on (typically 3270 or 23).</param>
    public Tn3270Server(int port)
        : this("0.0.0.0", port)
    {
    }

    /// <summary>
    ///     Bind to a specific IP address with the default CP1047
    ///     (<c>IBM01047</c>) EBCDIC code page.
    /// </summary>
    /// <param name="ipAddress">IPv4 address to bind to (e.g. <c>"127.0.0.1"</c>).</param>
    /// <param name="port">TCP port to listen on.</param>
    public Tn3270Server(string ipAddress, int port)
        : this(ipAddress, port, "IBM01047")
    {
    }

    /// <summary>
    ///     Bind to a specific IP address with a named EBCDIC code page resolved
    ///     by <see cref="Encoding.GetEncoding(string)" />.
    /// </summary>
    /// <param name="ipAddress">IPv4 address to bind to.</param>
    /// <param name="port">TCP port to listen on.</param>
    /// <param name="defaultEbcdicEncoding">
    ///     EBCDIC code page name (e.g. <c>"IBM01047"</c> for CP1047 or
    ///     <c>"IBM037"</c> for CP037). Each new connection wraps this in a
    ///     fresh <see cref="BclCodepage" />.
    /// </param>
    public Tn3270Server(string ipAddress, int port, string defaultEbcdicEncoding)
        : this(ipAddress, port, () => new BclCodepage(defaultEbcdicEncoding))
    {
    }

    /// <summary>
    ///     Bind to a specific IP address with a custom <see cref="ICodepage" />
    ///     factory. Use this overload when you need a non-<see cref="BclCodepage" />
    ///     implementation (e.g. a hand-rolled translation table).
    /// </summary>
    /// <param name="ipAddress">IPv4 address to bind to.</param>
    /// <param name="port">TCP port to listen on.</param>
    /// <param name="codepageFactory">
    ///     Factory invoked once per accepted connection to produce that
    ///     connection's <see cref="ICodepage" />.
    /// </param>
    public Tn3270Server(string ipAddress, int port, Func<ICodepage> codepageFactory)
    {
        IpAddress = ipAddress;
        Port = port;
        CodepageFactory = codepageFactory;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private string IpAddress { get; }
    private int Port { get; }
    private Func<ICodepage> CodepageFactory { get; }

    /// <summary>
    ///     Optional callback invoked with diagnostic messages (currently used for
    ///     telnet-negotiation mismatches). When null, diagnostics are silently dropped.
    /// </summary>
    public Action<string>? Logger { get; init; }

    /// <summary>
    ///     Accept connections in a loop until <paramref name="breakCondition" />
    ///     returns <c>true</c>. Each accepted client gets its own thread,
    ///     telnet-negotiates, then runs <paramref name="handleConnectionAction" />
    ///     to completion before the thread exits.
    /// </summary>
    /// <param name="breakCondition">
    ///     Polled before each <c>AcceptTcpClient</c>; return <c>true</c> to stop
    ///     accepting new connections and shut the listener down. Already-running
    ///     connection threads keep running until their handler returns.
    /// </param>
    /// <param name="whenHasNewConnection">
    ///     Invoked at the start of each connection thread (after accept, before
    ///     telnet negotiation). Useful for incrementing connection counters or
    ///     logging.
    /// </param>
    /// <param name="whenConnectionIsClosed">
    ///     Invoked at the end of each connection thread, after
    ///     <paramref name="handleConnectionAction" /> returns and the underlying
    ///     <see cref="ITn3270ConnectionHandler" /> has been disposed.
    /// </param>
    /// <param name="handleConnectionAction">
    ///     The per-connection app logic. Inside this callback you typically
    ///     register AID handlers via <see cref="ITn3270ConnectionHandler.SetAidAction" />
    ///     and drive a sequence of <see cref="ITn3270ConnectionHandler.ShowScreen" />
    ///     calls. Returning ends the connection.
    /// </param>
    public void StartListener(Func<bool> breakCondition, Action whenHasNewConnection, Action whenConnectionIsClosed,
        Action<ITn3270ConnectionHandler> handleConnectionAction)
    {
        var server = new TcpListener(IPAddress.Parse(IpAddress), Port);
        server.Start();

        while (!breakCondition())
        {
            var client = server.AcceptTcpClient();
            new Thread(() =>
            {
                whenHasNewConnection();

                using (var tn3270ConnectionHandler = new Tn3270ConnectionHandler(client, CodepageFactory(), Logger))
                {
                    tn3270ConnectionHandler.NegotiateTelnet();
                    handleConnectionAction(tn3270ConnectionHandler);
                }

                whenConnectionIsClosed();
            }).Start();
        }

        server.Stop();
    }
}