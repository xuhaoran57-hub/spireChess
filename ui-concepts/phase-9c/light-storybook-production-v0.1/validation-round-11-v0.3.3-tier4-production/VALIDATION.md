# Round 11 验收记录

## 结论

`UNITY_BATCH_RELEASE`

11 / 11 张四级随从完成以下离线门禁：

- 配置身份、批次、等级和生成状态一致；
- 源图与 Unity Calibration 副本 SHA-256 完全一致；
- 画幅位于 1.23–1.27，符合约 5:4；
- 明亮/中间调比例不低于 0.50，近黑比例低于 0.12；
- 图片 `.meta` GUID 与 Batch 04 隔离 Catalog 精确绑定；
- Batch 04 Catalog 共 71 项，并完整包含 Batch 03 的全部 60 项；
- 本批 11 个 ArtId 不存在于 Batch 03、冻结 v0.3.2 或正式 Runtime Catalog。

机器结果见 `VALIDATION-REPORT-v0.3.3.json`。

## 人工生成复核

- 三张铸魂保持连贯的非人形锻造机械身份，护盾、失盾与相邻强化线索清楚。
- 三张星契保持清楚的人形职业身份，并使用无字星图、星环、光轨和法术薄片表达机制。
- 两张旅团保持人类佣兵/监察官身份；破盾动作与三次猎群监察计数可辨，无可读标记。
- 百鸣兽群呈现自然兽群与两只迅捷幼灵；山腹吞灵者保持自然巨熊解剖与吞灵成长关系。
- 藤冠祭司最终版保留山羊祭司、藤冠与恰好四个小型友方灵光，并修正为约 5:4 画幅。
- 全部图片无可读文字、卡框、UI、Logo、签名或水印。

## Unity 批次放行

- Batch 04 Builder 在 Unity 2022.3.62f3c1 中重建通过，11 / 11 Sprite
  导入策略与 Catalog 绑定通过；
- 普通/金色 × Compact/Full 双分辨率矩阵通过人工视觉复核；
- Shop 与 5v5 Battle 裁切通过；
- 全量 EditMode 373 / 373、PlayMode 30 / 30 通过；
- Runtime/Formal 受保护资产哈希保持不变。

统一证据见
[`../unity-batch-release-v0.3.3/`](../unity-batch-release-v0.3.3/README.md)。
状态提升为 `UNITY_BATCH_RELEASE`，仍不修改 Runtime Catalog。
