# 阶段 9B G1 运行时视觉评审 v0.1

- 日期：2026-07-23
- 收口更新：2026-07-24
- Unity：2022.3.62f3c1
- 评审范围：共享 `PF_Card`、`PF_BattleStandee`、商店卡牌、战斗详情、合法目标、护盾与普通/金色对照
- 结论：运行时共同风格、8 张锚点盲测、信息可读性、金色表现、组件复用、批量生产成本与正式使用许可均通过；G1 退出门已关闭
- 成本/许可签字包：`phase-9b-g1-production-license-signoff-v0.1.md`

## 1. 本轮修改

- 以 `style-tile-wandering-storybook-v0.3.png` 为唯一风格基准，生成并接入共享普通/金色绘本卡框；暖纸、深墨、靛蓝缝线和局部金箔取代黑银厚金属语言。
- `CardView` 信息区统一为暖纸底/深墨字；流派色集中在名牌与插画蒙层，金色不改写流派主色。
- 战斗区普通立牌使用独立银灰/白镴框，金色立牌继续使用原暖金框与 Tint；同角色对照同时依赖冷暖、明度和材质区分。
- `PF_BattleStandee/TargetHighlight` 改为 `Sliced + fillCenter=false` 的细绿空心边线；取消整块青色填充。
- 护盾 Additive 根据实机反馈从 Alpha 0.46 提升至 0.78，覆盖由 124×211 扩大到 132×222，仍位于立牌框和数值圆章下方。
- 槽位与立牌根 `Image` 设为全透明但继续接收 Raycast；移除玩家/敌方红蓝矩形和占位底，只保留空槽编号、立牌本体与目标描边。
- 新增同角色普通/金色立牌双分辨率捕获，保留共享 Prefab、Catalog 与 Theme 路径，不增加单卡分支。

## 2. 证据

