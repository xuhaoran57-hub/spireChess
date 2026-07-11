# 阶段 4A 单层最薄闭环技术方案 v0.1

版本：0.1
状态：已按方案完成实现和自动化验证；待集中 Unity 人工验收
上位设计：`phase-4-node-design-v0.1.md`
关联文档：`development-plan.md`、`game-design.md`、`shop-economy-design-v0.1.md`、`phase-3-build-design-v0.1.md`、`phase-4b-technical-design-v0.1.md`

## 1. 目的

阶段 4A 的目标是用一张固定单层地图验证最小爬塔纵切面，而不是一次实现阶段 4 的全部节点。

本阶段要打通：

`RunTest 地图 → 强制首战商店 → 战斗 → 结果与奖励 → 普通节点二选一 → Boss 商店 → Boss 战斗 → 原型通关或失败`

4A 完成后应能够证明：

- 地图、商店和战斗三个场景由同一个 `RunSession` 状态驱动。
- 空阵容玩家可以从 3 金币首店开始构筑，并完成三个战斗节点。
- `RunTurn`、预算、升级降价和 Boss 重试使用同一套经济时钟。
- 战斗伤害、奖励和节点状态不会因场景重载重复结算。
- 普通失败可以继续，Boss 失败可以重试，生命归零可以结束单局。
- 固定地图和固定遭遇来自配置，后续扩展三层或随机地图不需要改流程契约。

## 2. 范围

### 2.1 4A 包含

- `RunState`、`RunPhase` 和节点尝试状态。
- 一张固定单层地图。
- 一个强制开局小怪、两个分支小怪和一个 Boss。
- 小怪与 Boss 的商店前置、战斗、结果和节点结算。
- 玩家当前生命、最大生命和伤害计算。
- 普通奖励生成、延迟商店资源和卡牌奖励队列基础能力。
- Boss 失败重试。
- 原型通关、单局失败和重新开始。
- `RunTest` 最小地图、结果、奖励和结算 UI。
- 对应 EditMode 与 PlayMode 自动化测试。

### 2.2 4A 不包含

- 三层正式地图和 6-8 节点的完整单层规模。
- 精英、事件、锻造台和恢复点的交互实现。
- 随机地图生成。
- 正式 Boss 专属机制、正式美术和正式数值平衡。
- 退出应用后的单局存档恢复。
- 阶段 1-3 的集中 Unity 人工验收；按当前约定留到阶段 2-4 功能完成后统一进行。

这些内容不进入 4A 代码，但公共状态和接口不能阻止 4B、4C 接入。

## 3. 现有基础与缺口

| 领域 | 现有基础 | 4A 缺口 |
| --- | --- | --- |
| 应用生命周期 | `GameApp.Run` 持有跨场景 `RunSession` | `RunSession` 仍是测试链路，不含正式单局状态机 |
| 商店 | `ShopSession` 已支持购买、刷新、升级、冻结、阵容和三连 | `Round` 同时承担预算与开店次数，不能表达非战斗节点和 Boss 重试 |
| 商店奖励 | 已有 `GrantGold`、`GrantFreeRefreshes` | 缺少外部升级折扣、卡牌奖励领取和牌池预留入口 |
| 战斗 | `BattleSimulator` 和回放流程可用 | `Winner == null` 无法区分双方同时倒下和 30 回合上限 |
| 跨场景战斗 | `BattleContext`、`PendingBattle`、返回场景链路可用 | 缺少节点、尝试、遭遇和幂等结算身份 |
| 地图 | 无正式运行时模型 | 需要地图定义、提供器、校验器和节点状态推进 |
| 生命与奖励 | 无正式单局实现 | 需要伤害结算、延迟资源、奖励队列与清理 |
| UI | `ShopTest`、`BattleTest` 原型可运行 | 需要 `RunTest` 和基于 `RunPhase` 的入口适配 |

4A 采用增量迁移：保留现有 `ShopTest`、`BattleTest` 的独立测试能力，同时为爬塔模式增加明确入口，不把测试控制器直接改造成承载全部领域规则的总控制器。

## 4. 总体结构

