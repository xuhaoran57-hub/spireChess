# PF_Card Unity 落地任务单与逐项验收矩阵 v0.1

## 1. 文档信息

- 状态：Completed（2026-07-17，EditMode 173 / 173、PlayMode 14 / 14）
- 日期：2026-07-17
- Unity：2022.3.62f3c1
- UI 技术：UGUI + `UnityEngine.UI.Text`
- 工程入口：`sc/`
- 需求基线：`phase-7-ui-vertical-slice-technical-design-v0.1.md` 第 7.1-7.4 节
- 本轮范围：共享 `CardView + PF_Card`、实际字体/几何验证和对应测试
- 不在本轮：`PF_ShopScreen`、`PF_ShopSlot`、Controller 接入、正式插画和完整动画

本任务单是 Unity 落地执行清单。设计冲突时以 Phase 7 技术设计为准；若 Unity 实测证明 66 字完整描述无法容纳，应先更新几何契约和本文，再继续制作商店界面。

## 2. 本轮交付物

| 交付物 | 目标路径 | 必须提交 |
| --- | --- | --- |
| 卡牌渲染组件 | `sc/Assets/Scripts/UI/Common/CardView.cs` | 是 |
| 共享卡牌 Prefab | `sc/Assets/Prefabs/UI/Common/PF_Card.prefab` | 是 |
| Layout EditMode 测试 | `sc/Assets/Tests/EditMode/CardViewLayoutTests.cs` | 是 |
| Render EditMode 测试 | `sc/Assets/Tests/EditMode/CardViewRenderTests.cs` | 是 |
| 文案与实际字体测试 | 可合并到上述测试文件 | 是 |
| Full/Compact 验收截图 | 建议放入 `ui-concepts/unity-validation/pf-card-v0.1/` | 是 |
| 卡牌预览场景 | `sc/Assets/Scenes/CardUiPreview.unity` | 可选；不得成为运行时依赖 |

Unity 新建的目录、脚本、Prefab、测试和截图必须保留对应 `.meta`。不要手动复用其他资源的 GUID。

## 3. 进入条件

- [x] 使用 Unity Hub 打开 `sc/`，版本确认为 `2022.3.62f3c1`。
- [x] 等待首次 Import 完成，Console 中没有编译错误、重复 GUID 或 Missing Script。
- [x] 检查当前新增源码的 `.meta` 已导入，未被 Unity 自动重新生成成另一组 GUID。
- [x] Test Runner 能发现当前 EditMode 基线；代码侧共有 164 个测试标注。若发现数量不同，先记录差异。
- [x] 运行全部 EditMode；新增 UI 纯 C# 测试必须通过。
- [x] 运行既有 PlayMode 基线，预期 14 个用例通过。
- [x] 保存一份进入前 Test Runner 截图或 XML 结果。

进入门失败时先修复导入/编译/测试问题，不开始制作 Prefab。

## 4. 实施任务单

### T0：建立 Unity 基线

- [x] 完成第 3 节全部进入条件。
- [x] 使用独立 batchmode 日志重新导入并编译，确认 Console 零 Error；非交互门禁不依赖 Error Pause。
- [x] 确认 `SpireChess.Runtime` 引用正常，`CardViewModel` 和 `UiTextFormatter` 可被 MonoBehaviour 使用。
- [x] 确认 EditMode 测试程序集引用 `SpireChess.Runtime`，无需新增程序集引用。

完成证据：进入前 Test Runner 结果、零编译错误 Console 截图。

### T1：创建目录和 CardView 骨架

- [x] 在 Unity 中创建 `Assets/Prefabs/UI/Common/`。
- [x] 创建 `Assets/Scripts/UI/Common/CardView.cs`，命名空间为 `SpireChess.UI`。
- [x] `CardView` 只依赖 `CardViewModel`、`UiTextFormatter` 和 UGUI，不引用 `ShopSession`、`RunSession` 或 Controller。
- [x] `Render(CardViewModel model)` 对 `null` 明确抛出 `ArgumentNullException`。
- [x] `Render()` 可重复调用；每次都完整重置上一张卡留下的显隐、颜色、字号和文本。
- [x] 不在 `CardView` 中执行购买、拖拽、出售或领域校验。

完成证据：脚本编译通过，组件可挂到空 UGUI GameObject。

### T2：创建 PF_Card 层级

按以下名称创建节点，测试将依赖这些稳定名称：

