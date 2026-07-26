# Phase 9C 铸魂随从专用插画提示词记录 v0.1

- 日期：2026-07-26
- 生成方式：Codex 内置 ImageGen
- 范围：剩余 13 个铸魂随从；不包含校准项 `hearth_core_spark`
- 输出目录：`masters/minions/forge-soul/`
- ImageGen 源目录：
  `C:/Users/eden.xu/.codex/generated_images/019f9ebd-6389-72d2-b723-8e208325f95f/`
- 状态：母版生成与本地技术检查完成；尚未复制到 Unity Runtime，也未取得
  项目负责人逐项生产许可

## 共同输入与最终调用规则

每项均为一次独立 ImageGen 调用。四张输入图片在所有调用中只作为参考：

1. `../../phase-9b/style-tiles/style-tile-wandering-storybook-v0.3.png`：
   媒介、纸纹、色彩与笔触参考。
2. `../../phase-9b/archetype-anchor-illustrations-v0.2/masters/forge-soul-shield-squire.png`：
   小型铸魂空壳与锻铁材质参考。
3. `../../phase-9b/archetype-anchor-illustrations-v0.2/masters/forge-undying-furnace-king.png`：
   高阶铸魂空壳与体量上限参考。
4. `masters/minions/forge-soul/hearth-core-spark.png`：
   本轮约 5:4 横构图、纸纹和非人结构校准参考。

下列每项的“最终调用提示词”均包含该项代码块以及以下共同要求；生成时共同要求
已完整展开到单次调用中：

```text
Use case: stylized-concept
Asset type: landscape game-card illustration master for the named Forge Soul minion
Input images: Image 1 is style-only reference for warm Wandering Storybook
watercolor, walnut/colored ink, fibrous ivory paper and irregular painted
edges. Images 2 and 3 are species/material references only for living empty
forged shells. Image 4 is the current production calibration for horizontal
framing, paper texture and non-human anatomy. Do not copy, recolor, trace, or
reuse any reference subject, pose, shield silhouette, crown, round furnace
body, or scene.
Style/medium: warm hand-painted storybook watercolor with deep walnut and
colored ink contours, visible ivory paper fibers, irregular brush edges,
tactile blackened iron and restrained ember orange; background one to two
contrast steps below the subject.
Composition/framing: horizontal approximately 5:4; exactly one main creature;
broad thumbnail-readable silhouette; all mechanism-critical details fully
inside the central 80%; outer 7% contains no essential detail; generous
breathing room; no crop.
Constraints: the armor shell itself is non-human life. No wearer, skin, face,
eyes, hair, flesh, five-finger hands, human waist or hips, boots, clothing,
robe, or human anatomy. Use empty cavities, separated forged plates,
restrained ember soul-filaments, furnace grates, three-pronged clamps and
wedge-shaped supports. Avoid robot styling, pistons, wiring, gear piles,
steampunk, demon traits, gothic spikes, humanoid knights or workers,
symmetry-heavy mech design, and reference-subject duplication. No card frame,
UI, title strip, text, letters, numbers, legible writing, runes, heraldry,
emblem, logo, signature, or watermark.
```

## 1. 铜环学徒 / `copper_ring_apprentice`

- ImageGen 源文件名：`call_PS6jVSBeWYSsB1dfL81y9hu5.png`
- 最终路径：
  `masters/minions/forge-soul/copper-ring-apprentice.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create one small apprentice Forge Soul whose armor shell
itself is the living creature. Build an original low-tier silhouette around an
open tilted copper-ring collar and three separated, soot-dark iron plates,
with a narrow orange ember cavity visible through the center. One compact
three-pronged clamp carefully fastens a glowing copper reinforcement ring
toward a small abstract allied shield-stone on the left, visually expressing
“at battle start, give the left ally health.” The other clamp holds a plain
riveting peg. Use two uneven wedge-shaped supports and restrained ember
soul-filaments between separated plates. The creature should feel diligent,
modest, and newly forged, never humanoid.
Scene/backdrop: quiet caravan forge workbench at dusk, low-contrast bellows,
copper scraps and a soft kiln glow; no banners or readable marks.
Additional constraints: do not copy any anchor shield body. The allied
shield-stone is a subordinate mechanism prop, not another creature.
```

