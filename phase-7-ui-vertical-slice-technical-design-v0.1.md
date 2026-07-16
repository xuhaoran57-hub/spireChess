# 阶段 7 UI 纵向切片技术实施方案 v0.1

## 1. 文档信息

- 文档状态：Draft
- 适用阶段：阶段 7 UI/UX
- 首个落地界面：正式商店 UI
- Unity 版本：2022.3.62f3c1
- UI 技术：Unity UGUI
- 基准分辨率：1920×1080

## 2. 目标

本方案以正式商店界面为第一个 UI 纵向切片，在不重写现有规则层的前提下，完成一条玩家可直接操作和理解的商店体验，并为战斗、奖励等界面复用统一的卡牌展示组件。

首个切片需要覆盖：

```text
进入商店
→ 查看商品、金币和酒馆状态
→ 购买随从或法术
→ 上阵和调整五人站位
→ 刷新、冻结、升级和出售
→ 处理发现或效果选择
→ 锁定阵容并进入战斗
```

完成后的 UI 不再依赖测试按钮或控制台操作，且不会在 View 中直接修改金币、卡牌属性或阵容状态。

## 3. 范围

### 3.1 本阶段包含

- 一套可复用的卡牌显示 Prefab。
- 正式商店场景布局。
- 商店商品、战斗区和备战区渲染。
- 购买、出售、刷新、冻结、升级、上阵和换位交互。
- 发现、效果目标和待领取奖励弹窗。
- 永久成长、护盾、下场战斗护盾、临时法术等状态显示。
- 商店操作错误提示和基础数值变化反馈。
- 1920×1080 和 1920×1200 的布局适配。
- 对应 EditMode、PlayMode 测试。

### 3.2 本阶段不包含

- UI Toolkit 或 TextMeshPro 迁移。
- 完整地图美术。
- 战斗完整动画与特效。
- 正式音效包。
- 完整新手教程。
- 对象池和复杂局部刷新系统。
- 微信小游戏专项适配。
- 新卡牌、新机制或平衡性调整。

## 4. 技术决策

1. 继续使用项目现有 UGUI 技术栈和 `UnityEngine.UI.Text`。
2. 保留现有 `ShopSession`、`RunSession` 和 `BattleSimulator`，UI 不重新实现规则。
3. 第一版继续由 `ShopTestController` 承担 Presenter 职责，待正式 UI 稳定后再考虑改名或拆分。
4. 使用共享 `CardViewModel + CardView` 渲染卡牌，商店交互继续交给 `ShopCardView` 和 `ShopSlotView`。
5. 每次操作后重建商店卡牌。当前同屏卡牌数量很少，第一版不引入对象池。
6. 迁移期间保留旧的运行时动态 UI 作为回退；正式场景和测试稳定后再删除。

## 5. 总体架构

```text
玩家点击或拖拽
        ↓
ShopTestController
        ↓
ShopSession 执行领域规则
        ↓
ShopScreenStateBuilder 读取最新状态
        ↓
ShopScreenView.Render()
        ↓
UGUI Prefab 更新显示与反馈
```

职责约束：

- Domain：持有真实状态并校验操作是否合法。
- Controller：接收输入、调用 Domain、管理选择状态和操作反馈。
- StateBuilder：把领域对象转换成只读 UI 状态。
- View：显示状态、转发输入、播放动画。
- View 不得直接修改 `ShopSession` 或 `ShopCardInstance`。

## 6. 目录规划

新增目录和文件：

```text
sc/Assets/Scripts/UI/Common/
├── CardViewModel.cs
├── CardView.cs
└── UiTextFormatter.cs

sc/Assets/Scripts/UI/Shop/
├── ShopCardViewModelFactory.cs
├── ShopScreenState.cs
├── ShopScreenStateBuilder.cs
├── ShopScreenView.cs
└── ChoiceOverlayView.cs

sc/Assets/Prefabs/UI/Common/
└── PF_Card.prefab

sc/Assets/Prefabs/UI/Shop/
├── PF_ShopSlot.prefab
├── PF_ShopScreen.prefab
└── PF_ChoiceOverlay.prefab
```

