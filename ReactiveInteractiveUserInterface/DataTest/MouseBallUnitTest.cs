//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class MouseBallUnitTest
  {
    [TestMethod]
    public void MouseBallImplementsIBallTest()
    {
      var ball = new MouseBall();
      ball.Stop();
      Assert.IsInstanceOfType(ball, typeof(IBall));
    }

    [TestMethod]
    public void MouseBallHasLargeMassTest()
    {
      var ball = new MouseBall();
      ball.Stop();
      Assert.IsTrue(ball.Mass > 1.0, "Mouse ball must be significantly heavier than regular balls.");
    }

    [TestMethod]
    public void PositionUpdateFiresNotificationTest()
    {
      var ball = new MouseBall();
      try
      {
        int notifications = 0;
        ball.NewPositionNotification += (_, _) => Interlocked.Increment(ref notifications);

        ball.UpdatePosition(new Vector(100, 150));
        Thread.Sleep(50); // allow Run() loop to process

        Assert.IsTrue(notifications >= 1, "NewPositionNotification must fire after UpdatePosition.");
      }
      finally
      {
        ball.Stop();
      }
    }

    [TestMethod]
    public void PositionIsUpdatedAfterUpdatePositionTest()
    {
      var ball = new MouseBall();
      try
      {
        ball.UpdatePosition(new Vector(123.0, 456.0));
        Thread.Sleep(50);

        var (pos, _) = ball.GetState();
        Assert.AreEqual(123.0, pos.x, 1e-9, "Ball X must reflect the last mouse position.");
        Assert.AreEqual(456.0, pos.y, 1e-9, "Ball Y must reflect the last mouse position.");
      }
      finally
      {
        ball.Stop();
      }
    }

    [TestMethod]
    public void EnqueueCorrectionDoesNotMoveMouseBallTest()
    {
      var ball = new MouseBall();
      try
      {
        ball.UpdatePosition(new Vector(200.0, 200.0));
        Thread.Sleep(50);

        // Physics correction should be ignored — position stays at mouse location.
        ball.EnqueueCorrection(new VecFixture(999.0, 999.0), new VecFixture(0, 0));
        Thread.Sleep(50);

        var (pos, _) = ball.GetState();
        Assert.AreEqual(200.0, pos.x, 1e-9, "EnqueueCorrection must not override mouse-controlled position.");
        Assert.AreEqual(200.0, pos.y, 1e-9, "EnqueueCorrection must not override mouse-controlled position.");
      }
      finally
      {
        ball.Stop();
      }
    }

    [TestMethod]
    public void GetStateIsThreadSafeTest()
    {
      var ball = new MouseBall();
      bool exceptionThrown = false;
      bool running = true;
      try
      {
        var writer = new Thread(() =>
        {
          int i = 0;
          while (running)
          {
            ball.UpdatePosition(new Vector(i % 400, (i * 7) % 400));
            i++;
            Thread.SpinWait(10);
          }
        }) { IsBackground = true };

        var reader = new Thread(() =>
        {
          try
          {
            while (running)
            {
              var (pos, vel) = ball.GetState();
              _ = pos.x + pos.y + vel.x + vel.y; // use values
              Thread.SpinWait(5);
            }
          }
          catch { exceptionThrown = true; }
        }) { IsBackground = true };

        writer.Start();
        reader.Start();
        Thread.Sleep(200);
        running = false;
        writer.Join(500);
        reader.Join(500);

        Assert.IsFalse(exceptionThrown, "GetState() threw an exception under concurrent writes.");
      }
      finally
      {
        ball.Stop();
      }
    }

    private record VecFixture(double x, double y) : IVector;
  }
}
