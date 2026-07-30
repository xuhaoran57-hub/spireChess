# Round 10 验收记录

## 结论

`UNITY_BATCH_RELEASE`

7 / 7 张三级随从完成以下离线门禁：

- 配置身份、批次、等级和生成状态一致；
- 源图与 Unity Calibration 副本 SHA-256 完全一致；
- 画幅位于 1.23–1.27，符合约 5:4；
- 明亮/中间调比例不低于 0.50，近黑比例低于 0.12；
- 图片 `.meta` GUID 与 Batch 03 隔离 Catalog 精确绑定；
- Batch 03 Catalog 共 60 项，并完整包含 Batch 01/02 的 18 个新增 ArtId；
- 本批 7 个 ArtId 不存在于 Batch 02、冻结 v0.3.2 或正式 Runtime Catalog。

机器结果见 `VALIDATION-REPORT-v0.3.3.json`。

## 人工生成复核

- 三张铸魂均为连贯非人形机械结构，无穿戴者、异常肢体或可读纹章。
- 回响咏星师保持清楚人形职业身份，法术薄片无字，共鸣回响数量清楚。
- 古苔巨幼体保持四足一尾和两块蛋壳；身体仍以自然蝾螈为主。
- 群枝唤灵者保持两翼两足，定点修正后仅保留三株核心根芽。
- 獠牙领奔者保持自然野猪解剖、两枚獠牙和清楚的召唤物消散关系。
- 全部图片无可读文字、卡框、UI、Logo、签名或水印。

## Unity 批次放行

- Batch 03 Builder 在 Unity 2022.3.62f3c1 中重建通过，7 / 7 Sprite
  导入策略与 Catalog 绑定通过；
- 普通/金色 × Compact/Full 双分辨率矩阵通过人工视觉复核；
- Shop 与 5v5 Battle 裁切通过；
- 全量 EditMode 373 / 373、PlayMode 30 / 30 通过；
- Runtime/Formal 受保护资产哈希保持不变。

统一证据见
[`../unity-batch-release-v0.3.3/`](../unity-batch-release-v0.3.3/README.md)。
状态提升为 `UNITY_BATCH_RELEASE`，仍不修改 Runtime Catalog。
