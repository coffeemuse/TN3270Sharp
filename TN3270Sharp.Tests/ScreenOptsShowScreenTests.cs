/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * MIT License
 *
 * Copyright (c) 2020-2026 by Robert J. Lawrence (roblthegreat) and other
 * TN3270Sharp contributors.
 */

using TN3270Sharp;
using TN3270Sharp.Tests.TestSupport;

namespace TN3270Sharp.Tests;

/// <summary>
/// Integration tests for the <see cref="ScreenOpts"/>-driven
/// <see cref="ITn3270ConnectionHandler.ShowScreen"/> primary. Skipped unless
/// <c>TN3270SHARP_RUN_S3270=1</c> is set and <c>s3270</c> is on PATH.
/// </summary>
public class ScreenOptsShowScreenTests
{
    private static bool ShouldRun =>
        Environment.GetEnvironmentVariable("TN3270SHARP_RUN_S3270") == "1"
        && S3270Harness.IsAvailable;

    private const string SkipMessage =
        "ScreenOpts integration tests skipped (set TN3270SHARP_RUN_S3270=1 and ensure s3270 is on PATH).";

    /// <summary>
    /// Boots a <see cref="Tn3270Server"/> on a free port from a background
    /// thread, runs <paramref name="handlerBody"/> as the per-connection
    /// handler, then runs <paramref name="testBody"/> with a connected
    /// <see cref="S3270Harness"/>. The listener thread is daemon-flagged and
    /// exits with the test process — clean shutdown is a Tier 3.2 concern.
    /// </summary>
    private static void RunWithServer(
        Action<ITn3270ConnectionHandler> handlerBody,
        Action<S3270Harness> testBody)
    {
        var port = S3270Harness.FindFreePort();
        new Thread(() =>
        {
            new Tn3270Server("127.0.0.1", port).StartListener(
                breakCondition: () => false,
                whenHasNewConnection: () => { },
                whenConnectionIsClosed: () => { },
                handleConnectionAction: handlerBody);
        }) { IsBackground = true }.Start();

        Thread.Sleep(200);

        using var s3270 = new S3270Harness(port);
        s3270.Connect();
        testBody(s3270);
    }

    [Fact]
    public void NoResponse_ReturnsWithoutBlocking()
    {
        if (!ShouldRun) { Console.WriteLine(SkipMessage); return; }

        var noResponseReturned = new ManualResetEventSlim(false);

        RunWithServer(
            handlerBody: handler =>
            {
                var screen = new Screen { InitialCursorPosition = (3, 5) };
                screen.AddText(1, 1, "no-response test", intensity: true);
                screen.AddInput(3, 4, 20, "echo");

                handler.ShowScreen(screen, new ScreenOpts { NoResponse = true });
                noResponseReturned.Set();

                // Hold the connection so the test can finish cleanly.
                handler.SetAidAction(AID.PF3, handler.CloseConnection);
                handler.ShowScreen(screen);
            },
            testBody: s3270 =>
            {
                Assert.True(noResponseReturned.Wait(TimeSpan.FromSeconds(2)),
                    "ShowScreen with NoResponse=true blocked instead of returning.");
                s3270.Send("PF(3)");
            });
    }

