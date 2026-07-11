# 阶段 4B 路线风险收益技术方案 v0.1

版本：0.1
状态：已实现并完成自动化验证；Unity 人工验收暂缓
上位设计：`phase-4-node-design-v0.1.md`
前置实现：`phase-4a-technical-design-v0.1.md`
关联文档：`development-plan.md`、`game-design.md`、`shop-economy-design-v0.1.md`

## 1. 目的

阶段 4A 已经证明地图、商店、战斗、生命、普通奖励和 Boss 重试可以形成单层闭环。阶段 4B 的目标是在不扩展到三层完整单局的前提下，让路线选择第一次产生明确的风险与收益差异。

4B 要打通：

`强制首战 → 高风险精英/锻造路线或稳定普通/事件/恢复路线 → Boss → 原型结算`

本阶段需要验证：

- 精英路线是否通过更高伤害风险换取明显更高的构筑收益。
- 事件、锻造和恢复点在不打开商店的情况下，是否仍正确推进 `RunTurn` 和自然升级降价。
- 阻塞式三选一奖励、事件选择、锻造目标和恢复选择是否都能跨场景重建且只提交一次。
- 锻造是否通过永久修改已有随从，与商店购买和事件资源形成明确分工。
- 同样数量的节点和商店机会下，两条路线能否产生可理解的取舍。

## 2. 范围

### 2.1 4B 包含

- 将 4A 固定地图扩为 7 个可见节点、每条路线访问 4 个节点的单层地图。
- 一个第一层精英遭遇“嘲讽壁垒”。
- 精英胜利后的阻塞式三选一奖励。
- 一个通用事件节点和第一批 5 个事件。
- 一个锻造台节点和 3 个固定配方：+2/+2、`Shield`、`Taunt`。
- 一个恢复点节点：恢复 6、最大生命 +2 并恢复 2、离开。
- 非战斗节点的 `RunTurn`、自然升级降价与幂等提交。
- 持有随从永久修改的统一领域入口。
- `RunTest` 中的精英预览、奖励、事件、锻造和恢复交互。
- 对应 EditMode 和 PlayMode 自动化测试。

### 2.2 4B 不包含

- 第二、三层地图、跨层推进和完整单局统计。
- 第二、三层精英主题。
- 基于节点权重的随机地图生成。
- 新战斗关键词、精英专属技能和 Boss 专属机制。
- `Cleave` 锻造配方。
- 正式地图动画、节点移动动画和正式数值平衡。
- 退出应用后的单局存档恢复。
- 集中 Unity 人工验收；继续按约定留到阶段 2-4 功能完成后执行。

### 2.3 Boss 奖励边界

4B 会实现可复用于精英和 Boss 的高价值奖励选择基础，但固定单层原型的 Boss 胜利仍直接结束原型，不发功能奖励。

原因是第一、二层 Boss 奖励只有在存在下一层商店时才具有真实构筑价值。如果在单层原型结束前发卡，奖励会立即随单局清理，无法验证实际收益。第一、二层 Boss 奖励表的正式启用和跨层保留放到 4C；这不是接口缺口，而是流程消费端尚不存在。

## 3. 4A 现有基础与 4B 缺口

| 领域 | 4A 已实现 | 4B 需要补充 |
| --- | --- | --- |
| 流程 | `RunSession`、`RunState`、节点尝试和战斗状态机 | 非战斗节点分发、四种阻塞选择阶段 |
| 地图 | 固定 4 节点地图、可达状态和图校验 | 7 节点风险收益地图、不同节点内容引用校验 |
| 经济时钟 | `StartRound(runTurn)`、`AdvanceSkippedRound(runTurn)` | 在非战斗节点进入时正式调用并记录提交 |
| 战斗 | 普通和 Boss 遭遇、伤害拆分 | 精英遭遇、+2 修正、精英胜负分支 |
| 奖励 | 自动单项奖励、FIFO 卡牌队列 | 阻塞式奖励候选、候选牌池预留和目标型奖励 |
| 事件 | 无 | 事件抽取、选项合法性和原子效果事务 |
| 锻造 | 金色继承已有，卡牌属性修改为内部能力 | 统一持有随从修改入口、关键词授予和目标验证 |
| 恢复 | `Health`、`MaxHealth` 状态存在 | 恢复/最大生命选项和零收益校验 |
| UI | 地图、战斗结果、卡牌奖励领取 | 精英风险提示与四类选择覆盖层 |

