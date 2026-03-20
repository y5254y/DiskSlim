using System.Diagnostics;
using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// CompactOS 系统压缩服务实现，通过调用 compact.exe 查询和控制系统文件压缩
/// </summary>
public class CompactOsService : ICompactOsService
{
    // 典型系统压缩节省空间估算：1.5 GB
    private const long EstimatedSavedBytes = 1_500_000_000L;

    /// <inheritdoc />
    public async Task<CompactOsStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunCompactAsync("/CompactOS:query", cancellationToken);

            // 同时识别英文和中文输出
            bool isCompressed =
                output.Contains("system is in the Compact state", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("系统目前处于压缩状态", StringComparison.OrdinalIgnoreCase);

            return new CompactOsStatus(isCompressed, output, IsSuccess: true);
        }
        catch (Exception ex)
        {
            return new CompactOsStatus(
                IsCompressed: false,
                RawOutput: string.Empty,
                IsSuccess: false,
                ErrorMessage: ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<CompactOsResult> EnableCompactionAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("正在启用 CompactOS 系统压缩，请稍候（可能需要数分钟）…");
        return await RunCompactionCommandAsync("/CompactOS:always", EstimatedSavedBytes, progress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CompactOsResult> DisableCompactionAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("正在禁用 CompactOS 系统压缩，请稍候…");
        return await RunCompactionCommandAsync("/CompactOS:never", -EstimatedSavedBytes, progress, cancellationToken);
    }

    /// <summary>
    /// 执行 compact.exe 压缩控制命令并返回结果
    /// </summary>
    private async Task<CompactOsResult> RunCompactionCommandAsync(
        string argument,
        long estimatedBytes,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await RunCompactAsync(argument, cancellationToken);
            progress?.Report("操作完成。");
            return new CompactOsResult(IsSuccess: true, Output: output, EstimatedSavedBytes: estimatedBytes);
        }
        catch (Exception ex)
        {
            return new CompactOsResult(
                IsSuccess: false,
                Output: string.Empty,
                EstimatedSavedBytes: 0,
                ErrorMessage: ex.Message);
        }
    }

    /// <summary>
    /// 以异步方式运行 compact.exe 并返回标准输出内容
    /// </summary>
    private static async Task<string> RunCompactAsync(string argument, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo("compact.exe", argument)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动 compact.exe 进程");

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var errorMsg = !string.IsNullOrWhiteSpace(error) ? error : output;
            throw new InvalidOperationException($"compact.exe 返回错误（退出码 {process.ExitCode}）：{errorMsg}");
        }

        return string.IsNullOrWhiteSpace(output) ? error : output;
    }
}
