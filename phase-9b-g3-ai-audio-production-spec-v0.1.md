# 阶段 9B G3 AI 自制音频生产规范 v0.1

- 日期：2026-07-26
- 状态：项目负责人已确定正式音频改为自行使用 AI 工具生成；正式候选尚未完成
- 生产范围：3 首可无缝循环 BGM、25 个 P0 语义 Cue、64 个短音效变体
- Runtime 契约：28 个 Cue / 67 个非空且互不重复的 Clip 引用
- 工程范围：AudioMixer、Catalog、AudioService、MusicDirector、音量设置、事件映射与自动化由项目代码完成
- 关联方案：`phase-9b-presentation-vertical-slice-technical-design-v0.1.md`
- 本地参考：当前 3 BGM + 64 SFX 的确定性程序合成包只用于联调、节奏参考和 A/B 对照，见 `phase-9b-g3-local-synth-placeholder-audio-v0.1.md`
- 权利边界：AI 生成不自动等于可商用或可登记为 `Runtime Ready`；项目负责人必须按实际使用的工具、账户方案、生成日期和发行地区复核服务条款。本规范是项目验收要求，不构成法律意见

## 1. 生产目标与状态边界

总体声音方向是“暗色奇幻棋盘上的温暖绘本远征”：

- 核心材质为木、纸、旧铜、低饱和织物和克制的魔法玻璃，不使用现代科幻界面音；
- 铸魂可使用低频金属、炉火和重量，荒灵可使用木、叶和轻盈气流，星契可使用玻璃、
  书页、细铃和折光；P0 Cue 仍是事件级通用音，不按卡牌逐张扩张；
- 高频操作短、清楚、不过亮，连续点击、刷新、攻击或嵌套召唤不会形成刺耳峰值；
- BGM 可长时间循环，为卡牌文字和 1.5–6 kHz 的关键 SFX 瞬态留出空间；
- 禁止在 Prompt 中要求模仿具名作曲家、艺人、游戏、电影、角色、主题曲或商标音；
- 禁止使用未经许可的参考音频、第三方采样、翻唱、分轨、人声克隆或可识别旋律。

AI 生成与 Runtime 状态严格分离：

1. `AI Draft`：AI 工具原始输出，只进入工作区，不进入 Runtime Catalog。
2. `Production Candidate`：完成筛选、剪辑、循环、格式和来源记录；这是生产台账状态，
   Catalog 仍保持 `Pending` 或不接入，不得标记 `ProductionApproved`。
3. `ProductionApproved`：该 Cue 的全部规定变体、权利记录、独立文件 QA 和项目负责
   人逐 Cue 听审均通过，才可在 Catalog 显式批准。
4. `Runtime Ready`：28 Cue / 67 Clip 全部批准，严格门禁、全量测试、压力测试和最终
   台账均通过后，才可用于关闭 G3。

当前 `Local Synth Placeholder` 不会因为可播放而自动晋级到上述任何生产状态。

## 2. AI 生成与留档流程

### 2.1 每次生成必须记录

每个候选至少记录：

| 字段 | 要求 |
| --- | --- |
| Cue / Variant | 稳定 Cue ID 与 `01..NN` 变体号 |
| AI 工具 | 产品名、提供方、访问方式 |
| 模型 | 工具显示的模型、版本或模式；不可获得时明确写“不可获得” |
| 账户与方案 | 账户持有人、免费/订阅/企业方案；不得记录密码、Token 或支付密钥 |
| 生成时间 | 含时区的日期时间 |
| Job / Seed | 任务 ID、历史页 URL、seed 和参数；不可获得时明确记录 |
| Prompt | 完整正向 Prompt、负面 Prompt、矩阵版本与后续修改 |
| 参考输入 | 上传的音频、图像或其他素材；没有时明确写“无” |
| 原始输出 | AI 原始文件名、原生格式/采样率/位深、SHA-256；不得覆盖 |
| 服务条款 | 生成当日条款 URL、访问日期和项目方保存的截图/PDF |
| 商用与 Content ID | 商用范围、署名要求、再分发限制、输出是否非排他、Content ID/自动认领限制 |
| 人工后期 | 剪辑、降噪、重采样、声道、循环、EQ、压限、响度和使用的软件版本 |
| 最终文件 | Master/Runtime 路径与 SHA-256 |
| 评审 | 评审人、日期、结论与驳回原因 |

若工具无法明确支持预期的商业发行、游戏内嵌分发和必要修改，或可能由平台/第三方对
游戏内容发起 Content ID/自动认领，该输出只能保留为参考，不得标记
`ProductionApproved`。

