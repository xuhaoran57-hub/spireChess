# 阶段 9B 视听表现资产盘点表 v0.1

- 日期：2026-07-22
- 状态：初始盘点完成，待 Style Tile 评审后进入制作
- 对应方案：`phase-9b-presentation-vertical-slice-technical-design-v0.1.md`
- 内容基线：5.5.0 / 8B.1
- 配置真源：`sc/Assets/Resources/Configs/Json/`
- UI 真源：`sc/Assets/Prefabs/UI/`

## 1. 使用规则

本表同时承担范围控制、制作排期、运行时接线和来源审计。状态只允许使用：

| 状态 | 含义 |
| --- | --- |
| `未制作` | 只有需求或配置 ID，没有可评审资产 |
| `概念参考` | 有方向稿，但尺寸、来源、风格或运行时规格未冻结，不能接入正式资源 |
| `制作中` | 已确定负责人和规格，尚未通过美术评审 |
| `待接入` | 导出文件已通过美术评审，尚未进入 Catalog/Prefab |
| `Runtime Ready` | 已接入 Unity，引用、导入设置、来源和许可证完整 |
| `已验收` | Runtime Ready 且通过自动化、双分辨率、性能和人工验收 |
| `回退资产` | 9B 有意使用的种族/类型剪影，不是缺失引用；9C 必须继续追踪专属资产 |

优先级：

- `P0`：没有该资产就不能完成 9B 指定流程。
- `P1`：用于质量压力、预览或外部试玩，允许晚于第一条可玩链路接入。
- `P2`：9C 全量生产范围，9B 只计数和保留接口。

每次状态变化必须同时填写运行时路径、来源/许可和评审结论。仅把 PNG/WAV 放进目录不等于 Runtime Ready。

## 2. 当前仓库基线

### 2.1 内容资产

| 类别 | 当前数量 | 当前可用资源 | 缺口 | 9B 处理 |
| --- | ---: | --- | --- | --- |
| 非 Token 随从 | 64 | 配置、名称、文案、数值和语义 `artId` | 64 个 `artId` 全为 `placeholder_*`；无正式 Sprite；`audioId` 全空 | 12 张专属样板插画，其余使用种族回退图 |
| Token | 3 | 配置和语义 `artId` | 3 张均无正式 Sprite；`audioId` 全空 | 3 张全部制作 |
| 法术 | 16 | 配置、法术类型和语义 `artId/iconId` | 16 张均为占位插画；`audioId` 全空 | 4 张专属样板插画，其余使用法术类型回退图 |
| 遗珍 | 15 | 15 个 `uiIconId` | 项目内没有对应 Sprite | 制作 3 个样板图标 |
| 事件 | 14 | 文案与效果配置 | 无事件插画 | 9B 使用统一事件面板与类型图标；专属插画进入 9C |
| 遭遇 | 51 | 阵容、名称和三层曲线 | 无遭遇/敌方主题背景 | 9B 制作一套普通战斗背景；楼层/Boss 差异进入 9C |
| 地图 | 3 张、每层 19 节点 | 布局、连线与状态 | 无正式节点/连线/背景 Sprite | 7 类节点、5 类状态和第一层背景进入 P0 |
| 种族图标 | 4 个语义 ID | `icon_forge_soul`、`icon_wild_spirit`、`icon_starbound`、`icon_wayfarer` | 无 Sprite | 三主种族 P0，旅团回退/正式图标 P1 |

### 2.2 工程资产

| 类别 | 当前数量 | 当前状态 | 说明 |
| --- | ---: | --- | --- |
| UI Prefab | 13 | 功能结构完成、视觉占位 | Card、Shop、Battle、Run、MainMenu 与选择层均已有序列化接线 |
| 正式/预览 Scene | 11 | 可运行 | 包含 Boot、MainMenu、Shop、Battle、Run 及对应 Preview |
| 正式字体 | 1 | Runtime Ready | Noto Sans CJK SC Regular，已有 OFL 许可证 |
| 位图/Sprite | 0 | 未制作 | `sc/Assets` 中没有 PNG/JPG 运行时资源 |
| Material/Shader/VFX | 0 | 未制作 | 当前表现主要由 UGUI 纯色和协程完成 |
| AudioClip/AudioMixer | 0 | 未制作 | 没有 WAV/OGG/MP3/Mixer 资源 |
| 概念图 | 7 张卡牌/商店方向图 + 验收截图 | 概念参考 | 位于 `ui-concepts/`，不能直接等同于 Runtime Ready |

