# 阶段 9B 视听表现资产盘点表 v0.1

- 日期：2026-07-22
- 更新：2026-07-31（补记 3 张旧 G2 Token 未使用 v0.3.3 冻结风格；v0.3.4 候选待批准）
- 状态：G1、G2 与 v0.3.3 的 42 张非 Token 随从 / 9 张法术 Runtime 晋级已关闭；3 张 v0.3.4 Token Refresh 候选待视觉批准和 Runtime 晋级；G3 工程与本地播放链路为 `Commissioning Ready`，音频严格门禁、背景生产许可与 G3/G4 总门禁未关闭
- 对应方案：`phase-9b-presentation-vertical-slice-technical-design-v0.1.md`
- 内容基线：5.5.0 / 8B.1
- 配置真源：`sc/Assets/Resources/Configs/Json/`
- UI 真源：`sc/Assets/Prefabs/UI/`
- G0 基线：`phase-9b-g0-baseline-v0.1.md`
- 来源台账：`phase-9b-asset-source-ledger-v0.1.md`
- G1 成本/许可签字包：`phase-9b-g1-production-license-signoff-v0.1.md`
- G2 生产许可签字包：`phase-9b-g2-production-license-signoff-v0.1.md`
- G3 工程交接：`phase-9b-g3-engineering-handoff-v0.1.md`

## 1. 使用规则

本表同时承担范围控制、制作排期、运行时接线和来源审计。状态只允许使用：

| 状态 | 含义 |
| --- | --- |
| `未制作` | 只有需求或配置 ID，没有可评审资产 |
| `概念参考` | 有方向稿，但尺寸、来源、风格或运行时规格未冻结，不能接入正式资源 |
| `制作中` | 已确定负责人和规格，尚未通过美术评审 |
| `工程样板` | 可用于内部接线、截图和风格评审；生产许可或运行验收未完成 |
| `Local Synth Placeholder` | 可用于内部播放、接线、混音和自动化；不是生产资产，必须被 `ProductionStrict` 拒绝 |
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
| 非 Token 随从 | 64 | 64 张专属插画均已进入正式 Runtime Catalog 并精确命中；v0.3.3 晋级资产为 `Runtime Ready` | 美术缺口 0；`audioId` 全空 | G1/G2 样板与 v0.3.2/v0.3.3 量产资产合并后，64 / 64 配置 ArtId Exact |
| Token | 3 | 配置、语义 `artId`；3 张旧 G2 专属插画仍为现行 Runtime，精确命中及旧版专门卡面矩阵通过 | 3 张旧图未使用 v0.3.3 冻结 Style Tile；v0.3.4 新风格候选待视觉批准和 Runtime 晋级；`audioId` 全空 | Catalog Exact 只保护接线；新风格完成由独立 Token Refresh 门禁判断 |
| 法术 | 16 | 16 张专属插画均已进入正式 Runtime Catalog 并精确命中；v0.3.3 晋级资产为 `Runtime Ready` | 美术缺口 0；`audioId` 全空 | G2 样板与 v0.3.2/v0.3.3 量产资产合并后，16 / 16 配置 ArtId Exact |
| 遗珍 | 15 | 15 个 `uiIconId`；3 个样板图标均为 `Runtime Ready`，精确命中及 Run UI 双分辨率视觉检查通过 | 其余 12 个无对应 Sprite | 3 个样板图标由精确命中门禁保护 |
| 事件 | 14 | 文案与效果配置 | 无事件插画 | 9B 使用统一事件面板与类型图标；专属插画进入 9C |
| 遭遇 | 51 | 阵容、名称和三层曲线 | 无遭遇/敌方主题背景 | 9B 制作一套普通战斗背景；楼层/Boss 差异进入 9C |
| 地图 | 3 张、每层 19 节点 | 布局、连线与状态 | 无正式节点/连线/背景 Sprite | 7 类节点、5 类状态和第一层背景进入 P0 |
| 种族图标 | 4 个语义 ID | `icon_forge_soul`、`icon_wild_spirit`、`icon_starbound`、`icon_wayfarer` | 无 Sprite | 三主种族 P0，旅团回退/正式图标 P1 |

### 2.2 工程资产

| 类别 | 当前数量 | 当前状态 | 说明 |
| --- | ---: | --- | --- |
| UI Prefab | 13 + 运行时 System Menu | G3 统一皮肤与序列化接线完成 | Card、Shop、Battle、Run、MainMenu、选择/确认层均已重建；System Menu 与音频设置运行时复用 |
| 正式/预览 Scene | 11 | 可运行 | 包含 Boot、MainMenu、Shop、Battle、Run 及对应 Preview |
| 正式字体 | 1 | Runtime Ready | Noto Sans CJK SC Regular，已有 OFL 许可证 |
| 位图/Sprite | Runtime Catalog 86 条 | 83 个配置 ArtId 全部 Exact；v0.3.3 的 51 项卡牌美术为 `Runtime Ready` | 66 张晋级 PNG 位于 `Assets/Art/Presentation/Runtime/LightStorybookV033/`；51 / 51 v0.3.3 量产图与 15 张 v0.3.2 Formal 图采用冻结导入策略，Calibration 引用为 0；公共组件、遗珍和诊断图沿用 G1/G2 许可结论；3 张 Token 仍是旧 G2 Runtime，v0.3.4 候选未晋级 |
| Material/Shader/VFX | 1 Material + 程序化表现组件 | G3 工程完成 | 护盾 `M_BattleShieldAdditive`、程序化背景、容量受限 `PresentationFxPool`、Shop/Battle 结构化反馈与清理门禁均完成 |
| AudioClip/AudioMixer | 67 / 1 | 67 个 WAV 均为 `Local Synth Placeholder`；Mixer 与 Catalog 工程完成 | `SpireChessAudio.mixer` 已含 Master/Music/SFX/UI；28 Cue 可播放但没有 `ProductionApproved` Clip，`ProductionStrict` 精确报告 28 个占位错误 |
| 概念/验证图 | 既有方向图 + G3 28 张双分辨率截图 | G3 验证证据已归档 | G3 截图位于 `ui-concepts/unity-validation/g3-*-v0.1/`；截图证据不等于音频 Runtime Ready |

