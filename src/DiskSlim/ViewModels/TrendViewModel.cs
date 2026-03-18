using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 趋势分析页面的单个数据点
/// </summary>
public class TrendDataPoint
{
    /// <summary>时间点</summary>
    public DateTime Time { get; set; }

    /// <summary>已用空间（字节）</summary>
    public long UsedBytes { get; set; }

    /// <summary>已用空间（GB）</summary>
    public double UsedGB => Helpers.FileSizeHelper.ToGB(UsedBytes);

    /// <summary>时间格式化文字</summary>
    public string TimeText => Time.ToString("MM/dd HH:mm");

    /// <summary>鼠标悬停显示文字</summary>
    public string TooltipText => $"{Time:yyyy-MM-dd HH:mm}\n{Helpers.FileSizeHelper.Format(UsedBytes)}";
}

/// <summary>
/// 增长趋势分析页面 ViewModel
/// </summary>
public partial class TrendViewModel : ObservableObject
{
    private readonly ISnapshotService _snapshotService;

    /// <summary>趋势图数据点</summary>
    public ObservableCollection<TrendDataPoint> TrendPoints { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "请先添加快照数据以分析趋势";

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private string _selectedTimeRange = "全部";

    [ObservableProperty]
    private string _predictionText = string.Empty;

    [ObservableProperty]
    private bool _hasPrediction;

    [ObservableProperty]
    private bool _isDangerPrediction;

    [ObservableProperty]
    private string _minValueText = "0 GB";

    [ObservableProperty]
    private string _maxValueText = "0 GB";

    [ObservableProperty]
    private string _latestValueText = "—";

    [ObservableProperty]
    private string _growthRateText = "—";

    /// <summary>可选时间范围</summary>
    public List<string> TimeRanges { get; } = ["7天", "30天", "90天", "全部"];

    public TrendViewModel(ISnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    /// <summary>
    /// 加载趋势数据
    /// </summary>
    [RelayCommand]
    public async Task LoadTrendDataAsync()
    {
        IsLoading = true;
        StatusMessage = "正在加载趋势数据...";

        try
        {
            var allSnapshots = await _snapshotService.GetSnapshotsAsync();

            // 按时间范围筛选
            var cutoff = SelectedTimeRange switch
            {
                "7天" => DateTime.Now.AddDays(-7),
                "30天" => DateTime.Now.AddDays(-30),
                "90天" => DateTime.Now.AddDays(-90),
                _ => DateTime.MinValue
            };

            var filtered = allSnapshots
                .Where(s => s.SnapshotTime >= cutoff)
                .OrderBy(s => s.SnapshotTime)
                .ToList();

            TrendPoints.Clear();
            foreach (var snap in filtered)
            {
                TrendPoints.Add(new TrendDataPoint
                {
                    Time = snap.SnapshotTime,
                    UsedBytes = snap.UsedBytes
                });
            }

            HasData = TrendPoints.Count > 0;

            if (HasData)
            {
                // 最大值/最小值/最新值
                double minGB = TrendPoints.Min(p => p.UsedGB);
                double maxGB = TrendPoints.Max(p => p.UsedGB);
                MinValueText = $"{minGB:F1} GB";
                MaxValueText = $"{maxGB:F1} GB";
                LatestValueText = Helpers.FileSizeHelper.Format(TrendPoints.Last().UsedBytes);

                // 计算增长率
                if (TrendPoints.Count >= 2)
                {
                    var first = TrendPoints.First();
                    var last = TrendPoints.Last();
                    double days = (last.Time - first.Time).TotalDays;
                    if (days > 0)
                    {
                        double deltaGB = last.UsedGB - first.UsedGB;
                        double dailyGB = deltaGB / days;
                        GrowthRateText = dailyGB == 0
                            ? "±0.00 GB/天（稳定）"
                            : $"{dailyGB:+0.00;-0.00} GB/天";

                        // 预测
                        CalculatePrediction(last, dailyGB);
                    }
                    else
                    {
                        GrowthRateText = "数据点时间间隔太短";
                    }
                }

                StatusMessage = $"共 {TrendPoints.Count} 个数据点（{SelectedTimeRange}）";
            }
            else
            {
                StatusMessage = "所选时间范围内无快照数据，请先在"历史快照"页创建快照";
                PredictionText = string.Empty;
                HasPrediction = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 计算磁盘满载预测
    /// </summary>
    private void CalculatePrediction(TrendDataPoint latest, double dailyGrowthGB)
    {
        if (dailyGrowthGB <= 0)
        {
            PredictionText = "磁盘使用量稳定或在缩减，无需担心";
            HasPrediction = true;
            IsDangerPrediction = false;
            return;
        }

        // 获取磁盘总大小（假设总大小固定，从最后的数据点推算）
        // 这里用快照的 TotalBytes 近似
        try
        {
            var driveInfo = new System.IO.DriveInfo(@"C:\");
            double totalGB = Helpers.FileSizeHelper.ToGB(driveInfo.TotalSize);
            double freeGB = Helpers.FileSizeHelper.ToGB(driveInfo.AvailableFreeSpace);
            double daysToFull = freeGB / dailyGrowthGB;

            if (daysToFull < 0)
            {
                PredictionText = "磁盘使用量稳定或在缩减，无需担心";
                IsDangerPrediction = false;
            }
            else if (daysToFull < 7)
            {
                PredictionText = $"⚠️ 危险！按当前增长速率，C盘将在约 {daysToFull:F0} 天内满！请立即清理！";
                IsDangerPrediction = true;
            }
            else if (daysToFull < 30)
            {
                PredictionText = $"⚠️ 注意：按当前速率，C盘将在约 {daysToFull:F0} 天内满，建议提前清理";
                IsDangerPrediction = true;
            }
            else
            {
                PredictionText = $"✅ 按当前速率，C盘约 {daysToFull:F0} 天后才会满（{DateTime.Now.AddDays(daysToFull):yyyy年MM月dd日}）";
                IsDangerPrediction = false;
            }

            HasPrediction = true;
        }
        catch
        {
            HasPrediction = false;
        }
    }

    /// <summary>
    /// 切换时间范围并重新加载
    /// </summary>
    partial void OnSelectedTimeRangeChanged(string value)
    {
        _ = LoadTrendDataAsync();
    }
}
