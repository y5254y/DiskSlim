using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace DiskSlim.Controls;

/// <summary>
/// 磁盘空间仪表盘环形图控件，通过 Path + 弧形几何体绘制使用率
/// </summary>
public sealed partial class SpaceGaugeControl : UserControl
{
    // ===== 依赖属性 =====

    public static readonly DependencyProperty UsagePercentProperty =
        DependencyProperty.Register(
            nameof(UsagePercent),
            typeof(double),
            typeof(SpaceGaugeControl),
            new PropertyMetadata(0.0, OnUsagePercentChanged));

    public static readonly DependencyProperty UsageTextProperty =
        DependencyProperty.Register(
            nameof(UsageText),
            typeof(string),
            typeof(SpaceGaugeControl),
            new PropertyMetadata("0%"));

    /// <summary>使用率（0.0 ~ 100.0）</summary>
    public double UsagePercent
    {
        get => (double)GetValue(UsagePercentProperty);
        set => SetValue(UsagePercentProperty, value);
    }

    /// <summary>中心显示文字（如 "72%"）</summary>
    public string UsageText
    {
        get => (string)GetValue(UsageTextProperty);
        set => SetValue(UsageTextProperty, value);
    }

    public SpaceGaugeControl()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => DrawArc();
    }

    /// <summary>
    /// 使用率变化时重绘弧形
    /// </summary>
    private static void OnUsagePercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpaceGaugeControl control)
            control.DrawArc();
    }

    /// <summary>
    /// 绘制环形弧形（使用 Path + ArcSegment）
    /// </summary>
    private void DrawArc()
    {
        GaugeCanvas.Children.Clear();

        double size = ActualWidth > 0 ? ActualWidth : 180;
        double strokeWidth = 14;
        double radius = (size - strokeWidth) / 2;
        double cx = size / 2;
        double cy = size / 2;

        double percent = Math.Max(0, Math.Min(100, UsagePercent)) / 100.0;
        if (percent <= 0) return;

        // 从顶部（-90度）开始顺时针绘制
        double startAngle = -Math.PI / 2;
        double endAngle = startAngle + (2 * Math.PI * percent);

        // 如果满100%，稍微缩小一点以避免 ArcSegment 的 bug
        bool isLargeArc = percent >= 0.5;
        if (percent >= 1.0)
        {
            endAngle = startAngle + 2 * Math.PI - 0.001;
            isLargeArc = true;
        }

        Point startPoint = new Point(
            cx + radius * Math.Cos(startAngle),
            cy + radius * Math.Sin(startAngle));
        Point endPoint = new Point(
            cx + radius * Math.Cos(endAngle),
            cy + radius * Math.Sin(endAngle));

        // 根据使用率决定颜色（>90% 红色警告，>70% 黄色，其他为主题色）
        Color arcColor = UsagePercent >= 90
            ? Color.FromArgb(255, 232, 77, 80)   // 红色
            : UsagePercent >= 70
                ? Color.FromArgb(255, 255, 168, 0) // 黄色
                : Color.FromArgb(255, 0, 120, 212); // 蓝色（Fluent accent）

        var arcPath = new Path
        {
            Stroke = new SolidColorBrush(arcColor),
            StrokeThickness = strokeWidth,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Fill = new SolidColorBrush(Colors.Transparent)
        };

        var pathFigure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false
        };

        var arcSegment = new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Clockwise
        };

        pathFigure.Segments.Add(arcSegment);
        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);
        arcPath.Data = pathGeometry;

        GaugeCanvas.Children.Add(arcPath);
    }
}