```text
FixedMapProvider ──> MapDefinition
                         │
ConfigService ───────────┼──> RunSession ──> RunState
                         │         │
                         │         ├──> ShopSession
                         │         ├──> BattleContext / BattleSimulator
                         │         ├──> BattleSettlementCalculator
                         │         └──> RewardQueue / PoolReservation
                         │
                         └────────────> RunTest / ShopTest / BattleTest
```

职责边界：

- `RunSession` 是唯一流程写入口，验证阶段、推进状态并协调子系统。
- `RunState` 只保存单局事实，不直接加载场景，也不持有 Unity 组件。
- `MapDefinition` 是地图数据；地图 UI 不读取原始 JSON。
- `ShopSession` 继续拥有金币、酒馆、商品、牌池和玩家卡牌集合。
- `BattleSimulator` 只产生战斗事实，不修改单局生命、地图或奖励。
- `BattleSettlementCalculator` 把战斗事实和遭遇配置转换为伤害及节点结果。
- UI 只提交命令并渲染状态，不能自行推进 `RunTurn` 或发放奖励。

## 5. 核心领域模型

### 5.1 `RunState`

建议字段：

```csharp
public sealed class RunState
{
    public int Seed { get; }
    public int Floor { get; internal set; }
    public int RunTurn { get; internal set; }
    public int Health { get; internal set; }
    public int MaxHealth { get; internal set; }
    public RunPhase Phase { get; internal set; }
    public MapDefinition CurrentMap { get; internal set; }
    public string CurrentNodeId { get; internal set; }
    public NodeAttemptState CurrentAttempt { get; internal set; }
    public PendingRewardChoice PendingRewardChoice { get; internal set; }
    public Queue<PendingCardReward> PendingCardRewards { get; }
    public DelayedShopResources DelayedShopResources { get; }
}
```

地图节点的 `Locked`、`Reachable`、`Current`、`Resolved` 状态可以保存在独立的 `MapProgressState` 中，避免污染不可变的 `MapDefinition`。

初始值固定为：

- `Floor = 1`
- `RunTurn = 0`
- `Health = MaxHealth = 20`
- 空战斗区、空备战区
- `Phase = MapSelection`
- 仅强制首战节点为 `Reachable`

### 5.2 `RunPhase`

4A 使用以下阶段：

```csharp
public enum RunPhase
{
    MapSelection,
    EnteringNode,
    Shop,
    Battle,
    BattleResult,
    RewardChoice,
    RunWon,
    RunLost
}
```

`EventChoice`、`EnhanceChoice`、`RestChoice` 和 `FloorComplete` 保留给后续阶段增加。4A 不用无意义的阶段占位驱动 UI。

### 5.3 `NodeAttemptState`

```csharp
public sealed class NodeAttemptState
{
    public string NodeAttemptId { get; }
    public string NodeId { get; }
    public string EncounterId { get; }
    public int RunTurn { get; }
    public bool RunTurnCommitted { get; internal set; }
    public bool BattleSettled { get; internal set; }
    public bool HealthDamageApplied { get; internal set; }
    public bool RewardGenerated { get; internal set; }
    public bool NodeResolved { get; internal set; }
}
```

约束：

- 选择节点与创建尝试、推进 `RunTurn` 是一个领域操作。
- 场景重载恢复当前 `NodeAttemptId`，不能重新进入节点。
- Boss 失败后节点仍为 `Current`；点击重试时创建新的尝试和 ID。
- `BattleContext` 必须携带 `NodeAttemptId` 和 `EncounterId`。
- 只接受与当前尝试 ID 相同的战斗结果；迟到或重复结果不产生状态变更。

第一版是内存态，不承诺进程崩溃恢复；这些标记解决的是场景切换、重复按钮和重复回调造成的二次提交，并为后续存档格式预留边界。

### 5.4 地图模型

建议类型：