### 2.3 已有概念参考

| 资源 | 路径 | 可复用结论 | 不可直接复用原因 |
| --- | --- | --- | --- |
| 天穹契约者普通 | `ui-concepts/card-ui-sky-covenant-normal-v0.2.png` | 2:3 卡面、星契色彩和信息骨架 | 不是 Unity Artwork Sprite，未经过 9B 统一风格与来源验收 |
| 天穹契约者金色 | `ui-concepts/card-ui-sky-covenant-golden-style-v0.2.png` | 金色框架、角饰和流光方向 | 图中数据只作风格参考，正式数据必须来自配置 |
| 万蹄奔潮普通 | `ui-concepts/card-ui-ten-thousand-hoof-normal-v0.2.png` | 荒灵皮肤、三标签与长文案压力 | 未拆分插画、框架和状态层 |
| AI 初稿 | `ui-concepts/card-ui-generated-initial-draft-v0.1.png` | 仅用于比较插画气质 | 风格、来源台账和运行时规格未冻结 |
| 商店线框 | `ui-concepts/shop-ui-wireframe-v0.1.png` | 信息架构已冻结 | 低保真，不代表最终材质和美术 |

## 3. 12 张样板随从

### 3.1 铸魂

| 编号 | 等级 | 名称 / 配置 ID | 普通/金色身材 | 关键词与核心反馈 | 专属插画 | 当前状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| M-F01 | 1 | 铸魂盾侍 / `forge_soul_shield_squire` | 1/3；2/6 | 嘲讽、开场护盾、金色左侧授盾 | `card_minion_forge_soul_shield_squire.png` | 未制作 | P0 |
| M-F02 | 2 | 回火修补匠 / `tempering_mender` | 2/3；4/6 | 战吼、目标框、下场护盾/永久生命分支 | `card_minion_tempering_mender.png` | 未制作 | P0 |
| M-F03 | 4 | 裂甲复仇者 / `cracked_armor_avenger` | 5/4；10/8 | 护盾、亡语、战后随从奖励 | `card_minion_cracked_armor_avenger.png` | 未制作 | P1 |
| M-F04 | 5 | 不熄炉王 / `undying_furnace_king` | 6/8；12/16 | 嘲讽、开场护盾、连续护盾转移、2/4 次上限 | `card_minion_undying_furnace_king.png` | v0.4 合成待最终评审；Sprite Catalog/Prefab 已接线；Unity 运行验证待执行；来源台账待补 | P1 |

铸魂配套反馈：熔火种族皮肤、护盾获得/破裂、失盾触发、永久成长、沉重攻击/死亡音色。

### 3.2 荒灵

| 编号 | 等级 | 名称 / 配置 ID | 普通/金色身材 | 关键词与核心反馈 | 专属插画 | 当前状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| M-W01 | 1 | 幼鹿灵 / `young_deer_spirit` | 1/1；2/2 | 亡语、幼灵召唤、满场失败补偿 | `card_minion_young_deer_spirit.png` | 未制作 | P0 |
| M-W02 | 2 | 腐叶承嗣 / `rotleaf_heir` | 2/4；4/8 | 嘲讽、亡语、随机存活荒灵本场成长 | `card_minion_rotleaf_heir.png` | 未制作 | P0 |
| M-W03 | 4 | 狐群巢母 / `fox_den_matriarch` | 4/5；8/10 | 嵌套亡语、双尾狐影、连续幼灵召唤 | `card_minion_fox_den_matriarch.png` | 未制作 | P1 |
| M-W04 | 5 | 万蹄奔潮 / `ten_thousand_hoof_surge` | 7/8；14/16 | 召唤强化、立即攻击、Token 死亡永久成长、长文案 | `card_minion_ten_thousand_hoof_surge.png` | 概念参考 | P1 |