“服务允许商用”和“输出具备可主张的排他著作权”是两个不同结论，必须分别记录，
不得从前者推定后者。本阶段默认不把 AI 生成音频注册到 Content ID 或其他自动认领
系统；发行平台要求披露生成式内容时，应能从逐文件台账追溯到工具、模型、输入和
人工编辑链。

### 2.2 生成批次

1. 方向批次：3 首 BGM 各生成 3 个候选；先生成
   `ui_click / shop_refresh / battle_attack_light / battle_hit /
   battle_summon / battle_death` 六个锚点 Cue。
2. 锁定批次：项目负责人确定共同世界、明暗、重量、空间和瞬态边界。
3. 全量批次：按本规范逐 Variant 生成 2–4 个候选，只选择 1 个进入规定槽位。
4. 后期批次：保存 AI 原始输出，另存人工编辑 Master；不得对有明显 AI 伪影的文件
   仅靠强限幅掩盖问题。
5. 接入批次：先完成来源记录和文件级检查，再接入 Catalog；不得先把状态改成
   `ProductionApproved` 后补材料。

全部 Cue 显式批准后再运行依赖该状态的 `ProductionStrict`。若严格门禁发现任一 Cue
的路径、格式、声道、数量或 Importer 不合格，必须立即把受影响 Cue 的台账退回
`Production Candidate`，同时把 Catalog 状态改回 `Pending`，修复并重新批准；
不得保留失败状态等待补做。

同一 Cue 的变体应保持约 70%–85% 的共同声纹。差异来自材质、微小音高、触发力度和
尾音细节，不得通过极端响度、完全不同的语义或长短悬殊制造“变体”。

## 3. Prompt 使用规则

以下 Prompt 为工具无关的英文自然语言版本。若 AI 工具有独立 Negative Prompt 字段，
将负面部分填入该字段；若没有，则在正向 Prompt 末尾追加 `Avoid:`。AI 输出的时长、
采样率和声道不能只凭 Prompt 视为合格，必须由文件检查和后期处理确认。

### 3.1 BGM 通用负面 Prompt

```text
No vocals, lyrics, spoken words, whispers, choir, voice-like pads, borrowed or
recognizable melody, named-artist imitation, copyrighted game or film theme,
modern EDM synths, rock drum kit, huge cinematic brass, trailer braams, risers,
jump-scare impacts, aggressive side-chain pumping, dominant sub-bass, piercing
2–8 kHz lead, dense wall-of-sound orchestration, abrupt ending cadence, fade-in,
fade-out, long silence, crowd ambience, shop chatter, sound logos or watermarks.
```

### 3.2 SFX 通用正向 Prompt

```text
Create one original isolated single-shot game sound effect for a warm storybook
dark-fantasy strategy game played on a tactile board. Use hand-made materials:
wood, parchment, aged brass, muted cloth and restrained magical glass. Give it
an immediate readable onset, exactly one event, a clean controlled tail,
close-miked detail and comfortable headroom. No background ambience. Match the
requested duration and channel layout. The result must remain clear during
rapid repeated gameplay without becoming harsh.
```

### 3.3 SFX 通用负面 Prompt

```text
No speech, breath, vocalization, music bed, melodic phrase, recognizable sound
logo, named-game imitation, modern sci-fi beep, laser, gun, explosion, cinematic
braam, trailer riser, casino or slot-machine sound, exaggerated sub-bass, harsh
high-frequency ring, clipping, limiter pumping, DC offset, background noise,
multiple repetitions, baked pitch randomization, long leading silence or an
excessive reverb tail.
```

每个短音的完整 Prompt 由以下四部分拼接：

```text
SFX 通用正向 Prompt
+ 本 Cue 的核心 Prompt
+ 本 Variant 的差异 Prompt
+ “Target duration / Output channel layout”
```

负面 Prompt 为“SFX 通用负面 Prompt + 本 Cue 禁止项”。

## 4. 三首 BGM 生产与提示词矩阵

### 4.1 技术矩阵

| Cue ID | Runtime 文件 | 调式 / BPM / 小节 | 目标时长 | Loop sample（48 kHz） | 上下文 |
| --- | --- | --- | ---: | --- | --- |
| `bgm_main_menu` | `Music/bgm_main_menu_v01.ogg` | D Dorian / 72 / 32 bars | 106.666667 秒 | 0 → 5,120,000 | MainMenu |
| `bgm_run_shop` | `Music/bgm_run_shop_v01.ogg` | G Dorian / 90 / 48 bars | 128 秒 | 0 → 6,144,000 | Run/Map 与 Shop，共享且互切不重启 |
| `bgm_battle_normal` | `Music/bgm_battle_normal_v01.ogg` | D Aeolian / 120 / 48 bars | 96 秒 | 0 → 4,608,000 | 普通 Battle |

