using DiskSlim.Models;
using Microsoft.Data.Sqlite;

namespace DiskSlim.Services;

/// <summary>
/// 磁盘快照服务实现，使用 SQLite 存储历史快照数据
/// </summary>
public class SnapshotService : ISnapshotService
{
    private readonly string _dbPath;
    private const string DriveToScan = @"C:\";

    public SnapshotService()
    {
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiskSlim");
        Directory.CreateDirectory(appDataDir);
        _dbPath = Path.Combine(appDataDir, "snapshots.db");
    }

    /// <summary>
    /// 初始化快照数据库，创建表结构
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS DiskSnapshots (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                SnapshotTime TEXT    NOT NULL,
                Label        TEXT    NOT NULL DEFAULT '',
                TotalBytes   INTEGER NOT NULL DEFAULT 0,
                UsedBytes    INTEGER NOT NULL DEFAULT 0,
                FreeBytes    INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS SnapshotFolderItems (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                SnapshotId  INTEGER NOT NULL REFERENCES DiskSnapshots(Id) ON DELETE CASCADE,
                FolderPath  TEXT    NOT NULL,
                SizeBytes   INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_folder_snapshot ON SnapshotFolderItems(SnapshotId);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 保存快照到数据库（含文件夹明细）
    /// </summary>
    public async Task SaveSnapshotAsync(DiskSnapshot snapshot)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            // 插入快照主记录
            var insertSnap = conn.CreateCommand();
            insertSnap.Transaction = (SqliteTransaction)tx;
            insertSnap.CommandText = """
                INSERT INTO DiskSnapshots (SnapshotTime, Label, TotalBytes, UsedBytes, FreeBytes)
                VALUES ($time, $label, $total, $used, $free);
                SELECT last_insert_rowid();
                """;
            insertSnap.Parameters.AddWithValue("$time", snapshot.SnapshotTime.ToString("o"));
            insertSnap.Parameters.AddWithValue("$label", snapshot.Label);
            insertSnap.Parameters.AddWithValue("$total", snapshot.TotalBytes);
            insertSnap.Parameters.AddWithValue("$used", snapshot.UsedBytes);
            insertSnap.Parameters.AddWithValue("$free", snapshot.FreeBytes);

            var idObj = await insertSnap.ExecuteScalarAsync();
            int snapId = System.Convert.ToInt32(idObj);
            snapshot.Id = snapId;

            // 插入文件夹明细
            foreach (var folder in snapshot.FolderItems)
            {
                var insertFolder = conn.CreateCommand();
                insertFolder.Transaction = (SqliteTransaction)tx;
                insertFolder.CommandText = """
                    INSERT INTO SnapshotFolderItems (SnapshotId, FolderPath, SizeBytes)
                    VALUES ($sid, $path, $size);
                    """;
                insertFolder.Parameters.AddWithValue("$sid", snapId);
                insertFolder.Parameters.AddWithValue("$path", folder.FolderPath);
                insertFolder.Parameters.AddWithValue("$size", folder.SizeBytes);
                await insertFolder.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 获取所有快照列表（不含文件夹明细，按时间倒序）
    /// </summary>
    public async Task<IReadOnlyList<DiskSnapshot>> GetSnapshotsAsync()
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        var snapshots = new List<DiskSnapshot>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, SnapshotTime, Label, TotalBytes, UsedBytes, FreeBytes
            FROM DiskSnapshots
            ORDER BY Id DESC;
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            snapshots.Add(new DiskSnapshot
            {
                Id = reader.GetInt32(0),
                SnapshotTime = DateTime.Parse(reader.GetString(1)),
                Label = reader.GetString(2),
                TotalBytes = reader.GetInt64(3),
                UsedBytes = reader.GetInt64(4),
                FreeBytes = reader.GetInt64(5)
            });
        }

        return snapshots;
    }

    /// <summary>
    /// 获取指定快照的完整数据（含文件夹明细）
    /// </summary>
    public async Task<DiskSnapshot?> GetSnapshotWithDetailsAsync(int snapshotId)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        // 读取快照主记录
        var snapCmd = conn.CreateCommand();
        snapCmd.CommandText = """
            SELECT Id, SnapshotTime, Label, TotalBytes, UsedBytes, FreeBytes
            FROM DiskSnapshots
            WHERE Id = $id;
            """;
        snapCmd.Parameters.AddWithValue("$id", snapshotId);

        DiskSnapshot? snapshot = null;
        await using (var reader = await snapCmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                snapshot = new DiskSnapshot
                {
                    Id = reader.GetInt32(0),
                    SnapshotTime = DateTime.Parse(reader.GetString(1)),
                    Label = reader.GetString(2),
                    TotalBytes = reader.GetInt64(3),
                    UsedBytes = reader.GetInt64(4),
                    FreeBytes = reader.GetInt64(5)
                };
            }
        }

