//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2023, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//  by introducing yourself and telling us what you do with this community.
//_____________________________________________________________________________________________________________________________________

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LogicIBall = TP.ConcurrentProgramming.BusinessLogic.IBall;
using LogicIPosition = TP.ConcurrentProgramming.BusinessLogic.IPosition;

namespace TP.ConcurrentProgramming.Presentation.Model
{
  internal class ModelBall : IBall
  {
    public ModelBall(double top, double left, LogicIBall underneathBall)
    {
      _uiContext = SynchronizationContext.Current;
      TopBackingField = top;
      LeftBackingField = left;
      underneathBall.NewPositionNotification += NewPositionNotificationHandler;
    }

    #region IBall

    public double Top
    {
      get { return TopBackingField; }
      private set
      {
        if (TopBackingField == value)
          return;
        TopBackingField = value;
        RaisePropertyChanged();
      }
    }

    public double Left
    {
      get { return LeftBackingField; }
      private set
      {
        if (LeftBackingField == value)
          return;
        LeftBackingField = value;
        RaisePropertyChanged();
      }
    }

    public double Diameter { get; init; } = 0;

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion INotifyPropertyChanged

    #endregion IBall

    #region private

    private double TopBackingField;
    private double LeftBackingField;
    private readonly SynchronizationContext? _uiContext;

    private void NewPositionNotificationHandler(object? sender, LogicIPosition e)
    {
      double newTop = e.y;
      double newLeft = e.x;
      if (_uiContext != null)
        _uiContext.Post(_ => { Top = newTop; Left = newLeft; }, null);
      else
        { Top = newTop; Left = newLeft; }
    }

    private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion private

    #region testing instrumentation

    [Conditional("DEBUG")]
    internal void SetLeft(double x)
    { Left = x; }

    [Conditional("DEBUG")]
    internal void SettTop(double x)
    { Top = x; }

    #endregion testing instrumentation
  }
}
