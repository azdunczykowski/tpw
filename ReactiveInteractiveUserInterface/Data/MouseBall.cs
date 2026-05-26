//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
  // A ball whose position is driven by external input (mouse) rather than physics.
  // Participates in collision detection; EnqueueCorrection is a no-op since the mouse
  // controls position. Velocity is computed from successive position updates.
  // Notification fires synchronously on the calling thread to avoid lag.
  internal class MouseBall : IBall
  {
    internal MouseBall()
    {
      _position = new Vector(-TableDimensions.BallSize, -TableDimensions.BallSize);
      _velocity = new Vector(0, 0);
    }

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Position { get { lock (_lock) { return _position; } } }

    public IVector Velocity { get { lock (_lock) { return _velocity; } } }

    public double Mass => 1.5;

    public bool IsKinematic => true;

    public (IVector position, IVector velocity) GetState()
    {
      lock (_lock) { return (_position, _velocity); }
    }

    public void EnqueueCorrection(IVector newPosition, IVector newVelocity)
    {
      // Position is mouse-controlled — ignore physics corrections.
    }

    #endregion IBall

    #region internal API

    internal void UpdatePosition(Vector newPosition)
    {
      double now = _stopwatch.Elapsed.TotalSeconds;
      Vector pos;
      lock (_lock)
      {
        double dt = (_lastUpdateSec < 0.0) ? 0.0 : (now - _lastUpdateSec);
        if (dt >= 0.005 && dt < 0.15)
        {
          double vx = (newPosition.x - _position.x) / dt;
          double vy = (newPosition.y - _position.y) / dt;
          double speed = Math.Sqrt(vx * vx + vy * vy);
          if (speed > MaxCursorSpeed) { vx = vx / speed * MaxCursorSpeed; vy = vy / speed * MaxCursorSpeed; }
          _velocity = new Vector(_velocity.x * 0.4 + vx * 0.6, _velocity.y * 0.4 + vy * 0.6);
        }
        else if (dt >= 0.15)
        {
          _velocity = new Vector(0.0, 0.0);
        }
        _position = newPosition;
        pos = _position;
        _lastUpdateSec = now;
      }
      NewPositionNotification?.Invoke(this, pos);
    }

    internal void Stop() { }

    #endregion internal API

    #region private

    private Vector _position;
    private Vector _velocity;
    private readonly object _lock = new object();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastUpdateSec = -1.0;

    private const double MaxCursorSpeed = 320.0; // px/s cap to prevent explosive collisions

    #endregion private
  }
}
