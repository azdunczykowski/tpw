//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
  internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
  {
    #region ctor

    public BusinessLogicImplementation() : this(null)
    { }

    internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
    {
      layerBellow = underneathLayer == null ? UnderneathLayerAPI.GetDataLayer() : underneathLayer;
    }

    #endregion ctor

    #region BusinessLogicAbstractAPI

    public override void Dispose()
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      layerBellow.Dispose();
      Disposed = true;
    }

    public override void Start(int numberOfBalls, Action<IPosition, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      layerBellow.Start(numberOfBalls, (startingPosition, dataBall) =>
      {
        lock (_ballsLock)
          _dataBalls.Add(dataBall);
        dataBall.NewPositionNotification += HandleCollisions;
        Ball logicBall = new Ball(dataBall);
        upperLayerHandler(new Position(startingPosition.x, startingPosition.y), logicBall);
      });
    }

    #endregion BusinessLogicAbstractAPI

    #region private

    private bool Disposed = false;
    private readonly UnderneathLayerAPI layerBellow;
    private readonly List<Data.IBall> _dataBalls = [];
    private readonly object _ballsLock = new();
    private const double BallDiameter = Data.TableDimensions.BallSize;

    private void HandleCollisions(object? sender, Data.IVector _)
    {
      if (sender is not Data.IBall movedBall) return;
      List<Data.IBall> snapshot;
      lock (_ballsLock)
        snapshot = [.. _dataBalls];
      var (p1, v1) = movedBall.GetState();
      foreach (Data.IBall other in snapshot)
      {
        if (ReferenceEquals(other, movedBall)) continue;
        var (p2, v2) = other.GetState();
        ResolveCollision(movedBall, p1, v1, other, p2, v2);
        (p1, v1) = movedBall.GetState();
      }
    }

    private void ResolveCollision(Data.IBall a, Data.IVector aPos, Data.IVector aVel,
                                  Data.IBall b, Data.IVector bPos, Data.IVector bVel)
    {
      double dx = bPos.x - aPos.x;
      double dy = bPos.y - aPos.y;
      double dist2 = dx * dx + dy * dy;
      if (dist2 >= BallDiameter * BallDiameter || dist2 < 1e-10) return;

      double dvx = aVel.x - bVel.x;
      double dvy = aVel.y - bVel.y;
      double dot = dvx * dx + dvy * dy;
      if (dot <= 0.0) return;

      double dist = Math.Sqrt(dist2);
      double nx = dx / dist;
      double ny = dy / dist;
      double vRelN = dot / dist;
      double ma = a.Mass;
      double mb = b.Mass;
      double impulse = 2.0 * ma * mb / (ma + mb) * vRelN;

      a.Velocity = new DataVector(aVel.x - impulse / ma * nx, aVel.y - impulse / ma * ny);
      b.Velocity = new DataVector(bVel.x + impulse / mb * nx, bVel.y + impulse / mb * ny);

      double overlap = BallDiameter - dist;
      a.Position = new DataVector(aPos.x - overlap * 0.5 * nx, aPos.y - overlap * 0.5 * ny);
      b.Position = new DataVector(bPos.x + overlap * 0.5 * nx, bPos.y + overlap * 0.5 * ny);
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
