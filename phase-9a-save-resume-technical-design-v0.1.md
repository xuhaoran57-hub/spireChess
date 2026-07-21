# 阶段 9A 正式启动流程与单局存档恢复技术方案 v0.1

- 日期：2026-07-21
- 状态：代码与自动化完成，待真实进程人工验收
- 实现基线：`eb1cdc7 feat(ui): finalize battle and run screens`
- 内容版本：5.5.0
- 最低规则版本：8B.1
- 完整配置哈希：`818596be90de4e2ddf6c4b7f9ba0b6e1fee994fcc31ec9893652e02f49ef4311`

实施结果（2026-07-21）：

- 已完成正式 MainMenu、单槽位存档恢复、四条可重放随机流、完整 DTO 快照、原子主备存储、应用层保存协调器和阶段路由。
- 全量 EditMode 264 / 264、PlayMode 22 / 22 通过。
- 1920×1080、1920×1200 MainMenu 与 1920×1080 确认弹窗截图检查通过。
- 尚需按第 16 节执行真实进程退出、战斗播放中断和手工破坏主备文件验收；通过前不建立 9A 候选提交。

## 1. 背景

阶段 7B、8A、8B 已完成正式战斗、商店、地图和遗珍 UI，三层单局、Boss 重试、事件额外战斗及阻塞选择也已通过自动化和人工验收。当前运行路径仍有三个原型遗留：

1. `GameApp.Initialize()` 每次启动都直接 `StartNewRun()`，`MainMenu` 只有空场景并被自动跳转到 `RunTest`。
2. `SaveService` 只做普通文本覆盖；启动时写入的 `TestSaveData` 只是烟测，不是领域存档，也没有原子写、备份、校验或版本兼容。
3. `RunSession`、`ShopSession` 和 `RelicService` 的可变状态分散在私有字段中，并持续消费四条 `System.Random` 流；只保存 seed 会导致恢复后的商品、奖励、事件和遗珍发生变化。

阶段 9A 将现有正式单局从“进程内可玩”推进到“可以从正式菜单开始、退出并可靠继续”。本阶段不改变卡牌、地图、遗珍、战斗或经济规则。

## 2. 目标与非目标

### 2.1 目标

1. 建立正式 `MainMenu`，提供“新游戏”“继续游戏”“设置占位”“退出”。
2. 支持一个本地单局槽位，在所有稳定 `RunPhase` 下保存并恢复。
3. 保存后继续不得重抽地图、商品、奖励、事件、发现、遗珍或战斗结果。
4. 保存后继续不得重复推进 `ShopTurn`、扣除生命、发放奖励、结算战斗或触发遗珍。
5. 使用主档、临时档、备份档和 SHA-256 校验，明确处理写入中断、主档损坏和版本不兼容。
6. 将场景跳转集中到状态路由器，Controller 不再自行推导恢复目标场景。
7. 为后续多存档、云存档和 schema 迁移保留接口，但不在 v1 实现这些功能。

### 2.2 非目标

- 多槽位、跨设备同步、Steam Cloud 或微信云存档。
- 局外收藏、账号、成就或跨局成长。
- 存档加密、防修改或竞技反作弊；校验和只用于发现意外损坏。
- 保存战斗动画播放到第几帧、当前高亮或拖拽中的 UI 状态。
- 在本阶段重命名 `RunTest`、`ShopTest`、`BattleTest` 资源；这些名字只作为内部场景 ID，玩家界面不显示 `Test`。
- 为任意旧开发快照做尽力迁移。9A 之前没有正式公开存档，因此 v1 可以严格拒绝未知格式。
- 恢复 R17 Phase 6B 的旧 S1/S2。

## 3. 已冻结结论