共同产出要求：

- AI 原始输出使用工具提供的最高质量格式；优先无损；
- Master 为 48 kHz / 24-bit / stereo WAV，Runtime 为项目确认后的 Ogg Vorbis；
- Runtime 只使用 `AudioSource.loop` 整文件循环，不支持文件内部的自定义 loop 区间；
  因此 OGG 必须裁成 `loopStartSample = 0`、`loopEndSample = decoded sample count`，
  并把解码后的精确 sample count 写入台账；
- 文件内不烘焙淡入淡出；
- 首尾拼接无爆音、跳相位、可感知空白、尾音切断或混响环境突变；
- 目标约 -16 LUFS-I，True Peak 不高于 -1 dBTP；三首主观响度一致；
- 低频不过量，立体声相关性安全；正式接入仍使用 `Streaming Vorbis`、后台加载、
  不预载并保留 48 kHz。

### 4.2 `bgm_main_menu`

正向 Prompt：

```text
Compose an original seamless 32-bar instrumental loop in D Dorian at 72 BPM,
4/4, exactly 106.666667 seconds before post-processing. It is the main menu of
a warm storybook dark-fantasy expedition played on a wooden tactical board:
quiet anticipation before departure, intimate rather than heroic. Use sparse
nylon or wooden plucked strings, soft low strings, occasional muted frame-drum
touches, a restrained aged-brass or glass bell accent, and a barely audible
warm drone. Write a simple non-recognizable motif with generous rests and an
A–A'–B–A shape. Keep transients gentle and leave space for UI clicks and Chinese
text reading. End on continuation harmony that reconnects naturally to bar 1;
no final cadence and no baked fade. Original composition only.
```

候选差异：

- Candidate A delta：`Let plucked strings carry the main contour; use bell accents only at section boundaries.`
- Candidate B delta：`Emphasize warmth from low strings and wooden plucks; reduce high-frequency glass color.`
- Candidate C delta：`Add a very light parchment-and-travel texture; it must not become ambience or rhythmic noise.`

拒绝条件：听起来像胜利结算、悲伤挽歌、宏大冒险主题或咖啡馆轻音乐；主旋律过强；
循环点像歌曲结束后重新播放。

### 4.3 `bgm_run_shop`

正向 Prompt：

```text
Compose an original seamless 48-bar instrumental loop in G Dorian at 90 BPM,
4/4, exactly 128 seconds before post-processing. It accompanies both route-map
travel and shop preparation in a warm storybook dark-fantasy strategy game.
Create a steady light walking pulse with wooden plucks, warm pizzicato low
strings, muted hand percussion, subtle parchment flicks and tiny clockwork-brass
details. It should feel practical, curious and forward-moving, never comic or
busy. Use an A–B–A'–C form with small arrangement changes but stable energy so
switching between Run and Shop does not feel like a scene restart. Leave clear
space for purchase, refresh and reward SFX. The last bar must flow directly
into bar 1 without a final cadence or fade. Original composition only.
```

候选差异：

- Candidate A delta：`Lean toward a travel-step character, with clearer wooden percussion and plucked strings.`
- Candidate B delta：`Lean toward a preparation workbench, using slightly more aged-brass and light mechanical texture without sounding modern.`
- Candidate C delta：`Lean toward route planning, emphasizing parchment and low strings while preserving the 90 BPM pulse.`

拒绝条件：出现酒馆喧闹、人群、现金机、赌场、滑稽拨弦、蒸汽朋克机器轰鸣或明显
战斗紧张感；Shop 与 Run 听起来像两首拼接音乐。

### 4.4 `bgm_battle_normal`

正向 Prompt：

```text
Compose an original seamless 48-bar instrumental combat loop in D Aeolian at
120 BPM, 4/4, exactly 96 seconds before post-processing. It is normal battle
music for a warm storybook dark-fantasy tactical board game: focused momentum,
clear pulse and controlled danger, not epic boss music. Use low-string and
wooden-pluck ostinatos, dry frame drums, restrained hammered-wood or dulcimer
accents, muted aged-brass support and a subtle dark drone. Keep the arrangement
lean, with no large impact on every beat, so attack, hit, shield, summon and
death SFX remain intelligible. Build small eight-bar intensity waves without a
drop, breakdown or final cadence. The final bar must reconnect seamlessly to
bar 1; no fade. Original composition only.
```

