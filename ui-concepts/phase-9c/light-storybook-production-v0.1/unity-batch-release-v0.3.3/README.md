# Phase 9C v0.3.3 Unity 批次放行

- 日期：2026-07-30
- Unity：2022.3.62f3c1，Windows / DX11
- 项目：`sc/`
- 结论：`UNITY_BATCH_RELEASE`
- 人工视觉验收：通过
- Runtime 提升：未执行

本目录记录 v0.3.3 Batch 01–06 的 Unity 重建、自动化测试、双分辨率截图和
人工视觉复核结果。最终候选仍位于隔离的 Calibration Catalog，不修改正式
Runtime Catalog 或冻结的 v0.3.2 Formal Catalog。

## 放行结果

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| Batch 01–06 重建 | 通过，六个构建器均以退出码 0 完成 | `logs/phase9c-capture.log` |
| 最终候选完整性 | 86 个 Catalog 条目；83 个配置 ArtId 全部 Exact；51 / 51 量产 ArtId | `capture-index.json` |
| 量产纹理导入策略 | 51 / 51：MipMap 关闭、Uncompressed、Max 2048、不可读 | `tests/EditMode-results.xml` |
| 卡面矩阵 | 38 张：普通/金色 × Full/Compact，覆盖 Batch 01–06 | `screenshots/` |
| 商店/战斗裁切 | 4 张：Shop、Battle 各 1920×1080 / 1920×1200 | `screenshots/` |
| 截图完整性 | 共 42 张；21 张 1920×1080、21 张 1920×1200 | `capture-index.json` |
| 渲染精确性 | 缺图回退、渲染断言、异常、字体克隆错误均为 0 | `logs/phase9c-capture.log` |
| EditMode | 373 / 373 通过 | `tests/EditMode-results.xml` |
| PlayMode | 30 / 30 通过 | `tests/PlayMode-results.xml` |
| Runtime / Formal 隔离 | 10 / 10 个受保护 Catalog、Prefab 及 `.meta` 前后 SHA-256 一致 | `release-manifest.json` |
| 候选身份稳定性 | Batch 01–06 的 6 / 6 固定 Catalog GUID 重建前后不变 | `release-manifest.json` |
| 人工视觉复核 | 通过：主体裁切、普通/金色身份、名称、等级/费用、属性、规则文本、商店和 5v5 立牌均可辨 | 本文件与 `screenshots/` |

Compact 长文案遵循现有三行/四行省略契约；Full 长文案使用现有五行硬门禁，
任何不适配内容都会中止截图。本次所有卡牌均完成渲染，未触发 Full 布局异常。

Unity 启动日志仅保留本机 Licensing token 刷新提示；脚本编译、Batch 重建、
截图和测试均正常退出。

## 自动化结果

| 平台 | Result | Total | Passed | Failed | Skipped | Inconclusive | Duration |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| EditMode | Passed | 373 | 373 | 0 | 0 | 0 | 6.709 s |
| PlayMode | Passed | 30 | 30 | 0 | 0 | 0 | 33.831 s |

关键证据 SHA-256：

- `capture-index.json`：
  `226cd178b83c90b867f254d0ff0215337f83ce698f1ea94c324e1b37c80f9ddc`
- `logs/phase9c-capture.log`：
  `07918fadccbc42a3b3749439548a0359a804d2093cc0f9927b40a04e24846697`
- `tests/EditMode-results.xml`：
  `ee981d51ce49920f8e27b361750b90c54c8dc76004e44888c86411c70a76e7ae`
- `tests/PlayMode-results.xml`：
  `da593b43a96fa25c39f6e760cf458683b61b43ba80c283efe5f836132f7e8dc1`

`release-manifest.json` 保存所有截图、日志和测试产物的逐文件 SHA-256、10 个
受保护资产的前后哈希，以及 6 个候选 Catalog GUID 的期望值和重建前后值。

## 人工视觉复核

- Batch 01–05 的 42 张量产随从均使用真实候选美术，未出现占位图或缺图回退；
- Batch 06 的 9 张法术在 Full/Compact 下均保持费用、等级、类型与主体清楚；
- 普通与金色卡框、数值翻倍和金色文字身份清楚，主体焦点未被卡框裁掉；
- Shop 商品区、战斗区、手牌区均使用候选 Catalog；选中、护盾、金色和法术状态
  可同时辨认；
- Battle 5v5 立牌主体居中，普通/金色、护盾、嘲讽、亡语与属性徽章无遮挡。

代表性证据：

- [Batch 01 Full](screenshots/batch-01-tier1-full-page-01-1920x1200.png)
- [Batch 05 Full](screenshots/batch-05-tier5-full-page-01-1920x1200.png)
- [Batch 06 法术 Full](screenshots/batch-06-spells-full-page-01-1920x1200.png)
- [Shop 裁切](screenshots/shop-production-crop-1920x1200.png)
- [Battle 裁切](screenshots/battle-production-crop-1920x1200.png)

## 资源预算与隔离

51 张本轮量产源图共 152,534,496 字节（约 145.47 MiB）；按原始像素 RGBA32
估算为 320,895,884 字节（约 306.03 MiB）。它们继续保留在隔离候选中，
本次不扩大 Runtime 常驻资源。若后续提升到正式 Runtime，必须另做纹理压缩、
图集拆分、加载策略和目标平台内存复核，不得把本批放行等同于 Runtime 提升。

## 复跑

在仓库根目录执行：

```powershell
.\tools\run_phase9c_unity_acceptance.ps1
```

脚本会自动解析项目固定的 Unity 版本，重建 Batch 01–06、生成 42 张截图、
运行全量 EditMode/PlayMode、核对受保护资产哈希，并刷新
`release-manifest.json`。
