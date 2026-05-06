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
        lock (_lock)
          _dataBalls.Add(dataBall);
        dataBall.NewPositionNotification += OnDataBallPositionChanged;
        Ball logicBall = new Ball(dataBall);
        upperLayerHandler(new Position(startingPosition.x, startingPosition.y), logicBall);
      });
    }

    #endregion BusinessLogicAbstractAPI

    #region private

    private bool Disposed = false;
    private readonly UnderneathLayerAPI layerBellow;
    private readonly List<Data.IBall> _dataBalls = [];
    private readonly object _lock = new();
    private const double BallDiameter = 20.0;

    private void OnDataBallPositionChanged(object? sender, Data.IVector newPos)
    {
      if (sender is not Data.IBall movedBall) return;
      List<Data.IBall> snapshot;
      lock (_lock)
        snapshot = [.. _dataBalls];
      foreach (Data.IBall other in snapshot)
      {
        if (ReferenceEquals(other, movedBall)) continue;
        ResolveCollision(movedBall, newPos, other);
      }
    }

    private void ResolveCollision(Data.IBall a, Data.IVector aPos, Data.IBall b)
    {
      lock (_lock)
      {
        Data.IVector bPos = b.Position;
        double dx = bPos.x - aPos.x;
        double dy = bPos.y - aPos.y;
        double dist2 = dx * dx + dy * dy;
        if (dist2 >= BallDiameter * BallDiameter || dist2 < 1e-10) return;

        Data.IVector va = a.Velocity;
        Data.IVector vb = b.Velocity;
        double dvx = va.x - vb.x;
        double dvy = va.y - vb.y;
        double dot = dvx * dx + dvy * dy;
        if (dot <= 0.0) return;

        double dist = Math.Sqrt(dist2);
        double nx = dx / dist;
        double ny = dy / dist;
        double vRelN = dot / dist;
        double ma = a.Mass;
        double mb = b.Mass;
        double impulse = 2.0 * ma * mb / (ma + mb) * vRelN;

        a.Velocity = new DataVector(va.x - impulse / ma * nx, va.y - impulse / ma * ny);
        b.Velocity = new DataVector(vb.x + impulse / mb * nx, vb.y + impulse / mb * ny);
      }
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
