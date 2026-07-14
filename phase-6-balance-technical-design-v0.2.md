# 阶段 6 平衡调试与体验迭代技术方案 v0.2

版本：0.2 设计稿
状态：统计与遥测基础设施、Unity 正式批次入口、成长速度校准链路、`R1-flourish-unity` S0/S1、`R2-shield-source-consolidation` S0 与 `R3-B02-B04-trigger-conversion` S0 已完成，待 S2 人工单局采样与 fixture v0.3 定标
Unity：2022.3.62f3c1
内容基线：`content-release 5.1.0`，64 个非 Token 随从、3 个 Token、16 张法术
自动化基线：Unity EditMode 111 / 111、PlayMode 14 / 14；`R1-flourish-unity`、`R2-shield-source-consolidation` 与 `R3-B02-B04-trigger-conversion` S0 确定性复跑通过
关联文档：`development-plan.md`、`phase-5c-content-pool-balance-technical-design-v0.1.md`、`minion-pool-expansion-rebalance-design-v0.2.md`、`phase-5-unity-acceptance-checklist.md`

## 1. 目标与边界

本阶段在 v0.2 功能联合人工验收通过后，恢复此前暂缓的正式数值工作。目标不是继续增加卡牌，而是回答以下问题：

1. 六套固定构筑在相同发育档位下是否存在明显固定最优解；
2. v0.2 扩池后，核心获取、三连和双核心成型是否过慢；
3. 正常发育与最高发育的差距是否仍处于可控范围；
4. 经济、路线、精英、Boss 和奖励是否形成有意义的取舍；
5. 两轮调整后是否可以冻结 MVP 的 v0.2 数值基线。

本阶段包含：

- 六套固定构筑的正常/最高发育战斗批次；
- 20 局固定种子的完整三层人工单局；
- 战斗样本、单局摘要、卡牌漏斗和调数日志的数据结构；
- 一轮极端值收敛和一轮整体收敛；
- 每轮后的全量自动化与受影响人工冒烟。

本阶段仍不包含：

- 新增随从、法术、遭遇、奖励或复杂遗物；
- 光环及死亡、移动、召唤后的动态刷新；
- 任意脚本化效果 DSL；
- 完整确定性命令重放；
- 启发式商店 AI 或无人值守批量单局；
- 正式 UI、美术、动画、音频和平台适配。

没有完整命令重放不阻塞本阶段：战斗批次使用固定构筑和固定种子；完整单局使用固定种子、原始 NDJSON 和人工决策摘要复核。

## 2. 统一口径

### 2.1 发育档位

- `fixtureVersion = 0.2.0` 的 `N` 是人工锁定的第 10 回合正常发育快照；`H` 使用全金色基础身材和 `N` 永久加成的 2 倍。它们只比较阵容成型后的战斗强度，不能证明真实成长速度。
- `fixtureVersion = 0.3.0` 的 `N` 改为可识别成型样本第 10 回合永久面板的 P50，`H` 改为 P90；两个档位的金色状态和永久加成逐槽独立记录，不再强制 `H = N × 2`。
- v0.3 同时记录各构筑第 10 回合成型率、首个/第二个核心回合、三连、施法、刷新和购买投入；战斗胜率与成型率分开评价。
- 初始 20 局 `S2_RUNS` 可以产生 provisional 结果，但每个构筑少于 10 个可识别第 10 回合样本时不得将校准状态标为 `Ready`，也不得替换当前 v0.2 夹具。
- `N` 和 `H` 都是可重复的代表夹具，不代表每局都必须精确形成该阵容。

### 2.2 面板与输出

- 永久面板：五个非 Token 随从的 `基础身材 + PermanentBonus` 总和，不含 Token、护盾和下一战临时加成。
- 战斗初始面板：永久面板加本文件指定的 `NextCombat` 临时覆盖层。
- 第一轮原始输出：战斗开始伤害与第 1 轮内所有攻击、反击、溅射和立即攻击发出的正数伤害之和，不扣护盾、减伤和过量伤害。
- 第一轮有效输出：同期实际扣除的生命之和，不计护盾吸收和过量伤害。
- 最高/正常输出比：同一构筑 `H / N` 的第一轮原始输出比。
- 对局得分率：`(胜场 + 0.5 × 平局) / 总场次`；原始胜、负、平仍分别保留。

### 2.3 镜像对局

`BattleSimulator` 每轮由玩家方先行动，因此任意 A/B 对局必须同时运行：

1. A 在玩家方、B 在敌方；
2. B 在玩家方、A 在敌方。

两种方向使用相同种子集合并后才计算 A 对 B 的得分率。任何单方向结果不得用于调数结论。

## 3. 六套固定构筑

### 3.1 构筑总表

| ID | 构筑 | 主机制 | `N` 永久面板 | `H` 永久面板 | 核心观察项 |
| --- | --- | --- | ---: | ---: | --- |
| `B01_SHIELD` | 护盾壁垒 | 补盾、嘲讽、护盾转移 | 40/50 | 80/100 | 护盾抵挡次数、补盾次数、循环上限 |
| `B02_BREAK` | 失盾反击 | 失盾成长、反击爆发 | 45/35 | 90/70 | 失盾次数、临时/永久成长、首轮爆发 |
| `B03_SUMMON` | 频率召唤 | 嵌套亡语、立即攻击 | 40/40 | 80/80 | 成功/失败召唤、立即攻击、非 Token 核心存活 |
| `B04_DEATH` | 亡语成长 | 非 Token 死亡、残局成长 | 40/50 | 80/100 | 非 Token 死亡、永久差量、残局核心 |
| `B05_SPELL` | 法术回响 | 施法成长、下一战强化 | 65/66 | 130/132 | 法术投入、首轮输出、稳定性 |
| `B06_REFRESH` | 经济刷新 | 刷新里程碑、金币与护盾 | 48/60 | 96/120 | 有效刷新、免费刷新、金币净收益、下一战护盾 |

六套名称只用于测试与设计复盘，不写回卡牌 `archetypes`，也不参与游戏内发现、奖励或效果判断。

### 3.2 固定站位与身材

下表中的槽位均为从左到右的 `0-4`。`N 永久加成`直接传入 `permanentAttackBonus` / `permanentHealthBonus`；`N 最终`和 `H 最终`用于自动校验夹具构造结果。

