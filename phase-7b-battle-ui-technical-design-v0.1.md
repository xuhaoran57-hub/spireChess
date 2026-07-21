# 阶段 7B 正式战斗 UI 纵向切片技术方案 v0.1

日期：2026-07-21
状态：已完成 Unity 资产生成、正式场景接线、legacy 回退移除、全量回归与双分辨率验收

## 1. 目标

将 BattleTest 从运行时动态搭建界面迁移到 Prefab 和序列化引用构成的正式战斗界面，并复用阶段 7A 已冻结的 PF_Card。玩家应能清楚看到攻击、伤害、护盾破裂、死亡、召唤和战斗结果，同时保持现有战斗结算、单局状态和随机确定性不变。

## 2. 边界

本阶段包含：

- PF_BattleSlot、PF_BattleScreen 和可复现 Editor 构建入口；
- 玩家与敌方各 5 个固定战斗槽；
- BattleCardViewModelFactory 和只读 BattleScreenState；
- 稳定的战斗运行时实例 ID；
- 结构化战斗播放事件；
- 1×/2× 播放速度和跳过表现；
- 攻击、伤害、破盾、死亡、召唤与结果反馈；
- 正式场景接线、EditMode/PlayMode 回归和双分辨率截图。

本阶段不包含：

- 卡牌规则、数值、牌池、遭遇或 fixture 修改；
- 新效果 DSL；
- 正式美术、音频、粒子特效和复杂 Timeline；
- 奖励、地图、事件、锻造、恢复界面重制；
- Phase 6B S1/S2。

### 2.1 本次冻结边界（2026-07-21）

本次提交冻结以下代码基础，不再继续扩展 Phase 7B 功能范围：

- `BattleMinionRuntime.RuntimeInstanceId` 与 `BattlePlaybackEvent` 数据契约；
- `BattleSimulator.SimulatePlayback()` 的结构化事件采集，且不改变普通模拟结果、日志与确定性哈希输入；
- `BattleCardViewModelFactory`、`BattleScreenState`、`BattleScreenStateBuilder` 与 `BattleScreenView`；
- `BattleTestController` 的正式 View、1×/2× 与跳过表现接口；
- `BattleUiPrefabBuilder` 的 Prefab、预览场景、正式场景接线与截图生成入口；
- 不依赖已生成 Prefab 的纯 C# / EditMode 测试基础。

后续已在 Unity 2022.3.62f3c1 中完成以上冻结边界：

- 生成并提交 `PF_BattleSlot`、`PF_BattleScreen` 与 `BattleUiPreview.unity`；
- 完成 `BattleTest.unity` 正式序列化接线；
- 增加 Prefab 层级、绑定、布局、重复渲染和正式场景唯一性 EditMode 回归；
- 增加拖拽换位、速度按钮和跳过只结算一次的输入级 PlayMode 回归；
- 删除 Controller 中运行时创建 Canvas、槽位、卡牌和 EventSystem 的 legacy 路径；
- 输出并检查 1920×1080、1920×1200 截图，修复战斗卡牌锚点偏移；
- 最终全量 EditMode 229 / 229、PlayMode 20 / 20 通过；
- 交互验收发现 `LogPanel` 覆盖顶栏射线，导致准备阶段“开始战斗”不可点击；将日志面板顶部收至顶栏下方 20px、关闭背景射线后复验通过，并增加几何、射线属性与 EventSystem 点击回归。

## 3. 核心约束

1. BattleSimulator 仍是唯一结算真源，UI 不重新计算伤害或目标。
2. 结构化事件只描述已经发生的结果，不驱动领域逻辑。
3. 卡牌名称、描述、身材、关键词和金色状态全部来自配置与 BattleMinionRuntime，不得写死单卡数据。
4. 跳过只停止表现并展示同一次模拟的最终结果，不允许重新模拟。
5. 非播放批次不采集表现事件，避免改变阶段 6 批次成本与确定性哈希。
6. 战斗场景只保留一个 Controller、一个 Canvas 和一个 EventSystem。

## 4. 表现数据契约

### 4.1 稳定实例 ID

BattleMinionRuntime.RuntimeInstanceId 在一次战斗内保持稳定：

- 玩家持有卡优先沿用 SourceInstanceId；
- 无持有实例的初始单位使用阵营、槽位和卡牌 ID 生成确定性 ID；
- 召唤物使用单次模拟内递增的召唤事件 ID；
- Clone() 必须保留该 ID。

