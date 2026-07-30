# Round 8：v0.3.3 一级随从量产 Batch 01

- 日期：2026-07-30
- 范围：剩余 7 张一级随从
- 状态：候选已生成；离线门禁通过；Unity 运行时复验待执行
- Runtime：未修改

## 交付范围

| 阵营 | 随从 |
| --- | --- |
| 铸魂 | 铜环学徒、炉心火种 |
| 荒灵 | 裂爪幼兽、苔痕守苗 |
| 星契 | 星尘随侍、观星学徒 |
| 旅团 | 流浪剑客 |

七张图片均使用冻结 Style Tile 作为唯一图像参考，内容语义来自
`minions.v0.1.json`。完整 Prompt 见 `PROMPTS-v0.3.3.zh-CN.md`。

## 隔离接线

资源副本位于：

`sc/Assets/Art/Presentation/Calibration/LightStorybookProductionV033Batch01/`

隔离 Catalog：

`sc/Assets/Configs/Presentation/PresentationSpriteCatalog_LightStorybookProductionV033Batch01.asset`

Unity 菜单
`Spire Chess/UI/Build Light Storybook Production v0.3.3 Batch 01`
可从量产清单重新复制、配置并校验七张图片。该流程以 v0.3.2 Formal Catalog
为基线创建新 Catalog，不修改冻结 v0.3.2 Catalog 或正式 Runtime Catalog。

## 离线校验

```powershell
python ui-concepts/phase-9c/light-storybook-production-v0.1/validation-round-8-v0.3.3-tier1-production/validate.py
```

校验覆盖：

- 7 张一级随从身份与配置范围；
- 文件哈希、约 5:4 画幅和 v0.3.3 亮度门槛；
- Unity Calibration 副本一致性；
- 图片 `.meta` GUID 与隔离 Catalog 精确绑定；
- 新增 7 个 ArtId 不进入 v0.3.2 冻结 Catalog 和正式 Runtime Catalog。

离线报告结果仍为 `PASS_OFFLINE_UNITY_PENDING`，详见
`VALIDATION-REPORT-v0.3.3.json`；它只表示离线脚本不能替代 Unity。Batch 01–06
已完成 Unity 导入、卡框裁切、EditMode/PlayMode 和人工视觉复核，最终状态为
`UNITY_BATCH_RELEASE`，统一证据见
[`../unity-batch-release-v0.3.3/`](../unity-batch-release-v0.3.3/README.md)。
