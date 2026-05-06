//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.BusinessLogic
{
  internal class Ball : IBall
  {
    public Ball(Data.IBall ball)
    {
      _dataBall = ball;
      ball.NewPositionNotification += RaisePositionChangeEvent;
    }

    #region IBall

    public event EventHandler<IPosition>? NewPositionNotification;

    public double KineticEnergy
    {
      get
      {
        Data.IVector v = _dataBall.Velocity;
        return 0.5 * (v.x * v.x + v.y * v.y);
      }
    }

    public double MomentumMagnitude
    {
      get
      {
        Data.IVector v = _dataBall.Velocity;
        return Math.Sqrt(v.x * v.x + v.y * v.y);
      }
    }

    public double Mass => _dataBall.Mass;

    #endregion IBall

    #region private

    private readonly Data.IBall _dataBall;

    private void RaisePositionChangeEvent(object? sender, Data.IVector e)
    {
      NewPositionNotification?.Invoke(this, new Position(e.x, e.y));
    }

    #endregion private
  }
}