| 构筑 | 槽位 | 随从 ID | 名称 | `N` 永久加成 | `N` 最终 | `H` 最终 |
| --- | ---: | --- | --- | ---: | ---: | ---: |
| `B01_SHIELD` | 0 | `shieldwall_furnace_keeper` | 盾墙执炉者 | +2/+5 | 3/9 | 6/18 |
| `B01_SHIELD` | 1 | `resonance_bell_guard` | 共鸣钟卫 | +4/+5 | 6/10 | 12/20 |
| `B01_SHIELD` | 2 | `molten_core_standard` | 熔核执旗手 | +8/+3 | 11/7 | 22/14 |
| `B01_SHIELD` | 3 | `hearth_core_aegis_officer` | 炉心圣盾官 | +4/+5 | 8/11 | 16/22 |
| `B01_SHIELD` | 4 | `undying_furnace_king` | 不熄炉王 | +6/+5 | 12/13 | 24/26 |
| `B02_BREAK` | 0 | `oathbroken_blade_soul` | 断誓刃魂 | +8/+2 | 16/7 | 32/14 |
| `B02_BREAK` | 1 | `ember_engraver` | 余烬刻师 | +2/+3 | 4/6 | 8/12 |
| `B02_BREAK` | 2 | `undying_furnace_king` | 不熄炉王 | +3/+2 | 9/10 | 18/20 |
| `B02_BREAK` | 3 | `counterflow_smith` | 逆流铸师 | +3/+2 | 6/6 | 12/12 |
| `B02_BREAK` | 4 | `cracked_armor_avenger` | 裂甲复仇者 | +5/+2 | 10/6 | 20/12 |
| `B03_SUMMON` | 0 | `fox_den_matriarch` | 狐群巢母 | +4/+5 | 8/12 | 16/24 |
| `B03_SUMMON` | 1 | `hundred_song_herd` | 百鸣兽群 | +3/+4 | 7/8 | 14/16 |
| `B03_SUMMON` | 2 | `ten_thousand_hoof_surge` | 万蹄奔潮 | +5/+4 | 12/12 | 24/24 |
| `B03_SUMMON` | 3 | `many_branch_invoker` | 群枝唤灵者 | +5/+4 | 7/8 | 14/16 |
| `B03_SUMMON` | 4 | `rending_cub` | 裂爪幼兽 | +4/+3 | 6/4 | 12/8 |
| `B04_DEATH` | 0 | `rootbound_soul_guide` | 归根引魂者 | +2/+1 | 5/6 | 10/12 |
| `B04_DEATH` | 1 | `rotleaf_heir` | 腐叶承嗣 | +3/+2 | 5/6 | 10/12 |
| `B04_DEATH` | 2 | `ancient_moss_hatchling` | 古苔巨幼体 | +5/+4 | 8/9 | 16/18 |
| `B04_DEATH` | 3 | `ancient_mountain_spirit` | 群山古灵 | +6/+4 | 12/13 | 24/26 |
| `B04_DEATH` | 4 | `world_eating_final_bloom` | 终花吞世者 | +6/+4 | 10/16 | 20/32 |
| `B05_SPELL` | 0 | `glimmer_mage` | 微光术士 | +7/+12 | 9/13 | 18/26 |
| `B05_SPELL` | 1 | `rune_ward_reader` | 符文护读者 | +8/+8 | 9/12 | 18/24 |
| `B05_SPELL` | 2 | `echo_starchanter` | 回响咏星师 | +10/+9 | 13/12 | 26/24 |
| `B05_SPELL` | 3 | `secret_page_refractor` | 秘页折光师 | +10/+9 | 13/13 | 26/26 |
| `B05_SPELL` | 4 | `falling_star_prophet` | 陨星先知 | +15/+10 | 21/16 | 42/32 |
| `B06_REFRESH` | 0 | `stardust_attendant` | 星尘随侍 | +7/+7 | 8/10 | 16/20 |
| `B06_REFRESH` | 1 | `star_etched_timekeeper` | 星刻计时员 | +6/+6 | 8/9 | 16/18 |
| `B06_REFRESH` | 2 | `fate_track_recorder` | 命轨记录员 | +7/+7 | 9/12 | 18/24 |
| `B06_REFRESH` | 3 | `star_ring_treasurer` | 星环司库 | +7/+7 | 11/13 | 22/26 |
| `B06_REFRESH` | 4 | `sky_covenant_bearer` | 天穹契约者 | +8/+8 | 12/16 | 24/32 |

夹具要求：

- 每张非 Token 随从设置稳定且非空的 `SourceInstanceId`：`{fixtureId}-S{slot}`；
- v0.2 的 `N` 使用普通基础身材和表中永久加成，`H` 使用金色基础身材和表中永久加成的 2 倍；
- v0.3 每个槽位必须显式提供 `normalIsGolden`、`highIsGolden`、`highPermanentAttackBonus` 和 `highPermanentHealthBonus`；
- v0.3 文件必须引用 `balance_fixture_calibration.csv`，每个构筑必须提供样本量、成型率、P50/P90 代表种子和 `Ready` 状态；未达到样本门槛时夹具校验直接失败；
- 配置基础身材、表中加成和最终身材任一不一致时，夹具校验直接失败；
- 牌池副本数量不限制战斗夹具；完整单局仍严格使用有限牌池。

### 3.3 归一化商店输入标签与下一战覆盖层

固定战斗快照必须区分永久面板和下一战临时状态：

| 构筑 | `N` 标准输入 | `H` 标准输入 | 战斗快照覆盖层 |
| --- | --- | --- | --- |
| `B01_SHIELD` | 无额外商店输入 | 无额外商店输入 | 仅使用卡牌自身关键词和战斗开始效果 |
| `B02_BREAK` | 无额外商店输入 | 无额外商店输入 | 仅使用卡牌自身关键词和战斗开始效果 |
| `B03_SUMMON` | 无额外商店输入 | 无额外商店输入 | Token 由实际亡语产生，不预放 Token |
| `B04_DEATH` | 无额外商店输入 | 无额外商店输入 | 不预先写入永久差量 |
| `B05_SPELL` | 最终商店使用 2 张法术 | 最终商店使用 4 张法术 | `N`：槽 0 获得下一战护盾，槽 1 获得临时 +1/+4；`H`：槽 0 获得下一战护盾，槽 1、2 各获得临时 +1/+4 |
| `B06_REFRESH` | 最终商店成功主动刷新 4 次 | 最终商店成功主动刷新 6 次 | `N`：槽 0、3、4 获得下一战护盾；`H`：五个槽位均获得下一战护盾 |

说明：

