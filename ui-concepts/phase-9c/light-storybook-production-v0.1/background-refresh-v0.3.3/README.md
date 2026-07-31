# v0.3.3 明亮绘本背景更新

- 生成日期：2026-07-31
- 生成方式：Codex 内置 ImageGen；每张图独立生成
- 唯一图像参考：
  `../../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`
- 冻结规则：
  `../freeze-v0.3.3/BRIGHT-STYLE-RULES-v0.3.3.md`
- 生产 Prompt 基线：
  `../freeze-v0.3.3/PRODUCTION-PROMPTS-v0.3.3.zh-CN.md`
- 当前状态：`Runtime Connected / Pending Unity G4-V`

本批次按 v0.3.3 明亮旅行绘本规范重做主菜单、商店、楼层地图、战斗和静谧林地
事件五张视觉资产。五张图片均原路径替换，现有 `.meta`、GUID、Resources 路径、
事件 `artId` 和代码接线均保持不变。

这次替换只完成生成、人工构图初筛和离线文件门禁，不等于 `Runtime Ready`。
仍需在 Unity 2022.3.62f3c1 中重新导入并完成 EditMode、PlayMode、Clean Player
以及 1920×1080 / 1920×1200 的 G4-V 五画面视觉复核和负责人签字。

## 资产结果

亮度统计先将图片缩放到 160×128，再按 Rec.709 luma 计算。`亮/中亮` 表示
`luma >= 85`，`近黑` 表示 `luma < 25`。

| Asset ID | Runtime 路径 | 像素 | 亮/中亮 | 近黑 | SHA-256 |
| --- | --- | ---: | ---: | ---: | --- |
| `backdrop_main_menu` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_main_menu.png` | 1672×941 | 98.54% | 0.00% | `7a77c9d196b5fbe61943b7e908d4b05aa5bc24a77782680a4e3e10430eb79d7b` |
| `backdrop_shop` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_shop.png` | 1672×941 | 95.77% | 0.00% | `d3931517c12b7f3af33fb523fab0e6e4a427d232030791b840c2588a96b90a22` |
| `backdrop_floor_map` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_floor_map.png` | 1672×941 | 99.38% | 0.00% | `7fdd4f205a3b119718f9f5977a58a1c82ded1cb06d3a1502cebf0b15ac2723a0` |
| `backdrop_battle` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_battle.png` | 1672×941 | 95.57% | 0.00% | `51f913ca2793512063c8863f2d9af31a5a2ffda03adfe27b40e2bf9ea35b080a` |
| `event_tranquil_grove` | `sc/Assets/Resources/Presentation/Events/event_tranquil_grove.png` | 1448×1086 | 96.78% | 0.00% | `adcb71e599c541a4b68e32f1c2d7bd4f6a2591c569b8c6d5a018f750e9f77d70` |

五张普通场景均超过冻结建议的 60% 亮/中亮门槛，并低于 8% 近黑上限。旧版同口径
亮/中亮为 5.12%–31.32%，其中主菜单、商店和事件近黑分别为 22.09%、22.63%
和 40.21%，因此旧版不再作为当前明亮主题视觉基线。

## 构图结论

- 主菜单：旅行尖塔车队位于右侧，左侧和中部保留连续浅天空与山坡。
- 商店：叙事物件收在四周和上部，中下部保留明亮、低细节柜台。
- 地图：按林地、河桥、花草地、浅岩高地和山村组织左至右旅程；中部不烘焙
  路线、节点或文字。
- 战斗：上方锈红敌方石台与下方蓝绿我方木台清楚分离，两个五单位横排区域
  均无角色、槽位或文字。
- 事件：白昼树泉为唯一焦点，旅行者、铜铃、缎带和骑士像均保持次要。

完整生成文本见 `PROMPTS-v0.3.3.md`，机器可读记录见
`ASSET-MANIFEST-v0.3.3.json`。
