/*
 * This file is part of https://github.com/FuzzyMainframes/TN3270Sharp
 *
 * MIT License
 *
 * Copyright (c) 2020-2026 by Robert J. Lawrence (roblthegreat) and other
 * TN3270Sharp contributors.
 */

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TN3270Sharp.Tests.TestSupport;

/// <summary>
/// Drives the <c>s3270</c> scripted 3270 emulator (part of x3270) as a
/// subprocess so tests can interact with a real terminal client end-to-end.
///
/// Typical use:
///   using var harness = new S3270Harness(port);
///   harness.Connect();
///   harness.Send("String(\"hello\")");
///   harness.Send("Enter");
///   var line = harness.AsciiRow(1);
///
/// s3270's scripting protocol prints zero or more data lines, then a status
/// line, then either <c>ok</c> or <c>error</c> on its own line. <see cref="Send"/>
/// returns the data lines (status line stripped) and throws on <c>error</c>.
/// </summary>
internal sealed class S3270Harness : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private readonly int _port;

    public S3270Harness(int port)
    {
        _port = port;
        _process = new Process
        {
            StartInfo = new ProcessStartInfo("s3270", "-utf8 -model 2")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };
        _process.Start();
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;
    }

    /// <summary>True when <c>s3270</c> is on PATH and can be invoked.</summary>
    public static bool IsAvailable
    {
        get
        {
            try
            {
                using var probe = Process.Start(new ProcessStartInfo("s3270", "-v")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
                if (probe == null) return false;
                probe.WaitForExit(2000);
                return probe.HasExited && probe.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Find an unused TCP port to bind the test server to.</summary>
    public static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>Connect the emulator to <c>127.0.0.1:&lt;port&gt;</c> and wait
    /// until the field-formatted screen has arrived.</summary>
    public void Connect()
    {
        Send($"Connect(\"127.0.0.1:{_port}\")");
        // Wait for the host to send a formatted screen (i.e. a Write order with
        // at least one field) before letting the test poke at it.
        Send("Wait(InputField)");
    }

    /// <summary>
    /// Send one s3270 action and return the data lines it produced. Throws
    /// <see cref="InvalidOperationException"/> if s3270 reports <c>error</c>.
    /// </summary>
    public IReadOnlyList<string> Send(string command)
    {
        _stdin.WriteLine(command);
        _stdin.Flush();

        var dataLines = new List<string>();
        string? statusLine = null;
        while (true)
        {
            var line = _stdout.ReadLine();
            if (line == null)
                throw new InvalidOperationException(
                    $"s3270 closed stdout while waiting for a response to: {command}");

            if (line == "ok")
                return dataLines;

            if (line == "error")
                throw new InvalidOperationException(
                    $"s3270 reported error for: {command}\nStatus: {statusLine}\nData:\n{string.Join("\n", dataLines)}");

            // s3270 prefixes each non-result line with "data: " for actual
            // payload, and emits a status line just before ok/error. The
            // status line has no prefix and starts with one of U/L/E/N etc.
            if (line.StartsWith("data: ", StringComparison.Ordinal))
                dataLines.Add(line.Substring("data: ".Length));
            else
                statusLine = line;
        }
    }

    public string AsciiRow(int row) =>
        Send($"Ascii({row - 1},0,80)").FirstOrDefault() ?? string.Empty;

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                try { _stdin.WriteLine("Quit"); _stdin.Flush(); } catch { }
                _process.WaitForExit(1000);
                if (!_process.HasExited)
                    _process.Kill();
            }
        }
        finally
        {
            _stdin.Dispose();
            _stdout.Dispose();
            _process.Dispose();
        }
    }
}
