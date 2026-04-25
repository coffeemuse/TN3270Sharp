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

/* Thanks go to Alexandre Bencz (bencz) for the major re-write of the connection handling */

using System.Net.Sockets;

namespace FuzzyMainframes.TN3270;

internal class Tn3270ConnectionHandler : ITn3270ConnectionHandler, IDisposable
{
    private readonly Dictionary<AID, Action?> AidActions;
    private readonly ICodepage Codepage;
    private readonly Telnet Telnet;

    public Tn3270ConnectionHandler(TcpClient tcpClient, ICodepage codepage, Action<string>? logger = null)
    {
        Codepage = codepage;
        Telnet = new Telnet(tcpClient, tcpClient.GetStream(), codepage, logger);
        AidActions = [];
        ResetAidActions();
    }

    public void Dispose()
    {
        Telnet.Dispose();
    }

    public void ShowScreen(Screen screen, ScreenOpts? opts = null)
    {
        opts ??= new ScreenOpts();

        opts.BeforeScreenRenderAction?.Invoke();

        var (cursorRow, cursorCol) = ResolveCursor(screen, opts);
        Telnet.SendScreen(screen, cursorRow, cursorCol, opts.NoClear);

        opts.PostSendCallback?.Invoke(opts.CallbackData);

        if (opts.NoResponse)
            return;

        try
        {
            Telnet.Read(bufferBytes =>
            {
                var recvdAID = (AID)bufferBytes[0];

                if (opts.ExecutePredefinedAidActions
                    && AidActions.TryGetValue(recvdAID, out var action))
                    action?.Invoke();

                var response = new Response(bufferBytes, Codepage);
                response.ParseFieldsScreen(screen);

                opts.ScreenBufferProcess?.Invoke(recvdAID);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            CloseConnection();
        }
    }

    // 0 in CursorRow/CursorCol is the "unset" sentinel — 0 is unambiguously
    // invalid in the project's 1-based coord system, so it doubles as
    // "fall back to Screen.InitialCursorPosition".
    private static (int row, int col) ResolveCursor(Screen screen, ScreenOpts opts)
    {
        var row = opts.CursorRow > 0 ? opts.CursorRow : screen.InitialCursorPosition.row;
        var col = opts.CursorCol > 0 ? opts.CursorCol : screen.InitialCursorPosition.column;
        return (row, col);
    }

    public void SetAidAction(AID aidCommand, Action action)
    {
        AidActions[aidCommand] = action;
    }

    public void CloseConnection()
    {
        Telnet.CloseConnection();
    }

    public void NegotiateTelnet()
    {
        Telnet.Negotiate();
    }

    public void ResetAidActions()
    {
        foreach (var aid in Enum.GetValues(typeof(AID)).Cast<AID>())
            AidActions[aid] = null;
    }
}