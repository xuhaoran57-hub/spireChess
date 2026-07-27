# 阶段 9B G4-E 工程集成门禁关闭记录 v0.1

- 日期：2026-07-27
- 结论：`G4-E 通过并关闭`
- 工程证据候选：`f377497d1f3e65486370d6b35d91811d1bff50bc`
- Unity：2022.3.62f3c1
- 适用边界：只关闭工程可独立证明的集成门禁；不关闭 G4-V 视觉签字、
  G4-P 第二机/两机阈值、G4-U 外部试玩或 G4-X 正式音频

## 1. 为什么现在可以关闭 G4-E

G4-E 验证的是“游戏能否以同一冻结候选稳定完成正式流程”，不是验证全量美术是否
已经生产完成。下列工程事实已经在同一干净候选上形成可复核证据：

- EditMode 351 / 351、PlayMode 30 / 30，0 失败、0 跳过、0 inconclusive；
- 26 个正式 Windows Player 运行全部 `AcceptancePassed`；
- 真实 UI 链路覆盖
  `MainMenu → Run → Shop → Run → Battle → Run → MainMenu → Continue`；
- 存档恢复、1×/2×/跳过等价、单次结算、动态召唤、清理归零均通过；
- 12 随从、3 Token、4 法术、3 遗珍组成的 22 项 G2 样板 Catalog 全部 Exact；
- 双分辨率 Core/Frozen/Stress 技术采集、DEV-A 性能基线、构建与证据哈希均已归档。

证据明细继续以
`phase-9b-g4-non-audio-acceptance-v0.1.md` 和
`ui-concepts/unity-validation/g4-formal-chain-v0.1/manifest.md` 为准。

## 2. G4-E 与 G4-V 的分界

| 范围 | 状态 | 说明 |
| --- | --- | --- |
| G4-E 工程集成 | 已关闭 | 流程、输入、存档、确定性、构建、自动化和开发机性能证据完整 |
| G4-V 正式视觉样板 | 候选已接入，待验收 | 主菜单、楼层地图、商店、事件、战斗背景已形成同风格闭环 |
| G4-P 第二机 | 未完成 | 执行包已存在，仍需不同配置实体机结果与两机阈值 |
| G4-U 外部试玩 | 未完成 | 仍需至少 5 名未参与实现者记录 |
| G4-X 正式音频 | 阻塞 | Placeholder 只能联调，不能关闭 G3/G4 总门禁 |

因此，后续卡牌立绘和全量场景美术未齐不使 G4-E 失去意义；它们影响的是 G4-V 和
9C 资产完成度。只有后续改动造成工程回归失败，才重新打开 G4-E。

## 3. 代表最终效果的正式视觉闭环

本轮新增并接通以下五类视觉样板：

| 场景 | Runtime 资源 | 接线 |
| --- | --- | --- |
| 主菜单主视觉 | `Resources/Presentation/Backdrops/backdrop_main_menu` | `PresentationBackdropGraphic` 自动加载；程序化背景保留为缺图降级 |
| 一套楼层/地图背景 | `Resources/Presentation/Backdrops/backdrop_floor_map` | `RunScreenView` 的滚动地图背景层 |
| 商店环境 | `Resources/Presentation/Backdrops/backdrop_shop` | 统一背景组件；商品/阵容/手牌面板改为半透明以显露环境 |
| 静谧林地事件插画 | `Resources/Presentation/Events/event_tranquil_grove` | `EventConfig.artId → RunChoiceOverlayState.ArtworkId → RunScreenView`，插画出现时选项改为双列 |
| 战斗背景 | `Resources/Presentation/Backdrops/backdrop_battle` | `BattleScreenView` 棋盘底层；敌我行保留半透明语义色 |

既有代表性卡牌、法术和遗珍继续使用已通过 G2/G4 的
`PresentationSpriteCatalog` 22 项 Exact 范围，不复制第二套目录或绕过 Catalog。

资源、SHA-256、生成提示词和状态见
`ui-concepts/phase-9b/g4-visual-slice-v0.1/README.md`。

## 4. 当前环境的复验边界

本轮已完成 JSON 解析、PNG 尺寸/非空、SHA-256、Unity `.meta` 配对、GUID 唯一性和
`git diff --check` 静态校验，并增加以下 Unity 门禁：

- 四张背景与一张事件插画必须可作为 Sprite 从 Resources 导入；
- `tranquil_grove.artId` 必须进入 Run 事件选择状态；
- 正式图缺失时继续使用现有程序化/纯色降级，不阻断输入和流程。

当前机器没有项目指定的 Unity 2022.3.62f3c1，因此本轮视觉增量尚未生成新的
Unity XML、Player 构建或双分辨率截图。进入 G4-V 人工签字前必须在指定 Unity 中：

1. 运行完整 EditMode 与 PlayMode，要求 0 失败、0 跳过；
2. 重建 Windows Player，复跑 Core/Frozen 正式链路；
3. 在 1920×1080 和 1920×1200 复核五个新增画面、事件选项交互和文字对比度；
4. 负责人确认生成式资产许可后，才能从 `工程样板` 晋级为
   `生产许可已确认` / `Runtime Ready`。

这些是 G4-V 候选晋级条件，不回溯否定 `f377497` 已关闭的 G4-E 工程事实。
