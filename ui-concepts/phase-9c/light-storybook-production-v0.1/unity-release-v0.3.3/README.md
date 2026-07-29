# 明亮主题 v0.3.3 Unity 最终放行

- 日期：2026-07-29
- Unity：2022.3.62f3c1，Windows / DX11
- 项目：`sc/`
- 结论：`UNITY_FINAL_RELEASE`
- 人工视觉验收：通过

本目录记录冻结包
`../freeze-v0.3.3/`
在 Unity 中的最终构建、自动化测试与人工视觉验收结果。冻结规则、Prompt 和
基线文件保持不变；Unity 验收过程中产生的场景、Prefab、Catalog 与截图均位于
隔离的 Calibration 范围。

## 放行结果

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| 离线冻结校验 | 7 / 7 通过 | `../freeze-v0.3.3/FREEZE-VALIDATION-REPORT-v0.3.3.json` |
| Console / 脚本编译 | 通过，零编译错误 | Formal Catalog、多屏构建和完整测试均以退出码 0 完成 |
| 隔离场景重载 | 通过 | Shop = 15 张 Exact 卡；Battle = 10 个 Exact 立牌；两场景均有且仅有一个活动 Validation Camera |
| EditMode | 359 / 359 通过 | `tests/EditMode-results.xml` |
| PlayMode | 30 / 30 通过 | `tests/PlayMode-results.xml` |
| 人工视觉与交互 | 通过 | Shop 卡牌齐全；Battle 可开始；新立绘正确；护盾与卡片背景区分清楚 |
| 正式 Runtime 隔离 | 通过 | 正式 Cards、正式 `PresentationSpriteCatalog.asset` 和正式 UI Prefab 无工作区改动 |
| 序列化范围审计 | 通过 | 新增或更新内容仅为 Calibration 资产、隔离配置/场景/Prefab、对应 `.meta` 与本放行证据 |

离线脚本的结果名仍为 `PASS_OFFLINE_UNITY_PENDING`，它只表达该脚本不会代替
Unity 运行时验收；本文件记录的自动化和人工结果共同完成
`UNITY_FINAL_RELEASE`。

## 最终截图

- [Formal Catalog 15 张卡](screenshots/formal-catalog-final-1920x1080.png)
- [Shop 15 张卡](screenshots/shop-final-1920x1080.png)
- [Battle 5v5 普通与护盾同屏](screenshots/battle-normal-shield-final-1920x1080.png)

三张截图均来自 Unity Game 视图的 `Full HD (1920x1080)` 预设。

## 自动化结果

| 平台 | Result | Total | Passed | Failed | Skipped | Inconclusive | Duration |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| EditMode | Passed | 359 | 359 | 0 | 0 | 0 | 6.384 s |
| PlayMode | Passed | 30 | 30 | 0 | 0 | 0 | 33.547 s |

测试结果 SHA-256：

- `EditMode-results.xml`：
  `d3622bb33250687393216da525f5af61b667cf745258bbae5bb9f53c89677ab7`
- `PlayMode-results.xml`：
  `ed64b8765671bedaf3bdb78cb0bb5176174d244b88ad8176e94bc5b5dd518215`

截图 SHA-256：

- `formal-catalog-final-1920x1080.png`：
  `c6ffb4acfbe68a2b9d4e75d9d30eacf61fa2f8a79c6a56b1b84a9e68b83f35e8`
- `shop-final-1920x1080.png`：
  `2dc39e5c871b772013c4bd39b40bcc667b94abcd0fce0378ad05742f23706772`
- `battle-normal-shield-final-1920x1080.png`：
  `baa8d47328e6e9761483a955ee5d26a946f1fc8bcd0f10b363207c60ea14df5d`

## 后续约束

后续正式卡池和背景继续使用 v0.3.3 Prompt 与视觉回归基线。任何冻结规则变化
必须创建 v0.3.4，不原地修改 v0.3.3。
