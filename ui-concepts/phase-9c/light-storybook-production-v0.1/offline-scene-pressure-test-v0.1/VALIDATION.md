# 明亮绘本场景压力测试 v0.1

## 范围

- 分辨率：1920×1080。
- 商店：4 名随从商品、1 张法术商品、5 个战斗位、5 张手牌。
- 战斗：5 vs 5，使用 `BattleStandeeView` 的 160×240 几何。
- 卡池：Round 7 正式卡池 v0.3.2。

## 结论

### 商店

通过。

- 完整卡和紧凑卡的左上角费用均可读。
- 名称、种族、攻击和生命在真实密度下没有明显碰撞。
- 半透明纸色面板能够把卡牌从高细节绘本背景中分离出来。
- 商店部分可以进入明亮主题 v0.3.3 冻结候选。

### 战斗

离线验证通过，进入 v0.3.3 冻结候选。

1. `BattleStandeeView.ApplyPortrait` 当前设置 `preserveAspect=false`。
   已增加 `AspectRatioFitter.EnvelopeParent`，在 120×192 遮罩内进行居中覆盖裁切。
2. 新护盾改为浅蓝、象牙白的边缘高光结构，中心完全透明。
3. 护盾不再整体压暗立绘，并保持了状态辨识度。
4. 攻击、生命、金色框与亡语标记在 1920×1080 下可读。

## Unity 恢复后需要完成

- 运行 `BattleUiPrefabBuilder.BuildAndCapture()`，重建 Catalog、Prefab 与截图。
- 确认 Additive 材质下护盾亮度没有过曝。
- EditMode 与 PlayMode 测试通过后正式冻结 v0.3.3。

## 重新生成

```powershell
python render_scenes.py
```
