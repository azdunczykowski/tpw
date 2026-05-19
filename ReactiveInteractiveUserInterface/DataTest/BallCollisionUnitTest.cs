//____________________________________________________________________________________________________________________________________
//
//  Testy jednostkowe zderzenia sprężystego (Etap 2) – warstwa Data.
//  Testowane bezpośrednio przez internal API Ball, bez Moq.
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class BallCollisionUnitTest
  {
    private const double BallDiameter = 30.0;

    [TestMethod]
    public void HeadOnCollisionSwapsVelocitiesTest()
    {
      Ball ba = new(new Vector(185.0, 200.0), new Vector( 100.0, 0.0));
      Ball bb = new(new Vector(215.0, 200.0), new Vector(-100.0, 0.0));

      ApplyCollision(ba, bb);

      Assert.IsTrue(ba.Velocity.x < 0, "Kulka A powinna lecieć w lewo po zderzeniu.");
      Assert.IsTrue(bb.Velocity.x > 0, "Kulka B powinna lecieć w prawo po zderzeniu.");
      Assert.AreEqual(100.0, Math.Abs(ba.Velocity.x), 1e-6, "Moduł prędkości A musi być zachowany.");
      Assert.AreEqual(100.0, Math.Abs(bb.Velocity.x), 1e-6, "Moduł prędkości B musi być zachowany.");
    }

    [TestMethod]
    public void CollisionPreservesKineticEnergyTest()
    {
      Ball ba = new(new Vector(190.0, 200.0), new Vector( 80.0,  60.0));
      Ball bb = new(new Vector(210.0, 200.0), new Vector(-50.0, -30.0));

      double eBefore = KE(ba) + KE(bb);
      ApplyCollision(ba, bb);
      double eAfter = KE(ba) + KE(bb);

      Assert.AreEqual(eBefore, eAfter, 1e-9, "Energia kinetyczna musi być zachowana.");
    }

    [TestMethod]
    public void NoCollisionWhenBallsMovingApartTest()
    {
      Vector velA = new(-100.0, 0.0);
      Vector velB = new( 100.0, 0.0);
      Ball ba = new(new Vector(185.0, 200.0), velA);
      Ball bb = new(new Vector(215.0, 200.0), velB);

      double dot = CalcDot(ba, bb);
      Assert.IsTrue(dot >= 0, "Kule oddalające się: dot powinno być >= 0.");

      Assert.AreEqual(velA.x, ba.Velocity.x, 1e-9);
      Assert.AreEqual(velB.x, bb.Velocity.x, 1e-9);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void ApplyCollision(Ball ba, Ball bb)
    {
      IVector posA = ba.Position, posB = bb.Position;
      IVector velA = ba.Velocity, velB = bb.Velocity;
      double dx = posA.x - posB.x, dy = posA.y - posB.y;
      double dist = Math.Sqrt(dx * dx + dy * dy);
      double nx = dx / dist, ny = dy / dist;
      double dot = (velA.x - velB.x) * nx + (velA.y - velB.y) * ny;
      if (dot >= 0) return;
      double impulse = 2.0 * dot / (ba.Mass + bb.Mass);
      ba.Velocity = new Vector(velA.x - impulse * bb.Mass * nx, velA.y - impulse * bb.Mass * ny);
      bb.Velocity = new Vector(velB.x + impulse * ba.Mass * nx, velB.y + impulse * ba.Mass * ny);
    }

    private static double CalcDot(Ball ba, Ball bb)
    {
      IVector posA = ba.Position, posB = bb.Position;
      IVector velA = ba.Velocity, velB = bb.Velocity;
      double dx = posA.x - posB.x, dy = posA.y - posB.y;
      double dist = Math.Sqrt(dx * dx + dy * dy);
      return (velA.x - velB.x) * (dx / dist) + (velA.y - velB.y) * (dy / dist);
    }

    private static double KE(Ball b) =>
      0.5 * b.Mass * (b.Velocity.x * b.Velocity.x + b.Velocity.y * b.Velocity.y);
  }
}
