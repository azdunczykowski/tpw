//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//_____________________________________________________________________________________________________________________________________

using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using UnderneathLayerAPI = TP.ConcurrentProgramming.BusinessLogic.BusinessLogicAbstractAPI;

namespace TP.ConcurrentProgramming.Presentation.Model
{
  internal class ModelImplementation : ModelAbstractApi
  {
    internal ModelImplementation() : this(null)
    { }

    internal ModelImplementation(UnderneathLayerAPI? underneathLayer)
    {
      layerBellow = underneathLayer == null ? UnderneathLayerAPI.GetBusinessLogicLayer() : underneathLayer;
      eventObservable = Observable.FromEventPattern<BallChaneEventArgs>(this, "BallChanged");
    }

    #region ModelAbstractApi

    public override void Dispose()
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(ModelImplementation));
      layerBellow.Dispose();
      Disposed = true;
    }

    public override double TotalEnergy => _totalEnergy;
    public override double TotalMomentum => _totalMomentum;

    public override double CanvasWidth { get; set; } = 400.0;
    public override double CanvasHeight { get; set; } = 400.0;

    public override event EventHandler? PhysicsChanged;

    public override IDisposable Subscribe(IObserver<IBall> observer)
    {
      return eventObservable.Subscribe(x => observer.OnNext(x.EventArgs.Ball), ex => observer.OnError(ex), () => observer.OnCompleted());
    }

    public override void Start(int numberOfBalls)
    {
      layerBellow.Start(numberOfBalls, StartHandler);
      PhysicsChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion ModelAbstractApi

    #region API

    public event EventHandler<BallChaneEventArgs>? BallChanged;

    #endregion API

    #region private

    private bool Disposed = false;
    private readonly IObservable<EventPattern<BallChaneEventArgs>> eventObservable;
    private readonly UnderneathLayerAPI layerBellow;
    private double _totalEnergy = 0;
    private double _totalMomentum = 0;

    private void StartHandler(BusinessLogic.IPosition position, BusinessLogic.IBall ball)
    {
      double vx = ball.Velocity.x;
      double vy = ball.Velocity.y;
      _totalEnergy += 0.5 * ball.Mass * (vx * vx + vy * vy);
      _totalMomentum += ball.Mass * Math.Sqrt(vx * vx + vy * vy);
      double diameter = UnderneathLayerAPI.GetDimensions.BallDimension;
      ModelBall newBall = new ModelBall(position.y, position.x, ball) { Diameter = diameter };
      BallChanged?.Invoke(this, new BallChaneEventArgs() { Ball = newBall });
    }

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    [Conditional("DEBUG")]
    internal void CheckUnderneathLayerAPI(Action<UnderneathLayerAPI> returnNumberOfBalls)
    {
      returnNumberOfBalls(layerBellow);
    }

    [Conditional("DEBUG")]
    internal void CheckBallChangedEvent(Action<bool> returnBallChangedIsNull)
    {
      returnBallChangedIsNull(BallChanged == null);
    }

    #endregion TestingInfrastructure
  }

  public class BallChaneEventArgs : EventArgs
  {
    public IBall Ball { get; init; } = null!;
  }
}
