# Round 11 四级随从量产 Prompt v0.3.3

- 日期：2026-07-30
- 工具：Codex 内置 ImageGen
- 模式：逐张生成；仅对局部偏差做定点编辑
- 唯一图像参考：
  `../../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`
- 冻结规则：`../freeze-v0.3.3/`
- 输出：本目录 `cards/`

## 共同 Prompt

每张图均使用以下共同约束，并叠加单卡变量：

> Use case: stylized-concept. Asset type: Unity card-game minion illustration
> master, no card frame. Image 1 is the sole style reference. Use only its
> bright wandering-storybook watercolor/gouache medium, warm ivory paper,
> colored ink outlines, open daylight, and palette. Do not copy any character,
> object, text, UI, or composition. About 5:4 landscape; one main subject
> occupies 50–70%; important content stays inside the central 80%; the outer
> 7% carries no essential details. Use diffuse bright daylight; bright and
> mid-tone pixels dominate and near-black is minimal. No dark scene, black
> vignette, card frame, UI, readable text, letters, numbers, plus signs,
> arrows, runes, logo, signature, watermark, or extra major characters.

阵营补充：

- 铸魂必须是承载灵魂光的连贯手工铠甲、工具或机械，不使用异常肢体。
- 星契保持清楚的人形职业身份，并同时出现至少两种观星、知识或契约线索。
- 旅团保持清楚的人类职业身份，以实用装备、行动和队形表达机制。
- 荒灵保留自然动物骨骼，不默认将身体做成植物、岩石或人形祭司。

## 单卡变量

### 烬甲裁决者

无穿戴者的非人形裁决铠甲，中央暖色灵魂炉心由一圈完整浅蓝护盾保护。护盾一侧
正在裂散，余烬沿肩甲和裁决锤点亮；三枚无字金属节拍片表达第三次友方失盾后的
永久成长。

### 炉心圣盾官

宽肩、低重心的非人形炉心圣盾机械，胸腔暖炉通过导管把完整浅蓝护盾投向画面最左
的小型友方铸魂。远处一名友方铸魂化为余烬，能量回流并强化另一名友方铸魂。

### 鸣铁堡垒

钟形铁堡与厚重锻造腿构成的非人形铸魂堡垒，正面完整护盾表达嘲讽。左右各一名
低细节友方接受钟鸣暖光，明确表达战吼同时永久强化相邻随从；所有旗片和甲面无字。

### 陨光裁定者

日照观星台上的人类裁定者，以黄铜星盘、两张无字法术薄片和两道陨光轨迹记录本
商店阶段用法术的次数。第二道陨光在武器边缘形成宽阔溅射弧，不出现文字或数字。

### 星环司库

人类星契司库操作四段式黄铜星环刷新机构，旁边是一枚无字金币状筹码与封闭账匣。
第四段星环点亮后，金币落入掌中，同时一圈浅蓝护盾投向低细节友方星契。

### 星门讲师

明亮学院庭院中的人类讲师，站在开放黄铜星门、无字星图和三张空白法术薄片之间。
讲师从三张候选中展示一张，另一圈浅蓝护盾在其身侧形成，表达发现法术与用法术后
获得护盾。

### 破阵佣兵

实用装备的人类重装佣兵在攻击前用钩锤击碎目标的浅蓝护盾，破盾动作发生在武器
真正命中目标之前。背景只保留低细节队形，不使用纹章、文字或阵营旗号。

### 猎群监察官

人类监察官在明亮营地观察敌方召唤轨迹，手持无字机械计数栏；栏上恰好三枚清楚
的圆形计数珠依次点亮。第一次点亮在武器外缘形成一次宽阔溅射弧，敌方召唤物仅以
低细节剪影和足迹表达。

### 百鸣兽群

自然动物骨骼的明亮奔行兽群，以一只成年鹿形主兽为视觉中心，周围鸣禽与叶状暖光
汇成繁茂。主兽身后清楚出现两只小型迅捷幼灵，它们沿两条短促动势立即向前冲击。

### 山腹吞灵者

自然解剖的巨型山熊伏在日照山腹洞口，吸收友方荒灵消散形成的暖色灵光。灵光先
充盈胸腹生命，再沿前爪形成较细攻击光；远处胜利后的稳定苔环表达永久成长。

### 藤冠祭司

自然四足山羊祭司，保留真实蹄、尾与双角，藤冠只附着于角和颈部。身旁恰好四个
小型友方灵光依次消散，暖色繁茂环扩散到低细节荒灵盟友；不得把主体做成人形。

定点编辑记录：

- 藤冠祭司先修正友方灵光数量为恰好四个，再只扩展画布并重构边缘到约 5:4；
  主体、盟友关系和四个灵光保持不变。
- 猎群监察官曾尝试移除武器装饰并进一步规整三枚计数珠，定点编辑服务连续网络
  失败；保留经人工复核通过的原始候选，未引入替代生成链路。
