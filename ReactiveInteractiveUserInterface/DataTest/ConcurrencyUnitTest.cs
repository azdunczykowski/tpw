//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class ConcurrencyUnitTest
  {
    private const int TargetPeriodMs  = 16;
    private const int MeasureMs       = 500;
    private const int MinMovesPerBall = (MeasureMs / TargetPeriodMs) / 2;

    private static DiagnosticLogger CreateTempLogger() =>
      new DiagnosticLogger(Path.Combine(Path.GetTempPath(), $"conc_{Guid.NewGuid():N}.txt"));

    [TestMethod]
    public void MultipleBalls_RunConcurrentlyTest()
    {
      const int numberOfBalls = 10;
      int[] moveCounts = new int[numberOfBalls];

      using DiagnosticLogger logger = CreateTempLogger();
      using DataImplementation dataLayer = new DataImplementation(logger);

      int index = 0;
      dataLayer.Start(numberOfBalls, (_, ball) =>
      {
        int i = index++;
        ball.NewPositionNotification += (_, _) =>
          Interlocked.Increment(ref moveCounts[i]);
      });

      var sw = System.Diagnostics.Stopwatch.StartNew();
      Thread.Sleep(MeasureMs);
      sw.Stop();

      for (int i = 0; i < numberOfBalls; i++)
      {
        Assert.IsTrue(moveCounts[i] >= MinMovesPerBall,
          $"Kulka {i} wykonała tylko {moveCounts[i]} ruchów w {sw.ElapsedMilliseconds}ms. " +
          $"Oczekiwano min {MinMovesPerBall}. Szeregowanie nieefektywne.");
      }
    }
  }
}
