/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * MIT License
 *
 * Copyright (c) 2020-2026 by Robert J. Lawrence (roblthegreat) and other
 * FuzzyMainframes.TN3270 contributors.
 */

using System.Net.Sockets;
using System.Text;
using FuzzyMainframes.TN3270;

namespace FuzzyMainframes.TN3270.Tests;

/// <summary>
/// Wire-byte tests for <see cref="Telnet.SendScreen(Screen, int, int, bool)"/>.
/// These run in CI without external dependencies — they capture the stream
/// output and assert on the protocol bytes directly. End-to-end MDT-preservation
/// behavior (which depends on the client honoring WCC 0xc2) is covered visually
/// in the s3270 integration tests; this file pins down the bytes we send.
/// </summary>
public class TelnetSendScreenTests
{
    static TelnetSendScreenTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static byte[] CaptureSendScreen(Screen screen, int row, int col, bool noClear)
    {
        var stream = new MemoryStream();
        using var tcp = new TcpClient();
        var telnet = new Telnet(tcp, stream, new BclCodepage("IBM01047"));
        telnet.SendScreen(screen, row, col, noClear);
        return stream.ToArray();
    }

    [Fact]
    public void Default_EmitsEraseWrite_WithCursorReposition()
    {
        // Empty screen makes the byte sequence trivial:
        //   0xf5  EraseWrite
        //   0xc3  WCCdefault (reset MDT, restore keyboard)
        //   0x11  SBA
        //   XX XX buffer address for cursor (5,8)
        //   0x13  IC
        //   0xff  IAC
        //   0xef  EOR
        var screen = new Screen { InitialCursorPosition = (5, 8) };

        var bytes = CaptureSendScreen(screen, 5, 8, noClear: false);

        Assert.Equal(8, bytes.Length);
        Assert.Equal(0xf5, bytes[0]);
        Assert.Equal(0xc3, bytes[1]);
        Assert.Equal(0x11, bytes[2]);
        // bytes[3..5] are the encoded buffer address — exact value comes from
        // Utils.GetPosition and is covered by UtilsPositionTests.
        Assert.Equal(0x13, bytes[5]);
        Assert.Equal(0xff, bytes[6]);
        Assert.Equal(0xef, bytes[7]);
    }

    [Fact]
    public void NoClear_EmitsWrite_AndOmitsCursorReposition()
    {
        // Empty screen + noClear:
        //   0xf1  Write
        //   0xc2  WCCnoReset (preserve MDT, restore keyboard)
        //   0xff  IAC
        //   0xef  EOR
        // No SBA/IC trailer — cursor stays where the user left it.
        var screen = new Screen { InitialCursorPosition = (5, 8) };

        var bytes = CaptureSendScreen(screen, 5, 8, noClear: true);

        Assert.Equal(4, bytes.Length);
        Assert.Equal(0xf1, bytes[0]);
        Assert.Equal(0xc2, bytes[1]);
        Assert.Equal(0xff, bytes[2]);
        Assert.Equal(0xef, bytes[3]);
    }

    [Fact]
    public void NoClear_StillEmitsFieldOrders()
    {
        // A field present in the screen still gets written on the wire under
        // NoClear — only the leading EraseWrite and the trailing SBA+IC are
        // suppressed. This pins the contract: fields you put in the screen
        // are emitted; if you want to leave a client field undisturbed,
        // omit it from the screen passed to ShowScreen.
        var screen = new Screen { InitialCursorPosition = (5, 8) };
        screen.AddText(1, 1, "hi", intensity: false);

        var bytes = CaptureSendScreen(screen, 5, 8, noClear: true);

        Assert.Equal(0xf1, bytes[0]);
        Assert.Equal(0xc2, bytes[1]);
        // Last two bytes are the IAC EOR trailer.
        Assert.Equal(0xff, bytes[^2]);
        Assert.Equal(0xef, bytes[^1]);
        // An SBA (0x11) is present somewhere in the middle for the field
        // position, so the body is non-trivial.
        Assert.Contains((byte)0x11, bytes[2..^2]);
    }
}
