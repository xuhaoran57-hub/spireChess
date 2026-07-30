# Round 12：v0.3.3 五级随从量产 Batch 05

- 日期：2026-07-30
- 范围：剩余 6 张五级随从
- 状态：候选已生成；离线门禁通过；Unity 运行时复验待执行
- Runtime：未修改

## 交付范围

| 阵营 | 随从 |
| --- | --- |
| 铸魂 | 断誓刃魂、千环守墓者 |
| 星契 | 陨星先知、命运洗牌师 |
| 旅团 | 王庭赏金客 |
| 荒灵 | 终花吞世者 |

6 张图均只使用冻结 Style Tile 作为图像参考，内容语义来自
`minions.v0.1.json`。完整 Prompt 与生成记录见
`PROMPTS-v0.3.3.zh-CN.md`。

## 隔离接线

Unity 图片副本：

`sc/Assets/Art/Presentation/Calibration/LightStorybookProductionV033Batch05/`

累计隔离 Catalog：

`sc/Assets/Configs/Presentation/PresentationSpriteCatalog_LightStorybookProductionV033Batch05.asset`

该 Catalog 以 Batch 04 为基线，保留前四批 36 个新增 ArtId，再加入本批 6 个；
总条目数为 77。Unity 菜单
`Spire Chess/UI/Build Light Storybook Production v0.3.3 Batch 05`
可从量产清单重建资源和 Catalog，不修改 Batch 01–04、冻结 v0.3.2 或正式
Runtime Catalog。

## 离线校验

```powershell
python ui-concepts/phase-9c/light-storybook-production-v0.1/validation-round-12-v0.3.3-tier5-production/validate.py
```

当前结果为 `PASS_OFFLINE_UNITY_PENDING`。离线通过不替代 Unity 导入、
普通/金色卡框裁切、EditMode/PlayMode 与人工视觉复核。