| 场景 | 证据 | 结论 |
| --- | --- | --- |
| Full / Compact / 普通 / 金色 / 状态覆盖 | `ui-concepts/unity-validation/pf-card-v0.1/` | 1080p、1200p 均完整显示名称、等级、正文、攻防与状态 |
| 商店商品、战斗区、手牌 | `ui-concepts/unity-validation/pf-shop-screen-v0.1/` | 五商品与 Compact 卡并排可读，公共框无单卡分支 |
| 五槽战斗、关键词、护盾、合法目标 | `ui-concepts/unity-validation/pf-battle-screen-v0.2/battle-screen-1920x1080.png` 与 `battle-screen-1920x1200.png` | 槽位背景已移除；空心目标边线不遮盖肖像；增强护盾清楚可见 |
| Hover/点击锁定详情 | `ui-concepts/unity-validation/pf-battle-screen-v0.2/battle-standee-detail-1920x1080.png` | 详情卡为绘本语言且不遮挡日志 |
| 普通/金色同角色 | `battle-standee-rarity-1920x1080.png` 与 `battle-standee-rarity-1920x1200.png` | 普通银灰与金色暖金可立即区分，铸魂红、肖像与攻防数字保持清楚 |
| 8 张锚点 5 秒盲测 | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/` 与 `archetype-card-blind-test-v0.2/` | 铸魂、荒灵、星契与旅团可凭轮廓、主色和形状语言区分；项目方确认无问题 |
| 6 张新增核心样板 | `ui-concepts/phase-9b/sample-minion-illustrations-v0.1/` | 1024×1536 master 与 160×240 缩略检查完成；仅为 G2 前置工程样板 |
| 4 个轻量数值组件 | `ui-concepts/phase-9b/card-components-number-tags-v0.2/` | Full、Compact 与四位数离线评审通过；当前 Unity 候选未运行验证 |

历史自动化证据（2026-07-23）：EditMode 267 / 267、PlayMode 22 / 22 通过。新增门禁验证普通/金色立牌使用不同 Sprite、目标边线 `fillCenter=false`、护盾 Alpha 0.75–0.82，以及透明槽位/立牌根仍保持 Raycast。该结果不包含 2026-07-24 的 8 张锚点/4 组件 G2 候选改动。

## 3. G1 评审矩阵

| 项目 | 结果 | 依据/剩余风险 |
| --- | --- | --- |
| 共同风格 | 通过 | 卡框与立牌均使用纸张、墨线、缝线、皮角和克制金箔；炉王插画与亡语蜡封保留威胁感 |
| 三流派 5 秒区分 | 通过 | 8 张锚点完成不依赖文字的立绘与实际卡面盲测；铸魂、荒灵、星契和旅团可凭轮廓、主色与形状语言区分 |
| 信息可读性 | 通过 | Full、Compact、Shop、Battle、详情和状态覆盖已在双分辨率截图与字体测试验证 |
| 金色表现 | 通过 | 同角色对照中普通为银灰/石墨，金色保持暖金；两者都未覆盖流派色与攻防数字 |
| 组件系统 | 通过 | 普通/金色分别使用 Catalog 中的共享立牌框 Sprite；卡牌与立牌分别单 Prefab；状态由 Catalog/Theme/ViewModel 参数驱动 |
| 批量生产成本与许可 | 通过 | 项目方于 2026-07-24 接受建议效率门槛，并确认个人版 OpenAI 服务、输入参考权利、输出非唯一性及 28 项活动生产候选的正式使用范围；人民币精确成本仍须在获得实际工时与账单后另行核算 |

因此勾选 G1 退出门。28 项活动生产候选的状态更新为 `生产许可已确认`；本结论不改变
第 4 节的 G2 边界，也不将任何本轮未验证候选标记为 `Runtime Ready`。

## 4. G1 / G2 边界

- 当前 8 张锚点与 4 个轻量组件写入 Catalog / `PF_Card` 的工作属于 G2 技术预接入，不作为 G1 退出证据。
- 当前工作树没有本轮 Unity 编译、EditMode、PlayMode 或双分辨率截图结果；未验证前不得标记为 `Runtime Ready`。
- 6 张新增核心样板在 G1 只完成 master、提示词和离线缩略检查；Runtime、Catalog、卡面与 Unity 验收留在 G2。
- 行脚医师与百技学徒仅为 G1 附加盲测锚点，不替换三主种族 12 张 G2 样板。

## 5. 生成与导出记录

- 模式：Codex 内置 imagegen（非 CLI）；后端具体模型版本与种子未暴露。
- 普通框最终提示词：以旧 `card_frame_normal.png` 为几何编辑目标、Style Tile v0.3 与首轮纸框为材质参考；只把金属像素改为暖象牙水彩纸、胡桃墨线、靛蓝缝线、皮角与克制叶/罗盘饰；严格保留三处开口和分隔位置；所有透明区使用均匀 `#FF00FF`，禁止文字、厚金属、哥特尖刺、阴影和不透明面板。
- 金色框最终提示词：以最终普通框为绝对几何；保留纸面与靛蓝布，只在细内线、罗盘/叶饰、铆钉和窄外缘增加不规则手刷金箔；严格保留所有开口与色键区域；禁止整框金属化、霓虹金、发光和新增装饰。
- 普通立牌银框提示词：以现有暖金 `standee_frame.png` 为编辑目标、Style Tile v0.3 为材质参考；严格保留拱形、内开口、底座、铆钉和像素对位，只把黄铜/木质改为冷银灰旧白镴、浅银磨损高光和石墨凹槽；禁止金色、木色、蓝色偏色、文字和新增装饰；透明区使用纯 `#FF00FF`。
- Alpha：公共卡框使用 imagegen 技能随附 `remove_chroma_key.py` 的边界自动色键参数；普通立牌银框使用固定 `#FF00FF`、容差 36、软边、1 px 收边/羽化与去溢色。
- 运行时导出与 SHA-256 见 `phase-9b-asset-source-ledger-v0.1.md`。