- 3.2 的 `N/H` 身材是所有商店操作结束后的归一化永久面板；本节的“标准输入”只解释覆盖层来源，不是要由 `ShopSession` 重新执行的动作脚本；
- “主动刷新”包含玩家点击后消耗免费次数的刷新，不包含场景初始化或效果内部重建商店；
- 表中的永久加成已经包含标准输入产生的永久成长；构造静态夹具时不得再次重放施法或刷新，否则会重复计数；
- 覆盖层通过 `initialAttack`、`initialHealth` 和临时战斗关键词写入，不得计入 `PermanentBonus`；
- `B06_REFRESH_N` 将星环司库的两个随机护盾固定落在槽 3、4，槽 0 护盾来自天穹契约者；这是夹具输入的一部分，真实单局仍保留随机目标；
- 完整单局不强行指定覆盖层目标，必须由真实目标选择和商店状态产生；
- 若运行时真实目标规则与本表不一致，应先修正文档或夹具，禁止静默修改统计结果。

### 3.4 标准木桩与机制压力板

新增仅用于输出测量的 `D00_OUTPUT_DUMMY`：

- 五个槽位均使用 `wandering_swordsman` 作为无战斗效果载体；
- 每个载体 `initialAttack = 0`、`initialHealth = 500`；
- 不带嘲讽、护盾、溅射或其他永久关键词；
- 木桩不参与胜率统计，只用于记录不受反击干扰的战斗开始和第 1 轮纯输出；
- 木桩场景显式标记为不计入竞技安全统计，完整战斗产生的回合上限仅保留为测量口径诊断；
- 每个构筑的 `N`、`H` 均以玩家方对木桩运行，使用正式战斗种子集。

另建用于激活机制的 `D01_MECHANIC_PRESSURE`：

- 五个槽位同样使用无战斗效果的 `wandering_swordsman`；
- 对 `N` 快照使用 `initialAttack = 8`、`initialHealth = 500`；
- 对 `H` 快照使用 `initialAttack = 16`、`initialHealth = 1000`，使压力随面板等比例放大；
- 不带额外关键词，按正常攻击顺序施压；
- 只用于测量失盾、死亡、召唤、立即攻击、永久差量和核心存活，不参与构筑胜率；
- 第一轮输出上限和 `H/N` 输出比仍只取 `D00_OUTPUT_DUMMY`，不使用会提前击杀单位的压力板结果。

### 3.5 对局矩阵

正式战斗批次包含：

1. 六套 `N` 构筑两两对局，共 15 组，每组运行两个镜像方向；
2. 六套 `H` 构筑两两对局，共 15 组，每组运行两个镜像方向；
3. 十二个构筑快照分别对 `D00_OUTPUT_DUMMY`；
4. 十二个构筑快照分别对匹配档位的 `D01_MECHANIC_PRESSURE`；
5. 不运行 `N` 对 `H` 胜率，二者差距通过木桩输出比、机制压力板和真实完整单局观察。

主种子集下的总量为：

- `N` 两两对局：15 × 2 × 1000 = 30,000 场；
- `H` 两两对局：15 × 2 × 1000 = 30,000 场；
- 木桩输出：12 × 1000 = 12,000 场；
- 机制压力：12 × 1000 = 12,000 场；
- 每个正式候选合计 84,000 场。

### 3.6 完整单局核心判定

完整单局不读取已经停止使用的 `archetypes` 字段，使用版本化的 `coreClassifierVersion = "0.2.0"` 按本方案的测试清单离线判定。该映射与构筑夹具一起保存，不写入正式卡牌配置：

| 构筑 | 核心 A | 核心 B | 激活证据 |
| --- | --- | --- | --- |
| `B01_SHIELD` | 盾墙执炉者、共鸣钟卫、炉心圣盾官、不熄炉王 | 熔核执旗手、千环守墓者 | 同场发生至少 2 次友方获得或失去护盾 |
| `B02_BREAK` | 破盾刃胚、誓刃甲胄、裂甲复仇者、烬甲裁决者、断誓刃魂 | 余烬刻师、逆流铸师、裂甲复仇者、烬甲裁决者、断誓刃魂 | 同场发生至少 2 次友方失盾，且产生一次失盾收益 |
| `B03_SUMMON` | 幼鹿灵、双尾狐灵、狐群巢母、百鸣兽群 | 裂爪幼兽、疾羽林隼、群枝唤灵者、獠牙领奔者、万蹄奔潮 | 同场至少成功召唤 2 个 Token |
| `B04_DEATH` | 苔痕守苗、根须吞噬者、古苔巨幼体、腐叶承嗣、山腹吞灵者 | 归根引魂者、藤冠祭司、群山古灵、终花吞世者 | 同场至少触发 1 次非 Token 荒灵死亡收益 |
| `B05_SPELL` | 微光术士、符文护读者、回响咏星师、秘页折光师 | 星门讲师、陨光裁定者、陨星先知 | 同一商店阶段至少使用 2 张法术 |
| `B06_REFRESH` | 观星学徒、星刻计时员、命轨记录员 | 星尘随侍、星盘校准师、月轮调度者、星环司库、天穹契约者 | 同一商店阶段至少主动刷新 2 次 |

- `firstCoreTurn`：首次获得任一核心 A 或核心 B 持有实例的 `RunTurn`；
- `secondCoreTurn`：战斗区同时存在至少一个核心 A 和一个不同实例的核心 B，且已经满足激活证据的首个 `RunTurn`；
- 同一张同时属于 A/B 的卡不能单独完成双核心，必须存在两个不同持有实例；
- `dualCoreBeforeF3Boss`：进入第三层 Boss 节点前 `secondCoreTurn` 已产生；
- `coreSurvivalRate`：某构筑在战斗开始时属于核心 A/B 的非 Token 实例中，战斗结束仍存活的实例数占比；按构筑和候选聚合；
- 混合构筑可以同时满足多个构筑，`finalBuildId` 取触发次数和终局核心数量更高者；仍相同时记为 `Mixed`，没有满足任何双核心条件时记为 `Unclassified`，不得由测试者强行归类。

## 4. 固定种子

### 4.1 种子集合

| 集合 | 范围 | 数量 | 用途 |
| --- | ---: | ---: | --- |
| `S0_SMOKE` | 1000-1099 | 100 | Unity “模拟100场”、改动后快速回归 |
| `S1_CALIBRATION` | 1000-1999 | 1000 | R0、第一轮和第二轮正式战斗对比；包含 `S0` |
| `S2_RUNS` | 2000-2019 | 20 | 完整三层人工单局 |
| `S3_HOLDOUT_A` | 9000-9099 | 100 | 第二轮候选锁定后的第一组防过拟合复核 |
| `S4_HOLDOUT_B` | 9100-9199 | 100 | `S3` 失败并产生新候选时使用的预注册备用留出集 |

种子规则：

