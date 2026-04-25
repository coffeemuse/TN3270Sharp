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

namespace FuzzyMainframes.TN3270;

/// <summary>
///     Per-connection contract handed to the user's
///     <see cref="Tn3270Server.StartListener" /> callback. Drives a single
///     client through any number of render/response turns and exposes the
///     hooks for closing the socket and pre-registering AID handlers.
/// </summary>
public interface ITn3270ConnectionHandler
{
    /// <summary>
    ///     Render <paramref name="screen" /> to the terminal and, unless
    ///     <see cref="ScreenOpts.NoResponse" /> is set, block until the user
    ///     presses an AID key. See <see cref="ScreenOpts" /> for the
    ///     individual knobs (cursor placement, callbacks, no-response sends,
    ///     etc.). Passing <c>null</c> uses the default options, which match
    ///     the most common case: blocking read with predefined AID handlers
    ///     enabled.
    /// </summary>
    void ShowScreen(Screen screen, ScreenOpts? opts = null);

    /// <summary>
    ///     Pre-register an action to run whenever <paramref name="aidCommand" />
    ///     arrives in a <see cref="ShowScreen" /> response. Use this for keys
    ///     that have the same meaning across most screens (e.g. <c>PF3</c> as
    ///     "exit"). Pre-registered actions fire before any per-call callback
    ///     supplied via <see cref="ScreenOpts.ScreenBufferProcess" />, and
    ///     can be suppressed for a single turn by setting
    ///     <see cref="ScreenOpts.ExecutePredefinedAidActions" /> to <c>false</c>.
    /// </summary>
    /// <param name="aidCommand">AID key to bind to.</param>
    /// <param name="action">Action invoked when that AID arrives.</param>
    void SetAidAction(AID aidCommand, Action action);

    /// <summary>
    ///     Close the underlying TCP connection. Any in-flight
    ///     <see cref="ShowScreen" /> read returns, the connection thread
    ///     unwinds out of the user's <c>handleConnectionAction</c>, and the
    ///     server's <c>whenConnectionIsClosed</c> callback fires.
    /// </summary>
    void CloseConnection();
}