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
  public class TimingUnitTest
  {
    [TestMethod]
    public void BallCompletesExpectedMovesTest()
    {
      Ball ball = new(new Vector(100.0, 100.0), new Vector(10.0, 10.0));
      int count = 0;
      ManualResetEventSlim done = new(false);

      ball.NewPositionNotification += (sender, pos) =>
      {
        if (Interlocked.Increment(ref count) >= 500)
          done.Set();
      };

      bool completed = done.Wait(TimeSpan.FromSeconds(10));
      ball.Stop();

      Assert.IsTrue(completed, $"Ball completed only {count} moves in 10 seconds; expected at least 500 (50/s at 10ms tick).");
    }

    [TestMethod]
    public void BallNotificationIntervalTest()
    {
      Ball ball = new(new Vector(100.0, 100.0), new Vector(10.0, 10.0));
      List<long> timestamps = [];
      ManualResetEventSlim done = new(false);
      const int target = 30;
      Stopwatch sw = Stopwatch.StartNew();

      ball.NewPositionNotification += (sender, pos) =>
      {
        lock (timestamps)
        {
          timestamps.Add(sw.ElapsedMilliseconds);
          if (timestamps.Count >= target)
            done.Set();
        }
      };

      bool signaled = done.Wait(TimeSpan.FromSeconds(5));
      ball.Stop();

      Assert.IsTrue(signaled, "Ball did not produce enough notifications in time.");
      Assert.IsTrue(timestamps.Count >= target);

      List<double> intervals = [];
      for (int i = 1; i < timestamps.Count; i++)
        intervals.Add(timestamps[i] - timestamps[i - 1]);

      double avgMs = intervals.Average();
      Assert.IsTrue(avgMs >= 5.0 && avgMs <= 50.0,
          $"Average notification interval {avgMs:F1}ms should be in range [5ms, 50ms] (target ~10ms).");
    }

    [TestMethod]
    public void SchedulingEffectivenessTest()
    {
      using DataImplementation impl = new();
      const int numberOfBalls = 5;
      List<long> firstNotificationTimes = [];
      object sync = new();
      ManualResetEventSlim done = new(false);
      Stopwatch sw = Stopwatch.StartNew();

      impl.Start(numberOfBalls, (pos, ball) =>
      {
        ball.NewPositionNotification += (sender, e) =>
        {
          lock (sync)
          {
            if (firstNotificationTimes.Count < numberOfBalls)
            {
              firstNotificationTimes.Add(sw.ElapsedMilliseconds);
              if (firstNotificationTimes.Count == numberOfBalls)
                done.Set();
            }
          }
        };
      });

      bool allStarted = done.Wait(TimeSpan.FromSeconds(5));
      Assert.IsTrue(allStarted, "Not all balls produced their first notification in time.");
      Assert.AreEqual(numberOfBalls, firstNotificationTimes.Count);

      long spread = firstNotificationTimes.Max() - firstNotificationTimes.Min();
      Assert.IsTrue(spread < 100L,
          $"First notifications spread {spread}ms should be < 100ms across all {numberOfBalls} balls.");
    }

    [TestMethod]
    public void HighPrecisionTimerResolutionTest()
    {
      Assert.IsTrue(Stopwatch.IsHighResolution,
          "System should provide a high-resolution timer for accurate scheduling.");

      Stopwatch sw = Stopwatch.StartNew();
      long freq = Stopwatch.Frequency;
      Assert.IsTrue(freq >= 1000L,
          $"Timer frequency {freq} Hz is too low for sub-millisecond precision.");

      Ball ball = new(new Vector(100.0, 100.0), new Vector(10.0, 0.0));
      List<long> ticks = [];
      ManualResetEventSlim done = new(false);

      ball.NewPositionNotification += (sender, pos) =>
      {
        lock (ticks)
        {
          ticks.Add(Stopwatch.GetTimestamp());
          if (ticks.Count >= 10) done.Set();
        }
      };

      done.Wait(TimeSpan.FromSeconds(2));
      ball.Stop();

      Assert.IsTrue(ticks.Count >= 10);
      List<double> intervalsMs = [];
      for (int i = 1; i < ticks.Count; i++)
        intervalsMs.Add((double)(ticks[i] - ticks[i - 1]) / Stopwatch.Frequency * 1000.0);

      double avg = intervalsMs.Average();
      Assert.IsTrue(avg >= 5.0 && avg <= 50.0,
          $"High-resolution average interval {avg:F2}ms should be in [5ms, 50ms].");
    }
  }
}