- v1 只有一个正式单局槽位：`run-slot-0.json`。
- schema 使用整数版本 `1`，格式标识固定为 `spire-chess-run`。
- 存档只保存 DTO，不直接序列化领域对象、Unity 对象、配置对象或私有字段。
- 配置只保存 ID；恢复时从当前 `ConfigService` 重新绑定，并要求完整配置哈希完全一致。
- 当前固定地图保存 `mapId` 和全部节点状态，不复制配置中的地图文案和遭遇内容。
- 所有随机候选一旦物化就保存具体候选；恢复不能重新生成。
- 持续随机流保存初始 seed 和带返回值的调用日志，恢复时重放并校验，保持当前 `System.Random` 行为不变。
- 战斗播放期间不生成中间快照。异常退出后回到战斗开始前；结算提交后则恢复到 `BattleResult` 或后续阻塞选择。
- 领域命令成功后先同步写入存档，再执行跨场景跳转。
- 存档损坏或不兼容时不得静默开始新局，也不得自动删除原文件。
- 平衡批处理参数存在时不读取或覆盖玩家正式槽位。

## 4. 总体架构

```text
MainMenuController
        │ New / Continue / Delete
        ▼
GameApp ── RunPersistenceCoordinator ── RunSaveRepository
   │                 │                         │
   │                 │ capture / restore       ├─ run-slot-0.json
   │                 ▼                         ├─ run-slot-0.json.tmp
   │           RunSnapshotMapper               └─ run-slot-0.json.bak
   │                 │
   ▼                 ▼
SceneFlowRouter ◄─ RunSession + ShopSession + RelicService
   │
   ├─ RunTest
   ├─ ShopTest
   └─ BattleTest
```

职责边界：

- `RunSaveRepository` 只负责文档格式、校验和、原子文件操作和主备恢复。
- `RunSnapshotMapper` 只负责领域对象与 DTO 的双向映射，不写文件、不加载场景。
- `RunSnapshotValidator` 在恢复前后验证引用和领域不变量。
- `RunPersistenceCoordinator` 包装正式 Controller 的成功命令，递增 revision、保存并维护“已保存/未保存”状态。
- `SceneFlowRouter` 只根据已恢复的 `RunPhase` 选择场景，不修改领域状态。
- `GameApp` 是组合根，负责配置身份、仓库、当前单局、菜单和路由；不再启动即创建新局。

## 5. 正式启动与场景路由

### 5.1 启动流程

1. `Boot` 创建并初始化 `GameApp`。
2. 加载和校验配置，同时计算统一的运行时配置哈希。
3. 只读取存档头部并构建 `MainMenuScreenState`，不自动恢复完整单局。
4. 进入 `MainMenu`。
5. 玩家选择“继续游戏”后才校验 checksum、恢复领域对象并路由场景。
6. 玩家选择“新游戏”时，如果已有存档，必须二次确认；新单局创建并成功写入首个快照后才替换当前活动单局。

删除 `GameApp.RunSaveSmokeTest()` 和 `TestSaveData` 的启动写盘行为；文件存储能力由 EditMode 测试验证，不能污染玩家目录。

### 5.2 Phase 路由表

| `RunPhase` | 恢复场景 | 说明 |
| --- | --- | --- |
| `MapSelection` | `RunTest` | 显示当前层地图 |
| `Shop` | `ShopTest` | 保持商品、金币、冻结和阻塞选择 |
| `Battle` | `BattleTest` | 使用已保存 `PendingBattle` 回到开战前准备态 |
| `BattleResult` | `RunTest` | 显示结算、继续或 Boss 重试 |
| `RewardChoice` | `RunTest` | 恢复已物化奖励候选 |
| `RelicChoice` | `RunTest` | 恢复已物化遗珍候选及生命代价 |
| `EventChoice` | `RunTest` | 恢复当前事件和选项 |
| `EnhanceChoice` | `RunTest` | 恢复配方和目标选择 |
| `RestChoice` | `RunTest` | 恢复恢复选项 |
| `FloorComplete` | `RunTest` | 显示进入下一层操作 |
| `RunWon` / `RunLost` | `RunTest` | 保留最终结算，直到玩家开始新局或删除存档 |

`EnteringNode` 是同步命令中的瞬时阶段，不允许写入正式存档。加载时遇到该阶段视为不完整快照并尝试备份档，而不是猜测后续状态。

### 5.3 路由 API

新增 `GameSceneId` 和 `SceneFlowRouter`，统一维护场景名：

```text
GameSceneId.MainMenu   -> MainMenu
GameSceneId.Run        -> RunTest
GameSceneId.Shop       -> ShopTest
GameSceneId.Battle     -> BattleTest
```