荒灵配套反馈：叶片/花粉种族皮肤、召唤轨迹、Token 入场、死亡连锁、立即攻击与生长浮字。

### 3.3 星契

| 编号 | 等级 | 名称 / 配置 ID | 普通/金色身材 | 关键词与核心反馈 | 专属插画 | 当前状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| M-S01 | 2 | 星盘校准师 / `astrolabe_calibrator` | 2/2；4/4 | 第一次刷新、最低攻击星契、永久攻击 | `card_minion_astrolabe_calibrator.png` | 未制作 | P0 |
| M-S02 | 3 | 秘页折光师 / `secret_page_refractor` | 3/4；6/8 | 护盾、前 2 次施法、永久 +1/+1 或 +2/+2 | `card_minion_secret_page_refractor.png` | 未制作 | P0 |
| M-S03 | 3 | 星图掮客 / `star_map_broker` | 3/3；6/6 | 条件战吼、随从发现、金色两轮阻塞选择 | `card_minion_star_map_broker.png` | 未制作 | P1 |
| M-S04 | 5 | 天穹契约者 / `sky_covenant_bearer` | 4/8；8/16 | 每 4/3 次刷新、群体永久成长、进度条 | `card_minion_sky_covenant_bearer.png` | 概念参考 | P1 |

星契配套反馈：星盘/折光种族皮肤、刷新计数环、施法闪烁、发现展开、群体成长和轻量金色流光。

### 3.4 样板卡验收矩阵

每张样板卡必须至少产出以下运行时状态，不为金色复制独立插画：

| 状态 | 12 张普通 | 12 张金色 | Full | Compact | 商店 | 战斗 | 选择层 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 基础卡面 | 必须 | 必须 | 必须 | 必须 | 必须 | 必须 | 必须 |
| 成长数值 | 适用卡 | 适用卡 | 必须 | 必须 | 必须 | 必须 | 不要求播放 |
| 永久护盾 | 适用卡 | 适用卡 | 必须 | 必须 | 必须 | 必须 | 保留事实 |
| 下场护盾 | 回火等适用卡 | 适用卡 | 必须 | 必须 | 必须 | 必须 | 保留事实 |
| 进度 | 天穹等适用卡 | 适用卡 | 必须 | 必须 | 必须 | 不强制 | 保留文本 |
| 选中/合法目标/禁用 | 适用卡 | 适用卡 | 必须 | 必须 | 必须 | 准备态适用 | 必须 |

## 4. Token、法术、遗珍与回退图

### 4.1 Token

| 编号 | 名称 / ID | 身材 | 依赖来源 | 插画 | 状态 | 优先级 |
| --- | --- | --- | --- | --- | --- | --- |
| T-01 | 幼灵 / `token_young_spirit` | 1/1 | 幼鹿灵、狐群巢母 | `card_token_token_young_spirit.png` | 未制作 | P0 |
| T-02 | 双尾狐影 / `token_two_tailed_fox_shadow` | 2/2 | 狐群巢母 | `card_token_token_two_tailed_fox_shadow.png` | 未制作 | P0 |
| T-03 | 迅捷幼灵 / `token_swift_young_spirit` | 2/1 | 立即攻击压力预览 | `card_token_token_swift_young_spirit.png` | 未制作 | P1 |

Token 必须与荒灵共享色彩语言，但轮廓、边框或 Token 徽章应让玩家在 Compact 模式立即识别其“战斗结束消失”身份。

### 4.2 法术

