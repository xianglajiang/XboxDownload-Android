# XboxDownload Android 移植项目

将 Windows XboxDownload 应用转换为 Android 应用。

## 项目目录结构

```
XboxDownload.Android/
├── XboxDownload.Android.csproj          # 项目文件
├── MainActivity.cs                       # 主界面 (含底部导航)
├── Models/
│   └── Models.cs                         # 数据模型
├── ViewModels/
│   ├── SpeedTestViewModel.cs             # 测速功能
│   ├── HostViewModel.cs                   # Host 管理
│   └── StoreViewModel.cs                  # 商店浏览
├── Services/
│   └── XboxDownloadVpnService.cs         # 核心 VPN DNS 拦截服务
├── Fragments/
│   ├── HomeFragment.cs                    # 首页 (VPN控制)
│   ├── SpeedTestFragment.cs               # 测速页面
│   ├── HostFragment.cs                    # Host管理页面
│   ├── StoreFragment.cs                   # 商店页面
│   └── SettingsFragment.cs                # 设置页面
├── Resources/
│   ├── layout/
│   │   ├── MainActivity.xml              # 主界面布局
│   │   ├── FragmentHome.xml              # 首页布局
│   │   ├── FragmentSpeedTest.xml         # 测速布局
│   │   ├── FragmentHost.xml              # Host布局
│   │   ├── FragmentStore.xml             # 商店布局
│   │   └── FragmentSettings.xml          # 设置布局
│   └── menu/
│       └── bottom_navigation_menu.xml     # 底部导航菜单
└── Properties/
    └── AndroidManifest.xml               # 权限配置
```

## 功能对照表

| 原版功能 | Android 实现 | 状态 |
|----------|-------------|------|
| DNS 劫持 (UDP:53) | VpnService DNS 拦截 | ✅ 完成 |
| Xbox CDN 加速 | 域名映射到国内 CDN | ✅ 完成 |
| CDN 测速 | SpeedTestViewModel | ✅ 完成 |
| Host 管理 | HostViewModel | ✅ 完成 |
| 商店浏览 | StoreViewModel | ✅ 完成 |
| UI 框架 | Fragment + BottomNav | ✅ 完成 |
| HTTP(S) 代理 | VpnService 流量拦截 | ⏳ 待开发 |

## UI 界面

1. **首页** - VPN 开关、状态显示
2. **测速** - CDN 测速、IP 选择
3. **Host** - 自定义 Host 管理
4. **商店** - Xbox Game Pass、精选游戏
5. **设置** - DNS 配置、关于

## 工作原理

```
用户设备
    │
    ▼
┌─────────────────────┐
│   VPN Service       │  ← 创建本地 VPN
│  (本应用)           │
└─────────────────────┘
    │
    ▼ DNS 查询 (UDP:53)
┌─────────────────────┐
│   DNS 拦截器        │  ← 解析域名
│  DnsConnection     │    检查规则
└─────────────────────┘
    │
    ├─ 匹配规则 ──→ 返回国内 CDN IP
    │
    └─ 未匹配 ──→ 转发到上游 DNS
```

## 环境要求

- .NET 10.0 SDK
- Android SDK (API 24+)
- Visual Studio 2022 / Rider / VS Code

## 构建步骤

```bash
# 1. 进入项目目录
cd XboxDownload.Android

# 2. 还原依赖
dotnet restore

# 3. 构建 Debug 版本
dotnet build

# 4. 构建 Release ARM64
dotnet build -c Release -r android-arm64

# 5. 构建 APK
dotnet publish -c Release -r android-arm64 -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore= keystore.jks \
    -p:AndroidSigningKeyAlias=keyalias \
    -p:AndroidSigningKeyPass=password \
    -p:AndroidSigningStorePass=password
```

## 安装测试

```bash
# 通过 ADB 安装
adb install bin/Release/net10.0-android/android-arm64/XboxDownload.Android.apk

# 或直接在 Android Studio 运行
```

## 已知限制

1. **VPN 权限** - 首次使用需要用户授权 VPN 权限
2. **HTTPS 拦截** - 需要用户安装根证书（可选功能）
3. **后台运行** - 需要保持前台通知
4. **网络限制** - 部分网络环境下 VPN 可能无法建立

## 与原版功能对比

| 功能 | Windows 原版 | Android 版 |
|------|-------------|-----------|
| DNS 劫持 | ✅ | ✅ (VPN) |
| HTTP 代理 | ✅ | ⏳ |
| 系统 DNS 修改 | ✅ | ❌ |
| 防火墙配置 | ✅ | ❌ |
| 测速 | ✅ | ✅ |
| Host 管理 | ✅ | ✅ |
| 商店浏览 | ✅ | ✅ |
| 游戏下载 | ✅ (HTTP代理) | ❌ |

## 技术栈

- **.NET 10** + **Xamarin.Android**
- **AndroidX** + **Material Design**
- **VPN Service** 实现 DNS 拦截

## 许可证

MIT License - 与原版项目一致
