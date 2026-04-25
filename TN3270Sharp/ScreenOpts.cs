/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * Portions of this code may have been adapted or originated from another MIT
 * licensed project and will be explicitly noted in the comments as needed.
 *
 * MIT License
 *
 * Copyright (c) 2020-2026 by Robert J. Lawrence (roblthegreat) and other
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

namespace TN3270Sharp;

/// <summary>
///     Options bag controlling how <see cref="ITn3270ConnectionHandler.ShowScreen" />
///     drives a single render/response cycle. Modeled on go3270's
///     <c>ScreenOpts</c>; see <c>plans/go3270-port-roadmap.md</c> for the porting plan.
/// </summary>
/// <remarks>
///     All cursor coordinates use the project-wide 1-based convention (see
///     <c>CLAUDE.md</c>). NoClear support is tracked under Tier 3.2 and not
///     present here yet.
/// </remarks>
public sealed record class ScreenOpts
{
    /// <summary>
    ///     If true, send the screen and return immediately without blocking on
    ///     a response from the terminal. <see cref="PostSendCallback" /> still
    ///     fires, so callers can chain follow-on work after the bytes hit the
    ///     wire.
    /// </summary>
    public bool NoResponse { get; init; }

    /// <summary>
    ///     1-based cursor row override. The default value of <c>0</c> means
    ///     "use <see cref="Screen.InitialCursorPosition" />".
    /// </summary>
    public int CursorRow { get; init; }

    /// <summary>
    ///     1-based cursor column override. The default value of <c>0</c> means
    ///     "use <see cref="Screen.InitialCursorPosition" />".
    /// </summary>
    public int CursorCol { get; init; }

    /// <summary>
    ///     Invoked after the screen bytes have been written to the wire and
    ///     before any blocking read. Runs even when <see cref="NoResponse" />
    ///     is true. The argument is <see cref="CallbackData" /> verbatim — the
    ///     library does not pass wire bytes or other state.
    /// </summary>
    public Action<object?>? PostSendCallback { get; init; }

    /// <summary>Opaque marker passed to <see cref="PostSendCallback" />.</summary>
    public object? CallbackData { get; init; }

    /// <summary>
    ///     When true (the default), AID handlers registered through
    ///     <see cref="ITn3270ConnectionHandler.SetAidAction" /> fire when a
    ///     matching AID arrives in the response.
    /// </summary>
    public bool ExecutePredefinedAidActions { get; init; } = true;

    /// <summary>
    ///     Synchronous pre-send hook, invoked just before the screen bytes are
    ///     written to the wire.
    /// </summary>
    public Action? BeforeScreenRenderAction { get; init; }

    /// <summary>
    ///     Synchronous post-response hook, invoked after the response has been
    ///     parsed into the screen and any registered AID handler has run.
    /// </summary>
    public Action<AID>? ScreenBufferProcess { get; init; }
}
