# Light Wandering Storybook Production Rules v0.3

首次建立：2026-07-28

v0.3 更新：2026-07-29

状态：已冻结的明亮绘本生产规则；A/B 验证期间不替换正式 Runtime 资源。

## v0.3.3 冻结包

`freeze-v0.3.3/` 已收束现行风格规则、生产 Prompt、视觉基线、离线校验脚本和
Unity 交接清单。Batch 01–06 已完成 Unity 2022.3.62f3c1 批次验收，当前状态为
`UNITY_BATCH_RELEASE`；候选仍保持隔离，未提升到 Runtime Catalog。

## 唯一共同风格参考

`../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`

后续 Style Tile、铸魂成品、Boss 成品和暗色场景不得作为共同色板、光照、
明暗或媒介参考。它们只能提供角色身份、玩法语义或构图信息。

## 冻结的全局视觉规则

1. 默认采用晨间至下午的漫射日光。
2. 每张画面至少 50% 为亮部或中亮部；普通场景建议达到 60%。
3. 近黑面积不超过约 12%；普通场景建议不超过 8%。
4. 阴影必须保留蓝灰、青绿、紫灰或暖褐色相，不得压成纯黑。
5. 背景可以比主体低一个对比层级，但不得通过整体降低曝光实现。
6. 火焰、魔法与灯具只能作为局部强调，不能成为普通场景的主光源。
7. 禁止黑色暗角、全局棕黑调色、烟尘天空和以暗区承载 UI 留白。
8. Boss、亡语与危险事件通过尺度、姿态、轮廓和局部色彩表达威胁，
   不得改写全局明亮绘本基调。

## 铸魂种族设定

铸魂是由炉火灵性驱动的铸造生命。其文化与视觉语义围绕锻造、炉火、盾甲、
修复、誓约和手工金属展开；具体身体结构不作全族硬限制。

“三爪锻造夹”“空心躯壳”“断开的甲片”“固定炉栅”等只能作为单个角色的
可选设计，不是种族共同规则。角色可以采用完整手掌、连贯甲体或其他符合身份、
阶级、能力和叙事的结构。

## A/B 资源

- `ab-production-v0.1/forge-card-old-dark.png`
- `ab-production-v0.1/forge-card-new-light.png`
- `ab-production-v0.1/battle-backdrop-old-dark.png`
- `ab-production-v0.1/battle-backdrop-new-light.png`

## Unity A/B

执行菜单 `Spire Chess/UI/Build Light Storybook A-B Scene` 后生成：

- `PresentationTheme_LightStorybook.asset`
- `PresentationSpriteCatalog_LightStorybook.asset`
- 独立的 Light Storybook 战斗 Prefab
- `BattleLightStorybookAB.unity`

这些资产不覆盖现有 Theme、Catalog、正式 Prefab 或正式场景。

A 组继续使用现有 `Assets/Scenes/BattleTest.unity`；B 组使用新增的
`Assets/Scenes/Calibration/BattleLightStorybookAB.unity`。两组复用同一个
`BattleTestController` 和相同测试数据，只改变 Theme、炉王插画与战斗背景。

## 多界面 A/B

执行菜单 `Spire Chess/UI/Build Light Storybook Multi-Screen A-B`，在保留上述
战斗 A/B 的同时生成：

- `MainMenuLightStorybookAB.unity`
- `ShopLightStorybookAB.unity`
- `RunLightStorybookAB.unity`

对应 A 组分别为现有 `MainMenu.unity`、`ShopTest.unity` 和 `RunTest.unity`。
B 组继续复用各自原有测试控制器与数据。

主菜单和商店的旧版颜色大量硬编码，A/B 构建器只在独立 Prefab 副本上将暗色
无贴图表面、文字和边线确定性映射到 Light Theme。地图使用 Light Theme 的原生
引用。旧的主菜单、商店和地图生产背景在 B 组中关闭，先单独验证 UI 色板、弹窗、
状态和可读性；新的正式明亮背景通过后再接入，避免把背景插画差异误判为 Theme
差异。

## 第二轮小批量样本

`validation-round-2/` 保存基于迁移后生产规则重新生成的六张样本：