## 2. 余烬刻师 / `ember_engraver`

- ImageGen 源文件名：`call_X1LjFqsEXnbjNA7A0aXT62VS.png`
- 最终路径：`masters/minions/forge-soul/ember-engraver.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one compact non-human Forge Soul artisan whose
shell is an asymmetrical low wedge-shaped engraving press, not a person. Three
separated soot-black forged plates surround an empty ember-lit cavity with a
narrow horizontal furnace grate. One long three-pronged clamp holds a cracked
allied shield fragment flat against a small anvil shelf, while the other clamp
guides a single glowing chisel that inscribes a warm orange reinforcing groove
across the crack, expressing “when an allied Forge Soul loses shield, restore
health; the first engraving becomes permanent.” Give it three uneven wedge
supports, a distinctive sloped back plate, and restrained ember
soul-filaments joining the parts. The creature should read as patient
precision and restoration, not combat.
Scene/backdrop: quiet soot-marked engraving corner of a caravan forge,
low-contrast trays of blank metal slivers and a dim kiln; no extra creatures
and no readable marks.
Additional constraints: avoid a humanoid blacksmith, apron, ordinary hand, or
tool-wielding person.
```

## 3. 破盾刃胚 / `shieldbreaker_blade_blank`

- ImageGen 源文件名：`call_c9pNljhkhAVd8698spAVJNR9.png`
- 最终路径：
  `masters/minions/forge-soul/shieldbreaker-blade-blank.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one non-human Forge Soul whose living body is
a broad unfinished blade-blank chassis held upright at a diagonal by three
separated blackened-iron shell fins. A small orange furnace grate is recessed
into the thick blunt blade spine; there is no face and no wearer. A curved
protective shell plate is visibly intact in front, while hairline fractures
across the blade release several controlled ember ribbons that stream backward
toward three faint, non-creature shield shapes in the distant formation,
expressing “Shield; on death, all allies that still have shield gain attack.”
Use two low asymmetrical wedge supports and one compact three-pronged clamp
gripping a plain quenching tong; maintain a heavy, blunt, workshop-made
silhouette distinct from a knight or sword-wielding humanoid.
Scene/backdrop: low-contrast quenching yard beside a caravan forge, dark water
trough and cooling iron slabs, dusk smoke; no other creatures and no readable
marks.
Additional constraints: avoid an ordinary floating sword or humanoid knight.
The distant shield shapes are low-contrast props only.
```

## 4. 盾墙执炉者 / `shieldwall_furnace_keeper`

- ImageGen 源文件名：`call_cewuIqkOBnxdqmDg8qo7iu6I.png`
- 最终路径：
  `masters/minions/forge-soul/shieldwall-furnace-keeper.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one broad, low non-human Forge Soul built as a
living portable furnace-wall, not a humanoid guard. Its silhouette is a
horizontal dark-iron heatbox with an off-center rectangular orange grate, two
widely separated curved buttress plates facing outward, and three wedge-shaped
supports. Two short three-pronged clamps extend toward the left and right edges
to catch the broken rim of two small abstract shield slabs positioned as
mechanism props, while a warm reinforcing ember pulse travels from the central
furnace into both slabs, expressing “the first adjacent ally to lose shield
gains +1/+2 this battle.” Keep the two shield props low and subordinate, never
full creatures. Add one tall soot vent plate on only one side for asymmetry and
recognition.
Scene/backdrop: low-contrast defensive forge lane with stacked blank plates
and muted kiln smoke at dusk; no other creatures and no readable banners.
Additional constraints: avoid tank styling and do not copy the shield-squire
body.
```

## 5. 逆流铸师 / `counterflow_smith`