```text
PF_Card
├── Background
├── RaceSkin
├── ArtworkMask
│   └── Artwork
├── NormalFrame
├── GoldenFrame
├── CostBadge
│   └── Cost
├── TierBadge
│   └── Tier
├── NamePlate
│   └── Name
├── InfoPanel
│   ├── RaceOrSpellType
│   ├── AbilityLabelRow
│   │   ├── Label0
│   │   ├── Label1
│   │   └── Label2
│   ├── Description
│   └── Progress
│       ├── ProgressFill
│       └── ProgressText
├── StateBadgeRow
│   ├── ShieldBadge
│   ├── NextCombatShieldBadge
│   └── TemporaryBadge
├── AttackBadge
│   └── Attack
├── HealthBadge
│   └── Health
├── SpellFooter
├── GrowthFeedbackRoot
├── SelectionFrame
├── LegalTargetFrame
└── DisabledMask
    ├── DisabledIcon
    └── DisabledReason
```

- [x] 根节点挂载 `RectTransform`、`Image`、`CanvasGroup`、`CardView`。
- [x] 根节点 `Image.raycastTarget = true`。
- [x] 所有装饰 `Image` 和文本关闭 `raycastTarget`。
- [x] `RaceSkin` 位于背景与插画之间；普通/金色切换不得覆盖种族皮肤和等级底色。
- [x] `ArtworkMask` 只裁切插画；外框、徽章和覆盖层不放在 Mask 内。
- [x] `GoldenFrame`、状态徽章、选中框、合法目标框和禁用层默认关闭。
- [x] 三个能力标签节点固定存在，通过显隐复用，不在每次 Render 时动态创建。
- [x] `ShopCardView` 不放在共享 Prefab 的强制依赖中；由商店实例化后按上下文挂载或初始化。

完成证据：Prefab 层级截图、根节点 Inspector 截图、无 Missing Reference。

### T3：落实两套序列化几何

建议在 `CardView` 中使用可序列化 Layout Binding：每个目标 `RectTransform` 保存一份 Full Rect 和一份 Compact Rect。`ApplyLayout()` 只切换这两套数据，不按屏幕尺寸重新计算。

坐标约定：子节点 `anchorMin = anchorMax = (0, 1)`、`pivot = (0, 1)`；表中 `x, y` 转为 `anchoredPosition = (x, -y)`。

| 区域 | Full | Compact |
| --- | --- | --- |
| 根节点 | `240×360` | `160×240` |
| 外框安全区 | `6, 6, 228, 348` | `4, 4, 152, 232` |
| 插画 | `12, 12, 216, 184` | `8, 8, 144, 112` |
| 费用徽章 | `8, 8, 48, 48` | `6, 6, 34, 34` |
| 等级徽章 | `184, 8, 48, 48` | `120, 6, 34, 34` |
| 状态徽章行 | `60, 157, 120, 22` | `42, 91, 76, 18` |
| 名称牌 | `24, 181, 192, 32` | `16, 108, 128, 26` |
| 信息面板 | `12, 199, 216, 149` | `8, 122, 144, 110` |
| 种族/法术类型 | `44, 215, 152, 18` | `28, 136, 104, 14` |
| 能力标签行 | `20, 235, 200, 20` | `12, 154, 136, 16` |
| 描述，无进度 | `12, 256, 216, 52` | `12, 172, 136, 33` |
| 描述，有进度 | `12, 256, 216, 31` | `12, 172, 136, 21` |
| 进度条 | `62, 293, 116, 18` | `44, 197, 72, 14` |
| 攻击徽章 | `8, 308, 44, 44` | `6, 204, 32, 32` |
| 生命徽章 | `188, 308, 44, 44` | `122, 204, 32, 32` |
| 法术页脚 | `58, 318, 124, 22` | `42, 211, 76, 16` |
| 选中/目标/禁用层 | `0, 0, 240, 360` | `0, 0, 160, 240` |

- [x] Full 与 Compact 根节点严格保持 `2:3`。
- [x] 隐藏费用、进度、攻防或法术页脚时，其他区域不重排。
- [x] 切换模式 20 次后坐标无累计偏移。
- [x] Prefab Stage 与运行时实例的 Rect 一致。

完成证据：两套 Inspector 数值截图、自动几何测试。

### T4：字体、换行和截断

| 内容 | Full 基准 | Compact 基准 | Full/Compact 最小 |
| --- | ---: | ---: | ---: |
| 费用、等级、攻防 | 26 | 20 | 不缩放 |
| 名称 | 22 | 16 | 18 / 14 |
| 种族/法术类型 | 14 | 11 | 12 / 11 |
| 能力标签 | 12 | 10 | 11 / 10 |
| 描述 | 14 | 11 | 11 / 10 |
| 进度 | 12 | 10 | 不缩放 |

