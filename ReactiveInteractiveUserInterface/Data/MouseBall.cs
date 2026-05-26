//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;
using System.Threading.Channels;

namespace TP.ConcurrentProgramming.Data
{
  // A ball whose position is driven by external input (mouse) rather than physics.
  // Participates in collision detection; EnqueueCorrection is a no-op since the mouse
  // controls position. Velocity is computed from successive position updates.
  internal class MouseBall : IBall
  {
    internal MouseBall()
    {
      _position = new Vector(-TableDimensions.BallSize, -TableDimensions.BallSize);
      _velocity = new Vector(0, 0);
      _thread = new Thread(Run) { IsBackground = true, Name = "MouseBall" };
      _thread.Start();
    }

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Position { get { lock (_lock) { return _position; } } }

    public IVector Velocity { get { lock (_lock) { return _velocity; } } }

    public double Mass => 100.0;

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
      _positionChannel.Writer.TryWrite(newPosition);
    }

    internal void Stop() => _running = false;

    #endregion internal API

    #region private

    private Vector _position;
    private Vector _velocity;
    private volatile bool _running = true;

    private readonly Thread _thread;
    private readonly object _lock = new object();
    private readonly Channel<Vector> _positionChannel = Channel.CreateUnbounded<Vector>();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _lastUpdateMs = -1;

    private void Run()
    {
      while (_running)
      {
        bool updated = false;
        while (_positionChannel.Reader.TryRead(out Vector? newPos) && newPos != null)
        {
          long now = _stopwatch.ElapsedMilliseconds;
          lock (_lock)
          {
            double dt = _lastUpdateMs < 0 ? 0 : (now - _lastUpdateMs) / 1000.0;
            if (dt > 0 && dt < 0.5)
            {
              _velocity = new Vector(
                (newPos.x - _position.x) / dt,
                (newPos.y - _position.y) / dt);
            }
            else
            {
              _velocity = new Vector(0, 0);
            }
            _position = newPos;
          }
          _lastUpdateMs = now;
          updated = true;
        }

        if (updated)
          NewPositionNotification?.Invoke(this, _position);

        Thread.Sleep(8);
      }
    }

    #endregion private
  }
}
