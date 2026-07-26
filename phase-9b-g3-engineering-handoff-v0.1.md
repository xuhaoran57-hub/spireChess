# 阶段 9B G3 工程交接与门禁记录 v0.1

- 日期：2026-07-25
- 更新：2026-07-26（本地程序合成占位音频接入与显式生产状态门禁）
- Unity：2022.3.62f3c1
- 状态：屏幕、地图、通用 VFX 与音频工程已完成；67 个本地程序合成占位 Clip 可供运行时联调，但正式 AI Clip 尚未生成和验收，因此 G3 总门禁未关闭、音频不得标记为 `Runtime Ready`
- G2 前置：39 项活动生产 Sprite 均为 `Runtime Ready`
- AI 自制音频生产规范：`phase-9b-g3-ai-audio-production-spec-v0.1.md`
- 本地占位包：`phase-9b-g3-local-synth-placeholder-audio-v0.1.md`
- 独立审核：开发完成后新建只读审核智能体复核；一处旧测试计数已修正，最终
  P0 / P1 / P2 均无可操作遗留问题

## 1. 本轮完成范围

### 1.1 统一屏幕与地图表现

- 扩展 `PresentationTheme`，MainMenu、Shop、Choice、Run/Map、Battle、
  Confirm/System Menu 使用同一套旅团绘本颜色、纸面、边线和交互语言。
- 新增 `PresentationBackdropGraphic`，以运行时程序网格提供 MainMenu、Shop、
  Run 与 Battle 背景变体，不引入无来源位图。
- MainMenu 完成标题、存档摘要、主/危险按钮、确认弹窗和共享音频设置入口。
- Shop 完成顶部资源栏、商品/阵容/手牌区、详情栏、选择层及结构化反馈；验证状态
  改用 G2 配置真源，截图前强制校验所有预览 `ArtId` 精确命中。
- Run/Map 完成 7 类节点、`Locked/Reachable/Current/Resolved/Abandoned`
  5 类节点表现，以及 `Locked/Reachable/Resolved/Abandoned` 4 类连线表现。
- Choice 与 Run System Menu 完成同源皮肤；音频设置面板由 MainMenu/Run 共用。

### 1.2 通用反馈与战斗表现

- 新增容量受限的 `PresentationFxPool`；浮字/反馈复用对象，容量上限 32，支持立即
  清理，页面禁用与战斗跳过不会遗留 transient 实例。
- Shop 的刷新、购买、出售、上场、法术、三连、发现和升级结构化事件已映射到
  视觉反馈与唯一语义音频 Cue。
- Battle 的 `CombatStarted`、`RoundStarted`、`AttackStarted`、
  `DamageApplied`、`ShieldGained`、`ShieldLost`、`StatsChanged`、
  `UnitDied`、`UnitSummoned`、`CombatEnded` 十类事件已接入统一表现。
- 战斗支持 2× 表现速度、跳过、重置和胜负结果层；跳过/重置会清理协程、临时
  VFX 与立牌状态，不改领域最终状态。
- Battle 验证立牌均使用 Sprite Catalog 精确命中的 G2 插画，不再把缺图诊断图
  当作正常预览资源。

### 1.3 音频工程

已完成以下运行时资产与代码：

- `sc/Assets/Audio/Presentation/SpireChessAudio.mixer`
  - Master
  - Music
  - SFX
  - UI
  - `MasterVolumeDb/MusicVolumeDb/SfxVolumeDb/UiVolumeDb` 四个公开参数
- `sc/Assets/Resources/Presentation/PresentationAudioCatalog.asset`
  - 3 个 BGM Cue：`bgm_main_menu`、`bgm_run_shop`、`bgm_battle_normal`
  - 25 个 P0 事件 Cue
  - 共 28 个唯一语义 ID
- `AudioService`：常驻唯一服务、Mixer 音量、变体选择、并发、冷却和 voice 回收。
- `AudioPlaybackLimiter`：并发与冷却的纯逻辑门禁。
- `MusicDirector`：MainMenu、Run/Shop、Battle 三个上下文的切换与交叉淡化；
  同一上下文不重启。
- `PresentationAudioSettings`：Master/Music/SFX/UI 本机持久化，独立于单局存档。
- `AudioSettingsPanelView`：MainMenu 与 Run System Menu 共用的四路音量面板。
- `G3AudioAssetBuilder`：可重复生成 Mixer/Catalog，按 Cue ID 保留未来已接入 Clip，
  同时保留显式资产状态，不会在重建时抹掉或自动批准资源。
- `G3PlaceholderAudioAssetBuilder`：按冻结矩阵导入、配置并挂接 28 Cue / 67 Clip；
  只更新 `Pending/Placeholder` Cue，不会把已有 `ProductionApproved` Cue 降级。
