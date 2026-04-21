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
  public class BallUnitTest
  {
    [TestMethod]
    public void ConstructorTestMethod()
    {
      Vector testVector = new Vector(10.0, 10.0);
      Ball newInstance = new(testVector, new Vector(1.0, 1.0));
      newInstance.Stop();
    }

    [TestMethod]
    public void MoveTestMethod()
    {
      Vector initialPosition = new(100.0, 100.0);
      Ball newInstance = new(initialPosition, new Vector(50.0, 30.0));
      IVector? receivedPosition = null;
      ManualResetEventSlim notified = new ManualResetEventSlim(false);
      newInstance.NewPositionNotification += (sender, position) =>
      {
        Assert.IsNotNull(sender);
        receivedPosition = position;
        notified.Set();
      };
      bool signaled = notified.Wait(TimeSpan.FromSeconds(1));
      newInstance.Stop();
      Assert.IsTrue(signaled, "Ball did not raise NewPositionNotification within timeout.");
      Assert.IsNotNull(receivedPosition);
    }
  }
}
