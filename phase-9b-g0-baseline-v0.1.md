# 阶段 9B G0 范围与基线冻结记录 v0.1

- 日期：2026-07-22
- 状态：G0 已通过，允许进入 G1 Style Tile
- 工程候选：`5545a4eca69381238c7f0b25098548588df78258`
- Unity：2022.3.62f3c1（1623fc0bbb97）
- 内容身份：5.5.0 / 8B.1
- 完整配置哈希：`818596be90de4e2ddf6c4b7f9ba0b6e1fee994fcc31ec9893652e02f49ef4311`
- 对应方案：`phase-9b-presentation-vertical-slice-technical-design-v0.1.md`
- 资产盘点：`phase-9b-asset-inventory-v0.1.md`
- 来源台账：`phase-9b-asset-source-ledger-v0.1.md`

## 1. G0 冻结结论

| 决策项 | 冻结结论 |
| --- | --- |
| 首要目标平台 | Windows 10/11 x64 PC，键鼠输入 |
| 验证分辨率 | 1920×1080、1920×1200 |
| Unity 版本 | 2022.3.62f3c1，不在 9B 中途升级 |
| 字体 | Noto Sans CJK SC Regular；使用仓库内 SIL Open Font License 1.1 |
| 运行时资产 | PNG、OGG、WAV、Unity Material/Prefab 等可运行导出进入 Git |
| 分层源文件 | PSD、KRA、DAW 工程、高采样率母带进入项目方管理的外部资产库，不直接提交 Git |
| Git LFS | 当前机器已安装 3.7.1，但仓库未配置；G0 不引入 LFS，后续如需迁移必须独立评审 |
| 生成式样板 | 未完成生产许可确认的资产仅限内部工程与风格评审，不标记 Runtime Ready，不作为最终发布资产 |
| 玩法边界 | 9B 不修改规则、数值、地图、随机流、配置身份或存档 schema |
| 范围变更 | 新增样板必须替换等量现有样板，或进入 9C |

## 2. 工程基线

| 项目 | 冻结值 |
| --- | --- |
| 候选提交 | `5545a4e fix(ui): add main menu display camera` |
| EditMode | 266 / 266 通过 |
| PlayMode | 22 / 22 通过 |
| 编译/未处理异常 | 0 |
| G0 复核方式 | 在 detached worktree 中检出精确候选 `5545a4e` 后运行全量 Unity 测试 |
| G0 测试时长 | EditMode 6.954 秒；PlayMode 7.755 秒（不含首次导入和 Runner 退出清理） |
| 正式场景 | Boot、MainMenu、ShopTest、BattleTest、RunTest |
| 正式 UI | Card、MainMenu、Shop、Run/Map、Battle、Choice/System Menu |
| 测试命令 | `.\tools\run_unity_tests.ps1 -Platform All` |

测试数字以候选提交的完整 Unity 回归为准；本次 G0 复核失败、跳过和可疑日志均为 0。文档、清单和来源台账不改变运行时程序集或配置身份。

## 3. 参考测试机

| 项目 | 配置 |
| --- | --- |
| 操作系统 | Windows 11 专业版 64-bit，10.0.26200 |
| CPU | Intel Core i5-12600KF，10 核 16 线程 |
| 内存 | 15.9 GB |
| GPU | NVIDIA GeForce RTX 5070，12227 MiB，驱动 591.86 |
| 角色 | G0/G1 开发与视觉基线机，不替代 G4 的第二台目标测试机 |

G0 只冻结可重复采集环境，不提前写死帧时间或内存门槛。G3 建立性能采集，G4 至少增加一台不同配置的 Windows x64 测试机后再冻结硬门槛。

## 4. 9B 范围

### 4.1 本阶段交付

- 12 张样板随从：铸魂、荒灵、星契各 4 张。
- 3 张 Token、4 张法术、3 件遗珍图标。
- 铸魂、荒灵、星契、旅团四种回退图，法术类型回退图和缺失诊断图。
- 普通/金色公共框架、种族皮肤、首批状态与关键词图标。
- MainMenu、Shop、Run/Map、Battle、Choice/System Menu 的纵向切片换肤。
- 通用商店反馈、十类战斗事件表现、3 套 BGM、P0 音效和四路音量设置。
- 双分辨率、存档恢复、跳过/2×、性能基线和至少 5 名外部试玩者的验收记录。

### 4.2 明确不进入 9B

- 其余 52 张非 Token 随从专属插画和 12 张法术专属插画。
- 其余 12 件遗珍图标、14 个事件专属插画和第二/三层最终背景。
- 每张卡独立动画、粒子、配音或专属音效。
- 微信小游戏、发布包、云存档、局外成长和完整新手引导。
- 任意规则、数值、经济、地图、随机流、配置哈希或存档格式修改。

## 5. Before 截图清单

以下文件作为 G0 视觉差异基线。它们来自 Unity 正式 Prefab 的验证夹具，用于 G1–G3 前后对比；G4 仍须在正式 `MainMenu → Run → Shop → Battle → Run` 链路重新截图和验收。