`RunTestController`、`ShopTestController`、`BattleTestController` 不再散落字符串调用 `SceneManager.LoadScene()`；它们提交成功操作后请求 `GameApp.Router.GoToCurrentRunPhase()`。

`GameSceneId` 是不依赖 Unity API 的纯枚举，只有 `SceneFlowRouter` 负责把它映射为 Unity Scene 名称。现有 `BattleContext.ReturnSceneName` 和 `RunSession` 的 `returnSceneName` 参数不得原样进入正式存档：正式流程在领域提交后按 `RunPhase` 路由；确有回流意图时只保存受验证的 `GameSceneId`，不保存任意场景字符串。战斗预设等测试模式的临时回流行为不进入正式槽位。

## 6. 存档文档格式

### 6.1 外层文档

```json
{
  "format": "spire-chess-run",
  "schemaVersion": 1,
  "contentVersion": "5.5.0",
  "rulesVersion": "8B.1",
  "configHash": "818596...",
  "appVersion": "0.1.0",
  "gitCommit": "optional-build-identity",
  "unityVersion": "2022.3.62f3c1",
  "savedAtUtc": "2026-07-21T13:00:00Z",
  "revision": 42,
  "summary": {
    "floor": 2,
    "health": 13,
    "maxHealth": 20,
    "shopTurn": 8,
    "phase": "RelicChoice"
  },
  "payload": {},
  "payloadSha256": "..."
}
```

`payloadSha256` 对 `payload` 的紧凑规范 JSON 计算。规范化规则与现有 `BalanceConfigHasher` 一致：对象属性按 ordinal 键名递归排序、数组保持原顺序，再使用 `JToken.ToString(Formatting.None)` 输出；外层仍使用 pretty JSON 便于诊断。校验不得只依赖反序列化时偶然保留的属性顺序。

`summary` 只是 MainMenu 的预览缓存，不是领域真值。继续游戏时必须忽略它并从校验通过的 `payload` 恢复；摘要字段越界或与 payload 不一致时，菜单降级显示“已有存档”，不能据此修改单局或拒绝一个 payload 本身有效的存档。

### 6.2 兼容判定

加载顺序固定为：

1. JSON 可解析且 `format` 正确。
2. `schemaVersion == 1`。
3. `payloadSha256` 匹配。
4. `contentVersion`、`rulesVersion`、`configHash` 与当前运行时完全一致。
5. DTO 字段、配置引用和领域不变量通过验证。

任一步失败都返回结构化 `RunSaveLoadStatus`，不抛到 UI：

- `Missing`
- `Valid`
- `RecoveredFromBackup`
- `CorruptJson`
- `ChecksumMismatch`
- `UnsupportedSchema`
- `IncompatibleContent`
- `InvalidReference`
- `InvalidDomainState`
- `RandomReplayMismatch`
- `IoFailure`

v1 建立 `IRunSaveMigration` 接口和迁移注册表，但不提供实际迁移。未来只允许 `N -> N+1` 的显式、可测试迁移；配置哈希变化默认不迁移单局。

## 7. Payload 快照边界

### 7.1 根快照

`RunSavePayloadV1` 包含：

- `RunStateSnapshotV1`
- `ShopSessionSnapshotV1`
- `RelicServiceSnapshotV1`
- `RandomStreamsSnapshotV1`
- `BattleContextSnapshotV1 pendingBattle`
- `BattleContextSnapshotV1 lastBattleContext`
- `BattleResultSnapshotV1 lastBattleResult`
- `RunSequenceSnapshotV1`
- `CoreActivationEvidenceSnapshotV1`
- 防重复标记：`turnTenSnapshotRecorded`、`runEndedRecorded`

### 7.2 RunState

必须保存：

- seed、floor、`ShopTurn`、`MapStep`、生命和最大生命。
- 当前 `RunPhase`、`mapId`、所有节点的 `RunNodeStatus`、当前节点 ID。
- 完整 `NodeAttemptState` 幂等标记。
- `LastSettlement`、最后奖励摘要和延迟商店资源。
- FIFO `PendingCardReward`，包括池预留数量。
- 已物化的 Reward/Relic/Event/Enhance/Rest 选择。
- `OwnedRelicState` 的来源、获得时机、周期进度和触发次数。
- `RunStatistics` 的开始/完成时间和全部累计字段。

