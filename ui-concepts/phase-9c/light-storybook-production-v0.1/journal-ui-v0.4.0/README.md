# v0.4.0 日记式 UI 正式视觉素材

- 生成日期：2026-08-03
- 状态：`GENERATED_CANDIDATE_UNITY_IMPORT_PENDING`
- Runtime 目录：`sc/Assets/Resources/Presentation/Journal/`
- 唯一图片参考：冻结的明亮旅行绘本 Style Tile
  `ui-concepts/phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`

这批素材服务于 v0.4.0 的封面 → 目录 → 角色选择 → 地图、章节完成页与结局页。
它们只替换 UI 视觉槽位；角色被动、章节推进、存档与解锁领域逻辑保持既有实现。

## 视觉与生产合同

每张图只输入上述 Style Tile，且仅继承媒介、纸纹、线条、色板与曝光，不复制其人物、
物件、文字或构图。共同要求：

- 明亮旅行绘本：水彩/水粉、彩色胡桃墨线、暖象牙纸、开放白昼；
- 亮部与中亮部为主，保留外置 UI 文案的低细节安全区；
- 不生成图内文字、数字、Logo、水印、卡框或 UI 控件；
- 不使用夜景、暗角、黑色留白、烟尘或炉火主照明。

完整文件尺寸、哈希、资源 ID 与导入合同见
[ASSET-MANIFEST-v0.4.0.json](ASSET-MANIFEST-v0.4.0.json)，共同 Prompt 与每项
语义变量见 [PROMPTS-v0.4.0.zh-CN.md](PROMPTS-v0.4.0.zh-CN.md)。

## 资源清单

| UI 槽位 | Runtime Sprite | 用途 |
| --- | --- | --- |
| 封面 | `journal_cover_v0_4_0` | 封面页主插画，标题与按钮由 UI 渲染。 |
| 目录 | `journal_contents_v0_4_0` | 目录页的低细节纸本背景。 |
| 战士 | `journal_hero_warrior_v0_4_0` | 已解锁战士角色卡。 |
| 法师 | `journal_hero_mage_v0_4_0` | 已解锁法师角色卡。 |
| 盗贼 | `journal_hero_rogue_v0_4_0` | 已解锁盗贼角色卡。 |
| 锁定角色 | `journal_hero_locked_v0_4_0` | 未解锁角色卡的非身份化旅行者剪影。 |
| 荒野 | `journal_chapter_wilderness_v0_4_0` | 首章地图转场与章节完成页。 |
| 星轨高原 | `journal_chapter_startrail_highlands_v0_4_0` | 第二章完成页。 |
| 铸魂熔城 | `journal_chapter_soulforge_city_v0_4_0` | 终章完成页。 |
| 结局 | `journal_ending_v0_4_0` | RunWon 结局页。 |

`PresentationArtworkResources` 是唯一运行时加载入口；未知角色或地图 ID 会返回
`null`，页面保留旧的中性占位回退，避免资源缺失改变可玩流程。

## 验证与放行边界

离线一致性校验：

```powershell
python ui-concepts/phase-9c/light-storybook-production-v0.1/journal-ui-v0.4.0/validate_journal_assets.py
```

它验证文件存在、PNG 尺寸、SHA-256、Sprite `.meta` 合同与唯一 Style Tile 记录，
输出 `PASS_OFFLINE_UNITY_PENDING`。它不能替代 Unity 导入、EditMode/PlayMode、
1920×1080 / 1920×1200 真机截图，或人工视觉签字。

本次离线结果为
[JOURNAL-ASSET-VALIDATION-REPORT.json](JOURNAL-ASSET-VALIDATION-REPORT.json) 的
`PASS_OFFLINE_UNITY_PENDING`。

正式 Player 证据仍必须按
[`ui-concepts/unity-validation/v0.4.0-journal-ui/README.md`](../../../unity-validation/v0.4.0-journal-ui/README.md)
写入独立、干净提交对应的证据包；当前工作树的素材生成与离线核对不可伪装为 Player 放行。