### 2.3 已有概念参考

| 资源 | 路径 | 可复用结论 | 不可直接复用原因 |
| --- | --- | --- | --- |
| 天穹契约者普通 | `ui-concepts/card-ui-sky-covenant-normal-v0.2.png` | 2:3 卡面、星契色彩和信息骨架 | 不是 Unity Artwork Sprite，未经过 9B 统一风格与来源验收 |
| 天穹契约者金色 | `ui-concepts/card-ui-sky-covenant-golden-style-v0.2.png` | 金色框架、角饰和流光方向 | 图中数据只作风格参考，正式数据必须来自配置 |
| 万蹄奔潮普通 | `ui-concepts/card-ui-ten-thousand-hoof-normal-v0.2.png` | 荒灵皮肤、三标签与长文案压力 | 未拆分插画、框架和状态层 |
| AI 初稿 | `archive/deprecated-temp-20260731/payload/deprecated-drafts/card-ui-generated-initial-draft-v0.1.png` | 仅用于比较插画气质；已归档 | 风格、来源台账和运行时规格未冻结 |
| 商店线框 | `ui-concepts/shop-ui-wireframe-v0.1.png` | 信息架构已冻结 | 低保真，不代表最终材质和美术 |

## 3. 12 张样板随从

### 3.1 铸魂

| 编号 | 等级 | 名称 / 配置 ID | 普通/金色身材 | 关键词与核心反馈 | 专属插画 | 当前状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| M-F01 | 1 | 铸魂盾侍 / `forge_soul_shield_squire` | 1/3；2/6 | 嘲讽、开场护盾、金色左侧授盾 | `card_minion_forge_soul_shield_squire.png` | G1 非人修正版 master 与盲测通过；G2 Runtime/Catalog/PF_Card 接入、精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | P0 |
| M-F02 | 2 | 回火修补匠 / `tempering_mender` | 2/3；4/6 | 战吼、目标框、下场护盾/永久生命分支 | `card_minion_tempering_mender.png` | 1024×1536 master 与离线缩略检查完成；G2 Runtime/Catalog 接入、精确命中、本轮最终全量回归及专门卡面双分辨率视觉矩阵通过；生产许可已确认（个人版 OpenAI Terms of Use） | P0 |
| M-F03 | 4 | 裂甲复仇者 / `cracked_armor_avenger` | 5/4；10/8 | 护盾、亡语、战后随从奖励 | `card_minion_cracked_armor_avenger.png` | 1024×1536 master 与离线缩略检查完成；G2 Runtime/Catalog 接入、精确命中、本轮最终全量回归及专门卡面双分辨率视觉矩阵通过；生产许可已确认（个人版 OpenAI Terms of Use） | P1 |
| M-F04 | 5 | 不熄炉王 / `undying_furnace_king` | 6/8；12/16 | 嘲讽、开场护盾、连续护盾转移、2/4 次上限 | `card_minion_undying_furnace_king.png` | G1 非人王者 v0.5 master 与盲测通过；G2 Runtime/Catalog/PF_Card 接入、精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | P1 |

铸魂配套反馈：熔火种族皮肤、护盾获得/破裂、失盾触发、永久成长、沉重攻击/死亡音色。

### 3.2 荒灵

| 编号 | 等级 | 名称 / 配置 ID | 普通/金色身材 | 关键词与核心反馈 | 专属插画 | 当前状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| M-W01 | 1 | 幼鹿灵 / `young_deer_spirit` | 1/1；2/2 | 亡语、幼灵召唤、满场失败补偿 | `card_minion_young_deer_spirit.png` | G1 master 与盲测通过；G2 Runtime/Catalog/PF_Card 接入、精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | P0 |
| M-W02 | 2 | 腐叶承嗣 / `rotleaf_heir` | 2/4；4/8 | 嘲讽、亡语、随机存活荒灵本场成长 | `card_minion_rotleaf_heir.png` | 1024×1536 master 与离线缩略检查完成；G2 Runtime/Catalog 接入、精确命中、本轮最终全量回归及专门卡面双分辨率视觉矩阵通过；生产许可已确认（个人版 OpenAI Terms of Use） | P0 |
| M-W03 | 4 | 狐群巢母 / `fox_den_matriarch` | 4/5；8/10 | 嵌套亡语、双尾狐影、连续幼灵召唤 | `card_minion_fox_den_matriarch.png` | 1024×1536 master 与离线缩略检查完成；G2 Runtime/Catalog 接入、精确命中、本轮最终全量回归及专门卡面双分辨率视觉矩阵通过；生产许可已确认（个人版 OpenAI Terms of Use） | P1 |
| M-W04 | 5 | 万蹄奔潮 / `ten_thousand_hoof_surge` | 7/8；14/16 | 召唤强化、立即攻击、Token 死亡永久成长、长文案 | `card_minion_ten_thousand_hoof_surge.png` | G1 master 与盲测通过；G2 Runtime/Catalog/PF_Card 接入、精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | P1 |

