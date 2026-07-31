# 项目临时与废弃项归档（2026-07-31）

本目录保存本次清理中从活动项目位置移出的内容。归档原则是：只有能够证明为
生成缓存、无活动引用的临时检查工程、与 Runtime 完全重复的根目录副本、
被当前版本明确取代的生成文档，或不参与当前运行时/验收的早期草稿才移动。

归档不表示永久删除。`MOVE-MAP.csv` 记录每个顶层移动项的原路径和归档路径；
`FILE-MANIFEST.csv` 记录 payload 中每个文件的大小与 SHA-256，可用于完整性检查。

Git 中的完整归档单元是同级 ZIP、校验文件和本目录控制文件。为避免把同一批
临时二进制以“解包目录 + ZIP”重复写入仓库，`generated-check-harnesses/` 与
`python-bytecode/` 的本地解包副本由 `.gitignore` 精确忽略；ZIP 已逐文件按
`FILE-MANIFEST.csv` 验证。原本受版本控制的重复配置、旧图鉴和草稿仍以普通文件
保存在 payload 中，便于直接查看和按路径恢复。

## 归档范围

| 类别 | 文件数 | 字节数 | 说明 |
| --- | ---: | ---: | --- |
| 临时编译与模拟检查工程 | 383 | 95,949,209 | `sc/Temp` 下 10 个无活动引用的 .NET 检查目录及其 `bin/obj` 输出 |
| Python 字节码缓存 | 12 | 187,367 | 6 个可重新生成的 `__pycache__` 目录 |
| 根目录重复配置 | 2 | 128,868 | 与 Unity Runtime 配置逐字节一致，且没有工具读取根目录副本 |
| 已被取代的随从图鉴 | 2 | 40,976 | v5.1.0、v5.3.0 已由 v5.3.1 取代 |
| 早期废弃草稿 | 2 | 5,646,374 | 独立 UI 初稿及已有正式同目录版本的盾环傀儡草稿 |
| **合计** | **401** | **101,952,794** | 约 97.2 MiB |

## 明确保留

- `sc/Temp/phase9b-card-composite/`：虽然位于 Temp，但仍被
  `tools/compose_furnace_king_card.py` 和美术来源文档引用。
- `sc/Assets/Audio/**/Placeholder`、`placeholder_*` 配置 ID：仍属于当前运行时
  联调协议，名称为 Placeholder 不代表无效。
- `LightStorybookProductionV033Batch01`–`Batch06`、相关 Catalog、Builder、
  Calibration Prefab/Scene：仍是 Promotion Builder 和签字证据的输入。
- `ui-concepts` 中的冻结包、验证轮次、截图和 A/B 对照：属于审计证据，不按
  “版本号较旧”直接判定为垃圾。
- 当前 v0.3.3 背景更新及其他已有未提交改动：没有进入本次归档。

没有移动任何受版本控制的 C# Runtime/Editor 源码。审计未发现带有
`[Obsolete]`、明确废弃标记且同时无菜单、测试、构建器或序列化引用的代码；
仅归档了 `sc/Temp` 中独立、可重新生成的检查工程源码和编译产物。

## 恢复

按 `MOVE-MAP.csv` 将归档路径移回原路径即可。恢复前应先确认原路径不存在，
避免覆盖后续新增文件。压缩副本位于同级
`archive/deprecated-temp-20260731.zip`，其 SHA-256 保存在
`archive/deprecated-temp-20260731.zip.sha256`。
