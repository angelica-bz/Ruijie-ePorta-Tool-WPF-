# 更新日志

## v1.0.3 (2026-06-01)

### 修复
- 修复 `GetUuid()` 延迟初始化锁对象导致的 TOCTOU 竞态条件
- 修复配置页保存后未同步刷新 `SharedHeaders`，导致监控线程使用过期请求头的问题
- 修复 `AniGroups` 字典在多线程间无锁访问可能导致的 `InvalidOperationException` 崩溃

### 变更
- JSON 解析器：用 `System.Web.Script.Serialization.JavaScriptSerializer` 替换手写递归下降解析器（166 行 → 30 行），支持 Unicode 转义和科学计数法
- 配置管理：提取 `ReloadRuntimeConfig()` 方法统一刷新 `SharedCfg`/`SharedHeaders`，消除"保存了配置但只刷新了一半"的隐患
- 配置管理：删除 `ConvertToStrings` 往返转换，配置值保留原始类型（`Boolean`/`Integer`/`String`）
- 配置管理：删除未使用的 `check_school_network` 和 `disconnect_network` 配置键
- 颜色系统：XAML 为唯一颜色来源，VB 代码在 UI 线程启动时通过 `CType` 缓存到 `MyColor`，消除双维护
- 动画系统：`AniTimer` 改为快照遍历 + 延迟删除，明确 UI 线程约束；`AniStart`/`AniStop` 使用 `ConcurrentDictionary` 安全操作
- 动画系统：提取 `AniDisposeCore` + `RemoveFromParent`（兼容 `Panel`/`ContentControl`/`Decorator`），三个控件的 `AniDispose` 改为薄包装
- 删除未使用的控件：`MyListItem`、`MyLoading`、`MyIconTextButton`（约 700 行死代码）
- 测试运行时禁用 `DailyWrite`，防止测试消息污染生产日志文件

### 技术细节
- `.vbproj` 新增 `System.Web.Extensions` 引用
- `App.xaml` 清理 16 个未使用的 `ColorObject*`/`ColorObjectGray*` 资源定义
- `ModAnimation.vb` 新增 `Imports System.Collections.Concurrent` 和 `Imports System.Linq`

## v1.0.2 (2026-05-21)

### 修复
- 修复开机启动（`--background`）模式下，前端UI校园网状态始终显示"未知"的问题
- 修复开机启动模式下，前端日志面板无任何日志记录的问题（日志文件写入正常）

### 技术细节
- `NetworkMonitor` 新增 `GetSnapshot()` 方法，在锁内原子返回（连接状态、校园网状态、最近50条日志缓冲），用于UI延迟挂载时同步状态
- `NetworkMonitor` 内部新增 `_SchoolReachable` 字段追踪校园网可达性，`_RecentLogs` 队列缓冲最近50条日志消息
- `PageStatus.StartMonitor()` 改为快照驱动：在 `AddHandler` 之前通过 `GetSnapshot()` 一次性回放状态和日志，消除事件丢失窗口期
- 增加 `_MonitorHandlersAttached` 守卫，防止重复调用 `StartMonitor()` 时重复回放日志

## v1.0.0 (2026-05-02)

### 新增
- WPF 重写版本，替换原 Python 项目
- 图形化界面（状态页 + 配置页）
- 自定义控件和动画系统（PCL2 风格）
- 断线自动重连，可配置重连间隔
- Windows 开机自启支持
- 实时事件日志 + 本地日志文件写入
- 后台网络监控线程，不阻塞 UI