- `tools/generate_g3_placeholder_audio.py`：以固定种子、振荡器、滤波噪声和数学包络
  生成 3 首完整长度循环 BGM 与 64 个 SFX 变体，不读取第三方采样。
- `tools/validate_g3_placeholder_audio.py`：以内置冻结契约独立校验占位目录文件集合、
  Manifest 字段、48 kHz / 24-bit、声道、采样数、峰值、RMS、循环接缝和逐文件
  SHA-256，不导入生成器规格。

Catalog 当前包含可播放但不能进入生产的本地占位音频：

- 3 个 BGM WAV + 25 Cue / 64 个 SFX 变体，共 67 个 48 kHz / 24-bit Clip；
- BGM 为 stereo；SFX 依语义为 mono 或 stereo；Manifest 的
  `productionReady` 固定为 `false`；
- `Commissioning` 校验通过，28 个可播放 Cue 各报告一条占位 warning；
- `ProductionStrict` 以退出码 1 按设计失败，精确报告 28 个
  `uses Placeholder audio and is not production-approved`；
- 严格门禁要求冻结的 28 Cue / 67 个不重复变体精确齐全、无 null、显式为
  `ProductionApproved`，并精确匹配正式路径、48 kHz/声道、Importer 策略及 SFX
  24-bit PCM 源格式；不得用静音、占位、临时合成音或第三方未知来源素材伪造完成。

## 2. 自动化门禁

2026-07-26 使用 Unity 2022.3.62f3c1 执行：

```powershell
& .\tools\run_unity_tests.ps1 `
  -Platform All `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' `
  -TimeoutSeconds 1800 `
  -ShutdownGraceSeconds 10
```

结果：

| 平台 | 结果 | 通过 | 失败 | 跳过 | 强制结束 |
| --- | --- | ---: | ---: | ---: | --- |
| EditMode | Passed | 346 / 346 | 0 | 0 | 否 |
| PlayMode | Passed | 25 / 25 | 0 | 0 | 否 |

覆盖重点：

- G2 样板配置与 Sprite Catalog 精确命中，非法 ID 只能进入诊断路径。
- Audio Catalog 28 个 ID、Bus、循环、数值、Mixer Group 与四个公开参数。
- 音频 `Commissioning/ProductionStrict` 双模式边界。
- 28 Cue / 67 Clip 精确路径、数量、非空与引用去重门禁；占位可播放但不能越过
  生产门禁。
- `Pending/Placeholder/ProductionApproved` 生命周期、重建逐引用保留、占位挂接
  不降级正式 Cue，以及“已批准但缺 Clip/重复 Clip/错格式”的反向门禁。
- 快速连续切换 BGM、交叉淡化中停止音乐时双声道收束；保存成功/失败分别映射
  `ui_confirm/ui_error`。
- MainMenu/Run 共享音频面板的文案、不可压缩最小高度和完整绑定。
- Shop/Battle 结构化事件到 VFX/音频 Cue 的唯一映射。
- VFX 池容量、回收与立即清理。
- 战斗 2×、跳过、重置、结果层与销毁安全。
- Run 节点/连线状态和遗珍精确图标。

权威结果文件：

- `sc/Logs/TestResults/EditMode-results.xml`
- `sc/Logs/TestResults/PlayMode-results.xml`

67 个占位 WAV 合计 100,097,828 bytes（约 100.10 MB），仓库当前未配置 Git LFS。
本轮未提交这些文件；提交前需明确接受普通 Git 体积增长，或另行决定受控生成/LFS
方案。

## 3. 双分辨率视觉证据

以下目录均由真实图形设备捕获，不使用 `-nographics`：

| 界面 | 证据目录 | 状态 |
| --- | --- | --- |
| MainMenu / Confirm / Audio Settings | `ui-concepts/unity-validation/g3-main-menu-v0.1/` | 1920×1080、1920×1200 人工复核通过 |
| Shop / Choice / Feedback | `ui-concepts/unity-validation/g3-shop-screen-v0.1/` | 两分辨率；所有预览卡面精确命中 |
| Battle / Detail / Rarity / Result | `ui-concepts/unity-validation/g3-battle-screen-v0.1/` | 两分辨率；无正常流程诊断图 |
| Run / Choice / System / Audio Settings | `ui-concepts/unity-validation/g3-run-screen-v0.1/` | 两分辨率；节点、遗珍和菜单可读 |

本轮视觉复核同时修复：

- 音频设置文本创建后未写入 `Text.text`。
- 主菜单大标题行高不足时 `VerticalWrapMode.Truncate` 生成 0 顶点。
- 设置面板标题/说明被纵向布局压缩到不足 7 px。
- Shop/Battle 验证模型的空 `ArtId` 误触发缺图诊断图。

