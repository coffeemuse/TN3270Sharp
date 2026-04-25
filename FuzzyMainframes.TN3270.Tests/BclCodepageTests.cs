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

public class BclCodepageTests
{
    static BclCodepageTests()
    {
        // Tn3270Server normally registers this; tests instantiate BclCodepage
        // directly so they need to register it themselves.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Theory]
    [InlineData("IBM01047")]
    [InlineData("IBM037")]
    public void Encode_Decode_PrintableAsciiRoundTrip(string codepage)
    {
        var cp = new BclCodepage(codepage);
        const string input = "Hello, world! 0123456789";

        var encoded = cp.Encode(input);
        var decoded = cp.Decode(encoded);

        Assert.Equal(input, decoded);
        Assert.Equal(input.Length, encoded.Length);
    }

    [Fact]
    public void Id_ReportsEncodingName()
    {
        var cp = new BclCodepage("IBM01047");
        Assert.Equal("IBM01047", cp.Id);
    }

    [Fact]
    public void Cp1047_And_Cp037_DifferOnSquareBrackets()
    {
        // The whole point of switching the default to CP1047: CP1047 places
        // `[` at 0xAD and `]` at 0xBD (matching what most modern hosts ship),
        // while CP037 has them at 0xBA and 0xBB. Anything that flips the
        // default back to CP037 will surface as different bytes here.
        var cp1047 = new BclCodepage("IBM01047");
        var cp037 = new BclCodepage("IBM037");

        var brackets1047 = cp1047.Encode("[]");
        var brackets037 = cp037.Encode("[]");

        Assert.NotEqual(brackets1047, brackets037);
        Assert.Equal(new byte[] { 0xAD, 0xBD }, brackets1047);
        Assert.Equal(new byte[] { 0xBA, 0xBB }, brackets037);
    }

    [Fact]
    public void Encode_Space_To_Cp1047_X40()
    {
        // EBCDIC space is 0x40 in both CP1047 and CP037 — a trivial sanity
        // check that the Encoding.Convert pipeline isn't doing something weird
        // like surrogate substitution on plain ASCII.
        var cp = new BclCodepage("IBM01047");
        var bytes = cp.Encode(" ");
        Assert.Single(bytes);
        Assert.Equal(0x40, bytes[0]);
    }

    [Fact]
    public void Decode_EmptyArray_ReturnsEmptyString()
    {
        var cp = new BclCodepage("IBM01047");
        Assert.Equal(string.Empty, cp.Decode(Array.Empty<byte>()));
    }
}