- `MapRequest`：单局种子、层数、难度、允许节点类型。
- `MapDefinition`：地图 ID、楼层、节点集合、起始节点集合。
- `MapNodeDefinition`：节点 ID、类型、布局坐标、内容 ID、后继 ID。
- `MapProgressState`：节点运行时状态与当前节点。
- `IMapProvider`：按请求返回已经过校验的地图。
- `FixedMapProvider`：4A 唯一实现，从配置读取固定地图。
- `MapValidator`：执行图结构和内容引用校验。

`RunSession` 只依赖 `IMapProvider`。未来的 `RandomMapProvider` 必须返回相同 `MapDefinition`，不能让 UI 或节点结算感知地图来源。

### 5.5 遭遇与奖励

- `EncounterConfig` 定义敌方槽位、节点类别、伤害修正和奖励表 ID。
- `RewardTableConfig` 定义可生成奖励及权重。
- `PendingRewardChoice` 表示尚未完成的候选选择。
- `PendingCardReward` 保存卡牌类型、配置 ID、实例所需信息和牌池预留数量。
- `DelayedShopResources` 保存下个商店金币、免费刷新和额外升级折扣。

奖励生成只由 `RunSession` 在 `rewardGenerated == false` 时调用。UI 不直接随机抽取奖励。

### 5.6 持有随从修改入口

4A 不实现锻造台，但当前 `ShopCardInstance.ApplyPermanentStats` 是 `internal`，永久关键词也没有统一写入口。4B 前应通过 `OwnedMinionModifier` 或等价领域服务提供：

- 永久属性修改。
- 永久关键词授予及重复校验。
- 合成素材效果继承。
- 修改结果的原子校验和事件通知。

锻造 UI 不应直接把集合强转后修改，也不应为锻造单独复制三连继承规则。

## 6. 4A 固定地图

4A 使用一张四节点可见、单条路线访问三个节点的技术验证地图：

```text
                         ┌─> f1_normal_a ─┐
f1_opening_normal ───────┤                ├─> f1_boss
                         └─> f1_normal_b ─┘
```

| 节点 | 类型 | 进入条件 | 遭遇要求 | 成功后 |
| --- | --- | --- | --- | --- |
| `f1_opening_normal` | Normal | 新单局唯一可达 | 仅一个低强度 1 级敌人 | 解锁 A/B |
| `f1_normal_a` | Normal | 首战已完成 | 固定基础阵容 A | 解锁 Boss |
| `f1_normal_b` | Normal | 首战已完成 | 固定基础阵容 B，与 A 有站位或关键词差异 | 解锁 Boss |
| `f1_boss` | Boss | A 或 B 已完成 | 固定第一层 Boss 阵容 | 胜利则原型通关 |

关键规则：

- 地图进入时完整显示四个节点和连线，但只有首战可选。
- A/B 是互斥路线；选择一个后不能返回另一个。
- 4A 地图小于正式每层 6-8 节点规模，只用于纵切面验证，4C 再扩为正式三层配置。
- 三个首次尝试分别令 `RunTurn` 为 1、2、3，对应预算为 3、4、5 金币。
- Boss 第一次失败后的重试令 `RunTurn` 为 4，对应 6 金币；后续继续递增并在 10 金币封顶。
- 最终 Boss 胜利不发放功能奖励，直接进入 `RunWon`；奖励表接口仍保留，供 4C 的第一、二层 Boss 使用。

开局敌人具体使用哪个 1 级配置由遭遇 JSON 决定，但必须通过验证器保证只有一个启用的 1 级非 Token 随从，不能把“弱首战”写死在控制器中。

## 7. 流程状态机