荒灵配套反馈：叶片/花粉种族皮肤、召唤轨迹、Token 入场、死亡连锁、立即攻击与生长浮字。

### 3.3 星契

| 编号 | 等级 | 名称 / 配置 ID | 普通/金色身材 | 关键词与核心反馈 | 专属插画 | 当前状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| M-S01 | 2 | 星盘校准师 / `astrolabe_calibrator` | 2/2；4/4 | 第一次刷新、最低攻击星契、永久攻击 | `card_minion_astrolabe_calibrator.png` | G1 master 与盲测通过；G2 Runtime/Catalog/PF_Card 接入、精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | P0 |
| M-S02 | 3 | 秘页折光师 / `secret_page_refractor` | 3/4；6/8 | 护盾、前 2 次施法、永久 +1/+1 或 +2/+2 | `card_minion_secret_page_refractor.png` | 1024×1536 master 与离线缩略检查完成；G2 Runtime/Catalog 接入、精确命中、本轮最终全量回归及专门卡面双分辨率视觉矩阵通过；生产许可已确认（个人版 OpenAI Terms of Use） | P0 |
| M-S03 | 3 | 星图掮客 / `star_map_broker` | 3/3；6/6 | 条件战吼、随从发现、金色两轮阻塞选择 | `card_minion_star_map_broker.png` | 1024×1536 master 与离线缩略检查完成；G2 Runtime/Catalog 接入、精确命中、本轮最终全量回归及专门卡面双分辨率视觉矩阵通过；生产许可已确认（个人版 OpenAI Terms of Use） | P1 |
| M-S04 | 5 | 天穹契约者 / `sky_covenant_bearer` | 4/8；8/16 | 每 4/3 次刷新、群体永久成长、进度条 | `card_minion_sky_covenant_bearer.png` | G1 master 与盲测通过；G2 Runtime/Catalog/PF_Card 接入、精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | P1 |

星契配套反馈：星盘/折光种族皮肤、刷新计数环、施法闪烁、发现展开、群体成长和轻量金色流光。

### 3.4 G1 附加旅团盲测锚点

| 名称 / 配置 ID | 用途 | 当前状态 | 是否计入 12 张 G2 样板 |
| --- | --- | --- | --- |
| 行脚医师 / `traveling_physician` | 验证实用、非对称负重不会与三主种族混淆 | G1 master/盲测通过；G2 Runtime/Catalog 精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | 否 |
| 百技学徒 / `many_arts_apprentice` | 验证拼装训练装备不会误读为铸魂 | G1 master/盲测通过；G2 Runtime/Catalog 精确命中与全量回归通过；生产许可已确认（个人版 OpenAI Terms of Use） | 否 |

这两张只扩大 G1 的区分压力，不替换三主种族 12 张核心样板，也不改变 G0
冻结的 9B 交付数量。

### 3.5 样板卡验收矩阵

每张样板卡必须至少产出以下运行时状态，不为金色复制独立插画：

| 状态 | 12 张普通 | 12 张金色 | Full | Compact | 商店 | 战斗 | 选择层 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 基础卡面 | 必须 | 必须 | 必须 | 必须 | 必须 | 必须 | 必须 |
| 成长数值 | 适用卡 | 适用卡 | 必须 | 必须 | 必须 | 必须 | 不要求播放 |
| 永久护盾 | 适用卡 | 适用卡 | 必须 | 必须 | 必须 | 必须 | 保留事实 |
| 下场护盾 | 回火等适用卡 | 适用卡 | 必须 | 必须 | 必须 | 必须 | 保留事实 |
| 进度 | 天穹等适用卡 | 适用卡 | 必须 | 必须 | 必须 | 不强制 | 保留文本 |
| 选中/合法目标/禁用 | 适用卡 | 适用卡 | 必须 | 必须 | 必须 | 准备态适用 | 必须 |

新增 6 张核心随从、3 个 Token、4 张法术的专门卡面补充矩阵已于 2026-07-25
完成：1920×1080、1920×1200 各 38 个状态、合计 76 次 `PF_Card` 渲染，
6 张最终证据图与逐卡结论见 `ui-concepts/unity-validation/g2-card-matrix-v0.4/`；
v0.1-v0.3 作为历次修复前审核基线保留。

## 4. Token、法术、遗珍与回退图

### 4.1 Token

