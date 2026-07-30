# Round 9 二级随从量产 Prompt v0.3.3

- 日期：2026-07-30
- 工具：Codex 内置 ImageGen
- 模式：逐张生成；仅对局部偏差做定点编辑
- 唯一图像参考：
  `../../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`
- 冻结规则：`../freeze-v0.3.3/`
- 输出：本目录 `cards/`

## 共同 Prompt

每张图均使用以下共同约束，并叠加后续单卡变量：

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

阵营补充约束：

- 铸魂：灵魂装入连贯的手工铠甲、武器或机械结构，不使用身体恐怖或异常肢体。
- 星契：主体为清楚、亲和的人形角色，并同时具备至少两种观星、契约或知识线索。
- 旅团：以个体职业和旅行装备识别，不使用共享制服或共享生理结构。
- 荒灵：优先保留自然动物解剖，不默认将身体做成植物或岩石。

## 单卡变量

### 余烬刻师

明亮锻造庭院中的铜铁铸魂刻师，手持余烬刻笔。一名低细节友方铸魂的浅蓝护盾
刚破裂；暖色临时生命波流向友方，同时一枚稳定的抽象余烬刻痕固定在铠甲上。

### 破盾刃胚

完整紧凑的活体铸魂，核心武器是一柄宽阔未完成刃胚，主体位于浅蓝护盾中。
消散的灵魂能量分流至两名仍有护盾的低细节友方，并使其武器边缘变亮。

### 盾墙执炉者

具备宽阔炉门胸甲和热源容器的守护型铸魂。两名低细节友方相邻站立，其中一名
护盾刚破裂；执炉者只发送一道暖色加固波，使该友方轮廓更稳固。

### 月相抄写员

日照观星台上的人类抄写员，手持完全无字的棱镜册页，并操作外置月相仪。
三件低细节法术物件悬浮，其中一件被柔光选中；克制的星光尾迹连接远处较弱的
人类星契友方。

定点编辑：将初稿中的狐形类人友方替换为低细节人类星契学徒，其余内容保持不变。

### 符文护读者

浅蓝护盾保护的人类契约学者，阅读两张完全无字的棱镜薄片。两次独立法术脉冲
流向远处较弱的人类星契友方，使其武器边缘留下稳定暖光。

### 星刻计时员

人类计时员操作外置天球钟与黄铜星盘。恰好两颗高亮轨道珠完成一周，并释放一圈
新的浅蓝循环；机械只使用圆、弧、齿轮和珠粒，不出现数字、刻度或文字。

### 黑市小贩

明亮路边集市的隐蔽侧帐中，一名装备挎包、折篷与旅行工具的小贩从半开背包里
友好地递出恰好一枚铜币。谨慎感来自摊位位置，不使用黑暗或威胁。

定点编辑：将铜币替换为完全光滑、无边纹、无徽记、无字符的铜色圆片。

### 雇佣盾手

日照山路上的职业旅行盾手，使用实用轻甲、睡袋、绳索和旧盾。一名相邻的低细节
小型铸魂使浅蓝护盾在盾手周围形成；主体不是全身重甲骑士。

### 根须吞噬者

自然可信的獾形山地动物，保留正常毛发、口鼻与挖掘爪。环境根须将一名消散友方
兽魂的能量送入主体；宽暖光表示临时成长，前腿上的稳定根环表示首次永久成长。

### 疾羽林隼

自然林隼在明亮山林上空转弯飞行，严格保持两翼两足和正常羽毛。一名小型召唤
兽魂在身后化为光点，能量使前翼与利爪边缘变亮。

### 双尾狐灵

自然四足狐灵，主体严格只有两条且两条均完整可见。两只幼狐灵从柔和消散轮廓中
形成，少量未用暖光转向远处低细节动物友方。

定点编辑：移除初稿主体躯干和双尾上的全部白色旋纹与符文式装饰，恢复自然狐毛，
保持双尾、两只幼狐灵与其余构图不变。