    [Fact]
    public void PostSendCallback_FiresAfterWire_BeforeBlockingRead()
    {
        if (!ShouldRun) { Console.WriteLine(SkipMessage); return; }

        // PostSendCallback runs synchronously in ShowScreen right after
        // Telnet.SendScreen returns; ScreenBufferProcess runs inside the
        // Telnet.Read callback after the AID arrives. Stamp only the first
        // invocation of each — Telnet.Read may dispatch its action across
        // multiple network reads, so ScreenBufferProcess can fire more than
        // once. The ordering between the first PostSendCallback and the first
        // ScreenBufferProcess is what the contract guarantees.
        var counter = 0;
        var postSendOrder = 0;
        var bufferProcessOrder = 0;
        var done = new ManualResetEventSlim(false);

        RunWithServer(
            handlerBody: handler =>
            {
                var screen = new Screen { InitialCursorPosition = (3, 5) };
                screen.AddText(1, 1, "callback ordering", intensity: true);
                screen.AddInput(3, 4, 20, "echo");

                handler.SetAidAction(AID.PF3, handler.CloseConnection);
                handler.ShowScreen(screen, new ScreenOpts
                {
                    PostSendCallback = _ =>
                    {
                        if (postSendOrder == 0)
                            postSendOrder = Interlocked.Increment(ref counter);
                    },
                    ScreenBufferProcess = _ =>
                    {
                        if (bufferProcessOrder == 0)
                            bufferProcessOrder = Interlocked.Increment(ref counter);
                    },
                });
                done.Set();
            },
            testBody: s3270 =>
            {
                s3270.Send("PF(3)");
                Assert.True(done.Wait(TimeSpan.FromSeconds(5)),
                    "Handler did not finish after PF(3).");
                Assert.Equal(1, postSendOrder);
                Assert.Equal(2, bufferProcessOrder);
            });
    }

    [Fact]
    public void PostSendCallback_FiresEvenWhen_NoResponse()
    {
        if (!ShouldRun) { Console.WriteLine(SkipMessage); return; }

        var callbackFired = new ManualResetEventSlim(false);
        var receivedData = new object();
        object? observedData = null;

        RunWithServer(
            handlerBody: handler =>
            {
                var screen = new Screen { InitialCursorPosition = (3, 5) };
                screen.AddText(1, 1, "no-response + callback", intensity: true);
                screen.AddInput(3, 4, 20, "echo");

                handler.ShowScreen(screen, new ScreenOpts
                {
                    NoResponse = true,
                    PostSendCallback = data => { observedData = data; callbackFired.Set(); },
                    CallbackData = receivedData,
                });

                handler.SetAidAction(AID.PF3, handler.CloseConnection);
                handler.ShowScreen(screen);
            },
            testBody: s3270 =>
            {
                Assert.True(callbackFired.Wait(TimeSpan.FromSeconds(2)),
                    "PostSendCallback did not fire when NoResponse=true.");
                Assert.Same(receivedData, observedData);
                s3270.Send("PF(3)");
            });
    }

    [Fact]
    public void CursorRow_CursorCol_Override_PlacesCursorThere()
    {
        if (!ShouldRun) { Console.WriteLine(SkipMessage); return; }

        RunWithServer(
            handlerBody: handler =>
            {
                var screen = new Screen { InitialCursorPosition = (1, 1) };
                screen.AddText(1, 1, "cursor override", intensity: true);
                // Attribute byte at (10, 19) — input area starts at (10, 20),
                // matching where ScreenOpts will place the cursor. Wait(InputField)
                // requires the cursor to land in an unprotected area.
                screen.AddInput(10, 19, 20, "target");

                handler.SetAidAction(AID.PF3, handler.CloseConnection);
                handler.ShowScreen(screen, new ScreenOpts
                {
                    CursorRow = 10,
                    CursorCol = 20,
                });
            },
            testBody: s3270 =>
            {
                Assert.Equal((10, 20), s3270.Cursor);
                s3270.Send("PF(3)");
            });
    }

    [Fact]
    public void CursorRow_CursorCol_Zero_FallsBackTo_InitialCursorPosition()
    {
        if (!ShouldRun) { Console.WriteLine(SkipMessage); return; }

        RunWithServer(
            handlerBody: handler =>
            {
                var screen = new Screen { InitialCursorPosition = (5, 8) };
                screen.AddText(1, 1, "fallback cursor", intensity: true);
                // Attribute byte at (5, 7) so input area starts at (5, 8).
                screen.AddInput(5, 7, 20, "target");

                handler.SetAidAction(AID.PF3, handler.CloseConnection);
                // Default ScreenOpts: CursorRow/CursorCol both 0.
                handler.ShowScreen(screen, new ScreenOpts());
            },
            testBody: s3270 =>
            {
                Assert.Equal((5, 8), s3270.Cursor);
                s3270.Send("PF(3)");
            });
    }
}