- 所有集合均为闭区间整数；`S0/S1/S3/S4` 逐场传给 `BattleSimulator(new Random(seed))`，`S2` 则将种子直接传给 `RunSession(seed)`，再由现有 `SeedDeriver` 派生商店、奖励、事件和战斗随机流；
- 两轮调整必须重复使用完整的 `S1_CALIBRATION`，不得替换不利种子；
- `S3_HOLDOUT_A` 在第二轮候选锁定前不查看，不据其单个样本反向调参；
- `S3` 失败后若继续调参，`S3` 自动转为已观察校准证据，新候选只能用未查看的 `S4_HOLDOUT_B` 决定冻结；若 `S4` 也失败，必须先预注册新的不重叠留出集再生成候选；
- 发现确定性问题时记录种子并修复根因，不从集合中删除；
- 每条结果同时记录 Unity 版本、内容版本和 Git 提交，跨运行时版本的结果不得直接合并。

执行量口径：每个正式候选的 `S1` 结果集为 84,000 场；R0 将第一次 `S0` 结果复用为 `S1` 的前 100 个种子，只额外执行一次 8,400 场 `S0` 确定性复跑，因此 R0 共执行 92,400 次模拟；每个留出集候选另执行 8,400 场。R0、R1、R2 各完成 20 局人工单局，总计至少 60 局。

### 4.2 20 局完整单局分配

| 种子 | 初始构筑意图 |
| --- | --- |
| 2000-2002 | `B01_SHIELD` 护盾壁垒 |
| 2003-2005 | `B02_BREAK` 失盾反击 |
| 2006-2008 | `B03_SUMMON` 频率召唤 |
| 2009-2011 | `B04_DEATH` 亡语成长 |
| 2012-2014 | `B05_SPELL` 法术回响 |
| 2015-2017 | `B06_REFRESH` 经济刷新 |
| 2018-2019 | 自适应混合构筑，不预设主机制 |

“构筑意图”只约束优先评估方向，不允许为了完成指定阵容而重开或修改随机结果。未找到核心、被迫转型和最终形成混合构筑都属于扩池体验数据，必须记录 `intendedBuildId` 与 `finalBuildId`。

人工单局采集通过 Unity 启动参数启用；进入运行场景后仍由测试者完成决策：

```powershell
Unity.exe -projectPath sc `
  -balanceRunSeed 2000 `
  -balanceRunOutput balance-results/phase-6-v0.2/R3-growth-calibration/S2_RUNS/raw
```

完成若干或全部种子后，运行离线聚合：

```powershell
Unity.exe -batchmode -nographics -projectPath sc `
  -executeMethod SpireChess.Editor.RunGrowthCalibrationCommand.RunFromCommandLine `
  -runTelemetryInput balance-results/phase-6-v0.2/R3-growth-calibration/S2_RUNS/raw `
  -runDecisionInput balance-results/phase-6-v0.2/R3-growth-calibration/S2_RUNS/decisions `
  -balanceCandidate R3-growth-calibration -balanceTuningRound R3 `
  -balanceOutput balance-results/phase-6-v0.2/R3-growth-calibration/S2_RUNS
