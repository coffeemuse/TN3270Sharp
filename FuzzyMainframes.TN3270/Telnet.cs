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


using System.Net.Sockets;
using System.Text;

namespace FuzzyMainframes.TN3270;

internal class Telnet : IDisposable
{
    private readonly ICodepage _codepage;
    private readonly Action<string>? _logger;
    private readonly List<byte> _readAccumulator = [];

    public Telnet(TcpClient tcpClient, Stream stream, ICodepage codepage, Action<string>? logger = null)
    {
        TcpClient = tcpClient;
        Stream = stream;
        _codepage = codepage;
        _logger = logger;

        BufferBytes = new byte[256];
        TotalBytesReadFromBuffer = 0;
        ConnectionClosed = false;
    }

    protected TcpClient TcpClient { get; }
    protected Stream Stream { get; }
    protected byte[] BufferBytes { get; set; }
    protected int TotalBytesReadFromBuffer { get; set; }
    protected bool ConnectionClosed { get; private set; }

    public void Dispose()
    {
        CloseConnection();
    }

    public void CloseConnection()
    {
        if (ConnectionClosed)
            return;

        Stream.Close();
        TcpClient.Close();
        ConnectionClosed = true;
    }

    public void Negotiate()
    {
        // RFC 1576
        // This is no proper negotiation, but at least it checks if the client is compatible and responds as expected.
        WriteToStream(TelnetCommands.IAC, TelnetCommands.DO, TelnetCommands.TERMINAL_TYPE);
        ExpectFromStream(TelnetCommands.IAC, TelnetCommands.WILL, TelnetCommands.TERMINAL_TYPE);

        WriteToStream(TelnetCommands.IAC, TelnetCommands.SB, TelnetCommands.TERMINAL_TYPE, 0x01, TelnetCommands.IAC,
            TelnetCommands.SE);
        ExpectFromStream(TelnetCommands.IAC, TelnetCommands.SB, TelnetCommands.TERMINAL_TYPE, 0x0);
        _ = ReadFromStream();
        // buffer now contains the terminal type (RFC1340) followed by IAC SE
        List<byte> terminalType = [];
        for (var i = 0; i < BufferBytes.Length; i++)
        {
            if (BufferBytes[i] != TelnetCommands.IAC || BufferBytes[i + 1] != TelnetCommands.SE)
                continue;
            for (var j = 0; j < i; j++)
                terminalType.Add(BufferBytes[j]);
            break;
        }

        var terminalTypeString = Encoding.ASCII.GetString(terminalType.ToArray());

        WriteToStream(TelnetCommands.IAC, TelnetCommands.DO, TelnetCommands.EOR);
        ExpectFromStream(TelnetCommands.IAC, TelnetCommands.WILL, TelnetCommands.EOR);

        WriteToStream(TelnetCommands.IAC, TelnetCommands.DO, TelnetCommands.BINARY);
        ExpectFromStream(TelnetCommands.IAC, TelnetCommands.WILL, TelnetCommands.BINARY);

        WriteToStream(TelnetCommands.IAC, TelnetCommands.WILL, TelnetCommands.EOR, TelnetCommands.IAC,
            TelnetCommands.WILL, TelnetCommands.BINARY);
        ExpectFromStream(TelnetCommands.IAC, TelnetCommands.DO, TelnetCommands.EOR, TelnetCommands.IAC,
            TelnetCommands.DO, TelnetCommands.BINARY);
    }

    public void UnNegotiate()
    {
        WriteToStream(TelnetCommands.IAC, TelnetCommands.WONT, TelnetCommands.IAC, TelnetCommands.WONT,
            TelnetCommands.BINARY);
        _ = ReadFromStream();

        WriteToStream(TelnetCommands.IAC, TelnetCommands.DONT, TelnetCommands.BINARY);
        _ = ReadFromStream();

        WriteToStream(TelnetCommands.IAC, TelnetCommands.DONT, TelnetCommands.EOR);
        _ = ReadFromStream();

        WriteToStream(TelnetCommands.IAC, TelnetCommands.DONT, TelnetCommands.TERMINAL_TYPE);
        _ = ReadFromStream();
    }

    protected void ExpectFromStream(params byte[] expected)
    {
        var actual = new byte[expected.Length];
        var n = Stream.Read(actual, 0, actual.Length);
        if (n == expected.Length && expected.AsSpan().SequenceEqual(actual.AsSpan(0, n)))
            return;

        _logger?.Invoke($"telnet negotiation: expected {Hex(expected)}, got {Hex(actual.AsSpan(0, n))}");
    }

    private static string Hex(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder(data.Length * 3);
        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(data[i].ToString("x2"));
        }

        return sb.ToString();
    }

    protected int ReadFromStream() => Stream.Read(BufferBytes, 0, BufferBytes.Length);

    protected void WriteToStream(params byte[] data) => Stream.Write(data);
    

    // Reads the inbound stream and dispatches <paramref name="action"/> exactly
    // once with the next complete logical 3270 record (terminated by IAC EOR,
    // 0xff 0xef), then returns. The record passed to the callback is a freshly-
    // allocated byte[] sized to the record itself, EOR trailer included. TCP
    // segmentation can split a single record across multiple Stream.Read
    // returns, and a single Stream.Read can contain bytes from the next record;
    // both cases are handled by the instance-level accumulator. The caller
    // (typically Tn3270ConnectionHandler.ShowScreen) calls Read again for each
    // subsequent AID.
    public void Read(Action<byte[]> action)
    {
        while (!ConnectionClosed)
        {
            if (TryExtractRecord(_readAccumulator, out var record))
            {
                action(record);
                return;
            }

            TotalBytesReadFromBuffer = ReadFromStream();
            if (TotalBytesReadFromBuffer == 0)
                return;

            for (var i = 0; i < TotalBytesReadFromBuffer; i++)
                _readAccumulator.Add(BufferBytes[i]);
        }
    }

    private static bool TryExtractRecord(List<byte> accumulator, out byte[] record)
    {
        for (var i = 0; i + 1 < accumulator.Count; i++)
        {
            if (accumulator[i] != 0xff || accumulator[i + 1] != 0xef)
                continue;

            var length = i + 2;
            record = accumulator.GetRange(0, length).ToArray();
            accumulator.RemoveRange(0, length);
            return true;
        }

        record = [];
        return false;
    }

    public void SendScreen(Screen screen) => SendScreen(screen, screen.InitialCursorPosition.row, screen.InitialCursorPosition.column);

    public void SendScreen(Screen screen, int row, int col, bool noClear = false)
    {
        if (noClear)
        {
            DataStream.Write(Stream);
            WriteToStream((byte)ControlChars.WCCnoReset);
        }
        else
        {
            DataStream.EraseWrite(Stream);
            WriteToStream((byte)ControlChars.WCCdefault);
        }

        foreach (var fld in screen.Fields)
        {
            // tell the terminal where to draw field
            DataStream.SBA(Stream, fld.Row, fld.Column);
            WriteToStream(screen.BuildField(fld));

            var content = fld.Contents;
            if (fld.Name != "")
            {
                // TODO
            }

            if (!string.IsNullOrEmpty(content))
                WriteToStream(_codepage.Encode(content));
        }

        // Non-clearing updates deliberately leave the cursor where the user
        // put it — go3270's showScreenInternal does the same.
        if (!noClear)
        {
            DataStream.SBA(Stream, row, col);
            DataStream.IC(Stream);
        }

        WriteToStream(TelnetCommands.IAC, 0xef);
    }

    private enum TelnetState
    {
        Normal = 0,
        Command = 1,
        SubNegotiation = 2
    }
}