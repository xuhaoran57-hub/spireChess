# Token Refresh 生成 Prompt v0.3.4

- 日期：2026-07-31
- 工具：Codex 内置 ImageGen
- 模式：逐张新图生成；未使用定点编辑
- 输出：本目录 `masters/`
- 每张图单独调用；首轮 3 次，双尾狐影构图与肢体修订 2 次，共 5 次

## 图像参考及角色

唯一传入的图像参考：

`../../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`

它的角色仅为 `style reference`：控制明亮流浪绘本式水彩 / 水粉媒介、
象牙纤维纸、彩色墨线、自然日光、轻暖配色和开放曝光。不得复制参考图中的
人物、物件、内容、姿态、布局或构图。

没有把任何旧 Token 图或本批次前一张输出作为图像参考传入。三个角色之间的
物种一致性仅通过文字描述约束。

## 幼灵

```text
Use case: stylized-concept. Create a production game-card illustration master for
the Wild Spirit token “Young Spirit”, about 5:4 landscape, full bleed, with no
card frame or UI. Image 1 is the only style reference: use only its bright
wanderer's-storybook watercolor and gouache medium, colored-ink contours, warm
ivory fibrous paper, open daylight exposure, and restrained natural palette.
Do not copy any character, object, pose, content, layout, or composition from
the reference.

Show exactly one small fawn spirit in a bright woodland clearing. It must read
immediately as a juvenile deer: compact fawn anatomy, long slim legs, cloven
hooves, large leaf-shaped ears, a short tail, and no antlers. Give it pale
cream fur, soft moss-green ear tips and leaf markings, and one warm amber
life-glow in the chest. The expression is gentle, curious, and newly summoned.
Use a few floating leaves and a faint ground-level spirit trail only as
secondary motion accents. Do not use the old seed-pod creature design.

Keep the creature at roughly 55–65% of the frame, with its face, chest glow,
legs, and silhouette inside the central 80%. Keep the outer 7% free of
essential detail. At least 70% of the silhouette must read against a
low-detail light background. Favor bright and middle values; keep near-black
areas minimal.

No night, dusk, moon, dark vignette, horror, extra animal, antlers, humanoid
anatomy, card border, UI, button, panel, readable text, letters, numbers,
symbols, logo, signature, or watermark.
```

输出：
`masters/token-young-spirit-v0.3.4.png`

## 迅捷幼灵

```text
Use case: stylized-concept. Create a production game-card illustration master
for the Wild Spirit token “Swift Young Spirit”, about 5:4 landscape, full
bleed, with no card frame or UI. Image 1 is the only style reference: use only
its bright wanderer's-storybook watercolor and gouache medium, colored-ink
contours, warm ivory fibrous paper, open daylight exposure, and restrained
natural palette. Do not copy any character, object, pose, content, layout, or
composition from the reference.

Show exactly one small fawn spirit, the same species description as Young
Spirit: juvenile deer anatomy, pale cream fur, long slim legs, cloven hooves,
large moss-tipped leaf-shaped ears, short tail, subtle moss-green leaf
markings, no antlers, and one warm amber life-glow in the chest. Depict it in
a clear airborne sprint across a bright meadow, with the front legs reaching
forward and the rear legs driving off the ground. A sparse curved trail of
wind-borne green leaves should reinforce immediate attack and speed without
obscuring the body. Do not use the old seed-pod creature design.

Keep the creature at roughly 55–65% of the frame. Its face, chest glow, hooves,
and readable action silhouette must stay inside the central 80%; the outer 7%
contains no essential detail. At least 70% of the silhouette must read against
a low-detail light background. Favor bright and middle values; keep near-black
areas minimal.

No night, dusk, moon, dark vignette, horror, extra animal, antlers, humanoid
anatomy, duplicate body, card border, UI, button, panel, readable text,
letters, numbers, symbols, logo, signature, or watermark.
```

输出：
`masters/token-swift-young-spirit-v0.3.4.png`

## 双尾狐影 r1（用户否决）