| 编号 | 等级 | 名称 / ID | 验证语义 | 插画 | 状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- |
| S-01 | 1 | 小型锻体 / `minor_tempering` | 单目标永久 +1/+1、合法目标框 | `card_spell_minor_tempering.png` | 未制作 | P0 |
| S-02 | 2 | 免费刷新 / `free_refresh` | 经济状态、刷新按钮脉冲 | `card_spell_free_refresh.png` | 未制作 | P0 |
| S-03 | 4 | 高阶发现 / `advanced_discovery` | 三选一、条件提高候选等级 | `card_spell_advanced_discovery.png` | 未制作 | P1 |
| S-04 | 4 | 战前赐福 / `prebattle_benediction` | 全体下场护盾、战后存活成长 | `card_spell_prebattle_benediction.png` | 未制作 | P1 |

其余 12 张法术在 9B 使用按 Growth/Economy/Discovery 等法术类型区分的正式回退插画，不能使用纯色空块。

### 4.3 遗珍

| 编号 | 等级 | 名称 / ID | 验证语义 | 图标 | 状态 | 优先级 |
| --- | --- | --- | --- | --- | --- | --- |
| R-01 | Crown | 回魂丧钟 / `crown_echo_bell` | 额外亡语、荒灵组合 | `icon_relic_crown_echo_bell.png` | 未制作 | P1 |
| R-02 | Crown | 千盾王冠 / `crown_thousand_shields` | 开场护盾、铸魂组合 | `icon_relic_crown_thousand_shields.png` | 未制作 | P1 |
| R-03 | Curio | 漏刻齿轮 / `curio_refresh_gear` | 首次付费刷新免费、星契组合 | `icon_relic_curio_refresh_gear.png` | 未制作 | P1 |

### 4.4 回退资产

| 编号 | 回退类型 | 覆盖范围 | 运行时文件 | 状态 | 优先级 |
| --- | --- | --- | --- | --- | --- |
| F-01 | 铸魂剪影 | 未完成专属插画的铸魂 | `fallback_minion_forge_soul.png` | 未制作 | P0 |
| F-02 | 荒灵剪影 | 未完成专属插画的荒灵 | `fallback_minion_wild_spirit.png` | 未制作 | P0 |
| F-03 | 星契剪影 | 未完成专属插画的星契 | `fallback_minion_starbound.png` | 未制作 | P0 |
| F-04 | 旅团剪影 | 未完成专属插画的旅团 | `fallback_minion_wayfarer.png` | 未制作 | P0 |
| F-05 | 法术类型组 | 未完成专属插画的 12 张法术 | `fallback_spell_<type>.png`，预计 3–5 张 | 未制作 | P0 |
| F-06 | 缺失诊断图 | 非法/未知 ArtId | `fallback_missing_art.png`，必须显眼且只用于诊断 | 未制作 | P0 |

`回退资产` 是 9B 有意控制范围的正式中间方案；`fallback_missing_art` 则表示接线错误，两者不能混用。

## 5. 卡牌公共视觉资产

| 编号 | 资产组 | 数量基线 | 内容 | 状态 | 优先级 |
| --- | --- | ---: | --- | --- | --- |
| C-01 | 普通公共框架 | 1 套 | 银黑外框、名称牌、信息区、攻防徽章 | 静态框样板完成；动态徽章仍沿用现有 UI | P0 |
| C-02 | 金色公共框架 | 1 套 | 金色外框、角饰、流光遮罩/材质 | 静态框样板完成；流光材质未制作 | P0 |
| C-03 | 种族皮肤 | 4 套 | 铸魂、荒灵、星契、旅团；不改变卡牌几何 | 未制作 | P0 |
| C-04 | 法术卡变体 | 1 套 | 隐藏攻防、法术页脚、法术类型皮肤 | 未制作 | P0 |
| C-05 | 等级强调 | 5 套 | T1–T5 灰/绿/蓝/紫/橙，作为强调而非整卡染色 | 纯色原型 | P0 |
| C-06 | 状态徽章 | 3 枚 | 永久护盾、下场护盾、临时 | 文字原型 | P0 |
| C-07 | 关键词图标 | 首批 6 枚 | 嘲讽、战吼、亡语、护盾、溅射、成长/进度 | 未制作 | P0 |
| C-08 | 交互层 | 4 套 | 选中、合法目标、禁用、不可支付 | 纯色原型 | P0 |
| C-09 | 数值变化反馈 | 1 套 | 正增长、负增长、攻击/生命差值浮字 | 文字原型 | P0 |
| C-10 | Token 身份 | 1 套 | Token 徽章/边框与战后消失提示 | 未制作 | P0 |

