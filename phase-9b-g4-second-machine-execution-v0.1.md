# Phase 9B G4 第二台机器执行说明 v0.1

- 日期：2026-07-26
- 候选提交：`f377497d1f3e65486370d6b35d91811d1bff50bc`
- Build ID：`20260726-g4-f377497`
- Unity：2022.3.62f3c1
- Player EXE SHA-256：
  `fa01ccdbaa5f74c777609235b99ba8988285b2bf0754445e85bba268b2e61eb7`
- Build Manifest SHA-256：
  `e09691e14ba931dddade86223527fa30e02dfcc6071a0e08be63bfea12023576`
- 用途：G4-P03 第二台不同配置 Windows x64 机器复验
- 状态：执行包已生成并完成逐条目读取校验；第二台机器尚未执行，G4-P03 仍为
  `未执行`
- 本地执行包：
  `sc/Builds/G4/20260726-g4-f377497/G4-SecondMachine-f377497.zip`
- 执行包大小：149,906,773 bytes
- 执行包 SHA-256：
  `83699817f774b8736cc3852eb17e8fb391c480bffc7f2829788c4d1c79fa796d`

## 1. 包内容

```text
G4-SecondMachine-f377497/
├── Windows-x64/
│   ├── SpireChess.exe
│   ├── g4-build-manifest.json
│   └── ...
├── tools/
│   ├── run_g4_acceptance.ps1
│   └── run_g4_acceptance_matrix.ps1
└── README.md
```

不需要安装 Unity。不要替换 `Windows-x64/` 内任何文件；脚本启动前会按 Build
Manifest 重新校验 247 个构建文件的长度和 SHA-256。

归档共 274 个 ZIP 条目，其中 251 个为文件；已逐条打开并读至末尾，且确认包含
Player、Build Manifest、两支执行脚本和 README。执行包属于本地交付物，不纳入
Git；上述路径和哈希用于交接时确认同一归档。

## 2. 运行环境记录

执行前记录：

```powershell
Get-ComputerInfo |
  Select-Object WindowsProductName, WindowsVersion, OsBuildNumber,
    CsProcessors, CsTotalPhysicalMemory

Get-CimInstance Win32_VideoController |
  Select-Object Name, DriverVersion, AdapterRAM

powercfg /getactivescheme
```

如果 CIM 查询被系统策略拒绝，可用 `systeminfo` 与显卡厂商工具替代。另行记录：

- 显示器刷新率；
- 是否接电、电源模式；
- 测试期间可见的后台高负载程序；
- 执行人、日期和机器代号。

关闭 Unity Editor、其他 SpireChess Player、游戏、录屏和下载任务。所有矩阵串行
执行；Player 窗口必须保持可见，不得最小化或切为隐藏桌面。

## 3. 执行顺序

在解压后的包根目录打开 PowerShell。先允许本进程执行本地脚本：

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

### 3.1 完整预热兼可视证据

以下三组各运行两个分辨率。Core 与 Stress 的性能值不计入后续性能汇总，它们分别
作为对应性能矩阵的完整预热轮；Frozen 是额外的全链路视觉证据。

```powershell
& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '.\Windows-x64\SpireChess.exe' `
  -OutputDirectory '.\Evidence\core-visual' `
  -Repetitions 1 `
  -TimeoutSeconds 300

& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '.\Windows-x64\SpireChess.exe' `
  -OutputDirectory '.\Evidence\frozen-visual' `
  -FrozenVisual `
  -Repetitions 1 `
  -TimeoutSeconds 300

& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '.\Windows-x64\SpireChess.exe' `
  -OutputDirectory '.\Evidence\stress-visual' `
  -Stress `
  -Repetitions 1 `
  -TimeoutSeconds 300
```

### 3.2 Core 与 Stress 计量

预热完成后立即运行，不重启机器、不改变电源/显示/后台负载设置：

```powershell
& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '.\Windows-x64\SpireChess.exe' `
  -OutputDirectory '.\Evidence\core-performance' `
  -NoScreenshots `
  -Repetitions 5 `
  -TimeoutSeconds 300

& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '.\Windows-x64\SpireChess.exe' `
  -OutputDirectory '.\Evidence\stress-performance' `
  -Stress `
  -NoScreenshots `
  -Repetitions 5 `
  -TimeoutSeconds 300
```

Stress 5×2 预计约 11 分钟。脚本每 10 秒输出心跳，并有启动、无进展和总运行
watchdog；不要因为单个 Player 约 64 秒而手动结束。任何强制结束、非零退出码、
runtime failure marker 或 hash mismatch 都属于失败。

## 4. 回收证据

完成后保留整个 `Evidence/`，并计算包：

```powershell
Compress-Archive `
  -Path '.\Evidence\*' `
  -DestinationPath '.\G4-P03-Evidence-<机器代号>.zip'

Get-FileHash `
  '.\G4-P03-Evidence-<机器代号>.zip' `
  -Algorithm SHA256
```

回传：

- 环境记录；
- 完整 `Evidence/` 压缩包及 SHA-256；
- 五组 Matrix Summary JSON / runs CSV；
- 是否出现窗口遮挡、卡死、黑图、明显顿挫或人工终止；
- Stress 十卡 Shop 初始化尖峰的体感描述。

## 5. 判定边界

- 第二机 26 个运行全部 `FormalCandidate` / `AcceptancePassed`，且预热、五轮、
  错误日志、清理和内存趋势有效，才能通过 G4-P03。
- DEV-A 与第二机数据齐全后，项目负责人才能冻结 G4-P04 的加载、帧时间和内存
  门槛。
- Development Build 数据不能描述为 Release 性能。
- 当前音频为 Placeholder；音频内存、Streaming、混音和听感一律
  `PROVISIONAL`。
- 本步骤不替代负责人双分辨率视觉签字或至少 5 名外部试玩。