| 当前阶段 | 命令或事实 | 条件 | 下一个阶段 | 主要提交 |
| --- | --- | --- | --- | --- |
| `MapSelection` | `EnterNode(nodeId)` | 节点为 `Reachable` | `EnteringNode` | 创建尝试并推进一次 `RunTurn` |
| `EnteringNode` | 准备战斗节点 | 尝试未开始商店 | `Shop` | 按当前 `RunTurn` 开商店并应用延迟资源 |
| `Shop` | `EndShop()` | 无发现、无待领取卡牌奖励 | `Battle` | 锁定阵容并创建带尝试 ID 的战斗上下文 |
| `Battle` | `SubmitBattleResult()` | 尝试 ID 匹配且未结算 | `BattleResult` | 保存一次战斗事实 |
| `BattleResult` | 结算玩家胜利 | 普通节点 | `MapSelection` 或 `RewardChoice` | 生成一次奖励；无阻塞选择时完成节点 |
| `BattleResult` | 结算玩家失败或平局 | 普通节点且生命大于 0 | `MapSelection` | 扣一次生命，无奖励，完成节点 |
| `BattleResult` | 结算玩家胜利 | Boss | `RunWon` | 完成 Boss 和原型单局 |
| `BattleResult` | `RetryBoss()` | Boss 失败且生命大于 0 | `EnteringNode` | 新建尝试并推进一次 `RunTurn` |
| `BattleResult` | 任何伤害结算 | 生命等于 0 | `RunLost` | 清理未领取奖励预留 |
| `RewardChoice` | 选择或跳过 | 候选仍有效 | `MapSelection` | 提交奖励并完成节点 |
| `RunWon` / `RunLost` | `StartNewRun()` | 用户确认 | `MapSelection` | 清理旧单局并创建全新状态 |

普通随机单项奖励不需要进入 `RewardChoice`，在 `BattleResult` 中生成后直接结算。只有奖励表明确要求多选一时才进入阻塞选择阶段。

战斗场景提交结果时只保存战斗事实；`BattleSettled` 在 `RunSession` 完成结果分类后才置为 true，生命、奖励和节点完成仍分别由其余标记控制。重复战斗回调可以通过当前阶段、已保存结果和尝试 ID 拒绝，不把“收到结果”与“完成结算”混成同一个提交点。

所有公开命令先验证 `RunPhase`。非法阶段调用返回明确错误且不修改任何状态。

## 8. `RunTurn` 与商店经济接口

### 8.1 精确语义

- `RunTurn` 从 0 开始。
- 每个节点首次进入时提交一次；每次 Boss 重试再提交一次。
- 商店预算为 `min(RunTurn + 2, 10)`。
- 战斗节点在商店结束且未成功升级时增加一次自然升级降价。
- 非战斗节点不打开商店，但等价增加一次自然升级降价。
- 额外升级折扣可以叠加，直到下一次成功升级后清零。
- 升级最终费用最低为 1。

### 8.2 `ShopSession` 调整建议

建议新增：

```csharp
public ShopOperationResult StartRound(int runTurn);
public ShopOperationResult AdvanceSkippedRound(int runTurn);
public void GrantUpgradeDiscount(int amount);
public int LastEconomyTurn { get; }
```

行为约束：

- `StartRound(runTurn)` 使用传入值计算预算，不再自行假设“开一次店等于时间推进一次”。
- `AdvanceSkippedRound(runTurn)` 只推进经济时间和自然升级降价，不打开商店、不补货、不清除冻结。
- 两个入口都校验 `runTurn` 单调递增；同一 `runTurn` 的重复恢复调用必须幂等，不能再次给预算或降价。
- `EndRound()` 继续负责商店关闭和“本回合未升级”的自然降价。
- `CurrentUpgradeCost` 同时减去自然降价和额外折扣，并 `Math.Max(1, ...)`。
- `UpgradeTavern()` 成功后清零自然降价和额外折扣；失败时两者都不变。
- 暂时保留 `StartNextRound()`，内部转调 `StartRound(LastEconomyTurn + 1)`，保证现有阶段 2、3 测试和独立 `ShopTest` 不被一次性破坏。

4A 只有战斗节点，不会调用 `AdvanceSkippedRound`，但此接口和状态必须在经济时钟改造时一起确定，避免 4B 加入事件、锻造和恢复点后再次改变 `Round` 语义。

### 8.3 延迟资源应用顺序

进入战斗节点商店时：

1. `StartRound(currentRunTurn)` 重置预算并处理补货。
2. 应用 `nextShopGoldBonus`。
3. 应用 `pendingFreeRefreshes`。
4. 应用 `nextUpgradeDiscount`。
5. 按 FIFO 打开待领取卡牌奖励。
6. 队列清空后允许正常商店操作和结束商店。

