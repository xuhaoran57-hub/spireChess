# Phase 9B G4 正式工程候选证据 v0.1

- 日期：2026-07-26
- 状态：工程自动化证据与 DEV-A P02 通过；项目负责人视觉签字、第二台机器、
  两机门槛、外部试玩和正式音频仍未完成
- Unity：2022.3.62f3c1
- 机器：`DESKTOP-453378L`，Windows 11，i5-12600KF，RTX 5070，15.9 GB RAM
- 目标：Windows x64、Windowed、High、VSync 1、1920×1080 / 1920×1200

本目录归档的是提交 `f377497` 的干净 Development Build 工程候选证据，不是
Release 性能证明，也不代表 G4、阶段 9B、正式音频或 9C 决策已经关闭。

## 1. 候选身份

| 字段 | 值 |
| --- | --- |
| Git commit | `f377497d1f3e65486370d6b35d91811d1bff50bc` |
| Build ID | `20260726-g4-f377497` |
| Build Manifest SHA-256 | `e09691e14ba931dddade86223527fa30e02dfcc6071a0e08be63bfea12023576` |
| EXE SHA-256 | `fa01ccdbaa5f74c777609235b99ba8988285b2bf0754445e85bba268b2e61eb7` |
| Build GUID | `ad484d86de724715b894b7967eead420` |
| 工作树 / 构建 | `sourceTreeDirty=false` / `cleanBuild=true` |
| 构建文件 | 247 个逐文件 SHA-256；Unity BuildReport `totalSizeBytes=251,053,253`；逐文件 `sizeBytes` 求和 251,098,763 bytes（含 45,510-byte DoNotShip 描述文件） |
| Build Manifest | `candidate-manifest/g4-build-manifest.json` |

EXE 壳文件的 SHA 与较早构建相同，因此候选身份必须同时引用完整 Build Manifest
SHA；只引用 EXE SHA 不足以冻结候选。

本目录的局部 `.gitattributes` 将生成证据按原始字节保存，避免 `core.autocrlf`
在重新检出时改写换行并使归档 SHA-256 失效。

## 2. Unity 全量测试

| 平台 | 结果 | 通过 / 失败 / 跳过 / Inconclusive | 耗时 | XML SHA-256 |
| --- | --- | --- | ---: | --- |
| EditMode | Passed | 351 / 0 / 0 / 0 | 6.7499407 s | `a2bdcab030c88a8fe5ce76a35ed427fe51797a5a84281e62e29ddcf88ab774e2` |
| PlayMode | Passed | 30 / 0 / 0 / 0 | 33.5135665 s | `bd29017bfd68ef952ee8f358a5fd75f301d7b98e94b81e01504bc19f2b2137f8` |

两轮均由 `tools/run_unity_tests.ps1` 受控执行，`ForcedShutdown=false`。XML
副本位于 `tests/`；详细 Unity 日志保留于本机忽略目录
`sc/Logs/TestResults/`。

## 3. 正式矩阵

| 矩阵 | Matrix ID | 运行 | 截图 | Summary SHA-256 |
| --- | --- | ---: | ---: | --- |
| Core visual | `20260726-134409-DESKTOP-453378L` | 2 | 32 | `04004d7fa1383868dc460c0180d70d17a5a707c88d69059acb60ce3e3a500036` |
| Frozen visual | `20260726-134454-DESKTOP-453378L` | 2 | 42 | `3c6b7376f3e786ceff8804f034ef5c0d1e3835d14d469fb47bd2b0ed36d55aa1` |
| Stress visual | `20260726-134612-DESKTOP-453378L` | 2 | 6 | `1d4f7bf9b23babc49f2c69364afec5a7ebad8522e909a8200e39ed2077752d60` |
| Core performance | `20260726-135013-DESKTOP-453378L` | 10 | 0 | `3c9c3d1a717b3178bf93d4e1cacf4e1f458bf5802b9e4bf76b43d2aaad23b810` |
| Stress performance | `20260726-135305-DESKTOP-453378L` | 10 | 0 | `ba2eee8fce9ba4c7fc5496b6bff2c3975ca23b07f8b760fda49af37c2130cc4c` |

共 26 / 26 个正式 Player 运行：