事件、锻造和恢复只保存配置 ID；恢复时重新绑定 `EventConfig`、`EnhanceNodeConfig`、配方和 `RestNodeConfig`。奖励和遗珍候选保存候选实例 ID、具体配置 ID和已物化数值，禁止重抽。

### 7.3 ShopSession

必须覆盖全部会影响后续规则的可变状态：

- 回合、金币、酒馆等级、刷新次数、免费刷新、开关店、冻结和本回合升级状态。
- 当前随从商品 ID 列表、法术商品 ID。
- `MinionPool` 每个随从的剩余副本数。
- 手牌区与战斗区的固定槽位。
- 每个 `ShopCardInstance` 的实例 ID、配置 ID、类型、金色、永久成长、繁茂成长、永久关键词、三连发现标记、临时法术标记和下场战斗修饰。
- `PendingDiscover`：来源实例、槽位、候选 ID、可取消标记。
- `PendingEffectChoice`：类型、来源、效果 ID、候选、剩余选择次数和是否替换来源。
- 当前商店激活效果、每商店使用次数、阶段统计、战后延迟增益和战斗开始效果。
- 实例序号、当前等级未升级回合数、升级折扣、延迟金币。
- 遗珍规则修饰，以及“首次免费购买/刷新/出售奖励”当前是否仍可用。

恢复不能通过重新执行 `StartRound()`、`Refresh()` 或重新打出卡牌来构造上述状态，因为这些操作会消费牌池、随机流并重复触发事件。`ShopSession` 需要无副作用的内部恢复构造入口。

### 7.4 序号和遥测证据

以下私有状态必须进入快照，否则恢复后会发生 ID 冲突或重复遥测：

- `RunSession.attemptSequence`
- `RunSession.rewardSequence`
- `RunSession.choiceSequence`
- `RelicService.choiceSequence`
- `RelicService.candidateSequence`
- `ShopSession.cardInstanceSequence`
- `CoreActivationEvidence` 全字段
- `turnTenSnapshotRecorded`
- `runEndedRecorded`

普通玩家存档不保存打开的 `StreamWriter`。恢复后记录 `RunResumed`；通过 `-balanceRunSeed` 启动的批处理禁用正式槽位自动保存，避免污染正式存档和确定性输出。

### 7.5 战斗上下文

`BattleContextSnapshotV1` 保存：

- 遭遇 ID、显示名、节点尝试 ID 和战斗 seed；如确需回流目标，只保存 `GameSceneId`，不保存任意场景名。
- 玩家/敌方 5 个槽位的完整 `BattleMinionRuntime` 快照。
- 战斗开始效果和 `BattleRuleModifiers`。
- 双方繁茂层数。

随从快照保存配置 ID、来源/运行时实例 ID、金色、当前攻防、战斗最大生命、永久差量、护盾、关键词和召唤倍率。

`BattleResultSnapshotV1` 是已提交结果的紧凑审计/UI 快照，只保存最终棋盘、胜者、结束原因和文本日志。永久差量、战后奖励与核心证据在结算事务内已经写入各自领域快照，不再通过结果重放；不保存 `Steps`、`PlaybackEvents` 或高频 diagnostics。恢复到 `BattleResult` 时不得再次调用 `TryCompleteBattle()`。

## 8. 随机流延续

### 8.1 为什么不能只保存 seed

当前持续流为：

| 流 | Stream ID | 消费方 |
| --- | ---: | --- |
| Shop | 101 | 商品、发现、商店效果、有限牌池 |
| Reward | 202 | 节点奖励和自动卡牌 |
| Event | 303 | 事件物化和事件随机效果 |
| Relic | 404 | 遗珍候选和随机遗珍效果 |

恢复时只用 seed 重建会从流开头继续，造成商品和候选变化。直接反射 `System.Random` 私有字段不稳定，也不能跨 Unity/Mono 版本保证。

### 8.2 v1 方案：可重放随机流

