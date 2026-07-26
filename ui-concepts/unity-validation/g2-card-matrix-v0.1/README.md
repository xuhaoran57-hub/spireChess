# Phase 9B G2 专门卡面视觉矩阵 v0.1

> 历史审核基线：后续复核发现正文/标签安全区、Compact 截断身份表达和 3 张
> 插画横构图仍需修正。最终通过版本及回归结论见相邻的
> `g2-card-matrix-v0.3/`；本目录不再代表当前最终状态。

- 日期：2026-07-25
- Unity：2022.3.62f3c1
- 采集入口：`Spire Chess/UI/Capture G2 Card Matrix`
- 采集实现：`sc/Assets/Editor/G2CardMatrixCapture.cs`
- 状态：13 张目标卡的双分辨率专门视觉矩阵通过
- 门禁边界：G2 总门禁仍未关闭；唯一剩余阻塞是新增 11 项 ImageGen
  资产的生产许可/来源签字

## 1. 范围与配置身份

本矩阵补齐 G1 尚未覆盖的 6 张核心随从、3 个 Token 和 4 张法术。采集工具直接从
正式 Resources 配置构建 `CardViewModel`，并在截图前要求下列 13 个配置 `artId`
全部通过 `PresentationSpriteCatalog.TryGetArtwork` 精确命中；不允许使用语义回退
或缺失诊断图代替。

| 类型 | 名称 | 配置 ID | 配置 `artId` |
| --- | --- | --- | --- |
| 随从 | 回火修补匠 | `tempering_mender` | `placeholder_card_tempering_mender` |
| 随从 | 裂甲复仇者 | `cracked_armor_avenger` | `placeholder_card_cracked_armor_avenger` |
| 随从 | 腐叶承嗣 | `rotleaf_heir` | `placeholder_card_rotleaf_heir` |
| 随从 | 狐群巢母 | `fox_den_matriarch` | `placeholder_card_fox_den_matriarch` |
| 随从 | 秘页折光师 | `secret_page_refractor` | `placeholder_card_secret_page_refractor` |
| 随从 | 星图掮客 | `star_map_broker` | `placeholder_card_star_map_broker` |
| Token | 幼灵 | `token_young_spirit` | `placeholder_token_young_spirit` |
| Token | 双尾狐影 | `token_two_tailed_fox_shadow` | `placeholder_token_two_tailed_fox_shadow` |
| Token | 迅捷幼灵 | `token_swift_young_spirit` | `placeholder_token_swift_young_spirit` |
| 法术 | 小型锻体 | `minor_tempering` | `placeholder_spell_minor_tempering` |
| 法术 | 免费刷新 | `free_refresh` | `placeholder_spell_free_refresh` |
| 法术 | 高阶发现 | `advanced_discovery` | `placeholder_spell_advanced_discovery` |
| 法术 | 战前赐福 | `prebattle_benediction` | `placeholder_spell_prebattle_benediction` |

## 2. 状态与渲染计数

每个目标分辨率均使用同一份正式 `PF_Card` 完成以下渲染：

| 画面 | Full 240×360 | Compact 160×240 | 每分辨率合计 |
| --- | ---: | ---: | ---: |
| 6 张核心随从普通/金色 | 12 | 12 | 24 |
| 3 个 Token | 3 | 3 | 6 |
| 4 张法术 | 4 | 4 | 8 |
| 合计 | 19 | 19 | 38 |

1920×1080 与 1920×1200 各完成 38 次 `PF_Card` 渲染，双分辨率总计 76 次。
Token 只验证普通 Token 身份；法术不具有金色、攻防或随从状态。6 张核心随从的
普通/金色版本复用同一主插画，由共享框架、材质、数值和状态参数表达差异。

## 3. 截图证据

