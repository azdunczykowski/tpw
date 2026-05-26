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
using System.Threading.Channels;

namespace TP.ConcurrentProgramming.Data
{
  internal class Ball : IBall
  {
    internal Ball(Vector initialPosition, Vector initialVelocity,
                  int ballId = 0,
                  ILogger? logger = null)
    {
      _position = initialPosition;
      _velocity = initialVelocity;
      _ballId = ballId;
      _logger = logger;
      _stopwatch = Stopwatch.StartNew();

      _thread = new Thread(Run) { IsBackground = true, Name = $"Ball-{ballId}" };
      _thread.Start();
    }

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Position
    {
      get { lock (_lock) { return _position; } }
    }

    public IVector Velocity
    {
      get { lock (_lock) { return _velocity; } }
    }

    public double Mass { get; } = 1.0;

    public (IVector position, IVector velocity) GetState()
    {
      lock (_lock) { return (_position, _velocity); }
    }

    public void EnqueueCorrection(IVector newPosition, IVector newVelocity)
    {
      _corrections.Writer.TryWrite(
        (new Vector(newPosition.x, newPosition.y), new Vector(newVelocity.x, newVelocity.y)));
    }

    #endregion IBall

    #region internal API

    internal void Stop() => _running = false;

    internal void Move(double deltaTime)
    {
      IVector posSnapshot;
      lock (_lock)
      {
        _position = new Vector(
          _position.x + _velocity.x * deltaTime,
          _position.y + _velocity.y * deltaTime);
        ApplyWallBounce();
        posSnapshot = _position;
      }

      _logger?.Log(LogLevel.Info,
        $"Ball {_ballId} pos=({posSnapshot.x:F4},{posSnapshot.y:F4}) vel=({_velocity.x:F4},{_velocity.y:F4}) t={_stopwatch.ElapsedMilliseconds}ms");

      NewPositionNotification?.Invoke(this, posSnapshot);
    }

    internal void Move(double deltaTime, double tableWidth, double tableHeight, double ballDiameter)
      => Move(deltaTime);

    #endregion internal API

    #region private

    private Vector _position;
    private Vector _velocity;
    private volatile bool _running = true;

    private readonly int _ballId;
    private readonly ILogger? _logger;
    private readonly Thread _thread;
    private readonly object _lock = new object();
    private readonly Stopwatch _stopwatch;
    private readonly Channel<(Vector position, Vector velocity)> _corrections =
      Channel.CreateUnbounded<(Vector, Vector)>();

    internal const int TargetPeriodMs = 16; // ~60 fps

    private void ApplyWallBounce()
    {
      double maxX = TableDimensions.Width - TableDimensions.BallSize;
      double maxY = TableDimensions.Height - TableDimensions.BallSize;
      double px = _position.x, py = _position.y;
      double vx = _velocity.x, vy = _velocity.y;

      for (int i = 0; i < 8; i++)
      {
        bool bounced = false;
        if      (px < 0)    { px = -px;            vx =  Math.Abs(vx); bounced = true; }
        else if (px > maxX) { px = 2 * maxX - px;  vx = -Math.Abs(vx); bounced = true; }
        if      (py < 0)    { py = -py;             vy =  Math.Abs(vy); bounced = true; }
        else if (py > maxY) { py = 2 * maxY - py;  vy = -Math.Abs(vy); bounced = true; }
        if (!bounced) break;
      }

      px = Math.Clamp(px, 0.0, maxX);
      py = Math.Clamp(py, 0.0, maxY);

      _position = new Vector(px, py);
      _velocity = new Vector(vx, vy);
    }

    private void Run()
    {
      var sw = Stopwatch.StartNew();
      while (_running)
      {
        while (_corrections.Reader.TryRead(out var correction))
        {
          lock (_lock)
          {
            _position = correction.position;
            _velocity = correction.velocity;
          }
        }

        double realDelta = sw.Elapsed.TotalSeconds;
        sw.Restart();
        if (realDelta > 0.2) realDelta = TargetPeriodMs / 1000.0;

        Move(realDelta);

        long elapsed = sw.ElapsedMilliseconds;
        int toWait = TargetPeriodMs - (int)elapsed;
        if (toWait > 0) Thread.Sleep(toWait);
      }
    }

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckVelocity(Action<double, double> returnVelocity)
    {
      lock (_lock)
        returnVelocity(_velocity.x, _velocity.y);
    }

    [Conditional("DEBUG")]
    internal void SimulateMove()
    {
      while (_corrections.Reader.TryRead(out var correction))
      {
        lock (_lock)
        {
          _position = correction.position;
          _velocity = correction.velocity;
        }
      }
      Move(TargetPeriodMs / 1000.0);
    }

    #endregion TestingInfrastructure
  }
}