```

## 5. 数据结构

### 5.1 现状与扩展原则

当前 `BattleBatchRunner` 只汇总场次、玩家胜、敌方胜和平局；`RunTelemetry` 使用 `schemaVersion = 1` 的通用 NDJSON 外壳。正式调数前需要补充诊断计数和离线聚合，但不破坏已有原始事件：

- 原始 NDJSON 继续使用现有外壳；
- 新增事件使用向后兼容的 payload；
- 战斗与卡牌漏斗聚合继续使用 `balanceSchemaVersion = "0.2.0"`；增加第 10 回合流派/投入字段后的单局汇总和成长校准表使用 `0.3.0`；
- `configHash` 为随从、法术、遭遇、奖励和发布清单规范化 JSON 按固定顺序拼接后的 SHA-256，用于阻止同名候选混入不同配置；
- CSV 使用 UTF-8、英文列名、`.` 作为小数点、空值留空；
- 所有百分比在文件中保存 `0-1` 小数，展示时再格式化为百分数。

### 5.2 构筑夹具表 `balance_fixture_slots.csv`

一行表示一个槽位：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `fixtureVersion` | string | 固定为 `0.2.0` |
| `coreClassifierVersion` | string | 固定为 `0.2.0`，关联第 3.6 节映射 |
| `fixtureId` | string | 如 `B03_SUMMON_N` |
| `buildId` | string | 六套构筑之一 |
| `developmentLevel` | enum | `N` / `H` |
| `slot` | int | 0-4 |
| `minionId` | string | 配置 ID |
| `isGolden` | bool | `H` 为 true |
| `permanentAttackBonus` | int | 永久攻击加成 |
| `permanentHealthBonus` | int | 永久生命加成 |
| `temporaryAttackBonus` | int | 下一战临时攻击 |
| `temporaryHealthBonus` | int | 下一战临时生命 |
| `temporaryKeywords` | string[] | 下一战临时关键词 |
| `sourceInstanceId` | string | 稳定且非空的实例 ID |
| `expectedInitialAttack` | int | 构造后的预期攻击 |
| `expectedInitialHealth` | int | 构造后的预期生命 |

### 5.3 单场战斗表 `balance_battle_samples.csv`

一行表示一个种子、一个方向的一场战斗：

| 字段组 | 字段 |
| --- | --- |
| 身份 | `balanceSchemaVersion`、`candidateId`、`contentVersion`、`configHash`、`gitCommit`、`unityVersion`、`tuningRound`、`batchId` |
| 夹具 | `fixtureVersion`、`coreClassifierVersion`、`seedSet`、`seed`、`playerFixtureId`、`enemyFixtureId`、`developmentLevel`、`orientation` |
| 结果 | `winner`、`outcomeReason`、`roundCount`、`playerSurvivors`、`enemySurvivors`、`playerSurvivorInstanceIds`、`enemySurvivorInstanceIds` |
| 输出 | `playerOpeningRawDamage`、`enemyOpeningRawDamage`、`playerRound1RawDamage`、`enemyRound1RawDamage`、对应有效伤害 |
| 攻击 | `playerNormalAttacks`、`enemyNormalAttacks`、`playerImmediateAttacks`、`enemyImmediateAttacks`、`playerCleaveHits`、`enemyCleaveHits` |
| 召唤 | 玩家/敌方各自的 `SummonAttempts`、`SummonSuccesses`、`SummonFailures`、`TokenDeaths`、`NonTokenDeaths`，字段统一加 `player` / `enemy` 前缀 |
| 护盾 | 玩家/敌方各自的 `ShieldsGranted`、`ShieldsLost`、`ShieldDamageBlocks`、`FurnaceTransfers`，字段统一加前缀 |
| 成长 | 玩家/敌方各自的 `TemporaryAttackGained`、`TemporaryHealthGained`、`PermanentAttackDelta`、`PermanentHealthDelta`，以及 `permanentDeltasByInstanceJson` |
| 安全 | `processedEffectCount`、`hitEffectLimit`、`exceptionType`、`determinismHash` |

`determinismHash` 由最终棋盘、胜负、原因、永久差量和诊断计数的规范化 JSON 计算；同版本、同夹具、同方向、同种子重复运行时必须一致。

### 5.4 战斗聚合表 `balance_battle_summary.csv`

一行表示一个镜像合并后的构筑对，或一个构筑对木桩的汇总：

| 字段 | 说明 |
| --- | --- |
| `tuningRound`、`candidateId` | 调数轮次和候选编号 |
| `seedSet`、`buildA`、`buildB`、`developmentLevel` | 聚合维度 |
| `battles`、`aWins`、`bWins`、`draws` | 原始结果 |
| `aScoreRate` | `(A 胜 + 0.5 × 平局) / battles` |
| `aDecisiveWinWilsonLow`、`aDecisiveWinWilsonHigh` | 排除平局后的胜率 95% Wilson 区间，仅辅助判断 |
| `roundLimitRate`、`mutualEliminationRate` | 异常结束和同时消灭比例 |
| `medianRounds`、`p90Rounds` | 战斗长度 |
| `meanRound1RawDamageA/B` | 第一轮原始输出 |
| `meanSurvivorsA/B` | 最终存活数 |
| `meanSummonFailuresA/B`、`meanImmediateAttacksA/B` | 召唤构筑诊断 |
| `meanShieldBlocksA/B`、`meanFurnaceTransfersA/B` | 护盾构筑诊断 |
| `meanPermanentDeltaA/B` | 每场永久成长总量 |
| `determinismFailures`、`effectLimitHits`、`exceptions` | 必须为 0 的安全计数 |

### 5.5 完整单局表 `balance_run_summary.csv`

一行表示一局完整三层人工单局：

| 字段组 | 字段 |
| --- | --- |
| 身份 | `tuningRound`、`candidateId`、`contentVersion`、`configHash`、`gitCommit`、`unityVersion`、`coreClassifierVersion`、`seed`、`tester` |
| 构筑 | `intendedBuildId`、`finalBuildId`、`finalBoardJson`、`finalPermanentAttack`、`finalPermanentHealth` |
| 回合 10 | `turn10Reached`、`turn10BuildId`、`turn10BoardJson`、`turn10PermanentAttack`、`turn10PermanentHealth`、`turn10CoreInstanceIds` |
| 回合 10 投入 | `turn10RefreshesPaid`、`turn10RefreshesFree`、`turn10MinionsBought`、`turn10MinionsSold`、`turn10SpellsUsed`、`turn10TavernUpgrades`、`turn10GoldWasted`、`turn10TriplesFormed` |
| 进度 | `result`、`floorReached`、`runTurn`、`elapsedMinutes`、`healthRemaining` |
| 战斗 | `battlesWon`、`battlesNotWon`、`elitesAttempted`、`elitesDefeated`、`bossAttempts`、`bossesDefeated`、`coreSurvivorsByBattleJson`、`permanentDeltasByInstanceJson` |
| 商店 | `refreshesPaid`、`refreshesFree`、`minionsBought`、`minionsSold`、`spellsUsed`、`tavernUpgrades`、`goldWasted` |
| 成型 | `firstCoreTurn`、`secondCoreTurn`、`dualCoreBeforeF3Boss`、`triplesFormed`、`targetedDiscoversUsed` |
| 路线 | `routeNodeIds`、`eliteRouteChosen`、`eventChoices`、`rewardChoices` |
| 体验 | `failureReason`、`boringMoment`、`unfairMoment`、`decisionSummaryPath`、`rawTelemetryPath` |

新增 `RunEnded`、`ShopSnapshot`、`Turn10Snapshot`、`EventChoiceResolved`、`RewardChoiceResolved` 和扩展后的 `BattleCompleted` 事件，以便从 NDJSON 自动生成大部分字段；人工只补构筑意图、体验备注和高层决策摘要。

### 5.6 卡牌漏斗表 `balance_card_funnel.csv`

按 `candidateId + tuningRound + tier + cardId` 聚合：

- `offered`：进入商店或奖励候选的次数；
- `boughtOrPicked`：购买或选取次数；
- `played`、`sold`、`survivedToRunEnd`；
- `tripleMaterials`、`triplesCompleted`；
- `discoverOffered`、`discoverPicked`；
- `offerToPickRate`、`pickToPlayRate`。

只有 `offered >= 10` 的卡牌才进入审查列表；比较时使用平滑转化率 `(boughtOrPicked + 1) / (offered + 2)`，同时展示原始分子分母。低样本卡牌只展示，不据此单卡调数。

### 5.7 成长速度校准表 `balance_fixture_calibration.csv`

按构筑输出：意图局数、第 10 回合到达率、按意图成型率、可识别样本数、P50/P90 永久面板、核心回合与商店投入中位数、P50/P90 代表种子和完整棋盘 JSON。状态分为：

- `NoFormedSamples`：没有可识别成型样本；
- `Insufficient`：少于 3 个样本；
- `Provisional`：3-9 个样本，只能用于调查；
- `Ready`：至少 10 个样本，可以生成 fixture v0.3 候选。

### 5.8 调数日志 `balance_change_log.csv`

每次配置或规则修改一行：

| 字段 | 说明 |
| --- | --- |
| `changeId` | 稳定编号，如 `R1-004` |
| `tuningRound`、`candidateId` | 所属轮次和候选 |
| `targetType`、`targetId`、`jsonPath` | 修改对象和字段 |
| `oldValue`、`newValue` | 修改前后值 |
| `evidenceMetric`、`evidenceValue` | 触发修改的指标 |
| `rootCause` | 机制、数值、获取率、敌人或路线 |
| `expectedEffect` | 对指标的预期方向和幅度 |
| `affectedFixtures`、`affectedTests` | 必须重跑的范围 |
| `result` | `Kept` / `Reverted` / `Superseded` |

### 5.9 文件布局

```text
sc/Assets/Tests/Fixtures/Balance/
  balance-fixtures.v0.2.json

balance-results/phase-6-v0.2/
  R0-baseline/
  R1-candidate-*/
  R2-candidate-*/
  final/
