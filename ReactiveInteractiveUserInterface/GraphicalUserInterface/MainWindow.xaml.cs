//__________________________________________________________________________________________
//
//  Copyright 2024 Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and to get started
//  comment using the discussion panel at
//  https://github.com/mpostol/TP/discussions/182
//__________________________________________________________________________________________

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TP.ConcurrentProgramming.Presentation.ViewModel;

namespace TP.ConcurrentProgramming.PresentationView
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
      if (DataContext is MainWindowViewModel viewModel)
        viewModel.Dispose();
      base.OnClosed(e);
    }

    private void BallTableBorder_MouseMove(object sender, MouseEventArgs e)
    {
      var border = (Border)sender;
      var pos = e.GetPosition(border);
      double borderLeft = border.BorderThickness.Left;
      double borderTop  = border.BorderThickness.Top;
      if (DataContext is MainWindowViewModel vm)
        vm.OnMouseMove(pos.X - borderLeft, pos.Y - borderTop);
    }
  }
}
