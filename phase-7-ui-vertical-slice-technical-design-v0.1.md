# 阶段 7 UI 纵向切片技术实施方案 v0.1

## 1. 文档信息

- 文档状态：实施中（共享 `PF_Card` 已于 2026-07-17 完成）
- 适用阶段：阶段 7 UI/UX
- 首个落地界面：正式商店 UI
- Unity 版本：2022.3.62f3c1
- UI 技术：Unity UGUI
- 基准分辨率：1920×1080

### 1.1 商店线框图冻结基线

- 冻结日期：2026-07-17。
- 冻结版本：`shop-ui-wireframe-v0.1`。
- 规范文件：`ui-concepts/shop-ui-wireframe-v0.1.png`。
- 原始确认稿：`ui-concepts/ChatGPT Image 2026年7月17日 15_01_32.png`，原文件保留不覆盖。
- 图像尺寸：1672×941，宽高比约 1.7768。
- 文件大小：1,493,321 bytes。
- SHA-256：`d3d200a0550deb24c384217ea32db43a623c1c6800bdc510a00b01adf30c37c4`。

该版本冻结商店的信息架构和区域关系：顶部状态栏、完整卡牌商品行、紧凑战斗区、紧凑手牌区，以及右侧固定操作/详情栏。它是 Prefab 和状态契约的低保真验收基线，不代表最终美术；布局结构发生变化时应新建版本，不直接覆盖 v0.1。

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
- 商店商品、战斗区和手牌区渲染。
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