| 编号 | 名称 / ID | 身材 | 依赖来源 | 插画 | 状态 | 优先级 |
| --- | --- | --- | --- | --- | --- | --- |
| T-01 | 幼灵 / `token_young_spirit` | 1/1 | 幼鹿灵、狐群巢母 | `card_token_token_young_spirit.png` | `Runtime Ready`；Catalog 精确命中、最终全量回归、专门卡面双分辨率视觉矩阵与项目负责人复核通过 | P0 |
| T-02 | 双尾狐影 / `token_two_tailed_fox_shadow` | 2/2 | 狐群巢母 | `card_token_token_two_tailed_fox_shadow.png` | `Runtime Ready`；横构图修订、Catalog 精确命中、最终全量回归、专门卡面双分辨率视觉矩阵与项目负责人复核通过 | P0 |
| T-03 | 迅捷幼灵 / `token_swift_young_spirit` | 2/1 | 立即攻击压力预览 | `card_token_token_swift_young_spirit.png` | `Runtime Ready`；横构图修订、Catalog 精确命中、最终全量回归、专门卡面双分辨率视觉矩阵与项目负责人复核通过 | P1 |

Token 必须与荒灵共享色彩语言，但轮廓、边框或 Token 徽章应让玩家在 Compact 模式立即识别其“战斗结束消失”身份。

### 4.2 法术

| 编号 | 等级 | 名称 / ID | 验证语义 | 插画 | 状态 | 优先级 |
| --- | ---: | --- | --- | --- | --- | --- |
| S-01 | 1 | 小型锻体 / `minor_tempering` | 单目标永久 +1/+1、合法目标框 | `card_spell_minor_tempering.png` | `Runtime Ready`；Catalog 精确命中、最终全量回归、专门卡面双分辨率视觉矩阵与项目负责人复核通过 | P0 |
| S-02 | 2 | 免费刷新 / `free_refresh` | 经济状态、刷新按钮脉冲 | `card_spell_free_refresh.png` | `Runtime Ready`；Catalog 精确命中、最终全量回归、专门卡面双分辨率视觉矩阵与项目负责人复核通过 | P0 |
| S-03 | 4 | 高阶发现 / `advanced_discovery` | 三选一、条件提高候选等级 | `card_spell_advanced_discovery.png` | `Runtime Ready`；Catalog 精确命中、最终全量回归、专门卡面双分辨率视觉矩阵与项目负责人复核通过 | P1 |
| S-04 | 4 | 战前赐福 / `prebattle_benediction` | 全体下场护盾、战后存活成长 | `card_spell_prebattle_benediction.png` | `Runtime Ready`；Catalog 精确命中、最终全量回归、专门卡面双分辨率视觉矩阵与项目负责人复核通过 | P1 |

其余 12 张法术继续保留按 Growth/Economy/Discovery 等类型回退的代码机制；按本轮优先级，4 张精美法术类型回退图暂缓制作，不能把缺失诊断图当作正式回退插画。

### 4.3 遗珍

| 编号 | 等级 | 名称 / ID | 验证语义 | 图标 | 状态 | 优先级 |
| --- | --- | --- | --- | --- | --- | --- |
| R-01 | Crown | 回魂丧钟 / `crown_echo_bell` | 额外亡语、荒灵组合 | `icon_relic_crown_echo_bell.png` | `Runtime Ready`；Catalog 精确命中、全量回归、Run UI 双分辨率视觉检查与项目负责人复核通过 | P1 |
| R-02 | Crown | 千盾王冠 / `crown_thousand_shields` | 开场护盾、铸魂组合 | `icon_relic_crown_thousand_shields.png` | `Runtime Ready`；Catalog 精确命中、全量回归、Run UI 双分辨率视觉检查与项目负责人复核通过 | P1 |
| R-03 | Curio | 漏刻齿轮 / `curio_refresh_gear` | 首次付费刷新免费、星契组合 | `icon_relic_curio_refresh_gear.png` | `Runtime Ready`；Catalog 精确命中、全量回归、Run UI 双分辨率视觉检查与项目负责人复核通过 | P1 |

### 4.4 回退资产

