# Round 12 五级随从量产 Prompt v0.3.3

- 日期：2026-07-30
- 工具：Codex 内置 ImageGen
- 模式：逐张生成；终花吞世者在冻结规则复核后整张重生成
- 唯一图像参考：
  `../../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`
- 冻结规则：`../freeze-v0.3.3/`
- 输出：本目录 `cards/`

## 共同 Prompt

每张图均使用以下共同约束，并叠加单卡变量：

> Use case: stylized-concept. Asset type: Unity card-game tier-5 minion
> illustration master, no card frame. Image 1 is the sole style reference.
> Use only its bright wandering-storybook watercolor/gouache medium, warm
> ivory paper, colored ink outlines, open daylight, and palette. Do not copy
> any character, object, text, UI, or composition. About 5:4 landscape; one
> main subject occupies 50–70%; important content stays inside the central
> 80%; the outer 7% carries no essential details. Use diffuse bright daylight;
> bright and mid-tone pixels dominate and near-black is minimal. No dark
> scene, black vignette, card frame, UI, readable text, letters, numbers,
> plus signs, arrows, runes, logo, signature, watermark, or extra major
> characters.

阵营补充：

- 铸魂必须是承载灵魂光的连贯手工铠甲、武器或机械，无穿戴者与异常肢体。
- 星契保持清楚的人形职业身份，并同时出现至少两种观星、知识或契约线索。
- 旅团保持清楚的人类职业身份，以实用装备、行动和队形表达机制。
- 荒灵主体由单卡身份决定，可以是可信动物、类人守林者、山野精怪或祖灵；身份
  来自山林、兽魂和生命循环关系，不使用全族统一身体模板。

## 单卡变量

### 断誓刃魂

一把破损巨刃、暖色灵魂火与紧凑空甲环组成无穿戴者的铸魂机械。左侧浅蓝护盾正在
破裂并将能量注入刃缘；低细节训练傀儡化为暖光后，刃后重新形成完整护盾。

### 千环守墓者

低重心四足墓园守卫机械，暖色灵魂灯被多层黄铜同心环保护，正面厚甲表达嘲讽。
灯体裂为温和亡语光，恰好两个小型存活友方获得完整浅蓝护盾，低位暖光波覆盖其他
低细节存活友方。

### 陨星先知

日照观星庭院中的人类先知学者，操作黄铜陨星仪。恰好两张完全无字的法术纸化为
两道星光，分别强化恰好两个装备朴素、攻击力较低的人类星契友方。

### 命运洗牌师

人类星契交易师在明亮观星市集转动三段式黄铜洗牌轮，轮上恰好三个无字节拍点。
三张完全无字的半透明人形星契候选片展开，其中一张被浅蓝光选中；手心放一枚无字
金币。首次调用遇到网络错误，随后以内置 ImageGen 和相同 Prompt 成功重试。

### 王庭赏金客

装备实用弩具、绳索与无字赏金牌的人类旅团猎手。战斗开始时发出覆盖远处全部敌方
剪影的第一层金色压力波；恰好三个同种族敌人聚成一组，并被第二层更亮的重叠波
重点覆盖。

### 终花吞世者

一株宏大的远古终末花灵与山野生命现象，明确不是动物也不是人形。主体由象牙白、
浅金、珊瑚和深青色层叠花瓣、明亮的花粉与雄蕊旋涡，以及古老根系、藤蔓和覆苔
土地构成；根系形成连贯的生命神龛结构，但不得模仿动物腿、爪、嘴、眼睛或面孔。

多个已死亡、非召唤物友方荒灵以小型动物或祖灵形生命回声消散为暖色光流，被吸入
花心旋涡；每道回声都使雄蕊更强烈地亮起，表达获得死亡友方当前攻击力。最先恰好
两道生命回声额外凝结为恰好两个体量较大、彼此分离的琥珀种核，对称嵌入根冠，
表达每场前两次永久成长；不得出现第三个种核或重复的花形计数标记。

场景为明亮开放的山林空地，浅蓝天空、象牙色岩石和远处绿色山脊。主体占画面
55%–70%，完整根冠与两个种核均位于中央安全区。整体古老、壮丽而非恐怖，不出现
食人花口器、触手怪物、动物解剖、人脸、肢体、尸体、血腥或黑暗氛围。

生成修订记录：初版因额外加入“必须采用自然动物骨骼”而被错误设计为巨型食蚁兽；
复核冻结规则后确认荒灵主体应由单卡身份决定，因此废弃动物候选并使用上述 Prompt
整张重生成。新版本仍只使用冻结 Style Tile 作为图像参考。
