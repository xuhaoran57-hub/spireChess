# 阶段 5A 通用效果系统技术方案 v0.1

版本：0.1  
状态：技术方案已完成，待实施  
关联文档：`json-schema-design-v0.1.md`、`phase-3-build-design-v0.1.md`、`minion-design-v0.1.md`、`spell-design-v0.1.md`

## 1. 目的

阶段 5A 把阶段 1-4 中分散在 `ShopSession` 与 `BattleSimulator` 内的少量硬编码效果，扩展为确定性、可验证、可回放的通用效果执行框架，为阶段 5B 的 1-3 级内容和阶段 5C 的完整内容池提供统一基础。

本阶段优先解决以下问题：

- `effects` 为空时卡面文案无法兑现；
- 商店触发、战斗触发与跨阶段延迟效果缺少共同语义；
- `OnBattleStart`、`OnRefresh`、`OnSpellUsed`、护盾变化和召唤/死亡联动尚未形成统一队列；
- 临时、下一战和永久修改缺少清晰的所有权与回写边界；
- 复杂卡牌若逐张写分支，会快速破坏测试和结算顺序。

5A 的目标不是一次性填完全部卡牌配置，而是让后续卡牌主要通过 JSON 组合已有触发、目标、条件和动作完成。

## 2. 范围

### 2.1 包含

- 商店与战斗共享的效果定义、校验、条件判断、目标选择和执行结果模型；
- 商店触发队列与战斗触发队列；
- `Manual`、`OnPlay`、`OnSell`、`OnRefresh`、`OnShopPhaseStart`、`OnShopPhaseEnd`；
- `OnBattleStart`、`OnAttackBefore`、`OnKill`、`OnShieldGained`、`OnShieldLost`、`OnSummon`、`OnDeath`、`OnFriendlyDeath`、`OnSummonedUnitDeath`、`OnCombatEnd`；
- `ModifyStats`、`AddShield`、`RemoveShield`、`AddKeyword`、`DealDamage`、`GainGold`、`ScheduleGold`、`FreeRefresh`；
- `DiscoverMinion`、`DiscoverSpell`、`GrantRandomSpell`、`CopyMinion`、`SummonToken`、`ImmediateAttack`、`ActivateCardListeners`、`SetPendingCombatBuff`；
- `Permanent`、`Combat`、`NextCombat`、`ShopPhase` 四类持续时间；
- 玩家选择、随机、最低攻击/生命、相邻、最左/最右、全体和种族筛选；
- 每商店、每战斗、每局触发次数限制；
- 效果日志、确定性随机、循环保护和自动化测试基座。

### 2.2 不包含

- 持续存在并随站位实时重算的光环；
- 战斗中由玩家主动施法；
- 复杂遗物、装备、局外成长；
- 任意脚本表达式或在 JSON 中执行代码；
- 正式动画、音效和美术表现；
- 完整 52 个随从和 15 张普通法术的配置填充，该工作分属 5B/5C；
- `Run` 持续时间的通用叠层效果，5A 只保留字段与校验入口。

## 3. 现有基础与缺口

现有能力：

- `ShopSession` 已支持 `OnPlay`、`Manual`、永久属性、金币、免费刷新和三连发现；
- `BattleSimulator` 已支持攻击、护盾抵挡、溅射、亡语召唤与召唤后立即攻击；
- `RunSession` 已有延迟商店资源、奖励队列和 `SourceInstanceId`；
- `EffectConfig` 已包含 trigger、action、target、value、condition、limit、discover 和 fallback；
- 商店操作已经采用“先验证、后提交”的原子操作原则。

主要缺口：

- 商店效果计划只识别 3 个动作，且写在 `ShopSession` 内；
- 战斗只在死亡时扫描 `OnDeath/SummonToken`，没有通用事件队列；
- 护盾变化、攻击者、死亡对象、召唤来源等触发上下文未建模；
- `NextCombat` 状态无法从持有实例进入战斗快照并一次性消费；
- 战斗内产生的永久修改没有安全回写持有实例的差量模型；
- 发现状态只支持三连随从发现，不能复用到普通法术、战吼和种族选择；
- 当前配置校验只覆盖字段合法性，未覆盖“触发 × 动作 × 阶段”的组合合法性。

## 4. 总体架构

效果定义共享，但商店和战斗分别拥有执行器。两者状态所有权不同，不把所有逻辑塞进一个巨型类。

