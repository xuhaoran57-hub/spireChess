# Phase 9C v0.3.3 Runtime 晋级验收摘要

- 状态：`Runtime Ready`
- 关闭时间：`2026-07-31T22:10:48+08:00`
- Unity：`2022.3.62f3c1`
- 晋级提交：`8fc61a5472b2ca4eb14e02b88c427e1dd8b089fb`
- Build ID：`v033-runtime-8fc61a5-20260731`
- 证据分类：`FormalCandidate`
- 关闭授权：项目负责人在 2026-07-31 Codex 任务中指示
  `关闭 v0.3.3 Runtime 晋级`

本摘要只关闭 v0.3.3 的 42 张非 Token 随从与 9 张法术 Runtime 晋级。正式音频、
背景生产许可、G3/G4 总门禁、第二台机器和外部试玩不在本次关闭范围内。
正式 Catalog 的 83 个配置 ArtId Exact 包含 3 张既有 G2 Token；Exact 只证明
接线正确，不代表这 3 张 Token 已按 v0.3.3 冻结风格重新生成。Token 新风格更新
作为独立的 `v0.3.4 Token Refresh` 管理。

## 1. 晋级身份与导入策略

- 晋级清单：`promotion-manifest.json`
- 清单 SHA-256：
  `98d79536a00f1b85570064ad35a65b632e644899ac3b5db930abc054d69ea294`
- 清单状态：`PROMOTED`
- 正式 Catalog GUID：`75d638606a8084146524a35a317a2cca`，晋级前后保持不变。
- 正式 Catalog：86 个条目、86 个唯一 ID；83 个配置 ArtId 全部 Exact。
- v0.3.3 量产美术：51 / 51；正式目录共 66 张晋级 PNG，其中包含 15 张
  v0.3.2 Formal 基线和 51 张 v0.3.3 量产图。
- Runtime 引用 Calibration 数：0。
- 66 张纹理均使用 Standalone DXT1、Max 1024、Quality 50，并关闭
  Mipmap 与 Read/Write；源文件、Runtime 文件与导入策略复核无失败。

## 2. 自动化与干净 Player

| 项目 | 结果 | SHA-256 |
| --- | --- | --- |
| EditMode | 383 / 383，通过；0 失败、0 跳过 | `353ea0f371575372f0b9254beec04c51ad25ac7ae08462ae17aadd8925aa1c36` |
| PlayMode | 30 / 30，通过；0 失败、0 跳过 | `139ab1debb938156d0c4a70607cb69a035f8034cc0e774a98195df6c258bc79b` |
| G4-V 聚合清单 | 双分辨率 10 图，`FormalCandidate` | `0361b40bbff5675f3c80ace1081e86d0d161338f2e9e260296d138e3e730539b` |
| Stress 汇总 | 2 个分辨率 × 5 次，10 / 10 `AcceptancePassed` | `32f81fb5be18ae91c21324c956a6320ec55b70d33721361308caabf6a83428ae` |
| Stress CSV | 10 次运行索引 | `cb7fa9f0b90fa33e0a24021e4261ac2b234a17ea3dca8117ee2ae2ddca19b12a` |

本机原始证据分别位于
`sc/Logs/G4/G4V/v033-runtime-8fc61a5-20260731/` 与
`sc/Logs/G4/RuntimePromotion/v033-runtime-8fc61a5-20260731-stress/`。

Player 由 `sourceTreeDirty=false`、`cleanBuild=true` 的晋级提交构建：

- Player：`sc/Builds/G4/v033-runtime-8fc61a5-20260731/Windows-x64/SpireChess.exe`
- Player SHA-256：
  `fa01ccdbaa5f74c777609235b99ba8988285b2bf0754445e85bba268b2e61eb7`
- Build Manifest SHA-256：
  `95c013dd6bb5ecd1dacacdfd9245339804c68fedff0ba415e50bc1e9fca4dca4`
- 构建清单：248 个文件、426,148,750 bytes（406.41 MiB）。

## 3. 双分辨率视觉结论

1920×1080 与 1920×1200 各复核 Main Menu、Floor Map、Shop、Battle 和
Tranquil Grove Event，共 10 张正式 Player 截图。报告中的样板 Catalog 为
22 / 22 Exact，画面观测为 fallback=0、diagnostic=0、missing=0；人工复核未发现
由本次 Runtime 替换引入的缺图、错误回退、主体裁切、不可接受的 DXT1 失真或色带。

该结论只证明本次卡牌美术晋级没有视觉阻断项，不替代五张背景的独立生产许可签字，
也不宣称关闭既有的全局 UI 优化项。

## 4. 内存与首次 Shop 证据

短链 G4-V：

| 分辨率 | Peak Total | Final Total | Peak/Final Texture | Shop Load | Activation → First Frame | First Frame |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1920×1080 | 449.06 MiB | 447.89 MiB | 351.75 MiB | 13.16 ms | 38.72 ms | 33.33 ms |
| 1920×1200 | 450.70 MiB | 449.59 MiB | 351.75 MiB | 12.37 ms | 37.74 ms | 33.33 ms |

60 秒 Stress 矩阵：

| 分辨率 | 运行 | 首次 10 卡 Shop 激活 | 最大帧 | Peak Total | Final Total | Texture |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1920×1080 | 5 / 5 | 488.75–554.07 ms | 483.35–633.35 ms | 462.36–467.76 MiB | 444.92–445.25 MiB | 345.85 MiB |
| 1920×1200 | 5 / 5 | 492.25–501.23 ms | 616.69–633.35 ms | 464.08–469.38 MiB | 446.70–446.95 MiB | 345.85 MiB |

10 次运行均为 Catalog Exact、清理归零；最后 30 秒纹理内存范围为 0，Total
Memory 净变化均为负，未观察到单调增长。首次 10 卡 Shop 的 0.49–0.55 秒激活和
约 0.63 秒最大帧作为本次晋级接受的已知一次性尖峰保留；它不关闭 G4 的第二机与
跨机器性能门槛。

## 5. 剩余边界

性能报告因 28 个音频 Cue 仍为 `Placeholder` 而保留 audio provisional 标记；
该标记不影响本次 Sprite Runtime 晋级结论，但正式音频仍必须单独通过
`ProductionStrict`、听审和 G3 门禁。