- ImageGen 源文件名：`call_y7Z2kc2zF5bzewq9GKGv74rl.png`
- 最终路径：`masters/minions/forge-soul/counterflow-smith.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one non-human Forge Soul smith designed around
an inverted crucible shell suspended above a low split anvil base. Its armor
shell is the creature; the center is an empty dark cavity crossed by a narrow
orange furnace grate. A distinctive U-shaped open channel carries a restrained
ribbon of molten metal backward and upward from a cracked shield fragment into
the crucible, clearly showing reverse flow. One compact three-pronged clamp
steadies the fragment; a second asymmetrical clamp holds a short blocky forging
hammer, with no human hand. Two uneven wedge supports and one rear stabilizer
give a forward-leaning silhouette. The reverse molten stream first illuminates
the creature’s hammer edge, then the repaired shield, expressing “the first
two allied shield losses grant stats; the first also permanently strengthens
the smith.”
Scene/backdrop: low-contrast channel-forge floor with shallow cooling grooves
and a muted ember basin, dusk haze; no other creatures and no readable marks.
Additional constraints: avoid a humanoid blacksmith, apron, ordinary person
holding tools, or closed robotic torso.
```

## 6. 熔核执旗手 / `molten_core_standard`

- ImageGen 源文件名：`call_POQkMVmv7arXDUILklRiC9NT.png`
- 最终路径：`masters/minions/forge-soul/molten-core-standard.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one tall non-human Forge Soul whose living
body is an asymmetrical mobile standard-pylon, not a flag-bearing person. A
narrow vertical empty cavity is enclosed by three separated blackened-iron
banner plates; a rectangular molten core grate glows low in the torso-pylon.
From it, three restrained orange ember conduits climb to a split, blank metal
standard at the top and fan outward toward several small abstract shield
silhouettes in the distant background, visibly turning their weapon-like
edges warm, expressing “whenever a non-token ally gains a shield from an
effect, it permanently gains attack.” Give the pylon three widely spaced wedge
feet and one compact three-pronged service clamp folded against the side. The
blank metal standard must have no symbol, writing, heraldry, cloth, or human
proportions.
Scene/backdrop: low-contrast forge mustering yard with rows of plain shield
slabs and smoky sunset, no other living creatures and no readable banners.
Additional constraints: avoid a humanoid standard bearer or ordinary
flagpole.
```

## 7. 誓刃甲胄 / `oathblade_armor`

- ImageGen 源文件名：`call_wkoE3HIJzMdVeDnbQ5qjtelk.png`
- 最终路径：`masters/minions/forge-soul/oathblade-armor.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one non-human Forge Soul made from a hollow
crescent-shaped armor shrine. There is no wearer: three separated curved
blackened-iron plates form an open dark cavity around a narrow vertical orange
grate. A broad removable shield-shell is mounted across the front and is shown
at the instant of cracking; from behind it, one short retaliatory blade plate
snaps outward on an ember filament toward a plain impact block, visually
expressing “when this creature loses shield, deal 2 damage to the attacker.”
The blade must be part of the living shell, not held by a humanoid. Use three
low wedge supports arranged in an offset tripod and one small three-pronged
stabilizer clamp. Give the silhouette a distinctive open crescent and
side-sprung blade, unlike the shield squire or blade blank.
Scene/backdrop: low-contrast oath-forge alcove with plain chain links and an
unmarked stone testing block, warm dusk furnace haze; no other creatures,
shrines with writing, or banners.
Additional constraints: avoid a humanoid armor suit, knight pose, or ordinary
floating weapon.
```

## 8. 共鸣钟卫 / `resonance_bell_guard`

