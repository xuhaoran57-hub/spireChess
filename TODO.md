# 项目待办

版本：0.1  
当前目标：完成 Unity 项目准备阶段收尾，并进入阶段 1 战斗原型。

## P0：阶段 0 收尾

- 创建 Unity 2022 LTS 2D 项目。
- 建立 Unity 目录结构：
  - `Assets/Configs/Json/`
  - `Assets/Scripts/`
  - `Assets/Scenes/`
  - `Assets/Prefabs/`
  - `Assets/Art/Placeholders/`
- 接入 TextMeshPro。
- 接入 Newtonsoft Json。
- 将以下配置放入 Unity：
  - `minions.v0.1.json`
  - `spells.v0.1.json`
- 实现 `ConfigService`，读取随从和法术 JSON。
- 实现配置校验：
  - 随从 ID 不重复。
  - 法术 ID 不重复。
  - 随从数量为 52，其中 Token 为 2。
  - 法术数量为 15。
  - `tier`、`race`、`keywords` 合法。
- 实现最小存档接口：
  - `ISaveStorage`
  - `FileSaveStorage`
  - 使用 `Application.persistentDataPath`

## P0：阶段 1 战斗原型

- 创建 `BattleTest` 场景。
- 创建卡牌 UI Prefab。
- 从 `MinionConfig` 生成卡牌显示。
- 显示双方各 5 个战斗格。
- 支持拖拽调整站位。
- 实现运行时随从对象：
  - 当前攻击
  - 当前生命
  - 永久攻击加成
  - 永久生命加成
  - 护盾状态
  - 是否金色
- 实现最小自动战斗：
  - 从左到右行动。
  - 普通攻击。
  - 嘲讽优先。
  - 护盾抵挡伤害。
  - 死亡移除。
  - 胜负判断。
- 实现战斗日志。

## P1：后续系统

- 实现溅射规则：
  - 普通溅射对目标左右两侧造成 50% 伤害。
  - 金色溅射对目标左右两侧造成 100% 伤害。
- 实现 Token 召唤、召唤失败和立即攻击。
- 实现 `OnPlay` 战吼触发。
- 实现金色三连合成。
- 实现发现牌。
- 实现商店刷新、购买、出售和酒馆升级。

## 暂缓

- 完整地图。
- 完整商店 UI。
- 所有随从效果 DSL。
- 微信小游戏打包。
- 正式美术和动画。