候选差异：

- Candidate A delta：`Let the wooden-drum pulse lead while low strings provide only continuous forward motion.`
- Candidate B delta：`Let the low-string ostinato lead; make percussion drier and sparser.`
- Candidate C delta：`Let hammered-wood or dulcimer texture lead, but keep it dark enough not to mask shield and stat-growth effects.`

拒绝条件：Boss 级史诗铜管、合唱、摇滚鼓组、预告片冲击、持续高密度打击、恐怖片
氛围或循环前明显收尾。

## 5. P0 短音技术与输出矩阵

### 5.1 精确输出矩阵

| 领域 | Cue ID | 数量 | 参考时长 / nominal samples | 声道 | Runtime 文件 |
| --- | --- | ---: | ---: | --- | --- |
| UI | `ui_click` | 3 | 0.10 秒 / 4,800 samples | mono | `SFX/UI/sfx_ui_click_01..03.wav` |
| UI | `ui_confirm` | 2 | 0.24 秒 / 11,520 samples | mono | `SFX/UI/sfx_ui_confirm_01..02.wav` |
| UI | `ui_cancel` | 2 | 0.20 秒 / 9,600 samples | mono | `SFX/UI/sfx_ui_cancel_01..02.wav` |
| UI | `ui_error` | 2 | 0.32 秒 / 15,360 samples | mono | `SFX/UI/sfx_ui_error_01..02.wav` |
| Shop | `shop_refresh` | 3 | 0.48 秒 / 23,040 samples | stereo | `SFX/Shop/sfx_shop_refresh_01..03.wav` |
| Shop | `shop_buy` | 3 | 0.35 秒 / 16,800 samples | mono | `SFX/Shop/sfx_shop_buy_01..03.wav` |
| Shop | `shop_sell` | 3 | 0.38 秒 / 18,240 samples | mono | `SFX/Shop/sfx_shop_sell_01..03.wav` |
| Shop | `shop_play` | 3 | 0.44 秒 / 21,120 samples | mono | `SFX/Shop/sfx_shop_play_01..03.wav` |
| Shop | `shop_spell` | 3 | 0.65 秒 / 31,200 samples | mono | `SFX/Shop/sfx_shop_spell_01..03.wav` |
| Shop | `shop_triple` | 1 | 1.15 秒 / 55,200 samples | mono | `SFX/Shop/sfx_shop_triple_01.wav` |
| Shop | `shop_discover_open` | 1 | 0.80 秒 / 38,400 samples | stereo | `SFX/Shop/sfx_shop_discover_open_01.wav` |
| Shop | `shop_discover_pick` | 2 | 0.38 秒 / 18,240 samples | mono | `SFX/Shop/sfx_shop_discover_pick_01..02.wav` |
| Shop | `shop_upgrade` | 1 | 1.10 秒 / 52,800 samples | mono | `SFX/Shop/sfx_shop_upgrade_01.wav` |
| Battle | `battle_attack_light` | 4 | 0.28 秒 / 13,440 samples | mono | `SFX/Battle/sfx_battle_attack_light_01..04.wav` |
| Battle | `battle_hit` | 4 | 0.30 秒 / 14,400 samples | mono | `SFX/Battle/sfx_battle_hit_01..04.wav` |
| Battle | `battle_shield_gain` | 3 | 0.50 秒 / 24,000 samples | mono | `SFX/Battle/sfx_battle_shield_gain_01..03.wav` |
| Battle | `battle_shield_break` | 3 | 0.55 秒 / 26,400 samples | mono | `SFX/Battle/sfx_battle_shield_break_01..03.wav` |
| Battle | `battle_stat_up` | 3 | 0.42 秒 / 20,160 samples | mono | `SFX/Battle/sfx_battle_stat_up_01..03.wav` |
| Battle | `battle_death` | 4 | 0.90 秒 / 43,200 samples | mono | `SFX/Battle/sfx_battle_death_01..04.wav` |
| Battle | `battle_token_death` | 3 | 0.35 秒 / 16,800 samples | mono | `SFX/Battle/sfx_battle_token_death_01..03.wav` |
| Battle | `battle_summon` | 4 | 0.55 秒 / 26,400 samples | mono | `SFX/Battle/sfx_battle_summon_01..04.wav` |
| Result | `battle_victory` | 1 | 1.35 秒 / 64,800 samples | mono | `SFX/Battle/sfx_battle_victory_01.wav` |
| Result | `battle_defeat` | 1 | 1.35 秒 / 64,800 samples | mono | `SFX/Battle/sfx_battle_defeat_01.wav` |
| Run | `run_node_select` | 3 | 0.34 秒 / 16,320 samples | mono | `SFX/Run/sfx_run_node_select_01..03.wav` |
| Run | `run_reward` | 2 | 0.85 秒 / 40,800 samples | mono | `SFX/Run/sfx_run_reward_01..02.wav` |
| 合计 | 25 Cue | 64 | — | 60 mono + 4 stereo | 不含 3 首 BGM |

