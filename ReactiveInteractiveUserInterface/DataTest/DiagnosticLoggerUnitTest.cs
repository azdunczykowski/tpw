//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class DiagnosticLoggerUnitTest
  {
    [TestMethod]
    public void LoggerWritesAsciiEntriesTest()
    {
      string path = Path.Combine(Path.GetTempPath(), $"diag_test_{Guid.NewGuid():N}.txt");
      try
      {
        using (var logger = new DiagnosticLogger(path))
        {
          logger.Log(0, 10.5, 20.5, 30.0, -40.0, 100);
          logger.Log(1, 11.0, 21.0, -30.0, 40.0, 200);
        }

        Assert.IsTrue(File.Exists(path), "Plik diagnostyczny nie został utworzony.");

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.ASCII);
        Assert.IsTrue(lines.Length >= 3, $"Oczekiwano nagłówka + >=2 wpisów, jest {lines.Length} linii.");
        Assert.AreEqual("timestamp_ms;ball_id;pos_x;pos_y;vel_x;vel_y", lines[0].Trim());

        string[] parts = lines[1].Split(';');
        Assert.AreEqual(6, parts.Length, "Wpis powinien mieć 6 pól oddzielonych średnikami.");
        Assert.AreEqual("100", parts[0].Trim(), "Pole timestamp_ms");
        Assert.AreEqual("0",   parts[1].Trim(), "Pole ball_id");
        Assert.AreEqual("10.5000", parts[2].Trim(), "Pole pos_x");
      }
      finally
      {
        if (File.Exists(path)) File.Delete(path);
      }
    }

    [TestMethod]
    public void LoggerIsNonBlockingTest()
    {
      string path = Path.Combine(Path.GetTempPath(), $"diag_nb_{Guid.NewGuid():N}.txt");
      try
      {
        using var logger = new DiagnosticLogger(path);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 2000; i++)
          logger.Log(i % 10, i, i, i, i, i);
        sw.Stop();

        Assert.IsTrue(sw.ElapsedMilliseconds < 500,
          $"Log() blokuje wątek – 2000 wywołań zajęło {sw.ElapsedMilliseconds}ms (oczekiwano <500ms).");
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

        Assert.AreEqual(posNoLog.x, posWithLog.x, 1e-9, "Logger zmienił pozycję X kuli.");
        Assert.AreEqual(posNoLog.y, posWithLog.y, 1e-9, "Logger zmienił pozycję Y kuli.");
        Assert.AreEqual(velNoLog.x, velWithLog.x, 1e-9, "Logger zmienił prędkość X kuli.");
        Assert.AreEqual(velNoLog.y, velWithLog.y, 1e-9, "Logger zmienił prędkość Y kuli.");
      }
      finally
      {
        if (File.Exists(path)) File.Delete(path);
      }
    }
  }
}
