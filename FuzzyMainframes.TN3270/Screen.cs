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


/*
 * This file includes several helper methods added by M4xAmmo to make adding fields
 * a but more user friendly. Thanks!
 */

namespace FuzzyMainframes.TN3270;

/// <summary>
///     UI model for one 3270 screen: an ordered list of <see cref="Field" />
///     elements plus the initial cursor position. Built up via
///     <see cref="AddText(int, int, string, bool, Color, Highlight)" />,
///     <see cref="AddInput(int, int, string, bool, bool, bool, bool)" />, and
///     <see cref="AddEOF" /> helpers, then handed to
///     <see cref="ITn3270ConnectionHandler.ShowScreen" />.
/// </summary>
/// <remarks>
///     Row/column values are 1-based with <c>(1, 1)</c> at the upper-left;
///     the default 24x80 screen has rows 1–24 and columns 1–80. Field
///     <c>(Row, Column)</c> coordinates point at the field's attribute byte;
///     visible content lands at <c>(Row, Column + 1)</c>.
/// </remarks>
public class Screen
{
    /// <summary>
    ///     Creates an empty screen with the cursor positioned at <c>(1, 1)</c>.
    /// </summary>
    public Screen()
    {
        Fields = [];
        InitialCursorPosition = (1, 1);
    }

    /// <summary>
    ///     Optional human-readable name for this screen. Not transmitted on the
    ///     wire; useful for routing and logging.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Ordered list of fields that make up this screen. The order matters —
    ///     it determines the sequence of 3270 <c>SF</c>/<c>SFE</c> orders
    ///     emitted on the wire and the tab order on the terminal.
    /// </summary>
    public List<Field> Fields { get; set; }

    /// <summary>
    ///     1-based <c>(row, column)</c> at which the cursor lands when the
    ///     screen is rendered. Overridable per call via
    ///     <see cref="ScreenOpts.CursorRow" /> / <see cref="ScreenOpts.CursorCol" />.
    /// </summary>
    public (int row, int column) InitialCursorPosition { get; set; }

    /// <summary>
    ///     Add a named protected (display-only) text field to this screen.
    /// </summary>
    /// <param name="row">1-based row of the field's attribute byte (1–24 on a default screen).</param>
    /// <param name="column">
    ///     1-based column of the field's attribute byte (1–80 on a default
    ///     screen). The visible text starts at column + 1.
    /// </param>
    /// <param name="name">Optional name; pass <see cref="string.Empty" /> for unnamed display labels.</param>
    /// <param name="contents">Text to display.</param>
    /// <param name="intensity">When true, render the text with high intensity.</param>
    /// <param name="color">Foreground color; <see cref="Color.DefaultColor" /> defers to the terminal.</param>
    /// <param name="highlighting">Highlight attribute (blink, reverse video, underscore).</param>
    public void AddText(int row, int column, string name, string contents, bool intensity = false,
        Color color = Color.DefaultColor, Highlight highlighting = Highlight.DefaultHighlight)
    {
        Fields.Add(new Field
        {
            Column = column,
            Row = row,
            Name = name,
            Contents = contents,
            Intensity = intensity,
            Highlighting = highlighting,
            Color = color
        });
    }

    /// <summary>
    ///     Add an unnamed protected (display-only) text field to this screen.
    /// </summary>
    /// <param name="row">1-based row of the field's attribute byte.</param>
    /// <param name="column">
    ///     1-based column of the field's attribute byte. The visible text
    ///     starts at column + 1.
    /// </param>
    /// <param name="contents">Text to display.</param>
    /// <param name="intensity">When true, render the text with high intensity.</param>
    /// <param name="color">Foreground color; <see cref="Color.DefaultColor" /> defers to the terminal.</param>
    /// <param name="highlighting">Highlight attribute (blink, reverse video, underscore).</param>
    public void AddText(int row, int column, string contents, bool intensity = false,
        Color color = Color.DefaultColor, Highlight highlighting = Highlight.DefaultHighlight)
    {
        AddText(row, column, string.Empty, contents, intensity, color, highlighting);
    }

