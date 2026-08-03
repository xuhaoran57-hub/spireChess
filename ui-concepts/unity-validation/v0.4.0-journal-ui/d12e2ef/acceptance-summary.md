# v0.4.0 Clean Player 验收摘要：d12e2ef

状态：**Clean Player 验收链已关闭**。候选
`d12e2ef0c528a697a4b57fa73f101dfe3059c6c5` 在干净工作树上完成全量 Unity
测试、Clean Windows x64 构建、既有核心链双分辨率和 Journal UI 双分辨率验收。

本结论只关闭 `V40-QA-01`、`V40-QA-02`、`V40-QA-03` 及对应 Clean Player
工作板项；不代替 `V40-QA-04` 的章节进度 S0，也不关闭 `AC-12`、外部资产许可、
第二机性能或外部试玩签字。

## 候选身份

| 项目 | 结果 |
| --- | --- |
| 分支 | `codex/v040-clean-player` |
| 候选提交 | `d12e2ef0c528a697a4b57fa73f101dfe3059c6c5` |
| 构建 ID | `d12e2ef-clean-player` |
| 构建工作树 | `sourceTreeDirty=false`、`cleanBuild=true` |
| Unity | `2022.3.62f3c1` |
| 运行机器 | `DESKTOP-453378L`，Windows x64 |
| 配置哈希 | `9ed1dbb542cffb31dab62aa339767bc4497ec7c858028cf36e3802c283646c7f` |
| Player SHA-256 | `fa01ccdbaa5f74c777609235b99ba8988285b2bf0754445e85bba268b2e61eb7` |
| 构建 Manifest SHA-256 | `224fbeb7cba021e4c8e4edb00b9307bcbdadc07e2b8731b14c87bee5e273e9fd` |

## 自动化与构建

| 门禁 | 结果 | 时长 |
| --- | --- | --- |
| EditMode | 458 / 458 通过；0 failed、0 skipped、0 inconclusive | 70.821 秒 |
| PlayMode | 30 / 30 通过；0 failed、0 skipped、0 inconclusive | 34.911 秒 |
| Windows x64 Development Player | `Succeeded`；Clean Build；未强制终止 | 19.176 秒 |

测试 XML、日志、构建日志和完整构建 Manifest 均已归档在本目录。新增的遗珍存档回归
覆盖派生战斗开始效果在 `PendingBattle`、`LastBattleContext` 和
`LastBattleResult` 三个边界的捕获与恢复。

## Player 结果

| 链路 | 1920×1080 | 1920×1200 | 运行时错误 |
| --- | --- | --- | --- |
| 既有核心链 | `AcceptancePassed`，16 张截图 | `AcceptancePassed`，16 张截图 | 0 |
| Journal UI | `AcceptancePassed`，7 张截图 | `AcceptancePassed`，7 张截图 | 0 |

核心链完成菜单、地图、商店、战斗、系统菜单、保存返回和继续恢复。Journal 链完成：

`Cover → CoverSkipButton → Contents → NewGame → HeroSelection → ConfirmHero →
Map → ChapterComplete → Ending → ReturnToMainMenu → Continue → Ending restored`

Journal 使用 `journal-fixture-v1` 确定性准备章节边界；可见选择、翻页动作、遗珍选择、
返回目录和继续游戏均由正式 Player UI 点击完成。这不是一次完整手动通关。

两种分辨率的第 6、7 张截图哈希分别一致，符合预期：继续游戏恢复的是同一
`RunWon` 结局页面；流程成功由 Player 日志、保存文件、恢复检查点和
`AcceptancePassed` 报告共同证明。

## 本轮关闭前修复

- 誓刃甲胄的护盾丢失触发保留真实攻击者上下文，并限制为自身破盾。
- 英雄确认和结局继续动作在执行前等待按钮通过真实 UI 射线命中，消除页面切换后的
  点击竞态。
- 遗珍生成的战斗开始效果改为由已持久化的 `BattleRuleModifiers` 恢复时重建，不再把
  运行时临时 `EffectConfig` 错当作内容配置引用；结局返回目录保存已实测通过。

## 视觉复核

已逐图检查核心链 32 张和 Journal 链 14 张原始截图。1920×1080、1920×1200 下未发现：

- 页面、主要按钮、遮罩或文本裁切；
- 控件互相遮挡或不可点击；
- 贴图缺失、空白画面或错误占位；
- 章节完成、结局与继续恢复状态不一致。

这是 Codex 工程审图结论；正式资产来源/许可、第二机性能和外部试玩仍按权威矩阵保持
开放。

## 证据入口

- 总清单：[manifest.json](manifest.json)
- 测试：[tests](tests)
- Clean 构建：[build](build)
- 核心链：[core-matrix](core-matrix)
- Journal 1920×1080：[player-1920x1080](player-1920x1080)
- Journal 1920×1200：[player-1920x1200](player-1920x1200)

失败项：无。候选复验日期：2026-08-03。