保留并逐步改造：

- `sc/Assets/Scripts/UI/Shop/ShopTestController.cs`
- `sc/Assets/Scripts/UI/Shop/ShopCardView.cs`
- `sc/Assets/Scripts/UI/Shop/ShopSlotView.cs`
- `sc/Assets/Scripts/UI/CardTierPalette.cs`

## 7. 共享卡牌组件

### 7.1 CardViewModel

`CardViewModel` 只描述卡牌应该显示什么，不引用或修改领域对象。

建议字段：

```csharp
namespace SpireChess.UI
{
    public sealed class CardViewModel
    {
        public string InstanceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RaceText { get; set; }

        public int Tier { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public int Cost { get; set; }

        public bool IsMinion { get; set; }
        public bool IsGolden { get; set; }
        public bool IsSelected { get; set; }
        public bool IsAffordable { get; set; }
        public bool HasShield { get; set; }
        public bool HasNextCombatShield { get; set; }
        public bool IsTemporary { get; set; }

        public string[] Keywords { get; set; }
    }
}
```

`CardViewModel` 不应包含：

- `ShopSession`
- `ShopCardInstance`
- `MinionConfig`
- `SpellConfig`
- 点击回调或领域操作方法

### 7.2 ShopCardViewModelFactory

提供三个转换入口：

```csharp
public static CardViewModel FromOffer(
    MinionConfig minion,
    int currentGold);

public static CardViewModel FromOffer(
    SpellConfig spell,
    int currentGold);

public static CardViewModel FromOwned(
    ShopCardInstance card,
    bool selected);
```

随从实例的基础身材：

```csharp
var baseAttack = card.IsGolden
    ? card.Minion.GoldenAttack
    : card.Minion.Attack;

var baseHealth = card.IsGolden
    ? card.Minion.GoldenHealth
    : card.Minion.Health;
```

当前身材直接读取：

```csharp
card.CurrentAttack
card.CurrentHealth
```

状态映射：

```text
HasShield           ← card.HasPermanentShield
HasNextCombatShield ← card.HasPendingCombatShield
IsTemporary         ← card.ExpiresAtShopEnd
IsGolden            ← card.IsGolden
```

商品价格必须使用 `ShopEconomyRules` 中的常量，禁止在 View 中写裸数字。

### 7.3 PF_Card Prefab

建议层级：

```text
PF_Card
├── Background                 Image
├── TierFrame                  Image
├── GoldenFrame                Image
├── Header
│   ├── Name                   Text
│   └── Tier                   Text
├── Race                       Text
├── Description                Text
├── KeywordRow
│   ├── ShieldBadge
│   ├── NextShieldBadge
│   └── TemporaryBadge
├── AttackBadge
│   └── Attack                 Text
├── HealthBadge
│   └── Health                 Text
├── SelectionFrame             Image
└── DisabledMask               Image
```

根节点组件：

- `Image`
- `CanvasGroup`
- `CardView`
- `ShopCardView`

约束：

- 根节点 `Image` 保持 `raycastTarget = true`。
- 所有装饰图片关闭 `raycastTarget`。
- `DisabledMask` 只负责灰态，不阻断根节点的错误提示点击。
- 卡牌建议设计尺寸为 220×280。
- 描述文本允许自动换行，垂直方向截断。

### 7.4 CardView

`CardView` 只负责渲染，不响应购买或拖拽操作。

