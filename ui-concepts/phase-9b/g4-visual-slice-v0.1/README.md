# G4 正式视觉样板闭环 v0.1

- 日期：2026-07-27
- 当前生产 Prompt 风格基线：
  `style-tiles/style-tile-d-wandering-storybook-v0.1.png`
- 角色/材质参考：
  `ui-concepts/phase-9c/full-art-production-v0.1/masters/minions/forge-soul/cinder-armor-arbiter.png`
- 工具：Codex 内置 ImageGen；后端模型版本与种子不可获得
- 状态：正式效果候选，当前按 `工程样板` 管理；待 Unity G4-V 双分辨率复验和
  项目负责人生产许可签字，不得标记 `Runtime Ready`

## 资产清单

| Asset ID | Runtime 路径 | 像素 | SHA-256 |
| --- | --- | --- | --- |
| `backdrop_main_menu` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_main_menu.png` | 1672×941 | `22bf895f37af610bc19d2d5db7ef01e5cddb670dfbb18299836426e01e727dbd` |
| `backdrop_floor_map` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_floor_map.png` | 1881×836 | `3ca9619518b6ef76146aac7db7674bf8d33a4ca23d9ae1817acf083d5b22ab27` |
| `backdrop_shop` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_shop.png` | 1672×941 | `5568bca7cf58c6dc5bdd748028775d0322d61daa484b7408681d6502044f9095` |
| `event_tranquil_grove` | `sc/Assets/Resources/Presentation/Events/event_tranquil_grove.png` | 1448×1086 | `ce17a57d2e7219aa9a94fff9469198b1f3dd925a374da0ba7cd5252957193a73` |
| `backdrop_battle` | `sc/Assets/Resources/Presentation/Backdrops/backdrop_battle.png` | 1672×941 | `ee48fcaf2d4ff7e186c36d0e1b9150419e15c982f2c80af112efe7d63bca2d4e` |

## 构图验收目标

- 主菜单：尖塔位于右侧，左/中区域允许标题和菜单卡保持高对比。
- 地图：同一画面具有林地、商旅桥梁、熔岩门三个进程区；不烘焙节点、连线或文字。
- 商店：左右/上方提供环境叙事，中下方以低细节浅木与羊皮纸台面承载卡牌，
  不通过降低曝光制造留白。
- 事件：静谧林地具有唯一焦点；插画和选择项并排，不把图烘焙进 UI。
- 战斗：敌我两层平台清楚分离，五单位横排区域不放前景角色或文字。

完整提示词见 `PROMPTS.md`。原始 ImageGen 输出保留在 Codex 生成目录，Runtime 文件
为未经二次像素编辑的直接复制；Unity 仅通过 Sprite 导入和运行时颜色/透明度合成。
