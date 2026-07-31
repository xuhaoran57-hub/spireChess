# Phase 9C v0.3.3 Runtime 晋级门禁技术设计 v0.1

## 1. 目标

在任何代码复制量产纹理或改写正式 `PresentationSpriteCatalog.asset` 之前，提供一个
默认阻断、可在 Unity BatchMode 重放的晋级门禁。门禁只判定“是否允许开始晋级”，
不执行晋级。

## 2. 权威输入

- `phase-9c-v0.3.3-runtime-promotion-contract.json`
- `phase-9c-v0.3.3-runtime-promotion-signoff.md`
- `ui-concepts/phase-9c/light-storybook-production-v0.1/PRODUCTION-MANIFEST-v0.3.3.json`
- `ui-concepts/phase-9c/light-storybook-production-v0.1/unity-batch-release-v0.3.3/`
- Batch 06 隔离 Catalog 与当前正式 Runtime Catalog

机器契约固定候选 GUID、数量、Unity 版本、自动化结果、截图索引、晋级前 Runtime
状态、目标导入策略和负责人批准状态。

## 3. 判定流程

1. **RPG-01 候选身份**：验证 Batch 06 GUID、86 个唯一且非空的 Catalog 条目；
   当前配置的 83 个 ArtId 必须全部 Exact。
2. **RPG-02 生产清单**：验证 51 项均为 `generated`，ID/ArtId 唯一，源文件及
   `sources` 中配置、基线 Catalog、Style Tile、Prompt 的 SHA-256 不漂移。
3. **RPG-03 Unity 证据**：验证 Release Manifest、Capture Index、373/373、
   30/30、42 图；逐文件复验 47 项已归档证据的大小与 SHA-256，固定未归档
   Capture Log 的 Manifest 身份，并确认 10 项受保护资产集合及前后哈希一致。
4. **RPG-04 晋级前隔离**：正式 Catalog 保持固定 GUID 和 24 条目；51 项量产
   ArtId 不得提前出现；Batch 06 中对应 Sprite 必须来自六个 Calibration 批次，
   且副本哈希与生产源图一致。
5. **RPG-05 目标策略**：验证正式目录、DXT1、Max 1024、Quality 50、禁止
   Calibration 引用、保留 Runtime Catalog GUID 及晋级后复验要求。
6. **RPG-06 人工批准**：状态必须为 `Approved`，签字人与 ISO-8601 时间非空，
   四项负责人确认均为 `true`。

任一项失败，命令行入口以非零退出；失败明细写入
`sc/Logs/Phase9C/RuntimePromotionGate/v0.3.3/gate-result.json`。

## 4. 入口

Unity 菜单：

```text
Spire Chess/Release/Validate Phase 9C v0.3.3 Runtime Promotion Gate
```

仓库根目录：

```powershell
.\tools\run_phase9c_runtime_promotion_gate.ps1
```

RPG-06 保持 Pending 时，入口失败是预期行为。技术测试通过与负责人批准是两个独立
事实，不提供忽略批准的正式命令行参数。

## 5. Promotion Builder

`LightStorybookRuntimePromotionBuilder` 已实现为门禁后的独立第二阶段：

- 动态复制最终候选中所有指向 `Assets/Art/Presentation/Calibration/` 的图片，
  包括 51 项 v0.3.3 量产图及候选继承的隔离基线，写入
  `Assets/Art/Presentation/Runtime/LightStorybookV033/`；
- 统一应用 Sprite Single、Windows DXT1、Max 1024、Quality 50、关闭
  Mipmap/Readable 的冻结策略；
- 在原正式 Catalog 对象上复制候选序列化内容并重绑 Runtime Sprite，因此
  `PresentationSpriteCatalog.asset` 的 GUID 保持不变；
- Catalog 写入放在图片复制和导入复验之后；最终复验失败时恢复晋级前 Catalog；
- 重复执行先识别完整的晋级后状态，合法时不再改变资源，只重写确定性晋级清单；
- 晋级后强制验证 86 项唯一条目、83 项配置 Exact、51 项生产哈希、零 Calibration
  引用、目标导入策略和候选/Runtime Catalog GUID。

Unity 菜单：

```text
Spire Chess/Release/Promote Phase 9C v0.3.3 to Runtime
```

干净工作树中的正式命令行入口：

```powershell
.\tools\run_phase9c_runtime_promotion.ps1
```

执行后生成
`ui-concepts/phase-9c/light-storybook-production-v0.1/runtime-promotion-v0.3.3/promotion-manifest.json`。
Builder 完成只建立 Runtime 候选；仍须在提交前完成全量回归、Clean Player、视觉和
内存证据，才能标记 `Runtime Ready`。

本门禁不实现 Addressables、动态图集或多平台策略；当前目标仅为已冻结的
Windows x64 候选。