- 每个种族一张卡面：铸魂、荒灵、星契；
- 三张背景：主菜单、商店、楼层地图；
- `validation-round-2/VALIDATION.md` 记录亮度统计、人工复核、结论与 SHA-256。

本轮结论为“小批量风格验证通过，暂不覆盖 Runtime”。下一步是在多界面 A/B
副本中叠加真实 UI，检查标题、按钮、卡牌、路线节点和状态图标的可读性。

## Round 3：单风格参考

`validation-round-3-style-only/` 是去除现有随从硬约束和全部内容参考图后的独立
对照批次。六次生成都只输入冻结 style tile：

- 三张卡面只保留铸魂、荒灵、星契的宽泛种族前提；
- 三张背景只保留主菜单、商店、地图的界面用途和 UI 留白要求；
- 不使用现有随从、角色锚点、旧背景或 Round 2 图片。

完整 Prompt 和验证结论分别记录在 `PROMPTS.md` 与 `VALIDATION.md`。

## Round 4：种族语义校准

`validation-round-4-race-aligned/` 按最新种族设定重新生成铸魂、荒灵和星契卡面：

- 只使用冻结 style tile 作为视觉输入；
- 使用最新的种族主题、偏好关键词和主要联动作为内容语义；
- 不使用现有随从图片，也不恢复全族身体结构、指定道具数量或固定镜头模板。

中文版提示词和验证记录位于 `PROMPTS.zh-CN.md` 与 `VALIDATION.md`。

## 种族规则 v0.3

`RACE-VISUAL-RULES-v0.3.md` 提供全局、铸魂与荒灵基础规则；
`RACE-VISUAL-RULES-v0.3.1.md` 是现行星契规则。v0.3 在 v0.2 基础上增加
主体—背景局部分离门槛，v0.3.1 进一步强化星契的星海知识与命运契约身份。

- 荒灵通过山林、兽魂与生命循环关系识别，不要求主体由树叶、山石、藤蔓或其他
  自然材料组成；
- 星契是观星、术法、学派与契约构成的文化/职业阵营，常规卡默认使用身份清楚的
  人形或亲和型类人角色，不以异形化制造轮廓多样性。

`CALIBRATION-PROMPTS-v0.3.zh-CN.md` 定义本轮六张校准样本：三张星契身份
重校准和三张荒灵身体自由度校准。

## Round 5：v0.3 校准结果

`validation-round-5-v0.3-calibration/` 保存六张新校准图：

- 星契：微光契术师、月轮寻秘者、命约回收师；
- 荒灵：山门守望者、林隙引魂人、归羽祖灵。

六张均为 1402×1122、5:4，自动亮度与单图人工校准通过。星契恢复为清楚的职业
角色，荒灵不再使用统一身体材质；下一步进入隔离 Unity 普通/金色卡框验证。

## Round 6：v0.3.1 正式卡池验证

`validation-round-6-v0.3.1-formal-pool/` 直接使用正式配置中的六名随从：

- 星契：微光术士、命轨记录员、月轮调度者；
- 荒灵：幼鹿灵、归根引魂者、群山古灵。

六张只使用冻结 Style Tile 作为视觉参考。星契按 v0.3.1 改为身份清楚的人形
观测者、学者和施法者，并通过外部星图、棱镜书页、月轮与命运轨迹表达机制；
荒灵分别保留正常幼鹿、年长类人引魂者与雪豹形体。

画幅与自动亮度门槛全部通过，逐卡视觉验收记录见
`validation-round-6-v0.3.1-formal-pool/VALIDATION.md`。本批次可进入隔离卡框缩略图
验证，但仍不覆盖 Runtime 正式资源。

## Round 7：四族与法术隔离 Catalog

`validation-round-7-v0.3.2-formal-catalog/` 保存 15 张新生成的正式配置验证图：

- 铸魂：铸魂盾侍、共鸣钟卫、不熄炉王；
- 荒灵：幼鹿灵、归根引魂者、群山古灵；
- 星契：微光术士、命轨记录员、月轮调度者；
- 旅团：行脚医师、旧塔向导、镜钢决斗家；
- 法术：临时护符、星辉回款、传说招募。

