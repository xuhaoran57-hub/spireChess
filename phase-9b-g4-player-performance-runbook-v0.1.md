# Phase 9B G4 Player 构建与性能基线操作手册 v0.1

## 1. 目标与边界

本工具只为 G4 Windows x64 集成验收服务，运行真实 Build Settings 场景，不使用
Preview Scene 或 Editor 截图夹具冒充正式链路。

正式链路为：

`MainMenu -> Run -> Shop -> Run -> Battle -> Run -> MainMenu -> Continue`

当前正式音频尚未替换，因此报告中的音频内存和整体结论会明确标记为
`Placeholder` / `Provisional`。工具可以完成非音频基线，但不能据此关闭正式音频门禁。

## 2. 构建

```powershell
& .\tools\build_g4_windows.ps1 `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
```

默认输出：

- `sc/Builds/G4/<BuildId>/Windows-x64/SpireChess.exe`
- `sc/Builds/G4/<BuildId>/Windows-x64/g4-build-manifest.json`
- `sc/Logs/G4/G4-Windows-Build-<BuildId>.log`

构建固定使用以下正式场景：

1. `Boot`
2. `MainMenu`
3. `RunTest`
4. `ShopTest`
5. `BattleTest`

它是 `StandaloneWindows64` Development Build。构建期间临时使用独立的
`SpireChess.G4Validation / SpireChess G4 Acceptance` 产品身份，随后恢复项目设置；
因此 PlayerPrefs 和默认 persistentDataPath 不会与普通开发包共享。

需要排除增量缓存影响时可显式传 `-CleanBuild`。常规双机对比应统一是否使用该选项，
不能混合比较。

每次构建使用不可复用的 `BuildId` 和日志路径；输出目录非空或日志已存在时脚本会
直接拒绝执行。构建 Manifest 记录 Git SHA、工作树状态、EXE SHA-256，以及
`Windows-x64` 目录中每个文件的相对路径、字节数和 SHA-256。验收脚本在启动
Player 前重新校验全部文件，不能只校验壳 EXE。

Unity 构建脚本具有三层 watchdog：未产生日志的启动超时、日志/CPU 均无进展超时、
总运行超时。不要直接运行裸 `Unity.exe -version`、`-runTests -logFile -` 或无
watchdog 的 `-executeMethod` 命令；本机已经复现过它们在许可证/项目加载前无限驻留，
不能把进程仍存在视为构建或测试仍在正常推进。

## 3. 双分辨率正式链路

```powershell
& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '<冻结构建的 SpireChess.exe 绝对路径>' `
  -Quality High
```

不传 `-PlayerPath` 时，脚本只会选择 `sc/Builds/G4` 下最后生成且带 Manifest 的
构建；正式证据应显式传入本轮冻结构建的绝对路径。矩阵依次运行
`1920x1080` 和 `1920x1200`。

三种模式互斥：

- 无额外开关：Core 保存/继续与速度等价链；
- `-FrozenVisual`：固定 seed 78 的合法 4 店/4 战链，每分辨率 21 张截图；
- `-Stress`：同屏 10 张 Compact、嵌套亡语、群体成长、五轮
  Normal/Accelerated/Skip 对照和 30 秒稳定窗口。

每次运行都会创建不可复用的独立目录，
其中包含：

- `isolated-save/`：本轮唯一存档根；
- `performance/*.json`：环境、配置、汇总、场景加载和验收结果；
- `performance/*.csv`：逐帧可比较数据；
- `screenshots/*.png`：真实 Player checkpoint 截图；
- `player.log`：独立 Player 日志。

若目标 run 目录已非空，脚本会直接拒绝执行，不覆盖已有证据。
矩阵完成后还会在 Acceptance 根目录生成不可覆盖的
`g4-matrix-<MatrixId>-summary.json` 与 `g4-matrix-<MatrixId>-runs.csv`，
逐轮记录报告哈希、样本数、Frame P50/P95/P99、内存峰值、清理和 Catalog 门禁，
避免只保留人工抄写的平均数。

也可以只运行一个分辨率：

```powershell
& .\tools\run_g4_acceptance.ps1 -Resolution 1920x1080 -Quality High
```

截图采集必须让 Player 窗口实际显示。以隐藏窗口启动时，Windows Player 可能返回
尺寸正确但全黑的 PNG；验收脚本会抽样检查亮度范围、非暗像素比例和组内画面哈希，
黑图、空图或内容多样性不足均直接失败。

稳定的视觉候选先对 Core、Frozen、Stress 各跑 1 次带截图矩阵；性能重复采集使用
相同 Player、画质和 seed，关闭截图编码干扰并对 Core、Stress 各跑至少 5 次：

```powershell
& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '<冻结构建的 SpireChess.exe 绝对路径>' `
  -Quality High `
  -Repetitions 5 `
  -NoScreenshots

& .\tools\run_g4_acceptance_matrix.ps1 `
  -PlayerPath '<同一冻结构建的 SpireChess.exe 绝对路径>' `
  -Quality High `
  -Stress `
  -Repetitions 5 `
  -NoScreenshots
```

`-NoScreenshots` 只用于性能重复采集，不能代替双分辨率视觉证据。
它只关闭 PNG 捕获与编码，Player 窗口仍必须保持可见；隐藏或最小化窗口会改变
Windows 呈现/VSync 行为，产生不可作为 GPU 基线的亚毫秒假帧时间。

2026-07-26 当前有效开发机候选使用 Clean Development Build
`20260726-g4-f377497`，源提交为
`f377497d1f3e65486370d6b35d91811d1bff50bc`，`sourceTreeDirty=false`。
Player 为
`sc/Builds/G4/20260726-g4-f377497/Windows-x64/SpireChess.exe`，Build Manifest
SHA-256 为
`e09691e14ba931dddade86223527fa30e02dfcc6071a0e08be63bfea12023576`，
EXE SHA-256 为
`fa01ccdbaa5f74c777609235b99ba8988285b2bf0754445e85bba268b2e61eb7`。