## 6. 正式界面资产

### 6.1 Prefab 换肤清单

| 编号 | Prefab/界面 | 现状 | 9B 交付 | 状态 | 优先级 |
| --- | --- | --- | --- | --- | --- |
| UI-01 | `PF_Card` | 几何、字体、状态和交互完成；纯色 Artwork | 正式框架、插画、种族皮肤、图标和材质 | ArtId/框切换已接入；v0.3 视觉布局待同步；其余进行中 | P0 |
| UI-02 | `PF_ShopSlot` | 功能完成 | 商品底座、悬停/选中/空槽状态 | 未制作 | P0 |
| UI-03 | `PF_ShopScreen` | 功能完成、低保真配色 | 商店背景、顶部资源栏、三排区域、操作栏与反馈层 | 未制作 | P0 |
| UI-04 | `PF_ChoiceOverlay` | 功能完成 | 发现/奖励标题、候选底板、遮罩、确认反馈 | 未制作 | P0 |
| UI-05 | `PF_BattleSlot` | 功能完成 | 玩家/敌方槽位、目标/攻击状态、召唤落点 | 未制作 | P0 |
| UI-06 | `PF_BattleScreen` | 功能完成 | 普通战斗背景、顶部栏、双方区域、日志与胜负层 | 未制作 | P0 |
| UI-07 | `PF_RunMapNode` | 功能完成 | 7 类节点图标、5 类状态、当前节点强调 | 未制作 | P0 |
| UI-08 | `PF_RunMapEdge` | 功能完成 | 锁定/可达/完成/放弃连线 | 未制作 | P0 |
| UI-09 | `PF_RunRelicEntry` | 功能完成 | 冠冕/奇物底板、图标、进度 | 未制作 | P1 |
| UI-10 | `PF_RunChoiceOption` | 功能完成 | 事件/锻造/恢复/遗珍选项底板 | 未制作 | P0 |
| UI-11 | `PF_RunScreen` | 功能完成 | 第一层地图背景、顶栏、横向滚动区、结果层 | 未制作 | P0 |
| UI-12 | `PF_MainMenuScreen` | 功能完成 | 标题背景、按钮、存档摘要和状态层 | 未制作 | P0 |
| UI-13 | `PF_ConfirmDialog` | 功能完成 | 通用弹窗框架、危险/普通确认状态 | 未制作 | P0 |
| UI-14 | Run System Menu | 运行时创建、功能完成 | 与 MainMenu 同源皮肤、音量设置入口 | 未制作 | P0 |

### 6.2 地图图标与状态

| 资产组 | 数量 | 内容 | 状态 | 优先级 |
| --- | ---: | --- | --- | --- |
| 节点类型 | 7 | Shop、Normal、Elite、Event、Enhance、Rest、Boss | 未制作 | P0 |
| 节点状态 | 5 | Locked、Reachable、Current、Completed、Abandoned | 未制作 | P0 |
| 连线状态 | 4 | Locked、Reachable、Completed、Abandoned | 未制作 | P0 |
| 第一层背景 | 1 | 可横向滚动的地图底图/纹理，不烘焙节点位置 | 未制作 | P0 |
| 第二/三层背景 | 2 | 楼层差异 | 未制作 | P2 |

地图背景不得烘焙节点、连线、文字或可达状态；这些继续由 `PF_RunMapNode`/`PF_RunMapEdge` 动态渲染。

## 7. 动画与 VFX 盘点

### 7.1 商店与通用 UI

