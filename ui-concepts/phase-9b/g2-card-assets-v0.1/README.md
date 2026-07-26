# Phase 9B G2 样板资源 v0.1

- 日期：2026-07-24
- 更新：2026-07-25（专门卡面 v0.4 双分辨率视觉矩阵及最终全量回归通过）
- 生成方式：Codex 内置 ImageGen
- 范围：3 个 Token、4 张法术、3 件遗珍、1 张缺图诊断图
- 状态：已保存母版并复制到约定的 Unity Runtime 路径；技术门禁与适用视觉矩阵均已通过，项目负责人已确认生产使用许可，11 项均为 `Runtime Ready`，G2 关闭

本目录保存 G2 样板新增的 11 张图像母版。此前已经完成的 6 张核心随从位于
`../sample-minion-illustrations-v0.1/masters/`，不在本目录重复保存。

## 资源与 Runtime 精确映射

Token 与法术沿用配置中的 `artId`，遗珍沿用 `uiIconId`；诊断图由 Catalog 的
`missingArtwork` 字段引用，Sprite 名称为 `fallback_missing_art`。下表中的路径均相对仓库根目录。

| 类型 | 内容 ID / 用途 | 名称 | Catalog 键 / 字段 | Master | Unity Runtime |
|---|---|---|---|---|---|
| Token | `token_young_spirit` | 幼灵 | `placeholder_token_young_spirit` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/token-young-spirit.png` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_young_spirit.png` |
| Token | `token_two_tailed_fox_shadow` | 双尾狐影 | `placeholder_token_two_tailed_fox_shadow` | `ui-concepts/phase-9b/g2-card-assets-v0.2/masters/token-two-tailed-fox-shadow-landscape.png` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_two_tailed_fox_shadow.png` |
| Token | `token_swift_young_spirit` | 迅捷幼灵 | `placeholder_token_swift_young_spirit` | `ui-concepts/phase-9b/g2-card-assets-v0.2/masters/token-swift-young-spirit-landscape.png` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_swift_young_spirit.png` |
| 法术 | `minor_tempering` | 小型锻体 | `placeholder_spell_minor_tempering` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-minor-tempering.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_minor_tempering.png` |
| 法术 | `free_refresh` | 免费刷新 | `placeholder_spell_free_refresh` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-free-refresh.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_free_refresh.png` |
| 法术 | `advanced_discovery` | 高阶发现 | `placeholder_spell_advanced_discovery` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-advanced-discovery.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_advanced_discovery.png` |
| 法术 | `prebattle_benediction` | 战前赐福 | `placeholder_spell_prebattle_benediction` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-prebattle-benediction.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_prebattle_benediction.png` |
| 遗珍 | `crown_echo_bell` | 回魂丧钟 | `icon_relic_crown_echo_bell` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/relic-crown-echo-bell.png` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_crown_echo_bell.png` |
| 遗珍 | `crown_thousand_shields` | 千盾王冠 | `icon_relic_crown_thousand_shields` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/relic-crown-thousand-shields.png` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_crown_thousand_shields.png` |
| 遗珍 | `curio_refresh_gear` | 漏刻齿轮 | `icon_relic_curio_refresh_gear` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/relic-curio-refresh-gear.png` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_curio_refresh_gear.png` |
| 诊断 | Catalog 缺图 | 缺图诊断图 | `missingArtwork` → `fallback_missing_art` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/diagnostic-missing-art.png` | `sc/Assets/Art/Presentation/UI/Diagnostics/fallback_missing_art.png` |

幼灵 Token 与法术母版为 1024×1536 RGB PNG；双尾狐影和迅捷幼灵最终修订母版分别为
1403×1121、1402×1122 横构图 RGB PNG；遗珍与诊断母版均为 1254×1254 RGB PNG。
Token、法术图只包含画面，不包含卡框、文字、数值或 UI。遗珍图标保留大面积浅色纸纹背景，
方便在当前样板 UI 中稳定缩放。诊断图有意采用高对比洋红/黑棋盘格，不能当作正式回退美术。

## 视觉参考

本批图像是新生成内容，不是对参考图的局部编辑。参考图只用于约束媒介、色板、纸张纹理、
轮廓语言和缩略图可读性：

- 共同风格：`../style-tiles/style-tile-wandering-storybook-v0.3.png`
- 已冻结的四类锚点：`../archetype-anchor-illustrations-v0.2/masters/`
- 荒灵与铸魂的补充形状参考：`../sample-minion-illustrations-v0.1/masters/`

共同方向是 Wandering Storybook：温暖手绘水彩、深胡桃色/彩色墨线、纤维纸纹、宽阔可读的
一级形状、克制细节。避免写实照片、光滑数字卡牌渲染、动漫/抽卡风、可读文字、品牌标记、
水印与签名。完整生成提示词和两次受控编辑说明见 `PROMPTS.md`。

## 技术验证

- `CardUiPrefabBuilder` 与 `RunUiPrefabBuilder` 均已成功重建目标 Prefab。
- 精确命中门禁直接调用 `TryGetArtwork`，覆盖 22 个样板 ID 与 2 个旅团锚点；测试不允许通过
  种族/法术回退或诊断图蒙混过关。
- 当前配置中的随从 `artId`、法术 `artId` 与遗珍 `uiIconId` 均会逐项校验为精确命中；
  真实非法 ID 则必须返回 `fallback_missing_art` 诊断图。
- 本轮最终 Unity 全量测试结果为 EditMode 294/294、PlayMode 22/22，0 失败、0 跳过。
- 遗珍在 Run UI 的 1920×1080 与 1920×1200 截图均已人工验收，证据位于
  `../../unity-validation/pf-run-screen-v0.1/`。
- 新增 6 随从、3 Token、4 法术已完成 1920×1080 / 1920×1200 专门卡面视觉矩阵：
  每分辨率 38 个状态、合计 76 次 `PF_Card` 渲染，6 张截图与逐卡验收结论位于
  `../../unity-validation/g2-card-matrix-v0.4/`；v0.1-v0.3 作为历次修复前审核基线保留。
- 项目负责人已于 2026-07-25 人工复核并确认本目录 11 项的生产使用许可；既有技术
  门禁通过后全部标记为 `Runtime Ready`，G2 关闭。签字边界见
  `../../../phase-9b-g2-production-license-signoff-v0.1.md`。

## 母版校验值

| 文件 | 尺寸 | SHA-256 |
|---|---:|---|
| `masters/token-young-spirit.png` | 1024×1536 | `78668c5538a592a17e44888dc76018795da7b6f9ddfd32d468a9711989391218` |
| `../g2-card-assets-v0.2/masters/token-two-tailed-fox-shadow-landscape.png` | 1403×1121 | `6fa213e471af861a23cbf21c95fe2a566f9b4bebe08f90819a46932974cb1010` |
| `../g2-card-assets-v0.2/masters/token-swift-young-spirit-landscape.png` | 1402×1122 | `261cd67e14a819b849a7d0fef665ba8c92b29129702b2b6b5c31917dc26ced54` |
| `masters/spell-minor-tempering.png` | 1024×1536 | `9d56dc56f0b3d07e2d3ad89c46c29b15bc670efb6404e69c754a19c292aec667` |
| `masters/spell-free-refresh.png` | 1024×1536 | `e2e9d614f5fd12c8adb9efdbf35d5a4a9e18eee039e79629c44f7f71813dfe08` |
| `masters/spell-advanced-discovery.png` | 1024×1536 | `7c6e9a7dc7844fe8b8cafc03b00e67a6fbdc0a14c52ea9518debd0f79bbc936b` |
| `masters/spell-prebattle-benediction.png` | 1024×1536 | `2cfda1b7bde54707cd1f0898db7c08b3c688ed59e9495e0017cdc46c1bd26db6` |
| `masters/relic-crown-echo-bell.png` | 1254×1254 | `89d4836c0b66cb1ec710e79f9eb83415946cfdf3e55935d55c427726b3163569` |
| `masters/relic-crown-thousand-shields.png` | 1254×1254 | `945b60e1382858f3c6353376c9e19924633eb9cc292f00bc5ff0d1b650bc35b3` |
| `masters/relic-curio-refresh-gear.png` | 1254×1254 | `f3ab34827094b5b8940f6958bee09db04dd4439e35a309db26e6b09f9e9538e7` |
| `masters/diagnostic-missing-art.png` | 1254×1254 | `16a246b1220b0a1188bbf61fd98c507ac60e4a5ac3e209c0f959b2618e45d039` |

## 生成、来源与许可状态

- 所有 11 张母版均使用 Codex 内置 ImageGen 生成；未调用本地生成 CLI，也未引入外部图库素材。
- `token_two_tailed_fox_shadow` 与 `token_swift_young_spirit` 在首稿基础上使用内置 ImageGen
  做过受控编辑；其余文件采用选定的新图生成结果。
- 项目内可复现记录以本目录母版、上表校验值和 `PROMPTS.md` 为准。
- 项目负责人已于 2026-07-25 人工复核这 11 项最终 Runtime 资源，同意纳入项目生产
  使用；技术门禁已经通过，故全部标记为 `Runtime Ready`。历史竖构图首稿继续仅作
  迭代证据，不在签字范围。
