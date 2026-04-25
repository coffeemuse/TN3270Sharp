/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * MIT License
 *
 * Copyright (c) 2020-2026 by Robert J. Lawrence (roblthegreat) and other
 * FuzzyMainframes.TN3270 contributors.
 */

using System.Text;
using FuzzyMainframes.TN3270;

namespace FuzzyMainframes.TN3270.Tests;

public class ResponseParseTests
{
    private const byte SBA = 0x11;

    private static readonly ICodepage Cp;

    static ResponseParseTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp = new BclCodepage("IBM01047");
    }

    /// <summary>
    /// Builds a synthetic Read-Modified inbound buffer:
    ///   [AID][cursor-hi][cursor-lo] (SBA section)*  IAC EOR
    /// Each section is encoded by the caller via <see cref="EncodeField"/>.
    /// </summary>
    private static byte[] BuildInbound(AID aid, int cursorRow, int cursorCol, params byte[][] sections)
    {
        var cursor = Utils.GetPosition(cursorRow, cursorCol);
        var stream = new List<byte> { (byte)aid, cursor[0], cursor[1] };
        foreach (var section in sections)
            stream.AddRange(section);
        // IAC EOR terminator that ParseFieldsScreen looks for.
        stream.Add(0xff);
        stream.Add(0xef);
        return stream.ToArray();
    }

    /// <summary>
    /// Encodes one SBA + EBCDIC-data section. <paramref name="attrRow"/>/<paramref name="attrCol"/>
    /// is the field's attribute-byte position (the position registered on Field). The SBA
    /// itself points one cell forward — that's what real terminals send, and the parser
    /// subtracts 1 internally.
    /// </summary>
    private static byte[] EncodeField(int attrRow, int attrCol, string value)
    {
        // SBA address = attribute-byte address + 1, with screen wrap.
        var attrAddress = (attrRow - 1) * 80 + (attrCol - 1);
        var (sbaRow, sbaCol) = Utils.DecodeAddress(attrAddress + 1);
        var sba = Utils.GetPosition(sbaRow, sbaCol);
        var data = Cp.Encode(value);
        var bytes = new List<byte> { SBA, sba[0], sba[1] };
        bytes.AddRange(data);
        return bytes.ToArray();
    }

    [Fact]
    public void Parse_PopulatesFieldByAttributeBytePosition()
    {
        var screen = new Screen();
        screen.AddInput(5, 10, "name");

        var buffer = BuildInbound(AID.Enter, 5, 11,
            EncodeField(5, 10, "Robert"));

        var resp = new Response(buffer, Cp);
        resp.ParseFieldsScreen(screen);

        Assert.Equal(AID.Enter, resp.ActionID);
        Assert.Equal("Robert", screen.GetFieldData("name"));
    }

    [Fact]
    public void Parse_TrimsByDefault()
    {
        var screen = new Screen();
        screen.AddInput(1, 1, "name");

        // Buffer pads to field length with EBCDIC space (0x40); decode then trim.
        var buffer = BuildInbound(AID.Enter, 1, 5,
            EncodeField(1, 1, "Bob   "));

        new Response(buffer, Cp).ParseFieldsScreen(screen);

        Assert.Equal("Bob", screen.GetFieldData("name"));
    }

    [Fact]
    public void Parse_KeepSpaces_PreservesPadding()
    {
        var screen = new Screen();
        screen.AddInput(1, 1, "name");
        // Find the field we just added and flip the opt-out.
        screen.Fields.Single(f => f.Name == "name").KeepSpaces = true;

        var buffer = BuildInbound(AID.Enter, 1, 5,
            EncodeField(1, 1, "Bob   "));

        new Response(buffer, Cp).ParseFieldsScreen(screen);

        Assert.Equal("Bob   ", screen.GetFieldData("name"));
    }

    [Fact]
    public void Parse_CursorPosition_NoOffset()
    {
        // Response.Row/Column should match the cursor's actual 1-based position.
        // Unlike the SBA in field sections, the cursor bytes are not pre-offset.
        var screen = new Screen();
        var buffer = BuildInbound(AID.PF3, 12, 40);

        var resp = new Response(buffer, Cp);
        // No fields to parse — just verify cursor.
        Assert.Equal(AID.PF3, resp.ActionID);
        Assert.Equal(12, resp.Row);
        Assert.Equal(40, resp.Column);
    }

    [Fact]
    public void Parse_MultipleFields_PopulatesAllByName()
    {
        var screen = new Screen();
        screen.AddInput(3, 5, "first");
        screen.AddInput(7, 12, "second");

        var buffer = BuildInbound(AID.Enter, 7, 20,
            EncodeField(3, 5, "alpha"),
            EncodeField(7, 12, "beta"));

        new Response(buffer, Cp).ParseFieldsScreen(screen);

        Assert.Equal("alpha", screen.GetFieldData("first"));
        Assert.Equal("beta", screen.GetFieldData("second"));
    }

    [Fact]
    public void Parse_FieldAtScreenStart_SbaWrapsBackToLastCell()
    {
        // A field whose attribute byte sits at (24, 80) (the very last cell)
        // is unusual but valid: the SBA inside the response points at the
        // first character of the input area, which wraps to (1, 1). Stepping
        // back one (DecodeAddress(-1)) must wrap to (24, 80) to match.
        var screen = new Screen();
        screen.AddInput(24, 80, "wrapped");

        // SBA address = 1919 + 1 = 1920 → DecodeAddress wraps to (1, 1).
        var buffer = BuildInbound(AID.Enter, 1, 5,
            EncodeField(24, 80, "x"));

        new Response(buffer, Cp).ParseFieldsScreen(screen);

        Assert.Equal("x", screen.GetFieldData("wrapped"));
    }

    [Fact]
    public void Parse_ShortFrame_NoCursorBytes_DoesNotThrow()
    {
        // Some PA / Clear variants ship just the AID byte. ReadCursorPosition
        // should bail without indexing past the end.
        var resp = new Response(new byte[] { (byte)AID.Clear }, Cp);
        Assert.Equal(AID.Clear, resp.ActionID);
        Assert.Equal(0, resp.Row);
        Assert.Equal(0, resp.Column);
    }

    [Fact]
    public void Parse_UnknownFieldPosition_IgnoresSilently()
    {
        // Response carries a field section for a position the screen doesn't
        // know about — SetFieldValue returns without error and leaves the
        // known field untouched.
        var screen = new Screen();
        screen.AddInput(1, 1, "known");
        screen.Fields.Single(f => f.Name == "known").Contents = "preset";

        var buffer = BuildInbound(AID.Enter, 1, 5,
            EncodeField(10, 10, "ghost"));

        new Response(buffer, Cp).ParseFieldsScreen(screen);

        Assert.Equal("preset", screen.GetFieldData("known"));
    }
}