新增 `RecordedRandom : System.Random`，覆盖项目实际使用的 `Next()`、`Next(max)`、`Next(min,max)`、`NextDouble()` 和 `NextBytes()`：

- 新局仍使用当前 `System.Random` 算法和同一派生 seed，因此不改变 `eb1cdc7` 的随机结果。
- 每次调用记录操作类型、参数和返回值。
- 保存四条流的初始 seed 和调用日志。
- 恢复时按原顺序重放完全相同的重载，并核对返回值；不一致则返回 `RandomReplayMismatch`，禁止继续。
- 恢复完成后继续在同一实例上记录新调用。

调用日志不静默截断。正常三层单局预期远低于 100,000 次；超过硬上限视为异常并记录诊断。未来若切换到显式状态 PRNG，必须作为新的内容/规则候选重新建立确定性基线，不在 9A 偷换算法。

### 8.3 随机测试门禁

- 固定 seed 的新局在改造前后，前三轮商品、事件、奖励、遗珍候选和战斗哈希必须一致。
- 原始单局与“保存—恢复”分支继续执行相同操作，后续所有随机输出和最终领域指纹完全一致。
- 重放日志任一操作类型、参数或返回值被篡改时必须拒绝存档。

## 9. 捕获与恢复流程

### 9.1 捕获

1. 确认当前不在 `EnteringNode`，也不在战斗播放协程中。
2. `RunSnapshotMapper.Capture()` 复制所有 DTO，不持有领域对象引用。
3. `RunSnapshotValidator.ValidateDto()` 检查引用、容量和 phase/choice 对应关系。
4. 对 payload 生成规范 JSON 和 checksum。
5. `RunSaveRepository.WriteAtomic()` 写入新 revision。
6. 成功后更新 `LastSavedRevision` 和 UI 保存时间；失败则保留 `HasUnsavedChanges=true` 并显示非阻塞错误。

### 9.2 恢复顺序

恢复必须使用专用构造路径，不调用公开游戏命令：

1. 读取主档；失败时读取备份档。
2. 验证文档、身份、checksum 和 DTO。
3. 用 seed/floor 从 `IMapProvider` 获取当前固定地图，核对 `mapId` 后恢复节点状态。
4. 创建四条 `RecordedRandom` 并重放日志。
5. 创建 `ShopSession` 的无副作用恢复实例，恢复牌池、商品、收藏和商店瞬时状态。
6. 恢复 `RunState`、统计、奖励队列、当前尝试和全部阻塞选择。
7. 创建 `RelicService`，恢复持有进度和序号，再绑定事件一次。
8. 恢复 BattleContext、最后结果、序号和遥测幂等标记。
9. 执行领域级 `ValidateHydratedRun()`。
10. 将完整 `RunSession` 一次性发布到 `GameApp.Run`，然后按 phase 路由。

任何步骤失败都不得把半恢复对象发布到 `GameApp`。

### 9.3 恢复构造边界

新增内部 API：

- `RunSession.Restore(...)`
- `ShopSession.Restore(...)`
- `RunState.Restore(...)`
- `MapProgressState.Restore(...)`
- `RelicService.Restore(...)`

这些入口只接受已经验证的 DTO/值对象，不公开任意 setter，不使用反射，不通过 Json.NET 直接填充私有字段。事件订阅在字段全部恢复后统一建立，避免恢复过程触发商店或遗珍回调。

## 10. 保存时机与事务边界

### 10.1 自动保存点

- 新游戏创建完成。
- `RunTestController` 中任一成功节点、选择、继续、重试或重新开始操作完成后。
- `ShopTestController.ApplyOperation()` 中任一成功购买、出售、刷新、冻结、升级、移动、法术或发现操作完成后。
- 奖励领取/跳过成功后。
- `BattleTestController.FinalizeBattle()` 成功提交结算后。
- 玩家选择“保存并返回主菜单”时。
- `OnApplicationPause(true)` / `Application.quitting` 仅在存在 dirty revision 时做最后一次尽力写入；可靠性不能依赖该回调。

### 10.2 应用层协调器

正式 Controller 的领域写操作统一通过：

```text
RunPersistenceCoordinator.ExecuteRun(reason, operation)
RunPersistenceCoordinator.ExecuteShop(reason, operation)
RunPersistenceCoordinator.CommitBattle(reason, operation)
```

