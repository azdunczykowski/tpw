//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class BallPhysicsUnitTest
  {
    private static DiagnosticLogger CreateTempLogger() =>
      new DiagnosticLogger(Path.Combine(Path.GetTempPath(), $"phys_{Guid.NewGuid():N}.txt"));

    [TestMethod]
    public void SpeedMagnitudeConstantDuringMoveTest()
    {
      Ball ball = new(new Vector(200.0, 200.0), new Vector(-200.0, -150.0));
      ball.Stop();

      double initialSpeedSquared = Math.Pow(ball.Velocity.x, 2) + Math.Pow(ball.Velocity.y, 2);

      for (int step = 0; step < 100; step++)
      {
        ball.Move(0.01);
        double currentSpeedSquared = Math.Pow(ball.Velocity.x, 2) + Math.Pow(ball.Velocity.y, 2);
        Assert.AreEqual(initialSpeedSquared, currentSpeedSquared, 1e-9,
          "Moduł prędkości kuli zmienił się podczas ruchu (Data nie modyfikuje prędkości).");
      }
    }

    [TestMethod]
    public void EachBallHasItsOwnProcessTest()
    {
      using DiagnosticLogger logger = CreateTempLogger();
      using DataImplementation dataLayer = new DataImplementation(logger);
      int tasksTriggered = 0;

      dataLayer.Start(3, (startPos, ball) =>
      {
        ball.NewPositionNotification += (sender, newPos) =>
          Interlocked.Increment(ref tasksTriggered);
      });

      Thread.Sleep(200);

      Assert.IsTrue(tasksTriggered > 0,
        "Procesy w tle nie wystartowały – kulki nie poruszają się same.");
    }
  }
}
