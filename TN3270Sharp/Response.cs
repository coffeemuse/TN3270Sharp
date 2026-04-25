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

namespace TN3270Sharp;

public class Response
{
    public AID ActionID { get; set; }

    /// <summary>1-based row of the cursor at the time the AID key was pressed.</summary>
    public int Row { get; set; }

    /// <summary>1-based column of the cursor at the time the AID key was pressed.</summary>
    public int Column { get; set; }

    public Dictionary<byte[], string> Map { get; set; } = new Dictionary<byte[], string>();
    public byte[] BufferBytes { get; }

    private readonly ICodepage _codepage;

    public Response(byte[] bufferBytes, ICodepage codepage)
    {
        BufferBytes = bufferBytes;
        _codepage = codepage;

        ReadAction();
        ReadCursorPosition();
    }

    private void ReadAction()
    {
        ActionID = (AID)BufferBytes[0];
    }

    private void ReadCursorPosition()
    {
        // Bytes 1 and 2 of the inbound stream carry the cursor's 12-bit buffer
        // address at AID time. Short frames (some PA / Clear variants) skip them.
        if (BufferBytes.Length < 3)
            return;

        var (row, col) = Utils.DecodePosition(BufferBytes[1], BufferBytes[2]);
        Row = row;
        Column = col;
    }

    public void ParseFieldsScreen(Screen screen)
    {
        var inField = false;
        List<byte> fieldBytes = [];
        (int row, int col)? fieldPosition = null;

        for (var i = 0; i < BufferBytes.Length; i++)
        {
            var b = BufferBytes[i];
            if (b == 0xff)
            {
                if (i + 1 >= BufferBytes.Length)
                    return;

                if (BufferBytes[i + 1] == 0xef && fieldPosition != null)
                {
                    var data = _codepage.Decode(fieldBytes.ToArray());
                    screen.SetFieldValue(fieldPosition.Value.row, fieldPosition.Value.col, data);

                    fieldBytes.Clear();

                    return;
                }
            }
            if(b == 0x11)
            {
                if (inField && fieldPosition != null)
                {
                    var data = _codepage.Decode(fieldBytes.ToArray());
                    screen.SetFieldValue(fieldPosition.Value.row, fieldPosition.Value.col, data);
                }

                fieldBytes.Clear();
                inField = true;

                // The SBA in a Read-Modified response points to the first
                // character of the input area, one position past the field's
                // attribute byte. Fields are registered at the attribute byte
                // position, so step back one to find the matching Field.
                var inputAddr = (Utils.IODecodes[BufferBytes[++i]] << 6) | Utils.IODecodes[BufferBytes[++i]];
                fieldPosition = Utils.DecodeAddress(inputAddr - 1);

                continue;
            }
            if (!inField)
                continue;

            fieldBytes.Add(b);
        }
    }
}
