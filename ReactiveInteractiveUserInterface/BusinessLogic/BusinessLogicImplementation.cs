//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System.Collections.Concurrent;
using System.Diagnostics;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
  internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
  {
    private readonly record struct BallSnapshot(Data.IBall Ball, Data.IVector Position, Data.IVector Velocity, bool IsInit = false);

    #region ctor

    public BusinessLogicImplementation() : this(null, null)
    { }

    internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer, Data.ILogger? logger = null)
    {
      layerBellow = underneathLayer ?? UnderneathLayerAPI.GetDataLayer();
      if (underneathLayer == null)
      {
        _logger = logger ?? UnderneathLayerAPI.CreateDiagnosticLogger("bl_collisions.txt");
        _ownLogger = (logger == null);
      }
      else
      {
        _logger = logger;
        _ownLogger = false;
      }
      _physicsThread = new Thread(ProcessPhysics) { IsBackground = true, Name = "PhysicsEngine" };
      _physicsThread.Start();
    }

    #endregion ctor

    #region BusinessLogicAbstractAPI

    public override void Dispose()
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      Disposed = true;
      _physicsQueue.CompleteAdding();
      _physicsThread.Join(1000);
      if (_ownLogger) _logger?.Dispose();
      layerBellow.Dispose();
    }

    public override void Start(int numberOfBalls, Action<IPosition, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      layerBellow.Start(numberOfBalls, (startingPosition, dataBall) =>
      {
        int id = System.Threading.Interlocked.Increment(ref _nextBallId);
        _ballIds[dataBall] = id;
        _physicsQueue.TryAdd(new BallSnapshot(dataBall, startingPosition, dataBall.Velocity, IsInit: true));
        dataBall.NewPositionNotification += HandleCollisions;
        Ball logicBall = new Ball(dataBall);
        upperLayerHandler(new Position(startingPosition.x, startingPosition.y), logicBall);
      });

      Data.IBall? mouseBall = layerBellow.GetMouseBall();
      if (mouseBall != null)
      {
        int id = System.Threading.Interlocked.Increment(ref _nextBallId);
        _ballIds[mouseBall] = id;
        var (pos, vel) = mouseBall.GetState();
        _physicsQueue.TryAdd(new BallSnapshot(mouseBall, pos, vel, IsInit: true));
        mouseBall.NewPositionNotification += HandleCollisions;
        Ball mouseLogicBall = new Ball(mouseBall);
        upperLayerHandler(new Position(pos.x, pos.y), mouseLogicBall);
      }
    }

    public override void UpdateMousePosition(double x, double y)
    {
      if (Disposed) return;
      layerBellow.SetMouseBallPosition(x, y);
    }

    #endregion BusinessLogicAbstractAPI

    #region private

    private bool Disposed = false;
    private readonly UnderneathLayerAPI layerBellow;
    private const double BallDiameter = Data.TableDimensions.BallSize;

    private readonly BlockingCollection<BallSnapshot> _physicsQueue = new();
    private readonly Thread _physicsThread;

    private readonly Dictionary<Data.IBall, (Data.IVector pos, Data.IVector vel)> _worldState = new();
    private readonly Dictionary<Data.IBall, int> _ballIds = new();
    private int _nextBallId = 0;

    private readonly Data.ILogger? _logger;
    private readonly bool _ownLogger;

    private void HandleCollisions(object? sender, Data.IVector _)
    {
      if (sender is Data.IBall ball && !_physicsQueue.IsAddingCompleted)
      {
        var (pos, vel) = ball.GetState();
        _physicsQueue.TryAdd(new BallSnapshot(ball, pos, vel));
      }
    }

    private void ProcessPhysics()
    {
      foreach (BallSnapshot snap in _physicsQueue.GetConsumingEnumerable())
      {
        _worldState[snap.Ball] = (snap.Position, snap.Velocity);
        if (snap.IsInit) continue;

        Data.IVector curPos = snap.Position;
        Data.IVector curVel = snap.Velocity;

        foreach (Data.IBall other in _worldState.Keys.ToArray())
        {
          if (ReferenceEquals(other, snap.Ball)) continue;

          var (otherPos, otherVel) = _worldState[other];

          if (!TryComputeCollision(
                curPos, curVel, snap.Ball.Mass,
                otherPos, otherVel, other.Mass,
                out var newCurPos, out var newCurVel,
                out var newOtherPos, out var newOtherVel))
            continue;

          _worldState[snap.Ball] = (newCurPos, newCurVel);
          _worldState[other]     = (newOtherPos, newOtherVel);
          curPos = newCurPos;
          curVel = newCurVel;

          snap.Ball.EnqueueCorrection(newCurPos, newCurVel);
          other.EnqueueCorrection(newOtherPos, newOtherVel);

          if (_logger != null)
          {
            _ballIds.TryGetValue(snap.Ball, out int aId);
            _ballIds.TryGetValue(other, out int bId);
            _logger.Log(Data.LogLevel.Notice,
              $"Ball {aId} collided with Ball {bId} at ({curPos.x:F1},{curPos.y:F1}) newVel=({newCurVel.x:F1},{newCurVel.y:F1})");
            _logger.Log(Data.LogLevel.Notice,
              $"Ball {bId} collided with Ball {aId} at ({otherPos.x:F1},{otherPos.y:F1}) newVel=({newOtherVel.x:F1},{newOtherVel.y:F1})");
          }
        }
      }
    }

    private static bool TryComputeCollision(
      Data.IVector aPos, Data.IVector aVel, double ma,
      Data.IVector bPos, Data.IVector bVel, double mb,
      out Data.IVector newAPos, out Data.IVector newAVel,
      out Data.IVector newBPos, out Data.IVector newBVel)
    {
      newAPos = aPos; newAVel = aVel;
      newBPos = bPos; newBVel = bVel;

      double dx = bPos.x - aPos.x;
      double dy = bPos.y - aPos.y;
      double dist2 = dx * dx + dy * dy;
      if (dist2 >= BallDiameter * BallDiameter || dist2 < 1e-10) return false;

      double dvx = aVel.x - bVel.x;
      double dvy = aVel.y - bVel.y;
      double dot = dvx * dx + dvy * dy;
      if (dot <= 0.0) return false;

      double dist    = Math.Sqrt(dist2);
      double nx      = dx / dist;
      double ny      = dy / dist;
      double vRelN   = dot / dist;
      double impulse = 2.0 * ma * mb / (ma + mb) * vRelN;
      double overlap = BallDiameter - dist;

      double totalMass = ma + mb;
      double pushA = overlap * mb / totalMass;
      double pushB = overlap * ma / totalMass;

      newAPos = new DataVector(aPos.x - pushA * nx, aPos.y - pushA * ny);
      newAVel = new DataVector(aVel.x - impulse / ma * nx,  aVel.y - impulse / ma * ny);
      newBPos = new DataVector(bPos.x + pushB * nx, bPos.y + pushB * ny);
      newBVel = new DataVector(bVel.x + impulse / mb * nx,  bVel.y + impulse / mb * ny);
      return true;
    }

    private sealed class DataVector : Data.IVector
    {
      public double x { get; init; }
      public double y { get; init; }
      public DataVector(double x, double y) { this.x = x; this.y = y; }
    }

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}
