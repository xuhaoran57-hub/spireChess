# Battle Keyword Assets v0.1

本批次只验证“生成式美术原件能否替代程序绘制占位图”，不代表最终尺寸与布局已经冻结。

## 内容

- `bookmarks/bookmark-taunt.png`：嘲讽，红色织物盾章书签，透明背景。
- `bookmarks/bookmark-deathrattle.png`：亡语，裂蜡封与裂钟书签，透明背景。
- `bookmarks/bookmark-splash.png`：溅射，青色分叉刃书签，透明背景。
- `bookmarks/bookmark-overflow-blank.png`：`+N` 溢出页签，无文字透明原件；数字由运行时排版。
- `shield/shield-vfx-atlas-3x2-screen.png`：护盾六帧原始图集，纯黑背景。
- `shield/frames/`：按 3×2 图集拆出的六张 512×768 关键姿势。
- `events/deathrattle/deathrattle-event-atlas-3x2-screen.png`：亡语裂蜡封六帧事件图集。
- `events/deathrattle/frames/`：完整、初裂、扩裂、崩开、脱落、触发环六张拆帧。
- `events/splash/splash-event-atlas-3x2-screen.png`：溅射书签展开与分支命中六帧事件图集。
- `events/splash/frames/`：收拢、展开、就绪、主命中、分支命中、涟漪消退六张拆帧。
- `generated-sources/`：四枚书签的原始色键背景图，保留以便重新抠图。

## 合成规则

- 书签使用普通 Alpha 混合，不在商店卡与手牌卡显示，只挂在战斗区卡牌右侧。
- 单至三关键词直接堆叠；第四个位置改放 `+N` 页签。顺序固定为：嘲讽、护盾、亡语、溅射、其他。
- 护盾不是书签：覆盖卡牌立绘与边框内侧，使用 `Screen` 或 `Additive` 混合。
- 护盾六帧顺序：水彩绘入、完整、抵挡飞溅、初始裂纹、裂纹扩散、碎片消散。
- 亡语事件挂在死亡单位中心：封印裂开后，最后一帧触发环保留约 120–180ms，再执行亡语结果表现。
- 溅射事件以主命中点为原点：第五帧的两条支线由界面运行时根据实际次目标位置拉伸，生成图只提供材质与运动参考。
- 生成原件需要先在真实 160×240 战斗卡上进行减法简化和尺寸验证，不能直接按大图观感定稿。

## 生成与处理

原件通过内置图像生成工具按 gpt-image-2 资产提示规范生成。四枚书签分别生成于纯绿或纯洋红色键背景，再使用 imagegen skill 提供的 `remove_chroma_key.py` 进行软边、去溢色抠图；护盾保留黑底用于加色混合。