```csharp
public sealed class CardView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private GameObject goldenFrame;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private GameObject disabledMask;

    [SerializeField] private Text nameText;
    [SerializeField] private Text tierText;
    [SerializeField] private Text raceText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text attackText;
    [SerializeField] private Text healthText;

    [SerializeField] private GameObject shieldBadge;
    [SerializeField] private GameObject nextShieldBadge;
    [SerializeField] private GameObject temporaryBadge;

    public void Render(CardViewModel model)
    {
        nameText.text = model.Name;
        tierText.text = $"T{model.Tier}";
        raceText.text = model.RaceText;
        descriptionText.text = model.Description ?? string.Empty;
        attackText.text = model.Attack.ToString();
        healthText.text = model.Health.ToString();

        background.color = CardTierPalette.GetBackground(model.Tier);
        goldenFrame.SetActive(model.IsGolden);
        selectionFrame.SetActive(model.IsSelected);
        shieldBadge.SetActive(model.HasShield);
        nextShieldBadge.SetActive(model.HasNextCombatShield);
        temporaryBadge.SetActive(model.IsTemporary);
        disabledMask.SetActive(!model.IsAffordable);

        attackText.color = model.Attack > model.BaseAttack
            ? GrowthColor
            : Color.white;
        healthText.color = model.Health > model.BaseHealth
            ? GrowthColor
            : Color.white;
    }
}
```

## 8. 正式商店界面

### 8.1 PF_ShopScreen Prefab

```text
Canvas
└── SafeArea
    ├── Background
    ├── TopBar
    │   ├── RoundText
    │   ├── GoldText
    │   ├── TavernTierText
    │   ├── UpgradeCostText
    │   └── StatusText
    ├── Content
    │   ├── OfferPanel
    │   │   ├── Title
    │   │   └── OfferSlots
    │   │       ├── MinionSlot0
    │   │       ├── MinionSlot1
    │   │       ├── MinionSlot2
    │   │       ├── MinionSlot3
    │   │       └── SpellSlot
    │   ├── BattlePanel
    │   │   └── 5 × BattleSlot
    │   └── BenchPanel
    │       └── 5 × BenchSlot
    ├── ActionRail
    │   ├── RefreshButton
    │   ├── FreezeButton
    │   ├── UpgradeButton
    │   ├── SellButton
    │   └── EndButton
    ├── FeedbackLayer
    │   ├── StatusToast
    │   └── FloatingTextRoot
    └── ModalLayer
        ├── Blocker
        ├── ChoiceOverlay
        └── RewardOverlay
```

### 8.2 布局参数

- `CanvasScaler.referenceResolution`：1920×1080。
- `CanvasScaler.screenMatchMode`：Match Width Or Height。
- `CanvasScaler.matchWidthOrHeight`：0.5。
- TopBar 高度：96 px。
- ActionRail 宽度：220 px。
- 内容区左右边距：32 px。
- 面板间距：20 px。
- 卡牌间距：16 px。
- 卡牌行优先使用 `HorizontalLayoutGroup`，不要在代码中计算位置。
- 高等级尚未开放的商品位直接隐藏。

### 8.3 PF_ShopSlot Prefab

```text
PF_ShopSlot
├── Background
├── EmptyHint
├── SelectionFrame
└── Content
```

根节点挂载 `ShopSlotView`，动态卡牌实例挂到 `Content`。

## 9. ShopScreenState

```csharp
public sealed class ShopScreenState
{
    public int Round { get; set; }
    public int Gold { get; set; }
    public int TavernTier { get; set; }
    public int UpgradeCost { get; set; }
    public int RefreshCount { get; set; }
    public int FreeRefreshes { get; set; }
    public int FlourishStacks { get; set; }

    public bool IsFrozen { get; set; }
    public bool CanRefresh { get; set; }
    public bool CanUpgrade { get; set; }
    public bool CanFreeze { get; set; }
    public bool CanSell { get; set; }
    public bool CanEnd { get; set; }

    public CardViewModel[] MinionOffers { get; set; }
    public CardViewModel SpellOffer { get; set; }
    public CardViewModel[] BattleCards { get; set; }
    public CardViewModel[] BenchCards { get; set; }
}
```

`ShopScreenStateBuilder.Build()` 负责从 `ShopSession`、`RunSession` 和 Controller 的选择状态构建该对象。

按钮状态建议统一在 Builder 中计算：

```csharp
var blocked = session.PendingDiscover != null ||
              session.PendingChoice != null ||
              hasPendingReward;

var unlocked = session.IsShopOpen && !blocked;

state.CanRefresh = unlocked &&
    (session.FreeRefreshes > 0 ||
     session.Gold >= ShopEconomyRules.RefreshCost);

state.CanUpgrade = unlocked &&
    session.TavernTier < ShopEconomyRules.MaximumTavernTier &&
    !session.UpgradedThisRound &&
    session.Gold >= session.CurrentUpgradeCost;

state.CanFreeze = unlocked;
state.CanSell = unlocked && selectedBattleIndex >= 0;
state.CanEnd = unlocked && runSession != null;
```