```

每个执行 S2 的候选在 `S2_RUNS/` 下保存 `raw/*.ndjson`、`decisions/*.json`、`balance_run_summary.csv`、`balance_card_funnel.csv`、`balance_fixture_calibration.csv` 和 `calibration_metadata.json`。

- 夹具属于测试数据，不加入正式内容发布清单；
- 每个候选目录保存原始逐场样本、聚合表、20 局 NDJSON、人工摘要和调数日志；
- `final/` 只复制最终保留候选的聚合结果与报告，不覆盖原始候选；
- 大体积逐场数据是否提交 Git 由执行阶段决定，但最终汇总、夹具和调数日志必须版本化。

## 6. R0 基线

R0 不改数值，只验证工具和生成原始基线：

1. 运行当前全量 EditMode 111 / 111、PlayMode 14 / 14；
2. 校验 12 个构筑快照的 ID、站位、金色状态、永久加成、覆盖层和总面板；
3. 对 `S0_SMOKE` 重复运行两次，要求所有 `determinismHash` 一致；
4. 对 `S1_CALIBRATION` 运行 84,000 场正式批次；
5. 使用 `S2_RUNS` 完成 R0 的 20 局完整三层单局；
6. 生成四张聚合表和 R0 问题清单；
7. 只分类问题，不在 R0 过程中边跑边改。

如果诊断统计尚未实现，允许先得到胜负基线，但不得以只有胜负的数据开始正式调数。

### 6.1 `.NET` R0 预演记录（2026-07-14）

- 使用 `R0-preflight-dotnet` 完成 S1 的 84,000 场战斗和 S0 的 8,400 场全矩阵复跑；
- 逐场确定性失败 0、异常 0、效果上限命中 0；
- 2,722 场回合上限全部来自只用于首轮输出测量的 D00 木桩，镜像对局与机制压力板为 0；正式 R0 前需将 D00 排除出竞技回合上限口径或改为首轮后停止；
- 27 / 30 组镜像对局越过 25%-75% 第一轮触发线，B01/B02 系统性偏强，B03 系统性偏弱；
- 六套 H/N 首轮原始输出比均不高于 2.0，最高 H 木桩输出 267.578；B05 正常输出领先第二名 39.42%，越过 35% 复核线；
- 预演结果见 `balance-results/phase-6-v0.2/R0-preflight-dotnet/`；该批次使用 `.NET 8.0.20`，不得与后续 Unity 正式 R0 数据直接合并，也不替代 20 局人工单局。

### 6.2 `.NET` R1 合并候选记录（2026-07-14）

- 先后完成夹具、B03 生存、B01 护盾转移、B02 永久成长、B05 覆盖层和 B06 普通护盾六个单轴 S0；每个候选均运行 8,400 场并同种子复跑，确定性失败、异常和效果上限均为 0；
- 合并候选 `R1-combined-dotnet` 完成 84,000 场 S1 和 8,400 场 S0 复跑；竞技回合上限为 0，D00 的 2,722 场回合上限独立记录，不再形成竞技安全告警；
- B01 N/H 等权得分率由 90.94%/97.52% 降至 87.34%/96.58%，B06 N 由 21.48% 提升至 27.47%；B03 仍仅为 0.18%/1.79%，说明只增加两个核心生命不足以解决召唤构筑问题；
- B05 N 木桩首轮输出由 133.826 降至 131.813，但仍领先第二名 B06 的 95.988 达 37.32%，尚未通过 35% 专项目标；
- D01 攻击降至 8/16 后十二个压力板场景仍为零最终存活；它可继续用于触发机制，但不能作为核心存活率的充分测量，需要新增时点存活遥测或重新设计压力板；
- 结果见 `balance-results/phase-6-v0.2/R1-combined-dotnet/`；由于运行时为 `.NET 8.0.20`，仍不能替代 Unity 正式批次与 20 局人工单局。

### 6.3 Unity R1 繁茂候选记录（2026-07-14）

- 新增 `SpireChess.Editor.BalanceBatchCommand.RunFromCommandLine`，支持 `S0/S1/S3/S4`、候选 ID、调数轮次、自定义输出目录和可选确定性复跑；
- `R1-flourish-unity/S0_SMOKE` 完成 8,400 场首跑与 8,400 场同种子复跑，确定性失败、异常、效果上限和竞技回合上限均为 0；
- `R1-flourish-unity/S1_CALIBRATION` 完成 84,000 场正式矩阵，异常、效果上限和竞技回合上限均为 0；S0/S1 的 `configHash` 均为 `61d16989844d94b2d008d876273b5893c6713bf7ed52623f74e604508081de42`；
- B03 N/H 等权得分率为 12.38%/45.30%，H 档整体已进入可观察区，但 B03-H 对 B04-H 仍为 97.48%，N 档对 B01/B02/B05 仍明显偏弱；
- 30 组镜像对局中仍有 24 组位于 25%-75% 触发线之外；B05 N 木桩首轮输出 131.813，仍领先 B06 N 的 95.988 达 37.32%；
- 完整结果和结论见 `balance-results/phase-6-v0.2/R1-flourish-unity/`，该候选尚未完成 20 局人工单局，不能冻结。

命令行执行格式：

```powershell
Unity.exe -batchmode -nographics -quit -projectPath sc `
  -executeMethod SpireChess.Editor.BalanceBatchCommand.RunFromCommandLine `
  -balanceCandidate R1-flourish-unity -balanceTuningRound R1 `
  -balanceSeedSet S0
```

### 6.4 Unity R2 补盾来源收敛候选（2026-07-14）

- 断誓刃魂与不熄炉王保持不变；盾墙执炉者改为限次失盾属性补偿，共鸣钟卫移除补盾并提高生命补偿，炉心圣盾官保留开场定向护盾、将友军死亡补盾改为临时 +2/+2；
- `R2-shield-source-consolidation/S0_SMOKE` 完成 8,400 场首跑与 8,400 场同种子复跑，确定性失败、异常、效果上限和竞技回合上限均为 0；配置哈希为 `439b15d313a72914b994171d0358b6819d2f493b5f6487f66bc2fa4e72a38876`；
- B01 N/H 等权得分率由 86.20%/96.10% 降至 50.70%/73.30%，平均挡盾次数由 6.50/8.82 降至 3.98/5.58；不熄炉王平均转移次数仍为 1.99/3.76，符合保留 T5 补盾核心的目标；
- 25%-75% 触发线外的镜像对局由 24/30 降至 19/30，但 35%-65% 目标带外仍有 23/30；B01-H 仍偏强；
- B02 对 B03/B04/B05/B06 的同种子结果与 R1 完全相同，只因对 B01 的优势扩大而使整体 N/H 得分率升至 91.85%/86.15%，需要作为独立问题处理；
- 结果见 `balance-results/phase-6-v0.2/R2-shield-source-consolidation/`；S0 方向通过，可进入 S1 扩样，但尚未执行 S1。

### 6.5 当前六流派 Unity S0 总览（2026-07-14）

- 使用与 R2 补盾来源收敛候选相同的配置哈希 `439b15d313a72914b994171d0358b6819d2f493b5f6487f66bc2fa4e72a38876` 重跑全部 84 个场景；8,400 场首跑与 8,400 场复跑的确定性失败、异常、效果上限和竞技回合上限均为 0；
- N 档等权得分率由高到低为 B02 91.85%、B05 64.80%、B01 50.70%、B04 45.00%、B06 29.85%、B03 17.80%；
- H 档等权得分率由高到低为 B02 86.15%、B01 73.30%、B03 51.40%、B06 46.30%、B05 39.20%、B04 3.65%；
- 30 组镜像对局中 19 组越过 25%-75% 触发线，只有 7 组进入 35%-65% 目标带；主要后续问题为 B02 全档偏强、B03-N 与 B04-H 失效、B01-H 偏强、B05-N 输出偏高和 B06-N 偏弱；
- 完整对局矩阵和逐流派结论见 `balance-results/phase-6-v0.2/R2-current-all-builds-unity/R2-current-all-builds-S0-report.md`。

### 6.6 Unity R3 失盾压缩与亡语转化候选（2026-07-14）

- 断誓刃魂改为只响应自身失盾；裂甲复仇者改为限次成长；群山古灵改为前三次全体永久成长；金色终花吞世者前两次额外复制死亡单位的战斗生命上限并永久获得 +2/+2；
- 战斗运行时新增独立的战斗生命上限，Unity EditMode 109/109、PlayMode 14/14 全量通过；
- `R3-B02-B04-trigger-conversion/S0_SMOKE` 完成 8,400 场首跑与 8,400 场同种子复跑，确定性失败、异常、效果上限和竞技回合上限均为 0；配置哈希为 `208cdc47928dd51ab6951974c06dc25379e6c72a52d2a7669dd6121e10a3794a`；
- B02 N/H 等权得分率由 91.85%/86.15% 降至 79.40%/69.75%，临时攻击由 23.19/47.81 降至 5.97/14.43，但永久差量升至 7.00/10.14，仍需小步压缩；
- B04-H 由 3.65% 恢复到 39.75%，临时生命由 13.37 提高到 42.21，没有整体超调；B04-N 由 45.00% 降到 31.05%，需要补回第一次死亡的本场 +1/+1；
- 30 组镜像对局中，25%-75% 触发线外由 19 组降至 11 组，35%-65% 目标带内由 7 组增加到 10 组；完整报告见 `balance-results/phase-6-v0.2/R3-B02-B04-trigger-conversion/R3-B02-B04-trigger-conversion-S0-report.md`。

## 7. 第一轮调数：极端值与系统性问题

### 7.1 第一轮修改范围

第一轮只处理：

- 崩溃、非确定性、2048 效果上限和回合上限异常；
- 无限或近无限经济、护盾、召唤和永久成长；
- 得分率极端离群；
- `H/N` 输出倍率或木桩输出越过硬上限；
- 扩池导致核心、三连或双核心明显无法形成；
- 普通、精英和 Boss 的明显难度断层。

同一个根因优先只改一个轴：触发次数、单次值、身材、费用、获取率或敌人加成。一次同时改多个轴时，调数日志必须说明无法拆分的原因。

第一轮候选完成配置修改后，必须重跑完整 `S1_CALIBRATION` 和同一组 `S2_RUNS`；不能只用 R0 的完整单局数据判断第一轮结果。

### 7.2 第一轮触发门槛

任一条件满足即进入修改清单：

| 类别 | 触发门槛 |
| --- | --- |
| 安全 | 任意异常、非确定性、效果上限命中，或回合上限率 > 1% |
| 单对局 | 任一镜像构筑对得分率 < 25% 或 > 75% |
| 构筑整体 | 任一构筑对其余五套的等权平均得分率 < 30% 或 > 70% |
| 最高发育 | `H/N` 第一轮原始输出比 > 2.5，或 `H` 木桩第一轮原始输出 > 400 |
| 星契基准 | `B05_SPELL` 在 `S1` 上的正常木桩平均输出领先第二名 > 35% |
| 召唤 | 同一召唤事件重复立即攻击、Token 写入永久差量，或事件去重失败 |
| 护盾 | 普通/金色不熄炉王转移次数超过 2/4，或次数跨战斗继承 |
| 完整单局 | 20 局中少于 10 局在第三层 Boss 前形成双核心，或三连中位数为 0 |
| 节奏 | 单局时长中位数 < 8 分钟或 > 25 分钟 |

### 7.3 第一轮退出门槛

第一轮候选必须同时满足：

- 异常、非确定性、效果上限命中均为 0；
- 回合上限率不高于 1%；
- 所有镜像构筑对位于 25%-75%；
- 六套构筑对其余五套的等权平均得分率均位于 30%-70%；
- `H/N <= 2.5` 且 `H` 木桩第一轮原始输出不超过 400；
- 第一轮 20 局中至少 12 局在第三层 Boss 前形成双核心，三连中位数不低于 1，单局时长中位数处于 8-25 分钟；
- Token、立即攻击、护盾上限和永久差量身份规则全部保持；
- EditMode、PlayMode 全绿，受影响的 v0.2 人工项复测通过。

达到第一轮退出门槛只表示极端值已经收敛，不代表可以冻结数值。

## 8. 第二轮调数：整体收敛与冻结候选

### 8.1 第二轮执行

1. 使用第一轮候选重跑完整 `S1_CALIBRATION`；
2. 比较 R0、R1 和当前候选，不混用不同候选的数据；
3. 处理 35%-65% 目标带之外的剩余构筑、节奏和获取率问题；
4. 再次完成 `S2_RUNS` 的 20 局完整单局；
5. 候选锁定后运行尚未查看的 `S3_HOLDOUT_A`；若该集合已因前一候选失败而被查看，则改用 `S4_HOLDOUT_B`；
6. 若根据第二轮数据继续修改，必须重新生成完整第二轮数据，旧结果标为 `Superseded`；
7. 所有门槛通过后生成冻结报告。

### 8.2 第二轮战斗门槛

| 指标 | 冻结门槛 |
| --- | --- |
| 构筑整体 | 六套构筑对其余五套的等权平均得分率均为 40%-60% |
| 单对局硬边界 | 15 组镜像对局全部位于 30%-70% |
| 单对局目标带 | 至少 12 / 15 组位于 35%-65%；其余最多 3 组必须有机制解释和遗留记录 |
| 平局 | 总平局率不高于 10%，回合上限率不高于 1% |
| 正常面板 | 可比较完整单局的第 10 回合永久面板中位数处于对应目标的 ±15% |
| 最高发育 | 所有构筑 `H/N <= 2.5`，且任一 `H` 木桩第一轮原始输出不超过 400 |
| 星契稳定性 | `B05_SPELL` 可以最稳定，但正常输出领先第二名不超过 35% |
| 防过拟合 | 当前未查看留出集中构筑平均得分率不越过 35%-65%，且相对 `S1` 偏移不超过 10 个百分点 |

### 8.3 第二轮完整单局门槛

20 局固定种子同时满足：

- 至少 15 / 20 局在第三层 Boss 前形成可识别的双核心构筑；
- 三连次数中位数不低于 1；
- 熟悉规则的内部测试者通关 8-14 局，作为 40%-70% 的暂定内部通关率；
- 单局时长中位数处于 10-20 分钟，至少 16 / 20 局处于 8-25 分钟；
- 铸魂、荒灵、星契三个主种族各至少有一局通关；
- 不存在稳定净无限金币、故意输 Boss 刷资源或奖励预留不释放；
- 对 `offered >= 10` 的同等级可比卡牌，平滑购买/选择转化率相差超过 3 倍且原始转化率绝对差超过 15 个百分点时必须审查；该信号本身不是机械失败线，但冻结前不能存在未解释的持续离群卡牌；
- 精英路线选择率或精英挑战成功率在有效机会不少于 8 次时低于 25% 或高于 75%，必须复核风险收益；奖励选项出现不少于 8 次且选择率超过 75% 时必须复核固定最优问题；低样本只记录不调数；
- 少于 2 个可比较样本的构筑意图视为数据不足，不用替换种子粉饰结果；冻结前补充记录，但原 20 局仍保留。

40%-70% 只是当前熟练内部测试者的 MVP 暂定范围，不等同于未来新玩家目标。阶段 7 外部试玩后可以建立独立的新玩家目标。

### 8.4 机制专项门槛

- `B01_SHIELD`：普通/金色炉王单场转移不超过 2/4；不存在无限护盾循环。
- `B02_BREAK`：临时成长与永久差量分别记录；普通单卡每场永久收益通常不超过 2-4 点总属性，金色不超过普通的 2 倍。
- `B03_SUMMON`：每个成功召唤事件对同一实例最多一次立即攻击；Token 永久差量为 0；若非 Token 核心存活率低于 40%，该构筑整体得分率必须不低于 40%，否则需要处理卡格或生存问题。
- `B04_DEATH`：金色群山与金色终花不产生永久成长乘算；非 Token 与 Token 死亡计数严格分离。
- `B05_SPELL`：前两次施法限制准确；法术投入带来的稳定性不形成超过 35% 的首轮输出领先。
- `B06_REFRESH`：第 2/3/4/5 次刷新里程碑只触发配置允许的次数；免费刷新和金币收益不会自我生成无限循环。

## 9. 调数纪律

- 所有修改先写 `balance_change_log.csv`，再改配置或代码；
- 不根据单个种子、单场观感或未镜像的胜率调数；
- 先确认问题来自构筑强度、获取率、敌人、路线还是奖励，再选择修改位置；
- 同一轮不同时削弱强构筑并增强其全部克制者；
- 除非 20 局数据证明扩池稀释不可接受，不调整牌池副本数和商店格子；
- 不为了达成数字门槛破坏已经通过的规则、Token 身份或文案一致性；
- 自动化或人工功能验收回归时，优先回退候选，不将功能错误解释为平衡波动；
- 两轮是最低次数。第二轮仍有硬门槛失败时不得冻结，可继续产生新候选，但必须保持同一数据结构和种子集合。

## 10. 实施顺序

### P0：统计与夹具

- [x] 将六套 `N/H` 构筑和木桩定义为版本化夹具；
- [x] 同时定义纯输出木桩和 `N/H` 机制压力板；
- [x] 增加夹具 ID、卡牌 ID、最终身材、金色状态和 `SourceInstanceId` 校验；
- [x] 将第 3.6 节核心映射实现为版本化离线分类器，并覆盖 `Mixed` / `Unclassified`；
- [x] 扩展 `BattleSimulationResult` 或独立诊断收集器，提供第 5.3 节计数；
- [x] 扩展 `BattleBatchRunner`，输出逐场样本和镜像聚合；
- [x] 增加确定性哈希与同种子复跑测试；
- [x] 建立 CSV/NDJSON 聚合器并校验 schema 版本。
- [x] 增加 Unity Editor/CLI 正式批次入口，版本化输出逐场样本、场景/镜像汇总、确定性差异和 metadata。
- [x] 增加 fixture v0.3 的逐槽 N/H 金色状态与独立永久加成，并以校准证据和样本门槛阻止人工假设直接进入正式夹具。

### P0：完整单局遥测

- [x] 新增 `RunEnded`、`ShopSnapshot`、`Turn10Snapshot`、`EventChoiceResolved`、`RewardChoiceResolved` 和扩展 `BattleCompleted`；
- [x] 区分付费/免费刷新，记录购买、出售、施法、升级和金币浪费；
- [x] 第 10 回合额外记录实际流派、经济投入与三连，并生成按构筑的成型率、P50/P90 面板和代表棋盘；
- [x] 增加固定种子人工单局启动参数与 `RunGrowthCalibrationCommand` 离线聚合入口；
- [x] 记录核心首次获得、第二核心形成、三连和定向发现；
- [x] 提供人工决策摘要模板，不实现完整命令重放；
- [x] 从 20 份 NDJSON 自动生成 `balance_run_summary.csv` 和卡牌漏斗。

### P1：两轮执行

- [ ] 完成 R0 自动化、确定性检查、84,000 场基线和 20 局人工单局；
- [ ] 完成第一轮调数、84,000 场全量复跑、同种子 20 局完整单局和受影响人工冒烟；
- [ ] 完成第二轮调数、84,000 场全量复跑、20 局人工单局和 8,400 场未查看留出集复核；
- [ ] 生成冻结报告，更新 `TODO.md`、`development-plan.md` 和验收文档。

## 11. 完成标准

阶段 6 v0.2 只有在以下条件全部满足时才完成：

- 六套构筑、12 个快照、固定种子和数据 schema 均由自动化校验；
- R0、第一轮、第二轮数据可以按候选独立复现，不混用结果；
- R0、第一轮、第二轮分别完成同一组 20 个单局种子，总计至少 60 局可比记录；
- 第一轮和第二轮退出门槛全部通过；
- 每个候选的完整单局数据、卡牌漏斗和人工体验备注齐全；
- 自动化保持全绿，v0.2 功能人工验收没有回归；
- 所有保留或回退的修改都有调数日志；
- 输出最终平衡报告、遗留问题清单和冻结的内容版本；
- 阶段 7 可以在无需继续大规模改规则或内容池的前提下开始正式 UI/UX。