sc/Assets/Scripts/Shop/
└── ShopTargetingQuery.cs

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
    public enum CardDisplayMode
    {
        Full,
        Compact
    }

    public sealed class CardViewModel
    {
        public string InstanceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RaceText { get; set; }
        public string[] AbilityLabels { get; set; }
        public string ProgressText { get; set; }
        public string DisabledReason { get; set; }

        public int Tier { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public int Cost { get; set; }

        public CardDisplayMode DisplayMode { get; set; }
        public bool IsMinion { get; set; }
        public bool ShowCost { get; set; }
        public bool IsGolden { get; set; }
        public bool IsSelected { get; set; }
        public bool IsLegalTarget { get; set; }
        public bool IsInteractable { get; set; }
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

补充约束：

- `Description` 始终保存完整原文，截断只发生在 View。
- `AbilityLabels` 是面向玩家的短标签，不直接显示配置中的英文 `tags`。
- `ProgressText` 为空时不显示进度模块；第一版直接接收格式化后的 `3/4`，不在 View 中推导规则。
- `IsInteractable` 是最终输入状态，`IsAffordable` 仅用于费用颜色和不可购买原因；两者不能互相替代。
- 成长状态由 `Attack/Health` 与对应 `BaseAttack/BaseHealth` 比较得到，不再增加独立 `IsGrowth` 布尔值。

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

Factory 的默认上下文：

- `FromOffer` 生成 `Full`、`ShowCost=true` 的商品卡。
- `FromOwned` 生成 `Compact`、`ShowCost=false` 的持有卡。
- Factory 只映射卡牌事实和基础费用；`ShopScreenStateBuilder` 再根据阻塞选择、领域层备战区（UI 手牌）容量和当前选择状态填写 `IsInteractable`、`IsSelected` 与 `DisabledReason`。`ShopTargetingQuery` 复用 `ShopEffectEngine.TryBuildPlan()` 的目标过滤语义，Builder 据此填写 `IsLegalTarget` 并禁用非法目标。
- 法术始终令 `IsGolden=false`，并隐藏成长、攻防和护盾状态。

### 7.3 PF_Card Prefab

Unity 实施时使用 `pf-card-unity-implementation-checklist-v0.1.md` 作为逐项任务单和验收记录；本节继续作为几何与视觉规则的权威来源。

卡牌实例稿 `card-ui-sky-covenant-normal-v0.2.png`、`card-ui-sky-covenant-golden-style-v0.2.png` 和 `card-ui-ten-thousand-hoof-normal-v0.2.png` 均使用严格的 `2:3` 竖卡比例。正式 Prefab 以此为唯一比例基线，不继续使用旧版 `220×280` 建议。

#### 7.3.1 显示模式与使用场景

| 模式 | 参考尺寸 | 使用场景 | 信息策略 |
| --- | ---: | --- | --- |
| `Full` | `240×360` | 商店商品、发现/奖励候选、卡牌检查 | 显示完整描述、最多 3 个能力标签和可选进度 |
| `Compact` | `160×240` | 已拥有的战斗区、手牌区和后续战斗站位 | 保留核心身份和状态，描述最多 2-3 行 |

约束：

- 所有尺寸均为 1920×1080 参考分辨率下的 UGUI 单位，由 `CanvasScaler` 统一缩放。
- `Full` 是玩家第一次阅读卡牌时的默认模式；商品和选择候选不得使用 `Compact`。
- `Compact` 只用于已经获得的卡牌。选中紧凑卡牌时，商店固定状态区显示完整名称和描述；本阶段不为此提前实现独立 Tooltip 系统。
- 两种模式共用一个 `PF_Card`。`CardView` 只在两套已序列化布局间切换，不按屏幕尺寸临时计算比例。
- Slot 在卡牌四周至少保留 8 单位空白，避免合法目标光效或金色外框被父节点裁切。

#### 7.3.2 Prefab 层级

```text
PF_Card
├── Background                       Image
├── RaceSkin                         Image
├── ArtworkMask
│   └── Artwork                      Image
├── NormalFrame                      Image
├── GoldenFrame                      Image
├── CostBadge
│   └── Cost                         Text
├── TierBadge
│   └── Tier                         Text
├── NamePlate
│   └── Name                         Text
├── InfoPanel
│   ├── RaceOrSpellType              Text
│   ├── AbilityLabelRow
│   ├── Description                  Text
│   └── Progress
│       ├── ProgressFill             Image
│       └── ProgressText             Text
├── StateBadgeRow
│   ├── ShieldBadge
│   ├── NextCombatShieldBadge
│   └── TemporaryBadge
├── AttackBadge
│   └── Attack                       Text
├── HealthBadge
│   └── Health                       Text
├── SpellFooter                      Text
├── GrowthFeedbackRoot
├── SelectionFrame                   Image
├── LegalTargetFrame                 Image
└── DisabledMask
    ├── DisabledIcon                 Image
    └── DisabledReason               Text
```

#### 7.3.3 几何规格

坐标原点统一为卡牌左上角，格式为 `x, y, width, height`。状态变化只切换颜色、图标和覆盖层，不改变下表几何，避免同一排卡牌抖动。

| 区域 | `Full 240×360` | `Compact 160×240` | 说明 |
| --- | --- | --- | --- |
| 外框安全区 | `6, 6, 228, 348` | `4, 4, 152, 232` | 普通/金色共用占位 |
| 插画 | `12, 12, 216, 184` | `8, 8, 144, 112` | `Preserve Aspect`，使用遮罩裁切 |
| 费用徽章 | `8, 8, 48, 48` | `6, 6, 34, 34` | 左上；非商品且无费用语义时整体隐藏 |
| 等级徽章 | `184, 8, 48, 48` | `120, 6, 34, 34` | 右上；随从和法术始终显示 |
| 状态徽章行 | `60, 157, 120, 22` | `42, 91, 76, 18` | 位于插画底部，最多 3 个图标 |
| 名称牌 | `24, 181, 192, 32` | `16, 108, 128, 26` | 与插画/信息区轻微重叠，单行居中 |
| 信息面板 | `12, 199, 216, 149` | `8, 122, 144, 110` | 固定背景，不随文字增高 |
| 种族/法术类型 | `44, 215, 152, 18` | `28, 136, 104, 14` | 单行居中 |
| 能力标签行 | `20, 235, 200, 20` | `12, 154, 136, 16` | 完整最多 3 个，紧凑最多 2 个 |
| 描述，无进度 | `12, 256, 216, 52` | `12, 172, 136, 33` | 随从描述区；Full 使用信息面板全宽以容纳 66 字真实上限 |
| 描述，有进度 | `12, 256, 216, 31` | `12, 172, 136, 21` | 为进度条让出固定空间 |
| 进度条 | `62, 293, 116, 18` | `44, 197, 72, 14` | 为空时整体隐藏 |
| 攻击徽章 | `8, 308, 44, 44` | `6, 204, 32, 32` | 仅随从显示 |
| 生命徽章 | `188, 308, 44, 44` | `122, 204, 32, 32` | 仅随从显示 |
| 法术页脚 | `58, 318, 124, 22` | `42, 211, 76, 16` | 替代攻防徽章，显示“商店法术”等短类型 |
| 选中/目标/禁用层 | `0, 0, 240, 360` | `0, 0, 160, 240` | 不参与 Layout，不改变卡牌尺寸 |

字体基线：

| 内容 | `Full` | `Compact` | 最小字号 |
| --- | ---: | ---: | ---: |
| 费用、等级、攻防 | 26 | 20 | 不缩放 |
| 名称 | 22 | 16 | 18 / 14 |
| 种族/法术类型 | 14 | 11 | 12 / 11 |
| 能力标签 | 12 | 10 | 11 / 10 |
| 描述 | 14 | 11 | 11 / 10 |
| 进度 | 12 | 10 | 不缩放 |

#### 7.3.4 随从与法术变体

| 区域 | 随从 | 法术 |
| --- | --- | --- |
| 费用 | 商品/候选上下文显示 `ShopEconomyRules.MinionPurchaseCost`；已拥有随从隐藏 | 商品显示 `ShopEconomyRules.SpellPurchaseCost`；已拥有法术隐藏 |
| 等级 | 显示酒馆等级 | 显示法术等级 |
| 类型行 | 显示种族；无种族时显示“旅团”或配置对应文本 | 显示法术类型，例如“成长”“经济”“发现” |
| 标签 | 最多 3/2 个面向玩家的机制标签 | 最多 3/2 个用途标签 |
| 描述 | 使用随从描述；可显示机制进度 | 使用法术描述；描述区向下扩展至法术页脚上方 |
| 底部 | 显示攻击和生命 | 隐藏攻防，显示法术页脚 |
| 金色 | 支持 | 当前内容不支持，强制按普通法术渲染 |
| 成长/护盾 | 支持 | 不显示 |
| 临时 | 当前无临时随从，保留通用能力 | `ExpiresAtShopEnd` 时显示“临时”徽章 |

#### 7.3.5 长文案与截断规则

当前内容最长随从名称为 5 个汉字，最长随从描述为 64 个 Unicode 文本元素（山腹吞灵者与金色终花吞世者）；最长法术名称为 4 个汉字，最长法术描述为 45 个 Unicode 文本元素。普通万蹄奔潮描述为 63 字。布局继续保留已验证的 66/45 文本容量。

规则：

1. ViewModel 永远保留完整原文，View 不回写截断结果。
2. 名称、种族和法术类型禁止换行。先从基准字号缩至最小字号；仍放不下时在最后一个完整 Unicode 文本元素后追加 `…`。
3. 完整模式能力标签最多显示 3 个，紧凑模式最多显示 2 个；超出时最后一个位置显示 `+N`。标签自身禁止换行。
4. 描述允许自动换行，不允许滚动或突破信息面板。完整随从无进度最多 4 行、有进度最多 2 行；完整法术最多 5 行。紧凑随从无进度最多 3 行、有进度最多 2 行；紧凑法术最多 4 行。
5. 描述先缩小至该模式最小字号，再使用 `TextGenerator` 判断可见字符；溢出时按 Unicode 文本元素二分查找最长前缀并追加 `…`。禁止用固定 `Substring` 切割代理对或组合字符。
6. `Full` 模式必须让当前版本 64 字最长随从描述和 45 字最长法术描述完整显示，并继续支持 66/45 的设计容量；若实际字体下无法满足，则本契约验收失败，必须优先扩大信息区并同步更新几何表，不能静默缩到最小字号以下或截断当前内容。
7. `Compact` 允许截断，但选中后必须在商店状态区显示未经截断的名称和描述；操作结果 Toast 出现时可短暂覆盖，结束后恢复卡牌说明。
8. 不支持富文本标签。配置中的显式换行保留，连续空白折叠为一个空格。

`UiTextFormatter` 已实现上述规则中不依赖 Unity 的部分：空白规范化、单行化、3/2 标签折叠、描述行数、Unicode 文本元素计数和二分截断。`CardView` 先用 `TextGenerator` 从基准字号尝试到最小字号，再把最终字号下的 `fits` 判定传入 Formatter；`Full` 描述不满足时 Formatter 抛出契约错误，`Compact` 才追加省略号。

#### 7.3.6 卡牌状态与视觉优先级

普通、金色、成长和三个持续状态使用不同视觉通道，尽量允许叠加。视觉层级从底到顶为：

```text
种族/等级底色与插画
→ 普通或金色外框
→ 文字、攻防和成长颜色
→ 永久护盾 / 下场护盾 / 临时徽章
→ 选中内框
→ 合法目标外框
→ 不可操作遮罩与原因
```

| 状态 | 视觉规则 | 与其他状态冲突时 |
| --- | --- | --- |
| 普通 | 使用等级底色、种族皮肤和银黑公共框架 | 作为默认底层，不覆盖任何状态 |
| 金色 | 金色框架、角饰和轻微流光；正文保持高对比度 | 只替换普通外框，不替换种族皮肤、等级色或状态徽章 |
| 成长 | 高于普通/金色基础值的攻防数字使用 `#62E6A6`，变化反馈显示 `+X/+Y` | 金色随从以金色基础身材为比较基准；不把整张卡染绿 |
| 永久护盾 | 实心蓝色盾牌，色值 `#68C7FF` | 与下场护盾同时存在时两枚徽章都显示，永久护盾排在左侧 |
| 下场护盾 | 带时钟/单次标记的浅青盾牌，色值 `#9EEBFF`，短标签“下战” | 不与永久护盾合并，避免玩家误认为永久状态 |
| 临时 | 紫色沙漏徽章和“临时”，色值 `#C98BFF` | 只占状态徽章位，不改变金色或等级外框 |
| 选中 | 3 单位青色内框 `#59D8FF`，右上显示小型勾选标记 | 与合法目标同时存在时保留内框，合法目标使用外框 |
| 合法目标 | 4 单位绿色外框 `#6CFF8F`，允许低频呼吸；仅在目标选择阶段显示 | 优先占用最外层轮廓；仅 `IsInteractable=true` 时生效 |
| 不可操作 | 黑色 `55%` 遮罩、饱和度降至 `35%`、停止呼吸和成长动画；费用不足时费用改为 `#FF7B7B` | 最高优先级，隐藏合法目标光效并压暗选中框，但不移除金色、护盾等事实信息 |

同一张卡最多同时显示 3 枚状态徽章，固定顺序为“永久护盾 → 下场护盾 → 临时”。状态数量不会超过当前容量，不使用 `+N` 合并状态。

目标选择阶段的规则：

- 合法目标显示绿色外框。
- 非法目标使用不可操作遮罩，并由 `DisabledReason` 提供原因。
- 当前已选来源卡保留青色内框；如果同一卡同时是合法目标，绿色外框和青色内框并存。
- `DisabledMask` 不关闭根节点 `raycastTarget`，玩家点击后仍能看到错误原因。
- `ShopTargetingQuery.ForHandCard()` 只查询 `Manual` 法术和 `OnPlay` 随从中由 `PlayerChoice/None` 直接选择单个战斗区随从的效果；发现、复制和种族选择继续使用模态候选，不在主界面显示合法目标框。
- 查询逐个战斗位调用现有效果计划构建逻辑，但使用独立随机源且不执行 `ApplyPlan()`，不得推进商店 RNG、消耗手牌或修改属性。
- 无合法目标的战吼不显示目标框且仍允许随从进入空战斗位；无合法目标的定向法术禁用来源卡并显示“没有合法目标”。
- 发现、效果选择或奖励形成全局阻塞时，Builder 清除全部合法目标状态，阻塞原因优先于目标原因。

成长反馈规则：

- 首次 Render 不播放成长动画。
- 仅对相同 `InstanceId` 的相邻两次 Render 比较攻防差值。
- 正增长使用绿色，负增长使用 `#FF6B6B`；数值未变化不播放。
- 不可操作遮罩或模态弹窗开启时不播放成长浮字，避免反馈被遮挡后重复播放。

根节点组件：

- `Image`
- `CanvasGroup`
- `CardView`
- `ShopCardView`

约束：

- 根节点 `Image` 保持 `raycastTarget = true`。
- 所有装饰图片关闭 `raycastTarget`。
- `DisabledMask` 只负责灰态，不阻断根节点的错误提示点击。
- `Mask` 只用于插画；外框、状态徽章和目标光效不得被卡牌自身裁切。
- 隐藏费用、进度、攻防或法术页脚时不重排其他区域，只切换对应节点显隐。

### 7.4 CardView

`CardView` 只负责渲染，不响应购买或拖拽操作。

```csharp
public sealed class CardView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private GameObject costBadge;
    [SerializeField] private Text costText;
    [SerializeField] private GameObject goldenFrame;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private GameObject legalTargetFrame;
    [SerializeField] private GameObject disabledMask;
    [SerializeField] private Text disabledReasonText;

    [SerializeField] private Text nameText;
    [SerializeField] private Text tierText;
    [SerializeField] private Text raceText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private Text progressText;
    [SerializeField] private GameObject attackBadge;
    [SerializeField] private Text attackText;
    [SerializeField] private GameObject healthBadge;
    [SerializeField] private Text healthText;
    [SerializeField] private GameObject spellFooter;

    [SerializeField] private GameObject shieldBadge;
    [SerializeField] private GameObject nextShieldBadge;
    [SerializeField] private GameObject temporaryBadge;

    public void Render(CardViewModel model)
    {
        ApplyLayout(model.DisplayMode, model.IsMinion, !string.IsNullOrEmpty(model.ProgressText));

        nameText.text = UiTextFormatter.EllipsizeName(
            model.Name,
            NameFitsAtMinimumSize);
        tierText.text = $"T{model.Tier}";
        raceText.text = UiTextFormatter.EllipsizeName(
            model.RaceText,
            RaceFitsAtMinimumSize);
        descriptionText.text = UiTextFormatter.EllipsizeDescription(
            model.Description,
            model.DisplayMode,
            DescriptionFitsAtMinimumSize);
        costText.text = model.Cost.ToString();
        progressText.text = model.ProgressText ?? string.Empty;
        attackText.text = model.Attack.ToString();
        healthText.text = model.Health.ToString();

        background.color = CardTierPalette.GetBackground(model.Tier);
        costBadge.SetActive(model.ShowCost);
        progressRoot.SetActive(!string.IsNullOrEmpty(model.ProgressText));
        attackBadge.SetActive(model.IsMinion);
        healthBadge.SetActive(model.IsMinion);
        spellFooter.SetActive(!model.IsMinion);
        goldenFrame.SetActive(model.IsGolden);
        selectionFrame.SetActive(model.IsSelected);
        legalTargetFrame.SetActive(model.IsLegalTarget && model.IsInteractable);
        shieldBadge.SetActive(model.HasShield);
        nextShieldBadge.SetActive(model.HasNextCombatShield);
        temporaryBadge.SetActive(model.IsTemporary);
        disabledMask.SetActive(!model.IsInteractable);
        disabledReasonText.text = model.DisabledReason ?? string.Empty;
        costText.color = model.IsAffordable ? Color.white : UnaffordableColor;

        attackText.color = model.Attack > model.BaseAttack
            ? GrowthColor
            : Color.white;
        healthText.color = model.Health > model.BaseHealth
            ? GrowthColor
            : Color.white;
    }
}
```

示例中的三个 `FitsAtMinimumSize` 委托由 `CardView` 使用 `TextGenerator` 和第 7.3.5 节最大行数构建。示例省略了字号逐级尝试、能力标签实例化、状态徽章排序和成长浮字；这些逻辑仍只消费 ViewModel，不得反向调用领域层。`ApplyLayout` 只能应用第 7.3.3 节两套序列化几何，`UiTextFormatter` 必须遵循第 7.3.5 节的最小字号与 Unicode 截断规则。

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
    │   └── HandPanel
    │       ├── 5 × HandSlot
    │       └── PageControls
    ├── ActionRail
    │   ├── CardDetailPanel
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
- 五级酒馆商品行显示 4 个随从商品位和 1 个法术商品位；随从固定售价 3，法术固定售价 1。
- 商品行使用 `Full 240×360`，战斗区和手牌区使用 `Compact 160×240`。
- 战斗区固定显示 5 个槽位；随从可以在战斗区内部换位或出售，但不能撤回手牌。
- 手牌当前容量为 5，容量允许后续扩展到 10；每页始终显示 5 个槽位，仅当容量大于 5 时显示翻页控件。
- 购买的卡牌先进入手牌；随从成功打出、法术成功使用后从手牌消耗。
- 三行卡牌本体合计高度为 840；面板标题与两处间距必须控制在剩余内容高度内，不单独缩放某一行卡牌。
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

    public bool IsShopOpen { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsInteractionBlocked { get; set; }
    public string BlockReason { get; set; }
    public string StatusMessage { get; set; }

    public CardViewModel[] MinionOffers { get; set; }
    public CardViewModel SpellOffer { get; set; }
    public CardViewModel[] BattleCards { get; set; }
    public HandCardsState HandCards { get; set; }
    public ShopButtonStates Buttons { get; set; }
    public CardDetailPanelState DetailPanel { get; set; }
}

public sealed class HandCardsState
{
    public HandCardSlotState[] VisibleSlots { get; set; }
    public int Count { get; set; }
    public int Limit { get; set; }       // 当前 5，允许扩展到 10
    public int PageSize { get; set; }    // 固定为 5
    public int PageIndex { get; set; }
    public int PageCount { get; set; }
    public bool CanPageLeft { get; set; }
    public bool CanPageRight { get; set; }
}

public sealed class HandCardSlotState
{
    public int SlotIndex { get; set; }
    public CardViewModel Card { get; set; }
    public bool IsEmpty => Card == null;
}
```

`ShopScreenStateBuilder.Build()` 已实现为纯 C# 单向映射，签名为：

```csharp
public static ShopScreenState Build(
    ShopSession session,
    RunSession runSession = null,
    int selectedHandIndex = -1,
    int selectedBattleIndex = -1,
    int selectedEffectTargetIndex = -1,
    int handPageIndex = 0,
    string statusMessage = null);
```

它直接读取 `ShopSession` 的经济、商品、集合、冻结和阻塞选择状态；仅当 `runSession.Shop` 与传入 Session 为同一实例时，才读取待领取奖励并启用“进入战斗”。空商品/槽位保留为 `null`，索引不会因过滤而移动。

当 `selectedHandIndex` 指向定向法术或带直接目标战吼的随从时，Builder 调用 `ShopTargetingQuery`：合法战斗卡设置 `IsLegalTarget=true`，其他非空战斗卡设置 `IsInteractable=false` 和目标原因。该步骤只影响 ViewModel，不修改 `ShopSession`。

UI 层统一使用 `Hand` 命名；现有领域对象、错误码和 Controller 方法中的 `Bench` 暂时保留，Builder 负责边界翻译，不在本阶段扩大领域重构。`VisibleSlots` 每页构建 5 项并包含空槽，`SlotIndex` 必须保留原始领域槽位索引，不能改成页内的 0-4。

### 9.1 按钮状态

```csharp
public sealed class ShopActionButtonState
{
    public string Text { get; set; }
    public bool IsVisible { get; set; }
    public bool IsInteractable { get; set; }
    public bool IsActive { get; set; }
    public string DisabledReason { get; set; }
}

public sealed class ShopButtonStates
{
    public ShopActionButtonState Refresh { get; set; }
    public ShopActionButtonState Freeze { get; set; }
    public ShopActionButtonState Upgrade { get; set; }
    public ShopActionButtonState Sell { get; set; }
    public ShopActionButtonState EndShop { get; set; }
}
```

按钮状态只保存渲染数据，不保存回调或领域对象。Builder 按以下规则统一计算：

- 免费刷新次数大于 0 时，刷新按钮显示免费次数并优先消耗免费刷新；否则显示付费刷新费用。
- 酒馆达到最高等级时，升级按钮禁用并提供明确的 `DisabledReason`。
- `Freeze.IsActive` 表示当前冻结态；冻结按钮只有在商店开放且无全局阻塞时可交互。
- 出售按钮仅在选中战斗区随从时启用；手牌卡牌不能出售。
- 发现、效果选择或待领取奖励形成模态阻塞时，`IsInteractionBlocked=true`，所有商店操作统一禁用并复用 `BlockReason`。
- `IsVisible` 控制布局占位，`IsInteractable` 控制操作合法性，两者不得混用。

### 9.2 详情面板状态

```csharp
public enum ShopCardLocation
{
    None,
    MinionOffer,
    SpellOffer,
    Battle,
    Hand
}

public enum CardDetailStatusType
{
    Growth,
    PermanentShield,
    NextCombatShield,
    Temporary
}

public sealed class CardDetailStatusState
{
    public CardDetailStatusType Type { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
}

public sealed class CardDetailPanelState
{
    public CardViewModel Card { get; set; }
    public ShopCardLocation Location { get; set; }
    public int SlotIndex { get; set; } = -1;
    public CardDetailStatusState[] Statuses { get; set; }
    public bool IsVisible => Card != null;
}
```

详情面板位于右侧固定栏，未选中卡牌时隐藏内容但保留栏位。`Location + SlotIndex` 标识选择来源；`Statuses` 只包含卡牌实际拥有的成长、永久护盾、下场护盾和临时状态，不为不存在的状态生成灰色占位。

按钮状态在 Builder 中的全局门禁统一计算为：

```csharp
var blocked = session.PendingDiscover != null ||
              session.PendingChoice != null ||
              hasPendingReward;

state.IsInteractionBlocked = blocked;
state.BlockReason = blocked ? ResolveBlockReason(session) : null;

var unlocked = session.IsShopOpen && !state.IsInteractionBlocked;
state.Buttons.Refresh.IsInteractable = unlocked &&
    (session.FreeRefreshes > 0 || session.Gold >= ShopEconomyRules.RefreshCost);
state.Buttons.Upgrade.IsInteractable = unlocked &&
    session.TavernTier < ShopEconomyRules.MaximumTavernTier &&
    !session.UpgradedThisRound && session.Gold >= session.CurrentUpgradeCost;
state.Buttons.Freeze.IsInteractable = unlocked;
state.Buttons.Sell.IsInteractable = unlocked && selectedBattleIndex >= 0;
state.Buttons.EndShop.IsInteractable = unlocked && runSession != null;
```

商品可购买状态还需要考虑：

- 金币是否足够。
- 手牌（领域层备战区）是否有空位。
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

正式运行时只保留一个序列化字段：

```csharp
[SerializeField] private ShopScreenView screenView;
```

初始化流程：

```csharp
if (screenView == null)
{
    Debug.LogError("[ShopTest] Formal ShopScreenView is not configured.");
    return;
}

screenView.Bind(this);
RefreshAll();
```

`InitializeForTests()` 允许不绑定 View 的 headless Controller，只用于验证 Controller/领域调用；正常 `Start()` 缺少 `screenView` 时直接报错，不再动态创建回退界面。

`RefreshAll()` 优先渲染正式 View：

```csharp
private void RefreshAll()
{
    if (!initialized || screenView == null)
    {
        return;
    }

    var state = ShopScreenStateBuilder.Build(
        session,
        runSession,
        selectedHandIndex: selectedBenchIndex,
        selectedBattleIndex: selectedBattleIndex,
        selectedEffectTargetIndex: selectedEffectTargetIndex,
        statusMessage: StatusMessage);

    screenView.Render(state);
    screenView.ShowStatus(StatusMessage);
    RefreshFormalOverlays();
}
```

迁移对照完成后已删除旧动态 UI；商店不再包含运行时 `CreateText`、`CreatePanel`、`CreateButton` 或自动创建裸 Controller 的回退路径。

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
| 手牌随从拖入战斗位 | `PlayBenchMinion` | `ShopSession.PlayMinion` |
| 战斗位拖到另一位置 | `RepositionBattleMinion` | `ShopSession.RepositionBattleMinion` |
| 使用手牌法术 | `UseBenchSpell` | `ShopSession.UseSpell` |
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
5. 确认场景中只有一个 `EventSystem`。
6. 确认 Canvas 使用 1920×1080 和 Match 0.5。
7. 暂时保留场景名 `ShopTest`，不修改现有跳转字符串。

场景必须序列化唯一 Controller；不再提供运行时自动创建逻辑。

## 16. 实施和提交顺序

### 16.1 提交 1：共享卡牌

- 新增 `CardViewModel`。
- 新增 `ShopCardViewModelFactory`。
- 冻结商店线框图 v0.1。
- 新增 `ShopScreenState`、手牌分页、按钮和详情面板状态契约。
- 新增 `CardView`。
- 制作 `PF_Card`。
- 补充 Factory 和状态模型 EditMode 测试。

验收：普通、金色、永久成长、护盾、下场护盾和临时法术显示正确。

### 16.2 提交 2：静态商店布局

- 制作 `PF_ShopSlot`。
- 制作 `PF_ShopScreen`。
- 制作 `PF_ChoiceOverlay` 静态骨架，并实现 `ShopScreenView` / `ChoiceOverlayView` 的 ViewModel 渲染入口；本提交不接入领域 Session。
- 使用临时数据验证长文案、五张卡和金色边框。

验收：1920×1080 和 1920×1200 下无核心遮挡。

完成记录（2026-07-17）：三个 Prefab、可复现 Editor 构建/截图工具和 `ShopUiPreview.unity` 已落地；新增 5 个 EditMode 用例后全量 EditMode 178 / 178、PlayMode 14 / 14。商店双分辨率与选择弹窗截图位于 `ui-concepts/unity-validation/pf-shop-screen-v0.1/`。`Bind()` 入口已保留，但正式场景接线和真实 Session 交互仍属于 16.3 / 16.4。

### 16.3 提交 3：接入真实 Session

- 将已实现的 `ShopScreenStateBuilder` 接入 Controller。
- Controller 接入 `ShopScreenView`。
- 接通购买、出售、刷新、冻结、升级、上阵和换位。

验收：相同输入下，新旧 UI 的领域结果一致。

完成记录（2026-07-17）：`ShopTest.unity` 已切换到序列化正式 View；`ShopTestController` 通过 `ShopScreenStateBuilder` 渲染真实 Session，并用 `useLegacyRuntimeUi` 保留旧动态 UI。购买、按钮、卡牌/槽位交互、选择、奖励和进战斗共用原 Controller 方法；`OnShopEvent()` 只入事件日志，统一由操作完成点执行一次最终 Render。正式场景、强制发现和升级/拖拽上阵/换位/出售输入回归通过后，全量 EditMode 178 / 178、PlayMode 17 / 17。

拖拽完成语义：`ShopSlotView.OnDrop()` 调用 Controller 后必须标记该拖拽对象已由槽位处理；因为 Controller 会立即重建卡牌实例，旧对象在随后到达的 `ShopCardView.OnEndDrag()` 中直接销毁，不得重新挂回原槽。未命中任何 DropHandler 的拖拽仍恢复原父节点和原坐标。

### 16.4 提交 4：弹窗和反馈

- 接入发现和效果选择。
- 接入待领取奖励。
- 增加属性成长浮字和错误 Toast。
- 增加冻结、护盾和临时法术反馈。

验收：阻塞选择未完成时，其他商店操作全部锁定。

完成记录（2026-07-17）：发现、效果选择和待领取奖励已接入正式弹窗，四种族选择所需的 4 候选容量已覆盖。`ShopScreenView` 通过 `InstanceId` 快照比较播放成长/下降浮字和新护盾徽章脉冲，冻结切换脉冲操作按钮；Controller 以消息修订号保证成功/错误 Toast 只触发一次，运行时自动淡出。反馈验收图位于 `ui-concepts/unity-validation/pf-shop-screen-v0.1/shop-feedback-1920x1080.png`；全量 EditMode 180 / 180、PlayMode 17 / 17。

首场景修复记录（2026-07-19）：`BeforeSceneLoad` 不再立即检查编辑器当前活动场景，避免直接从 `ShopTest` 进入 Play 时在序列化正式 Controller 恢复前创建 legacy Controller。回退创建统一延后到 `sceneLoaded`，并增加初始化钩子无副作用及正式场景单 Controller 回归；全量 PlayMode 18 / 18。

双分辨率修复记录（2026-07-19）：真实 Session 在 1920×1200 首次渲染时，名称适配抛出 “The ellipsis does not fit the target text area”，导致卡牌只完成部分渲染并停在 Prefab 默认中心锚点。根因包含两层：拉伸文字节点的当帧 Rect 不稳定，以及动态字体 `TextGenerator` 输出使用 `Canvas.scaleFactor` 作为 `pixelsPerUnit`，旧代码却将像素结果直接与逻辑契约尺寸比较。`CardView` 现直接使用 Full/Compact 冻结契约区域，并按 `pixelsPerUnit` 还原生成结果；单行宽度使用 `GetPreferredWidth`，高度使用实际字形范围。商店与选择弹窗先锚定实例再 Render，商店输入组件和空槽显隐也在 Render 前完成；截图工具在每个目标画布尺寸下重新 Render。新增与 1920×1200 Match 0.5 相同的约 1.054 缩放回归，修复前稳定失败、修复后通过；相关 UI EditMode 17 / 17、全量 PlayMode 18 / 18。

迁移对照验证记录（2026-07-19）：正式和 legacy Controller 分别绑定两份同配置、同随机种子的 `ShopSession`，按相同操作脚本逐步比较操作结果及领域快照。快照覆盖经济、商品、牌池、完整手牌/战斗区卡牌状态、待处理候选、阶段统计，并在末尾增加 RNG 消耗刷新以检测正式 Render / Builder 的隐藏副作用。普通经济/阵容/定向法术与三连发现两组差分专项 2 / 2、全量 PlayMode 20 / 20 通过；结合既有正式输入和商店—战斗—返回回归，可进入 legacy 运行时 UI 删除阶段。

### 16.5 提交 5：测试和清理

- 更新 PlayMode 测试。
- 删除旧动态 UI 构建代码。
- 保留 Controller 的公开测试接口。
- 只清理本次迁移产生的无用字段和方法。

验收：正式商店不再依赖运行时 `CreateText`、`CreatePanel` 和 `CreateButton` 搭建界面。

完成记录（2026-07-19）：删除 `useLegacyRuntimeUi`、场景回退创建钩子以及动态 Canvas、面板、按钮、卡牌和弹窗代码；`ShopTestController.Start()` 现在要求序列化正式 `ShopScreenView`，测试入口仍可使用 headless Controller。差分回归转为正式 View/headless 副作用对照；正式商店 Prefab EditMode 6 / 6、专项 2 / 2、全量 PlayMode 18 / 18 通过。

## 17. 自动化测试

### 17.1 EditMode

- 普通随从 ViewModel 属性正确。
- 金色随从使用 GoldenAttack 和 GoldenHealth。
- 永久成长后的 Current 和 Base 属性正确。
- 永久护盾和下场战斗护盾分别显示。
- 临时法术显示临时标签。
- `Full` 和 `Compact` 根尺寸及第 7.3.3 节关键 Rect 与契约一致。
- 随从/法术变体分别显示攻防或法术页脚，法术不会进入金色、成长和护盾状态。
- 当前 64 字最长随从描述和 45 字最长法术描述在 `Full` 模式不截断，并保留 66/45 的设计容量回归。
- `Compact` 超长描述显示省略号，但 ViewModel 和选中说明仍保留完整原文。
- 永久护盾、下场护盾和临时徽章能同时显示并保持固定顺序。
- 选中与合法目标同时存在时显示内外双框；不可操作时隐藏目标呼吸并保留错误点击。
- 空状态默认集合与子状态非空，可安全完成第一次渲染。
- 手牌当前 5 格和未来 10 格容量均按每页 5 格表达，翻页后保留领域槽位索引。
- 免费/付费刷新、最高等级、冻结激活、出售选择和全局阻塞均能由按钮状态完整表达。
- 详情面板能区分商品、战斗区和手牌来源，并只显示实际存在的状态。
- Builder 从真实 `ShopSession` 映射经济、商品、战斗/手牌、选择、详情和禁用原因，不修改任何领域对象。
- 满手牌会禁用购买但不影响刷新；发现、效果选择和待领取奖励会覆盖所有按钮及卡牌交互。
- `ShopTargetingQuery` 与 Builder 能映射定向法术和战吼合法目标，遵循种族、金色、Token 和无目标战吼规则，查询前后领域状态与 RNG 序列一致。
- 刷新和升级按钮状态符合金币、等级和阻塞选择。
- 空手牌位和满手牌（领域层备战区）的商品状态正确。

### 17.2 PlayMode

- 加载 `ShopTest` 后只有一个 Controller、一个 Canvas 和一个 EventSystem。
- 点击刷新后金币和刷新次数同步更新。
- 购买随从后商品位清空，手牌区出现卡牌。
- 手牌随从拖入战斗位成功。
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
- 金币不足、手牌已满、目标无效时能看到明确原因。
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