每一项延迟资源需要“已应用到哪个 `RunTurn`”的记录，不能只依赖先清零后调用的时序来保证跨场景幂等。

## 9. 战斗结果与伤害

### 9.1 结果原因

新增：

```csharp
public enum BattleOutcomeReason
{
    Victory,
    MutualElimination,
    RoundLimit
}
```

- `Victory` 表示有且仅有一方获胜，具体胜者继续由 `Winner` 表示。
- `MutualElimination` 表示双方同时没有存活随从。
- `RoundLimit` 表示达到 `BattleSimulator.MaxRounds` 时双方仍未分出胜负。

`BattleSimulationResult` 增加 `OutcomeReason`。`BattleSimulator` 是原因的唯一判定者，结算层和 UI 不通过日志文本猜测原因。

### 9.2 迁移兼容

- 为现有构造函数保留兼容重载，逐步把生产调用和测试迁移到显式原因。
- `BattleTestController` 根据原因显示“双方同时倒下”或“达到回合上限”。
- `Winner` 保持可空，避免大范围改变现有战斗展示代码。

### 9.3 伤害结算

`BattleSettlementCalculator` 输入 `BattleSimulationResult` 与 `EncounterConfig`，输出不可变结算结果：

- 玩家胜利：0 伤害。
- `MutualElimination` 或 `RoundLimit`：1 伤害。
- 敌方胜利：`max(1, 敌方存活数 + 敌方存活随从最高等级 + damageBonus)`。
- 敌方没有存活随从时不使用失败公式。

普通节点 `damageBonus = 0`，Boss 为 `+4`。等级来自随从配置；金色随从仍使用原配置等级，Token 使用自身配置等级。

结算结果应携带伤害拆分，供 `RunTest` 显示“存活数 + 最高等级 + 节点修正”。生命修改只由 `RunSession` 在 `healthDamageApplied == false` 时提交。

## 10. 奖励队列与牌池预留

### 10.1 队列规则

- `PendingCardRewards` 是 FIFO 队列，第一版无硬上限。
- 地图流程可以在奖励未领取时继续。
- 下一次商店打开后，必须先领取或跳过全部队列项目，才能结束商店。
- 领取失败（例如备战区已满）不出队、不部分提交，玩家可以整理空间后重试或跳过。
- 单局结束时释放全部未领取的随从奖励预留。

### 10.2 候选与预留生命周期

1. 生成候选时，从 `MinionPool` 暂时取出候选所需实体副本。
2. 玩家选择后，未选随从候选立即返还。
3. 选中随从转为 `PendingCardReward`，继续持有预留。
4. 领取成功后，预留转为玩家卡牌持有，不返还牌池。
5. 跳过、单局结束或奖励作废时，返还预留。

普通法术不占有限随从牌池。所有预留操作使用奖励实例 ID 幂等，不能只按卡牌配置 ID 判断是否已经返还。

### 10.3 商店协作接口

`ShopSession` 或其受控领域服务需要提供：

- 尝试把指定奖励卡牌加入备战区。
- 为奖励候选保留和返还随从副本。
- 查询备战区空间，但不能以预检查代替原子领取。

不能从 `RunSession` 直接修改 `PlayerCollection` 的内部列表，否则三连、牌池副本和事件通知容易失去一致性。

## 11. 场景与控制器协作

### 11.1 `RunTest`

4A 新增 `RunTest` 作为爬塔流程的承载场景，最少显示：

- 当前生命、`RunTurn` 和当前层数。
- 四节点固定地图、连线和节点状态。
- 当前战斗结果、伤害拆分和奖励摘要。
- 普通节点继续、Boss 重试、通关/失败和重新开始按钮。
- 阻塞奖励选择，以及待领取卡牌奖励数量提示。

### 11.2 `ShopTest`

保留独立商店测试模式。爬塔模式下：

- 从 `RunSession.State.Phase == Shop` 重建界面。
- 不自行调用 `StartNextRound()`；由 `RunSession` 使用当前 `RunTurn` 开店。
- 结束商店时先检查奖励队列和发现阻塞，再请求 `RunSession.EndShop()`。
- 战斗上下文的返回场景固定为 `RunTest`。

