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
  public class WallBounceUnitTest
  {
    // Lokalna kopia logiki odbicia (ta sama co DataImplementation.BounceOffWalls)
    private static Action<Ball> MakeBounceCallback()
    {
      return ball =>
      {
        double maxX = TableDimensions.Width - TableDimensions.BallSize;
        double maxY = TableDimensions.Height - TableDimensions.BallSize;
        double px = ball.Position.x, py = ball.Position.y;
        double vx = ball.Velocity.x, vy = ball.Velocity.y;

        for (int i = 0; i < 8; i++)
        {
          bool bounced = false;
          if      (px < 0)    { px = -px;            vx =  Math.Abs(vx); bounced = true; }
          else if (px > maxX) { px = 2 * maxX - px;  vx = -Math.Abs(vx); bounced = true; }
          if      (py < 0)    { py = -py;             vy =  Math.Abs(vy); bounced = true; }
          else if (py > maxY) { py = 2 * maxY - py;  vy = -Math.Abs(vy); bounced = true; }
          if (!bounced) break;
        }

        px = Math.Clamp(px, 0.0, maxX);
        py = Math.Clamp(py, 0.0, maxY);

        ball.Velocity = new Vector(vx, vy);
        ball.Position = new Vector(px, py);
      };
    }

    [TestMethod]
    public void RightWallBounceTest()
    {
      Ball ball = new(new Vector(381.0, 100.0), new Vector(50.0, 0.0),
                      preNotificationCallback: MakeBounceCallback());
      ball.Stop();
      Thread.Sleep(20);

      double vxBefore = 0, vyBefore = 0;
      ball.CheckVelocity((vx, vy) => { vxBefore = vx; vyBefore = vy; });
      Assert.IsTrue(vxBefore > 0, "Initial vx should be positive.");

      ball.SimulateMove();

      double vxAfter = 0, vyAfter = 0;
      ball.CheckVelocity((vx, vy) => { vxAfter = vx; vyAfter = vy; });
      Assert.IsTrue(vxAfter < 0, "After right wall bounce vx must be negative.");
      Assert.AreEqual(vyBefore, vyAfter, 1e-10, "vy must not change on right wall bounce.");
    }

    [TestMethod]
    public void LeftWallBounceTest()
    {
      Ball ball = new(new Vector(0.1, 100.0), new Vector(-50.0, 0.0),
                      preNotificationCallback: MakeBounceCallback());
      ball.Stop();
      Thread.Sleep(20);

      double vxBefore = 0;
      ball.CheckVelocity((vx, vy) => vxBefore = vx);
      Assert.IsTrue(vxBefore < 0, "Initial vx should be negative.");

      ball.SimulateMove();

      double vxAfter = 0;
      ball.CheckVelocity((vx, vy) => vxAfter = vx);
      Assert.IsTrue(vxAfter > 0, "After left wall bounce vx must be positive.");
    }

    [TestMethod]
    public void BottomWallBounceTest()
    {
      Ball ball = new(new Vector(100.0, 381.0), new Vector(0.0, 50.0),
                      preNotificationCallback: MakeBounceCallback());
      ball.Stop();
      Thread.Sleep(20);

      double vyBefore = 0;
      ball.CheckVelocity((vx, vy) => vyBefore = vy);
      Assert.IsTrue(vyBefore > 0, "Initial vy should be positive.");

      ball.SimulateMove();

      double vxAfter = 0, vyAfter = 0;
      ball.CheckVelocity((vx, vy) => { vxAfter = vx; vyAfter = vy; });
      Assert.IsTrue(vyAfter < 0, "After bottom wall bounce vy must be negative.");
      Assert.AreEqual(0.0, vxAfter, 1e-10, "vx must not change on bottom wall bounce.");
    }

    [TestMethod]
    public void TopWallBounceTest()
    {
      Ball ball = new(new Vector(100.0, 0.1), new Vector(0.0, -50.0),
                      preNotificationCallback: MakeBounceCallback());
      ball.Stop();
      Thread.Sleep(20);

      double vyBefore = 0;
      ball.CheckVelocity((vx, vy) => vyBefore = vy);
      Assert.IsTrue(vyBefore < 0, "Initial vy should be negative.");

      ball.SimulateMove();

      double vxAfter = 0, vyAfter = 0;
      ball.CheckVelocity((vx, vy) => { vxAfter = vx; vyAfter = vy; });
      Assert.IsTrue(vyAfter > 0, "After top wall bounce vy must be positive.");
    }

    [TestMethod]
    public void HighVelocityNoBoundaryStickingTest()
    {
      Ball ball = new(new Vector(190.0, 190.0), new Vector(4000.0, 3000.0),
                      preNotificationCallback: MakeBounceCallback());
      ball.Stop();
      Thread.Sleep(20);

      bool outOfBounds = false;
      ball.NewPositionNotification += (sender, pos) =>
      {
        if (pos.x < 0 || pos.x > 380.0 || pos.y < 0 || pos.y > 380.0)
          outOfBounds = true;
      };

      for (int i = 0; i < 500; i++)
        ball.SimulateMove();

      Assert.IsFalse(outOfBounds, "Ball must stay within [0, 380] even at high velocity.");

      double vx = 0, vy = 0;
      ball.CheckVelocity((x, y) => { vx = x; vy = y; });
      double speed = Math.Sqrt(vx * vx + vy * vy);
      double expectedSpeed = Math.Sqrt(4000.0 * 4000.0 + 3000.0 * 3000.0);
      Assert.AreEqual(expectedSpeed, speed, 1e-6, "Speed must be preserved after high-velocity bounces.");
    }

    [TestMethod]
    public void SpeedPreservedAfterBounceTest()
    {
      Ball ball = new(new Vector(381.0, 381.0), new Vector(70.3, 80.7),
                      preNotificationCallback: MakeBounceCallback());
      ball.Stop();
      Thread.Sleep(20);

      double vx0 = 0, vy0 = 0;
      ball.CheckVelocity((vx, vy) => { vx0 = vx; vy0 = vy; });
      double speedBefore = Math.Sqrt(vx0 * vx0 + vy0 * vy0);

      for (int i = 0; i < 500; i++)
        ball.SimulateMove();

      double vxN = 0, vyN = 0;
      ball.CheckVelocity((vx, vy) => { vxN = vx; vyN = vy; });
      double speedAfter = Math.Sqrt(vxN * vxN + vyN * vyN);

      Assert.AreEqual(speedBefore, speedAfter, 1e-9, "Speed (|v|) must be preserved after bounces.");
    }
  }
}
