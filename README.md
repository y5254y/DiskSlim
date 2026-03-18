# DiskSlim — C盘瘦身大师

<div align="center">

![DiskSlim Logo](src/DiskSlim/Assets/app-icon.png)

**不用重装系统，不用动分区，轻松给C盘腾出大量空间**

*Free up your C drive without reinstalling Windows or repartitioning*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4)](https://www.microsoft.com/store)
[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-WinUI%203-0078D4)](https://learn.microsoft.com/windows/apps/winui/winui3/)

</div>

---

## 📖 项目介绍 / About

### 中文

DiskSlim（C盘瘦身大师）是一款专为 Windows 用户设计的 C 盘空间清理和等效扩容工具。
它解决了 Windows 用户最常见的痛点——C 盘空间不足，却不知道从哪里下手清理。

**核心理念：**
- 🛡️ **安全优先** — 三级安全标注（🟢安全/🟡谨慎/🔴危险），让用户放心操作
- 🔄 **可逆操作** — 所有危险操作均支持回退撤销
- 🎯 **精准清理** — 不仅清理系统垃圾，还能迁移软件和用户文件夹
- 🎨 **现代界面** — WinUI 3 原生界面，完美融入 Windows 11 风格

### English

DiskSlim is a Windows C: drive cleanup and capacity expansion tool designed for everyday users.
It addresses the most common Windows pain point — a full C: drive — without requiring disk
repartitioning or system reinstallation.

**Core principles:**
- 🛡️ **Safety first** — Three-tier safety ratings (🟢Safe/🟡Caution/🔴Danger)
- 🔄 **Reversible** — All destructive operations support undo/rollback
- 🎯 **Precise** — Clean system junk AND migrate folders/software
- 🎨 **Modern UI** — Native WinUI 3, perfect for Windows 11

---

## ✨ 功能特性 / Features

### 📊 空间分析仪表盘 / Space Analysis Dashboard
- C 盘空间使用环形图（已用/可用/总计）
- 可释放空间预估值
- Top 10 大文件/文件夹排行

### 🧹 智能清理 / Smart Cleanup
| 类型 | 说明 | 安全级别 |
|------|------|---------|
| 用户临时文件 | %TEMP% 临时文件 | 🟢 安全 |
| 系统临时文件 | C:\Windows\Temp | 🟢 安全 |
| 回收站 | 已删除文件 | 🟢 安全 |
| 错误报告 | .dmp/.wer 文件 | 🟢 安全 |
| 缩略图缓存 | 资源管理器缩略图 | 🟢 安全 |
| 浏览器缓存 | Edge/Chrome/Firefox 缓存 | 🟡 谨慎 |
| 开发工具缓存 | npm/pip/NuGet/Maven/Gradle | 🟡 谨慎 |
| 通讯软件缓存 | 微信/QQ/钉钉/Teams | 🟡 谨慎 |
| Windows Update 残留 | 更新下载缓存 | 🟡 谨慎 |
| Windows.old | 旧版 Windows 系统文件 | 🟡 谨慎 |
| 休眠文件 | hiberfil.sys | 🔴 危险 |

### 📋 清理历史报告 / Cleanup History
- 每次清理后自动生成报告（清理项目、释放空间、耗时）
- 应用内查看历史清理记录
- 支持导出为 TXT、CSV 或 HTML 文件

### 📸 历史快照与对比 / Snapshot History & Comparison *(Phase 3)*
- 保存C盘扫描快照（含时间戳、磁盘大小、各文件夹明细）
- 查看和管理所有历史快照
- 两次快照空间对比，高亮增长/缩减的文件夹

### 📈 增长趋势分析 / Growth Trend Analysis *(Phase 3)*
- 基于历史快照绘制C盘空间使用折线图
- 支持 7天/30天/90天/全部 时间范围查看
- 自动计算增长速率并预测C盘何时会满

### 🔍 旧文件/临时文件检测 / Old & Temp File Detection *(Phase 3)*
- 扫描长期未访问的旧文件（可配置：30/90/180/365天）
- 检测 .tmp/.temp/.bak/.old/.log 等临时文件
- 检测零字节空文件和 .dmp 崩溃转储文件
- 批量选择并移动到回收站

### ⏰ 定时扫描 + 通知 / Scheduled Scan & Notifications *(Phase 3)*
- 设置定时自动扫描计划（每天/每周/每月）
- 通过 Windows 任务计划程序注册定时任务
- 磁盘空间不足时发送 Windows Toast 通知

### 📂 文件夹迁移 / Folder Migration
将用户文件夹（桌面/文档/下载/图片/视频/音乐）迁移到其他磁盘，通过符号链接保持原路径透明可用

### 🔄 软件搬家 / Software Migration
将 C 盘上的已安装软件迁移到其他磁盘，通过 NTFS Junction 链接实现无感知迁移

---

## 🛠️ 技术栈 / Tech Stack

| 层次 | 技术 |
|------|------|
| UI 框架 | WinUI 3 + Windows App SDK 1.8 |
| 语言/运行时 | C# / .NET 10 |
| 架构 | MVVM (CommunityToolkit.Mvvm) |
| 依赖注入 | Microsoft.Extensions.DependencyInjection |
| 数据存储 | SQLite (Microsoft.Data.Sqlite) |
| 打包/分发 | MSIX → Microsoft Store |

---

## 📸 截图 / Screenshots

> 截图将在 UI 完善后更新 / Screenshots will be added after UI is polished

---

## 🚀 开发环境要求 / Development Requirements

- **操作系统**: Windows 10 版本 1903+ / Windows 11（推荐）
- **IDE**: Visual Studio 2022 17.8+ 或 Visual Studio 2026（推荐）
- **SDK**:
  - .NET 10.0 SDK
  - Windows App SDK 1.8
  - Windows 10 SDK 10.0.19041.0+
- **工作负载**: "Windows 应用程序开发"（Visual Studio）

---

## 🔨 构建说明 / Build Instructions

```bash
# 1. 克隆仓库
git clone https://github.com/y5254y/DiskSlim.git
cd DiskSlim

# 2. 还原 NuGet 包
dotnet restore DiskSlim.sln

# 3. 构建（Debug，x64）
dotnet build src/DiskSlim/DiskSlim.csproj -c Debug -r win-x64

# 4. 在 Visual Studio 中运行
# 打开 DiskSlim.sln，选择 x64 平台，按 F5 运行
```

> ⚠️ **注意**: 需要以管理员权限运行才能访问系统文件和创建符号链接

---

## 🗺️ 路线图 / Roadmap

### Phase 1 — MVP ✅
- [x] 项目架构搭建（MVVM + DI）
- [x] 磁盘扫描服务
- [x] 智能清理服务（10+ 清理项）
- [x] 文件夹迁移服务（Junction 符号链接）
- [x] 软件搬家服务
- [x] 主窗口 + 4个功能页面
- [x] 空间仪表盘环形图控件
- [x] 深色/浅色主题切换

### Phase 2 — 增强版 ✅
- [x] 软件缓存深度清理（浏览器 Chrome/Edge/Firefox、开发工具 npm/pip/NuGet/Maven/Gradle、通讯软件微信/QQ/钉钉/Teams）
- [x] Windows Update 残留清理（SoftwareDistribution\Download、Windows.old）
- [x] 用户文件夹迁移功能完善（自动检测可用盘符、扫描文件夹大小、迁移验证）
- [x] 安全等级标注系统完善（🟡/🔴级别清理弹出确认对话框）
- [x] 清理详情报告（历史记录、导出 TXT/HTML）

### Phase 3 — Pro 版 ✅
- [x] **📸 历史快照** — SQLite 存储磁盘扫描快照，含文件夹明细，可查看、删除和管理
- [x] **📊 快照对比** — 选择两个快照进行差异对比，高亮显示空间增减变化
- [x] **📈 趋势分析** — 基于历史快照绘制折线图，计算增长速率，预测C盘满载时间
- [x] **🔍 旧文件检测** — 扫描长期未访问文件、临时文件、空文件、崩溃转储文件
- [x] **🗑️ 安全删除** — 批量移动文件到回收站，在资源管理器中打开文件位置
- [x] **📄 CSV 导出** — 清理报告增加 CSV 格式导出，TXT/CSV/HTML 三种格式可选
- [x] **⏰ 定时扫描** — 通过 Windows 任务计划程序设置每天/每周/每月自动扫描
- [x] **🔔 空间通知** — 配置C盘空间阈值，触发 Windows Toast 通知
- [x] **⚙️ 设置页面** — 统一管理定时扫描、通知阈值、关于信息等设置

### Phase 4 — 未来计划
- [ ] Compact OS 系统压缩
- [ ] WSL 磁盘空间回收
- [ ] 系统托盘图标 + 实时空间显示
- [ ] Microsoft Store 正式发布

---

## ⚖️ 许可证 / License

本项目基于 [MIT License](LICENSE) 开源。

This project is open source under the [MIT License](LICENSE).

---

## 🤝 贡献 / Contributing

欢迎提交 Issue 和 Pull Request！

Issues and PRs are welcome!

