# Phase 9B 轻量数值组件样板 v0.2

- 日期：2026-07-24
- 状态：内部视觉与尺寸验证样板
- 风格：旅团绘本（Wandering Storybook）v0.3
- 样板卡：不熄炉王

## 这一版解决什么

v0.1 的四个数值组件采用大徽章/大浮雕，视觉重量超过卡名和立绘。v0.2 改为嵌入式小标签：

- 费用：缩小为单枚旧黄铜币，不使用桂冠、光芒或厚重投影。
- 等级：缩小为窄书签，只保留细缝线和五边形收口。
- 攻击：黑铁短剑帽＋向内扩展的低矮数字槽。
- 生命：右侧小蜡印＋向内扩展的低矮数字槽。
- 数字全部是后叠加文本，不烘焙进组件图。
- 流派色仍由立绘和名称区承担；数值组件保持语义中性。

视觉优先级为：立绘 > 卡名 > 攻血 > 费用/等级 > 装饰。

## 文件

| 文件 | 用途 |
| --- | --- |
| `component-strip-card-validation-measured-v0.2.png` | 主评审图：组件条、Full/Compact 实际卡面和四位数验证 |
| `component-strip-v0.2.png` | 独立组件条 |
| `full-240x360-v0.2.png` | 精确 240×360 Full 输出 |
| `compact-160x240-v0.2.png` | 精确 160×240 Compact 输出 |
| `card-base-native-frame-exact-v0.2.png` | 真实项目卡框原生 1017×1546 合成底卡 |
| `components/*.png` | 去绿底后的可复用透明组件 |
| `source-chroma/*.png` | imagegen 原始绿底源图，保留用于追溯 |

## 尺寸约定

| 组件 | Full 240×360 | Compact 160×240 |
| --- | ---: | ---: |
| 费用 | 28×29 | 19×20 |
| 等级 | 21×28 | 14×19 |
| 攻击 | 55×22 | 36×15 |
| 生命 | 55×22 | 36×15 |

攻击和生命只向卡面内侧扩展。`9999` 与 `1200` 是四位数压力值，并非不熄炉王的正式数值；正式普通数值仍为 6/8。

## 卡框来源与合成方式

卡框直接读取：

`sc/Assets/Art/Presentation/UI/Common/card_frame_storybook_normal_v2.png`

源文件尺寸为 1017×1546。脚本先在该原生尺寸上合成立绘与信息区，再把卡框原图以未缩放、未调色的方式进行 Alpha 合成；之后分别缩放到底卡目标尺寸，并在 Full/Compact 最终分辨率直接绘制组件和 1 px 数字描边。

因此这版没有让 imagegen 重绘卡框。Full 与 Compact 的数字也没有先画在大图后再缩小。

## 验证结论

- Full 240×360：四类信息有明确位置和材质差异，攻血条没有遮挡卡名或主体面部。
- Compact 160×240：`9999` 和 `1200` 均保留完整边界，没有裁切或相互覆盖。
- 费用和等级面积约为 v0.1 大徽章方案的三分之一。
- 攻击/生命高度压到 22 px / 15 px，并贴合底边框，视觉上属于卡框的一部分。
- 组件仍可支持 1–2 位常规数值；3–4 位时只扩展中间数字槽，不放大图标帽。

## 复现

```powershell
python tools/compose_card_component_validation_v02.py
```

绿底去除使用 imagegen skill 附带的 `remove_chroma_key.py`，参数为：

```text
--auto-key border --soft-matte
--transparent-threshold 12 --opaque-threshold 210
--edge-contract 1 --despill
```

完整生成提示词见 `PROMPTS.md`。

2026-07-24 项目方已按个人版 OpenAI Terms of Use 确认 4 个轻量组件的生产许可；
当前 Catalog / `PF_Card` 接入仍是 G2 未验证候选，Unity 验收前不得标记为
Runtime Ready。