| Cue ID | 触发 | 第一版表现 | 状态 | 优先级 |
| --- | --- | --- | --- | --- |
| `ui_button_press` | 通用按钮 | 0.08–0.12 秒压缩/回弹 | 未制作 | P0 |
| `ui_modal_open_close` | 确认/选择层 | 淡入、轻缩放、焦点转移 | 未制作 | P0 |
| `shop_refresh` | OnRefresh | 商品区横向/纵向替换与短闪光 | 未制作 | P0 |
| `shop_buy` | OnBuy | 商品飞向手牌、金币差值 | 未制作 | P0 |
| `shop_sell` | OnSell | 卡牌消散、金币回收 | 未制作 | P0 |
| `shop_play` | OnPlay | 卡牌落位、战吼触发点 | 未制作 | P0 |
| `shop_spell` | OnSpellUsed | 法术飞向目标/全局扩散 | 未制作 | P0 |
| `shop_triple` | OnTripleFormed | 三卡汇聚、金色框显现 | 未制作 | P0 |
| `shop_discover` | Discover Started/Resolved | 候选展开、选中吸收、其余退场 | 未制作 | P0 |
| `shop_upgrade` | OnTavernUpgraded | 等级徽章升级、短粒子 | 未制作 | P0 |
| `card_stat_delta` | 相同 InstanceId 数值变化 | `+X/+Y` 或负值浮字 | 原型已有 | P0 |
| `card_shield_state` | 护盾状态变化 | 徽章显现/破裂脉冲 | 未制作 | P0 |

### 7.2 战斗

| Cue ID | 结构化事件 | 第一版表现 | 状态 | 优先级 |
| --- | --- | --- | --- | --- |
| `battle_start` | CombatStarted | 双方卡牌就位、战场亮起 | 未制作 | P0 |
| `battle_round` | RoundStarted | 回合文本/短脉冲 | 未制作 | P1 |
| `battle_attack` | AttackStarted | 攻击者突进、目标高亮、轻冲击 | 协程原型 | P0 |
| `battle_damage` | DamageApplied | 伤害浮字、震动、闪白 | 协程原型 | P0 |
| `battle_shield_gain` | ShieldGained | 蓝色盾面显现 | 文字原型 | P0 |
| `battle_shield_break` | ShieldLost | 裂纹、碎片、徽章消失 | 文字原型 | P0 |
| `battle_stats` | StatsChanged | 差值浮字、种族色脉冲 | 文字原型 | P0 |
| `battle_death` | UnitDied | 暗化、破碎/消散、清槽 | 淡出原型 | P0 |
| `battle_summon` | UnitSummoned | 落点光环、缩放入场 | 缩放原型 | P0 |
| `battle_end` | CombatEnded | 胜负层、短停顿、返回按钮 | 文字原型 | P0 |
| `battle_skip` | 跳过 | 清理全部临时实例并直接显示 FinalState | 功能已有 | P0 |

VFX Runtime Ready 还要求完成对象复用、并发上限、跳过清理和场景退出清理；只看见动画不算完成。

## 8. 音频盘点

### 8.1 Mixer 与 BGM

| 编号 | 资源 | 数量 | 运行时文件/资产 | 状态 | 优先级 |
| --- | --- | ---: | --- | --- | --- |
| A-01 | AudioMixer | 1 | `SpireChessAudio.mixer`，Master/Music/SFX/UI | 未制作 | P0 |
| A-02 | 主菜单 BGM | 1 loop | `bgm_main_menu_v01.ogg` | 未制作 | P0 |
| A-03 | 地图/商店 BGM | 1 loop | `bgm_run_shop_v01.ogg` | 未制作 | P0 |
| A-04 | 普通战斗 BGM | 1 loop | `bgm_battle_normal_v01.ogg` | 未制作 | P0 |
| A-05 | Boss BGM | 1 loop | `bgm_battle_boss_v01.ogg` | 未制作 | P2 |
| A-06 | MusicDirector | 1 | 上下文切换、淡入淡出、跨场景不叠加 | 未制作 | P0 |

### 8.2 P0 音效 Cue

