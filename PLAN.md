# XboxDownload Android 移植计划

## 项目概览

目标：将 XboxDownload (Avalonia .NET 10) 转换为 Android 应用

## 核心差异

| 原版 (Windows) | Android 版 |
|----------------|------------|
| UDP:53 DNS 劫持 | VPN Service DNS 拦截 |
| HTTP/HTTPS 代理 (80/443) | VPN Service 流量拦截 |
| 修改注册表改 DNS | 用户手动设置设备 DNS |
| Windows 防火墙操作 | 不需要 |
| TcpConnectionListener | VpnService 处理 |

## Android 实现方案

### 1. VPN Service (核心)

```csharp
// AndroidVPNService.cs - 替代 DnsConnectionListener
public class XboxDownloadVpnService : VpnService
{
    private const int DNS_PORT = 53;
    private ParcelFileDescriptor? _vpnInterface;
    
    // DNS 拦截规则 (从原版移植)
    public static readonly ConcurrentDictionary<string, List<IPAddress>> Ipv4ServiceMap = new();
    public static readonly ConcurrentDictionary<string, List<IPAddress>> Ipv6ServiceMap = new();
    
    public override int onStartCommand(Intent? intent, int flags, int startId)
    {
        startTunnel();
        return START_STICKY;
    }
    
    private void startTunnel()
    {
        Builder builder = new Builder(this)
            .setSession("XboxDownload")
            .addAddress("10.0.0.2", 32)
            .addDnsServer("8.8.8.8")
            .addRoute("0.0.0.0", 0);
            
        _vpnInterface = builder.establish();
        
        // 启动 DNS 拦截线程
        new Thread(() => ListenDns()).Start();
    }
    
    private void ListenDns()
    {
        DatagramSocket dnsSocket = new DatagramSocket(DNS_PORT);
        while (true)
        {
            byte[] buffer = new byte[512];
            DatagramPacket packet = new DatagramPacket(buffer, buffer.length);
            dnsSocket.receive(packet);
            
            // 解析 DNS 查询并拦截
            var dnsQuery = ParseDnsQuery(buffer);
            var response = HandleDnsQuery(dnsQuery);
            
            if (response != null)
            {
                DatagramPacket responsePacket = new DatagramPacket(
                    response, response.length, packet.getAddress(), packet.getPort());
                dnsSocket.send(responsePacket);
            }
            else
            {
                // 转发到上游 DNS
                ForwardToUpstreamDns(buffer, packet.getAddress(), packet.getPort());
            }
        }
    }
}
```

### 2. 权限需求 (AndroidManifest.xml)

```xml
<uses-permission android:name="android.permission.INTERNET"/>
<uses-permission android:name="android.permission.FOREGROUND_SERVICE"/>
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_SPECIAL_USE"/>
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE"/>
```

### 3. 功能模块映射

| 原版模块 | Android 实现 | 文件 |
|----------|-------------|------|
| DnsConnectionListener | XboxDownloadVpnService | Services/VpnService.cs |
| TcpConnectionListener | ProxyConnectionHandler | Services/ProxyHandler.cs |
| SpeedTestViewModel | SpeedTestViewModel | ViewModels/ |
| HostViewModel | HostViewModel | ViewModels/ |
| StoreViewModel | StoreViewModel | ViewModels/ |
| MainWindow | MainActivity | Android/ |

### 4. UI 层 (Avalonia → Android)

由于 Avalonia 11 支持 Android，可以：

**方案 A**: 继续使用 Avalonia (推荐)
- 添加 `net10.0-android` TFM
- UI 代码几乎不用改

**方案 B**: 使用 Xamarin.Forms / .NET MAUI
- 重写 UI 层
- 更多工作量

## 移植优先级

### P0 (核心功能)
1. ✅ VPN Service DNS 拦截
2. ✅ 测速功能
3. ✅ Host 管理

### P1 (重要功能)
4. ⏳ 商店浏览 (StoreView)
5. ⏳ CDN 管理

### P2 (可选)
6. ❌ HTTP(S) 代理 (复杂，需要处理 TLS)
7. ❌ 修改设备 DNS (用户手动)

## 目录结构

```
XboxDownload.Android/
├── App/
│   ├── App.xaml
│   └── App.xaml.cs
├── Activities/
│   ├── MainActivity.cs
│   └── VpnActivity.cs
├── Services/
│   ├── XboxDownloadVpnService.cs
│   ├── DnsInterceptor.cs
│   └── ProxyConnectionHandler.cs
├── ViewModels/          # 从原版移植
│   ├── SpeedTestViewModel.cs
│   ├── HostViewModel.cs
│   └── StoreViewModel.cs
├── Models/              # 从原版移植
│   ├── Dns/
│   ├── Host/
│   └── SpeedTest/
└── Resources/
    ├── Akamai.txt
    └── CertDomain.txt
```

## 构建命令

```bash
# 添加 Android 支持
dotnet newAvalonia -o XboxDownload.Android -f net10.0-android

# 或在现有项目添加
<PropertyGroup>
  <TargetFramework>net10.0-android</TargetFramework>
  <RuntimeIdentifiers>android-arm;android-arm64;android-x64</RuntimeIdentifiers>
</PropertyGroup>

# 构建
dotnet build -c Release -r android-arm64
```

## 注意事项

1. **VPN 权限**: 首次使用需要用户授权 VPN 权限
2. **证书**: HTTPS 拦截需要用户安装根证书 (复杂)
3. **测速**: 功能可直接移植
4. **通知**: 需要前台 Service 通知保持运行