4B 继续保持领域逻辑不进入 MonoBehaviour。`RunTestController` 只渲染 `RunState` 中已经物化的选择状态，并向 `RunSession` 提交命令。

## 4. 4B 固定地图

### 4.1 地图结构

4B 使用 7 个可见节点：

```text
                              ┌─> 精英：嘲讽壁垒 ─> 锻造台 ─────┐
强制普通首战 ────────────────┤                               ├─> Boss
                              └─> 普通战斗 ─┬─> 事件 ──────────┤
                                             └─> 恢复点 ────────┘
```

精确连线：

```text
f1_opening_normal -> f1_elite_wall -> f1_enhance -> f1_boss
f1_opening_normal -> f1_safe_normal -> f1_event   -> f1_boss
                                      -> f1_rest    -> f1_boss
```

| 节点 | 类型 | 作用 |
| --- | --- | --- |
| `f1_opening_normal` | Normal | 强制首战，保持 4A 弱敌人边界 |
| `f1_elite_wall` | Elite | 高风险战斗，胜利后三选一高价值奖励 |
| `f1_enhance` | Enhance | 精英路线的确定性永久强化 |
| `f1_safe_normal` | Normal | 稳定路线的常规商店和普通奖励 |
| `f1_event` | Event | 在 5 个事件中按单局随机流物化一个事件 |
| `f1_rest` | Rest | 立即恢复或提高本局最大生命 |
| `f1_boss` | Boss | 两条主路线合流后的固定终点 |

### 4.2 路线公平性

- 两条主路线都访问 4 个节点并包含 Boss。
- 两条主路线都恰好打开 3 次战斗商店：首战、路线战斗、Boss。
- 高风险路线收益为“精英三选一 + 锻造”，代价是更强敌人和失败 +2 伤害修正。
- 稳定路线收益为“普通奖励 + 事件或恢复”，玩家再决定追求资源还是生命容错。
- 事件与恢复是互斥节点，避免稳定路线无成本拿到两种收益。

路线差异不通过额外商店次数制造，便于将测试结果归因于节点本身。

### 4.3 `RunTurn` 节奏

| 访问位置 | `RunTurn` | 是否开店 | 基础预算 |
| --- | ---: | --- | ---: |
| 强制首战 | 1 | 是 | 3 |
| 精英或安全普通战斗 | 2 | 是 | 4 |
| 锻造、事件或恢复 | 3 | 否 | 无当前预算 |
| Boss | 4 | 是 | 6 |

第三个非战斗节点调用 `ShopSession.AdvanceSkippedRound(3)`，因此 Boss 商店同时获得：

- `RunTurn = 4` 对应的 6 金币基础预算。
- 非战斗节点带来的一次自然升级降价。
- 节点奖励产生的延迟金币、免费刷新或额外升级折扣。

## 5. 总体架构

```text
MapDefinition / Node Config
            │
            v
       RunSession
       ├── Combat flow ─────> ShopSession / BattleSimulator
       ├── RewardChoiceService ─> Reward reservations / FIFO queue
       ├── RunEffectTransaction ─> health / delayed resources
       ├── OwnedMinionModifier ──> PlayerCollection / ShopCardInstance
       └── Node choice state ────> RunTest overlays
```

职责边界：

- `RunSession` 继续是唯一公开流程写入口，负责阶段校验、节点分发和提交顺序。
- `RewardChoiceService` 负责物化候选、持有候选预留、选择或跳过后的清理。
- `RunEffectTransaction` 负责事件和恢复效果的“全部验证后一次提交”。
- `OwnedMinionModifier` 负责所有持有随从永久修改，锻造和目标型精英奖励共同复用。
- `RunTestController` 不直接修改生命、卡牌、关键词或延迟资源。

4B 不要求引入完整依赖注入框架。上述服务可以由 `RunSession` 构造并持有，但每个服务必须可在纯 C# 测试中独立验证。

## 6. 状态模型扩展

### 6.1 `RunPhase`

在 4A 枚举基础上正式启用：

```csharp
EventChoice,
EnhanceChoice,
RestChoice
```

继续使用现有 `RewardChoice`。4B 不启用 `FloorComplete`，它属于 4C。

### 6.2 通用节点尝试

当前 `NodeAttemptState` 使用必填 `EncounterId`，只适合战斗节点。4B 调整为：