独立审核后进一步修复：

- `StopMusic` 中断交叉淡化时同步清理非活动 Music Source，避免旧 BGM 跨场景叠音；
  PlayMode 覆盖中断停止与连续三次切歌。
- Run System Menu 只有保存成功才播放 `ui_confirm`，保存失败改播 `ui_error`。
- 项目 Catalog 测试按实际待生产 Clip 数判断 Commissioning/Production 状态，不会在
  正式 AI 音频接入后反向阻断全量测试；新增可由菜单或命令行调用的严格生产门禁。
- 音量拖动只更新运行时数值，关闭面板或恢复默认时一次性保存，避免每个拖动采样都
  调用 `PlayerPrefs.Save()`；偏好测试改为先快照、结束后恢复。
- 恢复早期构建误覆盖的旧 `pf-*` 历史截图并删除重复捕获；G3 证据只写入
  `g3-*-v0.1` 目录。

## 4. 尚未完成及责任边界

本地 `Placeholder` 已完成开发播放链路，但不替代以下正式 AI 音频生产责任：

1. 项目负责人选择允许目标商用范围的 AI 音频工具与账号方案，并保存生成当日的
   服务条款/许可快照、工具和模型版本、任务 ID、种子及完整提示词。
2. 按生产规范生成、筛选并后期处理 3 首 BGM、25 个 Cue / 64 个 SFX 变体，保留
   原始生成文件、母带、Runtime 文件、loop sample 点和逐文件 SHA-256。
3. 用正式 Clip 替换 Catalog 中的占位引用并保持 `Pending`；通过来源/许可、独立文件
   QA 与逐 Cue 听审后显式标记 `ProductionApproved`，再执行 `ProductionStrict`、
   无缝循环、快速切场、嵌套亡语峰值与最终整体听审；失败 Cue 立即退回
   `Production Candidate`。
4. 项目负责人确认音频可进入生产使用后，才可把音频标为 `Runtime Ready` 并关闭 G3。

属于下一门 G4 的事项：

- 正式 MainMenu → Run → Shop → Battle → Run 链路双分辨率复验。
- 存档恢复、跳过/2×、两机性能与音频内存基线。
- 至少 5 名外部试玩者的理解度与体验记录。

## 5. 正式 AI 音频完成后的最短接入流程

1. 按 AI 自制音频生产规范校验文件名、格式、变体数、loop 点、生成记录和许可证。
2. 将文件放入非 `Placeholder` 的 `sc/Assets/Audio/Presentation/Music/` 与对应
   `SFX/` 子目录。
3. 在 `PresentationAudioCatalog.asset` 按现有 Cue ID 替换占位 Clip，不改语义 ID；
   台账先保持 `Production Candidate`，Catalog 保持 `Pending`。完成来源/许可、独立
   文件 QA 与逐 Cue 人工听审后，才可把 Catalog 标记为 `ProductionApproved`。
4. 全部 Cue 批准后执行 Catalog `ProductionStrict` 门禁和 Unity 全量测试；门禁失败
   时立即把受影响 Cue 的台账退回 `Production Candidate`、Catalog 改回 `Pending`，
   修复后重新批准。命令行门禁为：

   ```powershell
   $strictArgs = @(
     '-batchmode', '-quit',
     '-projectPath', 'D:\code\spireChess\spireChess\sc',
     '-executeMethod',
     'SpireChess.Editor.G3AudioAssetBuilder.ValidateProductionStrictFromCommandLine',
     '-logFile',
     'D:\code\spireChess\spireChess\sc\Logs\G3-Audio-ProductionStrict.log'
   )
   $strictProcess = Start-Process `
     -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' `
     -ArgumentList $strictArgs `
     -WindowStyle Hidden `
     -PassThru
   if (-not $strictProcess.WaitForExit(300000)) {
     $strictProcess.Kill()
     throw 'G3 ProductionStrict timed out after 300 seconds.'
   }
   if ($strictProcess.ExitCode -ne 0) {
     throw "G3 ProductionStrict failed with exit code $($strictProcess.ExitCode)."
   }
   ```

   当前占位包存在时，该命令应列出 28 条 `Placeholder` 未获生产批准错误并以退出码
   1 结束。正式包只有在 28 Cue / 67 个变体精确齐全、无 null/重复引用，全部通过
   来源审核并标记为 `ProductionApproved`，且精确符合正式路径、48 kHz/声道、
   Importer 策略及 SFX 24-bit PCM 源格式后才可通过。
5. 执行三上下文循环/切场、连续刷新、嵌套召唤/亡语与四路音量人工听审。
6. 更新资产盘点与来源台账；由项目负责人签字后标记 `Runtime Ready`。
