# 更新日志

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
