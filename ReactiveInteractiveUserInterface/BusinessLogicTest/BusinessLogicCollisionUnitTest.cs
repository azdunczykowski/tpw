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
  public class BusinessLogicCollisionUnitTest
  {
    [TestMethod]
    public void PositionEventPropagatesTest()
    {
      DataFixture fixture = new();
      using BusinessLogicImplementation logic = new(fixture);

      int updates = 0;
      logic.Start(1, (_, ball) =>
        ball.NewPositionNotification += (_, _) => Interlocked.Increment(ref updates));

      fixture.SimulateMove();

      Assert.IsTrue(updates >= 1,
        "Zdarzenie pozycji z Data musi dotrzeć przez BusinessLogic do obserwatora.");
    }

    [TestMethod]
    public void StartPositionPassedCorrectlyTest()
    {
      DataFixture fixture = new();
      using BusinessLogicImplementation logic = new(fixture);

      IPosition? startPos = null;
      logic.Start(1, (pos, _) => startPos = pos);

      Assert.IsNotNull(startPos);
      Assert.AreEqual(DataFixture.StartX, startPos.x, 1e-9);
      Assert.AreEqual(DataFixture.StartY, startPos.y, 1e-9);
    }

    [TestMethod]
    public void BallMassPassedCorrectlyTest()
    {
      DataFixture fixture = new();
      using BusinessLogicImplementation logic = new(fixture);

      IBall? logicBall = null;
      logic.Start(1, (_, ball) => logicBall = ball);

      Assert.IsNotNull(logicBall);
      Assert.AreEqual(DataFixture.BallMass, logicBall.Mass, 1e-9);
    }

    #region fixture

    private class DataFixture : Data.DataAbstractAPI
    {
      internal const double StartX   = 100.0;
      internal const double StartY   = 150.0;
      internal const double BallMass = 1.0;

      private readonly BallFixture _ball = new();

      public override void Start(int numberOfBalls, Action<Data.IVector, Data.IBall> upperLayerHandler) =>
        upperLayerHandler(new VecFixture(StartX, StartY), _ball);

      public override void Dispose() { }

      internal void SimulateMove() =>
        _ball.Raise(new VecFixture(StartX + 1, StartY + 1));
    }

    private class BallFixture : Data.IBall
    {
      public Data.IVector Position { get; private set; } = new VecFixture(DataFixture.StartX, DataFixture.StartY);
      public Data.IVector Velocity { get; private set; } = new VecFixture(0, 0);
      public double Mass => DataFixture.BallMass;
      public event EventHandler<Data.IVector>? NewPositionNotification;

      public (Data.IVector position, Data.IVector velocity) GetState()
        => (Position, Velocity);

      public void EnqueueCorrection(Data.IVector newPosition, Data.IVector newVelocity)
      {
        Position = newPosition;
        Velocity = newVelocity;
      }

      internal void Raise(Data.IVector v) => NewPositionNotification?.Invoke(this, v);
    }

    private class VecFixture : Data.IVector
    {
      public double x { get; init; }
      public double y { get; init; }
      public VecFixture(double x, double y) { this.x = x; this.y = y; }
    }

    #endregion fixture
  }
}
