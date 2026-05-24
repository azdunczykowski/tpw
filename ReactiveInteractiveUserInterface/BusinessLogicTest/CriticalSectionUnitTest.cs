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
  public class CriticalSectionUnitTest
  {
    [TestMethod]
    public void GetState_IsAtomicUnderConcurrentWritesTest()
    {
      var fixture = new TornReadBallFixture(px: 0, py: 0, vx: 1, vy: 1);
      bool tornRead = false;
      bool running = true;

      var writerThread = new Thread(() =>
      {
        int iteration = 0;
        while (running)
        {
          if (iteration % 2 == 0)
            fixture.SetState(new VecFix(0, 0),    new VecFix(1, 1));
          else
            fixture.SetState(new VecFix(100, 100), new VecFix(200, 200));
          iteration++;
          Thread.SpinWait(5);
        }
      }) { IsBackground = true };

      var readerThread = new Thread(() =>
      {
        while (running)
        {
          var (pos, vel) = fixture.GetState();
          bool isStateA = (pos.x == 0   && pos.y == 0   && vel.x == 1   && vel.y == 1);
          bool isStateB = (pos.x == 100 && pos.y == 100 && vel.x == 200 && vel.y == 200);
          if (!isStateA && !isStateB)
          {
            tornRead = true;
            break;
          }
          Thread.SpinWait(5);
        }
      }) { IsBackground = true };

      writerThread.Start();
      readerThread.Start();
      Thread.Sleep(300);
      running = false;
      writerThread.Join(500);
      readerThread.Join(500);

      Assert.IsFalse(tornRead, "GetState() zwróciło niespójny stan (torn read) – sekcja krytyczna nie działa.");
    }

    [TestMethod]
    public void HandleCollisions_UnderConcurrentAccessTest()
    {
      var balls = new[]
      {
        new BallFix(200, 200,  80,  0),
        new BallFix(220, 200, -80,  0),
        new BallFix(200, 220,   0, 80),
        new BallFix(220, 220,   0,-80),
        new BallFix(210, 210,  50, 50),
      };

      var api = new DataApiFixture(balls);
      using var logic = new BusinessLogicImplementation(api);
      logic.Start(balls.Length, (_, _) => { });

      bool exceptionThrown = false;
      int doneCount = 0;
      object doneLock = new object();

      var threads = balls.Select(b => new Thread(() =>
      {
        try
        {
          for (int i = 0; i < 20; i++)
          {
            b.SimulateMove();
            Thread.Sleep(1);
          }
        }
        catch
        {
          exceptionThrown = true;
        }
        finally
        {
          lock (doneLock) doneCount++;
        }
      }) { IsBackground = true }).ToArray();

      foreach (var t in threads) t.Start();
      foreach (var t in threads) t.Join(2000);

      Assert.AreEqual(balls.Length, doneCount, "Nie wszystkie wątki zakończyły pracę.");
      Assert.IsFalse(exceptionThrown, "Wyjątek podczas współbieżnego dostępu do HandleCollisions – race condition.");
    }

    #region testing instrumentation

    private class TornReadBallFixture : Data.IBall
    {
      private readonly object _lock = new();
      private Data.IVector _position;
      private Data.IVector _velocity;

      public TornReadBallFixture(double px, double py, double vx, double vy)
      {
        _position = new VecFix(px, py);
        _velocity = new VecFix(vx, vy);
      }

      public Data.IVector Position { get { lock (_lock) { return _position; } } }
      public Data.IVector Velocity { get { lock (_lock) { return _velocity; } } }
      public double Mass => 1.0;
      public event EventHandler<Data.IVector>? NewPositionNotification;

      internal void SetState(Data.IVector pos, Data.IVector vel)
      {
        lock (_lock) { _position = pos; _velocity = vel; }
      }

      public (Data.IVector position, Data.IVector velocity) GetState()
      {
        lock (_lock) { return (_position, _velocity); }
      }

      public void EnqueueCorrection(Data.IVector newPosition, Data.IVector newVelocity)
      {
        lock (_lock) { _position = newPosition; _velocity = newVelocity; }
      }
    }

    private class BallFix : Data.IBall
    {
      public double Px { get; private set; }
      public double Py { get; private set; }
      public double Vx { get; private set; }
      public double Vy { get; private set; }
      public double Mass => 1.0;

      private readonly object _lock = new();

      public BallFix(double px, double py, double vx, double vy)
      { Px = px; Py = py; Vx = vx; Vy = vy; }

      public Data.IVector Position { get { lock (_lock) { return new VecFix(Px, Py); } } }
      public Data.IVector Velocity { get { lock (_lock) { return new VecFix(Vx, Vy); } } }

      public event EventHandler<Data.IVector>? NewPositionNotification;

      public (Data.IVector position, Data.IVector velocity) GetState()
      {
        lock (_lock) { return (new VecFix(Px, Py), new VecFix(Vx, Vy)); }
      }

      public void EnqueueCorrection(Data.IVector newPosition, Data.IVector newVelocity)
      {
        lock (_lock) { Px = newPosition.x; Py = newPosition.y; Vx = newVelocity.x; Vy = newVelocity.y; }
      }

      internal void SimulateMove() =>
        NewPositionNotification?.Invoke(this, new VecFix(Px, Py));
    }

    private class DataApiFixture : Data.DataAbstractAPI
    {
      private readonly BallFix[] _balls;
      public DataApiFixture(BallFix[] balls) { _balls = balls; }
      public override void Start(int n, Action<Data.IVector, Data.IBall> handler)
      {
        foreach (var b in _balls)
          handler(new VecFix(b.Px, b.Py), b);
      }
      public override void Dispose() { }
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