文件名中的 Cue ID 已包含领域前缀，因此不得再重复拼接领域。权威格式为：

```text
Assets/Audio/Presentation/SFX/<Domain>/sfx_<cue-id>_<NN>.wav
```

例如 `sfx_shop_refresh_01.wav`，不是 `sfx_shop_shop_refresh_01.wav`。

表中的时长与 sample 数是同一目标在 48 kHz 下的 nominal 值，用于生成、裁切和同 Cue
一致性评审；它们不是 `ProductionStrict` 的逐文件等长门禁。实际时长允许按下述范围
调整，并必须把最终 sample count 逐文件写入台账。

短音共同规格：

- 最终源文件为 48 kHz / 24-bit PCM WAV；
- 目标时长允许在表值附近约 ±20%，但同一 Cue 内起音和尾音长度必须接近；
- 前导静音小于 10 ms；保留受控自然尾音，不保留多余空白；
- True Peak 不高于 -1 dBTP；若工具只测 sample peak，必须单独以 dBFS 记录，不得
  混称 True Peak；无削波、DC offset、不可解释噪声或 AI 水印；
- 除 `shop_refresh` 与 `shop_discover_open` 外全部为真实 mono，不依赖
  `forceToMono`；
- 源文件不烘焙随机音高、连续重复或多个独立事件；音高、并发和冷却由 Catalog 控制。

## 6. 25 Cue 完整 Prompt / 变体矩阵

表中“核心 Prompt”和某一个“Variant 差异”按第 3 节规则拼接，即得到该文件的完整
正向 Prompt。目标时长和声道取第 5.1 节同一行。

### 6.1 UI

| Cue | 核心 Prompt | Variant 差异 | Cue-specific negative Prompt |
| --- | --- | --- | --- |
| `ui_click` | `A tiny dry tap of a small wooden game token on parchment, finished with one restrained aged-brass tick; neutral navigation feedback, crisp and soft.` | `01: pale maple token, centered mid pitch, shortest tail.`<br>`02: darker walnut token, slightly lower pitch, rounder body.`<br>`03: bone-inlaid wooden token, slightly higher pitch, lighter tail.` | `Avoid confirmation rises, coins, lateral sweeps, or any emphasized domain-operation cue.` |
| `ui_confirm` | `A compact two-part upward confirmation made from a tactile wooden press followed by a warm restrained tonal accent; successful but not celebratory.` | `01: wood press followed by muted aged-brass minor-third rise.`<br>`02: parchment seal followed by soft magical-glass perfect-fourth rise.` | `No purchase, reward, victory or triple-merge character; use no more than two distinct sonic syllables.` |
| `ui_cancel` | `A soft downward close gesture: tactile release, folded parchment or damped wooden latch, calm and final without implying failure.` | `01: short parchment fold and wooden tuck, gentle downward contour.`<br>`02: damped wooden latch with a muted cloth release, slightly lower.` | `No error alarm, failure buzzer, heavy sub-bass, or reversed confirmation audio.` |
| `ui_error` | `A restrained low invalid-action response: muted wooden knock plus a very short damped brass downward wobble, readable but never alarming.` | `01: one compact low knock and a single damped downward resonance.`<br>`02: two very close soft knocks with a shorter, darker resonance.` | `No system alarm, horror sting, defeat result, explosion, or piercing buzzer.` |

### 6.2 Shop