- `evidenceClassification=FormalCandidate`；
- `completionStatus=AcceptancePassed`；
- 同一 Git commit、Player SHA 与 Build Manifest SHA；
- Error / Exception / Assert / 独立 failure marker 全部为 0；
- 清理门禁全部通过；
- G2 样板 Catalog `22 / 22 Exact`，可见样板违规 0；
- 非样板 Diagnostic 被显式记录，不作为正式美术命中。

`matrices/` 保存五组 v2 Summary JSON 与 runs CSV。JSON 继续保存每轮报告、原始
CSV、Player.log 的 SHA-256；完整原始文件保留于本机
`sc/Logs/G4/Formal/f377497/`。

## 4. 性能摘要

以下为五轮中位数；数据来自保持可见的 Development Player，无截图编码。

| 模式 / 分辨率 | Frame Avg | P95 | P99 | 单帧 Max | Total Peak / Final |
| --- | ---: | ---: | ---: | ---: | ---: |
| Core 1080p | 17.005 ms | 16.683 ms | 16.684 ms | 116.664 ms | 247.627 / 245.148 MiB |
| Core 1200p | 17.004 ms | 16.683 ms | 16.684 ms | 116.664 ms | 249.380 / 246.880 MiB |
| Stress 1080p | 16.861 ms | 16.683 ms | 16.684 ms | 583.363 ms | 266.868 / 248.963 MiB |
| Stress 1200p | 16.862 ms | 16.683 ms | 16.684 ms | 600.025 ms | 268.307 / 250.905 MiB |

20 个 JSON、20 个原始 CSV 共 42,162 条数据样本（另有 20 行表头）已核对。
四组均无跨完整五轮的单调内存增长；10 / 10 个 Stress 按“末样本前 30 秒的
首个样本 → 末样本”口径均回落 0.773–0.955 MiB，且 FX、非循环 AudioSource、
战斗动画始终为 0。

当前主要性能信号是 Stress 十卡 Shop 初始化：稳定出现 516–617 ms 单帧尖峰，
并伴随约 42.7 MB（约 40.8 MiB）单帧 GC Alloc。P95/P99 随后回到 VSync
附近。第二台机器和产品硬门槛尚未冻结，项目负责人需决定接受或要求优化。

预热与计量时序：

| 模式 | 不计入汇总的完整预热矩阵 | 计量矩阵 |
| --- | --- | --- |
| Core | `20260726-134409-DESKTOP-453378L`，13:44 UTC，1×2 visual | `20260726-135013-DESKTOP-453378L`，13:50 UTC，5×2 |
| Stress | `20260726-134612-DESKTOP-453378L`，13:46 UTC，1×2 visual | `20260726-135305-DESKTOP-453378L`，13:53 UTC，5×2 |

预热与对应计量使用同一 Build/Player/Manifest SHA、High、Windowed、VSync 1、
seed 和分辨率；预热值没有进入 performance Summary。截图编码只发生在被排除的
预热轮。结合 5×2 计量及逐轮/稳定期趋势审查，DEV-A G4-P02 通过。Frozen visual
seed 78 是额外全链路预热/视觉证据，不对应单独的 Frozen performance 组。

## 5. 视觉复核

自动门禁确认 80 / 80 PNG 尺寸正确、非黑且组内语义帧有差异。独立只读复核未发现
P0/P1：

- 随从种族行未被卡框遮挡；
- 种族下方不再显示“亡语 +2”“随从发现 刷新”等分类说明；法术类型行按设计保留；
- Full、Compact、战斗立牌的等级/攻击/生命可辨认；
- 卡面不显示“金色”文字，金色身份只由框架等视觉差异表达；
- 两种分辨率均未发现新增全局裁切。

已知非阻塞 P2：

1. Frozen/Stress 的 Battle Result 日志溢出时，顶部第一条可见日志裁掉半行；
2. Stress 1080p 的购买 Toast 会短暂遮住中间手牌底部数值带；归档选用无此遮挡的
   1200p Stress Shop；
3. Run Reward 第三行只露出上沿，滚动提示的可发现性偏弱。

这些 P2 未改动冻结候选，也不在此文件中伪装为已修复。项目负责人逐图签字时应决定
接受还是进入下一候选修复。

