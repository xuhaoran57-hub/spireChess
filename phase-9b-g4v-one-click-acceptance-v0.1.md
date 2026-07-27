# G4-V 一键视觉验收 v0.1

- 日期：2026-07-27
- Unity：`sc/ProjectSettings/ProjectVersion.txt` 指定版本
- 入口：`tools/run_g4v_visual_acceptance.ps1`
- 目标：用同一份新构建的 Windows Player，自动完成全量测试和
  1920×1080 / 1920×1200 五画面采集。

## 一键运行

正式候选必须来自干净工作树：

```powershell
.\tools\run_g4v_visual_acceptance.ps1 `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"
```

开发中可用脏构建验证链路，但证据只会标记为 `DirtyProbe`，不能用于
G4-V 签字：

```powershell
.\tools\run_g4v_visual_acceptance.ps1 `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe" `
  -AllowDirtyProbe
```

可选参数：

- `-BuildId <id>`：指定唯一构建和证据 ID。
- `-OutputDirectory <path>`：指定证据根目录。
- `-Quality High`：指定 Player 质量档。
- `-TestTimeoutSeconds`、`-BuildTimeoutSeconds`、
  `-PlayerTimeoutSeconds`：调整三段 watchdog 总时限。

脚本拒绝复用非空构建目录或证据目录。

## 自动步骤

1. 调用 `run_unity_tests.ps1 -Platform All`，要求 EditMode 和
   PlayMode 全部通过。
2. 调用 `build_g4_windows.ps1 -CleanBuild`，生成新的 Windows x64
   Development Player 和逐文件哈希清单。
3. 对同一 Player 运行两次 `run_g4_acceptance.ps1 -VisualSlice
   -Seed 10`。
4. 精确校验每个分辨率的五张 PNG、尺寸、非黑屏、画面差异、
   Player/Build 身份、运行时错误、样板 Catalog Exact 和表现清理。
5. 生成聚合清单 `g4v-visual-acceptance-manifest.json`，记录测试、
   构建、两轮运行和 10 张 PNG 的 SHA-256。

## 五画面契约

每个分辨率必须且只能生成：

1. `01-main-menu-<resolution>.png`
2. `02-floor-map-<resolution>.png`
3. `03-shop-environment-<resolution>.png`
4. `04-battle-background-<resolution>.png`
5. `05-event-tranquil-grove-<resolution>.png`

主菜单、地图、商店和战斗均通过正式 UI 按钮/地图节点进入。为避免为了
一张事件图重放整条长单局，事件画面在新的 seed=10 单局中只把
`f1_event` 状态设为 `Reachable`；之后仍调用真实
`RunSession.EnterNode`。Player 会继续断言真实事件选择结果为
`tranquil_grove`、配置插画为 `event_tranquil_grove`、正式选择项数量
一致且插画对象实际可见。这个夹具只用于 G4-V 静态视觉证据，不替代现有
合法完整链路验收。

## 输出

默认根目录：

```text
sc/Logs/G4/G4V/<BuildId>/
├─ tests/
│  ├─ EditMode-results.xml
│  └─ PlayMode-results.xml
├─ runs/
│  ├─ <BuildId>-g4v-1920x1080/
│  └─ <BuildId>-g4v-1920x1200/
└─ g4v-visual-acceptance-manifest.json
```

Player 位于：

```text
sc/Builds/G4/<BuildId>/Windows-x64/SpireChess.exe
```

脚本通过只说明自动门禁和证据完整；G4-V 仍需负责人逐图完成视觉与生产
许可签字，签字前五项新增美术保持 `工程样板`。