| 文件 | 内容 | 尺寸 | 字节数 | SHA-256 |
| --- | --- | ---: | ---: | --- |
| `g2-minions-full-1920x1080.png` | 6 随从普通/金色 Full | 1920×1080 | 1,376,817 | `2d3b7bdd90612617644a9aa36b486f483eb9a90478148673a1a51621b1e7a616` |
| `g2-minions-full-1920x1200.png` | 6 随从普通/金色 Full | 1920×1200 | 1,381,433 | `20a46601634d1d91ac2c3d94e6a8aaf1472e78bae5ebb1418895a20f3ec09b4f` |
| `g2-token-spells-full-1920x1080.png` | 3 Token＋4 法术 Full | 1920×1080 | 799,961 | `edf2ab0f8a24bff8c421da5473221a4d7f45459a479b80b2c591524e4344c45c` |
| `g2-token-spells-full-1920x1200.png` | 3 Token＋4 法术 Full | 1920×1200 | 804,443 | `553665919d2b6fa25c632fc215f67e6ae0303df558bf38252fb4a0fe12258baf` |
| `g2-all-compact-1920x1080.png` | 全部 19 个 Compact 状态 | 1920×1080 | 1,018,304 | `96e55454fe7a1920ff13095edc96c020a0e3b3f26037742b4dce7932d35f1ffe` |
| `g2-all-compact-1920x1200.png` | 全部 19 个 Compact 状态 | 1920×1200 | 1,022,854 | `180f1b4e33e710ef2fc6d3e66fbbf4c8cc5fa1d6d29b086c0b5db96a3c88639c` |

## 4. 人工视觉验收

六张证据图逐项检查通过：

- 13 个配置身份与插画一一对应，没有错图、空白、语义回退或缺失诊断图。
- Full/Compact 的主体焦点、流派轮廓和主色均保留；两种分辨率之间没有新增裁切偏移。
- 名称、等级/费用、种族/法术类型、正文、攻防和适用状态均保持可读；Compact
  长文案遵循既有截断契约，没有越出卡框或覆盖核心数值。
- 6 张随从的普通/金色身份可立即区分，同时保留相同角色与流派色。
- 3 个 Token 显示 T0、无购买费用、正确攻防与“战斗结束后消失”等配置正文。
- 4 张法术显示正确费用、类型与规则文本，不显示攻防或金色/随从状态。
- 1920×1080、1920×1200 下均无核心遮挡、错误叠层或画面越界。

G1 已验证的通用状态覆盖、商店/战斗/选择层上下文和共享组件复用结论保持有效；
本矩阵专门补齐此前只有 master/缩略图检查的 13 张目标卡。

## 5. 本轮代码回归

矩阵检查暴露并锁定两处运行时回归：

1. `CardViewRenderTests.EffectlessToken_UsesAuthoredDescription`
   - 无效果 Token 过去会因 `Effects` 为空而显示通用原型提示，覆盖已配置正文。
   - `MinionConfig.GetPrototypeDescription` 现在优先保留 Token 已配置的
     `Description` / `GoldenDescription`。
2. `BattleUiPrefabTests.StandeeDetail_PreservesArtworkFallbackResolution`
   - 战斗立牌构建 Hover/锁定详情模型时过去遗漏 `ArtworkFallbackId`。
   - `BattleScreenView` 现在将该字段传递给详情卡，确保详情与立牌使用一致的
     插画解析链。

最终 Unity 全量结果：

- EditMode：280 / 280 通过，0 失败，0 跳过。
- PlayMode：22 / 22 通过，0 失败，0 跳过。

## 6. 门禁结论

新增 6 张核心随从、3 个 Token 和 4 张法术的专门卡面视觉矩阵已通过，相关视觉待办
关闭。G2 主门继续保持未完成；新增 3 个 Token、4 张法术、3 件遗珍和 1 张缺失诊断图
共 11 项仍为 `工程样板`，在项目方完成生产许可/来源签字前不得标记为
`生产许可已确认`、`Runtime Ready` 或用于最终发布。