| 文件 | 分辨率 | 字节 | SHA-256 |
| --- | --- | ---: | --- |
| `ui-concepts/unity-validation/pf-main-menu-v0.1/confirm-dialog-1920x1080.png` | 1920×1080 | 90815 | `3e6cd8d930c5af639dacf9081b53a49c4628fb466cd635f8eff43abce3c77fee` |
| `ui-concepts/unity-validation/pf-main-menu-v0.1/main-menu-1920x1080.png` | 1920×1080 | 99786 | `91106dca9f6cc119c38e7abec89865c9294b9bb3a0e0db7fe2ea497ed384cade` |
| `ui-concepts/unity-validation/pf-main-menu-v0.1/main-menu-1920x1200.png` | 1920×1200 | 104243 | `a557d56dc9d327fd25c74760e02670e5cc8e82b1eda2f3004e8141a8a68c6239` |
| `ui-concepts/unity-validation/pf-shop-screen-v0.1/choice-overlay-1920x1080.png` | 1920×1080 | 263346 | `685d01e6cb0af43f5de7d25e1a30f0ff1380c98f7e39765461d776362019aa80` |
| `ui-concepts/unity-validation/pf-shop-screen-v0.1/shop-feedback-1920x1080.png` | 1920×1080 | 238036 | `e8f966dd3920e2aa1eb01f7755961d8dca22de80ca94ae7863612041327d01f6` |
| `ui-concepts/unity-validation/pf-shop-screen-v0.1/shop-screen-1920x1080.png` | 1920×1080 | 231300 | `212decf75f509f9fe6d2f10e127f40af78423c64a03310e291cdecb3df1a8015` |
| `ui-concepts/unity-validation/pf-shop-screen-v0.1/shop-screen-1920x1200.png` | 1920×1200 | 235802 | `10a8f953ec67c1be604a21a48d15a43947bdcb4a2e9b92e7e7a079e1df9824ec` |
| `ui-concepts/unity-validation/pf-run-screen-v0.1/run-choice-1920x1080.png` | 1920×1080 | 141077 | `6059e2d60358f05aabca8a2595c975f414b96d45e406cbd83fa5b91998df2c73` |
| `ui-concepts/unity-validation/pf-run-screen-v0.1/run-screen-1920x1080.png` | 1920×1080 | 155744 | `a0bb8dcd1fed898b515a5474b6d1f66702df9612439e00f3cdf8385d37b01d7a` |
| `ui-concepts/unity-validation/pf-run-screen-v0.1/run-screen-1920x1200.png` | 1920×1200 | 160425 | `08cad229cd63d1a41429ce0759a79c76eccfa77560872d7caaa50c86cd0ed0e9` |
| `ui-concepts/unity-validation/pf-battle-screen-v0.1/battle-screen-1920x1080.png` | 1920×1080 | 136492 | `c1029d6397b859dbb757ff76bd65f489a7fc0cc3c55d568676685c64540386fe` |
| `ui-concepts/unity-validation/pf-battle-screen-v0.1/battle-screen-1920x1200.png` | 1920×1200 | 140902 | `dd868b1ff57214d15802c4b0bbaa7ae15e92d2693dc93d563e5d29f29341b2ce` |
| `ui-concepts/unity-validation/pf-card-v0.1/pf-card-1920x1080.png` | 1920×1080 | 174859 | `f95030b63b221bc4d41e0984017fdff37058a69e9cd14e3ed4424c3a545d24b8` |
| `ui-concepts/unity-validation/pf-card-v0.1/pf-card-1920x1200.png` | 1920×1200 | 179311 | `251c33e6226a338009c51b4cbfd7ee2462e1d4b1e18208f8368ff35f094455ca` |

## 6. 资产治理

1. 每个外部、购买、委托或生成式资产必须先登记到 `phase-9b-asset-source-ledger-v0.1.md`。
2. 运行时文件名使用小写 ASCII 和下划线，配置继续使用稳定语义 ID。
3. 分层源文件不得在未评审容量、备份和权限方案前进入仓库。
4. 生成式资产缺少具体模型版本、来源或生产许可时，只能标记为内部工程样板。
5. `Runtime Ready` 必须同时满足运行时接线、导入设置、来源、许可、自动化和视觉评审，单独存在 PNG 不算完成。
6. 运行时导出与源文件都必须能够追溯版本；替换文件时同步更新哈希、人工修改和评审结论。

## 7. 已知缺口

- 当前只有不熄炉王和普通/金色框三个运行时工程样板；它们尚未取得生产发布许可确认。
- `PresentationTheme`、回退资源、Catalog 全量校验、音频服务、VFX 和正式界面皮肤尚未实现，属于 G1–G3。
- 当前截图是 Before 基线，不代表正式视觉品质或 G4 正式链路验收。
- 性能硬门槛和第二台测试机将在 G4 冻结。

## 8. G0 退出检查

- [x] 9B 范围、非目标和变更规则已冻结。
- [x] 工程候选、Unity 版本、配置身份和自动化基线已记录。
- [x] Windows x64、键鼠与双分辨率目标已冻结。
- [x] 字体、运行时导出、分层源文件和 Git LFS 策略已有结论。
- [x] 现有 Before 截图已记录路径、尺寸和 SHA-256。
- [x] 资产来源台账已建立；未清许可的生成式资产已限制为工程样板。
- [x] TODO、技术方案和资产盘点已同步到 G0 完成状态。

下一步只进入 G1：制作两套 Style Tile，并以铸魂盾侍、万蹄奔潮、天穹契约者及现有不熄炉王完成风格、流派区分、金色表现和生产成本评审。