## 6. 代表性原图

完整 80 图由六份 `visual-run-manifests/` 保存原文件名、语义 checkpoint 和
SHA-256。本目录保留 18 张代表性原图供长期复核：

| 文件 | SHA-256 |
| --- | --- |
| `screenshots/1920x1080/02-run-map-left.png` | `748d2f35b594d1b9c925c93ed78c829169ca48576753aeb8b03d40e40eebc742` |
| `screenshots/1920x1200/04-run-map-right.png` | `53899b31fc24454f1f5373e6227b9797ada81ad3706d8f8e27ce14c6b7a46a14` |
| `screenshots/1920x1080/05-shop-entry.png` | `0b30a39805741a84dcec923067b8fcaa55f6227ecc4229627da2b86c91bea3f3` |
| `screenshots/1920x1200/08-shop-target-or-warcry.png` | `4dda1f8672319661d7021017f584eed26eb9a144745a790d9f19cd6f9987b5b9` |
| `screenshots/1920x1080/09-shop-frozen.png` | `f187458883b1bf32de204cec6a99d8d9d2c1b2c06ed98740cb55c098ea66096d` |
| `screenshots/1920x1200/11-shop-upgrade.png` | `dc67202e617f1eaf84d0249bbb89646cb7a09d605835062c2eed8c1f9b1c43eb` |
| `screenshots/1920x1200/stress-01-shop-ten-compact.png` | `8ebf5709f473c1910c8a8c4c1cd0ae8a48ed90ace4712785ac3995c3b5470f5e` |
| `screenshots/1920x1080/12-battle-start.png` | `3ef7fae74f3e578e19144a147133c34cc0d1c453ef8f9fd69298b22dd85883c0` |
| `screenshots/1920x1200/13-battle-attack-shield.png` | `618ea8575a6b5ea1459dbd7bde3db8d70fa537c68ef22124d97f3cb7c0631051` |
| `screenshots/1920x1080/14-battle-death-summon.png` | `3d2791eae5d5007bdb7ae35ea4a2f1e9d6b155e4172cd32af71b3888b73af7fc` |
| `screenshots/1920x1200/15-battle-result.png` | `4951682e59683007e982d70f33969cf2afc57ab4030962f1aab2e9126201895e` |
| `screenshots/1920x1080/16-run-reward.png` | `9d7f552ada23f88e2f66c0dd88fe999a77be5c14a1c257ff87e442fd00f3c557` |
| `screenshots/1920x1080/18-run-system-menu.png` | `655971de1927037412f60380e592738f6ca77c61dbc76d7cca3ff3ab585b5ba2` |
| `screenshots/1920x1200/19-run-audio-settings.png` | `11f7ca0764c05f1e9266e098f86af48c6ad6ddbdd1c00a91be86b4346acd77ac` |
| `screenshots/1920x1080/20-main-menu-saved-run.png` | `8f22f3a2302be892796e326b7ad4586c7a54bb7d46566170629c523f8f54d91a` |
| `screenshots/1920x1200/21-continued-run.png` | `70ab165e181d249b21d6f292c41c9391d2022837c3386c2608c6cd30213606f1` |
| `screenshots/1920x1080/stress-02-battle-nested-ready.png` | `40bdd0c49a74326eb2ec49361105789329b136ebc4b3604c30dd4eb58776d979` |
| `screenshots/1920x1200/stress-03-battle-nested-result.png` | `6a494268875d5283d004c16f8b58c17612257d2806fc30c7363814bb25e1aa03` |

## 7. 未关闭项

- 项目负责人完成 V01/V02 双分辨率逐图复核并签字；
- 决定 Stress Shop 初始化尖峰是否接受或要求优化；
- 使用 `phase-9b-g4-second-machine-execution-v0.1.md` 和已校验执行包在第二台不同
  配置 Windows x64 机器运行同一冻结构建并冻结两机门槛；执行包已生成不等于
  G4-P03 已执行；
- 至少 5 名未参与实现者完成非音频试玩；
- 正式 AI 音频生成、权利台账、`ProductionStrict`、运行基线和人工听审；
- 最终阻塞清零与“进入 9C / 样板返工 / 方向不成立”三选一签字。