| 编号 | 回退类型 | 覆盖范围 | 运行时文件 | 状态 | 优先级 |
| --- | --- | --- | --- | --- | --- |
| F-01 | 铸魂剪影 | 未完成专属插画的铸魂 | `fallback_minion_forge_soul.png` | 未制作；本轮明确暂缓精美回退图，代码机制保留 | P0 |
| F-02 | 荒灵剪影 | 未完成专属插画的荒灵 | `fallback_minion_wild_spirit.png` | 未制作；本轮明确暂缓精美回退图，代码机制保留 | P0 |
| F-03 | 星契剪影 | 未完成专属插画的星契 | `fallback_minion_starbound.png` | 未制作；本轮明确暂缓精美回退图，代码机制保留 | P0 |
| F-04 | 旅团剪影 | 未完成专属插画的旅团 | `fallback_minion_wayfarer.png` | 未制作；本轮明确暂缓精美回退图，代码机制保留 | P0 |
| F-05 | 法术类型组 | 未完成专属插画的 12 张法术 | `fallback_spell_<type>.png`，4 张 | 未制作；本轮明确暂缓精美回退图，代码机制保留 | P0 |
| F-06 | 缺失诊断图 | 非法/未知 ArtId | `fallback_missing_art.png`，必须显眼且只用于诊断 | `Runtime Ready`；真实非法 ID 诊断、全量回归与项目负责人复核通过；仍禁止充当正式回退 | P0 |

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
| UI-01 | `PF_Card` | 几何、字体、状态和交互完成；`CardUiPrefabBuilder` 成功，Catalog 已覆盖 14 张随从、3 张 Token、4 张法术、3 个遗珍、1 张诊断图、4 个轻量组件及焦点裁切 | 正式框架、插画、种族皮肤、图标和材质 | 本轮最终 Unity 全量回归 EditMode 294 / 294、PlayMode 22 / 22，0 失败、0 跳过；24 个语义 ID 精确命中及 13 张目标卡的 v0.4 专门双分辨率视觉矩阵通过 | P0 |
| UI-02 | `PF_ShopSlot` | 功能完成 | 商品底座、悬停/选中/空槽状态 | G3 统一皮肤完成；双分辨率截图通过 | P0 |
| UI-03 | `PF_ShopScreen` | 功能完成、低保真配色 | 商店背景、顶部资源栏、三排区域、操作栏与反馈层 | G3 统一皮肤、结构化反馈与精确预览资源门禁完成；`g3-shop-screen-v0.1` 通过 | P0 |
| UI-04 | `PF_ChoiceOverlay` | 功能完成 | 发现/奖励标题、候选底板、遮罩、确认反馈 | G3 统一皮肤与双分辨率选择层通过 | P0 |
| UI-05 | `PF_BattleSlot` | 功能完成 | 玩家/敌方槽位、目标/攻击状态、召唤落点 | G3 统一皮肤与交互状态完成 | P0 |
| UI-06 | `PF_BattleScreen` | 功能完成 | 普通战斗背景、顶部栏、双方区域、日志与胜负层 | 十类事件、结果层、2×/跳过/重置清理和精确立牌资源完成；`g3-battle-screen-v0.1` 通过 | P0 |
| UI-07 | `PF_RunMapNode` | 功能完成 | 7 类节点图标、5 类状态、当前节点强调 | G3 7 类节点与 5 类状态完成；双分辨率通过 | P0 |
| UI-08 | `PF_RunMapEdge` | 功能完成 | 锁定/可达/完成/放弃连线 | G3 4 类连线状态完成；双分辨率通过 | P0 |
| UI-09 | `PF_RunRelicEntry` | `RunUiPrefabBuilder` 成功，3 个样板遗珍图标接线完成 | 冠冕/奇物底板、图标、进度 | `ui-concepts/unity-validation/pf-run-screen-v0.1/` 的 1920×1080 与 1920×1200 Run UI 截图人工检查通过；3 个图标均为 `Runtime Ready` | P1 |
| UI-10 | `PF_RunChoiceOption` | 功能完成 | 事件/锻造/恢复/遗珍选项底板 | G3 统一皮肤与选择层完成 | P0 |
| UI-11 | `PF_RunScreen` | 功能完成 | 第一层地图背景、顶栏、横向滚动区、结果层 | G3 程序化背景、节点/连线与状态完成；`g3-run-screen-v0.1` 通过 | P0 |
| UI-12 | `PF_MainMenuScreen` | 功能完成 | 标题背景、按钮、存档摘要和状态层 | G3 统一皮肤、程序化背景、音频入口完成；`g3-main-menu-v0.1` 通过 | P0 |
| UI-13 | `PF_ConfirmDialog` | 功能完成 | 通用弹窗框架、危险/普通确认状态 | G3 统一皮肤与双分辨率确认层通过 | P0 |
| UI-14 | Run System Menu | 运行时创建、功能完成 | 与 MainMenu 同源皮肤、音量设置入口 | G3 同源皮肤、四路音量面板与双分辨率证据通过 | P0 |

### 6.2 地图图标与状态

| 资产组 | 数量 | 内容 | 状态 | 优先级 |
| --- | ---: | --- | --- | --- |
| 节点类型 | 7 | Shop、Normal、Elite、Event、Enhance、Rest、Boss | G3 程序化主题完成 | P0 |
| 节点状态 | 5 | Locked、Reachable、Current、Resolved、Abandoned | G3 完成并由自动化覆盖 | P0 |
| 连线状态 | 4 | Locked、Reachable、Resolved、Abandoned | G3 完成并由自动化覆盖 | P0 |
| 第一层背景 | 1 | 可横向滚动的程序化底图，不烘焙节点位置 | G3 完成；无新增外部位图来源负担 | P0 |
| 第二/三层背景 | 2 | 楼层差异 | 未制作 | P2 |

地图背景不得烘焙节点、连线、文字或可达状态；这些继续由 `PF_RunMapNode`/`PF_RunMapEdge` 动态渲染。

## 7. 动画与 VFX 盘点

### 7.1 商店与通用 UI

