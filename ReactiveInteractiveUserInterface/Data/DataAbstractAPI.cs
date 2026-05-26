//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data
{
  public enum LogLevel
  {
    Emergency = 0,
    Alert     = 1,
    Critical  = 2,
    Error     = 3,
    Warning   = 4,
    Notice    = 5,
    Info      = 6,
    Debug     = 7
  }

  public interface ILogger : IDisposable
  {
    void Log(LogLevel level, string message);
  }

  public static class TableDimensions
  {
    public const double Width = 400.0;
    public const double Height = 400.0;
    public const double BallSize = 30.0;
  }

  public abstract class DataAbstractAPI : IDisposable
  {
    #region Layer Factory

    public static DataAbstractAPI GetDataLayer()
    {
      return modelInstance.Value;
    }

    #endregion Layer Factory

    #region public API

    public static ILogger CreateDiagnosticLogger(string filename = "diagnostic_log.txt")
      => DiagnosticLogger.CreateDefaultLogger(filename);

    public abstract void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler);

    public virtual IBall? GetMouseBall() => null;

    public virtual void SetMouseBallPosition(double x, double y) { }

    #endregion public API

    #region IDisposable

    public abstract void Dispose();

    #endregion IDisposable

    #region private

    private static Lazy<DataAbstractAPI> modelInstance = new Lazy<DataAbstractAPI>(() => new DataImplementation());

    #endregion private
  }

  public interface IVector
  {
    double x { get; init; }
    double y { get; init; }
  }

  public interface IBall
  {
    event EventHandler<IVector> NewPositionNotification;

    IVector Velocity { get; }

    double Mass { get; }

    bool IsKinematic => false;

    IVector Position { get; }

    (IVector position, IVector velocity) GetState();

    void EnqueueCorrection(IVector newPosition, IVector newVelocity);
  }
}
