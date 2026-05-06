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

namespace TP.ConcurrentProgramming.Data
{
  internal class Ball : IBall
  {
    #region ctor

    internal Ball(Vector initialPosition, Vector initialVelocity, double mass = 1.0, bool startThread = true)
    {
      _mass = mass;
      _posX = initialPosition.x;
      _posY = initialPosition.y;
      _velX = initialVelocity.x;
      _velY = initialVelocity.y;
      if (startThread)
      {
        _thread = new Thread(RunLoop) { IsBackground = true };
        _thread.Start();
      }
    }

    #endregion ctor

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Velocity
    {
      get => new Vector(_velX, _velY);
      set { _velX = value.x; _velY = value.y; }
    }

    public double Mass => _mass;

    public IVector Position => new Vector(_posX, _posY);

    #endregion IBall

    #region internal API

    internal void Stop() => _cancelled = true;

    #endregion internal API

    #region private

    private double _posX;
    private double _posY;
    private double _velX;
    private double _velY;
    private volatile bool _cancelled = false;
    private readonly Thread? _thread;
    private readonly double _mass;

    private const double TableWidth = 400.0;
    private const double TableHeight = 400.0;
    private const double BallDiameter = 20.0;
    private const double TickSeconds = 0.010;

    private void RunLoop()
    {
      Stopwatch sw = new Stopwatch();
      while (!_cancelled)
      {
        sw.Restart();
        Move();
        sw.Stop();
        TimeSpan remaining = TimeSpan.FromMilliseconds(10) - sw.Elapsed;
        if (remaining > TimeSpan.Zero)
          Thread.Sleep(remaining);
      }
    }

    private void Move()
    {
      _posX += _velX * TickSeconds;
      _posY += _velY * TickSeconds;

      const double maxX = TableWidth - BallDiameter;
      const double maxY = TableHeight - BallDiameter;

      if (_posX < 0.0)
      {
        _posX = -_posX;
        _velX = Math.Abs(_velX);
        if (_posX > maxX) _posX = maxX;
      }
      else if (_posX > maxX)
      {
        _posX = 2.0 * maxX - _posX;
        _velX = -Math.Abs(_velX);
        if (_posX < 0.0) _posX = 0.0;
      }

      if (_posY < 0.0)
      {
        _posY = -_posY;
        _velY = Math.Abs(_velY);
        if (_posY > maxY) _posY = maxY;
      }
      else if (_posY > maxY)
      {
        _posY = 2.0 * maxY - _posY;
        _velY = -Math.Abs(_velY);
        if (_posY < 0.0) _posY = 0.0;
      }

      NewPositionNotification?.Invoke(this, new Vector(_posX, _posY));
    }

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckVelocity(Action<double, double> returnVelocity)
    {
      returnVelocity(_velX, _velY);
    }

    [Conditional("DEBUG")]
    internal void SimulateMove()
    {
      Move();
    }

    #endregion TestingInfrastructure
  }
}