| Cue | 核心 Prompt | Variant 差异 | Cue-specific negative Prompt |
| --- | --- | --- | --- |
| `shop_refresh` | `A brisk refresh of several parchment cards and wooden shop tiles, with a readable horizontal spatial sweep and tiny restrained brass details; energetic but light.` | `01: parchment card fan moving left to right, ending in one wood tick.`<br>`02: three cards opening from center to both sides, ending in two tiny brass ticks.`<br>`03: wooden tiles raking right to left with a softer paper follow-through.` | `Stereo output is mandatory. No storm, spell release, cash-register sound, or generic UI whoosh.` |
| `shop_buy` | `A successful purchase: a small group of old brass or copper coins settles into a felt pouch, followed by one warm wooden ownership latch.` | `01: two light brass coins, soft felt catch, maple latch.`<br>`02: three smaller copper coins, slightly lower pouch body, walnut latch.`<br>`03: one heavier coin and two tiny contacts, short pouch clasp.` | `No casino, slot machine, modern cash register, reward chest, or reversed sell cue.` |
| `shop_sell` | `A sold object leaves the board first, then value returns as a delayed restrained coin response; outward motion and recovery, clearly different from buying.` | `01: wooden piece slides away, then one brass coin returns.`<br>`02: cloth-wrapped item lifts away, then two small copper contacts.`<br>`03: parchment tag releases, then a compact coin-and-pouch return.` | `Do not reverse shop_buy. No discard, death, or large-reward character.` |
| `shop_play` | `A carved game figure is placed firmly onto a wooden board, followed by a very small contained magical seal; clear arrival and board contact.` | `01: light wooden pawn placement, tight contact, minimal seal.`<br>`02: medium carved figure, warmer body, parchment seal.`<br>`03: heavier brass-inlaid figure, lower body, equally controlled peak.` | `No attack windup, life-damage impact, death collapse, or large summoning vortex.` |
| `shop_spell` | `A compact generic spell release: parchment sigil unfolds, restrained magical energy resolves, and one soft tonal accent confirms completion.` | `01: warm ember and old-brass accent, no fire explosion.`<br>`02: leaf-and-air curl with a muted wooden chime.`<br>`03: cool glass refraction with a soft astral pulse.` | `Do not bind the sound to a specific card. No speech, explosion, attack impact, or sustained spell ambience.` |
| `shop_triple` | `Three small wooden game pieces converge in three rapid tactile contacts, merge into one richer piece, then release a short warm golden brass-and-glass harmonic bloom.` | `01: only production variant; three contacts must remain readable before the merge.` | `No victory trombone, upgrade staircase, slot-machine jackpot, or duration longer than 1.5 seconds.` |
| `shop_discover_open` | `Three parchment choice cards fan outward from the center into a wide readable stereo layout, with restrained magical-glass edges and a soft central reveal.` | `01: only production variant; center-to-sides motion must be clear.` | `Stereo output is mandatory. No selection confirmation, triple merge, reward deposit, or long melody.` |
| `shop_discover_pick` | `One chosen parchment card moves forward and locks into place with a compact confirmation accent; decisive but smaller than a reward.` | `01: card slide, wooden seal, tiny warm brass tick.`<br>`02: card lift, parchment snap, muted glass confirmation.` | `Do not replay the discover-opening gesture. No generic UI confirmation, reward, or spell burst.` |
| `shop_upgrade` | `A clear four-step upward construction: wood, aged brass and a restrained gear or forge mechanism rise in level, ending with one firm structural latch.` | `01: only production variant; all four ascending stages must remain unmistakable.` | `No three-object convergence, victory fanfare, modern machinery, steam release, or casino sound.` |

### 6.3 Battle / Result

