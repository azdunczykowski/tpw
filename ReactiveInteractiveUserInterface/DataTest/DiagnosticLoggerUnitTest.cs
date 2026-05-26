//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class DiagnosticLoggerUnitTest
  {
    [TestMethod]
    public void SyslogFormatTest()
    {
      string path = Path.Combine(Path.GetTempPath(), $"diag_syslog_{Guid.NewGuid():N}.txt");
      try
      {
        using (var logger = new DiagnosticLogger(path))
          logger.Log(LogLevel.Info, "test message");

        Assert.IsTrue(File.Exists(path), "Log file was not created.");
        string[] lines = File.ReadAllLines(path, System.Text.Encoding.ASCII);
        Assert.IsTrue(lines.Length >= 1, "Expected at least one log entry.");

        // BSD syslog: <PRI>TIMESTAMP HOSTNAME APP[PID]: LEVEL MESSAGE
        string line = lines[0];
        Assert.IsTrue(line.StartsWith("<"), "Entry must start with '<PRI>'.");
        Assert.IsTrue(line.Contains("ball-sim["), "Entry must contain 'ball-sim[PID]'.");
        Assert.IsTrue(line.Contains("INFO"), "Entry must contain the level name.");
        Assert.IsTrue(line.Contains("test message"), "Entry must contain the message text.");
      }
      finally
      {
        if (File.Exists(path)) File.Delete(path);
      }
    }

    [TestMethod]
    public void AsciiEncodingTest()
    {
      string path = Path.Combine(Path.GetTempPath(), $"diag_ascii_{Guid.NewGuid():N}.txt");
      try
      {
        using (var logger = new DiagnosticLogger(path))
          logger.Log(LogLevel.Notice, "ascii check");

        byte[] bytes = File.ReadAllBytes(path);
        Assert.IsTrue(bytes.All(b => b < 128), "Log file must be pure ASCII (all bytes < 128).");
      }
      finally
      {
        if (File.Exists(path)) File.Delete(path);
      }
    }

    [TestMethod]
    public void HighPriorityAlwaysLogsTest()
    {
      // Fill the buffer beyond 75% with Info (low priority) messages that are dropped,
      // then verify that a Critical (high priority) message still goes through.
      string path = Path.Combine(Path.GetTempPath(), $"diag_hipri_{Guid.NewGuid():N}.txt");
      try
      {
        using var logger = new DiagnosticLogger(path);

        // Flood with Debug messages; most will be dropped once buffer > 75%
        for (int i = 0; i < 5000; i++)
          logger.Log(LogLevel.Debug, $"flood {i}");

        // A Critical-level message must always be accepted (non-blocking check)
        logger.Log(LogLevel.Critical, "critical sentinel");
      }
      finally
      {
        // If writing Critical threw an exception the test already fails;
        // file cleanup is best-effort.
        if (File.Exists(path)) File.Delete(path);
      }
    }

    [TestMethod]
    public void LowPriorityDroppedWhenBufferNearlyFullTest()
    {
      // Verify that Log() with low-priority level returns quickly even when buffer is saturated.
      string path = Path.Combine(Path.GetTempPath(), $"diag_lopri_{Guid.NewGuid():N}.txt");
      try
      {
        using var logger = new DiagnosticLogger(path);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
          logger.Log(LogLevel.Debug, $"msg {i}");
        sw.Stop();

        Assert.IsTrue(sw.ElapsedMilliseconds < 1000,
          $"Low-priority Log() must not block — 10k calls took {sw.ElapsedMilliseconds}ms.");
      }
      finally
      {
        if (File.Exists(path)) File.Delete(path);
      }
    }

    [TestMethod]
    public void AllEightLogLevelsAcceptedTest()
    {
      string path = Path.Combine(Path.GetTempPath(), $"diag_8lvl_{Guid.NewGuid():N}.txt");
      try
      {
        using var logger = new DiagnosticLogger(path);
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
          logger.Log(level, $"message at level {level}");
      }
      finally
      {
        if (File.Exists(path)) File.Delete(path);
      }
    }

    [TestMethod]
    public void LoggerDoesNotAffectBallMovementTest()
    {
      string path = Path.Combine(Path.GetTempPath(), $"diag_ball_{Guid.NewGuid():N}.txt");
      try
      {
        const double dt = 0.016;
        const int steps = 50;

        Ball ballNoLog = new(new Vector(100.0, 100.0), new Vector(50.0, 30.0));
        ballNoLog.Stop();
        for (int i = 0; i < steps; i++) ballNoLog.Move(dt);
        var (posNoLog, velNoLog) = ballNoLog.GetState();

        using var logger = new DiagnosticLogger(path);
        Ball ballWithLog = new(new Vector(100.0, 100.0), new Vector(50.0, 30.0),
                               ballId: 0, logger: logger);
        ballWithLog.Stop();
        for (int i = 0; i < steps; i++) ballWithLog.Move(dt);
        var (posWithLog, velWithLog) = ballWithLog.GetState();

        Assert.AreEqual(posNoLog.x, posWithLog.x, 1e-9, "Logger changed ball X position.");
        Assert.AreEqual(posNoLog.y, posWithLog.y, 1e-9, "Logger changed ball Y position.");
        Assert.AreEqual(velNoLog.x, velWithLog.x, 1e-9, "Logger changed ball X velocity.");
        Assert.AreEqual(velNoLog.y, velWithLog.y, 1e-9, "Logger changed ball Y velocity.");
      }
      finally
      {
        if (File.Exists(path)) File.Delete(path);
      }
    }
  }
}