| 领域 | Cue | 建议变体数 | 语义 | 状态 |
| --- | --- | ---: | --- | --- |
| UI | `ui_click` | 3 | 普通点击 | 未制作 |
| UI | `ui_confirm` | 2 | 确认/选择成功 | 未制作 |
| UI | `ui_cancel` | 2 | 取消/关闭 | 未制作 |
| UI | `ui_error` | 2 | 不可操作/保存失败 | 未制作 |
| Shop | `shop_refresh` | 3 | 刷新商品 | 未制作 |
| Shop | `shop_buy` | 3 | 成功购买 | 未制作 |
| Shop | `shop_sell` | 3 | 出售回收 | 未制作 |
| Shop | `shop_play` | 3 | 上场/战吼起点 | 未制作 |
| Shop | `shop_spell` | 3 | 使用法术 | 未制作 |
| Shop | `shop_triple` | 1 | 三连合成重点音 | 未制作 |
| Shop | `shop_discover_open` | 1 | 发现候选展开 | 未制作 |
| Shop | `shop_discover_pick` | 2 | 发现选择 | 未制作 |
| Shop | `shop_upgrade` | 1 | 酒馆升级 | 未制作 |
| Battle | `battle_attack_light` | 4 | 普通攻击 | 未制作 |
| Battle | `battle_hit` | 4 | 无护盾受伤 | 未制作 |
| Battle | `battle_shield_gain` | 3 | 获得护盾 | 未制作 |
| Battle | `battle_shield_break` | 3 | 护盾破裂 | 未制作 |
| Battle | `battle_stat_up` | 3 | 属性成长 | 未制作 |
| Battle | `battle_death` | 4 | 非 Token 死亡 | 未制作 |
| Battle | `battle_token_death` | 3 | Token 死亡，重量更轻 | 未制作 |
| Battle | `battle_summon` | 4 | 召唤入场 | 未制作 |
| Result | `battle_victory` | 1 | 胜利 | 未制作 |
| Result | `battle_defeat` | 1 | 失败 | 未制作 |
| Run | `run_node_select` | 3 | 地图节点选择 | 未制作 |
| Run | `run_reward` | 2 | 获得奖励/遗珍 | 未制作 |

所有高频 Cue 必须配置并发上限和冷却；嵌套亡语压力场景下不得线性叠加到失真。

## 9. Catalog 与运行时接线

| 编号 | 数据资产/代码边界 | 当前状态 | 9B 需要 | 优先级 |
| --- | --- | --- | --- | --- |
| D-01 | `CardViewModel.ArtId` | 已完成 | 字符串语义字段，不传 Unity 对象 | P0 |
| D-02 | Minion/Spell Factory 映射 | 已完成 | 从配置复制到 ViewModel | P0 |
| D-03 | `PresentationSpriteCatalog` | 样板接入 | 已完成不熄炉王与普通/金色框映射；重复/全量缺失校验待补 | P0 |
| D-04 | `PresentationTheme` | 不存在 | 公共色板、9-slice、材质、种族皮肤和状态图标 | P0 |
| D-05 | `PresentationAudioCatalog` | 不存在 | Cue→Clips、Mixer Group、音量/音高、并发/冷却 | P0 |
| D-06 | AudioService/MusicDirector | 不存在 | 常驻、跨场景唯一、设置持久化、淡入淡出 | P0 |
| D-07 | 资产来源台账 | 不存在 | 作者/工具/许可/版本/评审记录 | P0 |

9B 保留配置中现有 `placeholder_*` ArtId 作为稳定键，避免单纯改名改变完整配置哈希。是否在后续 schema 中把表现身份排除出玩法兼容哈希，需要单独技术决策，不在本阶段顺手修改。

## 10. 来源与许可证台账模板

每个外部、委托、购买或生成式资产增加一行：