```csharp
public sealed class NodeAttemptState
{
    public string NodeAttemptId { get; }
    public string NodeId { get; }
    public RunNodeType NodeType { get; }
    public string ContentId { get; internal set; }
    public int RunTurn { get; }

    public bool RunTurnCommitted { get; internal set; }
    public bool EconomyTurnCommitted { get; internal set; }
    public bool ContentGenerated { get; internal set; }
    public bool ChoiceCommitted { get; internal set; }
    public bool EffectApplied { get; internal set; }
    public bool BattleSettled { get; internal set; }
    public bool HealthDamageApplied { get; internal set; }
    public bool RewardGenerated { get; internal set; }
    public bool NodeResolved { get; internal set; }
}
```

- 战斗节点的 `ContentId` 是 `EncounterConfig.id`。
- 事件节点在进入时从事件池物化具体事件，随后把事件 ID 写入 `ContentId`。
- 锻造和恢复节点的 `ContentId` 是对应节点内容配置 ID。
- 原 `EncounterId` 可暂时保留为战斗节点兼容只读别名，待调用迁移完再移除。

标记必须按实际提交顺序设置。场景重载只读取状态，不重新抽事件、生成奖励候选或应用效果。

### 6.3 `RunState` 新增选择状态

```csharp
public PendingRewardChoice PendingRewardChoice { get; internal set; }
public PendingEventChoice PendingEventChoice { get; internal set; }
public PendingEnhanceChoice PendingEnhanceChoice { get; internal set; }
public PendingRestChoice PendingRestChoice { get; internal set; }
```

任意时刻最多只能存在一个阻塞选择。状态切换时执行断言，不能靠 UI 保证互斥。

### 6.4 随机流拆分

4A 的商店和流程内容共享同一个 `Random`。4B 加入隐藏事件与复杂奖励后应拆分确定性随机流：

- `ShopRandom`：商品与商店法术。
- `EventRandom`：事件池抽取。
- `RewardRandom`：奖励类别和卡牌候选。
- `MapRandom`：为后续随机地图预留，固定地图阶段不消费。

各子种子由单局种子和固定流 ID 派生。玩家多刷新一次商店不应改变之后进入的事件或精英奖励候选。物化后的事件和奖励必须保存在状态中，重载不能再次消费随机数。

子种子派生必须使用固定整数常量和稳定算法，例如 `SeedDeriver.Combine(runSeed, streamId)`；不能使用进程间可能变化的 `string.GetHashCode()`。

### 6.5 操作错误扩展

`RunOperationError` 增加：

- `InvalidChoice`
- `InvalidTarget`
- `NoBenefit`
- `InsufficientPool`
- `ChoiceAlreadyResolved`

UI 根据错误码显示禁用原因，领域层不能只返回自由文本。重复提交已经完成的选择返回 `ChoiceAlreadyResolved` 且状态不变。

## 7. 节点进入与状态机

### 7.1 统一进入流程

`RunSession.EnterNode(nodeId)` 调整为：

1. 验证 `MapSelection`、可达状态、内容引用和经济时钟。
2. 创建通用 `NodeAttemptState`，原子推进一次 `RunTurn`。
3. 按节点类型分发：
   - `Normal`、`Elite`、`Boss`：`Shop.StartRound(runTurn)`，进入 `Shop`。
   - `Event`、`Enhance`、`Rest`：`Shop.AdvanceSkippedRound(runTurn)`，物化选择状态，进入对应 Choice 阶段。
4. 完成后才允许 UI 切换表现。

如果第 3 步无法提交，不能留下已经变为 `Current` 的节点或已经增加但未记录的 `RunTurn`。实现可以使用预验证加不可失败提交，也可以使用内部事务对象；不能依赖异常后手工回滚多个可变对象。

### 7.2 状态转换

| 当前阶段 | 命令/事实 | 下一个阶段 | 节点是否完成 |
| --- | --- | --- | --- |
| `MapSelection` | 进入精英 | `Shop` | 否 |
| `Battle` | 提交精英胜利结果并产生候选 | `RewardChoice` | 否 |
| `Battle` | 提交精英失败/平局且玩家存活 | `BattleResult` | 是，可继续 |
| `RewardChoice` | 选择或跳过精英奖励 | `BattleResult` | 是 |
| `MapSelection` | 进入事件 | `EventChoice` | 否 |
| `EventChoice` | 提交普通事件选项 | `MapSelection` | 是 |
| `EventChoice` | 提交危险招募 | `RewardChoice` | 否，等待随从选择 |
| `RewardChoice` | 完成事件后续奖励 | `MapSelection` | 是 |
| `MapSelection` | 进入锻造 | `EnhanceChoice` | 否 |
| `EnhanceChoice` | 提交配方和目标或离开 | `MapSelection` | 是 |
| `MapSelection` | 进入恢复 | `RestChoice` | 否 |
| `RestChoice` | 提交恢复选项或离开 | `MapSelection` | 是 |

