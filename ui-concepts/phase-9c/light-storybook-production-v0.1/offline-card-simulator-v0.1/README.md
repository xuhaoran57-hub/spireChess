# 明亮绘本卡框离线模拟器 v0.1

本目录提供不依赖 Unity 的卡框排版检查工具，用于验证 Round 7 的 15 张正式卡池样本。

## 使用

1. 直接用浏览器打开 `index.html`。
2. 通过“普通/金色”“紧凑/完整”切换卡框状态。
3. 选择单卡并拖动“主体焦点 Y”，检查图片纵向裁切。
4. 右侧同步显示对应的 15 张卡矩阵。

如果修改了卡牌规格、焦点或排版脚本，执行：

```powershell
python render_matrices.py
```

脚本会重新输出：

- `matrices/matrix-normal-compact.png`
- `matrices/matrix-golden-compact.png`
- `matrices/matrix-normal-full.png`
- `matrices/matrix-golden-full.png`

## 对齐范围

- 卡框、金币、等级、攻击和生命标签：直接读取项目现有 PNG 资源。
- 字体：直接读取项目现有 Noto Sans CJK SC 字体。
- 卡牌矩形与层级：按 `CardView.ApplyLayout` 的 160×240 / 240×360 几何复刻。
- 费用：紧凑和完整布局均显示；当前随从固定为 3 费，法术读取规格中的费用。
- 金色数值和规则：读取 `FORMAL-CATALOG-SPECS-v0.3.2.json`。
- 法术遵守 Runtime 约束，没有金色形态；金色矩阵中的三张法术保持普通卡框。

## 已知边界

这是离线视觉模拟器，不替代 Unity 运行时验收。浏览器的文字测量、滤色与 TextMeshPro
仍可能有少量差异；四张 PNG 矩阵由 Pillow 固定渲染，适合评审留档和逐轮对比。