- ImageGen 源文件名：`call_D31jRxyc1nd2zM2hpnRIjAaf.png`
- 最终路径：`masters/minions/forge-soul/resonance-bell-guard.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one non-human Forge Soul whose living shell is
a wide split bronze-and-black-iron bell body suspended just above three low
wedge supports. The bell is empty—no wearer and no head—with a narrow orange
furnace grate visible high inside the open bell cavity. Two unequal flared side
plates act as protective acoustic wings. A single heavy internal clapper hangs
on restrained ember filaments and has just struck; two warm concentric
resonance waves travel horizontally toward small cracked shield-stone props on
both sides, and the props’ cracks visibly close, expressing “when an adjacent
ally loses shield and survives, it gains +1/+1 this battle.” Add one folded
three-pronged tuning clamp to the shorter wing. Keep the subject solemn, heavy,
and asymmetrical, not a humanoid bell knight.
Scene/backdrop: low-contrast bell-testing bay in a caravan forge with muted
hanging blank bells and smoke, no other creatures and no inscriptions.
Additional constraints: avoid church symbols, an ordinary bell tower, or a
humanoid guard.
```

## 9. 烬甲裁决者 / `cinder_armor_arbiter`

- ImageGen 源文件名：`call_vXnhqlggxK95GNvlWSF82CoR.png`
- 最终路径：`masters/minions/forge-soul/cinder-armor-arbiter.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one imposing non-human Forge Soul arbiter
whose body is an off-balance tripod of three tall overlapping armor slabs
around an empty ember cavity. The front slab is an intact dark shield-shell at
the instant it splits, exposing a bright narrow furnace grate and a single
forward orange cutting flare that conveys attack gained on shield loss. Along
the taller side slab, exactly three large plain rivet-seals form a vertical
sequence; the third has ignited into a lasting copper-orange reinforcement
band, conveying “after the third allied shield loss, permanently gain +2/+1.”
The rivet-seals are physical fasteners, not numbers or runes. One short
three-pronged clamp holds a broken shield shard like evidence; three uneven
wedge feet give a judicial but clearly non-humanoid silhouette.
Scene/backdrop: low-contrast forge judgment platform with plain stacked armor
plates and three dim braziers, smoky dusk; no people, extra creatures, writing,
throne, or banners.
Additional constraints: avoid a humanoid judge or knight and avoid a
scales-of-justice symbol.
```

## 10. 炉心圣盾官 / `hearth_core_aegis_officer`

- ImageGen 源文件名：`call_YpGaSbWtCGziVWhLjJC8xsqD.png`
- 最终路径：
  `masters/minions/forge-soul/hearth-core-aegis-officer.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one non-human Forge Soul command creature
shaped as a long asymmetrical shield-projector chassis, not a humanoid officer.
Its living shell comprises three separated blackened-iron plates around a
rectangular ember cavity and narrow furnace grate. The entire body leans toward
the left, where a large open C-shaped aegis emitter projects a translucent
warm shield around one small abstract allied shield-stone, clearly expressing
“at battle start, give the leftmost ally shield.” Behind the chassis, one
subdued ember from a fallen Forge Soul marker travels through an overhead
copper channel and blooms around a different small shield-stone, expressing
“when an allied Forge Soul dies, another is strengthened.” Props must remain
abstract and non-living. Use three offset wedge supports, one rear
counterweight plate, and a compact three-pronged signal clamp; no weapon.
Scene/backdrop: low-contrast command lane of a caravan forge with plain shield
racks and smoke, warm dusk; no people, other creatures, writing, insignia, or
banners.
Additional constraints: avoid cannon or tank styling, a humanoid officer,
knight pose, or ordinary shield bearer.
```

## 11. 鸣铁堡垒 / `ringing_iron_bastion`

