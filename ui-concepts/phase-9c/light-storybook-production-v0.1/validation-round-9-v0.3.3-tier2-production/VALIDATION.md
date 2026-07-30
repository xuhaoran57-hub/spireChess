# Round 9 验收记录

## 结论

`UNITY_BATCH_RELEASE`

11 / 11 张二级随从完成以下离线门禁：

- 配置身份、批次、等级和生成状态一致；
- 源图与 Unity Calibration 副本 SHA-256 完全一致；
- 画幅位于 1.23–1.27，符合约 5:4；
- 明亮/中间调比例不低于 0.50，近黑比例低于 0.12；
- 图片 `.meta` GUID 与 Batch 02 隔离 Catalog 精确绑定；
- Batch 02 Catalog 共 53 项，并完整包含 Batch 01 的 7 个新增 ArtId；
- 本批 11 个 ArtId 不存在于 Batch 01、冻结 v0.3.2 或正式 Runtime Catalog。

机器结果见 `VALIDATION-REPORT-v0.3.3.json`。

## 人工生成复核

- 铸魂三张均为灵魂驱动的连贯机械结构，无异常肢体。
- 星契三张均保持人形职业身份，并使用外置观星、知识或契约物件。
- 旅团两张以职业和旅行装备识别；黑市小贩最终铜币无标记。
- 荒灵三张均保持自然动物解剖；疾羽林隼为两翼两足，双尾狐灵严格双尾。
- 全部图片无可读文字、卡框、UI、Logo、签名或水印。

## Unity 批次放行

- Batch 02 Builder 在 Unity 2022.3.62f3c1 中重建通过，11 / 11 Sprite
  导入策略与 Catalog 绑定通过；
- 普通/金色 × Compact/Full 双分辨率矩阵通过人工视觉复核；
- Shop 与 5v5 Battle 裁切通过；
- 全量 EditMode 373 / 373、PlayMode 30 / 30 通过；
- Runtime/Formal 受保护资产哈希保持不变。

统一证据见
[`../unity-batch-release-v0.3.3/`](../unity-batch-release-v0.3.3/README.md)。
状态提升为 `UNITY_BATCH_RELEASE`，仍不修改 Runtime Catalog。
