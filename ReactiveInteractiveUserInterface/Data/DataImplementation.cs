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
  internal class DataImplementation : DataAbstractAPI
  {
    public DataImplementation() : this(null) { }

    internal DataImplementation(ILogger? logger)
    {
      _logger = logger ?? DiagnosticLogger.CreateDefaultLogger();
      _ownLogger = (logger == null);
    }

    #region DataAbstractAPI

    public override IBall? GetMouseBall() => _mouseBall;

    public override void SetMouseBallPosition(double x, double y)
    {
      if (Disposed) return;
      double clampedX = Math.Clamp(x, 0, TableDimensions.Width - TableDimensions.BallSize);
      double clampedY = Math.Clamp(y, 0, TableDimensions.Height - TableDimensions.BallSize);
      _mouseBall.UpdatePosition(new Vector(clampedX, clampedY));
    }

    public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(DataImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));

      Random random = new Random();
      for (int i = 0; i < numberOfBalls; i++)
      {
        double x = TableDimensions.BallSize + random.NextDouble() * (TableDimensions.Width - 2 * TableDimensions.BallSize);
        double y = TableDimensions.BallSize + random.NextDouble() * (TableDimensions.Height - 2 * TableDimensions.BallSize);
        double speed = 50.0 + random.NextDouble() * 100.0;
        double angle = random.NextDouble() * 2 * Math.PI;

        Vector startPos = new(x, y);
        Vector startVel = new(Math.Cos(angle) * speed, Math.Sin(angle) * speed);

        Ball newBall = new(startPos, startVel,
                           ballId: i,
                           logger: _logger);

        lock (_ballsLock) { BallsList.Add(newBall); }
        upperLayerHandler(startPos, newBall);
      }
    }

    #endregion DataAbstractAPI

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          lock (_ballsLock)
          {
            foreach (Ball ball in BallsList) ball.Stop();
            BallsList.Clear();
          }
          _mouseBall.Stop();
          if (_ownLogger) _logger.Dispose();
        }
        Disposed = true;
      }
    }

    public override void Dispose()
    {
      if (Disposed) throw new ObjectDisposedException(nameof(DataImplementation));
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    #endregion IDisposable

    #region private

    private bool Disposed = false;
    private readonly List<Ball> BallsList = [];
    private readonly object _ballsLock = new object();
    private readonly ILogger _logger;
    private readonly bool _ownLogger;
    private readonly MouseBall _mouseBall = new MouseBall();

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
      => returnBallsList(BallsList);

    [Conditional("DEBUG")]
    internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
      => returnNumberOfBalls(BallsList.Count);

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
      => returnInstanceDisposed(Disposed);

    #endregion TestingInfrastructure
  }
}