- ImageGen 源文件名：`call_s1QN9hevLHxrJ0OI799jZmtJ.png`
- 最终路径：`masters/minions/forge-soul/ringing-iron-bastion.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one massive non-human Forge Soul built as a
low arched mobile bastion, not a humanoid fortress knight. Its body is a wide
bridge-like shell of separated blackened-iron voussoir plates around a large
empty passage; a horizontal orange furnace grate glows in the arch keystone.
Beneath the arch hangs one thick plain iron resonance bar, struck by a compact
three-pronged clamp. A strong warm ring wave travels out to two small abstract
allied armor slabs positioned beside the bastion, while two side-mounted
pressing plates close around them as if permanently reforging both neighbors,
expressing “Taunt; battlecry permanently gives adjacent minions +3/+3.” Use
four wedge buttress feet, but no legs, throne, face, or crown. The arched bridge
silhouette must be unmistakably different from the Furnace King.
Scene/backdrop: low-contrast open reforging yard with plain anvils and soot
smoke, no other living creatures, banners, towers, or readable marks.
Additional constraints: avoid robot or tank styling, humanoid knight, throne,
crown, or castle façade.
```

## 12. 断誓刃魂 / `oathbroken_blade_soul`

- ImageGen 源文件名：`call_y6erl38nhqXnTKUWdCeNJFqy.png`
- 最终路径：`masters/minions/forge-soul/oathbroken-blade-soul.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one fierce non-human Forge Soul whose living
body is a sweeping chain of five separated, broken blade-shell segments held
together by restrained orange soul-filaments, forming a low serpentine
crescent rather than a humanoid or ordinary sword. The thick central segment
contains a small furnace grate and empty black cavity. At the front, a curved
protective shell has just burst apart and the exposed blade segments blaze
brighter, expressing attack gained on shield loss. At the far end of the
motion arc, the foremost segment has struck a plain training effigy made only
of stacked blank iron blocks; orange fragments from the impact loop backward
and re-form a new shield-shell around the central grate, expressing “after
killing an enemy, regain shield.” Support the hovering chain with three small
wedge anchor-stones; add one compact three-pronged clamp integrated near the
central segment.
Scene/backdrop: low-contrast scorched forge dueling lane at dusk, heat haze and
plain iron debris; no people, other creatures, bodies, writing, or banners.
Additional constraints: avoid a humanoid swordsman, ordinary floating sword,
mechanical snake, animal skull, dragon head, or excessive fire.
```

## 13. 千环守墓者 / `thousand_ring_tomb_guardian`

- ImageGen 源文件名：`call_EZqpTdzGh4lQ6pNXei9wMyYG.png`
- 最终路径：
  `masters/minions/forge-soul/thousand-ring-tomb-guardian.png`

最终调用提示词的内容专属部分：

```text
Primary request: Create exactly one monumental non-human Forge Soul tomb
guardian whose living body is a broad horizontal sarcophagus chassis assembled
from many offset concentric iron rings and three separated tomb-door plates
around a deep empty cavity. There is no corpse, face, wearer, or humanoid form.
A low rectangular furnace grate glows inside the cavity. The outer rings are
beginning to separate in a solemn deathrattle: exactly two large warm
ring-halves travel outward and close around exactly two small abstract allied
shield-stones, while a quieter amber pulse spreads along the ground to several
distant plain armor slabs, expressing “on death, shield two surviving allies
and permanently strengthen all surviving non-token allies.” Give the chassis
four squat wedge buttresses and two folded three-pronged clamps tucked under
the side rings. Keep all ring surfaces blank, worn, and non-symbolic.
Scene/backdrop: low-contrast forge necropolis yard with plain stone plinths,
cooled slag and dusk smoke; no bones, corpses, people, other creatures,
readable tombs, banners, or gothic architecture.
Additional constraints: avoid robot or vehicle styling, skulls, humanoid tomb
knight, throne, crown, or castle façade.
```

## 技术检查结果

- 13/13 文件存在，均为 `1402×1122`、`Format24bppRgb`。
- 宽高比均为 `1.2496`，符合约 5:4 横构图。
- 每项均为一个主铸魂生命体；用于表达相邻、授盾或群体效果的小盾石、甲片与
  训练块只作为低对比机制道具。
- 未发现可读文字、字母、数字、UI、卡框、徽标、签名或水印。
- 未复制到 `sc/Assets`，未修改 Sprite Catalog。
