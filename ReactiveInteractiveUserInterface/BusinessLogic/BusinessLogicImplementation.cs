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

    public BusinessLogicImplementation() : this(null)
    { }

    internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
    {
      layerBellow = underneathLayer == null ? UnderneathLayerAPI.GetDataLayer() : underneathLayer;
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
        _physicsQueue.TryAdd(new BallSnapshot(dataBall, startingPosition, dataBall.Velocity, IsInit: true));
        dataBall.NewPositionNotification += HandleCollisions;
        Ball logicBall = new Ball(dataBall);
        upperLayerHandler(new Position(startingPosition.x, startingPosition.y), logicBall);
      });
    }

    #endregion BusinessLogicAbstractAPI

    #region private

    private bool Disposed = false;
    private readonly UnderneathLayerAPI layerBellow;
    private const double BallDiameter = Data.TableDimensions.BallSize;

    private readonly BlockingCollection<BallSnapshot> _physicsQueue = new();
    private readonly Thread _physicsThread;

    private readonly Dictionary<Data.IBall, (Data.IVector pos, Data.IVector vel)> _worldState = new();

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

      newAPos = new DataVector(aPos.x - overlap * 0.5 * nx, aPos.y - overlap * 0.5 * ny);
      newAVel = new DataVector(aVel.x - impulse / ma * nx,  aVel.y - impulse / ma * ny);
      newBPos = new DataVector(bPos.x + overlap * 0.5 * nx, bPos.y + overlap * 0.5 * ny);
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
