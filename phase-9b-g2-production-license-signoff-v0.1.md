# 阶段 9B G2 新增资源生产许可签字包 v0.1

- 日期：2026-07-25
- 状态：项目负责人已人工复核并确认；11 项新增资源生产许可门禁通过
- 范围：3 个 Token、4 张法术、3 件遗珍和 1 张缺图诊断图
- 关联技术门禁：EditMode 294 / 294、PlayMode 22 / 22；24 个语义 ID 精确命中、非法 ID 诊断、遗珍与专门卡面双分辨率视觉检查均通过
- 关联台账：`phase-9b-asset-source-ledger-v0.1.md`
- 说明：本文是项目内部治理与证据记录，不构成法律意见；正式发行前如存在第三方权利疑问，应交由具备资质的法律专业人士复核。

## 1. 确认边界

本确认仅覆盖第 2 节列出的 11 项最终 Runtime 文件及对应 SHA-256，不自动扩展到
历史版本、被替代的首稿或未来新增资产。本批资产使用的风格与图像输入来自 G1 已完成
权利确认的项目参考；适用个人版 OpenAI 服务、输入参考权利、输出可能不唯一、必要
披露及人工适用性复核等治理条件沿用
`phase-9b-g1-production-license-signoff-v0.1.md` 第 4.1 节。

本次确认不把缺图诊断图变为正式卡牌回退美术。`fallback_missing_art` 只允许用于
非法或未知语义 ID 的显式诊断，不得替代精确命中门禁或未制作的种族/法术类型回退图。

## 2. 权威 11 项清单

| Asset ID | 最终 Runtime 路径 | SHA-256 |
| --- | --- | --- |
| `card_token_token_young_spirit` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_young_spirit.png` | `78668c5538a592a17e44888dc76018795da7b6f9ddfd32d468a9711989391218` |
| `card_token_token_two_tailed_fox_shadow` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_two_tailed_fox_shadow.png` | `6fa213e471af861a23cbf21c95fe2a566f9b4bebe08f90819a46932974cb1010` |
| `card_token_token_swift_young_spirit` | `sc/Assets/Art/Presentation/Cards/Tokens/card_token_token_swift_young_spirit.png` | `261cd67e14a819b849a7d0fef665ba8c92b29129702b2b6b5c31917dc26ced54` |
| `card_spell_minor_tempering` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_minor_tempering.png` | `9d56dc56f0b3d07e2d3ad89c46c29b15bc670efb6404e69c754a19c292aec667` |
| `card_spell_free_refresh` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_free_refresh.png` | `e2e9d614f5fd12c8adb9efdbf35d5a4a9e18eee039e79629c44f7f71813dfe08` |
| `card_spell_advanced_discovery` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_advanced_discovery.png` | `7c6e9a7dc7844fe8b8cafc03b00e67a6fbdc0a14c52ea9518debd0f79bbc936b` |
| `card_spell_prebattle_benediction` | `sc/Assets/Art/Presentation/Cards/Spells/card_spell_prebattle_benediction.png` | `2cfda1b7bde54707cd1f0898db7c08b3c688ed59e9495e0017cdc46c1bd26db6` |
| `icon_relic_crown_echo_bell` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_crown_echo_bell.png` | `89d4836c0b66cb1ec710e79f9eb83415946cfdf3e55935d55c427726b3163569` |
| `icon_relic_crown_thousand_shields` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_crown_thousand_shields.png` | `945b60e1382858f3c6353376c9e19924633eb9cc292f00bc5ff0d1b650bc35b3` |
| `icon_relic_curio_refresh_gear` | `sc/Assets/Art/Presentation/Icons/Relics/icon_relic_curio_refresh_gear.png` | `f3ab34827094b5b8940f6958bee09db04dd4439e35a309db26e6b09f9e9538e7` |
| `fallback_missing_art` | `sc/Assets/Art/Presentation/UI/Diagnostics/fallback_missing_art.png` | `16a246b1220b0a1188bbf61fd98c507ac60e4a5ac3e209c0f959b2618e45d039` |

## 3. 技术门禁与状态转换

截至本次确认，11 项资源均已完成：

- Unity 导入与序列化引用；
- Token、法术和遗珍样板语义 ID 的 Catalog 精确命中；
- 非法/未知 ID 的缺图诊断；
- EditMode 294 / 294、PlayMode 22 / 22 全量回归；
- 遗珍 Run UI 的 1920×1080 / 1920×1200 人工检查；
- 新增 6 随从、3 Token、4 法术的 v0.4 专门卡面双分辨率视觉矩阵。

因此，上述 11 项由 `工程样板` 直接更新为 `Runtime Ready`，G2 门禁关闭。后续
G3/G4 改动仍须执行新的回归与正式链路验收；本次状态不豁免未来变更。

## 4. 项目方签字记录

- 适用账号/协议：`个人版 OpenAI 服务 / Terms of Use`
- 签字人/项目负责人：`项目负责人（Codex 任务内确认）`
- 签字日期：`2026-07-25`
- [x] 已人工复核第 2 节 11 项最终 Runtime 资源。
- [x] 同意纳入本项目生产使用。
- [x] 同意在既有技术门禁通过后标记为 `Runtime Ready`。

项目负责人于 2026-07-25 在 Codex 任务中确认：

```text
确认 G2 的11 项资源，我已人工复核，同意纳入本项目生产使用，并在技术门禁通过后标记为 Runtime Ready。
```