### 11.3 `BattleTest`

保留独立预设测试模式。爬塔模式下：

- 从 `BattleContext` 读取 `NodeAttemptId` 和遭遇信息。
- 模拟完成后只提交一次结果，不直接扣生命或发奖励。
- 返回按钮进入 `RunTest`，由 `RunTest` 根据 `BattleResult` 展示并继续流程。
- 场景重载时如果结果已提交，只重建最终展示，不重复调用结算。

后续可以把共享展示抽为正式控制器，但 4A 不要求重命名现有测试场景。

## 12. 配置与校验

建议新增资源：

- `Assets/Resources/Configs/Json/run-maps.v0.1.json`
- `Assets/Resources/Configs/Json/encounters.v0.1.json`
- `Assets/Resources/Configs/Json/rewards.v0.1.json`

`ConfigService` 统一加载并建立 ID 索引。配置引用校验应在新单局开始前完成。

地图校验：

- 节点 ID 唯一，后继均存在，无环。
- 唯一起始节点是 `f1_opening_normal`。
- A/B 互斥且都能到达唯一 Boss。
- Boss 无后继。
- 节点类型与内容 ID 类型匹配。

遭遇校验：

- 槽位为 0-4 且不重复。
- 随从 ID 存在并启用。
- 普通节点修正为 0，4A Boss 修正为 +4。
- 开局遭遇恰好一个 1 级、非 Token、非金色随从。
- 奖励表 ID 存在；最终 Boss 可以显式使用空奖励表。

奖励校验：

- 权重大于 0，引用卡牌存在并符合奖励等级限制。
- 需要有限牌池副本的奖励声明正确数量。
- 延迟资源数值非负。

## 13. 建议代码布局

```text
Assets/Scripts/Run/
  RunSession.cs
  RunState.cs
  RunPhase.cs
  NodeAttemptState.cs
  BattleContext.cs
  Map/
    MapDefinition.cs
    MapProgressState.cs
    IMapProvider.cs
    FixedMapProvider.cs
    MapValidator.cs
  Rewards/
    RewardModels.cs
    RewardService.cs
    PendingCardRewardQueue.cs
  Settlement/
    BattleSettlementCalculator.cs

Assets/Scripts/Config/
  RunMapConfig.cs
  EncounterConfig.cs
  RewardConfig.cs

Assets/Scripts/UI/Run/
  RunTestController.cs
  RunMapNodeView.cs
```

文件布局是建议，不要求为了目录形式拆出没有行为的微型类。关键要求是领域逻辑不落入 MonoBehaviour，地图定义和运行进度不混为一个可变对象。

## 14. 自动化测试

### 14.1 EditMode

地图与流程：

- 固定地图合法，首战唯一可达，A/B 互斥，两路都到达 Boss。
- 新单局为空阵容、20/20 生命、`RunTurn = 0`。
- 三次首次进入产生不同尝试 ID，预算依次为 3、4、5。
- 重复进入或重载同一尝试不会重复推进 `RunTurn`。
- 普通失败完成节点；Boss 失败保留节点并在重试时创建新尝试。

经济：

- `StartRound(runTurn)` 按 `min(runTurn + 2, 10)` 给预算。
- 跳过商店的 `RunTurn` 推进一次自然降价且不会补货。
- 自然降价和额外折扣叠加，费用最低为 1。
- 升级成功清空两类折扣，升级失败不清空。
- 旧 `StartNextRound()` 行为保持现有测试兼容。

战斗与幂等：

- 正常胜负、双方同时倒下和回合上限返回不同原因。
- 普通与 Boss 伤害公式、平局 1 点伤害正确。
- 同一尝试重复提交战斗结果、扣血、生成奖励和完成节点都只生效一次。
- 迟到的旧尝试结果被拒绝。

奖励：

- 候选生成、未选返还、选中保留、领取转移和跳过返还正确。
- 队列保持 FIFO，备战区满时领取失败不丢奖励。
- 队列未清空时不能结束商店。
- 通关和失败会释放所有未领取预留。