精英战斗结果仍先在 `RunTest` 显示。精英胜利时奖励覆盖层与战斗结果可以同时存在，但地图必须保持不可点击。

## 8. 精英节点

### 8.1 遭遇

4B 只实现第一层精英主题“嘲讽壁垒”：

- 使用现有 `Taunt`、`Shield` 和基础属性。
- `damageBonus = 2`。
- 不增加精英专属战斗逻辑。
- 地图预览显示名称、主题、风险说明“失败伤害修正 +2”和“三选一高价值奖励”。

`EncounterConfig` 增加可选展示字段：

- `theme`
- `riskText`
- `rewardPreviewText`

这些字段只服务地图预览，不参与伤害计算。

### 8.2 结算

- 玩家胜利：生成一次 `PendingRewardChoice`，不立即完成节点。
- 敌方胜利：使用存活数 + 最高等级 +2，扣血后完成节点，无奖励。
- 双方同时倒下或回合上限：固定扣 1，完成节点，无奖励，不叠加 +2。
- 生命归零：直接进入 `RunLost`，不生成候选。

### 8.3 第一批奖励类别

候选池固定包含：

1. 当前酒馆等级的普通随从。
2. 不高于当前酒馆等级的普通商店法术。
3. 下个商店获得 2 次免费刷新。
4. 下个商店额外获得 3 金币。
5. 指定一个战斗区随从永久获得 +1/+2。

生成三个候选时优先使用不同 `category`。运行时排除不合法类别：

- 当前等级没有剩余随从副本时排除随从。
- 没有合法法术时排除法术。
- 战斗区为空时排除目标型永久强化。

免费刷新、额外金币和合法法术保证至少有三个兜底类别。若配置或运行时仍无法物化三个候选，奖励生成整体失败并记录配置错误，不能进入一个无法完成的 `RewardChoice`。

## 9. 阻塞式奖励选择

### 9.1 数据模型

```csharp
public sealed class PendingRewardChoice
{
    public string ChoiceId { get; }
    public string SourceAttemptId { get; }
    public RewardCompletionMode CompletionMode { get; }
    public IReadOnlyList<RewardCandidate> Candidates { get; }
}

public sealed class RewardCandidate
{
    public string CandidateId { get; }
    public string Category { get; }
    public RewardEffect Effect { get; }
    public string CardId { get; }
    public int ReservedPoolCopies { get; }
    public bool RequiresOwnedMinionTarget { get; }
}
```

`CompletionMode` 至少区分：

- `ReturnToBattleResult`：精英战斗奖励完成后显示继续按钮。
- `ResolveNodeToMap`：事件后续奖励完成后直接返回地图。
- `FloorComplete`：为 4C 的 Boss 奖励预留，4B 不启用。

### 9.2 候选生命周期

1. 物化候选时为随从候选从有限牌池保留实体副本。
2. 所有候选连同预留数量写入 `PendingRewardChoice`。
3. 场景重载直接渲染现有候选，不重新抽取。
4. 选择卡牌候选时，选中项进入 FIFO `PendingCardRewards`，未选随从立即返还牌池。
5. 选择资源或目标型奖励时，所有随从候选都返还牌池。
6. 主动跳过时返还全部预留，不提供替代金币。
7. 单局结束时同时清理 `PendingRewardChoice` 和 FIFO 队列的所有预留。

每次返还按 `CandidateId` 或奖励实例 ID 记录，重复回调不能再次返还。

### 9.3 目标型奖励

选择“永久 +1/+2”时，UI 必须同时提交 `candidateId` 和持有随从 `InstanceId`。领域层先验证：

- 候选仍属于当前选择。
- 目标仍在最终战斗区。
- 目标实例与 UI 展示的实例一致。
- 修改具有实际收益。

验证通过后一次提交属性和奖励选择。不能先关闭奖励再等待另一个无身份关联的目标点击。

### 9.4 奖励配置扩展

`RewardTableConfig` 增加：