```text
EffectConfig / ConfigValidator
              |
       EffectDefinitionCatalog
              |
     +--------+---------+
     |                  |
ShopEffectEngine   BattleEffectEngine
     |                  |
ShopSession        BattleSimulator
     |                  |
Owned instances    Battle runtime clones
     +---------+--------+
               |
       EffectResolutionLog
```

共享组件：

- `EffectDefinitionCatalog`：合法触发、动作、条件、持续时间及组合表；
- `EffectConditionEvaluator`：只读判断条件；
- `EffectTargetResolver`：根据上下文生成稳定排序的候选；
- `EffectUsageTracker`：记录每商店、每战斗和每局次数；
- `EffectResolution`：描述已确定目标和数值的不可变结果；
- `EffectResolutionLog`：供测试、UI 和调试使用，不反向修改领域状态。

领域专属组件：

- `ShopEffectEngine`：处理持有实例、金币、刷新、发现、复制和下一战状态；
- `BattleEffectEngine`：处理运行时随从、伤害、护盾、召唤、死亡和立即攻击；
- `BattlePermanentDelta`：记录战斗中明确允许永久回写的差量；
- `PendingCombatModifier`：记录下一场战斗一次性消费的增益。
- `ActiveShopEffect`：记录法术消耗后继续存在到商店结束的监听效果。

## 5. 核心数据模型

### 5.1 `EffectExecutionContext`

每次执行必须显式携带上下文，禁止执行器从全局单例读取状态。

建议字段：

| 字段 | 说明 |
| --- | --- |
| Phase | `Shop` 或 `Battle` |
| Trigger | 当前触发类型 |
| SourceConfigId | 来源配置 ID |
| SourceInstanceId | 来源持有实例或战斗实例 ID |
| SourceSide | 玩家或敌方 |
| SubjectInstanceId | 事件主体，例如失盾或死亡单位 |
| RelatedInstanceId | 攻击者、召唤来源或法术目标 |
| PlayerChoice | 玩家提交的槽位、候选或种族 |
| ShopStats | 刷新、购买和法术使用计数的只读快照 |
| Random | 当前系统专属的确定性随机流 |
| Sequence | 本次结算中的单调递增序号 |

上下文创建后不可在效果执行过程中改变。后续触发使用新事件创建新上下文。

### 5.2 `EffectEvent`

效果事件是触发队列中的最小单位：

- `Trigger`；
- 来源、主体、相关对象；
- 所属阵营；
- 触发载荷，例如伤害值、刷新次数、是否召唤物；
- `Sequence` 和 `Depth`；
- 产生事件的 `effectId`，便于诊断循环。

事件入队时只记录事实，不立即递归调用其他效果。

### 5.3 `EffectResolution`

执行前先把不确定性物化：

- 已选目标实例 ID 列表；
- 已确定的随机结果；
- 解析后的数值和持续时间；
- 将产生的状态变更；
- 将排入队列的后续事件；
- 失败原因或跳过原因。

玩家主动法术要求全部必要效果都能生成 resolution 后才提交。被动触发若没有合法目标则记录跳过，不回滚此前已经完成的独立事件。

### 5.4 `ShopPhaseStats`

由 `ShopSession` 持有并在每个商店阶段重置：

- `RefreshCount`；
- `SpellUsedCount`；
- `SpellBoughtCount`；
- `MinionBoughtCount`；
- `SummonedUnitDeathCount` 只属于战斗，不放入此模型；
- 按 `effectId + sourceInstanceId` 记录的 `perShop` 次数。

只有主动付费或免费刷新增加 `RefreshCount`。自动开店、冻结补货和场景重载不增加。

### 5.5 `PendingCombatModifier`

下一战效果存放在持有随从实例或单局全队状态中，不直接修改永久关键词。

建议字段：

- 来源 `effectId`；
- 目标持有实例 ID，或 `AllAllies`/种族筛选；
- 攻击、生命、关键词、护盾；
- 消费时机固定为下一次成功创建战斗快照；
- 是否要求目标仍在最终战斗区；
- 是否已消费。

创建战斗快照时只复制适用于最终阵容的 modifier。成功创建后原状态立即标记消费，场景重载不得重复应用。

### 5.6 `BattlePermanentDelta`

战斗内只有配置明确写 `duration: Permanent` 的效果才能产生永久差量：

- `SourceInstanceId`；
- 攻击/生命差量；
- 永久关键词集合；
- 来源 effect ID 和应用次数。