该 ID 只服务战斗状态关联和动画，不写回商店收藏。

### 4.2 结构化事件

BattlePlaybackEvent 最小支持：

- CombatStarted
- RoundStarted
- AttackStarted
- DamageApplied
- ShieldGained
- ShieldLost
- StatsChanged
- UnitDied
- UnitSummoned
- CombatEnded

每个事件保存事件后的只读棋盘快照、来源/目标实例 ID、阵营与槽位、数值差量和显示消息。既有 BattleStep 与完整日志继续保留，避免破坏已有测试和离线诊断。

## 5. UI 状态与层级

### 5.1 BattleScreenState

只读状态包含：

- 标题、遭遇名、当前状态和回合；
- 玩家/敌方各 5 张 CardViewModel；
- 完整战斗日志；
- 开始、速度、跳过、重置/预设和返回按钮状态；
- 当前播放速度与是否已结算。

### 5.2 Prefab

PF_BattleScreen 包含 SafeArea、TopBar、Board、EnemyRow、PlayerRow、LogPanel 和 FeedbackLayer。玩家与敌方行各放置 5 个 PF_BattleSlot。

Canvas 契约与商店一致：参考分辨率 1920×1080，Match 0.5。卡牌使用 PF_Card Compact 模式。

## 6. 播放规则

1. AttackStarted：攻击者向目标短距离突进，攻击者/目标/溅射目标使用不同高亮。
2. DamageApplied：目标震动并显示伤害浮字。
3. ShieldLost：护盾徽章脉冲后消失。
4. StatsChanged：复用 CardView.PlayStatChange()。
5. UnitDied：卡牌淡出后清空槽位。
6. UnitSummoned：新实例在目标槽位缩放出现。
7. 1×/2× 只缩放表现等待时间。
8. 跳过后立即渲染 FinalState、完整日志和结果状态，再执行一次既有 FinalizeBattle()。

## 7. Controller 迁移

BattleTestController 保留领域协调职责：

- 构造初始棋盘和 BattleSimulator；
- 调用一次 SimulatePlayback()；
- 将结构化事件交给 BattleScreenView；
- 将最终结果提交给 RunSession；
- 保留测试所需的公开状态和立即结算入口。

移除运行时 CreateCanvas、CreatePanel、CreateText、CreateButton 和旧卡牌层级构建路径。View 只消费状态和事件，不访问 RunSession 或重新计算领域结果。

## 8. 自动化

### 8.1 EditMode

- 运行时实例 ID 在克隆和事件快照间稳定，召唤物 ID 唯一；
- 非播放模拟不创建表现事件；
- 攻击、伤害、破盾、死亡和召唤事件顺序与最终棋盘一致；
- BattleCardViewModelFactory 正确映射普通、金色、成长、关键词和护盾；
- Prefab 层级、序列化绑定、槽位数量和 Canvas 参数符合契约；
- 重复 Render 不泄漏旧卡牌实例。

### 8.2 PlayMode

- BattleTest 只有一个 Controller、Canvas 和 EventSystem；
- 正式卡牌可在开战前拖拽换位；
- 开始战斗后按结构化事件完成播放；
- 速度按钮在 1×/2× 间切换；
- 跳过表现只提交一次结果；
- 重载已结算战斗不会重复结算；
- 商店到战斗再返回流程保持同一 RunSession。
- 日志面板不与顶栏相交且背景不拦截顶栏按钮；正式开始按钮通过 EventSystem 分发进入播放流程。

## 9. 人工验收

- 1920×1080 和 1920×1200 无核心遮挡或裁切；
- 普通、金色、护盾、当前身材和死亡状态清楚；
- 玩家能指出攻击者、主目标和溅射目标；
- 伤害、破盾、死亡与召唤顺序可理解；
- 2× 与跳过不会改变胜负、永久成长或奖励；
- 长日志可滚动查看。

2026-07-21 交互式人工验收已完成；“开始战斗”遮挡问题修复后再次复验，无阻塞问题。

## 10. 完成定义

- BattleTest 使用序列化正式 PF_BattleScreen；
- 正式战斗卡复用 PF_Card；
- 领域模拟与 UI 表现分离；
- 结构化事件覆盖本阶段反馈；
- 不存在旧动态 UI 搭建路径；
- 全量自动化通过；
- 双分辨率截图和人工验收记录归档；
- 本阶段不修改规则和数值，因此不要求运行 Phase 6B。
