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

using System.Text;

namespace FuzzyMainframes.TN3270;

/// <summary>
///     Translates between the wire EBCDIC bytes used by the 3270 datastream and the
///     ASCII strings used by application code. One instance per connection.
/// </summary>
public interface ICodepage
{
    /// <summary>The .NET encoding name backing this codepage (e.g. "IBM01047").</summary>
    string Id { get; }

    /// <summary>Encode an ASCII string to EBCDIC bytes for sending to the terminal.</summary>
    byte[] Encode(string ascii);

    /// <summary>Decode EBCDIC bytes received from the terminal back to an ASCII string.</summary>
    string Decode(byte[] ebcdic);
}

/// <summary>
///     Default <see cref="ICodepage" /> implementation that wraps a .NET
///     <see cref="Encoding" /> resolved by name. Suitable for any EBCDIC code page
///     supplied by <c>System.Text.Encoding.CodePages</c>.
/// </summary>
public sealed class BclCodepage : ICodepage
{
    private static readonly Encoding AsciiEncoding = Encoding.ASCII;
    private readonly Encoding _ebcdicEncoding;

    /// <summary>
    ///     Construct a codec backed by the named .NET <see cref="Encoding" />.
    /// </summary>
    /// <param name="encodingName">
    ///     Encoding name passed straight to <see cref="Encoding.GetEncoding(string)" /> —
    ///     typically <c>"IBM01047"</c> (CP1047) or <c>"IBM037"</c> (CP037).
    ///     <see cref="Tn3270Server" /> registers
    ///     <see cref="System.Text.CodePagesEncodingProvider" /> so the BCL
    ///     recognizes the IBM-prefixed names.
    /// </param>
    public BclCodepage(string encodingName)
    {
        Id = encodingName;
        _ebcdicEncoding = Encoding.GetEncoding(encodingName);
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public byte[] Encode(string ascii) =>
        Encoding.Convert(AsciiEncoding, _ebcdicEncoding, AsciiEncoding.GetBytes(ascii));

    /// <inheritdoc />
    public string Decode(byte[] ebcdic) =>
        AsciiEncoding.GetString(Encoding.Convert(_ebcdicEncoding, AsciiEncoding, ebcdic));
}