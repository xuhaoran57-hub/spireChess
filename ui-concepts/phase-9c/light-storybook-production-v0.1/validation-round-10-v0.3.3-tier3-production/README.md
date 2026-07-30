# Round 10：v0.3.3 三级随从量产 Batch 03

- 日期：2026-07-30
- 范围：剩余 7 张三级随从
- 状态：候选已生成；离线门禁通过；Unity 运行时复验待执行
- Runtime：未修改

## 交付范围

| 阵营 | 随从 |
| --- | --- |
| 铸魂 | 逆流铸师、熔核执旗手、誓刃甲胄 |
| 星契 | 回响咏星师 |
| 荒灵 | 古苔巨幼体、群枝唤灵者、獠牙领奔者 |

7 张图均只使用冻结 Style Tile 作为图像参考，内容语义来自
`minions.v0.1.json`。完整 Prompt 与定点编辑记录见
`PROMPTS-v0.3.3.zh-CN.md`。

## 隔离接线

Unity 图片副本：

`sc/Assets/Art/Presentation/Calibration/LightStorybookProductionV033Batch03/`

累计隔离 Catalog：

`sc/Assets/Configs/Presentation/PresentationSpriteCatalog_LightStorybookProductionV033Batch03.asset`

该 Catalog 以 Batch 02 为基线，保留前两批 18 个新增 ArtId，再加入本批 7 个；
总条目数为 60。Unity 菜单
`Spire Chess/UI/Build Light Storybook Production v0.3.3 Batch 03`
可从量产清单重建资源和 Catalog，不修改 Batch 01/02、冻结 v0.3.2 或正式
Runtime Catalog。

## 离线校验

```powershell
python ui-concepts/phase-9c/light-storybook-production-v0.1/validation-round-10-v0.3.3-tier3-production/validate.py
```

当前结果为 `PASS_OFFLINE_UNITY_PENDING`。离线通过不替代 Unity 导入、
普通/金色卡框裁切、EditMode/PlayMode 与人工视觉复核。
