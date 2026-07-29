# 明亮主题 v0.3.3 冻结包

- 日期：2026-07-29
- 状态：离线冻结通过，Unity 运行时验收待执行
- 视觉源：明亮旅行绘本 Style Tile
- 内容源：正式卡池配置与单卡语义

本目录把 v0.3.3 的五项工作收束为一个可追溯、可重复校验的交付包，不复制大图，
也不覆盖正式 Runtime 美术资源。

## 包含内容

1. `BRIGHT-STYLE-RULES-v0.3.3.md`
   冻结亮度、主体分离、阵营边界、卡框费用、战斗居中裁切和明亮护盾规则。
2. `PRODUCTION-PROMPTS-v0.3.3.zh-CN.md`
   提供随从、法术、主菜单、商店和地图的现行生产 Prompt。
3. `VISUAL-BASELINES-v0.3.3.json`
   登记 Style Tile、15 张正式卡、四张卡矩阵、商店与战斗场景及护盾资源的
   尺寸、哈希和结构契约。
4. `validate_freeze.py`
   离线检查文件哈希、画幅与亮度、费用结构、Prompt 漂移词、战斗裁切、
   护盾透明度及 Catalog 绑定。
5. `UNITY-HANDOFF-v0.3.3.md`
   Unity 恢复后的构建、测试、截图与放行清单。

## 运行校验

在仓库根目录执行：

```powershell
python ui-concepts/phase-9c/light-storybook-production-v0.1/freeze-v0.3.3/validate_freeze.py
```

校验结果写入 `FREEZE-VALIDATION-REPORT-v0.3.3.json`。离线通过只代表冻结包与
当前工作区一致；Unity Additive 材质、Prefab 序列化和 EditMode/PlayMode 仍须按
交接清单验收。