描述行数：

| 变体 | Full | Compact |
| --- | ---: | ---: |
| 随从，无进度 | 4 | 3 |
| 随从，有进度 | 2 | 2 |
| 法术 | 5 | 4 |

- [x] 所有 `Text.richText = false`。
- [x] 名称、种族、法术类型和能力标签禁止换行。
- [x] 描述允许换行，显式换行保留，连续空白折叠。
- [x] 关闭 Unity 自动 Best Fit，字号由 `CardView` 显式从基准递减到最小值。
- [x] 使用实际 Text、字体、Rect 和 `TextGenerator` 构造 `fits` 判定。
- [x] `fits` 同时检查宽高、生成行数和第 7.3.5 节最大行数。
- [x] 到最小字号仍溢出时，把 `fits` 传给 `UiTextFormatter`。
- [x] Full 描述若仍不适配，让测试失败；不得捕获后静默截断。
- [x] Compact 描述允许追加单个 `…`。
- [x] 名称/类型截断不得切开代理对、组合字符或 Unicode 文本元素。
- [x] Full 模式实际字体完整显示 66 文本元素的金色 `old_tower_guide` 描述。
- [x] Full 模式实际字体完整显示 45 文本元素的最长法术描述。
- [x] 普通万蹄奔潮 64 字描述完整显示且没有异常缩小。

完成证据：TextGenerator EditMode 测试、三张长文案截图和最终字号记录。

### T5：能力标签、进度和卡牌变体

- [x] 调用 `UiTextFormatter.FormatAbilityLabels()`。
- [x] Full 最多 3 个标签，Compact 最多 2 个标签。
- [x] 超出容量时最后一格显示 `+N`，N 为所有被隐藏标签数。
- [x] `ProgressText` 为空时隐藏整个 Progress，使用无进度描述 Rect。
- [x] `ProgressText` 非空时显示进度并切换有进度描述 Rect。
- [x] 暂无进度 Fill 数值契约时，ProgressFill 只显示占位，不从描述文字猜测进度。

变体矩阵：

| 变体 | 费用 | 攻防 | 法术页脚 | 金色 | 描述模式 |
| --- | --- | --- | --- | --- | --- |
| Full 随从商品 | 显示 | 显示 | 隐藏 | 可选 | Full |
| Full 法术商品 | 显示 | 隐藏 | 显示 | 强制普通 | Full |
| Compact 持有随从 | 隐藏 | 显示 | 隐藏 | 可选 | Compact |
| Compact 持有法术 | 隐藏 | 隐藏 | 显示 | 强制普通 | Compact |

- [x] 同一个 Prefab 连续 Render 四种变体，不残留上一变体节点。
- [x] 已拥有卡牌即使 `Cost` 有值也遵从 `ShowCost=false`。
- [x] 法术忽略金色、攻防、成长和护盾视觉。

完成证据：四变体同屏截图、Render 状态重置测试。

### T6：视觉状态和叠加优先级

| ViewModel | 节点/表现 | 验收要点 |
| --- | --- | --- |
| 普通 | `NormalFrame` | 默认显示 |
| `IsGolden` | `GoldenFrame` | 替换普通外框，不替换种族底色 |
| `Attack > BaseAttack` | Attack 成长绿 `#62E6A6` | 金色以金色基础攻击比较 |
| `Health > BaseHealth` | Health 成长绿 `#62E6A6` | 仅数字变色 |
| `HasShield` | `ShieldBadge` `#68C7FF` | 排在状态行左侧 |
| `HasNextCombatShield` | `NextCombatShieldBadge` `#9EEBFF` | 与永久护盾并存 |
| `IsTemporary` | `TemporaryBadge` `#C98BFF` | 不改变框体 |
| `IsSelected` | 青色内框 `#59D8FF` | 与合法目标可同时显示 |
| `IsLegalTarget && IsInteractable` | 绿色外框 `#6CFF8F` | 位于选中框外层 |
| `!IsInteractable` | 黑色 55% 禁用遮罩，视觉饱和度降至 35% | 最高优先级，隐藏目标呼吸并停止成长反馈 |
| `!IsAffordable` | 费用 `#FF7B7B` | 不等同于隐藏费用 |

