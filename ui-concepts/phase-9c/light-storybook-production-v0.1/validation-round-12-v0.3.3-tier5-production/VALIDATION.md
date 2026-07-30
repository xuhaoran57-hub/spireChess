# Round 12 验收记录

## 结论

`UNITY_BATCH_RELEASE`

6 / 6 张五级随从完成以下离线门禁：

- 配置身份、批次、等级和生成状态一致；
- 源图与 Unity Calibration 副本 SHA-256 完全一致；
- 画幅位于 1.23–1.27，符合约 5:4；
- 明亮/中间调比例不低于 0.50，近黑比例低于 0.12；
- 图片 `.meta` GUID 与 Batch 05 隔离 Catalog 精确绑定；
- Batch 05 Catalog 共 77 项，并完整包含 Batch 04 的全部 71 项；
- 本批 6 个 ArtId 不存在于 Batch 04、冻结 v0.3.2 或正式 Runtime Catalog。

机器结果见 `VALIDATION-REPORT-v0.3.3.json`。

## 人工生成复核

- 断誓刃魂保持无穿戴者的连贯铸魂机械，破盾、攻击增幅与击杀回盾关系可辨。
- 千环守墓者保持非人形环形守墓机械；恰好两个存活友方获得护盾，群体成长波清楚。
- 陨星先知保持人类职业身份，两张空白法术纸与两个低攻击友方目标数量准确。
- 命运洗牌师保持人类职业身份，三段刷新轮、三张无字候选与一枚金币清楚。
- 王庭赏金客保持人类旅团身份，广域起手波与三名同族敌人的重点二次波可辨。
- 终花吞世者为无脸、非动物、非人形的远古终末花灵；两个永久琥珀种核与死亡
  生命回声被花心吸收的关系清楚，未使用全族统一身体模板。
- 全部图片无可读文字、卡框、UI、Logo、签名或水印。

## Unity 批次放行

- Batch 05 Builder 在 Unity 2022.3.62f3c1 中重建通过，6 / 6 Sprite
  导入策略与 Catalog 绑定通过；
- 普通/金色 × Compact/Full 双分辨率矩阵通过人工视觉复核；
- Shop 与 5v5 Battle 裁切通过；
- 全量 EditMode 373 / 373、PlayMode 30 / 30 通过；
- Runtime/Formal 受保护资产哈希保持不变。

统一证据见
[`../unity-batch-release-v0.3.3/`](../unity-batch-release-v0.3.3/README.md)。
状态提升为 `UNITY_BATCH_RELEASE`，仍不修改 Runtime Catalog。