    /// <summary>
    ///     Add a named writeable input field to this screen. Use the overload
    ///     that takes a <c>length</c> when you also want to cap the field's
    ///     length with a trailing <see cref="AddEOF" />.
    /// </summary>
    /// <param name="row">1-based row of the field's attribute byte.</param>
    /// <param name="column">
    ///     1-based column of the field's attribute byte. The user-typed input
    ///     area starts at column + 1.
    /// </param>
    /// <param name="name">
    ///     Field name. Required-unique across writeable fields on the screen;
    ///     <see cref="GetFieldData" /> and <see cref="SetFieldValue(string, string)" />
    ///     look fields up by this name.
    /// </param>
    /// <param name="hidden">When true, mask the input (e.g. for passwords).</param>
    /// <param name="write">
    ///     When false the field is rendered as protected — useful when you want
    ///     to reserve a name for a field whose writeability you'll toggle later.
    /// </param>
    /// <param name="underscore">When true, render the input area with the underscore highlight.</param>
    /// <param name="numericOnly">When true, hint to the terminal that only digits are accepted.</param>
    public void AddInput(int row, int column, string name, bool hidden = false, bool write = true,
        bool underscore = true, bool numericOnly = false)
    {
        Fields.Add(new Field
        {
            Column = column,
            Row = row,
            Name = name,
            Write = write,
            Highlighting = underscore
                ? Highlight.Underscore
                : Highlight.DefaultHighlight,
            Hidden = hidden,
            NumericOnly = numericOnly
        });
    }

    /// <summary>
    ///     Add a named writeable input field with a fixed maximum length. Emits
    ///     the input field followed by a trailing <see cref="AddEOF" /> at
    ///     <c>(row, column + length + 1)</c> to cap the input area.
    /// </summary>
    /// <param name="row">1-based row of the field's attribute byte.</param>
    /// <param name="column">
    ///     1-based column of the field's attribute byte. The user-typed input
    ///     area starts at column + 1.
    /// </param>
    /// <param name="length">Maximum length of the input area in characters.</param>
    /// <param name="name">Required-unique field name (see overload without <paramref name="length" />).</param>
    /// <param name="hidden">When true, mask the input.</param>
    /// <param name="write">When false the field is rendered as protected.</param>
    /// <param name="underscore">When true, render with the underscore highlight.</param>
    /// <param name="numericOnly">When true, hint to the terminal that only digits are accepted.</param>
    public void AddInput(int row, int column, int length, string name, bool hidden = false, bool write = true,
        bool underscore = true, bool numericOnly = false)
    {
        AddInput(row, column, name, hidden, write, underscore, numericOnly);
        AddEOF(row, column + length + 1);
    }

    /// <summary>
    ///     Add a bare attribute byte at the given position. Typically used to
    ///     terminate a preceding writeable input field — the next attribute
    ///     byte ends the previous field's input area.
    /// </summary>
    /// <param name="row">1-based row of the attribute byte.</param>
    /// <param name="column">1-based column of the attribute byte.</param>
    public void AddEOF(int row, int column)
    {
        Fields.Add(new Field
        {
            Column = column,
            Row = row
        });
    }

