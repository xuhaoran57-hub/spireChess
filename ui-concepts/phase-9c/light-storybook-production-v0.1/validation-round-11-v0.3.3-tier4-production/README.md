# Round 11：v0.3.3 四级随从量产 Batch 04

- 日期：2026-07-30
- 范围：剩余 11 张四级随从
- 状态：候选已生成；离线门禁通过；Unity 运行时复验待执行
- Runtime：未修改

## 交付范围

| 阵营 | 随从 |
| --- | --- |
| 铸魂 | 烬甲裁决者、炉心圣盾官、鸣铁堡垒 |
| 星契 | 陨光裁定者、星环司库、星门讲师 |
| 旅团 | 破阵佣兵、猎群监察官 |
| 荒灵 | 百鸣兽群、山腹吞灵者、藤冠祭司 |

11 张图均只使用冻结 Style Tile 作为图像参考，内容语义来自
`minions.v0.1.json`。完整 Prompt 与定点编辑记录见
`PROMPTS-v0.3.3.zh-CN.md`。

## 隔离接线

Unity 图片副本：

`sc/Assets/Art/Presentation/Calibration/LightStorybookProductionV033Batch04/`

累计隔离 Catalog：

`sc/Assets/Configs/Presentation/PresentationSpriteCatalog_LightStorybookProductionV033Batch04.asset`

该 Catalog 以 Batch 03 为基线，保留前三批 25 个新增 ArtId，再加入本批 11 个；
总条目数为 71。Unity 菜单
`Spire Chess/UI/Build Light Storybook Production v0.3.3 Batch 04`
可从量产清单重建资源和 Catalog，不修改 Batch 01/02/03、冻结 v0.3.2 或正式
Runtime Catalog。

## 离线校验

```powershell
python ui-concepts/phase-9c/light-storybook-production-v0.1/validation-round-11-v0.3.3-tier4-production/validate.py
```

离线报告结果仍为 `PASS_OFFLINE_UNITY_PENDING`；它只表示离线脚本不能替代
Unity。Batch 01–06 已完成 Unity 导入、普通/金色卡框裁切、
EditMode/PlayMode 与人工视觉复核，最终状态为 `UNITY_BATCH_RELEASE`，统一证据见
[`../unity-batch-release-v0.3.3/`](../unity-batch-release-v0.3.3/README.md)。