五组正式矩阵：

| 模式 | Matrix ID | 轮数 |
| --- | --- | ---: |
| Core visual | `20260726-134409-DESKTOP-453378L` | 1×2 |
| Frozen visual | `20260726-134454-DESKTOP-453378L` | 1×2 |
| Stress visual | `20260726-134612-DESKTOP-453378L` | 1×2 |
| Core performance | `20260726-135013-DESKTOP-453378L` | 5×2 |
| Stress performance | `20260726-135305-DESKTOP-453378L` | 5×2 |

可提交索引与代表性原图位于
`ui-concepts/unity-validation/g4-formal-chain-v0.1/`。

## 4. 安全与真实性门禁

- `-g4Acceptance` / `-g4Perf` 必须同时提供绝对路径 `-g4SaveRoot`。缺失时
  `GameApp` 会在构造存档仓储前拒绝启动，因此不会读取真实玩家存档。
- Runner 使用可达地图节点进入 Shop 和 Battle，每个正式场景都真实加载。
- 首店会购买第一个非空随从并把它放入战斗槽，随后使用 2x 播放并触发 Skip。
- 战斗结果返回 Run 后，Runner 会处理可跳过奖励，并且只有
  `ContinueAfterBattle` 已进入 `MapSelection` 才执行保存返回。
- Continue 后会比较保存前后的完整 `RunStateFingerprint`。
- 每个截图 checkpoint 都审计当前活跃卡牌、立牌、奖励和遗珍的
  `ArtId / ArtworkResolution`。
- 22 个 G2 样板范围 ArtId 只允许 `Exact`；出现 `Fallback`、`Diagnostic` 或
  `Missing` 会使本轮失败。
- 非样板内容允许按冻结机制使用 `Fallback` / `Diagnostic`，但报告会逐项列出，
  不能作为正式美术命中或 Runtime Ready 证据。
- `run_g4_acceptance.ps1` 同样具有启动、无进展和总运行 watchdog，并在成功/失败
  路径都核对及回收本轮精确 PID；任何强制结束都属于失败，不得沿用旧结果文件。
- 正式运行拒绝 `sourceTreeDirty=true` 的构建；`-AllowDirtyProbe` 只允许单次本地
  诊断，矩阵永不接受 DirtyProbe。
- 单轮报告、证据与矩阵 schema 均为 v2；脚本会重验 runId、seed、完成状态、
  Build/Player 身份、JSON/CSV/Player.log 文件名与 SHA-256。
- Unity 的线程级 Error/Exception/Assert 同时写入结构化报告与
  `g4-runtime-failures.log`。报告通过后到 Player 退出前出现的错误仍会留下 marker，
  单轮和矩阵都会拒绝。
- Frozen 自动化与 21 图技术门禁通过仍不自动等于项目负责人完成 G4-V01/V02
  视觉签字。

## 5. 指标

JSON 汇总与 CSV 原始数据包括：

- 机器、CPU、GPU、内存、Unity / 应用版本和 build GUID；
- 实际分辨率、窗口模式、画质、VSync、AA、纹理质量和音频配置；
- 全局及逐场景 frame time、Main Thread、GC allocated/frame 分布；
- Total Used、GC Used、Texture、Audio memory 的峰值和结束值；
- 路由场景加载耗时、首帧耗时和首帧内存；
- checkpoint 的 ArtId 解析结果及 Exact/Fallback/Diagnostic/Missing 计数；
- 活跃 Presentation FX、非循环 AudioSource 和战斗动画清理状态。

某个 Unity 平台不提供的 ProfilerRecorder counter 会写入
`unavailableProfilerCounters`，值使用 `-1`，不会伪造为零。

## 6. 两机比较原则

两台机器必须使用同一个 Player、Git commit、画质、分辨率和 seed。先各自执行完整
双分辨率矩阵，再比较 JSON 的 P50/P95/P99、峰值内存和场景加载记录。

Development Build 本身有额外开销，所以本基线适合发现同类构建之间的回退，不等同于
最终 Release 包性能。Stress 已实现同屏 10 张 Compact 卡、嵌套亡语、连续召唤、
群体永久成长、1×/2×/Skip 五轮对照和 30 秒稳定窗口。

预热轮不要求使用专用矩阵或 `-NoScreenshots`，但必须在计量前完整运行、与计量使用
同一 Build/Player/Manifest SHA、画质、窗口、VSync、seed、分辨率，并明确不进入
计量 Summary。当前候选将 Core visual
`20260726-134409-DESKTOP-453378L` 和 Stress visual
`20260726-134612-DESKTOP-453378L` 各 1×2 轮透明指定为排除预热；它们分别先于
Core/Stress performance 5×2，截图编码只发生在被排除的预热轮。结合逐轮与 30 秒
稳定期趋势审查，当前 DEV-A G4-P02 通过。

G4-P03/P04 仍必须使用第二台不同配置机器，并由项目负责人冻结数值门槛。当前
Development Build 单机基线不是 Release 性能批准；正式音频接入前，音频内存、
Streaming 与混音均保持 `PROVISIONAL`。

第二机固定执行顺序、环境记录和证据回收要求见
`phase-9b-g4-second-machine-execution-v0.1.md`。本地执行包为
`sc/Builds/G4/20260726-g4-f377497/G4-SecondMachine-f377497.zip`，SHA-256 为
`83699817f774b8736cc3852eb17e8fb391c480bffc7f2829788c4d1c79fa796d`；
包已生成和校验不等于 G4-P03 已执行。
