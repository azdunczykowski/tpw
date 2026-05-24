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
  public class BallCollisionUnitTest
  {
    private const double BallDiameter = 30.0;

    [TestMethod]
    public void TwoBalls_HeadOnCollision_TransfersVelocityTest()
    {
      var ba = new BallFix(190.0, 200.0,  100.0, 0.0);
      var bb = new BallFix(210.0, 200.0, -100.0, 0.0);

      var api   = new DataApiFixture(ba, bb);
      var logic = new BusinessLogicImplementation(api);
      logic.Start(2, (_, _) => { });

      ba.SimulateMove();
      Thread.Sleep(100);
      logic.Dispose();

      Assert.IsTrue(ba.Vx < 0, $"Kulka A powinna leciec w lewo po zderzeniu (Vx={ba.Vx}).");
      Assert.IsTrue(bb.Vx > 0, $"Kulka B powinna leciec w prawo po zderzeniu (Vx={bb.Vx}).");
    }

    [TestMethod]
    public void TwoBalls_KineticEnergyConservedTest()
    {
      var ba = new BallFix(190.0, 200.0,  80.0,  60.0);
      var bb = new BallFix(210.0, 200.0, -50.0, -30.0);

      double eBefore = KE(ba) + KE(bb);

      var api   = new DataApiFixture(ba, bb);
      var logic = new BusinessLogicImplementation(api);
      logic.Start(2, (_, _) => { });

      ba.SimulateMove();
      Thread.Sleep(100);
      logic.Dispose();

      double eAfter = KE(ba) + KE(bb);
      Assert.AreEqual(eBefore, eAfter, eBefore * 0.01,
        $"Energia kinetyczna musi byc zachowana (przed={eBefore:F3}, po={eAfter:F3}).");
    }

    [TestMethod]
    public void TwoBalls_MovingApart_NoCollisionTest()
    {
      var ba = new BallFix(190.0, 200.0, -100.0, 0.0);
      var bb = new BallFix(210.0, 200.0,  100.0, 0.0);

      double vxA = ba.Vx;
      double vxB = bb.Vx;

      var api   = new DataApiFixture(ba, bb);
      var logic = new BusinessLogicImplementation(api);
      logic.Start(2, (_, _) => { });

      ba.SimulateMove();
      Thread.Sleep(100);
      logic.Dispose();

      Assert.AreEqual(vxA, ba.Vx, Math.Abs(vxA) * 0.01 + 1.0,
        "Kulka A nie powinna zmienic predkosci gdy kule sie oddalaja.");
      Assert.AreEqual(vxB, bb.Vx, Math.Abs(vxB) * 0.01 + 1.0,
        "Kulka B nie powinna zmienic predkosci gdy kule sie oddalaja.");
    }

    [TestMethod]
    public void ThreeBalls_ChainCollisionTest()
    {
      var ba = new BallFix(172.0, 200.0,  100.0, 0.0);
      var bb = new BallFix(200.0, 200.0,    0.0, 0.0);
      var bc = new BallFix(228.0, 200.0, -100.0, 0.0);

      var api   = new DataApiFixture(ba, bb, bc);
      var logic = new BusinessLogicImplementation(api);
      logic.Start(3, (_, _) => { });

      ba.SimulateMove();
      bc.SimulateMove();
      Thread.Sleep(100);
      logic.Dispose();

      Assert.IsTrue(ba.Vx <= 0, $"Kulka A powinna leciec w lewo lub stac (Vx={ba.Vx}).");
      Assert.IsTrue(bc.Vx >= 0, $"Kulka C powinna leciec w prawo lub stac (Vx={bc.Vx}).");
    }

    [TestMethod]
    public void ThreeBalls_KineticEnergyConservedTest()
    {
      var ba = new BallFix(172.0, 200.0,  100.0,  50.0);
      var bb = new BallFix(200.0, 200.0,  -30.0,  20.0);
      var bc = new BallFix(228.0, 200.0,  -80.0, -40.0);

      double eBefore = KE(ba) + KE(bb) + KE(bc);

      var api   = new DataApiFixture(ba, bb, bc);
      var logic = new BusinessLogicImplementation(api);
      logic.Start(3, (_, _) => { });

      ba.SimulateMove();
      bc.SimulateMove();
      Thread.Sleep(100);
      logic.Dispose();

      double eAfter = KE(ba) + KE(bb) + KE(bc);
      Assert.AreEqual(eBefore, eAfter, eBefore * 0.01,
        $"Energia kinetyczna 3 kul musi byc zachowana (przed={eBefore:F3}, po={eAfter:F3}).");
    }

    #region helpers

    private static double KE(BallFix b) =>
      0.5 * b.Mass * (b.Vx * b.Vx + b.Vy * b.Vy);

    #endregion helpers

    #region testing instrumentation

    private class DataApiFixture : Data.DataAbstractAPI
    {
      private readonly BallFix[] _balls;
      public DataApiFixture(params BallFix[] balls) { _balls = balls; }

      public override void Start(int n, Action<Data.IVector, Data.IBall> handler)
      {
        foreach (var b in _balls)
          handler(new VecFix(b.Px, b.Py), b);
      }

      public override void Dispose() { }
    }

    private class BallFix : Data.IBall
    {
      public double Px { get; private set; }
      public double Py { get; private set; }
      public double Vx { get; private set; }
      public double Vy { get; private set; }
      public double Mass => 1.0;

      public BallFix(double px, double py, double vx, double vy)
      { Px = px; Py = py; Vx = vx; Vy = vy; }

      public Data.IVector Position => new VecFix(Px, Py);

      public Data.IVector Velocity => new VecFix(Vx, Vy);

      public event EventHandler<Data.IVector>? NewPositionNotification;

      public (Data.IVector position, Data.IVector velocity) GetState()
        => (Position, Velocity);

      public void EnqueueCorrection(Data.IVector newPosition, Data.IVector newVelocity)
      {
        Px = newPosition.x; Py = newPosition.y;
        Vx = newVelocity.x; Vy = newVelocity.y;
      }

      internal void SimulateMove() =>
        NewPositionNotification?.Invoke(this, new VecFix(Px, Py));
    }

    private class VecFix : Data.IVector
    {
      public double x { get; init; }
      public double y { get; init; }
      public VecFix(double x, double y) { this.x = x; this.y = y; }
    }

    #endregion testing instrumentation
  }
}
