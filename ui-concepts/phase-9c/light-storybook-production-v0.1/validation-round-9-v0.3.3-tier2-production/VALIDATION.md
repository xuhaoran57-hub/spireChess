# Round 9 验收记录

## 结论

`PASS_OFFLINE_UNITY_PENDING`

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

## 待 Unity 执行

- 运行 Batch 02 Builder 并确认 11 个 Sprite 的导入设置；
- 生成普通/金色 × Compact/Full 十一卡矩阵；
- 检查商店与战斗立牌裁切；
- 运行全量 EditMode / PlayMode；
- 完成人工视觉复核后再决定是否提升状态。
