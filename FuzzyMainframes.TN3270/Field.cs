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
///     A single 3270 field on a <see cref="Screen" /> — a <c>(Row, Column)</c>
///     attribute-byte position plus the visible/editable area that follows it
///     until the next attribute byte. Use <see cref="Screen.AddText(int, int, string, bool, Color, Highlight)" />
///     and <see cref="Screen.AddInput(int, int, string, bool, bool, bool, bool)" />
///     for typical construction.
/// </summary>
/// <remarks>
///     <see cref="Row" /> and <see cref="Column" /> point at the field's
///     attribute byte, which itself occupies one displayed cell (typically
///     rendered blank). Visible content lands at <c>(Row, Column + 1)</c>.
/// </remarks>
public class Field
{
    /// <summary>Creates an empty <see cref="Field" /> with no position.</summary>
    public Field()
    {
    }

    /// <summary>Creates a <see cref="Field" /> at the given attribute-byte position.</summary>
    /// <param name="row">1-based row (1–24 on a default screen).</param>
    /// <param name="col">1-based column (1–80 on a default screen).</param>
    public Field(int row, int col)
    {
        Row = row;
        Column = col;
    }

    /// <summary>
    ///     1-based row of this field's attribute byte (1–24 on a default
    ///     screen). Visible content begins on the same row.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    ///     1-based column of this field's attribute byte (1–80 on a default
    ///     screen). Visible content starts at column + 1.
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    ///     The field's text value. On outbound rendering, this is what gets
    ///     displayed. On inbound responses, this is the user-entered value
    ///     with surrounding whitespace trimmed unless <see cref="KeepSpaces" />
    ///     is set.
    /// </summary>
    public string Contents { get; set; } = string.Empty;

    /// <summary>
    ///     When <c>true</c>, the field is writeable (input). When <c>false</c>,
    ///     the field is protected (display-only). Only writeable fields
    ///     receive user-entered values back from the terminal during
    ///     response parsing.
    /// </summary>
    public bool Write { get; set; }

    /// <summary>
    ///     When <c>true</c>, response values for this field are stored
    ///     verbatim instead of trimmed. The 3270 buffer right-pads each input
    ///     area to its full length with spaces, so most callers want the
    ///     default trim behaviour; opt out only when surrounding spaces are
    ///     semantically significant.
    /// </summary>
    public bool KeepSpaces { get; set; }

    /// <summary>
    ///     When <c>true</c>, render the field with high intensity.
    /// </summary>
    public bool Intensity { get; set; }

    /// <summary>
    ///     When <c>true</c>, the field's contents are not displayed by the
    ///     terminal — typical for password input.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    ///     Hint to the terminal that this writeable field accepts only digits.
    ///     Only takes effect when <see cref="Write" /> is <c>true</c>; on
    ///     protected fields the same wire bit means <see cref="Autoskip" />
    ///     instead, so the two settings are gated on opposite values of
    ///     <see cref="Write" /> and never collide.
    /// </summary>
    /// <seealso cref="Autoskip" />
    public bool NumericOnly { get; set; }

    /// <summary>
    ///     When <c>true</c>, the cursor skips past this protected field while
    ///     tabbing. Only takes effect when <see cref="Write" /> is <c>false</c>;
    ///     on writeable fields the same wire bit means <see cref="NumericOnly" />.
    ///     Implemented via the 3270 Protected+Numeric attribute combination,
    ///     which the terminal treats as auto-skip.
    /// </summary>
    /// <seealso cref="NumericOnly" />
    public bool Autoskip { get; set; }

    /// <summary>
    ///     Foreground color for the field. <see cref="Color.DefaultColor" />
    ///     leaves color selection to the terminal's defaults.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    ///     Highlight attribute for the field. <see cref="Highlight.DefaultHighlight" />
    ///     means no highlighting.
    /// </summary>
    public Highlight Highlighting { get; set; }

    /// <summary>
    ///     Identifier used to look up this field after a response — see
    ///     <see cref="Screen.GetFieldData" /> and
    ///     <see cref="Screen.SetFieldValue(string, string)" />. All writeable
    ///     fields on a screen must have a unique non-empty name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
///     3270 field foreground colors. <see cref="DefaultColor" /> defers to the
///     terminal's color rules; the named values map to the standard 3270
///     extended-color attribute bytes.
/// </summary>
public enum Color
{
    /// <summary>Terminal default color (no extended-color attribute emitted).</summary>
    DefaultColor = 0,

    /// <summary>Blue (<c>0xF1</c>).</summary>
    Blue = 0xf1,

    /// <summary>Red (<c>0xF2</c>).</summary>
    Red = 0xf2,

    /// <summary>Pink (<c>0xF3</c>).</summary>
    Pink = 0xf3,

    /// <summary>Green (<c>0xF4</c>).</summary>
    Green = 0xf4,

    /// <summary>Turquoise (<c>0xF5</c>).</summary>
    Turquoise = 0xf5,

    /// <summary>Yellow (<c>0xF6</c>).</summary>
    Yellow = 0xf6,

    /// <summary>White (<c>0xF7</c>).</summary>
    White = 0xf7
}

/// <summary>
///     3270 field highlight attributes. Mutually exclusive — a field has at
///     most one highlight setting at a time.
/// </summary>
public enum Highlight
{
    /// <summary>No highlighting (no extended-highlight attribute emitted).</summary>
    DefaultHighlight = 0,

    /// <summary>Blink (<c>0xF1</c>).</summary>
    Blink = 0xf1,

    /// <summary>Reverse video (<c>0xF2</c>).</summary>
    ReverseVideo = 0xf2,

    /// <summary>Underscore (<c>0xF4</c>). Used by default on input fields rendered with <c>underscore: true</c>.</summary>
    Underscore = 0xf4
}