| Cue ID | 触发 | 第一版表现 | 状态 | 优先级 |
| --- | --- | --- | --- | --- |
| `ui_button_press` | 通用按钮 | 0.08–0.12 秒压缩/回弹 | G3 工程完成 | P0 |
| `ui_modal_open_close` | 确认/选择层 | 淡入、轻缩放、焦点转移 | G3 工程完成 | P0 |
| `shop_refresh` | OnRefresh | 商品区替换与短闪光 | G3 结构化反馈完成 | P0 |
| `shop_buy` | OnBuy | 购买强调与金币差值 | G3 结构化反馈完成 | P0 |
| `shop_sell` | OnSell | 回收强调与金币返回 | G3 结构化反馈完成 | P0 |
| `shop_play` | OnPlay | 卡牌落位、战吼触发点 | G3 结构化反馈完成 | P0 |
| `shop_spell` | OnSpellUsed | 法术目标/全局强调 | G3 结构化反馈完成 | P0 |
| `shop_triple` | OnTripleFormed | 三连重点反馈 | G3 结构化反馈完成 | P0 |
| `shop_discover` | Discover Started/Resolved | 候选展开与选择确认 | G3 结构化反馈完成 | P0 |
| `shop_upgrade` | OnTavernUpgraded | 等级升级重点反馈 | G3 结构化反馈完成 | P0 |
| `card_stat_delta` | 相同 InstanceId 数值变化 | `+X/+Y` 或负值浮字 | G3 接入有限复用池 | P0 |
| `card_shield_state` | 护盾状态变化 | 徽章显现/破裂脉冲 | G3 工程完成 | P0 |

### 7.2 战斗

| Cue ID | 结构化事件 | 第一版表现 | 状态 | 优先级 |
| --- | --- | --- | --- | --- |
| `battle_start` | CombatStarted | 双方卡牌就位、战场亮起 | G3 工程完成 | P0 |
| `battle_round` | RoundStarted | 回合文本/短脉冲 | G3 工程完成 | P1 |
| `battle_attack` | AttackStarted | 攻击者突进、目标高亮、轻冲击 | G3 工程完成 | P0 |
| `battle_damage` | DamageApplied | 伤害浮字、震动、闪白 | G3 工程完成 | P0 |
| `battle_shield_gain` | ShieldGained | 蓝色盾面显现 | G3 工程完成 | P0 |
| `battle_shield_break` | ShieldLost | 裂纹、碎片、徽章消失 | G3 工程完成 | P0 |
| `battle_stats` | StatsChanged | 差值浮字、种族色脉冲 | G3 工程完成 | P0 |
| `battle_death` | UnitDied | 暗化、消散、清槽 | G3 工程完成 | P0 |
| `battle_summon` | UnitSummoned | 落点光环、缩放入场 | G3 工程完成 | P0 |
| `battle_end` | CombatEnded | 胜负层、短停顿、返回按钮 | G3 工程完成 | P0 |
| `battle_skip` | 跳过 | 清理全部临时实例并直接显示 FinalState | G3 回归通过 | P0 |

VFX Runtime Ready 还要求完成对象复用、并发上限、跳过清理和场景退出清理；只看见动画不算完成。

## 8. 音频盘点

### 8.1 Mixer 与 BGM

| 编号 | 资源 | 数量 | 运行时文件/资产 | 状态 | 优先级 |
| --- | --- | ---: | --- | --- | --- |
| A-01 | AudioMixer | 1 | `sc/Assets/Audio/Presentation/SpireChessAudio.mixer`，Master/Music/SFX/UI | 工程完成；四个公开音量参数通过自动化 | P0 |
| A-02 | 主菜单 BGM | 1 loop | 生产目标 `bgm_main_menu_v01.ogg`；当前 `Music/Placeholder/placeholder_bgm_main_menu_v01.wav` | 106.666667 秒占位已接入；正式 AI 母带/OGG、生成台账与听审待完成 | P0 |
| A-03 | 地图/商店 BGM | 1 loop | 生产目标 `bgm_run_shop_v01.ogg`；当前 `Music/Placeholder/placeholder_bgm_run_shop_v01.wav` | 128 秒占位已接入；正式 AI 母带/OGG、生成台账与听审待完成 | P0 |
| A-04 | 普通战斗 BGM | 1 loop | 生产目标 `bgm_battle_normal_v01.ogg`；当前 `Music/Placeholder/placeholder_bgm_battle_normal_v01.wav` | 96 秒占位已接入；正式 AI 母带/OGG、生成台账与听审待完成 | P0 |
| A-05 | Boss BGM | 1 loop | `bgm_battle_boss_v01.ogg` | 未制作 | P2 |
| A-06 | MusicDirector | 1 | 上下文切换、淡入淡出、跨场景不叠加 | G3 工程与自动化完成；真实循环听审待 Clip | P0 |

### 8.2 P0 音效 Cue

下表 25 个 Cue 的 64 个建议变体已全部按矩阵接入
`SFX/<Domain>/Placeholder/` WAV。正式 AI Clip 尚待生成、筛选与后期处理；当前
占位文件可联调，但状态统一为 `Placeholder`，不能通过生产门禁。完整提示词和变体
差异见 `phase-9b-g3-ai-audio-production-spec-v0.1.md`。

