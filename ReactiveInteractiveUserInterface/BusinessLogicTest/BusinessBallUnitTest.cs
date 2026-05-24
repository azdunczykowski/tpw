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
  public class BallUnitTest
  {
    [TestMethod]
    public void MoveTestMethod()
    {
      DataBallFixture dataBallFixture = new DataBallFixture();
      Ball newInstance = new(dataBallFixture);
      int numberOfCallBackCalled = 0;
      newInstance.NewPositionNotification += (sender, position) => { Assert.IsNotNull(sender); Assert.IsNotNull(position); numberOfCallBackCalled++; };
      dataBallFixture.Move();
      Assert.AreEqual<int>(1, numberOfCallBackCalled);
    }

    #region testing instrumentation

    private class DataBallFixture : Data.IBall
    {
      private double _velX, _velY;
      private double _posX, _posY;

      public Data.IVector Velocity => new VectorFixture(_velX, _velY);

      public double Mass => 1.0;

      public Data.IVector Position => new VectorFixture(_posX, _posY);

      public (Data.IVector position, Data.IVector velocity) GetState()
        => (new VectorFixture(_posX, _posY), new VectorFixture(_velX, _velY));

      public void EnqueueCorrection(Data.IVector newPosition, Data.IVector newVelocity)
      {
        _posX = newPosition.x; _posY = newPosition.y;
        _velX = newVelocity.x; _velY = newVelocity.y;
      }

      public event EventHandler<Data.IVector>? NewPositionNotification;

      internal void Move()
      {
        NewPositionNotification?.Invoke(this, new VectorFixture(0.0, 0.0));
      }
    }

    private class VectorFixture : Data.IVector
    {
      internal VectorFixture(double X, double Y)
      {
        x = X; y = Y;
      }

      public double x { get; init; }
      public double y { get; init; }
    }

    #endregion testing instrumentation
  }
}
