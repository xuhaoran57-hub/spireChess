# Round 13：v0.3.3 法术量产 Batch 06

- 日期：2026-07-30
- 范围：剩余 9 张法术
- 状态：候选已生成；离线门禁通过；Unity 运行时复验待执行
- 量产清单：51 / 51 已生成
- Runtime：未修改

## 交付范围

| 等级 | 法术 |
| --- | --- |
| 一级 | 应急补给、三连发现 |
| 二级 | 精准训练、厚皮药剂 |
| 三级 | 复制雏形、战团锻造 |
| 四级 | 血脉觉醒 |
| 五级 | 全军升格、命运重铸 |

9 张图均只使用冻结 Style Tile 作为图像参考，内容语义来自
`spells.v0.1.json`。完整 Prompt 见 `PROMPTS-v0.3.3.zh-CN.md`。

## 隔离接线

Unity 图片副本：

`sc/Assets/Art/Presentation/Calibration/LightStorybookProductionV033Batch06/`

最终累计隔离 Catalog：

`sc/Assets/Configs/Presentation/PresentationSpriteCatalog_LightStorybookProductionV033Batch06.asset`

该 Catalog 以 Batch 05 为基线，保留前五批 42 个新增随从 ArtId，再加入本批
9 个法术 ArtId；总条目数为 86。Unity 菜单
`Spire Chess/UI/Build Light Storybook Production v0.3.3 Batch 06`
可从量产清单和法术配置重建资源与 Catalog，不修改 Batch 01–05、冻结 v0.3.2
或正式 Runtime Catalog。

## 离线校验

```powershell
python ui-concepts/phase-9c/light-storybook-production-v0.1/validation-round-13-v0.3.3-spell-production/validate.py
```

离线报告结果仍为 `PASS_OFFLINE_UNITY_PENDING`；它只表示离线脚本不能替代
Unity。Batch 01–06 已完成 Unity 导入、法术卡框裁切、EditMode/PlayMode 与
人工视觉复核，最终状态为 `UNITY_BATCH_RELEASE`，统一证据见
[`../unity-batch-release-v0.3.3/`](../unity-batch-release-v0.3.3/README.md)。
