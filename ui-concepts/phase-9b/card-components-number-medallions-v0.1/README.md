# Phase 9B 卡牌数字组件样板 v0.1

- 日期：2026-07-24
- 状态：G1/G2 之间的内部视觉与尺寸验证样板
- 共同风格：旅团绘本（Wandering Storybook）v0.3
- 样板卡：不熄炉王

## 文件

| 文件 | 用途 |
| --- | --- |
| `component-strip-card-validation-measured-v0.1.png` | 主评审图：组件条、Full/Compact 对照和尺寸标注 |
| `full-240x360-v0.1.png` | 实际 240×360 Full 尺寸验证 |
| `compact-160x240-v0.1.png` | 实际 160×240 Compact 尺寸验证 |
| `component-strip-card-validation-v0.1.png` | 内置 imagegen 原始视觉稿 |

## 组件约定

| 语义 | 造型 | 运行时内容 |
| --- | --- | --- |
| 费用 | 皮扣悬挂的圆形旧黄铜币 | 数字文本 |
| 等级 | 五边形缝制布签 | 等级数字；颜色由 Tier Palette 驱动 |
| 攻击 | 带短剑浮雕的棱角黑铁章 | 可向内横向扩展的数字文本 |
| 生命 | 皮革底上的深红心形蜡封 | 可向内横向扩展的数字文本 |

数字不烘焙进最终运行时 Sprite。样板中的 `3`、`5`、`9999` 和 `1200`
只用于验证层级和四位数空间。

## 验证结论

- Full 240×360：费用、等级、攻击和生命具有不同轮廓与材质，四位数清晰。
- Compact 160×240：`9999` 与 `1200` 仍可直接读取，没有互相覆盖或遮挡名称。
- 四个组件均保持在卡牌根节点安全区内；攻击向右、生命向左扩展。
- 卡牌插画与名称仍是第一层信息，数字组件没有覆盖主体面部或名称栏。
- 后续运行时实现应保留精确数字，不缩写为 `9.9K` 或 `1.2K`。

## 生成记录

- 视觉底稿：Codex 内置 imagegen。
- 参考：
  - `../style-tiles/style-tile-wandering-storybook-v0.3.png`
  - `../../../sc/Assets/Art/Presentation/UI/Common/card_frame_storybook_normal_v2.png`
  - `../archetype-blind-test-v0.1/masters/forge-undying-furnace-king.png`
- 尺寸裁切、排版与标尺：`tools/compose_card_component_validation.py`。
- 当前为已被轻量 v0.2 取代的重型方向，只保留作对照证据；明确不纳入 28 项生产
  许可签字范围，不得标记为 Runtime Ready 或用于最终发布。

## 最终生成提示词

```text
Design one coherent Wandering Storybook component strip plus two actual
card-face mockups that replace floating colored number squares with physical
storybook objects.

Top row: a round aged-brass cost coin hanging from a leather buckle with 3;
a five-sided ochre-gold stitched tier bookmark with 5; an angular blackened-
iron attack medallion with a sword relief and an inward-expanding capsule
showing 9999; a deep-crimson heart-shaped wax health seal showing 1200.

Bottom row: the same Furnace King card in Full 240×360 and Compact 160×240,
using the shared warm-paper, deep-ink, indigo-stitching and leather-corner
card frame. Place cost upper-left, tier upper-right, attack lower-left and
health lower-right. Both four-digit values must remain readable without
clipping or overlap. Runtime-like warm-ivory numerals use a dark walnut outline.

No modern flat squares, glossy metal, neon, glassmorphism, gothic card frame,
extra icons, logo or watermark. Numerals are runtime text layered over
reusable components, not baked into illustration art.
```