### 14.2 PlayMode

- 新单局进入 `RunTest`，只有强制首战可选。
- 完成“首战商店 → 战斗 → 结果 → A/B → Boss → 通关”的完整胜利路径。
- 普通节点失败扣血一次后仍可继续。
- Boss 失败后返回结果页，重试进入新商店和新尝试。
- 生命归零进入失败页，不能再操作地图或奖励。
- 在 Shop、Battle、BattleResult 场景重载后不重复预算、扣血、奖励或节点完成。

现有 52 个 EditMode 和 3 个 PlayMode 测试必须继续通过；4A 新测试在此基础上增加，不以删除旧覆盖换取通过。

## 15. 实施顺序

1. 增加领域模型、`RunPhase`、地图定义和固定地图校验。
2. 调整 `ShopSession` 的显式经济时钟接口，并保持旧入口兼容。
3. 增加 `BattleOutcomeReason` 和纯领域伤害计算器。
4. 将现有 `RunSession` 从测试链路扩展为状态机，加入尝试幂等。
5. 实现延迟商店资源、奖励队列和牌池预留生命周期。
6. 增加地图、遭遇和奖励配置及加载校验。
7. 新增 `RunTest`，为 `ShopTest`、`BattleTest` 增加爬塔模式适配。
8. 补齐 EditMode 和 PlayMode 测试，再进行一次全量自动化回归。

每一步先完成纯 C# 领域测试，再接场景。这样 Unity 场景问题不会掩盖流程状态和牌池一致性问题。

## 16. 4A 完成标准

- 一张固定单层地图可以从空阵容完整走到原型通关或失败。
- 第一层必须先打强制普通节点，首战前商店预算为 3。
- 首战后可以在两个固定普通遭遇间选择，并最终进入 Boss。
- 普通失败、平局、Boss 失败重试和生命归零均符合设计。
- `RunTurn`、预算和升级降价在首次进入、重试及恢复调用下都只推进一次。
- 战斗结果明确区分正常胜负、双方同时倒下和回合上限。
- 奖励队列和有限牌池预留在领取、跳过及单局结束时保持守恒。
- Shop、Battle、Run 三个场景间切换不会重复结算。
- 新增自动化测试与既有测试全部通过。
- 文档、配置字段和代码命名一致。

4A 完成不等于阶段 4 完成。精英、事件、锻造、恢复、正式每层规模和三层完整单局分别在 4B、4C 继续实现。

## 17. 当前结论

本方案已完成实现，不再保留阻塞 4A 的规则空缺。后续扩展 4B、4C 时应继续遵守以下契约：

- 第一层强制首战，首战后分支。
- 每次节点尝试对应唯一 ID 和一次 `RunTurn`。
- 预算由 `RunTurn` 决定，非战斗节点仍推进经济时间。
- 战斗事实、单局伤害和节点奖励分层结算。
- 奖励使用 FIFO 队列并显式管理牌池预留。
- 场景只表现 `RunState`，不拥有流程真相。

实际实现已经覆盖第 15 节全部步骤：

- 新增固定地图、遭遇和奖励 JSON，并在 `ConfigService` 中完成加载和引用校验。
- `RunSession` 已扩展为由 `RunState`、`RunPhase` 和 `NodeAttemptState` 驱动的流程状态机。
- `ShopSession` 已支持显式 `RunTurn`、跳过商店回合和额外升级折扣，同时保留 `StartNextRound()` 兼容入口。
- `BattleSimulationResult` 已提供 `BattleOutcomeReason`，伤害由独立结算器计算。
- FIFO 卡牌奖励支持领取、跳过和有限牌池预留释放。
- `RunTest`、`ShopTest`、`BattleTest` 已完成爬塔模式适配，战斗结果场景重载不会重复结算。
- 自动化结果：EditMode 60 / 60、PlayMode 6 / 6 通过。

Unity 人工通关、失败和表现验收按当前约定暂缓，待阶段 2-4 功能集中验收时执行。下一步进入阶段 4B 的精英、事件、锻造、恢复和路线风险收益实现。