| 领域 | Cue | 建议变体数 | 语义 | 状态 |
| --- | --- | ---: | --- | --- |
| UI | `ui_click` | 3 | 普通点击 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| UI | `ui_confirm` | 2 | 确认/选择成功 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| UI | `ui_cancel` | 2 | 取消/关闭 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| UI | `ui_error` | 2 | 不可操作/保存失败 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_refresh` | 3 | 刷新商品 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_buy` | 3 | 成功购买 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_sell` | 3 | 出售回收 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_play` | 3 | 上场/战吼起点 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_spell` | 3 | 使用法术 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_triple` | 1 | 三连合成重点音 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_discover_open` | 1 | 发现候选展开 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_discover_pick` | 2 | 发现选择 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Shop | `shop_upgrade` | 1 | 酒馆升级 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_attack_light` | 4 | 普通攻击 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_hit` | 4 | 无护盾受伤 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_shield_gain` | 3 | 获得护盾 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_shield_break` | 3 | 护盾破裂 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_stat_up` | 3 | 属性成长 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_death` | 4 | 非 Token 死亡 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_token_death` | 3 | Token 死亡，重量更轻 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Battle | `battle_summon` | 4 | 召唤入场 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Result | `battle_victory` | 1 | 胜利 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Result | `battle_defeat` | 1 | 失败 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Run | `run_node_select` | 3 | 地图节点选择 | 定义/映射完成；正式 AI Clip 待生成/筛选 |
| Run | `run_reward` | 2 | 获得奖励/遗珍 | 定义/映射完成；正式 AI Clip 待生成/筛选 |

所有高频 Cue 必须配置并发上限和冷却；嵌套亡语压力场景下不得线性叠加到失真。

## 9. Catalog 与运行时接线

| 编号 | 数据资产/代码边界 | 当前状态 | 9B 需要 | 优先级 |
| --- | --- | --- | --- | --- |
| D-01 | `CardViewModel.ArtId` | 已完成 | 字符串语义字段，不传 Unity 对象 | P0 |
| D-02 | Minion/Spell Factory 映射 | 已完成 | 从配置复制到 ViewModel | P0 |
| D-03 | `PresentationSpriteCatalog` | v0.3.3 Runtime 晋级通过；Token Refresh 待晋级 | 正式 Catalog 86 条、83 个配置 ArtId Exact；正式 GUID 保持 `75d638606a8084146524a35a317a2cca`，51 项量产资产无 Calibration 引用；3 张 Token Exact 仅证明旧图接线，不能证明新风格完成；非法 ID 诊断机制继续保留 | P0 |
| D-04 | `PresentationTheme` | G3 统一主题完成 | 屏幕、地图、流派回退色、金色立牌 Tint、合法目标与选中状态色均已接入 | P0 |
| D-05 | `PresentationAudioCatalog` | 28 Cue / 67 Placeholder Clip 已精确挂接 | ID/Bus/循环/数值/Mixer Group 与 `Commissioning` 通过；`ProductionStrict` 因 28 Cue 均为 Placeholder，以退出码 1 按设计拒绝 | P0 |
| D-06 | AudioService/MusicDirector | G3 工程与占位播放链路完成 | 常驻唯一、设置持久化、淡入淡出、并发/冷却、跨场景上下文映射已覆盖；正式 AI 音频的音质、循环与峰值人工听审待完成 | P0 |
| D-07 | 资产来源台账 | G1/G2、Phase 9C v0.3.3 及 v0.3.4 Token 候选的源文件、工具和哈希边界已补齐 | v0.3.3 的 51 项量产资产晋级证据见签字包与验收摘要；3 张 Token 候选见 `ui-concepts/phase-9c/light-storybook-production-v0.1/token-refresh-v0.3.4/`，生产许可/视觉签字与 Runtime 导出仍待完成 | P0 |

9B 保留配置中现有 `placeholder_*` ArtId 作为稳定键，避免单纯改名改变完整配置哈希。是否在后续 schema 中把表现身份排除出玩法兼容哈希，需要单独技术决策，不在本阶段顺手修改。

## 10. 来源与许可证台账模板

权威台账见 `phase-9b-asset-source-ledger-v0.1.md`。每个外部、委托、购买或生成式资产增加一行：

| Asset ID | 类型 | 作者/负责人 | 工具/模型/素材库 | 来源链接或工程 | 许可证/商用范围 | 生成/购买日期 | 人工修改 | 导出版本 | 评审人 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 示例：`card_minion_xxx` | 插画 | 待填 | 待填 | 待填 | 待填 | YYYY-MM-DD | 待填 | v01 | 待填 | 不完整行不能 Runtime Ready |
| `card_minion_undying_furnace_king` | 随从插画 | 项目方 / Codex 导出 | 内置 GPT Image 工作流；后端具体模型版本/种子不可获得 | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/forge-undying-furnace-king.png` | 个人版 OpenAI Terms of Use；生产许可已确认 | 2026-07-24 | 非人纠正、端坐王者与威严受控编辑 | v05 | G1 盲测及许可签字通过 | G2 Runtime/Catalog 精确命中与全量回归已通过；G2 已关闭 |
| `card_frame_normal` | 公共普通框 | 用户提供 / Codex 修图 | ChatGPT（后端具体模型版本不可获得）、`extract_card_frame_alpha.py` | `ui-concepts/phase-9b/card-frames/shared-card-frame-normal-alpha-master-v0.1.png` | 不申请本轮生产许可；仅限历史工程样板 | 2026-07-22 | 棋盘格分离、真实 Alpha、边缘去白 | v01 | 已被替代 | 不纳入 28 项生产许可签字范围 |
| `card_frame_golden` | 公共金色框 | 用户提供参考 / Codex 修图 | ChatGPT（后端具体模型版本不可获得）、`create_golden_card_frame.py` | `ui-concepts/phase-9b/card-frames/shared-card-frame-golden-alpha-master-v0.1.png` | 不申请本轮生产许可；仅限历史工程样板 | 2026-07-22 | 古金材质迁移、裂纹/高光、复制普通框 Alpha | v01 | 已被替代 | 不纳入 28 项生产许可签字范围 |
| `card_frame_storybook_normal_v2` | 旅团绘本普通框 | Codex 生成/接入 | 内置 GPT Image 工作流、色键抠图工具 | `ui-concepts/phase-9b/card-frames/shared-card-frame-storybook-normal-chroma-v0.2.png` | 个人版 OpenAI Terms of Use；生产许可已确认 | 2026-07-23 | 以旧框为几何目标、v0.3 Style Tile 为风格参考；洋红色键转真实 Alpha | v02 | G1 运行时评审及许可签字通过 | 已替代旧黑银框用于 `PF_Card`；G2 Unity 全量回归通过 |
| `card_frame_storybook_golden_v2` | 旅团绘本金色框 | Codex 生成/接入 | 内置 GPT Image 工作流、色键抠图工具 | `ui-concepts/phase-9b/card-frames/shared-card-frame-storybook-golden-chroma-v0.2.png` | 个人版 OpenAI Terms of Use；生产许可已确认 | 2026-07-23 | 从普通框受控派生，保留纸面与靛蓝缝线，仅增强局部金箔 | v02 | G1 运行时评审及许可签字通过 | 与普通框共享几何；G2 Unity 全量回归通过 |