商品可购买状态还需要考虑：

- 金币是否足够。
- 备战区是否有空位。
- 是否存在阻塞选择或待领取奖励。

## 10. ShopScreenView

职责接口：

```csharp
public sealed class ShopScreenView : MonoBehaviour
{
    public void Bind(ShopTestController controller);
    public void Render(ShopScreenState state);
    public void RenderChoice(ChoiceViewModel choice);
    public void ShowStatus(string message);
}
```

`Bind()` 只绑定一次按钮事件：

```csharp
refreshButton.onClick.AddListener(
    () => controller.RefreshShop());

upgradeButton.onClick.AddListener(
    () => controller.UpgradeTavern());

freezeButton.onClick.AddListener(
    () => controller.ToggleFreeze());

sellButton.onClick.AddListener(
    () => controller.SellSelectedBattleMinion());

endButton.onClick.AddListener(
    () => controller.EndShopAndEnterBattle());
```

`Render()` 执行顺序：

1. 更新回合、金币、等级和升级费用。
2. 更新按钮文字与 `interactable`。
3. 清理各 Slot 旧卡。
4. 实例化 `PF_Card`。
5. 调用 `CardView.Render(model)`。
6. 初始化同节点上的 `ShopCardView`。

```csharp
shopCardView.Initialize(
    controller,
    rootCanvas,
    zone,
    index,
    draggable);
```

第一版每次重建卡牌，不实现对象池和复杂局部更新。

## 11. ShopTestController 迁移

新增序列化字段：

```csharp
[SerializeField] private ShopScreenView screenView;
[SerializeField] private bool useLegacyRuntimeUi;
```

初始化流程：

```csharp
if (screenView != null)
{
    screenView.Bind(this);
}
else if (useLegacyRuntimeUi)
{
    BuildUi();
}

RefreshAll();
```

`RefreshAll()` 优先渲染正式 View：

```csharp
private void RefreshAll()
{
    if (!initialized)
    {
        return;
    }

    if (screenView != null)
    {
        var state = ShopScreenStateBuilder.Build(
            session,
            runSession,
            selectedBenchIndex,
            selectedBattleIndex,
            selectedEffectTargetIndex);

        screenView.Render(state);
        screenView.ShowStatus(StatusMessage);
        RefreshFormalOverlays();
        return;
    }

    RefreshLegacyUi();
}
```

迁移期间保留旧动态 UI。正式场景和新测试全部通过后，单独提交删除 `CreateText`、`CreatePanel`、`CreateButton` 等正式界面不再使用的运行时构建代码。

### 11.1 避免重复渲染

当前领域事件和 `ApplyOperation()` 都可能触发刷新。正式 UI 应保证一次操作只执行一次最终 Render，否则成长动画可能在第二次渲染时丢失。

```csharp
private void OnShopEvent(ShopEventData eventData)
{
    eventLog.Add(BuildEventText(eventData));
    pendingFeedback.Enqueue(eventData);

    // 不在这里调用 RefreshAll()。
}
```

统一由 `ApplyOperation()` 在操作完成后调用 `RefreshAll()`。

## 12. 商店操作与交互规则

| 输入 | Controller 方法 | 领域操作 |
| --- | --- | --- |
| 点击随从商品 | `BuyMinionAt` | `ShopSession.BuyMinion` |
| 点击法术商品 | `BuySpellOffer` | `ShopSession.BuySpell` |
| 点击刷新 | `RefreshShop` | `ShopSession.Refresh` |
| 点击升级 | `UpgradeTavern` | `ShopSession.UpgradeTavern` |
| 点击冻结 | `ToggleFreeze` | `ShopSession.ToggleFreeze` |
| 点击出售 | `SellSelectedBattleMinion` | `ShopSession.SellBattleMinion` |
| 备战区拖入战斗位 | `PlayBenchMinion` | `ShopSession.PlayMinion` |
| 战斗位拖到另一位置 | `RepositionBattleMinion` | `ShopSession.RepositionBattleMinion` |
| 使用法术 | `UseBenchSpell` | `ShopSession.UseSpell` |
| 选择发现 | `SelectDiscoverCandidate` | `SelectDiscover` 或 `SelectEffectChoice` |
| 结束商店 | `EndShopAndEnterBattle` | `RunSession.EndShopAndPrepareBattle` |