```text
Use case: stylized-concept. Create a production game-card illustration master
for the Wild Spirit token “Two-Tailed Fox Shadow”, about 5:4 landscape, full
bleed, with no card frame or UI. Image 1 is the only style reference: use only
its bright wanderer's-storybook watercolor and gouache medium, colored-ink
contours, warm ivory fibrous paper, open daylight exposure, and restrained
natural palette. Do not copy any character, object, pose, content, layout, or
composition from the reference.

Show exactly one ancestral fox echo standing alert in a sunlit afternoon forest
glade. It has a slender russet-and-cream fox body with cool teal translucent
shadow edges, but it remains luminous and friendly rather than dark or
sinister. It must have exactly two large distinct tails: both tail bases must
be visible where they leave the body, and both separate tips must be easy to
trace. Add exactly two small amber leaf-light motes near the tails as a quiet
foreshadowing that its deathrattle summons two Young Spirits. Do not add any
other mote, fox, tail, or duplicate body.

Keep the fox and both complete tails at roughly 60–70% of the frame and inside
the central 80%. Keep the outer 7% free of essential detail. At least 70% of
the silhouette must read against a low-detail light background. “Shadow”
means only a local cool translucent echo around the fox; the overall scene is
bright afternoon daylight with dominant bright and middle values and almost
no near-black.

No night, dusk, moon, dark vignette, horror, third tail, hidden tail base,
extra fox, extra mote, humanoid anatomy, card border, UI, button, panel,
readable text, letters, numbers, symbols, logo, signature, or watermark.
```

输出：
`masters/token-two-tailed-fox-shadow-v0.3.4.png`

否决原因：两条尾巴在头部后方形成近似对称拱形，尾根像从肩背区域长出，
没有从骨盆自然承重和分叉。

## 双尾狐影 r2（用户否决）

```text
Use case: stylized-concept.
Asset type: production game-card illustration master for the Wild Spirit token
“Two-Tailed Fox Shadow”, about 5:4 landscape, full bleed, no card frame or UI.
Input images: Image 1 is the only style reference. Use only its bright
wandering-storybook watercolor and gouache medium, colored-ink contours, warm
ivory fibrous paper, open daylight exposure, and restrained natural palette.
Do not copy any character, object, pose, content, layout, or composition from
Image 1.

Primary request: Completely redesign the fox composition so the two tails are
anatomically attached to the rump and never appear to grow from the shoulders,
neck, head, or background.
Scene/backdrop: bright sunlit afternoon woodland meadow, low-detail behind the
silhouette.
Subject: exactly one slender russet-and-cream ancestral fox echo with subtle
cool teal translucent shadow edges; friendly and alert, not sinister. Show the
fox standing naturally on all four legs in a clear three-quarter side profile,
body running horizontally left-to-right. Place its head toward the right side
of the body and make its rear pelvis/rump clearly visible toward the
left-center.

Tail anatomy and composition are the highest priority: exactly two tails, both
visibly emerging side-by-side from one anatomically correct tailbone area at
the rear of the pelvis, directly behind and above the hind legs. Both tail
roots must be uncovered and traceable continuously from rump to tip. After a
short shared direction away from the rump, the tails diverge naturally: one
tail sweeps low and outward toward the lower-left, while the other rises in a
broad relaxed curve toward the upper-left. Keep both tails entirely behind the
rump and on the left side of the fox’s torso. The tails must not cross the
body, frame the head, form a heart/halo/wing shape, float separately, or appear
attached to the shoulders or neck. Preserve clear negative space between the
two tails. Use plausible fur weight, gravity, and flowing curves rather than
giant symmetrical plumes.

Add exactly two small amber leaf-light motes near the separate tail tips as a
quiet deathrattle hint. No other motes.
Composition/framing: full fox body, four paws, rump, both tail roots, both
complete tails and tips visible; subject occupies about 60–70% of frame; all
essential details inside central 80%; outer 7% free of essential details; at
least 70% of silhouette reads against a light low-detail background.
Lighting/mood: bright open afternoon daylight; dominant bright and middle
values; almost no near-black.
Constraints: exactly one fox, exactly four legs, exactly two anatomically
rooted tails, exactly two amber motes. No night, dusk, moon, dark vignette,
horror, third tail, hidden tail root, cropped tail tip, extra fox, extra mote,
duplicate body, wing-like tails, heart-shaped tails, humanoid anatomy,
readable text, letters, numbers, symbols, logo, signature, watermark, card
border, UI, button, or panel.
```

