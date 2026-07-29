# G4 正式视觉样板 ImageGen 提示词 v0.1

所有生成均同时引用：

- `ui-concepts/phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`
- `ui-concepts/phase-9c/full-art-production-v0.1/masters/minions/forge-soul/cinder-armor-arbiter.png`

第一张是唯一风格、色板、光照和气质参考；第二张只提供铸魂内容语义，不得影响
曝光、调色、气氛或其他场景的共同风格。

共同约束：旅团绘本、水彩与墨线、旧纸纹理、克制的靛蓝/青绿/赭金；禁止文字、
Logo、水印、UI 面板、卡框和 Mockup。

## Bright-style production override

Apply this block to every prompt below. Image 1,
`style-tile-d-wandering-storybook-v0.1.png`, is the sole reference for style,
palette, lighting and mood. Any character image is content reference only and
must not influence exposure, color grading or atmosphere.

Use diffuse morning-to-afternoon daylight and open chromatic shadows. At least
60% of each ordinary environment must remain light or middle-light; near-black
must remain below roughly 8%. Pale paper, sky blue, leaf green, warm ochre and
blue-gray must remain visible. Keep UI-safe areas quiet through restrained
detail, not reduced exposure.

No night, dusk, dark negative space, dark outer edges, forge-temple, soot haze,
black vignette, dominant brown/charcoal grading, or fire as primary/key light.

## backdrop_main_menu

Production-ready 16:9 main-menu hero: a colossal impossible spire rises from mist beyond a
winding woodland road, with a tiny caravan approaching. Put the strongest landmark on the
right and preserve a calm pale-sky and light-paper low-detail area on the left/center for
title and buttons without lowering exposure. Embed
subtle atlas motifs in sky and terrain; full bleed, no UI or text.

## backdrop_floor_map

Production-ready ultra-wide floor/map mural: left woodland trail and camp, center stone
bridge and merchant pavilion over a river, right volcanic fortress stairway and sealed gate.
Slightly elevated illustrated travel-map view, with a calm low-contrast center band. Do not
draw route nodes, paths, labels, icons or UI.

## backdrop_shop

Production-ready 16:9 traveling merchant workshop inside the spire in diffuse afternoon
daylight: open caravan tent,
old forge, shelves, maps, relic silhouettes, herbs, teal canopy and brass fittings. Keep
environmental detail at far edges and upper third, with a broad pale wood and parchment
central counter/table zone for cards and controls. Keep the center quiet through simple
shapes, not darkness. No foreground people, readable labels or UI.

## event_tranquil_grove

Production event illustration “Tranquil Grove”: a secret circular grove beneath a broken
spire dome, ancient pale tree around a glowing spring, bronze bells and faded ribbons,
moss-covered knight statues, and one kneeling traveler seen from behind. Quiet, uncanny and
hopeful in filtered daytime light; 4:3 composition, clear focal point and softly simplified
outer edges that remain chromatic and middle-light; no choices or UI frame.

## backdrop_battle

Production-ready 16:9 auto-battler arena: open-air old forge courtyard on a sunny mountain
route with two horizontal staging terraces. Use pale warm stone, painted weathered wood,
restrained iron braces, rust-red enemy cloth and dusty teal player cloth. Preserve two flat
uncluttered five-unit rows separated by a central gap. Threat and faction separation come
from silhouette, material and color, never from darkness or firelight. No characters,
cards, slot numbers, text or UI.