所有失败操作继续使用 `ShopOperationResult` 映射为用户可读错误，不在 View 中推测失败原因。

## 13. 数值变化反馈

为了显示天穹契约者、星盘校准师等刷新成长，`ShopScreenView` 保存上次渲染的实例属性：

```csharp
private readonly Dictionary<string, Vector2Int> previousStats;
```

重新渲染拥有 `InstanceId` 的卡牌时比较前后数值：

```csharp
if (previousStats.TryGetValue(model.InstanceId, out var oldStats))
{
    var attackDelta = model.Attack - oldStats.x;
    var healthDelta = model.Health - oldStats.y;

    if (attackDelta != 0 || healthDelta != 0)
    {
        cardView.PlayStatChange(attackDelta, healthDelta);
    }
}
```

推荐表现：

- 攻击成长：绿色 `+1 攻击`。
- 生命成长：绿色 `+1 生命`。
- 双成长：绿色 `+1/+1`。
- 属性下降：红色。
- 护盾获得：盾牌图标短暂放大。

刷新反馈顺序：

```text
点击刷新
→ 扣费或免费刷新提示
→ 商品淡出并替换
→ 友方成长卡牌显示 +X/+Y
→ 更新刷新次数
```

该逻辑只比较前后 UI 状态，不重新计算效果。

## 14. 选择和奖励弹窗

统一使用 `ChoiceOverlayView`。

```csharp
public sealed class ChoiceViewModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool CanCancel { get; set; }
    public ChoiceCandidateViewModel[] Candidates { get; set; }
}
```

候选分为：

- 随从或法术：复用 `PF_Card`。
- 种族、效果目标等非卡牌候选：使用普通选择按钮。

弹窗开启时：

- 激活 `ModalLayer/Blocker`。
- 禁用刷新、升级、冻结、出售和结束商店。
- 禁止卡牌拖拽。
- 必选的三连发现不允许取消，并显示原因。

弹窗只调用现有 Controller 方法，不直接调用领域层。

## 15. 场景接线步骤

在 `ShopTest.unity` 中执行：

1. 放入 `PF_ShopScreen`。
2. 新建 `ShopTestController` GameObject。
3. 挂载 `ShopTestController`。
4. 将场景中的 `ShopScreenView` 拖到 Controller 的 `screenView` 字段。
5. 关闭 `useLegacyRuntimeUi`。
6. 确认场景中只有一个 `EventSystem`。
7. 确认 Canvas 使用 1920×1080 和 Match 0.5。
8. 暂时保留场景名 `ShopTest`，不修改现有跳转字符串。

现有自动创建逻辑检测到场景已有 Controller 后不会再次创建，可以在迁移期间保留。

## 16. 实施和提交顺序

### 16.1 提交 1：共享卡牌

- 新增 `CardViewModel`。
- 新增 `ShopCardViewModelFactory`。
- 新增 `CardView`。
- 制作 `PF_Card`。
- 补充 Factory EditMode 测试。

验收：普通、金色、永久成长、护盾、下场护盾和临时法术显示正确。

### 16.2 提交 2：静态商店布局

- 制作 `PF_ShopSlot`。
- 制作 `PF_ShopScreen`。
- 使用临时数据验证长文案、五张卡和金色边框。

验收：1920×1080 和 1920×1200 下无核心遮挡。

### 16.3 提交 3：接入真实 Session

- 新增 `ShopScreenState`。
- 新增 `ShopScreenStateBuilder`。
- Controller 接入 `ShopScreenView`。
- 接通购买、出售、刷新、冻结、升级、上阵和换位。

