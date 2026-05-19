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
  // Programowanie czasu rzeczywistego: ruch oparty na rzeczywistym czasie delta ze Stopwatch,
  // nie na stałej nominalnej. Logowanie diagnostyczne przez nieblokującą kolejkę.
  internal class Ball : IBall
  {
    internal Ball(Vector initialPosition, Vector initialVelocity,
                  int ballId = 0,
                  DiagnosticLogger? logger = null,
                  Action<Ball>? preNotificationCallback = null)
    {
      _position = initialPosition;
      _velocity = initialVelocity;
      _ballId = ballId;
      _logger = logger;
      _preNotificationCallback = preNotificationCallback;
      _stopwatch = Stopwatch.StartNew();

      _thread = new Thread(Run) { IsBackground = true, Name = $"Ball-{ballId}" };
      _thread.Start();
    }

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Position
    {
      get { lock (_lock) { return _position; } }
      set { lock (_lock) { _position = new Vector(value.x, value.y); } }
    }

    public IVector Velocity
    {
      get { lock (_lock) { return _velocity; } }
      set { lock (_lock) { _velocity = new Vector(value.x, value.y); } }
    }

    public double Mass { get; } = 1.0;

    // Atomowy odczyt pozycji i prędkości pod jednym lockiem – zapobiega torn read.
    public (IVector position, IVector velocity) GetState()
    {
      lock (_lock) { return (_position, _velocity); }
    }

    #endregion IBall

    #region internal API

    internal void Stop() => _running = false;

    internal void Move(double deltaTime)
    {
      lock (_lock)
      {
        _position = new Vector(
          _position.x + _velocity.x * deltaTime,
          _position.y + _velocity.y * deltaTime);
      }

      _preNotificationCallback?.Invoke(this);

      if (_logger != null)
      {
        IVector pos, vel;
        lock (_lock) { pos = _position; vel = _velocity; }
        _logger.Log(_ballId, pos.x, pos.y, vel.x, vel.y, _stopwatch.ElapsedMilliseconds);
      }

      NewPositionNotification?.Invoke(this, _position);
    }

    // Overload dla kompatybilności ze starszymi testami
    internal void Move(double deltaTime, double tableWidth, double tableHeight, double ballDiameter)
      => Move(deltaTime);

    #endregion internal API

    #region private

    private Vector _position;
    private Vector _velocity;
    private volatile bool _running = true;

    private readonly int _ballId;
    private readonly DiagnosticLogger? _logger;
    private readonly Thread _thread;
    private readonly object _lock = new object();
    private readonly Stopwatch _stopwatch;
    private readonly Action<Ball>? _preNotificationCallback;

    internal const int TargetPeriodMs = 16; // ~60 fps

    // Real-time loop: deltaTime to rzeczywisty czas od ostatniej klatki (Stopwatch),
    // nie stała nominalna. Thread.Sleep ogranicza CPU ale nie wpływa na fizykę.
    private void Run()
    {
      var sw = Stopwatch.StartNew();
      while (_running)
      {
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
      Move(TargetPeriodMs / 1000.0);
    }

    #endregion TestingInfrastructure
  }
}