- [x] 三个状态徽章顺序固定为永久护盾、下场护盾、临时。
- [x] 三徽章同时出现仍位于卡牌 `2:3` 边界内。
- [x] 选中 + 合法目标显示内外双框。
- [x] 不可操作时压暗选中框、关闭合法目标光效、停止成长反馈并把视觉饱和度降至 35%，但保留金色和状态事实。
- [x] `DisabledMask` 不阻断根节点 raycast；点击仍可显示 `DisabledReason`。
- [x] 首次 Render 不播放成长动画；本轮允许只保留 `GrowthFeedbackRoot` 空挂点。

完成证据：状态组合截图、Render EditMode 测试。

### T7：编写自动化测试

`CardViewLayoutTests` 至少覆盖：

- [x] 从 `Assets/Prefabs/UI/Common/PF_Card.prefab` 加载成功。
- [x] 关键层级路径全部存在且名称正确。
- [x] 根节点组件完整，所有序列化字段非空。
- [x] Full/Compact 根尺寸和第 4 节每个 Rect 精确一致。
- [x] 两种模式切换不产生累计偏移。
- [x] `RaceSkin` 层级正确，外框、徽章、目标框和禁用层不位于 ArtworkMask 下。
- [x] 根节点可接收 raycast，装饰节点不接收。

`CardViewRenderTests` 至少覆盖：

- [x] `Render(null)` 明确失败。
- [x] Full 普通随从商品。
- [x] Full 金色随从商品。
- [x] Full 普通法术商品。
- [x] Compact 持有随从。
- [x] Compact 临时法术。
- [x] 有/无进度描述 Rect 切换。
- [x] 5 个能力标签分别渲染为 Full `2 + +3`、Compact `1 + +4`。
- [x] 成长攻防、双护盾、临时三徽章。
- [x] 选中 + 合法目标双框。
- [x] 不可操作覆盖目标状态并显示原因。
- [x] 先 Render 金色复杂状态，再 Render 普通法术，无状态泄漏。
- [x] 66 字随从和 45 字法术在 Full 下完整显示。
- [x] Compact 长描述追加 `…`，且不切开 `e\u0301` 或 emoji。

测试要求：

- [x] 优先直接实例化 Prefab 并检查真实 UGUI 状态，不复制 `CardView` 内部逻辑到测试。
- [x] TextGenerator 测试使用 Prefab 实际字体和 Rect，不用“字符数小于 N”代替。
- [x] 测试失败信息包含模式、卡牌变体、节点路径和实际/期望值。

完成证据：新增测试列表、Test Runner 全绿截图/XML。

### T8：手工视觉验收

创建一个 1920×1080 Canvas，`CanvasScaler` 使用 1920×1080、Match 0.5。一次摆放以下卡牌：

1. Full 普通随从商品。
2. Full 金色 66 字随从商品。
3. Full 45 字法术商品。
4. Compact 普通持有随从。
5. Compact 金色成长 + 永久护盾 + 下场护盾。
6. Compact 临时法术。
7. Compact 选中 + 合法目标。
8. Compact 不可操作 + DisabledReason。

- [x] 1920×1080 下完成截图。
- [x] 1920×1200 下完成截图。
- [x] 100% Game View 缩放检查像素边缘和文字清晰度。
- [x] 金色、状态徽章、选中框和目标框没有被 Mask 裁切。
- [x] Full 与 Compact 视觉身份一致，不像两套独立卡牌。
- [x] 占位图片不被误当成最终美术验收。

完成证据：两分辨率截图、问题清单归零或带明确后续项。

### T9：回归与交付

- [x] 新增测试全部通过。
- [x] 全量 EditMode 通过；结果数量应等于进入基线加本轮新增用例。
- [x] 既有 PlayMode 14/14 通过。
- [x] Console 无 Error、Missing Reference、字体溢出契约错误。
- [x] Prefab Apply 后重新打开 Unity，序列化引用仍完整。
- [x] 在空场景单独实例化 Prefab，Render 不依赖 ShopTest 场景对象。
- [x] 不修改 `ShopSession`、平衡配置和战斗规则。
- [x] 更新 TODO，将 `PF_Card` 标记完成并记录测试数和截图路径。

## 5. 逐项验收矩阵

