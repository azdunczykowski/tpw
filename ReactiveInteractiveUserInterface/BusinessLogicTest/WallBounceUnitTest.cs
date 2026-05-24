//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.BusinessLogic.Test
{
  [TestClass]
  public class WallBounceUnitTest
  {
    private const double TableWidth   = 400.0;
    private const double TableHeight  = 400.0;
    private const double BallDiameter = 30.0;
    private const double MaxX = TableWidth  - BallDiameter;
    private const double MaxY = TableHeight - BallDiameter;

    [TestMethod]
    public void LeftWallBounce_DataLayer_Test()
    {
      var ball = new BallWallTestHelper(px: -5.0, py: 200.0, vx: -100.0, vy: 0.0);
      ball.ApplyBounce(TableWidth, TableHeight, BallDiameter);

      Assert.IsTrue(ball.Vx > 0,
        $"vx powinno byc dodatnie po odbiciu od lewej sciany (Vx={ball.Vx}).");
      Assert.IsTrue(ball.Px >= 0,
        $"Kula weszla w lewa sciane (x={ball.Px}).");
    }

    [TestMethod]
    public void RightWallBounce_DataLayer_Test()
    {
      var ball = new BallWallTestHelper(px: MaxX + 5.0, py: 200.0, vx: 100.0, vy: 0.0);
      ball.ApplyBounce(TableWidth, TableHeight, BallDiameter);

      Assert.IsTrue(ball.Vx < 0,
        $"vx powinno byc ujemne po odbiciu od prawej sciany (Vx={ball.Vx}).");
      Assert.IsTrue(ball.Px <= MaxX,
        $"Kula weszla w prawa sciane (x={ball.Px}).");
    }

    [TestMethod]
    public void TopWallBounce_DataLayer_Test()
    {
      var ball = new BallWallTestHelper(px: 200.0, py: -5.0, vx: 0.0, vy: -100.0);
      ball.ApplyBounce(TableWidth, TableHeight, BallDiameter);

      Assert.IsTrue(ball.Vy > 0,
        $"vy powinno byc dodatnie po odbiciu od gornej sciany (Vy={ball.Vy}).");
      Assert.IsTrue(ball.Py >= 0,
        $"Kula weszla w gorna sciane (y={ball.Py}).");
    }

    [TestMethod]
    public void BottomWallBounce_DataLayer_Test()
    {
      var ball = new BallWallTestHelper(px: 200.0, py: MaxY + 5.0, vx: 0.0, vy: 100.0);
      ball.ApplyBounce(TableWidth, TableHeight, BallDiameter);

      Assert.IsTrue(ball.Vy < 0,
        $"vy powinno byc ujemne po odbiciu od dolnej sciany (Vy={ball.Vy}).");
      Assert.IsTrue(ball.Py <= MaxY,
        $"Kula weszla w dolna sciane (y={ball.Py}).");
    }

    [TestMethod]
    public void BallDoesNotStickToWallAtHighSpeedTest()
    {
      var ball = new BallWallTestHelper(px: -5.0, py: 200.0, vx: -5000.0, vy: 0.0);
      ball.ApplyBounce(TableWidth, TableHeight, BallDiameter);

      Assert.IsTrue(ball.Px >= 0,
        $"Kula przyklejona do lewej sciany (x={ball.Px}).");
      Assert.IsTrue(ball.Vx > 0,
        $"Predkosc powinna byc dodatnia po odbiciu (Vx={ball.Vx}).");
    }
  }

  internal class BallWallTestHelper
  {
    public double Px { get; private set; }
    public double Py { get; private set; }
    public double Vx { get; private set; }
    public double Vy { get; private set; }

    public BallWallTestHelper(double px, double py, double vx, double vy)
    { Px = px; Py = py; Vx = vx; Vy = vy; }

    public void ApplyBounce(double tableWidth, double tableHeight, double ballDiameter)
    {
      double maxX = tableWidth  - ballDiameter;
      double maxY = tableHeight - ballDiameter;
      double px = Px, py = Py, vx = Vx, vy = Vy;

      for (int i = 0; i < 8; i++)
      {
        bool bounced = false;
        if      (px < 0)    { px = -px;           vx =  Math.Abs(vx); bounced = true; }
        else if (px > maxX) { px = 2 * maxX - px; vx = -Math.Abs(vx); bounced = true; }
        if      (py < 0)    { py = -py;            vy =  Math.Abs(vy); bounced = true; }
        else if (py > maxY) { py = 2 * maxY - py;  vy = -Math.Abs(vy); bounced = true; }
        if (!bounced) break;
      }

      Px = Math.Clamp(px, 0, maxX);
      Py = Math.Clamp(py, 0, maxY);
      Vx = vx; Vy = vy;
    }
  }
}