生成式资产还必须保存固定风格参考、提示词、负面提示、种子/模型版本（若工具提供）、生成后人工修改记录。素材库资产必须保留购买凭证或许可文本。

## 11. 9C 全量生产余量（2026-07-31 复算）

v0.3.3 卡牌美术 Runtime 晋级关闭后，按当前 5.5.0 内容复算：

| 类别 | 总量 | 当前专属完成 | 当前最低剩余 | 备注 |
| --- | ---: | ---: | ---: | --- |
| 非 Token 随从专属插画 | 64 | 64 | 0 | 64 / 64 配置 ArtId 已在正式 Runtime Catalog Exact |
| Token 专属插画 | 3 | 3（旧 G2 Runtime） | 3 张新风格晋级 | v0.3.4 候选已生成；视觉批准、Runtime 覆盖、导入策略和 Unity 复验待完成 |
| 法术专属插画 | 16 | 16 | 0 | 16 / 16 配置 ArtId 已在正式 Runtime Catalog Exact |
| 遗珍正式图标 | 15 | 3 | 12 | 冠冕与奇物需要统一等级语言 |
| 事件专属插画 | 14 | 1 个工程候选 | 13 + 生产许可 | 静谧林地已接线并通过 G4-V 技术复验，生产许可仍独立开放 |
| 楼层地图背景 | 3 | 1 | 2 | 节点和连线不随背景复制 |
| Boss/精英战斗主题 | 待定 | 0 | 待定 | 51 个遭遇不等于 51 张背景，先定义复用策略 |
| 专属卡牌 VFX/音效 | 待定 | 0 | 待定 | 只给高辨识度核心卡立项，禁止默认每卡一套 |

9C 的产能估算必须基于 G2 实际工时：分别记录每张插画从草图、评审、修图、导出、接入到验收的中位数，不用概念图生成速度推算正式产能。

## 12. 第一批制作顺序

1. 建立本表的负责人、来源和评审字段，冻结状态词。
2. 输出两套 Style Tile；不制作其余样板卡。
3. 三主种族锚点与两张旅团附加盲测锚点已完成；旅团不计入 12 张 G2 核心样板。
4. 按本轮优先级只制作并接入一张高可见缺失诊断图；4 张种族与 4 张法术类型精美回退图明确暂缓，已实现的回退代码机制继续保留。
5. 12 张核心样板随从、2 张旅团附加锚点、3 个旧 G2 Token、4 张法术、3 件遗珍、1 张诊断图与 4 个轻量组件已进入 Sprite Catalog；两套 Builder、EditMode 294 / 294、PlayMode 22 / 22、24 个语义 ID 精确命中、真实非法 ID 诊断及遗珍 Run UI 双分辨率检查均通过。
6. 新增 6 张随从、3 张旧 G2 Token、4 张法术的专门卡面视觉矩阵已通过；项目负责人已单独复核并确认新增 11 项生产使用许可，11 项在 G2 范围内均为 `Runtime Ready`，G2 关闭。该历史结论不表示 Token 已采用后来冻结的 v0.3.3 Style Tile；新风格更新由 v0.3.4 Token Refresh 单独管理。
7. 卡牌公共框架和状态/关键词图标已完成并沿用 G1/G2 门禁。
8. Card → Shop → Run/Map → Battle → MainMenu/弹窗换肤已完成；G3 四组双分辨率证据归档。
9. VFX、AudioMixer、28 Cue 工程契约和运行时接线已完成；3 首完整 BGM 与 25 个 P0 Cue / 64 个 SFX 变体的本地占位包已接入，文件门禁和 Unity 346 / 346 EditMode、25 / 25 PlayMode 通过；正式 AI Clip 仍待生成、筛选与后期处理。
10. v0.3.3 的 42 张非 Token 随从与 9 张法术已晋级为 `Runtime Ready`；后续完成正式 AI 音频、背景生产许可、G3 严格门禁、G4 第二机与外部试玩后，再更新全局状态为已验收。
