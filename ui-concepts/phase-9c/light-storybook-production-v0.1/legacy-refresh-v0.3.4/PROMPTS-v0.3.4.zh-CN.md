# Legacy Card Art Refresh v0.3.4 Prompt 集

- 生成日期：2026-08-01
- 工具：Codex 内置 ImageGen
- 生成模式：每张图一次独立的新图生成；不做旧图编辑
- 唯一图像参考：
  `../../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`
- 参考图角色：只约束媒介、纸张、线条、曝光和色彩，不约束角色、物件或构图

## 公共 Prompt 块

下列公共块与后续每张卡的专属块共同构成本轮最终 Prompt 集：

> Create a brand-new landscape card illustration in the frozen
> "Wandering Storybook" production style shown by the style-only reference:
> bright daylight watercolor and gouache on warm ivory fibrous paper, colored
> ink outlines, soft pigment blooms, restrained amber accents, airy readable
> values, no photorealism and no glossy 3D rendering. Use an approximately 5:4
> landscape canvas. Keep one dominant subject and all critical anatomy or
> gameplay symbols inside the central 80 percent so the image survives card
> cropping. Light and middle values must occupy at least half of the image;
> near-black pixels must remain below twelve percent. No text, letters,
> numbers, logo, watermark, UI, card frame, night scene, moon, heavy vignette
> or near-black background. Treat the attached image as style-only reference.
> Do not copy its characters, props or composition.

种族与法术附加约束：

- 锻魂：必须是有生命的锻炉造物，而不是“人穿盔甲”；面部由炉口、煤火、
  铸铁或陶壳结构表达。
- 荒灵：动物或植物灵体的自然结构必须可信；严格控制腿、爪、尾巴数量和连接点。
- 星裔：人形角色保持正常两手，仪器为外置工具，不生成额外机械手臂。
- 游侠：旅行者、手艺人与可携带工具，轮廓清楚，避免堆满道具。
- 法术：用单一清晰的动作或符号讲机制，不画卡框、数值或文字。

## 14 张独立生成块

### 星盘校准师

> A calm Starbound astrolabe calibrator in a sunlit hill observatory, exactly
> two human hands adjusting one external brass astrolabe. The astrolabe,
> calibration needle and star-map disk form one readable triangular action.
> Normal human anatomy; no extra arms, floating hands or embedded machinery.

### 回火修补匠

> A small living Forge Soul mender beside a daylight workshop brazier, using
> two compact tool limbs to repair a cracked ceramic-metal companion shell.
> Its face is a warm furnace opening, not a human face behind armor. One clear
> repair action, gentle amber heat, no humanoid knight.

### 裂甲复仇者

> A single living Forge Soul avenger advancing through a bright forge yard.
> Its body is an animated cast-iron and fired-clay construct with one major
> glowing crack across the chest. The silhouette reads as a furnace creature,
> never as a person wearing armor. Two legs, two arms, no extra limbs.

### 腐叶承嗣

> A young Wild Spirit made from a small woodland animal and layered autumn
> leaves, standing in a bright mossy clearing. One crown of decaying leaves
> sheds a few amber spores while fresh green shoots emerge beneath it. Exactly
> four animal legs and one natural tail.

### 狐群巢母

> One adult Wild Spirit fox matriarch standing protectively before a warm,
> sunlit earthen den. The physical fox has exactly four legs, four paws and one
> tail. A subtle den-wall spirit projection may suggest the future two-tailed
> lineage, but it must not look like extra physical tails attached to her.

### 秘页折光师

> A Starbound scholar in a sunlit paper observatory, exactly two hands holding
> one translucent secret page before an external triangular prism. A single
> ray splits into three soft colored paths across a star chart. Normal anatomy;
> no extra hands, no floating limbs, no text on the page.

### 星图掮客

> A friendly Starbound map broker at an open-air daylight stall, exactly two
> hands presenting one rolled celestial map and one small brass measuring
> tool. A few hanging map ribbons and amber markers imply exchange, with no
> readable writing and no extra arms.

### 百艺学徒

> A young Wayfarer apprentice in a bright roadside workshop, practicing one
> clear craft while three compact travel tools are neatly arranged nearby.
> The character has exactly two arms and two legs. Keep a single dominant
> figure and avoid a cluttered inventory display.

### 万蹄奔潮

> One powerful Wild Spirit stag surging through a sunlit shallow stream, with
> leaf-and-water echoes suggesting a vast herd without adding literal bodies.
> Show exactly two front legs, two hind legs and four hooves on the physical
> animal. No duplicated legs, no fused hooves, no extra tail.

### 天穹契约者（r2）

> A Starbound covenant bearer standing under a bright open sky, exactly two
> hands holding one external ceremonial compass. Around the figure are exactly
> four and only four distinct covenant seal stations, clearly separated and
> equally readable. No fifth station, no repeated miniature seal, no text.

首轮图因出现 5 个契约站被预审否决；r2 使用上面的“exactly four and only
four”约束重新独立生成。

### 小型锻体

> A spell illustration centered on one small Forge Soul figurine on a sunlit
> anvil. One controlled tempering wave seals two visible red-and-blue seam
> lines across its ceramic-metal body. No smith character, no extra figurines,
> no text or numeric symbols.

### 免费刷新

> A bright market spell scene with exactly two adjacent display alcoves
> rotating into view, each secured by one visibly open clasp. A single airy
> ribbon of light connects the two alcoves to communicate a free refresh.
> Exactly two alcoves and two open clasps; no coins, prices, text or UI.

### 高阶发现

> A luminous discovery spell showing exactly three distinct archways in a
> bright storybook library garden. Each archway contains exactly one small
> silhouetted figure and one amber destination stop, for totals of three
> archways, three figures and three stops. No fourth option, no text or UI.

### 战前赐福

> A prebattle blessing spell with exactly five small allied figurines gathered
> beneath one broad translucent amber shield in daylight. The five figures are
> individually readable and form one compact formation. Exactly five figures,
> one shield canopy, no sixth figure, text, numbers or card frame.

## 已继承的 Token Prompt

3 张 Token 的最终 Prompt 与 r1/r2 否决记录保存在：

`../token-refresh-v0.3.4/PROMPTS-v0.3.4.zh-CN.md`

其中双尾狐影 Runtime 候选固定为 r3：四腿、四爪、两条尾巴均从后腿上方骨盆
区域发出；r1 尾根错误、r2 五腿错误均不得晋级。
