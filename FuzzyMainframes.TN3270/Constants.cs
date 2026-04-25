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

// 3270 Control Characters
internal enum ControlChars
{
    SF = 0x1d,
    SFE = 0x29,
    SA = 0x28,
    SBA = 0x11,
    IC = 0x13,
    PT = 0x05,
    RA = 0x3c,
    EUA = 0x12,
    WCCdefault = 0xc3,
    WCCnoReset = 0xc2,
    EraseWrite = 0xf5,
    Write = 0xf1
}

/// <summary>
///     Action ID byte sent by the terminal at the start of a Read-Modified
///     response, identifying which key the user pressed to release the screen
///     (Enter, a Program Function key, a Program Attention key, or Clear).
///     Use <see cref="ITn3270ConnectionHandler.SetAidAction" /> to register
///     handlers keyed on these values.
/// </summary>
public enum AID
{
    /// <summary>
    ///     No AID — used internally before the first response and as a sentinel
    ///     value; not produced by normal user keystrokes.
    /// </summary>
    None = 0x60,

    /// <summary>The user pressed Enter.</summary>
    Enter = 0x7D,

    /// <summary>Program Function key 1.</summary>
    PF1 = 0xF1,

    /// <summary>Program Function key 2.</summary>
    PF2 = 0xF2,

    /// <summary>Program Function key 3.</summary>
    PF3 = 0xF3,

    /// <summary>Program Function key 4.</summary>
    PF4 = 0xF4,

    /// <summary>Program Function key 5.</summary>
    PF5 = 0xF5,

    /// <summary>Program Function key 6.</summary>
    PF6 = 0xF6,

    /// <summary>Program Function key 7.</summary>
    PF7 = 0xF7,

    /// <summary>Program Function key 8.</summary>
    PF8 = 0xF8,

    /// <summary>Program Function key 9.</summary>
    PF9 = 0xF9,

    /// <summary>Program Function key 10.</summary>
    PF10 = 0x7A,

    /// <summary>Program Function key 11.</summary>
    PF11 = 0x7B,

    /// <summary>Program Function key 12.</summary>
    PF12 = 0x7C,

    /// <summary>Program Function key 13.</summary>
    PF13 = 0xC1,

    /// <summary>Program Function key 14.</summary>
    PF14 = 0xC2,

    /// <summary>Program Function key 15.</summary>
    PF15 = 0xC3,

    /// <summary>Program Function key 16.</summary>
    PF16 = 0xC4,

    /// <summary>Program Function key 17.</summary>
    PF17 = 0xC5,

    /// <summary>Program Function key 18.</summary>
    PF18 = 0xC6,

    /// <summary>Program Function key 19.</summary>
    PF19 = 0xC7,

    /// <summary>Program Function key 20.</summary>
    PF20 = 0xC8,

    /// <summary>Program Function key 21.</summary>
    PF21 = 0xC9,

    /// <summary>Program Function key 22.</summary>
    PF22 = 0x4A,

    /// <summary>Program Function key 23.</summary>
    PF23 = 0x4B,

    /// <summary>Program Function key 24.</summary>
    PF24 = 0x4C,

    /// <summary>Program Attention key 1.</summary>
    PA1 = 0x6C,

    /// <summary>Program Attention key 2.</summary>
    PA2 = 0x6E,

    /// <summary>Program Attention key 3.</summary>
    PA3 = 0x6B,

    /// <summary>
    ///     The user pressed Clear. The terminal blanks the screen locally
    ///     before sending the response.
    /// </summary>
    Clear = 0x6D
}

[Flags]
internal enum AttribChar
{
    Protected = 0b00100000,
    Unprotected = 0b00000000,
    Numeric = 0b00010000,
    Alpha = 0b00000000,
    Normal = 0b00000000,
    Intensity = 0b00001000,
    Hidden = 0b00001100,
    MDT = 0b000000001
}