输出：
`masters/token-two-tailed-fox-shadow-v0.3.4-r2.png`

否决原因：尾根关系已改善，但腹部下方多生成一条后腿，画面合计五条腿。

## 双尾狐影 r3（当前候选）

```text
Use case: stylized-concept.
Asset type: production game-card illustration master for the Wild Spirit token
“Two-Tailed Fox Shadow”, about 5:4 landscape, full bleed, no card frame or UI.
Input images: Image 1 is the only style reference. Use only its bright
wandering-storybook watercolor and gouache medium, colored-ink contours, warm
ivory fibrous paper, open daylight exposure, and restrained natural palette.
Do not copy any character, object, pose, content, layout, or composition from
Image 1.

Primary request: Create one anatomically correct quadruped fox with exactly four
legs and exactly two tails. Correct limb count and clean tail attachment are
mandatory.
Scene/backdrop: bright sunlit afternoon woodland meadow, with a pale low-detail
area directly behind the full silhouette.
Subject: exactly one slender russet-and-cream ancestral fox echo with subtle
cool teal translucent edge accents, friendly and alert. Show the fox in a
simple calm standing side-profile pose on level ground, body horizontal and
facing right. No running, crouching, twisting, crossed legs, raised paws, or
foreshortened limbs.

LEG COUNT — highest priority: exactly four legs and exactly four paws, no more
and no fewer. Make all four legs separately visible and easy to count. The two
front legs descend only from the chest/shoulders, spaced slightly apart; each
ends in one distinct front paw. The two hind legs descend only from the
pelvis/hips, spaced slightly apart; each has a clear fox-like hock and ends in
one distinct rear paw. Leave a clean empty gap under the middle of the belly
between the front pair and hind pair. Do not place any leg, paw, stump, furry
extension, shadow, branch, or grass shape in that central belly gap. No
overlapping legs, no duplicate limb, no fifth leg, no extra paw. Before
finalizing the anatomy, visually count: front leg 1, front leg 2, hind leg 1,
hind leg 2 — total four.

TAIL ANATOMY — equally mandatory: exactly two tails. Both tail roots visibly
emerge side-by-side from the anatomically correct tailbone area at the rear of
the pelvis, directly above the two hind legs. Both roots remain uncovered and
each tail is continuously traceable to its own tip. The two tails extend behind
the rump toward the left, never across the torso or head: one rests in a long
low S-curve just above the ground, the other rises in one relaxed arc above it.
Keep clear negative space between them. The tails must not form a heart, halo,
wings, or symmetrical frame around the fox, and must not appear attached to
the shoulders, neck, head, or background.

Add exactly two small amber leaf-light motes, one near each tail tip, and no
other motes.
Composition/framing: entire fox, all four paws, both tail roots, both complete
tails and tips visible; subject occupies about 60–70% of frame; all essential
details inside central 80%; outer 7% free of essential detail; clean readable
silhouette.
Lighting/mood: bright open afternoon daylight; dominant bright and middle
values; almost no near-black.
Constraints: exactly one fox; normal fox anatomy; exactly four legs; exactly
four paws; exactly two tails; exactly two amber motes. No fifth leg, extra
limb, extra paw, leg-like fur clump, duplicate body, third tail, hidden tail
root, cropped tip, extra fox, extra mote, night, dusk, moon, dark vignette,
horror, readable text, letters, numbers, symbols, logo, signature, watermark,
card border, UI, button, or panel.
```

输出：
`masters/token-two-tailed-fox-shadow-v0.3.4-r3.png`
