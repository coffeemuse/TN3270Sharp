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

public class UtilsPositionTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 80)]
    [InlineData(24, 1)]
    [InlineData(24, 80)]
    [InlineData(12, 40)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    public void GetPosition_DecodePosition_RoundTrip(int row, int col)
    {
        var bytes = Utils.GetPosition(row, col);
        Assert.Equal(2, bytes.Length);

        var (decodedRow, decodedCol) = Utils.DecodePosition(bytes[0], bytes[1]);
        Assert.Equal(row, decodedRow);
        Assert.Equal(col, decodedCol);
    }

    [Fact]
    public void GetPosition_OneOne_EncodesAsZeroAddress()
    {
        // (row=1, col=1) is buffer address 0 → IOCodes[0]=0x40 in both halves.
        var bytes = Utils.GetPosition(1, 1);
        Assert.Equal(0x40, bytes[0]);
        Assert.Equal(0x40, bytes[1]);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(79, 1, 80)]
    [InlineData(80, 2, 1)]
    [InlineData(1919, 24, 80)]
    public void DecodeAddress_KnownAddresses(int address, int expectedRow, int expectedCol)
    {
        var (row, col) = Utils.DecodeAddress(address);
        Assert.Equal(expectedRow, row);
        Assert.Equal(expectedCol, col);
    }

    [Fact]
    public void DecodeAddress_NegativeOne_WrapsToLastCell()
    {
        // Used by Response.ParseFieldsScreen to step back from the SBA address
        // (first char of input area) to the field's attribute byte position.
        // Address -1 should wrap to (24, 80) on a 24x80 screen.
        var (row, col) = Utils.DecodeAddress(-1);
        Assert.Equal(24, row);
        Assert.Equal(80, col);
    }

    [Fact]
    public void DecodeAddress_AcrossRowBoundary_StepsBackOne()
    {
        // Field attribute at (1,1) means the SBA points to (1,2). Stepping back
        // by 1 from address 1 yields (1,1) again — protects against the off-by-one
        // that the SBA-1 rule guards against.
        var (row, col) = Utils.DecodeAddress(1 - 1);
        Assert.Equal(1, row);
        Assert.Equal(1, col);
    }
}
