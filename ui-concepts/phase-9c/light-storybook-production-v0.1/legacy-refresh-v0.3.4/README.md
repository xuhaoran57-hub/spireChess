# Legacy Card Art Refresh v0.3.4

- 日期：2026-08-01
- 范围：10 张随从、4 张法术、3 张 Token，共 17 张
- 状态：`PROMOTED / CLOSED`
- Runtime：已原位替换
- 配置卡图新风格精确覆盖：`83/83`

## 结论

v0.3.3 的 Catalog 精确解析门禁只证明 ArtId 能找到图片，没有证明图片来自冻结的
Light Storybook 新风格。排查后确认 83 个配置 ArtId 中有 17 个仍指向旧
`Assets/Art/Presentation/Cards/` 图片。

本批次用 14 张新生成图片和 3 张已确认的 Token v0.3.4 候选完成替换。Runtime
PNG 采用原位覆盖，因此 17 个 ArtId、图片路径、`.meta` GUID 以及正式
`PresentationSpriteCatalog.asset` GUID 均保持不变；横版新图焦点统一为
`focalPointY = 0.5`。

## 晋级范围

| 类型 | 数量 | 内容 |
| --- | ---: | --- |
| 随从 | 10 | 星盘校准师、裂甲复仇者、狐群巢母、百艺学徒、腐叶承嗣、秘页折光师、天穹契约者、星图掮客、回火修补匠、万蹄奔潮 |
| 法术 | 4 | 高阶发现、免费刷新、小型锻体、战前赐福 |
| Token | 3 | 幼灵、迅捷幼灵、双尾狐影 r3 |

双尾狐影只晋级 r3：实体严格为四腿、四爪、两条尾巴，两个尾根均位于后腿上方
骨盆区域。r1 的尾根构图错误和 r2 的五腿错误只保留在 Token 审计目录，不得用于
Runtime。

天穹契约者只晋级 r2：首轮因画出 5 个契约站被否决；r2 固定为恰好 4 个契约站。

## Runtime 策略

- 图片：约 5:4 横版、亮色水彩 / 水粉、无文字、UI 或卡框。
- 导入：Sprite / Single / Full Rect，100 PPU，无 mipmap，不可读。
- Standalone：1024、DXT1、压缩质量 50、非 Crunch。
- Catalog：17 个条目的焦点统一为 0.5。
- 正式 Catalog GUID：
  `75d638606a8084146524a35a317a2cca`。
- 晋级后 Catalog SHA-256：
  `02351e80415b86fc755f32389a4e459db027844cb3e8cd320bb07e21858ee1ea`。

## 风格来源门禁

`LightStorybookArtRefreshV034Builder` 不再以“Catalog 能 Exact 解析”作为风格
已更新的充分条件。它构造并验证下面的精确集合：

```text
v0.3.3 已批准 Runtime 清单 66
+ v0.3.4 本次刷新清单 17
= 配置中的全部 ArtId 83
```

门禁逐项检查 ArtId、批准路径、SHA-256、Catalog 绑定、焦点、图片 GUID 和
TextureImporter 策略。配置增加、路径漂移、回退到旧图或图片被静默替换都会失败。

## 审计文件

- `ART-REFRESH-MANIFEST-v0.3.4.json`：17 张批准来源、旧 Runtime 哈希和 GUID。
- `RUNTIME-PROMOTION-RESULT-v0.3.4.json`：Unity 晋级结果与 83/83 结论。
- `PROMPTS-v0.3.4.zh-CN.md`：14 张新图的最终 Prompt 集。
- `../token-refresh-v0.3.4/`：3 张 Token 及双尾狐 r1/r2 否决记录。

## 本次关闭验证

- Unity 幂等晋级复跑：`83/83` 两次通过。
- Token 离线审计：`PASS_RUNTIME_PROMOTED`。
- Unity EditMode：`385/385`。
- Unity PlayMode：`30/30`。
- Runtime 内容基线：`9c7f14ab0f36a05ecc203d2881837bc3c33f68ac`；
  验收前后工作树均为干净状态。
- Windows x64 Clean Development Player：Build ID
  `v034-formal-9c7f14a`，`cleanBuild = true`、`sourceTreeDirty = false`，
  可执行文件 SHA-256
  `fa01ccdbaa5f74c777609235b99ba8988285b2bf0754445e85bba268b2e61eb7`。
- 1920×1080 正式核心流程：`AcceptancePassed`，MainMenu → Run → Shop →
  Run → Battle → Run → MainMenu → Continue 完整闭环通过，生成 16 张截图。
- 1920×1200 正式核心流程：`AcceptancePassed`，相同完整闭环通过，生成
  16 张截图。
- 运行时全卡画廊：从正式 `PresentationSpriteCatalog.asset` 确定性生成
  9 页、83 张全卡；83 个配置 ArtId、83 个索引项和 83 个唯一 ArtId 精确一致，
  9 张页面 PNG 均存在且 SHA-256 与画廊索引一致。
- 人工逐页复核未发现阻断项：双尾狐影为四腿、四爪、两尾且尾根位于骨盆；
  万蹄奔潮为四腿；战前赐福为 5 个单位；天穹契约者为 4 枚契约盘；
  高阶发现为 3 个拱门、3 个角色和 3 个落点。

正式验收报告中的 `provisional = true` 仅对应尚未进入
`ProductionStrict` 的音频内存与听感结论；它不影响本次卡牌美术与 Runtime
替换封版，也不据此关闭正式音频或 G4 跨机器门禁。

本地证据由工具生成并按仓库规则不纳入 Git；以下哈希用于复核同一次验收：

| 证据 | SHA-256 |
| --- | --- |
| `sc/Builds/G4/v034-formal-9c7f14a/Windows-x64/g4-build-manifest.json` | `732f126b286b1fac6f7d4d1cb0799b4831905c00bf6c25f02a2a57deb53372fb` |
| `sc/Logs/G4/Acceptance/v034-formal-9c7f14a-core-1920x1080/g4-evidence-manifest.json` | `d8a73cd5ede7deab69c83b471b8ea409ab66b3fe8da7f9873b936609cb009323` |
| `sc/Logs/G4/Acceptance/v034-formal-9c7f14a-core-1920x1080/performance/g4-performance-v034-formal-9c7f14a-core-1920x1080-1920x1080.json` | `87a3846cee6492bf877257ec115a6c2690711939f949c487503ea3c1caa096b2` |
| `sc/Logs/G4/Acceptance/v034-formal-9c7f14a-core-1920x1200/g4-evidence-manifest.json` | `27efecf6756718620e61d03012f772b5f5af1e3324cc79248013192fd5237ad4` |
| `sc/Logs/G4/Acceptance/v034-formal-9c7f14a-core-1920x1200/performance/g4-performance-v034-formal-9c7f14a-core-1920x1200-1920x1200.json` | `29b6553dad9f035686d2ef8ba84a42d7363d7610869214eb80965dfee88c8ea2` |
| `sc/Logs/Phase9C/ArtRefresh/v0.3.4/runtime-gallery-9c7f14a-formal/gallery/gallery-index.json` | `cc478defbaa2e18a09d6e17f2e4ef5823ca91c6e7804f1423c45c6ca5c891226` |

## 复现与验证

执行幂等晋级和 Unity 内 83/83 门禁：

```powershell
.\tools\run_phase9c_v034_art_refresh.ps1
```

运行 Unity 全量测试：

```powershell
.\tools\run_unity_tests.ps1 -Platform All
```