- `mode`：`AutomaticOne` 或 `ChooseOne`。
- `candidateCount`。
- `preferDistinctCategories`。
- `allowSkip`。

`RewardEntryConfig` 增加：

- `category`
- `attack`
- `health`
- `targetScope`
- `fallbackEntryId`

4A 普通奖励继续使用 `AutomaticOne`，不改变现有行为。

## 10. 锻造台与持有随从修改

### 10.1 统一修改入口

新增 `OwnedMinionModifier`，由商店领域拥有并通过 `ShopSession` 暴露受控方法：

```csharp
OwnedMinionModificationResult TryModifyBattleMinion(
    string instanceId,
    OwnedMinionModification modification);
```

`OwnedMinionModification` 支持：

- 永久攻击/生命增量。
- 授予一个永久关键词。

服务内部通过 `PlayerCollection` 查找战斗区实例，并调用 `ShopCardInstance` 的内部修改能力。`RunSession`、奖励服务和 UI 都不能直接修改 `PermanentKeywords` 集合。

锻造和精英奖励发生在商店关闭期间，因此该修改入口不要求 `ShopSession.IsShopOpen`；是否允许修改由 `RunSession` 当前阶段和来源尝试校验。现有商店法术的永久属性修改也应逐步转调同一服务，避免永久效果存在两条执行路径。

### 10.2 卡牌实例调整

`ShopCardInstance` 增加内部方法：

```csharp
internal bool HasPermanentKeyword(string keyword);
internal bool TryGrantPermanentKeyword(string keyword);
```

保留现有 `ApplyPermanentStats`，但所有外部用途统一经过 `OwnedMinionModifier`。

三连继续沿用现有规则：素材永久属性求和、永久关键词并集，金色随从仍可继续强化。锻造不复制或重写三连逻辑。

### 10.3 锻造状态与命令

`PendingEnhanceChoice` 保存：

- `SourceAttemptId`
- 固定的 3 个 `EnhancementRecipeConfig`
- 当前合法目标的只读展示数据
- `allowSkip`

公开命令：

```csharp
RunOperationResult ApplyEnhancement(string recipeId, string targetInstanceId);
RunOperationResult SkipEnhancement();
```

配方与目标在提交时重新验证，不能只依赖进入节点时的快照。

### 10.4 配方规则

| 配方 | 修改 | 重复规则 |
| --- | --- | --- |
| 均衡锻造 | 永久 +2/+2 | 可重复叠加 |
| 重甲铭刻 | 永久 `Shield` | 已有永久 `Shield` 时无收益、不可提交 |
| 守卫铭刻 | 永久 `Taunt` | 已有永久 `Taunt` 时无收益、不可提交 |

- 只允许最终战斗区随从。
- 金色随从合法。
- 每个锻造节点最多成功提交一次。
- 没有合法目标时仍可离开。
- 关键词白名单第一版仅允许 `Shield` 和 `Taunt`。

## 11. 事件系统

### 11.1 配置结构

新增 `events.v0.1.json`：

```text
EventConfigFile
├── eventPools[]
│   └── eventIds + weights
└── events[]
    ├── id / name / description
    └── options[]
        ├── id / label
        ├── requirements[]
        ├── effects[]
        └── followupRewardTableId
```

第一版只支持白名单需求和效果，不建立通用脚本语言。

需求类型：

- `MinimumHealthAfterCost`
- `MissingHealth`
- `RewardCandidatesAvailable`

效果类型：

- `LoseHealth`
- `HealHealth`
- `NextShopGold`
- `FreeRefresh`
- `UpgradeDiscount`
- `QueueRandomSpell`

最大生命修改只用于恢复点，第一批事件不开放。

### 11.2 事件物化

- 地图只显示通用事件图标和“未知事件”。
- 进入节点时使用 `EventRandom` 从节点引用的事件池抽取一次。
- 具体事件 ID、选项和显示内容写入 `PendingEventChoice`。
- 重载只恢复已经抽到的事件。
- 同一单层只有一个事件节点，因此 4B 不实现同局事件去重；配置保留后续去重标签扩展口。

### 11.3 原子效果事务

`RunEffectTransaction` 分两步：

1. `BuildPlan`：重新验证生命、收益、卡牌候选和当前尝试，预先物化所有可能失败的随机结果或奖励预留。
2. `Commit`：依照计划一次修改生命、延迟资源、奖励状态和节点状态，提交过程不再执行可能失败的查询。

任何验证失败都不扣生命、不发部分资源、不完成节点。