协调器只在结果成功时递增 revision 并保存，失败操作不创建新 revision。Controller 仍负责状态提示和渲染，不自行调用 `SaveService`。

每个跨场景操作顺序必须是：

```text
领域原子提交 -> 捕获并写入存档 -> SceneFlowRouter 跳转
```

写盘失败不回滚已经提交的领域状态；当前进程可继续游戏，但 UI 必须显示“尚未保存”，且“保存并退出”在重试成功前不可完成。

### 10.3 战斗特殊规则

- `Battle` phase 的耐久快照在进入 `BattleTest` 前已经写入。
- 点击“开始战斗”只开始表现，不覆盖耐久快照。
- 播放中隐藏或禁用“保存并退出”。
- 异常退出发生在播放中时，继续游戏回到相同准备棋盘并重新开始整场战斗。
- `TryCompleteBattle()` 成功后立即写入新的结果/选择 phase，之后才能显示返回流程按钮。

## 11. 原子文件与故障恢复

### 11.1 文件协议

`FileSaveStorage` 扩展或由 `AtomicFileSaveStorage` 取代：

1. 存储根目录固定为 `Application.persistentDataPath`，将完整内容写入同目录的 `run-slot-0.json.tmp`。
2. 使用 `FileStream.Flush(true)` 刷入磁盘。
3. 主档存在时，以平台支持的原子 replace 将旧主档保留为 `.bak`；主档不存在时将 `.tmp` 移为主档。
4. 成功后确认 `.tmp` 不存在。

文件名必须由仓库内部常量提供，外部 key 不得包含目录分隔符或 `..`。

若平台不支持安全 replace，本次保存返回 `IoFailure` 并保留旧主档，禁止使用“删除主档再移动临时档”的非原子降级。原子文件操作封装成可注入边界，以便在 EditMode 中注入各写入阶段的故障。

### 11.2 加载优先级

1. 主档有效：使用主档。
2. 主档无效、备份有效：返回 `RecoveredFromBackup`，保留坏主档的带时间戳诊断副本；用户确认继续后通过专用 repair 流程从已验证备份重建主档，不能让普通 replace 把好备份覆盖为坏主档。
3. 两者均无效：MainMenu 禁用继续，显示明确原因，提供删除按钮。
4. 仅 `.tmp` 存在：视为上次写入中断；优先主档/备份，不直接信任 `.tmp`。

I/O 异常消息不显示绝对用户路径和完整 JSON；详细堆栈只写本地日志。

## 12. MainMenu 与系统菜单 UI

### 12.1 MainMenu

新增：

- `MainMenuScreenState`
- `MainMenuScreenView`
- `MainMenuController`
- `PF_MainMenuScreen`
- `PF_ConfirmDialog`
- `MainMenuUiPrefabBuilder`

继续卡片显示楼层、生命、商店回合、当前阶段和最后保存时间。状态不兼容时显示“该单局来自不同内容版本”，不把技术异常直接展示给玩家。

### 12.2 单局内系统入口

Run、Shop、Battle 三个正式 Screen 增加统一“菜单”入口：

- 继续游戏
- 保存并返回主菜单
- 放弃当前单局（再次确认）

战斗播放中禁用后两项。自动保存已覆盖正常操作，“保存并返回”主要负责确认最后 revision 已落盘并释放当前内存单局。

## 13. 领域不变量验证

恢复前后至少检查：

- 当前 map ID、floor、规则档案和节点集合匹配配置。
- 节点状态只包含合法 ID，当前/可达/已完成组合与当前 phase 一致。
- 当前节点、尝试、pending choice 的 source attempt 完全一致。
- Phase 与 PendingReward/Relic/Event/Enhance/Rest 互斥关系正确。
- `ShopTurn == ShopSession.Round`，开店状态与 `RunPhase.Shop` 一致。
- 商品、手牌、战斗区、候选和奖励预留共同满足有限牌池守恒。
- 所有卡牌、遗珍、事件、配方、恢复、遭遇和地图 ID 在当前发布清单中存在。
- 卡牌实例 ID、attempt ID、choice ID、reward ID 唯一，序号不小于已存在 ID。
- 生命范围合法；`RunWon/RunLost`、完成时间和当前 phase 一致。
- `Battle` phase 必须有 PendingBattle；已结算 phase 不得再次提交同一 NodeAttempt。
- 随机流重放值全部一致。

