//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using TP.ConcurrentProgramming.Data;

namespace TP.ConcurrentProgramming.BusinessLogic.Test
{
  [TestClass]
  public class CollisionUnitTest
  {
    [TestMethod]
    public void TwoBallHeadOnCollisionTest()
    {
      TestBall ballA = new(100.0, 200.0, velX: 100.0, velY: 0.0);
      TestBall ballB = new(115.0, 200.0, velX: -100.0, velY: 0.0);
      using BusinessLogicImplementation logic = new(new TestDataLayer(ballA, ballB));
      logic.Start(2, (pos, ball) => { });

      ballA.RaiseMove();

      Assert.IsTrue(ballA.CurrentVelocity.x < 0.0, "Ball A must bounce back (move left) after head-on collision.");
      Assert.IsTrue(ballB.CurrentVelocity.x > 0.0, "Ball B must bounce forward (move right) after receiving impulse.");
    }

    [TestMethod]
    public void NoGhostCollisionAfterResolutionTest()
    {
      TestBall ballA = new(100.0, 200.0, velX: 100.0, velY: 0.0);
      TestBall ballB = new(115.0, 200.0, velX: -100.0, velY: 0.0);
      using BusinessLogicImplementation logic = new(new TestDataLayer(ballA, ballB));
      logic.Start(2, (pos, ball) => { });

      ballA.RaiseMove();
      double vAxAfter = ballA.CurrentVelocity.x;
      double vBxAfter = ballB.CurrentVelocity.x;

      ballB.RaiseMove();

      Assert.AreEqual(vAxAfter, ballA.CurrentVelocity.x, 1e-10,
          "Ball A velocity must not change after already-resolved collision (ghost-collision check).");
      Assert.AreEqual(vBxAfter, ballB.CurrentVelocity.x, 1e-10,
          "Ball B velocity must not change after already-resolved collision (ghost-collision check).");
    }

    [TestMethod]
    public void ThreeBallCollisionTest()
    {
      TestBall ballA = new(100.0, 200.0, velX: 100.0, velY: 0.0);
      TestBall ballB = new(115.0, 200.0, velX: -100.0, velY: 0.0);
      TestBall ballC = new(300.0, 200.0, velX: -50.0, velY: 0.0);
      using BusinessLogicImplementation logic = new(new TestDataLayer(ballA, ballB, ballC));
      logic.Start(3, (pos, ball) => { });

      ballA.RaiseMove();

      Assert.IsTrue(ballA.CurrentVelocity.x < 0.0, "Ball A bounces back after collision with B.");
      Assert.IsTrue(ballB.CurrentVelocity.x > 0.0, "Ball B moves forward after receiving impulse from A.");
      Assert.AreEqual(-50.0, ballC.CurrentVelocity.x, 1e-10, "Ball C must not be affected by A-B collision.");
    }

    [TestMethod]
    public void EnergyConservationAfterCollisionTest()
    {
      TestBall ballA = new(100.0, 200.0, velX: 80.0, velY: 30.0);
      TestBall ballB = new(115.0, 200.0, velX: -60.0, velY: -20.0);
      using BusinessLogicImplementation logic = new(new TestDataLayer(ballA, ballB));
      logic.Start(2, (pos, ball) => { });

      double ke0 = KineticEnergy(ballA) + KineticEnergy(ballB);
      ballA.RaiseMove();
      double ke1 = KineticEnergy(ballA) + KineticEnergy(ballB);

      Assert.AreEqual(ke0, ke1, 1e-6, "Total kinetic energy must be conserved after elastic collision.");
    }

    [TestMethod]
    public void TwoBallNotApproachingNoCollisionTest()
    {
      TestBall ballA = new(100.0, 200.0, velX: -100.0, velY: 0.0);
      TestBall ballB = new(115.0, 200.0, velX: 100.0, velY: 0.0);
      using BusinessLogicImplementation logic = new(new TestDataLayer(ballA, ballB));
      logic.Start(2, (pos, ball) => { });

      ballA.RaiseMove();

      Assert.AreEqual(-100.0, ballA.CurrentVelocity.x, 1e-10, "Ball A must not change velocity when balls are moving apart.");
      Assert.AreEqual(100.0, ballB.CurrentVelocity.x, 1e-10, "Ball B must not change velocity when balls are moving apart.");
    }

    #region helpers

    private static double KineticEnergy(TestBall ball)
    {
      IVector v = ball.CurrentVelocity;
      return 0.5 * ball.Mass * (v.x * v.x + v.y * v.y);
    }

    #endregion helpers

    #region testing instrumentation

    private class TestBall : Data.IBall
    {
      private double _velX, _velY;
      private readonly double _posX, _posY;

      public TestBall(double posX, double posY, double velX, double velY)
      {
        _posX = posX;
        _posY = posY;
        _velX = velX;
        _velY = velY;
      }

      public event EventHandler<IVector>? NewPositionNotification;

      public IVector Velocity
      {
        get => new VecFixture(_velX, _velY);
        set { _velX = value.x; _velY = value.y; }
      }

      public double Mass => 1.0;
      public IVector Position => new VecFixture(_posX, _posY);
      public IVector CurrentVelocity => Velocity;

      public void RaiseMove() =>
        NewPositionNotification?.Invoke(this, new VecFixture(_posX, _posY));
    }

    private record VecFixture(double x, double y) : IVector;

    private class TestDataLayer : Data.DataAbstractAPI
    {
      private readonly TestBall[] _balls;
      public TestDataLayer(params TestBall[] balls) => _balls = balls;

      public override void Start(int numberOfBalls, Action<IVector, Data.IBall> handler)
      {
        foreach (TestBall ball in _balls)
          handler(ball.Position, ball);
      }

      public override void Dispose() { }
    }

    #endregion testing instrumentation
  }
}