战斗结束后由 `RunSession` 统一提交到仍存在的持有实例。Token、敌方和缺少 `SourceInstanceId` 的对象不能产生玩家永久回写。

### 5.7 `ActiveShopEffect`

部分法术使用后仍需监听本商店后续事件，例如星辉回款。成功使用时把同一卡牌中非 `Manual` 的效果复制为运行时监听状态：

- 来源法术实例和配置 ID；
- 激活时的商店计数基线；
- 后续 trigger 与 effect ID；
- 已触发次数和 `perShop` 上限；
- 固定持续到当前商店结束。

法术卡本身仍立即消耗。监听状态不是卡牌，不占备战区，也不能跨商店保存。

## 6. 触发队列与结算顺序

### 6.1 通用规则

- 使用 FIFO 队列；
- 同一事件内，来源按槽位从左到右；
- 同槽位下按配置中的 effects 顺序；
- 普通随从只执行 `effects`，金色随从只执行 `goldenEffects`；
- 每个效果完成状态提交后，才把衍生事件加入队尾；
- UI 通知只在领域状态稳定后发送；
- 随机选择只使用对应系统的随机流，UI 刷新和日志不得消耗随机数。

### 6.2 商店操作顺序

一次购买、上场、出售、刷新或施法：

1. 验证阶段、资源、空间和玩家选择；
2. 为主动效果生成完整执行计划；
3. 提交原始操作；
4. 入队原始事件，例如 `OnPlay`、`OnSpellUsed`；
5. 处理商店效果队列直到稳定；
6. 逐个结算三连，每次合成后重新入队相关事件；
7. 处理新事件直到稳定；
8. 发出 UI 与调试日志。

主动法术失败时不消耗法术、不改变目标、不增加 `SpellUsedCount`。

### 6.3 战斗开始顺序

1. 从持有实例创建双方战斗快照；
2. 应用并消费 `PendingCombatModifier`；
3. 玩家侧按槽位从左到右入队 `OnBattleStart`；
4. 敌方侧按槽位从左到右入队 `OnBattleStart`；
5. 处理所有开始事件、伤害、死亡、召唤和立即攻击；
6. 若双方仍有存活单位，进入第 1 轮普通攻击。

战斗开始效果可能在首次普通攻击前直接结束战斗。

### 6.4 攻击与伤害顺序

单次攻击保持现有主体规则，并增加事件边界：

1. 确定攻击者和目标；
2. 处理明确的攻击前效果，例如移除目标护盾；
3. 计算主目标、反击和溅射伤害；
4. 逐个提交护盾抵挡或生命扣减；
5. 每次护盾获得/失去分别入队事件；
6. 收集本批死亡并清空槽位；
7. 按死亡发生顺序入队 `OnDeath` 与友方死亡观察事件；
8. 处理召唤、召唤失败补偿和立即攻击；
9. 队列稳定后判断胜负并生成播放步骤。

同一批伤害中的死亡先全部物化，再执行亡语，避免亡语结果改变同批死亡判定。

### 6.5 循环保护

- 单次商店命令最多处理 256 个效果事件；
- 单场战斗最多处理 2048 个效果事件；
- 最大派生深度 32；
- 超限时记录完整 effect 链并以 `EffectLoopDetected` 结束当前模拟；
- 配置校验应尽量提前拒绝显然的无成本自触发闭环。

自动化测试中超限必须失败；正式原型中不得静默吞掉循环。

## 7. 动作语义

| 动作 | 商店 | 战斗 | 关键语义 |
| --- | --- | --- | --- |
| ModifyStats | 是 | 是 | 按 duration 写入永久实例、战斗运行时或下一战状态 |
| AddShield | 是 | 是 | 商店中只能写永久关键词或 NextCombat；战斗中修改运行时护盾 |
| RemoveShield | 否 | 是 | 只在实际有护盾时产生 `OnShieldLost` |
| AddKeyword | 是 | 是 | `Permanent`、`Combat`、`NextCombat`；不允许任意字符串 |
| DealDamage | 否 | 是 | 必须走统一伤害入口，不能直接减生命 |
| GainGold | 是 | 否 | 当前商店立即获得，可超过基础预算 10 |
| ScheduleGold | 是 | 否 | 写入下一商店延迟资源，多张可叠加 |
| FreeRefresh | 是 | 否 | 增加当前商店免费刷新次数 |
| DiscoverMinion | 是 | 否 | 受有限牌池约束并进入阻塞选择状态 |
| DiscoverSpell | 是 | 否 | 从已开放法术无限池生成不重复候选 |
| GrantRandomSpell | 是 | 否 | 物化一张符合筛选条件的法术，可标记为当前商店临时卡 |
| CopyMinion | 是 | 否 | 生成普通基础副本并额外占用 1 个实体牌池副本 |
| SummonToken | 否 | 是 | 使用死亡原位/最近空位规则 |
| ImmediateAttack | 否 | 是 | 入队额外攻击，不消耗本轮普通行动资格 |
| ActivateCardListeners | 是 | 否 | 激活来源卡牌的非 Manual 商店监听效果 |
| SetPendingCombatBuff | 是 | 否 | 写入下一战一次性 modifier |

