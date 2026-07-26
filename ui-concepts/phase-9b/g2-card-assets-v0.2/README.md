# G2 卡面资源 v0.2

本轮只重构卡面审核中构图失焦最明显的 3 张图。目标画幅为约
`5:4` 的横向卡图窗口，关键主体均约束在中央 80% 安全区；运行时
PNG 沿用既有路径和 `.meta`，因此不会改变 Unity GUID。

## 母版与运行时路径

| 对象 | 版本化母版 | Unity 运行时资源 |
| --- | --- | --- |
| 狐群巢母 | `masters/minion-fox-den-matriarch-landscape.png` | `Assets/Art/Presentation/Cards/Minions/WildSpirit/card_minion_fox_den_matriarch.png` |
| 双尾狐影 | `masters/token-two-tailed-fox-shadow-landscape.png` | `Assets/Art/Presentation/Cards/Tokens/card_token_token_two_tailed_fox_shadow.png` |
| 迅捷幼灵 | `masters/token-swift-young-spirit-landscape.png` | `Assets/Art/Presentation/Cards/Tokens/card_token_token_swift_young_spirit.png` |

## 生成工具与最终提示词

工具：OpenAI ImageGen，基于 v0.1 对应源图做精确对象编辑。

### 狐群巢母

> Recompose the same Fox Den Matriarch into a 5:4 landscape card-art
> composition. Preserve the leaf-and-bark mother fox, two smaller fox
> offspring, den opening, looping tail, moonlit woodland, and the original
> watercolor/gouache paper texture. Keep all identity-defining subjects
> inside the central 80% safe area. No frame, UI, text, logo, or watermark.

### 双尾狐影

> Recompose the same shadow fox into a 5:4 landscape card-art composition.
> Show exactly one fox with exactly two large tails. Keep the fox silhouette,
> face, two eyes, and both tails inside the central 80% safe area; preserve
> the dark moonlit watercolor atmosphere. Remove every stray amber glowing
> dot or eye-like light other than the fox's own two eyes. No frame, UI,
> text, logo, or watermark.

### 迅捷幼灵

> Recompose the same swift young spirit into a 5:4 landscape action
> composition. Preserve the exact leaf-eared seed-pod creature, glowing
> core, twig arms, root legs, and watercolor paper texture. Show it
> sprinting or leaping left-to-right with both legs off the ground and a
> trailing arc of flying leaves. Keep the full action silhouette inside the
> central 80% safe area. No frame, UI, text, logo, or watermark.
