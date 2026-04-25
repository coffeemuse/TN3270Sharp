/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * MIT License
 *
 * Copyright (c) 2020-2026 by Robert J. Lawrence (roblthegreat) and other
 * TN3270Sharp contributors.
 */

using TN3270Sharp;

namespace TN3270Sharp.Tests;

public class ScreenBuildFieldTests
{
    private const byte SF = 0x1d;
    private const byte SFE = 0x29;
    private const byte BasicFieldAttrMarker = 0xc0;
    private const byte HighlightMarker = 0x41;
    private const byte ColorMarker = 0x42;

    private static Screen NewScreen() => new();

    [Fact]
    public void ProtectedTextField_DefaultStyle_EmitsSf()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = false };

        var bytes = screen.BuildField(field);

        // Protected (0x20) | Alpha | Normal | Normal = 0x20.
        Assert.Equal(new byte[] { SF, 0x20 }, bytes);
    }

    [Fact]
    public void WriteableInputField_DefaultStyle_EmitsSf()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = true };

        var bytes = screen.BuildField(field);

        // Unprotected | Alpha | Normal | Normal = 0x00.
        Assert.Equal(new byte[] { SF, 0x00 }, bytes);
    }

    [Fact]
    public void ProtectedTextField_Intensity_SetsIntensityBit()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = false, Intensity = true };

        var bytes = screen.BuildField(field);

        // 0x20 (Protected) | 0x08 (Intensity) = 0x28.
        Assert.Equal(new byte[] { SF, 0x28 }, bytes);
    }

    [Fact]
    public void HiddenWriteableField_SetsHiddenBits()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = true, Hidden = true };

        var bytes = screen.BuildField(field);

        // Unprotected | Alpha | Hidden (0x0C) = 0x0C.
        Assert.Equal(new byte[] { SF, 0x0C }, bytes);
    }

    [Fact]
    public void Writeable_NumericOnly_SetsNumericBit()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = true, NumericOnly = true };

        var bytes = screen.BuildField(field);

        // Unprotected (0x00) | Numeric (0x10) = 0x10.
        Assert.Equal(new byte[] { SF, 0x10 }, bytes);
    }

    [Fact]
    public void Protected_Autoskip_SetsNumericBit()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = false, Autoskip = true };

        var bytes = screen.BuildField(field);

        // Autoskip is implemented as Protected+Numeric: 0x20 | 0x10 = 0x30.
        Assert.Equal(new byte[] { SF, 0x30 }, bytes);
    }

    [Fact]
    public void NumericOnly_OnProtectedField_IsIgnored()
    {
        // NumericOnly is a writeable-input hint; on protected fields it must
        // not flip the Numeric bit (otherwise the field would auto-skip
        // unintentionally).
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = false, NumericOnly = true };

        var bytes = screen.BuildField(field);

        Assert.Equal(new byte[] { SF, 0x20 }, bytes);
    }

    [Fact]
    public void Autoskip_OnWriteableField_IsIgnored()
    {
        // Autoskip is a protected-only flag; setting it on a writeable field
        // must not flip the Numeric bit (which would change input validation).
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = true, Autoskip = true };

        var bytes = screen.BuildField(field);

        Assert.Equal(new byte[] { SF, 0x00 }, bytes);
    }

    [Fact]
    public void ColorOnly_EmitsSfeWithColorParam()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = false, Color = Colors.Red };

        var bytes = screen.BuildField(field);

        Assert.Equal(
            new byte[] { SFE, 0x02, BasicFieldAttrMarker, 0x20, ColorMarker, (byte)Colors.Red },
            bytes);
    }

    [Fact]
    public void HighlightOnly_EmitsSfeWithHighlightParam()
    {
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = true, Highlighting = Highlight.Underscore };

        var bytes = screen.BuildField(field);

        Assert.Equal(
            new byte[] { SFE, 0x02, BasicFieldAttrMarker, 0x00, HighlightMarker, (byte)Highlight.Underscore },
            bytes);
    }

    [Fact]
    public void ColorAndHighlight_EmitSfeWithBothParams_HighlightFirst()
    {
        var screen = NewScreen();
        var field = new Field(1, 1)
        {
            Write = true,
            Color = Colors.Yellow,
            Highlighting = Highlight.Blink
        };

        var bytes = screen.BuildField(field);

        // SFE / paramCount=3 / 0xc0 attr / highlight pair / color pair.
        // BuildField writes the highlight pair before the color pair.
        Assert.Equal(
            new byte[]
            {
                SFE, 0x03,
                BasicFieldAttrMarker, 0x00,
                HighlightMarker, (byte)Highlight.Blink,
                ColorMarker, (byte)Colors.Yellow
            },
            bytes);
    }

    [Fact]
    public void Sfe_NumericOnlyBit_StillGatedOnWrite()
    {
        // The numericBit gating must apply uniformly to both SF and SFE branches.
        // Writeable + NumericOnly + Color → SFE with attrByte = 0x10.
        var screen = NewScreen();
        var field = new Field(1, 1) { Write = true, NumericOnly = true, Color = Colors.Green };

        var bytes = screen.BuildField(field);

        Assert.Equal(
            new byte[] { SFE, 0x02, BasicFieldAttrMarker, 0x10, ColorMarker, (byte)Colors.Green },
            bytes);
    }
}
