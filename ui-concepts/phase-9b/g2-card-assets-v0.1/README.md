# Phase 9B G2 样板资源 v0.1

- 日期：2026-07-24
- 生成方式：Codex 内置 ImageGen
- 范围：3 个 Token、4 张法术、3 件遗珍、1 张缺图诊断图
- 状态：已保存母版并复制到约定的 Unity Runtime 路径；生产许可/来源确认仍待项目方确认

本目录保存 G2 样板新增的 11 张图像母版。此前已经完成的 6 张核心随从位于
`../sample-minion-illustrations-v0.1/masters/`，不在本目录重复保存。

## 资源与 Runtime 精确映射

Token 与法术沿用配置中的 `artId`，遗珍沿用 `uiIconId`；诊断图由 Catalog 的
`missingArtwork` 字段引用，Sprite 名称为 `fallback_missing_art`。下表中的路径均相对仓库根目录。

| 类型 | 内容 ID / 用途 | 名称 | Catalog 键 / 字段 | Master | Unity Runtime |
|---|---|---|---|---|---|
| Token | `token_young_spirit` | 幼灵 | `placeholder_token_young_spirit` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/token-young-spirit.png` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_young_spirit.png` |
| Token | `token_two_tailed_fox_shadow` | 双尾狐影 | `placeholder_token_two_tailed_fox_shadow` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/token-two-tailed-fox-shadow.png` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_two_tailed_fox_shadow.png` |
| Token | `token_swift_young_spirit` | 迅捷幼灵 | `placeholder_token_swift_young_spirit` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/token-swift-young-spirit.png` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_swift_young_spirit.png` |
| 法术 | `minor_tempering` | 小型锻体 | `placeholder_spell_minor_tempering` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-minor-tempering.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_minor_tempering.png` |
| 法术 | `free_refresh` | 免费刷新 | `placeholder_spell_free_refresh` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-free-refresh.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_free_refresh.png` |
| 法术 | `advanced_discovery` | 高阶发现 | `placeholder_spell_advanced_discovery` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-advanced-discovery.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_advanced_discovery.png` |
| 法术 | `prebattle_benediction` | 战前赐福 | `placeholder_spell_prebattle_benediction` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/spell-prebattle-benediction.png` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_prebattle_benediction.png` |
| 遗珍 | `crown_echo_bell` | 回魂丧钟 | `icon_relic_crown_echo_bell` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/relic-crown-echo-bell.png` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_crown_echo_bell.png` |
| 遗珍 | `crown_thousand_shields` | 千盾王冠 | `icon_relic_crown_thousand_shields` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/relic-crown-thousand-shields.png` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_crown_thousand_shields.png` |
| 遗珍 | `curio_refresh_gear` | 漏刻齿轮 | `icon_relic_curio_refresh_gear` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/relic-curio-refresh-gear.png` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_curio_refresh_gear.png` |
| 诊断 | Catalog 缺图 | 缺图诊断图 | `missingArtwork` → `fallback_missing_art` | `ui-concepts/phase-9b/g2-card-assets-v0.1/masters/diagnostic-missing-art.png` | `sc/Assets/Art/Presentation/UI/Diagnostics/fallback_missing_art.png` |

Token 与法术母版均为 1024×1536 RGB PNG；遗珍与诊断母版均为 1254×1254 RGB PNG。
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
- Unity 全量测试结果为 EditMode 278/278、PlayMode 22/22。
- 遗珍在 Run UI 的 1920×1080 与 1920×1200 截图均已人工验收，证据位于
  `../../unity-validation/pf-run-screen-v0.1/`。
- G2 总门禁仍保留两项待办：新增 11 张生成图的生产许可/来源签字，以及新增 6 随从、
  3 Token、4 法术的专门卡面视觉矩阵。

## 母版校验值

| 文件 | 尺寸 | SHA-256 |
|---|---:|---|
| `masters/token-young-spirit.png` | 1024×1536 | `78668c5538a592a17e44888dc76018795da7b6f9ddfd32d468a9711989391218` |
| `masters/token-two-tailed-fox-shadow.png` | 1024×1536 | `02e3ef4d1a88ed8ad06c88ecc835a6156f9bef65f8f142697f3b8e12e8497f3e` |
| `masters/token-swift-young-spirit.png` | 1024×1536 | `8381b50c434f38d9de79f79680b0625c62206f92044ae3f66efdb6d5eeeab1f4` |
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
- 当前记录中，项目方尚未明确确认这 11 张生成图的生产许可与最终来源验收。因此它们可以作为
  G2 工程接入和自动化样板资源，但在发布台账中不得标记为“生产许可已确认”或最终正式美术。