| Cue | 核心 Prompt | Variant 差异 | Cue-specific negative Prompt |
| --- | --- | --- | --- |
| `battle_attack_light` | `A fast light attack launch and movement arc for a small tactical board unit; clear forward motion with no contact or damage at the end.` | `01: dry cloth-and-wood swipe, shortest and neutral.`<br>`02: leaf-edged air rush, light organic texture.`<br>`03: thin restrained metal arc, no blade clash.`<br>`04: narrow magical-glass streak, no impact pulse.` | `Strictly no hit, body impact, shield break, gunshot, or heavy-weapon character.` |
| `battle_hit` | `A compact unshielded life-damage impact on a tactile fantasy board unit: body, wood, leather or earth contact, immediate and controlled, with no approach whoosh.` | `01: tight wood-and-leather thud, medium pitch.`<br>`02: softer cloth-covered body impact, slightly higher.`<br>`03: brass-inlaid wooden contact, brighter transient.`<br>`04: heavier earth-and-wood thump, still shorter than death.` | `Strictly no attack windup, glass shield, explosion, realistic bone or gore, or death tail.` |
| `battle_shield_gain` | `A protective magical shell forms around one unit: an upward wrap of restrained blue glass, faceted light and soft air, ending stable and intact.` | `01: thin clear-glass dome, quick high detail.`<br>`02: thicker faceted shell, warmer mid resonance.`<br>`03: star-prism weave, slightly wider shimmer but true mono source.` | `No fracture, life-damage impact, piercing ice sound, long melody, or stereo expansion.` |
| `battle_shield_break` | `A protective magical-glass shell cracks and collapses outward in one controlled event; readable fracture and falling energy, without body damage.` | `01: thin crystal crack with a short light shard cascade.`<br>`02: thicker faceted shell split with lower body.`<br>`03: prism shell breaks into fading magical motes.` | `No life-damage thud, attack whoosh, realistic window accident, or long shard tail.` |
| `battle_stat_up` | `A compact positive growth cue with an upward two- or three-stage contour, combining tactile material and restrained magic; encouraging but smaller than victory.` | `01: warm forge-brass rise with a soft ember finish.`<br>`02: wooden root and leaf bloom with gentle upward motion.`<br>`03: glass-and-star glint with a controlled final pulse.` | `No upgrade, triple merge, reward, victory fanfare, or sustained melody.` |
| `battle_death` | `A non-token tactical unit loses structure and leaves the board: substantial collapse of wood, cloth or magical material with a short dissipating tail; weighty but not graphic.` | `01: heavy carved-wood figure collapse with cloth settling.`<br>`02: brass-and-forge shell buckles and its ember extinguishes.`<br>`03: root-and-leaf body crumbles into a restrained organic fade.`<br>`04: glass-and-parchment astral construct fractures and dims.` | `Strictly no voice, scream, gore, explosion, victory or defeat result, or duration longer than 1.2 seconds.` |
| `battle_token_death` | `A small temporary token disappears from the board in one light ephemeral event; short, low-mass and clearly smaller than a real unit death.` | `01: tiny parchment pawn puff and fold.`<br>`02: small leaf-and-wood chip dispersal.`<br>`03: miniature glass mote pop and quick fade.` | `No heavy low end, body collapse, scream, explosion, or non-token death weight.` |
| `battle_summon` | `Restrained magical energy gathers inward, forms one tactical unit, and ends with a clear but light placement on the board.` | `01: ember-and-forge particles gather, then a small wooden landing.`<br>`02: leaves and soft air spiral inward, then an organic placement.`<br>`03: star-prism light focuses, then a glass-edged arrival.`<br>`04: parchment sigil folds inward, then a neutral carved-piece landing.` | `No attack dash, hit, revival voice, teleport explosion, or sustained magical ambience.` |
| `battle_victory` | `A short original warm ascending result cadence for wood, aged brass and restrained plucked strings; earned relief and forward momentum, ending cleanly.` | `01: only production variant; one compact phrase, no loop.` | `No full BGM, royal anthem, choir, slot-machine jackpot, or duration longer than 1.5 seconds.` |
| `battle_defeat` | `A short original restrained descending result cadence using low wood, muted strings and one soft aged-brass release; sober but not frightening or punitive.` | `01: only production variant; one compact phrase, no loop.` | `No failure buzzer, horror jump scare, scream, funeral march, or reversed victory audio.` |

### 6.4 Run

| Cue | 核心 Prompt | Variant 差异 | Cue-specific negative Prompt |
| --- | --- | --- | --- |
| `run_node_select` | `A route choice is committed on a parchment expedition map: one tactile map-marking action plus a compact travel confirmation.` | `01: wooden map pin and short pencil contact.`<br>`02: aged-brass compass click and parchment fold.`<br>`03: route thread tightens, followed by a small token tap.` | `Do not layer a generic UI click. No purchase, reward, combat-start, or long map ambience.` |
| `run_reward` | `A run reward is received and secured: tactile opening or placement followed by a warm restrained brass-and-magic acknowledgement.` | `01: card or small pouch reward, parchment lift and compact warm seal.`<br>`02: relic-sized reward, small wooden case and richer brass-glass aura.` | `No victory result, triple merge, purchase coins, slot machine, or duration longer than 1.2 seconds.` |

## 7. 唯一事件映射

同一次领域操作只消费一个主要操作 Cue，避免按钮点击音与领域操作音双播。

### UI

- 纯导航 → `ui_click`
- 确认 → `ui_confirm`
- 取消 → `ui_cancel`
- 失败 → `ui_error`
- 会产生 Shop/Run/Battle 领域事件的按钮不再额外播放 `ui_click`

### Shop

- `OnRefresh` → `shop_refresh`
- `OnBuy` → `shop_buy`
- `OnSell` → `shop_sell`
- `OnPlay` → `shop_play`
- `OnSpellUsed` → `shop_spell`
- `OnTripleFormed` → `shop_triple`
- `OnDiscoverStarted` → `shop_discover_open`
- `OnDiscoverResolved` → `shop_discover_pick`
- `OnTavernUpgraded` → `shop_upgrade`
- `OnShopPhaseStart/End`、`OnTripleRewardGranted`、`OnDiscoverCancelled` 当前不映射专用
  P0 Cue