| Asset ID | 类型 | 作者/负责人 | 工具/模型/素材库 | 来源链接或工程 | 许可证/商用范围 | 生成/购买日期 | 人工修改 | 导出版本 | 评审人 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 示例：`card_minion_xxx` | 插画 | 待填 | 待填 | 待填 | 待填 | YYYY-MM-DD | 待填 | v01 | 待填 | 不完整行不能 Runtime Ready |
| `card_minion_undying_furnace_king` | 随从插画 | 用户提供 / Codex 导出 | ChatGPT（具体模型待补）、Pillow | `sc/Temp/phase9b-card-composite/undying-furnace-king.png` | 待用户确认 | 2026-07-22 | 1024×1024 缩放导出、Artwork Mask 裁切验证 | v01 | 待评审 | 工程样板可用；许可未补前不标 Runtime Ready |
| `card_frame_normal` | 公共普通框 | 用户提供 / Codex 修图 | ChatGPT（具体模型待补）、`extract_card_frame_alpha.py` | `ui-concepts/phase-9b/card-frames/shared-card-frame-normal-alpha-master-v0.1.png` | 待用户确认 | 2026-07-22 | 棋盘格分离、真实 Alpha、边缘去白 | v01 | 待评审 | 与金色框 Alpha 完全一致 |
| `card_frame_golden` | 公共金色框 | 用户提供参考 / Codex 修图 | ChatGPT（具体模型待补）、`create_golden_card_frame.py` | `ui-concepts/phase-9b/card-frames/shared-card-frame-golden-alpha-master-v0.1.png` | 待用户确认 | 2026-07-22 | 古金材质迁移、裂纹/高光、复制普通框 Alpha | v01 | 待评审 | 静态框完成；Unity 流光材质未制作 |

生成式资产还必须保存固定风格参考、提示词、负面提示、种子/模型版本（若工具提供）、生成后人工修改记录。素材库资产必须保留购买凭证或许可文本。

## 11. 9C 全量生产余量

9B 全部完成后，按当前 5.5.0 内容仍至少剩余：

| 类别 | 总量 | 9B 专属完成 | 9C 最低剩余 | 备注 |
| --- | ---: | ---: | ---: | --- |
| 非 Token 随从专属插画 | 64 | 12 | 52 | 9B 期间由四种种族回退覆盖 |
| Token 专属插画 | 3 | 3 | 0 | 新增 Token 另计 |
| 法术专属插画 | 16 | 4 | 12 | 9B 期间由法术类型回退覆盖 |
| 遗珍正式图标 | 15 | 3 | 12 | 冠冕与奇物需要统一等级语言 |
| 事件专属插画 | 14 | 0 | 14 | 是否每事件独立插画在 9C 决策 |
| 楼层地图背景 | 3 | 1 | 2 | 节点和连线不随背景复制 |
| Boss/精英战斗主题 | 待定 | 0 | 待定 | 51 个遭遇不等于 51 张背景，先定义复用策略 |
| 专属卡牌 VFX/音效 | 待定 | 0 | 待定 | 只给高辨识度核心卡立项，禁止默认每卡一套 |

9C 的产能估算必须基于 G2 实际工时：分别记录每张插画从草图、评审、修图、导出、接入到验收的中位数，不用概念图生成速度推算正式产能。

## 12. 第一批制作顺序

1. 建立本表的负责人、来源和评审字段，冻结状态词。
2. 输出两套 Style Tile；不制作其余样板卡。
3. 先完成铸魂盾侍、万蹄奔潮、天穹契约者普通/金色三个风格锚点。
4. 同时制作四种种族回退图、法术类型回退图和缺失诊断图，保证正式流程没有空白资源。
5. 完成 Sprite Catalog、Theme 与 `CardViewModel.ArtId` 接线。
6. 完成剩余 9 张样板随从和 3 个 Token。
7. 完成 4 张法术、3 件遗珍、卡牌公共框架和状态/关键词图标。
8. 按 Card → Shop → Run/Map → Battle → MainMenu/弹窗顺序换肤。
9. 接入 VFX、AudioMixer、3 套 BGM 和 P0 Cue。
10. 完成自动化、双分辨率、真实性能和外部试玩后，把状态更新为已验收，并据实计算 9C 余量。
