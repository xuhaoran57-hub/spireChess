# 明亮绘本卡面机制压力测试 v0.1

- 日期：2026-07-28
- 人工复核更新：2026-07-29
- 范围：铸魂、荒灵、星契各 3 张，共 9 张
- 用途：验证种族语义、机制关系、亮度和实际卡框适配
- 视觉输入：仅使用冻结 Style Tile
- 玩法状态：校准数据，不写入正式卡池

## 结论

本批次整体未通过。9 张图片均满足 5:4 画幅和 v0.2 的整体亮度门槛，但自动
亮度合格没有保证主体—背景的局部分离。荒灵存在主体与背景同材质、同色阶和同
细节密度的问题；星契又因 v0.2 放开兽形、器物形和抽象生命而出现无设定依据的
异形化。

本批只保留为 v0.2 问题样本，不得进入量产。新生成任务改用
`../RACE-VISUAL-RULES-v0.3.md` 与
`../CALIBRATION-PROMPTS-v0.3.zh-CN.md`。

Unity 卡框 A/B 构建器已经接入，但当前机器未发现 Unity Editor，因此本轮尚未
生成 `.asset`、`.prefab`、`.unity` 和卡框截图。卡框可读性、普通/金色识别和
精确 Artwork 命中仍是进入全量生产前的待验门槛。

## 自动检查

计算口径：

- `light-mid`：Rec.709 灰度值大于或等于 0.45 的像素占比；
- `near-black`：Rec.709 灰度值小于 0.10 的像素占比；
- 缩略统计：最长边缩放至 400 像素后计算；
- 冻结门槛：`light-mid >= 50%`，`near-black < 12%`。

| 种族 | 卡面 | 尺寸 | 平均亮度 | light-mid | near-black | 自动亮度 |
|---|---|---:|---:|---:|---:|---|
| 铸魂 | 契盾工匠 | 1402×1122 | 0.615 | 72.7% | 0.3% | 通过 |
| 铸魂 | 裂响钟卫 | 1402×1122 | 0.679 | 84.1% | 0.1% | 通过 |
| 铸魂 | 余烬遗甲 | 1402×1122 | 0.665 | 82.9% | 0.2% | 通过 |
| 荒灵 | 山门角灵 | 1402×1122 | 0.595 | 72.5% | 0.0% | 通过 |
| 荒灵 | 落叶狐母 | 1402×1122 | 0.620 | 80.9% | 0.0% | 通过 |
| 荒灵 | 古根承生者 | 1402×1122 | 0.635 | 77.5% | 0.0% | 通过 |
| 星契 | 微光契术师 | 1402×1122 | 0.651 | 82.0% | 0.0% | 通过 |
| 星契 | 月轮寻秘者 | 1402×1122 | 0.653 | 82.4% | 0.0% | 通过 |
| 星契 | 命约回收师 | 1402×1122 | 0.651 | 76.9% | 0.1% | 通过 |

9 张图片宽高比均为 1.250，适合按 5:4 卡图视口裁切。表中“通过”仅代表
v0.2 自动画幅与整体亮度检查，不代表人工美术验收通过。

## 人工复核

### 铸魂

- 通过：三张分别采用工匠、钟卫和失效遗甲，未恢复“三爪锻造夹”、空心躯壳、
  固定炉栅或统一肢体数量。
- 通过：被封灵魂与铠甲/机关关系明确，护盾传递、失盾共鸣和亡语保护的动作方向
  可以不依赖文字辨认。
- 观察项：契盾工匠的两侧友方轮廓在紧凑卡框中可能被裁切，需要 Unity 截图确认。

### 荒灵

- 未通过：山口守卫、狐母和古根兽的主体都大量融合树叶、山石、藤蔓或根系，
  把可选内容误固化成了荒灵身体材料。
- 未通过：主体与山林背景共享相近色相、明度、纹理和边缘密度，缩略图下轮廓
  分离不足。
- v0.3 修订：荒灵改由山林身份、兽魂关系和生命循环识别，不再要求任何自然材料
  组成身体。

### 星契

