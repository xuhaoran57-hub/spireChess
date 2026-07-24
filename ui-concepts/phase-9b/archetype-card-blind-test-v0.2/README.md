# Phase 9B 三流派＋旅团卡面集成 v0.2

- 日期：2026-07-24
- 状态：立绘盲测通过后的卡面集成评审样板
- 卡框：项目现有旅团绘本普通框
- 数值组件：轻量标签 v0.2
- 输出：8 张 Full 240×360、8 张 Compact 160×240、1 组四位数压力样板
- 结论边界：不是 Runtime Ready

## 这一步验证什么

八张锚点立绘的流派区分盲测已经通过。本目录不重复进行“只凭立绘猜流派”的测试，而是把最新立绘装入实际卡面，验证：

- 同一普通卡框中，四类立绘与名称区是否仍能保持身份差异。
- 费用、等级、攻击和生命的轻量组件是否能在八张真实卡上保持统一层级。
- Full 240×360 是否能容纳真实名称、关键词和完整规则。
- Compact 160×240 是否能保留角色、数值与两行机制摘要。
- `9999/1200` 是否能在攻血标签内完整显示。

## 资产来源

| 内容 | 来源 |
| --- | --- |
| 八张冻结立绘 | `../archetype-anchor-illustrations-v0.2/masters/` |
| 普通卡框 | `../../../sc/Assets/Art/Presentation/UI/Common/card_frame_storybook_normal_v2.png` |
| 费用/等级/攻血组件 | `../card-components-number-tags-v0.2/components/` |
| 中文字体 | `../../../sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf` |
| 卡牌配置 | `../../../sc/Assets/Resources/Configs/Json/minions.v0.1.json` |
| 统一随从费用 | `ShopEconomyRules.MinionPurchaseCost = 3` |

卡框以原生 1017×1546 像素进行 Alpha 合成，没有重新生成、调色或描摹。数值文字在 240×360 和 160×240 最终分辨率直接绘制，并保留 1 px 描边。

## 正式八卡数据

| 类别 | 卡牌 | 费用 | 等级 | 攻击/生命 | 基础关键词 |
| --- | --- | ---: | ---: | ---: | --- |
| 铸魂 | 铸魂盾侍 | 3 | 1 | 1/3 | 嘲讽 |
| 铸魂 | 不熄炉王 | 3 | 5 | 6/8 | 嘲讽 |
| 荒灵 | 幼鹿灵 | 3 | 1 | 1/1 | 亡语 |
| 荒灵 | 万蹄奔潮 | 3 | 5 | 7/8 | — |
| 星契 | 星盘校准师 | 3 | 2 | 2/2 | — |
| 星契 | 天穹契约者 | 3 | 5 | 4/8 | — |
| 旅团 | 行脚医师 | 3 | 1 | 1/3 | 战吼 |
| 旅团 | 百技学徒 | 3 | 3 | 3/4 | — |

护盾是铸魂盾侍和不熄炉王的开场效果，不作为常驻基础关键词绘制。机制摘要也不等同于配置关键词。

## 输出文件

| 路径 | 用途 |
| --- | --- |
| `full-240x360/*.png` | 八张真实配置 Full 卡 |
| `compact-160x240/*.png` | 八张 Compact 机制摘要卡 |
| `four-digit-stress/*.png` | 不熄炉王 `9999/1200` Full/Compact 压力副本 |
| `review/eight-cards-full-240x360-v0.2.png` | Full 八卡总览 |
| `review/eight-cards-compact-160x240-v0.2.png` | Compact 八卡总览 |
| `review/four-digit-stress-v0.2.png` | 四位数对照评审图 |

四位数压力副本不是正式卡牌数据；正式不熄炉王仍为 6/8。

## Compact 与 Runtime 的差异

本轮 Compact 为组件和可读性压力样板，因此保留费用。当前正式 `CardViewModel` 对 owned Compact 约定为隐藏费用；若组件进入 Runtime，仍应遵守该契约。

另外，当前 Runtime 等级显示为 `T1`–`T5`，本样板的窄书签只显示 `1`–`5`。四个
轻量组件和八张最新立绘已有 Catalog / `PF_Card` 技术预接入候选，但当前环境尚未
运行 Unity 编译、自动化或双分辨率截图。本目录只能证明离线卡面方向成立，不能
声称 G2 运行时接入已经验收。

## 复现

```powershell
python tools/compose_archetype_card_blind_test_v02.py
```

脚本只读取配置、冻结立绘、项目卡框和轻量组件，确定性生成全部卡牌与评审图。

2026-07-24 项目方已按个人版 OpenAI Terms of Use 确认本目录所引用 8 张当前立绘和
4 个轻量组件的生产许可；离线合成卡面不是额外发行资产。上述候选仍须通过 G2，
验收前不得标记为 Runtime Ready。
