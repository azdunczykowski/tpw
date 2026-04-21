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
  public class BallThreadUnitTest
  {
    [TestMethod]
    public void EachBallHasOwnThreadTest()
    {
      using DataImplementation impl = new DataImplementation();
      int numberOfBalls = 3;
      HashSet<int> threadIds = new HashSet<int>();
      ManualResetEventSlim done = new ManualResetEventSlim(false);
      int eventCount = 0;
      int target = numberOfBalls * 3;

      impl.Start(numberOfBalls, (pos, ball) =>
      {
        ball.NewPositionNotification += (sender, e) =>
        {
          lock (threadIds)
          {
            threadIds.Add(Thread.CurrentThread.ManagedThreadId);
            eventCount++;
            if (eventCount >= target)
              done.Set();
          }
        };
      });

      bool signaled = done.Wait(TimeSpan.FromSeconds(5));
      Assert.IsTrue(signaled, "Balls did not raise enough notifications within timeout.");
      Assert.AreEqual(numberOfBalls, threadIds.Count, "Each ball must notify from its own unique thread.");
    }

    [TestMethod]
    public void BallVelocityIsNonIntegerTest()
    {
      using DataImplementation impl = new DataImplementation();
      int numberOfBalls = 5;
      bool anyNonInteger = false;

      impl.Start(numberOfBalls, (pos, ball) =>
      {
        IVector v = ball.Velocity;
        if (v.x != Math.Floor(v.x) || v.y != Math.Floor(v.y))
          anyNonInteger = true;
      });

      Assert.IsTrue(anyNonInteger, "Ball velocities should be non-integer floating-point values.");
    }

    [TestMethod]
    public void BallStaysWithinBoundsTest()
    {
      using DataImplementation impl = new DataImplementation();
      int numberOfBalls = 2;
      bool outOfBounds = false;
      ManualResetEventSlim done = new ManualResetEventSlim(false);
      int eventCount = 0;
      int target = numberOfBalls * 50;

      impl.Start(numberOfBalls, (pos, ball) =>
      {
        ball.NewPositionNotification += (sender, e) =>
        {
          if (e.x < 0 || e.x > 380.0 || e.y < 0 || e.y > 380.0)
            outOfBounds = true;
          lock (done)
          {
            eventCount++;
            if (eventCount >= target)
              done.Set();
          }
        };
      });

      done.Wait(TimeSpan.FromSeconds(5));
      Assert.IsFalse(outOfBounds, "Ball must stay within table bounds (0 to 380) at all times.");
    }
  }
}
