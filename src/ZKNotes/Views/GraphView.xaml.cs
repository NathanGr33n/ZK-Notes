using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class GraphView : UserControl
{
    private Point _panStart;
    private bool _isPanning;
    private const double NodeRadius = 8;

    public GraphView()
    {
        InitializeComponent();
        IsVisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            RefreshGraph();
    }

    private GraphViewModel? GetVm() => (DataContext as MainViewModel)?.Graph;

    private void RefreshGraph()
    {
        var vm = GetVm();
        if (vm is null) return;

        vm.BuildGraph();
        RenderGraph();
    }

    private void RenderGraph()
    {
        var vm = GetVm();
        if (vm is null) return;

        GraphCanvas.Children.Clear();

        var nodeMap = vm.Nodes.ToDictionary(n => n.Id);

        // Draw edges
        foreach (var edge in vm.Edges)
        {
            if (!nodeMap.TryGetValue(edge.SourceId, out var src) ||
                !nodeMap.TryGetValue(edge.TargetId, out var tgt))
                continue;

            var line = new Line
            {
                X1 = src.X, Y1 = src.Y,
                X2 = tgt.X, Y2 = tgt.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(80, 108, 182, 255)),
                StrokeThickness = 1.5
            };
            GraphCanvas.Children.Add(line);
        }

        // Draw nodes
        foreach (var node in vm.Nodes)
        {
            var ellipse = new Ellipse
            {
                Width = NodeRadius * 2,
                Height = NodeRadius * 2,
                Fill = new SolidColorBrush(Color.FromRgb(108, 182, 255)),
                Stroke = new SolidColorBrush(Color.FromRgb(140, 200, 255)),
                StrokeThickness = 1.5,
                Cursor = Cursors.Hand,
                Tag = node
            };

            Canvas.SetLeft(ellipse, node.X - NodeRadius);
            Canvas.SetTop(ellipse, node.Y - NodeRadius);

            ellipse.MouseEnter += Node_MouseEnter;
            ellipse.MouseLeave += Node_MouseLeave;
            ellipse.MouseLeftButtonDown += Node_Click;

            GraphCanvas.Children.Add(ellipse);

            // Label
            var label = new TextBlock
            {
                Text = node.Title,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(label, node.X + NodeRadius + 4);
            Canvas.SetTop(label, node.Y - 6);
            GraphCanvas.Children.Add(label);
        }
    }

    private void Node_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Ellipse { Tag: GraphNode node })
        {
            TooltipTitle.Text = node.Title;
            TooltipSnippet.Text = node.Snippet;
            TooltipBorder.Visibility = Visibility.Visible;

            var pos = e.GetPosition(this);
            TooltipBorder.Margin = new Thickness(pos.X + 16, pos.Y + 16, 0, 0);
        }
    }

    private void Node_MouseLeave(object sender, MouseEventArgs e)
    {
        TooltipBorder.Visibility = Visibility.Collapsed;
    }

    private void Node_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse { Tag: GraphNode node })
        {
            GetVm()?.OpenNodeCommand.Execute(node.Id);
            e.Handled = true;
        }
    }

    // Zoom
    private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? 1.1 : 0.9;
        var newScale = Math.Clamp(CanvasScale.ScaleX * factor, 0.2, 5.0);
        CanvasScale.ScaleX = newScale;
        CanvasScale.ScaleY = newScale;
    }

    // Pan
    private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source == GraphCanvas)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            GraphCanvas.CaptureMouse();
        }
    }

    private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        GraphCanvas.ReleaseMouseCapture();
    }

    private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;

        var pos = e.GetPosition(this);
        CanvasTranslate.X += pos.X - _panStart.X;
        CanvasTranslate.Y += pos.Y - _panStart.Y;
        _panStart = pos;
    }

    // Toolbar handlers
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshGraph();
    private void ZoomIn_Click(object sender, RoutedEventArgs e) => GetVm()?.ZoomInCommand.Execute(null);
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => GetVm()?.ZoomOutCommand.Execute(null);
    private void ResetView_Click(object sender, RoutedEventArgs e)
    {
        GetVm()?.ResetViewCommand.Execute(null);
        CanvasScale.ScaleX = 1;
        CanvasScale.ScaleY = 1;
        CanvasTranslate.X = 0;
        CanvasTranslate.Y = 0;
    }
}
