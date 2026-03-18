using DiskSlim.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace DiskSlim.Views;

/// <summary>
/// 增长趋势分析页面，展示C盘空间使用变化趋势和预测
/// </summary>
public sealed partial class TrendPage : Page
{
    public TrendViewModel ViewModel { get; }

    public TrendPage()
    {
        ViewModel = App.Services.GetRequiredService<TrendViewModel>();
        this.InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadTrendDataAsync();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrendViewModel.TrendPoints))
                DrawTrendChart();
        };
    }

    /// <summary>
    /// 在 Canvas 上绘制折线趋势图
    /// </summary>
    private void DrawTrendChart()
    {
        TrendCanvas.Children.Clear();

        var points = ViewModel.TrendPoints;
        if (points.Count < 2) return;

        double canvasWidth = TrendCanvas.ActualWidth > 0 ? TrendCanvas.ActualWidth : 800;
        double canvasHeight = TrendCanvas.ActualHeight > 0 ? TrendCanvas.ActualHeight : 200;
        double padding = 20;

        double minGB = points.Min(p => p.UsedGB);
        double maxGB = points.Max(p => p.UsedGB);
        double rangeGB = maxGB - minGB;
        if (rangeGB < 0.1) rangeGB = 0.1;

        double minTime = points.Min(p => p.Time.Ticks);
        double maxTime = points.Max(p => p.Time.Ticks);
        double timeRange = maxTime - minTime;
        if (timeRange <= 0) timeRange = 1;

        // 绘制背景网格线
        var gridBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 128, 128, 128));
        for (int i = 0; i <= 4; i++)
        {
            double y = padding + (canvasHeight - 2 * padding) * i / 4;
            var gridLine = new Line
            {
                X1 = padding, Y1 = y,
                X2 = canvasWidth - padding, Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1
            };
            TrendCanvas.Children.Add(gridLine);
        }

        // 绘制折线
        var lineBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212));
        var polyline = new Polyline
        {
            Stroke = lineBrush,
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round
        };

        var fillBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 0, 120, 212));
        var fillPolygon = new Polygon { Fill = fillBrush };

        // 左下角起始点
        fillPolygon.Points.Add(new Windows.Foundation.Point(padding, canvasHeight - padding));

        foreach (var point in points)
        {
            double x = padding + (canvasWidth - 2 * padding) * (point.Time.Ticks - minTime) / timeRange;
            double y = canvasHeight - padding - (canvasHeight - 2 * padding) * (point.UsedGB - minGB) / rangeGB;
            polyline.Points.Add(new Windows.Foundation.Point(x, y));
            fillPolygon.Points.Add(new Windows.Foundation.Point(x, y));
        }

        // 右下角结束点
        fillPolygon.Points.Add(new Windows.Foundation.Point(
            padding + (canvasWidth - 2 * padding), canvasHeight - padding));

        TrendCanvas.Children.Add(fillPolygon);
        TrendCanvas.Children.Add(polyline);

        // 绘制数据点圆圈
        foreach (var point in points)
        {
            double x = padding + (canvasWidth - 2 * padding) * (point.Time.Ticks - minTime) / timeRange;
            double y = canvasHeight - padding - (canvasHeight - 2 * padding) * (point.UsedGB - minGB) / rangeGB;
            var dot = new Ellipse
            {
                Width = 8, Height = 8,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = lineBrush,
                StrokeThickness = 2
            };
            Canvas.SetLeft(dot, x - 4);
            Canvas.SetTop(dot, y - 4);
            TrendCanvas.Children.Add(dot);
        }

        // 更新时间轴标签
        AxisStartLabel.Text = points.First().TimeText;
        AxisEndLabel.Text = points.Last().TimeText;
    }
}