新增 `RunStateFingerprint`，对所有持久字段生成规范哈希。`Capture -> Restore -> Capture` 除保存时间外必须得到完全相同的 payload 和指纹。

## 14. 遥测与诊断

记录但不包含完整存档内容：

- `RunSaveCreated`
- `RunSaveLoaded`
- `RunSaveFailed`
- `RunSaveRejected`
- `RunSaveRecoveredFromBackup`
- `RunResumed`
- `RunAbandoned`

字段包含 schema、内容版本、配置哈希、revision、phase、floor、shopTurn、耗时和失败分类。玩家路径、卡组全文和 JSON 不进入遥测。

## 15. 自动化测试

### 15.1 EditMode：存储与格式

- 首次写入、覆盖、`.tmp` 清理和 `.bak` 生成。
- 写入中断后主档仍可读。
- 主档损坏时恢复备份；主备均坏时返回明确状态。
- checksum、格式、schema、内容版本、规则版本和配置哈希拒绝。
- key 路径穿越拒绝。
- 删除存档同时清理主档、备份和临时档。

### 15.2 EditMode：快照完整性

- 初始地图和每种节点状态。
- 商店开启、冻结、升级、当前商品、牌池、手牌/战斗区和临时效果。
- 三连发现、普通可取消发现和多轮阻塞选择。
- 奖励、遗珍、事件、锻造、恢复五种 pending choice。
- Boss 失败重试、事件额外战斗、`Battle` 和 `BattleResult`。
- 第一/二层切层、`RunWon`、`RunLost`。
- 永久成长、关键词、下场护盾、遗珍进度、延迟资源和统计。
- 序号、幂等标记和有限牌池守恒。
- `Capture -> Restore -> Capture` payload 等价。

### 15.3 EditMode：随机延续

- 四条流的所有支持重载可记录、重放和继续。
- 固定 seed 改造前后 golden 输出不变。
- 原始/恢复分支后续商品、候选、事件、遗珍和战斗哈希一致。
- 调用参数或返回值被修改时拒绝恢复。

### 15.4 PlayMode

- Boot 进入 MainMenu，不再自动新建单局。
- 无存档时继续按钮禁用；新游戏创建首个快照并进入 Run。
- 已有存档的新游戏二次确认。
- 从 Map、Shop、Battle、五类 Choice、BattleResult、FloorComplete 和最终结算恢复到正确场景。
- 恢复后每个场景只有一个 Controller、Canvas、EventSystem。
- Shop 恢复不推进回合、不刷新商品；事件/遗珍恢复不重抽或扣血。
- 战斗播放中异常退出恢复到准备态；战斗结算后恢复不重复结算。
- 保存并返回主菜单、继续游戏、放弃单局完整闭环。
- 不兼容和损坏存档显示可理解的错误，并可安全删除。

## 16. 人工验收

至少完成以下真实进程退出场景，而不只是重载 Unity Scene：

1. 第一层商店购买、冻结后退出应用，重启并确认商品、金币、冻结和阵容一致。
2. 三连发现候选出现后退出，重启后候选顺序和牌池不变。
3. Boss 遗珍三选一出现后退出，重启后候选和生命不变。
4. 事件生命交易候选出现后退出，确认未选择时不扣血，选择后只扣一次。
5. 战斗准备态退出可继续；播放中强制关闭后回到准备态；结算后退出不重复伤害或奖励。
6. 第二层切换后退出，恢复同一层 19 节点和路线锁定状态。
7. 人为破坏主档，确认自动使用备份并提示；再破坏备份，确认不会静默开新局。
8. 1920×1080、1920×1200 下 MainMenu、确认弹窗和系统菜单无裁切。

## 17. 实施顺序