`PRODUCTION-RULES-v0.3.2.md` 补充旅团与法术规则。15 张只使用冻结 Style Tile
作为图像参考，画幅和自动亮度门槛全部通过。正式配置快照、中文版 Prompt 与逐张
验收分别位于：

- `FORMAL-CATALOG-SPECS-v0.3.2.json`
- `FORMAL-CATALOG-PROMPTS-v0.3.2.zh-CN.md`
- `validation-round-7-v0.3.2-formal-catalog/VALIDATION.md`

图片副本已准备在 Unity 隔离资源目录
`Assets/Art/Presentation/Calibration/LightStorybookFormalCatalogV032/`。
执行菜单 `Spire Chess/UI/Build Light Storybook Formal Catalog v0.3.2` 后生成
独立 Catalog、卡牌 Prefab 和五列验证场景；不会修改 Runtime Catalog 或正式
Prefab。当前机器没有项目指定的 Unity Editor，因此本轮只完成资源准备、构建器与
静态验证，Unity 序列化产物需在有 Unity 2022.3.62f3c1 的机器上生成。

## Round 8：v0.3.3 一级随从量产

`PRODUCTION-MANIFEST-v0.3.3.json` 以 v0.3.2 隔离 Formal Catalog 为覆盖基线，
从当前 67 张随从与 16 张法术配置中精确列出剩余 51 项：42 张随从与 9 张法术。
清单固定为一级 7、二级 11、三级 7、四级 11、五级 6、法术 9 六个批次，
并记录配置、Style Tile、生产 Prompt、基线 Catalog 哈希和逐项状态。

首批 7 张一级随从已使用 v0.3.3 冻结规则生成并接入新的隔离 Catalog：

- 铸魂：铜环学徒、炉心火种；
- 荒灵：裂爪幼兽、苔痕守苗；
- 星契：星尘随侍、观星学徒；
- 旅团：流浪剑客。

大图、亮度、哈希、Unity 副本和隔离 Catalog 绑定离线门禁通过；新 ArtId 未进入
v0.3.2 冻结 Catalog 或正式 Runtime Catalog。Unity 批次状态为
`UNITY_BATCH_RELEASE`，完整记录见
`validation-round-8-v0.3.3-tier1-production/`。

## Round 9：v0.3.3 二级随从量产

第二批 11 张二级随从已使用同一 v0.3.3 冻结规则生成：

- 铸魂：余烬刻师、破盾刃胚、盾墙执炉者；
- 星契：月相抄写员、符文护读者、星刻计时员；
- 旅团：黑市小贩、雇佣盾手；
- 荒灵：根须吞噬者、疾羽林隼、双尾狐灵。

Batch 02 隔离 Catalog 以 Batch 01 为基线累计扩展，总计 53 个条目；本批 11 个
ArtId 未写入 Batch 01、v0.3.2 冻结 Catalog 或正式 Runtime Catalog。当批完成时
量产清单为 18 项已生成、33 项待生成。大图、亮度、哈希、Unity 副本和 Catalog GUID
离线门禁通过，最终 Unity 批次状态为 `UNITY_BATCH_RELEASE`，完整记录见
`validation-round-9-v0.3.3-tier2-production/`。

## Round 10：v0.3.3 三级随从量产

第三批 7 张三级随从已使用同一 v0.3.3 冻结规则生成：

- 铸魂：逆流铸师、熔核执旗手、誓刃甲胄；
- 星契：回响咏星师；
- 荒灵：古苔巨幼体、群枝唤灵者、獠牙领奔者。

Batch 03 隔离 Catalog 以 Batch 02 为基线累计扩展，总计 60 个条目；本批 7 个
ArtId 未写入 Batch 02、v0.3.2 冻结 Catalog 或正式 Runtime Catalog。当批完成时
量产清单为 25 项已生成、26 项待生成。大图、亮度、哈希、Unity 副本和 Catalog GUID
离线门禁通过，最终 Unity 批次状态为 `UNITY_BATCH_RELEASE`，完整记录见
`validation-round-10-v0.3.3-tier3-production/`。

