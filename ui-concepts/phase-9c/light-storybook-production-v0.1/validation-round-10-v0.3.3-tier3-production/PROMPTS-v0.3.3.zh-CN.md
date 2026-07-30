# Round 10 三级随从量产 Prompt v0.3.3

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
- 荒灵保留自然动物骨骼，不默认将身体做成植物、岩石或人形祭司。

## 单卡变量

### 逆流铸师

倒置铜铁坩埚与低矮分砧构成非人形铸魂。U 形通道把两块破盾碎片的熔光逆向送回
坩埚，短暂加固两块碎片；第一道回流在锻锤边缘留下稳定暖光。

### 熔核执旗手

高瘦的非人形移动旗塔，由三块分离金属板、低位熔核和三只楔足组成。顶部是完全
无字、无布、无纹章的开叉金属标牌；熔核导流至两名新获浅蓝护盾的低细节友方，
使其武器边缘留下稳定暖光。

### 誓刃甲胄

无穿戴者的空心新月甲胄神龛，三片弧形甲板围绕暖色灵魂炉栅。正面护盾壳在浅蓝
护盾破裂瞬间打开，一片短刃沿余烬细丝弹向无标记测试石块。

### 回响咏星师

日照观星庭院中的中年人类咏星师，操作开叉黄铜共鸣架和两张完全无字的棱镜法术
薄片。恰好两道宽阔浅蓝回响依次发出；三名低细节人类友方中只有一名承接第二道
高亮回响，并留下稳定暖蓝光。

### 古苔巨幼体

自然可信的巨型蝾螈幼体，严格四足、一尾，背部局部覆盖古树皮状鳞片、苔藓和
小型层孔菌。身后只保留两块木质蛋壳；一名消散小兽魂带来宽阔临时成长光，
稳定苔环表示首次永久成长。

### 群枝唤灵者

自然长腿苍鹭，严格两翼两足，头背生长向后的细枝冠。抬起的脚旁只保留三株
新生根芽；暖色水纹强化根芽，脚踝的小型稳定芽环表示首次召唤后的永久成长。

定点编辑：移除初稿画面边缘和前景的额外小芽，仅保留主体前方三株核心根芽。

### 獠牙领奔者

自然成年野猪在明亮山路领跑，严格四足、一尾、两枚可见獠牙。一名小型召唤
野猪灵在后方化为暖色叶状光点，光点流向远处一名低细节动物友方并短暂强化其
姿态。
