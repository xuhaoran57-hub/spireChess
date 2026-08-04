# v0.4.0 当前候选工程与章节 S0 验收摘要：0eaa2e3

状态：**当前候选自动化与章节进度 S0 证据已关闭**。

候选 `0eaa2e371694598c3ab20686aea497731d98ab89` 在干净工作树上通过全量
EditMode/PlayMode，并以 `requireCleanSource=true`、`strictAcceptance=true`
生成 19,800 场章节进度 S0。该证据关闭 `V40-QA-01`、`V40-QA-04`，并与
`d12e2ef` 已冻结的 Clean Player 与双分辨率证据共同补齐 `AC-12` 的工程证据链；
它同时补齐 `AC-08` 的干净 S0 部分，但不替代尚缺的第二、三章正式地图页截图。

本结论不关闭三角色真实单局 `V40-BAL-01`～`V40-BAL-03`，也不替代正式资产
来源/许可、第二机性能、外部视觉签字或外部试玩。

## 候选身份

| 项目 | 结果 |
| --- | --- |
| 分支 | `codex/v040-clean-player` |
| 候选提交 | `0eaa2e371694598c3ab20686aea497731d98ab89` |
| Unity | `2022.3.62f3c1` |
| 内容版本 | `5.6.0` |
| 规则版本 | `8B.1` |
| 配置哈希 | `9ed1dbb542cffb31dab62aa339767bc4497ec7c858028cf36e3802c283646c7f` |
| 进度 fixture | `0.4.0`；SHA-256 `471bf06e2d98176911001cafc39c745acb2ce04687a5b6105074e7120338ac36` |
| 来源 fixture | `0.3.0`；SHA-256 `3b94e5e9212ffe15aaad4faa8990b91a7bc7cf190acf345997c1830cd1fb6e73` |
| 种子集 | `S0_CHAPTER_PROGRESS_FIXED`，`1000–1099` |
| 取证工作树 | `sourceTreeDirty=false` |

## 全量 Unity

| 门禁 | 结果 | 时长 |
| --- | --- | --- |
| EditMode | 459/459 通过；0 failed/skipped/inconclusive | 71.529 秒 |
| PlayMode | 30/30 通过；0 failed/skipped/inconclusive | 35.289 秒 |

测试 XML 与完整日志位于 [`tests/`](tests/)。
归档日志仅将 Unity 自动打印的部分遮罩许可证序列号统一替换为 `<redacted>`；
测试内容、结果 XML 与其他日志行保持不变。

| 文件 | SHA-256 |
| --- | --- |
| `EditMode-results.xml` | `d15c59534cf9d8123768306f2bc772156e6a3d63d27a394ace6bd22319d3a629` |
| `EditMode.log` | `9cad55c406ecc9fb965f13e564686cc6540644f13a4e6314171fdcd2967e4b38` |
| `PlayMode-results.xml` | `4b5095dee896827f3c6651f4b082a042f446841c053518a4ee3389d37d1c8529` |
| `PlayMode.log` | `ad8c8e9ce32ba5a80242a8b9a2302ddfc2d3be1ab572568428ab501ef80534eb` |

## Strict 章节进度 S0

| 指标 | 结果 |
| --- | --- |
| 正式遭遇 | 33（地图 30、事件 3） |
| 固定构筑 / 发育档 | 6 / `C2`、`C4`、`C5` |
| 场景 / 战斗 | 198 / 19,800 |
| 异常 / 效果上限 / 回合上限 | 0 / 0 / 0 |
| P0 / P1 / P2 | 0 / 0 / 1 |
| strict 结论 | `acceptancePassed=true`，`gateFailures=[]` |

唯一 P2 为 `f1_opening_encounter` 的 `C2` 聚合胜率 100%。它是既有的 F1
教学首战饱和项，不涉及异常、上限命中或构筑硬克制，按权威矩阵既定边界保留并接受。

- S0 元数据：[`chapter-progress-s0/metadata.json`](chapter-progress-s0/metadata.json)
- 结果报告：[`chapter-progress-s0/chapter_encounter_report.md`](chapter-progress-s0/chapter_encounter_report.md)
- 异常清单：[`chapter-progress-s0/chapter_encounter_anomalies.csv`](chapter-progress-s0/chapter_encounter_anomalies.csv)
- Unity 日志：[`chapter-progress-s0/unity.log`](chapter-progress-s0/unity.log)
- 输出集合 SHA-256：`a854578eeaca51acd9613dac2f4a1d390e6f89e4e2848ef854698eb48a50c795`
- `metadata.json` SHA-256：`38564b3c121f5403d753b17f2f9e33137445798de1b5ce8d96826046bbf6f46a`
- `unity.log` SHA-256：`3292b9ea75fd1853e5a052cc598a3e2f9b448ae6ce75a93c42ca37fe41e2003b`

元数据中的六个输出文件长度与 SHA-256 已逐项复算，规范化输出集合哈希也与
`outputSetSha256` 一致。

## 本轮回归与校准

`18b0e6e` 的首次 strict 复跑正确暴露 4 个 `BUILD_HARD_COUNTER` P1。原因是
`43973ca` 已修正护盾丢失事件的实际攻击者上下文，而旧的 `517d4fc+dirty` S0
运行在该修复之前，B02 结果不能继续复用。

本候选不回退正确的战斗规则，只校准 B02 进度夹具：

- F2 C4 五个槽位各增加 1 攻，`cinder_armor_arbiter` 额外增加 1 生命；
- F2 C5 五个槽位各增加 1 攻；
- EditMode 锁定 C4 总面板 `51/42`、C5 总面板 `68/50`。

校准后四个原 P1 清零，且未在精英、事件伏击、安全路线或其他章节产生新 P0/P1。

## 复现命令

```powershell
.\tools\run_unity_tests.ps1 `
  -Platform All `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' `
  -ResultsDirectory '.\sc\Logs\TestResults\0eaa2e3-release-candidate' `
  -TimeoutSeconds 1800
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'D:\code\spireChess\spireChess\sc' `
  -executeMethod SpireChess.Editor.ChapterEncounterSamplingCommand.RunFromCommandLine `
  -chapterSampleFixtureMode progress `
  -chapterSampleSeedSet S0_CHAPTER_PROGRESS_FIXED `
  -chapterSampleFirstSeed 1000 `
  -chapterSampleSeedCount 100 `
  -chapterSampleRequireCleanSource true `
  -chapterSampleStrictAcceptance true `
  -chapterSampleOutput 'balance-results\v0.4.0\release-candidate-0eaa2e3\chapter-progress-s0'
```

失败项：无。复验日期：2026-08-04。
