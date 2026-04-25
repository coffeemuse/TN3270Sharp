/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * Portions of this code may have been adapted or originated from another MIT
 * licensed project and will be explicitly noted in the comments as needed.
 *
 * MIT License
 *
 * Copyright (c) 2020, 2021, 20022 by Robert J. Lawrence (roblthegreat) and other
 * TN3270Sharp contributors.
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

namespace TN3270Sharp;

public class Tn3270Server
{
    public Tn3270Server(int port)
        : this("0.0.0.0", port)
    {
    }

    public Tn3270Server(string ipAddress, int port)
        : this(ipAddress, port, "IBM01047")
    {
    }

    public Tn3270Server(string ipAddress, int port, string defaultEbcdicEncoding)
        : this(ipAddress, port, () => new BclCodepage(defaultEbcdicEncoding))
    {
    }

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