### Battle / Result

- `AttackStarted` → `battle_attack_light`
- `DamageApplied && !WasBlocked` → `battle_hit`
- `ShieldGained` → `battle_shield_gain`
- `ShieldLost` → `battle_shield_break`
- 正向 `StatsChanged` → `battle_stat_up`
- `UnitSummoned` → `battle_summon`
- `UnitDied` 按移除前表现模型映射 `battle_token_death` / `battle_death`
- `CombatEnded` 按结构化胜负映射 `battle_victory` / `battle_defeat`，不得解析日志文字
- 平局当前静音，后续若增加专用 Cue 必须独立立项

### Run

- 成功确认地图节点 → `run_node_select`
- 成功领取奖励/遗珍 → `run_reward`

## 8. 后期与技术验收

### 8.1 BGM

- 优先从无损 AI 原始输出编辑；工具只提供有损文件时必须在台账披露，转成 WAV 不代表
  恢复无损质量；
- 记录原生导出的格式、采样率和位深；不得把 MP3、AAC 或 16-bit 文件上采样/升位后
  描述为“原生 48 kHz / 24-bit 无损母带”；
- 在 DAW 中按目标 BPM 和小节裁成精确长度，处理跨边界尾音并逐 sample 记录 loop；
- 不通过强压限把明显生成伪影、呼吸式噪声或结构跳变掩盖成“可用”；
- 检查至少连续循环 10 次、MainMenu ↔ Run/Shop ↔ Battle 快速切换和淡化中停止；
- 输出 Master WAV 与 Runtime OGG 的 SHA-256。

### 8.2 SFX

- 裁掉前导空白、重复触发和多余尾音，必要时做轻量去噪、EQ 和峰值控制；
- 同 Cue 全部变体在相同监听音量下比较起音、响度、长度和语义；
- 在连续刷新、连续受击、四层嵌套召唤/亡语压力场景下检查叠加峰值；
- `attack_light` 与 `hit`、`shield_break` 与 `hit`、`death` 与
  `token_death` 必须能盲听区分。

## 9. ProductionApproved / Runtime Ready 门禁

一个正式 AI Cue 只有同时满足以下条件才可标记 `ProductionApproved`：

- 该 Cue 的全部规定变体已完成，数量精确、无 null、无重复引用；
- Prompt、AI 工具/模型、原始输出、服务条款、商用范围、参考输入和人工修改已登记；
- 项目负责人确认相关权利证据足以用于计划中的商业发行；
- 文件名、路径、格式、声道、循环点和变体数符合本规范；
- BGM 精确位于 `Music/<cue>_v01.ogg`，48 kHz stereo，使用 Streaming Vorbis、
  后台加载且不预载；
- SFX 精确位于 `SFX/<Domain>/sfx_<cue-id>_<NN>.wav`，源文件为
  48 kHz / 24-bit PCM，按矩阵使用 mono/stereo，并以 Decompress On Load 预载；
- 人工听审无明显 AI 伪影、可识别借用、混淆、刺耳峰值或不可接受循环；
- Master/Runtime SHA-256 与台账一致。

G3 只有在以下全部满足后才能标记音频 `Runtime Ready` 并关闭：

- 25 个 P0 Cue 与 3 个 BGM 上下文全部精确命中，共 28 Cue / 67 Clip；
- `ProductionStrict` 通过；
- Unity EditMode/PlayMode 全量测试通过；
- BGM 循环、快速切场、同上下文不重启和跨场景不叠加通过；
- 嵌套亡语/召唤峰值、场景退出 voice 清理和四路音量通过；
- 项目负责人完成最终人工听审并在来源台账签字。

`ProductionStrict` 只覆盖 Catalog、精确路径、格式、声道和 Unity Importer 等工程
契约，不检查 AI 服务条款、Prompt/模型、音频内容重复、响度、True Peak、静音、
削波、DC offset、循环接缝、旋律相似风险或人工听审；这些项目必须由独立文件 QA、
来源台账与项目负责人签字补齐。

在正式 AI Clip 和来源记录完成前，本地合成音继续保持 `Placeholder`；
不得用 AI Draft、静音文件、未完成权利复核的候选或临时转码文件伪造
`ProductionApproved`、人工听审或 `Runtime Ready`。