    // Adapted from https://github.com/racingmars/go3270/blob/master/screen.go
    // Copyright 2020 by Matthew R. Wilson, licensed under the MIT license.
    // GetPosition translates row and col to buffer address control characters.
    // Borrowed from racingmars/go3270
    //
    // C#-ification and further changes are Copyright 2022 by Robert J. Lawrence (roblthegreat)
    // licened under the MIT license.
    internal byte[] BuildField(Field fld)
    {
        List<byte> buffer = [];
        // The Numeric bit doubles as auto-skip on protected fields and as the
        // numeric-input hint on writeable fields, so the two flags are gated
        // on opposite values of Write and never collide.
        var numericBit = (fld.Write && fld.NumericOnly) || (!fld.Write && fld.Autoskip);

        if (fld.Color == Color.DefaultColor && fld.Highlighting == Highlight.DefaultHighlight)
        {
            // We can use a simple SF
            buffer.Add((byte)ControlChars.SF);
            buffer.Add((byte)(
                (fld.Write ? FieldAttributes.Unprotected : FieldAttributes.Protected) |
                (numericBit ? FieldAttributes.Numeric : FieldAttributes.Alpha) |
                (fld.Intensity ? FieldAttributes.Intensity : FieldAttributes.Normal) |
                (fld.Hidden ? FieldAttributes.Hidden : FieldAttributes.Normal)
            ));
            return buffer.ToArray();
        }

        // otherwise we need to use SFE (SF Extended)
        buffer.Add((byte)ControlChars.SFE);
        var paramCount = 1;

        if (fld.Color != Color.DefaultColor)
            paramCount++;

        if (fld.Highlighting != Highlight.DefaultHighlight)
            paramCount++;

        buffer.Add((byte)paramCount);

        // Basic field attribute
        buffer.Add(0xc0);
        buffer.Add((byte)(
            (fld.Write ? FieldAttributes.Unprotected : FieldAttributes.Protected) |
            (numericBit ? FieldAttributes.Numeric : FieldAttributes.Alpha) |
            (fld.Intensity ? FieldAttributes.Intensity : FieldAttributes.Normal) |
            (fld.Hidden ? FieldAttributes.Hidden : FieldAttributes.Normal)
        ));

        // Highlighting Attribute
        if (fld.Highlighting != Highlight.DefaultHighlight)
        {
            buffer.Add(0x41);
            buffer.Add((byte)fld.Highlighting);
        }

        // Color attribute
        if (fld.Color != Color.DefaultColor)
        {
            buffer.Add(0x42);
            buffer.Add((byte)fld.Color);
        }

        return buffer.ToArray();
    }

    /// <summary>
    ///     Look up a field by name and return its current <see cref="Field.Contents" />,
    ///     or <c>null</c> if no field with that name exists. After
    ///     <see cref="ITn3270ConnectionHandler.ShowScreen" /> returns, this is
    ///     how callers retrieve user-entered values.
    /// </summary>
    /// <param name="fieldName">Name of the field to look up.</param>
    public string? GetFieldData(string fieldName)
    {
        var field = Fields.FirstOrDefault(x => x.Name == fieldName);

        return field?.Contents;
    }

    /// <summary>
    ///     Set a field's contents by 1-based attribute-byte coordinates. The
    ///     value is trimmed unless the field has <see cref="Field.KeepSpaces" />
    ///     set; no-op if no field exists at <c>(row, col)</c>.
    /// </summary>
    /// <param name="row">1-based row of the field's attribute byte.</param>
    /// <param name="col">1-based column of the field's attribute byte.</param>
    /// <param name="data">New contents.</param>
    public void SetFieldValue(int row, int col, string data)
    {
        var field = Fields.FirstOrDefault(x => x.Row == row && x.Column == col);
        if (field == null)
            return;

        field.Contents = field.KeepSpaces ? data : data.Trim();
    }

    /// <summary>
    ///     Set a field's contents by name. The value is stored verbatim
    ///     (no trim); no-op if no field has that name.
    /// </summary>
    /// <param name="fieldName">Name of the field to update.</param>
    /// <param name="fieldData">New contents.</param>
    public void SetFieldValue(string fieldName, string fieldData)
    {
        var field = Fields.FirstOrDefault(x => x.Name == fieldName);
        if (field == null)
            return;

        field.Contents = fieldData;
    }

    /// <summary>
    ///     Clear a field's contents by name. No-op if no field has that name.
    /// </summary>
    /// <param name="fieldName">Name of the field to clear.</param>
    public void ClearFieldValue(string fieldName)
    {
        var field = Fields.FirstOrDefault(x => x.Name == fieldName);
        if (field == null)
            return;

        field.Contents = "";
    }
}