- 未通过：三张卡对兽形、器物形、无脸面具和抽象生命的自由度过高，星契从
  学者/术士/契约职业阵营偏移为奇异生物阵营。
- 未通过：商店、法术页、丝带、战斗预览和次要角色同时出现，抢占主体对比和
  视觉面积。
- v0.3 修订：常规星契默认人形或亲和型类人角色，仪器全部作为外部工具，每张
  卡最多两个次要机制提示。
- 说明：命约回收师是机制与美术校准项。当前正式星契配置没有对应出售触发，不应
  在本轮写入正式玩法数据。

## Unity 隔离 A/B

构建器：

`sc/Assets/Editor/LightStorybookCardStressBuilder.cs`

Unity 菜单：

1. `Spire Chess/UI/Build Light Storybook Card Stress A-B`
2. `Spire Chess/UI/Build and Capture Light Storybook Card Stress A-B`

构建成功后生成：

- `Assets/Configs/Presentation/PresentationSpriteCatalog_LightStorybookMechanicStress.asset`
- `Assets/Prefabs/UI/Calibration/LightStorybook/PF_Card_MechanicStress.prefab`
- `Assets/Scenes/Calibration/LightStorybook/CardMechanicStressLightStorybookAB.unity`
- `Assets/Art/Presentation/Calibration/LightStorybookMechanicStress/`

截图输出：

- `ui-concepts/unity-validation/light-storybook-card-stress-v0.1/`
- 紧凑卡：普通与金色各 9 张；
- 完整卡：普通卡 9 张；
- 分辨率：1920×1080 与 1920×1200。

这些资源使用独立 Catalog、Prefab 和 Scene，不覆盖正式卡面资源或玩法配置。

## 进入全量生产的判定

当前状态：`自动画幅与亮度通过 / 人工美术验收失败 / 不执行 Unity 准入 /
不迁移正式卡池`。

本批 Unity 构建器继续保留用于问题复现，但不再作为量产准入样本。下一步先生成
v0.3 的三张星契与一张荒灵校准图，并满足：

1. 缩小至 160×128 后，主体在彩色和灰度下都清楚；
2. 主体与背景至少满足两项 v0.3 局部分离条件；
3. 荒灵主体不再默认融合植物、岩石或藤蔓；
4. 星契恢复可读的学者、术士和契约职业身份；
5. 每张图片最多两个次要机制提示；
6. 四张全部通过后，再进入 Unity 卡框验证。

## SHA-256

| 文件 | SHA-256 |
|---|---|
| `forge-soul/oath-shield-artisan-v0.1.png` | `CE32182C501054131B2947D6C098EF778FC91C714E5D5FAF645395A15755249C` |
| `forge-soul/crack-resonance-guard-v0.1.png` | `EE9C835CEFE0E5C0280B4B6107F508794B1C3D2D0F8A3EA36265894FB7C203CD` |
| `forge-soul/ember-reliquary-v0.1.png` | `5D5E7E4EF25868B9D5756D717D09F4CD7B8E46CB89F52C7C290F46E9771872D7` |
| `wild-spirit/mountain-gate-horn-spirit-v0.1.png` | `513A22E71AFA1F243F9C1B1C102883027FD157170DF737A5A32BD21FD3EB6512` |
| `wild-spirit/falling-leaf-fox-matriarch-v0.1.png` | `A05B69E39DD4BB3D91FF3CD467DD451BEB5D9EF2CBA8FA653D7B0579482844FA` |
| `wild-spirit/ancient-root-inheritor-v0.1.png` | `830D8DB64E3047333704243AA6A9AD88F848320177A00163A11AE75B82B94369` |
| `starbound/glimmer-contract-mage-v0.1.png` | `3AA4C28317D7DC694B03D45007B147698A6238635FC95285ABAFFDA79FDA8BE2` |
| `starbound/moonwheel-seeker-v0.1.png` | `A40CC2681C92EF37F235833BD27C3EC7B9A1CFDBE6AF529C837E6A0512473466` |
| `starbound/fate-reclaimer-v0.1.png` | `5CA81F4BBB1666FADB9D0B07C1844822C3EABB287C354027129CF835815CE60F` |