`CopyMinion` 在 5A 固定规则：

- 目标必须是玩家最终战斗区的普通或金色 1-2 级非 Token 随从；
- 获得的是同配置的普通基础副本，不复制金色、永久加成、关键词和触发计数；
- 必须有备战区空位和剩余实体副本；
- 验证成功后才同时占用牌池并生成卡牌；
- 任一条件失败时法术保留。

## 8. 条件、目标与选择

### 8.1 条件

5A 支持：

- `None`；
- `HasKeyword`、`HasShield`、`TargetAlreadyHasShield`；
- `IsGolden`；
- `SubjectIsToken`、`SubjectIsNonToken`、`SubjectRace`；
- `RaceCountAtLeast`；
- `PhaseStatAtLeast`；
- `IsMostCommonMainRace`；
- `HasGoldenMinion`；
- `CombatWon`、`AttackerExists`；
- `NoBoardSpace` 仅供召唤失败 fallback 使用。

条件只读，不得在判断时抽随机数或改变计数。

### 8.2 目标稳定排序

所有候选先按以下顺序排序，再应用 selector：

1. 玩家侧在敌方侧之前，除非上下文只允许单侧；
2. 战斗区槽位从左到右；
3. 备战区只在明确允许的发现/复制容器操作中使用；
4. 相同数值按槽位和实例 ID 稳定排序。

随机 selector 从稳定候选列表中使用一次随机调用。

### 8.3 统一阻塞选择

将当前 `ShopDiscoverState` 泛化为 `PendingEffectChoice`：

- `ChoiceType`：`Card`、`BattleTarget`、`Race`；
- 来源卡牌和 effect ID；
- 物化后的候选；
- 已预留的牌池副本；
- 取消策略；
- 完成后的后续效果索引。

取消策略必须由来源显式决定：普通效果选择可以允许取消；三连奖励发现固定为不可取消，避免通过取消和重新使用反复刷新候选。

进入选择状态后，购买、出售、刷新、升级、冻结、移动、其他法术和结束商店全部阻塞。候选必须物化保存，场景重载不得重新抽取。

## 9. 持续时间与状态所有权

| duration | 所有者 | 重置/消费时机 |
| --- | --- | --- |
| Permanent | `ShopCardInstance` 或 `BattlePermanentDelta` | 单局结束 |
| Combat | `BattleMinionRuntime` | 当前战斗结束 |
| NextCombat | `PendingCombatModifier` | 下一次成功创建战斗快照 |
| ShopPhase | `ShopSession`/效果监听状态 | 当前商店结束 |
| Run | 暂不通用实现 | 预留 |

禁止把 `Combat` 属性写回持有实例，禁止把 `NextCombat` 直接加入永久关键词集合。

## 10. 配置校验

`ConfigValidator` 增加以下校验：

- trigger、action、condition、duration 必须属于支持集合；
- trigger 与 action 必须允许在同一阶段组合；
- 需要 target/value/discover 的动作必须提供对应字段；
- `PlayerChoice` 只能用于主动操作或可进入阻塞选择的效果；
- `Permanent` 不能作用于 Token、敌方或缺少持有实例映射的目标；
- `NextCombat` 只能从商店/奖励阶段创建；
- `perShop`、`perCombat`、`perRun` 不得为负数；
- effect ID 在单个普通或金色效果列表内唯一；
- fallback 只允许白名单动作，且不得再次包含 fallback；
- 已开放进入商店或牌池的卡牌必须有可执行效果，或明确标记为纯身材卡；
- 发现配置的等级、数量、pick 和牌池范围合法。

