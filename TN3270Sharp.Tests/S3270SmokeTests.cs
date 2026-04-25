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
/// Opt-in smoke test that boots a real <see cref="Tn3270Server"/> and connects
/// the <c>s3270</c> emulator to it. Skipped unless the environment variable
/// <c>TN3270SHARP_RUN_S3270</c> is set to <c>1</c> and <c>s3270</c> is on PATH.
/// Real integration tests against this harness arrive with Tier 3.2.
/// </summary>
public class S3270SmokeTests
{
    private static bool ShouldRun =>
        Environment.GetEnvironmentVariable("TN3270SHARP_RUN_S3270") == "1"
        && S3270Harness.IsAvailable;

    [Fact]
    public void Connect_RenderScreen_AndExitOnPf3()
    {
        if (!ShouldRun)
        {
            // Surface a hint on local runs so the user knows the test was a no-op.
            Console.WriteLine("S3270SmokeTests skipped (set TN3270SHARP_RUN_S3270=1 and ensure s3270 is on PATH).");
            return;
        }

        var port = S3270Harness.FindFreePort();
        var handlerCompleted = new ManualResetEventSlim(false);

        var serverThread = new Thread(() =>
        {
            // breakCondition stays false; the listener thread is a daemon and
            // dies with the test process. A cleaner shutdown is a Tier 3.2 task.
            new Tn3270Server("127.0.0.1", port).StartListener(
                breakCondition: () => false,
                whenHasNewConnection: () => { },
                whenConnectionIsClosed: () => { },
                handleConnectionAction: handler =>
                {
                    var screen = new Screen { InitialCursorPosition = (3, 5) };
                    screen.AddText(1, 1, "smoke test", intensity: true);
                    screen.AddInput(3, 4, 20, "echo");

                    handler.SetAidAction(AID.PF3, handler.CloseConnection);
                    handler.ShowScreen(screen);

                    handlerCompleted.Set();
                });
        }) { IsBackground = true };
        serverThread.Start();

        // Give the listener a moment to bind.
        Thread.Sleep(200);

        using var s3270 = new S3270Harness(port);
        s3270.Connect();

        var firstRow = s3270.AsciiRow(1);
        Assert.Contains("smoke test", firstRow);

        s3270.Send("PF(3)");

        Assert.True(handlerCompleted.Wait(TimeSpan.FromSeconds(5)),
            "Connection handler did not return after PF3.");
    }
}
