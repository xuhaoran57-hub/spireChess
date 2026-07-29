# Unity 交接清单：明亮主题 v0.3.3

- 目标 Unity：2022.3.62f3c1
- 项目：`sc/`
- 当前状态：离线冻结通过，Unity 验收待执行

## 进入 Unity 前

1. 在仓库根目录运行：

   ```powershell
   python ui-concepts/phase-9c/light-storybook-production-v0.1/freeze-v0.3.3/validate_freeze.py
   ```

2. 确认结果为 `PASS_OFFLINE_UNITY_PENDING`。
3. 确认没有把
   `validation-round-7-v0.3.2-formal-catalog/` 的图片复制到正式 Runtime
   Catalog。
4. 保留正式卡面护盾字段，战斗立牌只读取独立的
   `BattleStandeeShieldOverlay`。

## 构建顺序

1. 打开 `sc/`，等待首次导入和脚本编译结束。
2. 确认 Console 没有编译错误。
3. 执行 `Spire Chess/UI/Build Light Storybook Formal Catalog v0.3.2`。
4. 打开隔离场景：
   `Assets/Scenes/Calibration/LightStorybook/FormalCatalogV032.unity`。
5. 执行 `Spire Chess/UI/Rebuild and Capture PF_Card`。
6. 执行 `Spire Chess/UI/Rebuild and Capture Battle UI`。
7. 如需核对主题副本，执行：
   - `Spire Chess/UI/Build Light Storybook A-B Scene`
   - `Spire Chess/UI/Build Light Storybook Multi-Screen A-B`

## EditMode 验收

至少运行：

- `BattleUiPrefabTests`
- 卡框 Prefab、CardView 和 Presentation Catalog 相关 EditMode 测试
- 明亮正式 Catalog 构建器相关测试（若 Test Runner 中已发现）

必须确认：

- `PF_BattleStandee` 绑定完整；
- 肖像存在 `AspectRatioFitter`，模式为 `EnvelopeParent`；
- 肖像宽高比来自实际 Sprite；
- `BattleStandeeShieldOverlay` 命中
  `shield_overlay_bright_storybook_v1`；
- 普通与金色卡的紧凑/完整布局均显示费用；
- 隔离 Catalog 的 15 个 `artId` 全部 Exact 命中。

## PlayMode 与人工视觉验收

### 卡框

- 在 160×240 和 240×360 下检查普通与金色；
- 费用、等级、名称、规则、攻击和生命无碰撞；
- 三张法术在金色矩阵中仍保持普通形态；
- 15 张卡主体在实际裁切下不丢失头部和身份核心。

### 商店

- 1920×1080 下显示 4 名随从商品、1 张法术、5 个战斗位和 5 张手牌；
- 卡牌从背景中清楚分离；
- 费用在完整商品卡和紧凑战斗/手牌卡中均可读。

### 战斗

- 5 vs 5，立牌为 160×240；
- 原画不拉伸，主体采用居中覆盖裁切；
- 护盾中心透明，人物面部、姿态和阵营身份不被遮挡；
- Additive 材质下浅蓝/象牙边缘不应过曝成白块；
- 护盾关闭时不存在残留发光；
- 金色框、攻击、生命、嘲讽、亡语和溅射标记仍可读。

## 放行条件

以下项目全部通过后，才能把状态从“离线冻结”改为“Unity 最终放行”：

1. Console 零编译错误；
2. 相关 EditMode 测试全部通过；
3. 商店与战斗 PlayMode 无布局或材质问题；
4. 重新截图与离线基线的结构和明亮风格一致；
5. 正式 Runtime 卡牌美术没有被隔离 Catalog 覆盖；
6. `git diff` 中没有非预期的 Prefab、Scene 或 Catalog 序列化改动。

若任一冻结规则需要修改，创建 v0.3.4，不原地改写 v0.3.3 基线。