        if (snapshot == null) return null;

        // 读取文件夹明细
        var folderCmd = conn.CreateCommand();
        folderCmd.CommandText = """
            SELECT Id, FolderPath, SizeBytes
            FROM SnapshotFolderItems
            WHERE SnapshotId = $sid
            ORDER BY SizeBytes DESC;
            """;
        folderCmd.Parameters.AddWithValue("$sid", snapshotId);

        await using var folderReader = await folderCmd.ExecuteReaderAsync();
        while (await folderReader.ReadAsync())
        {
            snapshot.FolderItems.Add(new SnapshotFolderItem
            {
                Id = folderReader.GetInt32(0),
                SnapshotId = snapshotId,
                FolderPath = folderReader.GetString(1),
                SizeBytes = folderReader.GetInt64(2)
            });
        }

        return snapshot;
    }

    /// <summary>
    /// 删除指定快照（含文件夹明细，通过 ON DELETE CASCADE 自动删除）
    /// </summary>
    public async Task DeleteSnapshotAsync(int snapshotId)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        // 先启用外键支持
        var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
        await pragmaCmd.ExecuteNonQueryAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM DiskSnapshots WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", snapshotId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 对比两次快照，返回各文件夹空间变化
    /// </summary>
    public async Task<IReadOnlyList<SnapshotDiffItem>> CompareSnapshotsAsync(int oldSnapshotId, int newSnapshotId)
    {
        var oldSnapshot = await GetSnapshotWithDetailsAsync(oldSnapshotId);
        var newSnapshot = await GetSnapshotWithDetailsAsync(newSnapshotId);

        if (oldSnapshot == null || newSnapshot == null)
            return [];

        // 构建文件夹路径 → 大小 映射
        var oldMap = oldSnapshot.FolderItems.ToDictionary(f => f.FolderPath, f => f.SizeBytes);
        var newMap = newSnapshot.FolderItems.ToDictionary(f => f.FolderPath, f => f.SizeBytes);

        // 合并所有文件夹路径
        var allPaths = oldMap.Keys.Union(newMap.Keys).ToHashSet();

        var diffs = new List<SnapshotDiffItem>();
        foreach (var path in allPaths)
        {
            oldMap.TryGetValue(path, out long oldSize);
            newMap.TryGetValue(path, out long newSize);

            diffs.Add(new SnapshotDiffItem
            {
                FolderPath = path,
                OldSizeBytes = oldSize,
                NewSizeBytes = newSize
            });
        }

        // 按变化量绝对值降序排列
        diffs.Sort((a, b) => Math.Abs(b.DeltaBytes).CompareTo(Math.Abs(a.DeltaBytes)));

        return diffs;
    }

    /// <summary>
    /// 扫描当前C盘状态，创建一个新快照
    /// </summary>
    public async Task<DiskSnapshot> CreateSnapshotAsync(string? label, IProgress<string>? progress, CancellationToken token)
    {
        progress?.Report("正在获取磁盘信息...");

        // 获取磁盘空间信息
        var driveInfo = new System.IO.DriveInfo(DriveToScan);
        var snapshot = new DiskSnapshot
        {
            SnapshotTime = DateTime.Now,
            Label = label ?? string.Empty,
            TotalBytes = driveInfo.TotalSize,
            UsedBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
            FreeBytes = driveInfo.AvailableFreeSpace
        };

        // 扫描C盘顶层文件夹大小
        progress?.Report("正在扫描文件夹大小...");
        try
        {
            var topDirs = Directory.GetDirectories(DriveToScan, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in topDirs)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    progress?.Report($"扫描：{Path.GetFileName(dir)}");
                    long size = await Task.Run(() => GetDirectorySize(dir, token), token);
                    snapshot.FolderItems.Add(new SnapshotFolderItem
                    {
                        FolderPath = dir,
                        SizeBytes = size
                    });
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // 忽略无权限的文件夹
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // 忽略扫描错误
        }

        progress?.Report("正在保存快照...");
        await SaveSnapshotAsync(snapshot);

        return snapshot;
    }

    /// <summary>
    /// 递归计算文件夹大小
    /// </summary>
    private static long GetDirectorySize(string path, CancellationToken token)
    {
        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch { }
            }
        }
        catch { }
        return total;
    }
}