| ID | 类别 | 验收场景 | 通过标准 | 方式 | 证据 | 结果 |
| --- | --- | --- | --- | --- | --- | --- |
| ENV-01 | 环境 | Unity 版本 | 2022.3.62f3c1，无升级提示造成的序列化变更 | 手工 | ProjectVersion/Hub 截图 | 通过 |
| ENV-02 | 环境 | 进入基线 | 当前 EditMode 全绿，既有 PlayMode 14/14 | 自动 | Test Runner XML/截图 | 通过 |
| ENV-03 | 环境 | 导入完整性 | 无重复 GUID、Missing Script、编译 Error | 自动+手工 | Console 截图 | 通过 |
| PFB-01 | Prefab | 资源路径 | `PF_Card.prefab` 可由 AssetDatabase 加载 | 自动 | Layout test | 通过 |
| PFB-02 | Prefab | 层级 | 第 4 节所有稳定路径（含 `RaceSkin`）存在 | 自动 | Layout test | 通过 |
| PFB-03 | Prefab | 序列化引用 | `CardView` 必需字段全部非空 | 自动 | Layout test | 通过 |
| PFB-04 | Prefab | Mask 边界 | 只有 Artwork 在 ArtworkMask 下 | 自动 | Layout test | 通过 |
| GEO-01 | 几何 | 根尺寸 | Full 240×360，Compact 160×240 | 自动 | Layout test | 通过 |
| GEO-02 | 几何 | 区域 Rect | 第 4 节 16 个子区域 Rect 两模式精确匹配 | 自动 | Layout test | 通过 |
| GEO-03 | 几何 | 模式切换 | 连续切换 20 次无累计偏移 | 自动 | Layout test | 通过 |
| GEO-04 | 几何 | 显隐稳定 | 隐藏费用/进度/攻防不重排 | 自动+手工 | Render test/截图 | 通过 |
| TXT-01 | 文案 | 名称单行 | 不换行；最小字号后按文本元素截断 | 自动 | TextGenerator test | 通过 |
| TXT-02 | 文案 | 类型单行 | 种族和法术类型不换行 | 自动 | TextGenerator test | 通过 |
| TXT-03 | 文案 | 空白 | 显式换行保留，连续空白折叠 | 自动 | Formatter/Render test | 通过 |
| TXT-04 | 文案 | Rich Text | `<b>` 等标签按普通字符显示 | 自动 | Render test | 通过 |
| TXT-05 | 文案 | 66 字随从 | Full 金色 `old_tower_guide` 完整显示 | 自动+手工 | TextGenerator test/截图 | 通过 |
| TXT-06 | 文案 | 45 字法术 | Full 最长法术完整显示 | 自动+手工 | TextGenerator test/截图 | 通过 |
| TXT-07 | 文案 | Compact 溢出 | 追加一个 `…`，不突破最大行数 | 自动 | Render test | 通过 |
| TXT-08 | 文案 | Unicode | 不切开代理对、组合字符、emoji | 自动 | Render test | 通过 |
| TXT-09 | 文案 | Full 失败策略 | 放不下时测试明确失败，不静默截断 | 自动 | Contract test | 通过 |
| LBL-01 | 标签 | Full 容量 | 最多 3 个，5 标签显示前 2 + `+3` | 自动 | Render test | 通过 |
| LBL-02 | 标签 | Compact 容量 | 最多 2 个，5 标签显示前 1 + `+4` | 自动 | Render test | 通过 |
| VAR-01 | 变体 | Full 随从商品 | 费用、等级、攻防显示，法术页脚隐藏 | 自动+手工 | Render test/截图 | 通过 |
| VAR-02 | 变体 | Full 法术商品 | 费用、等级、法术页脚显示，攻防隐藏 | 自动+手工 | Render test/截图 | 通过 |
| VAR-03 | 变体 | Compact 持有随从 | 费用隐藏，攻防显示 | 自动 | Render test | 通过 |
| VAR-04 | 变体 | Compact 持有法术 | 费用和攻防隐藏，法术页脚显示 | 自动 | Render test | 通过 |
| VAR-05 | 变体 | 重复 Render | 四种变体轮换无状态泄漏 | 自动 | Render test | 通过 |
| STA-01 | 状态 | 金色 | 只替换框体，不覆盖底色和文本 | 自动+手工 | Render test/截图 | 通过 |
| STA-02 | 状态 | 攻击成长 | 仅高于 BaseAttack 时数字变绿 | 自动 | Render test | 通过 |
| STA-03 | 状态 | 生命成长 | 仅高于 BaseHealth 时数字变绿 | 自动 | Render test | 通过 |
| STA-04 | 状态 | 永久护盾 | 永久护盾徽章显示 | 自动 | Render test | 通过 |
| STA-05 | 状态 | 下场护盾 | 下场护盾独立显示 | 自动 | Render test | 通过 |
| STA-06 | 状态 | 临时 | 临时徽章显示且不改框体 | 自动 | Render test | 通过 |
| STA-07 | 状态 | 三徽章 | 顺序固定且不越界 | 自动+手工 | Render test/截图 | 通过 |
| STA-08 | 状态 | 选中 | 青色内框显示 | 自动 | Render test | 通过 |
| STA-09 | 状态 | 合法目标 | 可交互时绿色外框显示 | 自动 | Render test | 通过 |
| STA-10 | 状态 | 双框 | 选中内框与合法目标外框并存 | 自动+手工 | Render test/截图 | 通过 |
| STA-11 | 状态 | 不可操作 | 55% 遮罩与 35% 饱和度优先，目标/成长反馈关闭，原因显示 | 自动+手工 | Render test/截图 | 通过 |
| STA-12 | 状态 | 金币不足 | 费用红色，事实状态保留 | 自动 | Render test | 通过 |
| RAY-01 | 输入 | 根节点 | 根 Image 可接收 raycast | 自动 | Layout test | 通过 |
| RAY-02 | 输入 | 装饰层 | 装饰 Image/Text 不抢输入 | 自动 | Layout test | 通过 |
| RAY-03 | 输入 | 禁用层 | 遮罩不阻断根节点错误点击 | 自动+PlayMode | Test/手工点击 | 通过 |
| REG-01 | 回归 | Formatter | 现有 10 个 Formatter 测试通过 | 自动 | Test Runner | 通过 |
| REG-02 | 回归 | ViewModel/Factory | 现有 Factory/状态/目标测试通过 | 自动 | Test Runner | 通过 |
| REG-03 | 回归 | EditMode 全量 | 全量无失败 | 自动 | XML/截图 | 通过 |
| REG-04 | 回归 | PlayMode | 既有 14/14，无新增回归 | 自动 | XML/截图 | 通过 |
| RES-01 | 分辨率 | 1920×1080 | 无裁切、文字重叠或状态越界 | 手工 | 截图 | 通过 |
| RES-02 | 分辨率 | 1920×1200 | 无裁切、文字重叠或状态越界 | 手工 | 截图 | 通过 |

