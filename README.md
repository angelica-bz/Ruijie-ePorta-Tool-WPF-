# Ruijie ePorta Tool (WPF)

锐捷 ePorta Web 认证自动登录/断开工具 —— WPF 图形界面版本

基于 [Red_lnn](https://github.com/Redlnn) 的 [Ruijie-ePorta-Tool](https://github.com/Redlnn/Ruijie-ePorta-Tool)（Python 版）重写为 WPF + VB.NET，提供更友好的图形界面操作体验。

> ⚠ 原项目已于 2023 年 10 月归档，本项目为其 WPF 重写版本，功能与原项目一致。

## 功能特性

- 图形化界面操作，支持状态监控、连接/断开、配置管理
- 自动检测网络连通状态，实时显示连接结果
- 支持断线自动重连（可配置间隔）
- 支持开机自启（Windows 启动目录快捷方式）
- 事件日志实时显示，同时写入本地日志文件
- 网络监控线程后台运行，不阻塞 UI
- 使用 PCL2 风格的自定义控件和动画系统

## 构建环境

- Visual Studio 2022
- .NET Framework 4.8
- VB.NET

## 构建方法

1. 克隆本仓库
2. 用 Visual Studio 2022 打开 `Ruijie-WPF.sln`
3. 生成解决方案（`Ctrl+Shift+B`）
4. 在 `Ruijie-WPF\bin\` 目录下找到 `Ruijie ePorta Tool.exe`

## 使用方法

1. 运行 `Ruijie ePorta Tool.exe`
2. 切换到「配置」页面，填写校园网服务器地址、登录参数等
3. 点击「保存配置」
4. 切换到「状态」页面，点击「连接」进行认证

配置文件 `config.yml` 会在首次运行时自动生成在 exe 所在目录。

## 权责声明

- 本程序仅供研究学习之用，无意对锐捷的认证机制做任何抵触性行为
- 本程序不可用于任何商业和不良用途，否则责任自负
- 本程序不保证在任何环境下能够通过，也不保证能按时按要求改进本程序
- 本程序不保证经过严格测试对机器无害，由于未知的使用环境或不当使用对计算机造成的损害，责任由使用者全部承担

## 致谢

- [Red_lnn](https://github.com/Redlnn) — 原版 Python 项目 [Ruijie-ePorta-Tool](https://github.com/Redlnn/Ruijie-ePorta-Tool)
- [龙腾猫跃](https://github.com/Meloong-Git) — [Plain Craft Launcher (PCL)](https://github.com/Meloong-Git/PCL) 的 UI 控件与动画系统

## 许可证

本项目基于原项目采用 [AGPL-3.0](LICENSE) 许可证。
