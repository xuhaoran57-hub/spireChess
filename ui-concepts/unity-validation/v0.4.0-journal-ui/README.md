# v0.4.0 日记式 UI 证据目录

状态：`待生成`。本目录只接收 v0.4.0 的新证据，绝不覆盖
`g3-*`、`g4-formal-chain-v0.1` 或 v0.3.3/v0.3.4 的冻结结果。

## 目录合同

```text
v0.4.0-journal-ui/
├─ README.md
├─ evidence-manifest.template.json
├─ editor-preview/                 # Prefab Builder 的双分辨率预览，非 Player 放行
└─ <candidate-id>/                 # 一次 Clean Player 验收的不可变证据包
   ├─ acceptance-summary.md
   ├─ manifest.json
   ├─ tests/
   ├─ player-1920x1080/
   └─ player-1920x1200/
```

`<candidate-id>` 使用已提交的短 SHA；诊断性脏工作树只能放在
`editor-preview/` 或临时目录，不能伪装为正式 Player 证据。

## 最低截图集合

每个 Player 分辨率都必须包含以下稳定文件名：

1. `01-journal-cover-<resolution>.png`
2. `02-journal-contents-<resolution>.png`
3. `03-journal-hero-select-<resolution>.png`
4. `04-map-chapter-1-<resolution>.png`
5. `05-chapter-complete-<resolution>.png`
6. `06-ending-<resolution>.png`
7. `07-continue-restored-<resolution>.png`

其中封面/目录/角色页/章节完成/结局必须由真实 Player UI 驱动；不得用静态合成图替代。
临时中性占位图可以作为本切片的布局证据，但不能作为正式美术签字或资产包关闭证据。

## 自动化入口

- `SpireChess/UI/Build Main Menu`：重建主菜单 Prefab/场景。
- `Spire Chess/UI/Rebuild and Capture Run UI`：重建 Run Prefab，并在
  `editor-preview/` 输出章节完成与结局的 1920×1080、1920×1200 预览。
- `tools/run_unity_tests.ps1 -Platform All`：EditMode/PlayMode 门禁。
- `tools/run_g4_acceptance.ps1 -JournalUi`：v0.4.0 专用 Clean Windows x64
  Player 链。它捕获本目录规定的七张截图：封面、目录、角色选择和地图由真实 UI
  点击进入；章节边界由 `G4PlayerAcceptanceRunner` 的既有 `RunSession` 确定性夹具
  快速准备，遗珍选择、章节翻页、结局返回和继续游戏仍全部点击正式 UI。证据摘要须
  记录 `journal-fixture-v1`，不得将该夹具描述为完整手动通关。
- `tools/run_g4_acceptance.ps1`：既有 Clean Windows x64 核心链，继续验证原有
  菜单、地图、商店、战斗与存档恢复，不替代上面的 v0.4.0 日记证据链。

正式包必须记录：候选提交、工作树状态、Unity 版本、配置哈希、命令行、测试 XML/日志、
Player 构建 Manifest、截图 SHA-256、人工复核结论和失败项。字段结构见
[evidence-manifest.template.json](evidence-manifest.template.json)。