## 6. 建议的 Unity 执行顺序

```text
导入与基线测试
→ CardView 骨架
→ Prefab 层级
→ Full 几何
→ Compact 几何
→ 四种卡牌变体
→ TextGenerator 与 66/45 文案门
→ 状态叠加
→ Layout/Render EditMode 测试
→ 两分辨率截图
→ 全量回归
```

不要先做金色特效或动画。先让普通 Full/Compact、66/45 文案和四种变体通过，再叠加状态。

## 7. 停止条件与问题处理

出现以下任一情况时停止扩展功能，先修复基线：

- Unity 导入后现有测试失败。
- Full 66 字随从描述在最小字号 11 下仍放不进 4 行。
- Prefab 必须依赖 `ShopTestController` 才能 Render。
- Full/Compact 需要运行时按分辨率计算位置才能对齐。
- 状态显隐导致其他 Rect 重排。
- Mask 裁切金色框、徽章或目标光效。
- 重复 Render 出现上一张卡的状态残留。

若 66 字文案门失败，记录实际字体、字号、生成行数、preferredWidth/Height 和当前 Rect；优先扩大描述/信息区并更新技术设计、任务单和布局测试，不修改配置文案来掩盖布局问题。

## 8. Definition of Done

只有同时满足以下条件，`PF_Card` 才可在 TODO 中标记完成：

- [x] `CardView` 不引用领域层和 Controller，重复 Render 安全。
- [x] 一个 `PF_Card` 支持 Full/Compact、随从/法术和全部已定义状态。
- [x] 第 4 节所有几何值通过自动测试。
- [x] 66 字随从和 45 字法术在 Full 实际字体下完整显示。
- [x] Compact Unicode 截断、能力标签和最大行数通过测试。
- [x] 普通、金色、成长、双护盾、临时、选中、合法目标、不可操作可正确叠加。
- [x] 新增 Layout/Render 测试及全量 EditMode 全绿。
- [x] 既有 PlayMode 14/14 全绿。
- [x] 1920×1080 和 1920×1200 截图验收通过。
- [x] Unity 重启后 Prefab 无丢引用、无 Missing Script、Console 无 Error。

完成后下一工作项是制作 `PF_ShopSlot + PF_ShopScreen`，再将 `ShopScreenStateBuilder` 接入 `ShopTestController`。