1. 从 `ConfigService.LoadFromResources()` 集中生成运行时 `ConfigIdentity`，与平衡哈希共用同一文件顺序和算法。
2. 实现 `RecordedRandom` 和改造四条持久随机流，先用 golden 测试证明随机输出不变。
3. 定义 v1 DTO、`RunSnapshotMapper`、恢复构造入口和 `RunSnapshotValidator`。
4. 实现 `AtomicFileSaveStorage`、`RunSaveRepository`、checksum 和主备恢复。
5. 实现 `RunPersistenceCoordinator`，接入 Run/Shop/Battle 三个 Controller 的单一成功操作出口。
6. 实现 `SceneFlowRouter`，移除 Controller 中散落的场景字符串。
7. 修改 `GameApp` 启动行为，删除自动新局和保存烟测。
8. 制作正式 MainMenu、确认弹窗和单局内系统菜单。
9. 补齐 EditMode、PlayMode 和真实进程退出人工验收。
10. 全量测试与截图通过后建立新的候选提交；不沿用 `eb1cdc7` 继续平衡批次。

实施时先完成纯 C# 快照和随机门禁，再接 UI。MainMenu 不得先以一个不完整的临时存档格式落地。

## 18. 建议文件结构

```text
Assets/Scripts/Save/
  AtomicFileSaveStorage.cs
  RunSaveDocumentV1.cs
  RunSaveRepository.cs
  RunSaveLoadResult.cs
  RunSnapshotMapper.cs
  RunSnapshotValidator.cs

Assets/Scripts/Utils/
  CanonicalJson.cs

Assets/Scripts/Run/
  RecordedRandom.cs

Assets/Scripts/App/
  GameSceneId.cs
  RunPersistenceCoordinator.cs
  SceneFlowRouter.cs

Assets/Scripts/Config/
  ConfigIdentity.cs

Assets/Scripts/UI/MainMenu/
  MainMenuScreenState.cs
  MainMenuScreenView.cs
  MainMenuController.cs

Assets/Scripts/UI/Common/
  RunSystemMenuView.cs

Assets/Editor/
  MainMenuUiPrefabBuilder.cs

Assets/Tests/EditMode/
  AtomicSaveStorageTests.cs
  RunSnapshotTests.cs
  RunSaveCompatibilityTests.cs
  RecordedRandomTests.cs

Assets/Tests/PlayMode/
  SaveResumePlayModeTests.cs
```

## 19. 风险与控制

| 风险 | 控制 |
| --- | --- |
| 漏存一个私有计数器导致重复 ID | 根快照清单、反向 capture 等价和序号唯一性测试 |
| 恢复时重放命令造成副作用 | 专用无副作用恢复构造，不调用公开游戏操作 |
| 改随机封装改变现有候选 | 使用相同 `System.Random`、记录/重放调用、golden 输出门禁 |
| 写入中断损坏唯一存档 | `.tmp` + flush + atomic replace + `.bak` |
| 配置升级后错误绑定旧 ID | 内容/规则/完整配置哈希严格匹配 |
| 战斗中途形成半结算 | 只保存开战前和领域结算后两个耐久边界 |
| Controller 遗漏保存调用 | 所有正式写操作收口到三个 Coordinator wrapper，并做输入级 PlayMode 覆盖 |
| JSON 过大或写盘卡顿 | 单槽位、紧凑 payload checksum、记录大小与耗时；达到阈值再评估压缩，不提前引入二进制格式 |

## 20. 完成定义

阶段 9A 完成必须同时满足：

- Boot 正式进入 MainMenu，不会隐式覆盖已有单局。
- 玩家可以新建、继续、保存退出和放弃一个本地单局。
- 所有稳定 `RunPhase` 均有明确恢复场景和自动化覆盖。
- 存档包含完整领域状态、四条随机流和幂等序号，不序列化 Unity/配置对象。
- 保存恢复前后 payload、领域指纹和后续随机结果一致。
- 战斗中途退出只回退到开战前，不产生半结算或重复结算。
- 主档损坏可恢复备份；主备均损坏或版本不兼容时不静默开新局。
- 配置身份与当前 5.5.0 / 8B.1 / 完整哈希一致。
- EditMode、PlayMode 全量通过，双分辨率截图和真实进程退出人工验收通过。
- 基于 9A 完成提交建立新候选；旧 R17 S1/S2 继续只作历史证据。