## Round 11：v0.3.3 四级随从量产

第四批 11 张四级随从已使用同一 v0.3.3 冻结规则生成：

- 铸魂：烬甲裁决者、炉心圣盾官、鸣铁堡垒；
- 星契：陨光裁定者、星环司库、星门讲师；
- 旅团：破阵佣兵、猎群监察官；
- 荒灵：百鸣兽群、山腹吞灵者、藤冠祭司。

Batch 04 隔离 Catalog 以 Batch 03 为基线累计扩展，总计 71 个条目；本批 11 个
ArtId 未写入 Batch 03、v0.3.2 冻结 Catalog 或正式 Runtime Catalog。当批完成时
量产清单为 36 项已生成、15 项待生成。大图、亮度、哈希、Unity 副本和 Catalog GUID
离线门禁通过，最终 Unity 批次状态为 `UNITY_BATCH_RELEASE`，完整记录见
`validation-round-11-v0.3.3-tier4-production/`。

## Round 12：v0.3.3 五级随从量产

第五批 6 张五级随从已使用同一 v0.3.3 冻结规则生成：

- 铸魂：断誓刃魂、千环守墓者；
- 星契：陨星先知、命运洗牌师；
- 旅团：王庭赏金客；
- 荒灵：终花吞世者。

Batch 05 隔离 Catalog 以 Batch 04 为基线累计扩展，总计 77 个条目；本批 6 个
ArtId 未写入 Batch 04、v0.3.2 冻结 Catalog 或正式 Runtime Catalog。当批完成时
量产清单为 42 项已生成、9 项待生成，剩余内容全部为法术。大图、亮度、哈希、Unity
副本和 Catalog GUID 离线门禁通过，最终 Unity 批次状态为 `UNITY_BATCH_RELEASE`，
完整记录见 `validation-round-12-v0.3.3-tier5-production/`。

## Round 13：v0.3.3 法术量产

第六批也是最后一批，共 9 张法术，已使用同一 v0.3.3 冻结规则生成：

- 一级：应急补给、三连发现；
- 二级：精准训练、厚皮药剂；
- 三级：复制雏形、战团锻造；
- 四级：血脉觉醒；
- 五级：全军升格、命运重铸。

Batch 06 隔离 Catalog 以 Batch 05 为基线累计扩展，总计 86 个条目；本批 9 个
ArtId 未写入 Batch 05、v0.3.2 冻结 Catalog 或正式 Runtime Catalog。剩余 51 项
量产清单当前为 51 项已生成、0 项待生成。大图、亮度、哈希、Unity 副本和 Catalog
GUID 离线门禁通过，最终 Unity 批次状态为 `UNITY_BATCH_RELEASE`，完整记录见
`validation-round-13-v0.3.3-spell-production/`。

## Unity Batch 01–06 放行

`unity-batch-release-v0.3.3/` 保存最终 Batch 06 隔离候选的 Unity 放行证据：
86 个 Catalog 条目、83 个配置 ArtId Exact、51 / 51 量产图、42 张
1920×1080/1920×1200 截图、EditMode 373 / 373、PlayMode 30 / 30，以及
Runtime/Formal Catalog 和正式 UI Prefab 的 10 / 10 前后哈希一致。人工视觉
复核覆盖普通/金色 × Full/Compact、法术、商店与 5v5 战斗立牌，结论为
`UNITY_BATCH_RELEASE`。本轮不执行 Runtime 提升。

## v0.2 机制压力测试归档

`mechanic-stress-test-v0.1/` 使用 v0.2 生成九张校准卡。自动画幅与整体亮度检查
通过，但人工复核发现主体—背景分离不足，并且星契出现无设定依据的异形化，因此
该批次不得作为量产通过样本。

执行 Unity 菜单
`Spire Chess/UI/Build and Capture Light Storybook Card Stress A-B` 可将九张
校准图复制到隔离资源目录，生成独立 Catalog、卡牌 Prefab 和校准场景，并输出
紧凑普通/金色卡与完整普通卡截图。详细门槛、亮度结果和当前运行状态记录在
`mechanic-stress-test-v0.1/VALIDATION.md`。
