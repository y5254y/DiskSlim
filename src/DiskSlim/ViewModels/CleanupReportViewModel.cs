using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DiskSlim.ViewModels;

/// <summary>
/// 清理历史报告 ViewModel，显示过往清理记录并支持导出
/// </summary>
public partial class CleanupReportViewModel : ObservableObject
{
    private readonly ICleanupReportService _reportService;

    /// <summary>历史报告列表</summary>
    public ObservableCollection<CleanupReport> Reports { get; } = new();

    [ObservableProperty]
    private CleanupReport? _selectedReport;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "加载历史记录中...";

    [ObservableProperty]
    private bool _hasReports;

    public CleanupReportViewModel(ICleanupReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// 加载历史报告列表
    /// </summary>
    [RelayCommand]
    public async Task LoadReportsAsync()
    {
        IsLoading = true;
        try
        {
            var reports = await _reportService.GetReportsAsync(50);
            Reports.Clear();
            foreach (var r in reports)
                Reports.Add(r);
            HasReports = Reports.Count > 0;
            StatusMessage = HasReports ? $"共 {Reports.Count} 条清理记录" : "暂无清理记录";
            if (HasReports && SelectedReport == null)
                SelectedReport = Reports[0];
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
    /// 删除选中的报告
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedReportAsync()
    {
        if (SelectedReport == null) return;

        try
        {
            await _reportService.DeleteReportAsync(SelectedReport.Id);
            Reports.Remove(SelectedReport);
            HasReports = Reports.Count > 0;
            SelectedReport = Reports.Count > 0 ? Reports[0] : null;
            StatusMessage = HasReports ? $"共 {Reports.Count} 条清理记录" : "暂无清理记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 将选中报告导出为 TXT 文件
    /// </summary>
    [RelayCommand]
    private async Task ExportToTextAsync()
    {
        if (SelectedReport == null) return;

        try
        {
            string content = _reportService.ExportToText(SelectedReport);
            await SaveFileAsync(content, "DiskSlim_清理报告.txt", "txt");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 将选中报告导出为 HTML 文件
    /// </summary>
    [RelayCommand]
    private async Task ExportToHtmlAsync()
    {
        if (SelectedReport == null) return;

        try
        {
            string content = _reportService.ExportToHtml(SelectedReport);
            await SaveFileAsync(content, "DiskSlim_清理报告.html", "html");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 将选中报告导出为 CSV 文件
    /// </summary>
    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        if (SelectedReport == null) return;

        try
        {
            string content = _reportService.ExportToCsv(SelectedReport);
            await SaveFileAsync(content, "DiskSlim_清理报告.csv", "csv");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 使用文件保存对话框保存文件
    /// </summary>
    private static async Task SaveFileAsync(string content, string suggestedName, string extension)
    {
        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = suggestedName;

        if (extension == "txt")
            picker.FileTypeChoices.Add("文本文件", new List<string> { ".txt" });
        else if (extension == "csv")
            picker.FileTypeChoices.Add("CSV 文件", new List<string> { ".csv" });
        else
            picker.FileTypeChoices.Add("HTML 文件", new List<string> { ".html" });

        // 将 picker 绑定到主窗口（WinUI 3 必须）
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file != null)
            await FileIO.WriteTextAsync(file, content);
    }
}
