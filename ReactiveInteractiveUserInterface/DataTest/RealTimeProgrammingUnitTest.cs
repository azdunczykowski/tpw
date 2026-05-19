//____________________________________________________________________________________________________________________________________
//
//  Testy programowania czasu rzeczywistego – Etap 3.
//
//  Weryfikują że:
//  1. Ruch kuli obliczany jest na podstawie rzeczywistego czasu delta (Stopwatch).
//  2. Kula przebywa właściwą drogę dla danego deltaTime niezależnie od jego wartości.
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class RealTimeProgrammingUnitTest
  {
    [TestMethod]
    public void MovementProportionalToDeltaTimeTest()
    {
      const double velocity = 100.0;

      Ball ball = new(new Vector(0.0, 0.0), new Vector(velocity, 0.0));
      ball.Stop();
      Thread.Sleep(50);

      ball.Move(0.1);
      double posAfter100ms = ball.Position.x;

      ball.Move(0.1);
      double posAfter200ms = ball.Position.x;

      Assert.IsTrue(posAfter100ms > 0, "Kula powinna się poruszyć po Move(0.1).");
      Assert.AreEqual(2.0, posAfter200ms / posAfter100ms, 0.01,
        $"Przemieszczenie musi być proporcjonalne do deltaTime. pos@100ms={posAfter100ms:F4}, pos@200ms={posAfter200ms:F4}");
    }

    [TestMethod]
    public void BallThreadFiresNotificationsInRealTimeTest()
    {
      int notifications = 0;
      Ball ball = new(new Vector(200.0, 200.0), new Vector(50.0, 50.0));
      ball.NewPositionNotification += (_, _) =>
        Interlocked.Increment(ref notifications);

      Thread.Sleep(200);
      ball.Stop();

      Assert.IsTrue(notifications >= 5,
        $"Kula powinna wyzwolić >=5 notyfikacji w 200ms przy ~16ms/klatkę (było: {notifications}).");
    }

    [TestMethod]
    public void FinalPositionDependsOnTotalElapsedTimeTest()
    {
      Vector vel = new(60.0, 40.0);

      Ball ba = new(new Vector(50.0, 50.0), vel);
      ba.Stop();
      Thread.Sleep(50);
      for (int i = 0; i < 10; i++) ba.Move(0.01);
      var (posA, _) = ba.GetState();

      Ball bb = new(new Vector(50.0, 50.0), vel);
      bb.Stop();
      Thread.Sleep(50);
      bb.Move(0.10);
      var (posB, _) = bb.GetState();

      Assert.AreEqual(posA.x, posB.x, 1e-9, "10×0.01s i 1×0.1s muszą dać ten sam wynik.");
      Assert.AreEqual(posA.y, posB.y, 1e-9, "Oś Y: 10×0.01s = 1×0.1s.");
    }
  }
}