### 11.4 第一批事件映射

| 事件 | 选项 | 技术效果 |
| --- | --- | --- |
| 鲜血契约 | 失去 3 生命，下一商店 +4 金币 | `LoseHealth(3)` + `NextShopGold(4)`；必须保留至少 1 生命 |
| 旧酒馆账本 | 升级折扣 -2 / 免费刷新 1 | `UpgradeDiscount(2)` 或 `FreeRefresh(1)` |
| 遗失的补给车 | 免费刷新 2 / 随机普通法术 | `FreeRefresh(2)` 或物化法术并进入 FIFO 队列 |
| 星泉 | 恢复 4 / 下一商店 +2 金币 | 满生命时禁用恢复；另一项始终合法 |
| 危险招募 | 失去 2 生命，当前酒馆等级随从三选一 | 先成功保留三个候选，再扣生命并进入 `RewardChoice` |

每个事件都包含“离开，不发生任何事”或至少一个无条件合法选项，保证不会死锁。

危险招募要求三个互不重复的当前等级随从候选。如果牌池不足，风险选项在提交前显示为不可用，已经临时保留的部分候选必须立即返还，且不能扣血。

### 11.5 事件命令

```csharp
RunOperationResult SelectEventOption(string eventId, string optionId);
```

命令同时验证 `EventChoice`、尝试 ID、当前物化事件和选项合法性。UI 不传递生命或奖励数值，避免客户端显示数据成为实际结算输入。

## 12. 恢复点

### 12.1 配置与状态

新增 `rests.v0.1.json`，第一版只有一个 `RestNodeConfig`：

- `heal_6`
- `max_health_2_heal_2`
- `leave`

`PendingRestChoice` 保存配置 ID、尝试 ID和选项展示状态。

### 12.2 提交规则

公开命令：

```csharp
RunOperationResult SelectRestOption(string optionId);
```

- `heal_6`：实际恢复 `min(6, MaxHealth - Health)`；满生命时不可提交。
- `max_health_2_heal_2`：先将 `MaxHealth += 2`，再将 `Health = min(MaxHealth, Health + 2)`；满生命时仍合法。
- `leave`：无效果但完成节点。
- 选项提交和节点完成是一个事务。

恢复点复用 `RunEffectTransaction` 的生命修改能力，但保留独立 `RestChoice` 阶段和配置类型，使 UI 语义与事件保持区分。

## 13. 非战斗节点经济与幂等

进入事件、锻造或恢复点时：

1. 预验证 `Shop` 已关闭且 `LastEconomyTurn == RunTurn`。
2. 新尝试将 `RunTurn` 增加 1。
3. 调用 `Shop.AdvanceSkippedRound(newRunTurn)`。
4. 成功后设置 `EconomyTurnCommitted = true`。
5. 物化节点内容并进入对应选择阶段。

场景重载不会再次调用 `AdvanceSkippedRound`。如果恢复逻辑发现 `RunTurnCommitted = true` 但 `EconomyTurnCommitted = false`，应将其视为非法中间状态并记录错误，而不是猜测是否需要再次降价。4B 仍是内存单局，正常领域命令必须在一个同步调用内完成这两个提交。

非战斗节点：

- 不重置当前金币。
- 不生成或清除商店商品。
- 不清除冻结。
- 不应用“下个商店”延迟资源。
- 只推进自然升级降价和 `LastEconomyTurn`。

## 14. 配置与校验

### 14.1 新增或扩展资源

- 扩展 `run-maps.v0.1.json`：7 节点 4B 地图。
- 扩展 `encounters.v0.1.json`：安全普通遭遇和“嘲讽壁垒”精英。
- 扩展 `rewards.v0.1.json`：精英 `ChooseOne` 奖励表、危险招募随从奖励表。
- 新增 `events.v0.1.json`：事件池和 5 个事件。
- 新增 `enhancements.v0.1.json`：3 个配方和锻造节点配置。
- 新增 `rests.v0.1.json`：恢复点配置。

### 14.2 地图校验

- 7 个节点 ID 唯一，所有引用存在，无环。
- 唯一起点仍为强制普通首战。
- 精英路线和安全路线都能到达唯一 Boss。
- 每条合法路线恰好访问 4 个节点。
- 每条主路线恰好包含 3 个战斗节点。
- 每层最多一个锻造节点。
- `payloadId` 根据节点类型引用正确内容集合。

