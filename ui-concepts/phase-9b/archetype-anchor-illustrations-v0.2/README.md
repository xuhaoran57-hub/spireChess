# Phase 9B 四类锚点立绘 v0.2（铸魂非人修正 v0.5）

- 日期：2026-07-24
- 生成方式：Codex 内置 imagegen
- 共同风格：旅团绘本（Wandering Storybook）v0.3
- 原图规格：8 张 RGB PNG，均为 1024×1536
- 状态：8 张立绘盲测通过，方向暂定冻结；不是 Runtime Ready

## 目标

这批立绘用于冻结四类内容的一级形状、材质和色群。8 张立绘盲测通过后，已作为 `../archetype-card-blind-test-v0.2/` 的实际卡面集成源。它们不继承 v0.1 尚未确认的具体角色造型，只保留配置中的角色与机制语义。

2026-07-24 项目方补充确认：**铸魂不是人类或穿甲人类，而是炉芯灵体寄宿于锻造外壳形成的非人生命；金属外壳本身就是身体。** 此规则覆盖此前未明确身体类型的旧提示词。

| 类别 | 一级形状 | 低阶/第一锚点 | 高阶/第二锚点 |
| --- | --- | --- | --- |
| 铸魂 | 非人空甲/炉体；封闭直边黑铁块体 | 铸魂盾侍 | 不熄炉王 |
| 荒灵 | 分叉、破边的有机曲线 | 幼鹿灵 | 万蹄奔潮 |
| 星契 | 轻薄、开放的圆弧与纵向织带 | 星盘校准师 | 天穹契约者 |
| 旅团 | 轻装、实用、非对称的拼装负重 | 行脚医师 | 百技学徒 |

共同细节预算：一个清晰主体、一个类属大形、最多一至两个辅助叙事点；背景比主体低一至两档对比。

铸魂共同物种规则：不得出现皮肤、人脸、头发、耳朵、眼白、肉体、五指手、人类靴子及完整人体腰胯结构；用空腔、断开的锻铁板、炉火魂丝、三爪夹手、楔形支足与炉栅表达生命。避免科幻机器人、齿轮蒸汽朋克、骷髅恶魔和哥特尖刺。

## 主文件

| 文件 | SHA-256 |
| --- | --- |
| `masters/forge-soul-shield-squire.png` | `6e85be99b0dac591e9c81d8adb5397839b14e7af8f4b366d9774c6d5e9c68664` |
| `masters/forge-undying-furnace-king.png` | `912e6ab1763993df087d7436b3784533df3b665028461fa9e27c6a90a9ebdf41` |
| `masters/wild-young-deer-spirit.png` | `34ced80f6a3428a0ddc41f8b4ae80ead5d5a039f8b14d4a0d738b9329a80f85b` |
| `masters/wild-ten-thousand-hoof-surge.png` | `32835879a5a470159d25e4240d3e2714defce89bca688e1ffc85e1735e0355ee` |
| `masters/star-astrolabe-calibrator.png` | `05eef768d11f2f509e4d0a48d732ad46ced8dd4939b8f91bc62c8e39899d87e6` |
| `masters/star-sky-covenant-bearer.png` | `88755c4da82cbdc45ca4b5c4b259a28e892fa7cd022d5443c79dddfd413a502c` |
| `masters/wayfarer-traveling-physician.png` | `f6fab09efe0c9e76c426c12ff1f6f46545201a222278cf4efef441df9d3a1f36` |
| `masters/wayfarer-many-arts-apprentice.png` | `aeacbf49f5c25b95512fde0b4693677dd2e26822d09a217df2c09cec6519883e` |

## 评审文件

| 文件 | 用途 |
| --- | --- |
| `review/eight-illustrations-review-v0.2.png` | 八图统一方向与配对关系评审 |
| `review/thumbnail-review-160x240-v0.2.png` | 160×240 原尺寸可读性检查 |
| `review/thumbnails-160x240/*.png` | 八张精确 160×240 缩略图 |

评审图由 `tools/compose_archetype_anchor_review.py` 确定性生成。

## 相比 v0.1 的收敛

- 铸魂：改为非人炉芯寄魂体；盾侍采用“盾面即躯干”的盾生空甲，炉王回到最初卡面的端坐王者、炉火拱、圆形炉芯与护盾网络构图，但改成内部无穿戴者的威严空甲。v0.5 进一步使用低机位、加宽肩甲、放大炉冠、加高阶梯王座并压暗背景，使其从沉稳守卫提升为压场君王。同步移除人脸、人体腰胯与布质衣物，并降低碎线和满屏烈焰。
- 荒灵：移除萌系种子陪体和鬼兽脸群；根、叶、树皮语法不再只依赖鹿角。
- 星契：去掉厚重机械底座、满袍星点和封闭光环；圆弧保持轻薄、开口和负空间。
- 旅团：道具收成两个主识别点；学徒改用钝头训练杆和无金属木盾，避免误读为铸魂战士。

## 生成与许可说明

- 共同参考：`../style-tiles/style-tile-wandering-storybook-v0.3.png`。
- 每类第二张使用本批第一张作为同类媒介和一级几何参考，不复用脸、姿态或具体装备；不熄炉王 v0.4 另参考最初源图 `sc/Temp/phase9b-card-composite/undying-furnace-king.png` 的构图与机制语义。
- 铸魂两张已按项目方“非人”纠正重做。不熄炉王最终威严强化版留档于 `revisions/forge-imperious-sovereign-v0.5/`；端坐空甲 v0.4、行走炉体 v0.3 和人类/人形旧稿分别保存在相邻的历史版本目录中，不再进入当前评审图。
- 天穹契约者与百技学徒各进行一次受控编辑，分别降低星饰密度、消除战士化武器。
- 完整提示词见 `PROMPTS.md`。
- 2026-07-24 项目方已按个人版 OpenAI Terms of Use 确认本目录 8 张当前 master
  的生产许可；它们仍须完成 G2 Unity 编译、自动化与双分辨率运行验收，验收前不得
  标记为 Runtime Ready。相邻历史修订稿不在本次 28 项许可范围内。