验收：相同输入下，新旧 UI 的领域结果一致。

### 16.4 提交 4：弹窗和反馈

- 接入发现和效果选择。
- 接入待领取奖励。
- 增加属性成长浮字和错误 Toast。
- 增加冻结、护盾和临时法术反馈。

验收：阻塞选择未完成时，其他商店操作全部锁定。

### 16.5 提交 5：测试和清理

- 更新 PlayMode 测试。
- 删除旧动态 UI 构建代码。
- 保留 Controller 的公开测试接口。
- 只清理本次迁移产生的无用字段和方法。

验收：正式商店不再依赖运行时 `CreateText`、`CreatePanel` 和 `CreateButton` 搭建界面。

## 17. 自动化测试

### 17.1 EditMode

- 普通随从 ViewModel 属性正确。
- 金色随从使用 GoldenAttack 和 GoldenHealth。
- 永久成长后的 Current 和 Base 属性正确。
- 永久护盾和下场战斗护盾分别显示。
- 临时法术显示临时标签。
- 刷新和升级按钮状态符合金币、等级和阻塞选择。
- 空备战位和满备战区的商品状态正确。

### 17.2 PlayMode

- 加载 `ShopTest` 后只有一个 Controller、一个 Canvas 和一个 EventSystem。
- 点击刷新后金币和刷新次数同步更新。
- 购买随从后商品位清空，备战区出现卡牌。
- 备战随从拖入战斗位成功。
- 两个战斗位能够调整站位。
- 冻结按钮文字和状态同步。
- 三连发现打开阻塞弹窗。
- 发现未完成时不能刷新、升级或结束商店。
- 结束商店后能够进入 `BattleTest`。
- 战斗返回后仍使用同一个 `RunSession` 和已有阵容。

现有 `ShopFlowPlayModeTests` 已覆盖商店到战斗、三连发现和重复订阅，可在其基础上补充正式 View 断言。

## 18. 手工验收

### 18.1 操作验收

- 新玩家能够识别金币、回合、酒馆等级和升级费用。
- 新玩家能够完成购买、刷新、上阵、换位和结束商店。
- 金币不足、备战区已满、目标无效时能看到明确原因。
- 冻结后商品保留，刷新后冻结状态按领域规则更新。
- 发现和效果选择不会被其他按钮绕过。

### 18.2 信息验收

- 普通和金色随从易于区分。
- 当前身材和成长后的属性易于识别。
- 永久护盾与下场战斗护盾不会混淆。
- 临时法术能够看出将在商店结束时消失。
- 刷新触发成长时，玩家能指出是哪张卡成长以及成长数值。

### 18.3 布局验收

- 1920×1080 无核心遮挡。
- 1920×1200 无核心遮挡。
- 最长随从名称和描述不会突破卡牌边界。
- 五个战斗位和五个备战位始终完整可见。
- 模态弹窗始终位于所有商店内容上方。

## 19. 完成定义

- 商店正式界面由 Prefab 和序列化引用构建。
- `ShopSession` 没有任何 UI 引用。
- 所有输入经过 Controller 调用领域操作。
- 共享卡牌组件可以用于后续战斗和奖励页面。
- 普通、金色、成长、护盾、下场护盾和临时状态可区分。
- 刷新成长有明确反馈。
- 发现弹窗不会被其他操作绕过。
- 商店到战斗再返回商店的现有流程保持通过。
- EditMode 和 PlayMode 测试通过。
- 本阶段不修改规则和数值，因此不要求重跑 S0。

## 20. 后续扩展顺序

商店 UI 稳定后，按以下顺序继续纵向切片：

1. 战斗卡牌接入共享 `CardView`。
2. 战斗事件播放、伤害、护盾破裂、死亡和召唤反馈。
3. 奖励三选一复用共享卡牌和 `ChoiceOverlayView`。
4. 地图节点和返回地图链路。
5. Tooltip、关键词说明和基础音效。

不要在共享卡牌尚未稳定时同时重做战斗和奖励界面，避免三个页面重复返工。