“恰好 4 节点”和“3 次战斗”是 4B 验证地图的专属校验，不应硬编码为所有未来地图的通用规则。可以通过地图配置中的 `validationProfile = Phase4BPrototype` 启用。

### 14.3 内容校验

精英：

- `category = Elite`、`damageBonus = 2`。
- 敌方随从均存在、启用且只使用当前支持的关键词。
- 奖励表为 `ChooseOne`，候选数量为 3。

锻造：

- 配方 ID 唯一。
- `ModifyStats` 至少一个属性非零。
- `GrantKeyword` 仅允许 `Shield`、`Taunt`。
- 固定节点恰好引用 3 个不同配方。

事件与恢复：

- 选项 ID 在内容内唯一。
- 效果和需求类型位于白名单。
- 生命代价配置保证领域层可检查“至少保留 1”。
- 每个事件至少有一个无条件合法的退出或收益选项。
- 所有后续奖励表存在且类型匹配。

奖励：

- `ChooseOne` 的候选数大于 1。
- 类别、目标范围和效果字段组合合法。
- 目标型奖励必须声明正收益。
- 危险招募只生成当前酒馆等级、非 Token、非金色随从。

## 15. UI 方案

4B 继续使用 `RunTest`，不新增独立 Event/Forge/Rest 场景。

### 15.1 地图

- 7 个节点按固定连线布局。
- 普通、精英、事件、锻造、恢复和 Boss 使用不同颜色与文字标识。
- 精英显示主题、失败 +2 修正和奖励提示。
- 事件在进入前只显示通用名称，不泄露具体事件。
- 选择阶段、奖励阶段和战斗结果未处理时禁用全部地图按钮。

### 15.2 奖励覆盖层

- 同时显示 3 个候选和“跳过”。
- 卡牌显示名称、等级、属性和描述。
- 资源显示生效时机，例如“下个商店 +3 金币”。
- 目标型奖励选择后显示当前战斗区随从，按 `InstanceId` 提交。
- 候选不合法时不生成按钮，而不是点击后才报错。

### 15.3 事件覆盖层

- 显示物化后的事件名称、说明和 2-3 个选项。
- 每个选项明确列出代价、收益和禁用原因。
- 危险招募提交后切换到奖励覆盖层。

### 15.4 锻造覆盖层

- 左侧显示三个固定配方。
- 右侧显示最终战斗区随从及永久关键词。
- 重复关键词目标置灰并说明“已拥有该永久关键词”。
- 始终提供离开按钮。

### 15.5 恢复覆盖层

- 显示当前/最大生命。
- 满生命时“恢复 6”置灰。
- “最大生命 +2 并恢复 2”和离开保持可用。

所有覆盖层都从 `RunState` 重建。关闭和重新打开 `RunTest` 不得重新抽候选或重复提交效果。

## 16. 自动化测试

### 16.1 EditMode

地图与经济：

- 4B 固定地图 7 节点、两条主路线和三次商店机会合法。
- 非战斗节点推进一次 `RunTurn` 和自然升级降价，但不补货、不应用延迟资源、不清除冻结。
- 同一节点尝试重载不会重复推进经济时钟。

精英与奖励：

- 精英胜利生成三个合法、优先不同类别的候选并阻塞节点。
- 精英失败使用 +2 修正，平局固定 1 点。
- 候选选择、跳过、牌池保留和返还保持守恒。
- 目标型奖励需要合法战斗区实例且只提交一次。
- 商店刷新次数不会改变同一种子的事件与奖励随机流。

锻造：

- +2/+2 可对同一实例跨节点重复叠加。
- `Shield`、`Taunt` 不能重复授予。
- 备战区目标、过期实例 ID、非法关键词和零收益修改不生效。
- 金色随从继承素材强化并仍可继续强化。

事件：

- 五个事件配置和选项均能物化。
- 生命代价至少保留 1，失败时不发部分收益。
- 满生命的星泉恢复不可选。
- 危险招募候选不足时不扣血并返还临时预留。
- 延迟资源和随机法术只提交一次。

恢复：

- 恢复不超过最大生命。
- 深度休整先增加最大生命再恢复。
- 满生命不能选择恢复 6，但可以深度休整或离开。
- 重复提交不会再次恢复或增加最大生命。

### 16.2 PlayMode