不支持的组合在资源加载时直接报错，不允许运行到玩家操作时才返回 `UnsupportedEffect`。

## 11. 建议代码布局

```text
Assets/Scripts/Effects/
  EffectDefinitionCatalog.cs
  EffectExecutionContext.cs
  EffectEvent.cs
  EffectResolution.cs
  EffectUsageTracker.cs
  EffectConditionEvaluator.cs
  EffectTargetResolver.cs
  PendingEffectChoice.cs

Assets/Scripts/Shop/Effects/
  ShopEffectEngine.cs
  ShopEffectPlanner.cs
  ShopPhaseStats.cs
  ActiveShopEffect.cs
  PendingCombatModifier.cs

Assets/Scripts/Battle/Effects/
  BattleEffectEngine.cs
  BattleEventQueue.cs
  BattlePermanentDelta.cs
  BattleEffectFailure.cs
```

`ShopSession` 和 `BattleSimulator` 继续负责流程编排，不再各自解释 JSON 字段。MonoBehaviour 只展示 resolution 日志和提交玩家选择。

## 12. 自动化测试

### 12.1 EditMode

- 每种 trigger 的入队时机与稳定顺序；
- 每种 action 的合法阶段、目标和持续时间；
- 玩家主动法术先验证后提交，失败不产生部分变化；
- `NextCombat` 只消费一次，场景重载不重复；
- `Combat` 不回写，`Permanent` 按 SourceInstanceId 回写；
- 护盾获得/失去事件只在状态实际变化时触发；
- 同批死亡先物化后结算亡语；
- 召唤、失败 fallback 和立即攻击顺序；
- `perShop`、`perCombat`、`perRun` 限制；
- 发现/复制的牌池预留与释放；
- 随机流在相同种子和命令序列下完全一致；
- 循环保护能报告 effect 链。

### 12.2 PlayMode

- 商店触发日志与卡面状态同步；
- 目标/种族/普通发现三类阻塞选择可按配置完成或取消；三连奖励发现只能完成、不能取消；
- 商店结束后下一战增益正确显示并只生效一次；
- 战斗开始、护盾、召唤、死亡触发逐步播放可见；
- 返回地图和下一商店后永久状态与临时状态边界正确；
- 场景重载不会重复订阅或重复触发。

## 13. 实施顺序

1. 建立共享 definition catalog、context、resolution 和 usage tracker；
2. 从 `ShopSession` 提取现有 `ModifyStats/GainGold/FreeRefresh`；
3. 泛化阻塞选择，保持三连发现回归通过；
4. 接入 `ScheduleGold`、`CopyMinion`、`DiscoverSpell` 和 `NextCombat`；
5. 为战斗建立事件队列并迁移现有亡语召唤；
6. 接入战斗开始、护盾变化、召唤/死亡观察事件；
7. 增加永久差量回写和战斗结束触发；
8. 完成配置组合校验、循环保护与日志；
9. 跑阶段 2-4 全量回归，再交付 5B 使用。

## 14. 完成标准

- 现有 73 个 EditMode 和 13 个 PlayMode 测试保持通过；
- 应急补给、临时护符、复制雏形可仅通过配置和通用动作实现；
- 铸魂盾侍、炉心火种、星尘随侍等代表性触发不需要卡牌 ID 特判；
- 商店与战斗效果使用同一套字段语义和校验表；
- 相同种子与命令序列得到相同日志、目标和最终状态；
- 不存在未报告的递归死循环、部分提交或临时状态永久化；
- 未实现卡牌继续保持原型提示，不得因框架上线而自动开放。

## 15. 关键决策

- 共享定义，不共享状态所有权：商店和战斗使用两个执行器；
- 所有衍生触发走 FIFO 队列，不使用深层递归；
- 主动操作全量预验证，被动触发允许单个效果无目标时跳过；
- 战斗开始时先物化双方全部 `OnBattleStart` 事件，再按玩家从左到右、敌方从左到右结算；事件已物化后不因来源提前死亡而取消；
- 战斗中明确标记为永久的效果在触发成功时即产生永久差量，不因单位随后死亡或战斗失败撤销；明确要求胜利或存活的效果除外；
- `NextCombat` 在成功创建战斗快照时消费；
- 战斗永久变化使用差量回写，Token 和敌方永不回写；
- 光环继续暂缓，避免在 5A 同时引入实时依赖图。
