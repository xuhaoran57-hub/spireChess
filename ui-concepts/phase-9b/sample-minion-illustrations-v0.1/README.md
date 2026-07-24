# Phase 9B · 其余六张样板随从立绘 v0.1

本目录补齐阶段 9B 十二张核心样板中尚未制作的六张随从立绘。它们延续
`archetype-anchor-illustrations-v0.2` 已冻结的“旅团绘本”媒介方向与三个
种族的一级形状语言，当前只作为美术样板，不依赖 Unity 环境。

## 立绘清单

| 种族 | 随从 | 等级 | 原始立绘 |
|---|---|---:|---|
| 铸魂 | 回火修补匠 | T2 | `masters/forge-tempering-mender.png` |
| 铸魂 | 裂甲复仇者 | T4 | `masters/forge-cracked-armor-avenger.png` |
| 荒灵 | 腐叶承嗣 | T2 | `masters/wild-rotleaf-heir.png` |
| 荒灵 | 狐群巢母 | T4 | `masters/wild-fox-den-matriarch.png` |
| 星契 | 秘页折光师 | T3 | `masters/star-secret-page-refractor.png` |
| 星契 | 星图掮客 | T3 | `masters/star-star-map-broker.png` |

所有 master 均为 1024×1536、2:3、纯立绘，不包含卡框、文字、数值或 UI。

## 评审材料

| 文件 | 用途 |
|---|---|
| `review/six-sample-minions-review-v0.1.png` | 六图统一方向与种族配对总览 |
| `review/thumbnail-review-160x240-v0.1.png` | Compact 立绘占位原尺寸可读性检查 |
| `review/thumbnails-160x240/*.png` | 六张精确 160×240 缩略图 |
| `PROMPTS.md` | 共享约束、参考图角色与逐张生成提示词 |

评审图可由下列命令重建：

```powershell
python tools\compose_sample_minion_review.py
```

## 当前检查结论

- 铸魂：炉芯、黑铁块体和盾板主题在 160×240 下仍可辨；没有人类面部或
  皮肤。
- 荒灵：野猪守卫与狐巢母的动物一级轮廓不同，枝叶破边保持同族一致性。
- 星契：人物职业、纸页道具与开放细弧足以区分两个功能方向，未使用可读
  文字或封闭光环。
- 六张主体均落在安全区内，后续可直接进入 PF_Card 卡框合成与卡内裁切
  检查；本轮未修改 Unity 资源。

## 生产许可与运行状态

2026-07-24 项目方已按个人版 OpenAI Terms of Use 确认本目录 6 张 master 的生产
许可。它们尚未接入 Runtime / Catalog / `PF_Card`，也未运行 Unity 验收，因此不是
Runtime Ready。