- 高风险路线完成“首战 → 精英 → 三选一 → 锻造 → Boss”。
- 稳定路线完成“首战 → 普通 → 事件 → Boss”。
- 稳定路线完成“首战 → 普通 → 恢复 → Boss”。
- 精英奖励、事件、锻造和恢复覆盖层在场景重载后保持原候选和原事件。
- 非战斗节点不会跳转到 `ShopTest` 或 `BattleTest`。
- 目标型奖励与锻造修改会出现在后续商店和战斗快照中。
- 4A 已有胜利、失败、Boss 重试和卡牌领取路径继续通过。

现有 EditMode 60 / 60、PlayMode 6 / 6 必须全部保留；4B 测试在此基础上增加。

## 17. 实施顺序

1. 泛化 `NodeAttemptState`，增加 Choice 阶段和四类 Pending Choice 模型。
2. 拆分确定性随机流，确保商店操作不影响事件和奖励。
3. 扩展配置模型、加载器和校验器，落地 7 节点固定地图。
4. 接通非战斗节点的 `AdvanceSkippedRound` 和幂等进入流程。
5. 实现 `OwnedMinionModifier` 与永久关键词安全入口。
6. 实现通用阻塞奖励候选、目标型奖励和牌池预留生命周期。
7. 实现精英遭遇和精英胜负/奖励流程。
8. 实现事件配置、效果事务和 5 个事件。
9. 实现锻造台与恢复点。
10. 扩展 `RunTest` 地图和四类覆盖层。
11. 补齐 EditMode、PlayMode 测试并执行全量回归。
12. 同步 TODO、开发计划和技术方案实现状态。

优先实现统一修改入口和奖励服务，再实现具体节点，避免精英奖励、危险招募和锻造分别复制卡牌修改与牌池逻辑。

## 18. 4B 完成标准

- 单层地图显示 7 个节点，每条路线访问 4 个节点并到达 Boss。
- 高风险路线和稳定路线拥有相同节点长度及商店次数。
- 精英胜利进入阻塞式三选一，失败/平局按规则扣血并继续。
- 五个事件、三个锻造配方和恢复点三个选项均可从配置运行。
- 非战斗节点正确推进 `RunTurn` 和自然升级降价，不触发商店。
- 所有选择和永久修改按节点尝试最多提交一次。
- 随从候选的牌池副本在选择、跳过、失败和单局结束时保持守恒。
- 锻造和目标型奖励的永久效果能进入后续商店实例与战斗快照。
- 场景重载不改变事件、奖励候选、配方或节点结算。
- 4A 与 4B 全部自动化测试通过。
- 文档、配置字段和代码命名一致。

## 19. 当前结论

4B 技术方案不存在阻塞开发的规则空缺，以下决策作为实现基线：

- 使用 7 节点固定地图，高风险路线为精英 → 锻造，稳定路线为普通 → 事件/恢复。
- 两条主路线均为 4 个访问节点、3 次战斗商店。
- 非战斗节点只推进经济时间，不应用延迟商店资源。
- 精英奖励使用物化后的阻塞式三选一，并显式管理候选牌池预留。
- 锻造和目标型奖励共享 `OwnedMinionModifier`。
- 事件使用白名单效果事务，不建立通用脚本 DSL。
- 事件、奖励、商店使用相互独立的确定性随机流。
- 第一、二层 Boss 奖励能力复用 4B 奖励服务，但等到 4C 有下一层消费场景时启用。

## 20. 实施结果

阶段 4B 已按本方案落地：

- 固定地图已扩展为 7 个可见节点，两条主路线均访问 4 个节点并获得 3 次战斗商店机会。
- 节点尝试已泛化，新增奖励、事件、锻造和恢复四类阻塞选择状态；非战斗节点使用显式跳过回合推进经济。
- 商店、奖励和事件使用独立的确定性随机流，选择状态物化后可跨 `RunTest` 重载保持。
- 精英“嘲讽壁垒”、阻塞式三选一、候选预留/返还、目标型永久属性奖励均已接通。
- 五个事件、危险招募后续三选一、三个锻造配方和三个恢复选项均已配置化运行。
- `RunTest` 已显示完整地图，并复用统一覆盖层处理四类选择；非战斗节点不切换到商店或战斗场景。
- 配置校验已覆盖非战斗节点引用、事件效果白名单、后续奖励、锻造配方和恢复选项。
- 全量自动化结果：EditMode 69 / 69、PlayMode 9 / 9 通过。

Unity 人工操作与表现验收继续按既定计划延后，待阶段 2-4 全部功能完成后集中执行。
