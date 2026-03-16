namespace DiskSlim.Services;

/// <summary>
/// 符号链接（Junction）服务实现，通过 Win32 API 创建和管理 NTFS Junction 链接
/// Junction 是目录级别的符号链接，无需管理员权限即可创建（某些情况下需要）
/// </summary>
public class SymlinkService : ISymlinkService
{
    // NTFS 重解析点标志
    private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
    private const uint FSCTL_SET_REPARSE_POINT = 0x000900A4;
    private const uint FSCTL_GET_REPARSE_POINT = 0x000900A8;
    private const uint FSCTL_DELETE_REPARSE_POINT = 0x000900AC;
    private const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;

    /// <summary>
    /// 创建 NTFS Junction 链接
    /// </summary>
    public async Task<bool> CreateJunctionAsync(string linkPath, string targetPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 确保链接路径的父目录存在
                string? parentDir = Path.GetDirectoryName(linkPath);
                if (parentDir != null && !Directory.Exists(parentDir))
                    Directory.CreateDirectory(parentDir);

                // 如果链接路径已存在（普通目录），先删除
                if (Directory.Exists(linkPath) && !IsJunction(linkPath))
                    return false; // 不能覆盖普通目录

                // 如果已是 Junction，先用 cmd rmdir 同步删除旧链接
                if (IsJunction(linkPath))
                {
                    var rmProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c rmdir \"{linkPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    rmProcess?.WaitForExit(5000);
                }

                // 创建空目录作为挂载点
                Directory.CreateDirectory(linkPath);

                // 通过 cmd mklink /J 创建 Junction（更简单可靠）
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c rmdir \"{linkPath}\" && mklink /J \"{linkPath}\" \"{targetPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                process?.WaitForExit(10000);
                return process?.ExitCode == 0 && IsJunction(linkPath);
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// 检测路径是否为 Junction（目录重解析点）
    /// </summary>
    public bool IsJunction(string path)
    {
        if (!Directory.Exists(path)) return false;

        var attr = File.GetAttributes(path);
        if (!attr.HasFlag(FileAttributes.ReparsePoint)) return false;

        // 进一步验证是 MountPoint 类型（Junction）
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return (dirInfo.Attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取 Junction 链接指向的真实目标路径
    /// </summary>
    public string? GetJunctionTarget(string junctionPath)
    {
        if (!IsJunction(junctionPath)) return null;

        try
        {
            // 使用 FileInfo.LinkTarget（.NET 6+ 支持）
            var linkTarget = new DirectoryInfo(junctionPath).LinkTarget;
            return linkTarget;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 删除 Junction 链接（只删除链接本身，不影响目标目录的内容）
    /// </summary>
    public async Task<bool> DeleteJunctionAsync(string junctionPath)
    {
        return await Task.Run(() =>
        {
            if (!IsJunction(junctionPath)) return false;

            try
            {
                // 使用 rmdir 删除 Junction（不加 /s 不会删除目标内容）
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c rmdir \"{junctionPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                process?.WaitForExit(5000);
                return !Directory.Exists(junctionPath) || !IsJunction(junctionPath);
            }
            catch
            {
                return false;
            }
        });
    }
}
