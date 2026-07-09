# JSON 字段设计文档

版本：0.1  
关联文档：`minion-design-v0.1.md`、`spell-design-v0.1.md`、`unity-project-prep-tech-plan.md`  
用途：定义随从与法术的 JSON 配置字段，供 Unity MVP 阶段读取和实现。

## 1. 设计原则

本项目的卡牌效果较多，完全硬编码会很难维护；但一开始设计过度通用的效果编辑器也会拖慢开发。因此本版本采用折中方案：

- 基础字段结构化。
- 效果用统一 `effects` 数组描述。
- 每个效果由 `trigger`、`action`、`target`、`value`、`condition` 等字段组合。
- 复杂效果允许先用 `effectId` 绑定代码实现。
- 文案字段独立保存，UI 不直接拼接效果描述。
- JSON 使用 Newtonsoft Json 解析，允许嵌套数组和更灵活的数据结构。
- MVP 阶段不实现 `fallbackEffects`，复杂牌先用 `effectId` 或代码特殊处理。

## 2. 枚举约定

### 2.1 种族

```json
["ForgeSoul", "WildSpirit", "Starbound", "Wayfarer"]
```

显示名：

| ID | 中文 |
| --- | --- |
| ForgeSoul | 铸魂 |
| WildSpirit | 荒灵 |
| Starbound | 星契 |
| Wayfarer | 旅团 |

### 2.2 关键词

```json
["Taunt", "Shield", "Deathrattle", "Battlecry", "Cleave"]
```

显示名：

| ID | 中文 |
| --- | --- |
| Taunt | 嘲讽 |
| Shield | 护盾 |
| Deathrattle | 亡语 |
| Battlecry | 战吼 |
| Cleave | 溅射 |

溅射规则：

- 普通溅射：攻击时，对攻击目标左右两侧的随从造成本次攻击伤害 50% 的伤害。
- 金色溅射：攻击时，对攻击目标左右两侧的随从造成本次攻击伤害 100% 的伤害。
- 溅射只影响攻击目标相邻位置上的随从，不会继续扩散。
- 溅射伤害可以被护盾抵挡。

### 2.3 流派标签

```json
[
  "ShieldWall",
  "ShieldBreakCounter",
  "SummonTempo",
  "DeathGrower",
  "SpellEcho",
  "RefreshEconomy",
  "EconomyTransition",
  "CounterTech"
]
```

## 3. 随从 JSON 字段

### 3.1 文件结构

建议按等级或整体文件保存。MVP 阶段可以先用单文件：

```text
Assets/Configs/Json/minions.json
```

顶层结构：

```json
{
  "version": "0.1",
  "minions": []
}
```

### 3.2 随从字段

```json
{
  "id": "forge_soul_shield_squire",
  "name": "铸魂盾侍",
  "title": "",
  "description": "战斗开始：获得护盾。",
  "goldenDescription": "战斗开始：获得护盾，并使左侧友方随从获得护盾。",

  "tier": 1,
  "race": "ForgeSoul",
  "archetypes": ["ShieldWall"],
  "keywords": ["Taunt"],
  "isToken": false,

  "attack": 1,
  "health": 3,
  "goldenAttack": 2,
  "goldenHealth": 6,

  "artId": "placeholder_card_forge_soul_001",
  "iconId": "icon_forge_soul",
  "audioId": "",

  "effects": [],
  "goldenEffects": [],

  "tags": ["starter", "frontline"],
  "enabled": true,
  "devNote": ""
}
```

### 3.3 字段说明

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| id | string | 是 | 唯一 ID，代码和存档引用 |
| name | string | 是 | 显示名 |
| title | string | 否 | 副标题或称号，MVP 可空 |
| description | string | 是 | 普通效果文案 |
| goldenDescription | string | 是 | 金色效果文案 |
| tier | int | 是 | 随从等级，1-5 |
| race | string | 是 | 种族 ID |
| archetypes | string[] | 否 | 流派标签 |
| keywords | string[] | 否 | 普通关键词 |
| isToken | bool | 是 | 是否 Token |
| attack | int | 是 | 普通基础攻击 |
| health | int | 是 | 普通基础生命 |
| goldenAttack | int | 是 | 金色基础攻击，默认普通攻击 x2 |
| goldenHealth | int | 是 | 金色基础生命，默认普通生命 x2 |
| artId | string | 否 | 美术资源 ID |
| iconId | string | 否 | 图标资源 ID |
| audioId | string | 否 | 音效资源 ID |
| effects | EffectConfig[] | 否 | 普通效果 |
| goldenEffects | EffectConfig[] | 否 | 金色效果 |
| tags | string[] | 否 | 调试和筛选标签 |
| enabled | bool | 是 | 是否进入牌池 |
| devNote | string | 否 | 设计备注 |

## 4. Token JSON 字段

Token 可以放在同一个 `minions.json` 中，但 `isToken` 为 `true`，且不进入商店牌池。

示例：

```json
{
  "id": "token_young_spirit",
  "name": "幼灵",
  "description": "战斗结束后消失。",
  "goldenDescription": "",
  "tier": 0,
  "race": "WildSpirit",
  "archetypes": ["SummonTempo"],
  "keywords": [],
  "isToken": true,
  "attack": 1,
  "health": 1,
  "goldenAttack": 0,
  "goldenHealth": 0,
  "artId": "placeholder_token_young_spirit",
  "iconId": "icon_wild_spirit",
  "audioId": "",
  "effects": [],
  "goldenEffects": [],
  "tags": ["token", "summon"],
  "enabled": true,
  "devNote": "Token 不进入商店，不参与三连。"
}
```

## 5. 法术 JSON 字段

### 5.1 文件结构

建议单文件：

```text
Assets/Configs/Json/spells.json
```

顶层结构：

```json
{
  "version": "0.1",
  "spells": []
}
```

### 5.2 法术字段

```json
{
  "id": "minor_tempering",
  "name": "小型锻体",
  "description": "使一个随从永久获得 +1/+1。",

  "tier": 1,
  "spellType": "Growth",
  "useTiming": ["Shop", "Prep"],
  "rarity": "Common",
  "cost": 3,

  "artId": "placeholder_spell_growth_001",
  "iconId": "icon_spell_growth",
  "audioId": "",

  "effects": [],

  "tags": ["permanent_growth"],
  "enabled": true,
  "devNote": ""
}
```

### 5.3 字段说明

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| id | string | 是 | 唯一 ID |
| name | string | 是 | 显示名 |
| description | string | 是 | 法术文案 |
| tier | int | 是 | 法术等级，1-5 |
| spellType | string | 是 | 法术类型 |
| useTiming | string[] | 是 | 可使用时机 |
| rarity | string | 否 | 稀有度 |
| cost | int | 是 | 商店购买费用 |
| artId | string | 否 | 美术资源 ID |
| iconId | string | 否 | 图标资源 ID |
| audioId | string | 否 | 音效资源 ID |
| effects | EffectConfig[] | 是 | 效果配置 |
| tags | string[] | 否 | 调试和筛选标签 |
| enabled | bool | 是 | 是否进入牌池 |
| devNote | string | 否 | 设计备注 |

### 5.4 法术类型

```json
[
  "Growth",
  "Economy",
  "Defense",
  "Refresh",
  "Discover",
  "Copy",
  "CombatBuff"
]
```

### 5.5 使用时机

```json
[
  "Shop",
  "Prep",
  "Combat"
]
```

MVP 默认不支持玩家在战斗中手动使用法术，因此 `Combat` 暂时不用。

金色效果规则：

- 普通效果全部写入 `effects`。
- 金色效果全部写入 `goldenEffects`。
- 运行时根据实例是否金色选择效果组，不自动从普通效果推导金色效果。

## 6. 效果 JSON 字段

### 6.1 EffectConfig

```json
{
  "id": "effect_001",
  "trigger": "OnBattleStart",
  "action": "AddShield",
  "target": {
    "side": "Ally",
    "scope": "Self",
    "race": "",
    "includeToken": false,
    "maxTargets": 1,
    "selector": "None"
  },
  "value": {
    "attack": 0,
    "health": 0,
    "amount": 1,
    "duration": "Combat",
    "keyword": "",
    "resource": ""
  },
  "condition": {
    "type": "None",
    "race": "",
    "keyword": "",
    "threshold": 0,
    "compare": "GreaterOrEqual",
    "phaseStat": ""
  },
  "limit": {
    "perCombat": 0,
    "perShop": 0,
    "perRun": 0
  },
  "discover": null,
  "fallbackEffects": []
}
```

### 6.2 EffectConfig 字段说明

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | string | 效果 ID，便于日志和调试 |
| trigger | string | 触发时机 |
| action | string | 执行动作 |
| target | TargetConfig | 目标选择 |
| value | ValueConfig | 数值、关键词、资源等 |
| condition | ConditionConfig | 触发条件 |
| limit | LimitConfig | 次数限制 |
| discover | DiscoverConfig | 发现池配置，仅发现类动作使用 |
| fallbackEffects | EffectConfig[] | 失败或条件不满足时的备用效果，MVP 阶段暂不实现 |

MVP 实现说明：

- `fallbackEffects` 字段可以保留在 JSON 中，但运行时先忽略。
- 需要备用逻辑的复杂牌，先通过代码特殊处理。
- 特殊处理统一使用 `effectId` 定位。

## 7. 触发时机

建议 MVP 支持以下触发：

```json
[
  "OnBuy",
  "OnSell",
  "OnPlay",
  "OnBattleStart",
  "OnDeath",
  "OnShieldGained",
  "OnShieldLost",
  "OnSummon",
  "OnSummonedUnitDeath",
  "OnSpellUsed",
  "OnRefresh",
  "OnShopPhaseStart",
  "OnShopPhaseEnd",
  "OnCombatEnd",
  "Manual"
]
```

说明：

- `OnBuy`：购买时触发。
- `OnPlay`：放入阵容时触发，战吼最终绑定此触发。
- `OnBattleStart`：战斗开始。
- `OnDeath`：自身死亡。
- `OnShieldGained`：获得护盾。
- `OnShieldLost`：失去护盾。
- `OnSummon`：召唤成功。
- `OnSummonedUnitDeath`：召唤物死亡。
- `OnSpellUsed`：使用法术。
- `OnRefresh`：商店刷新。
- `Manual`：法术主动使用。

## 8. 动作类型

建议 MVP 支持以下动作：

```json
[
  "ModifyStats",
  "AddShield",
  "RemoveShield",
  "AddKeyword",
  "DealDamage",
  "HealPlayer",
  "GainGold",
  "ScheduleGold",
  "FreeRefresh",
  "DiscoverMinion",
  "DiscoverSpell",
  "CopyMinion",
  "SummonToken",
  "ImmediateAttack",
  "SetPendingCombatBuff"
]
```

关键动作说明：

| 动作 | 说明 |
| --- | --- |
| ModifyStats | 修改攻击/生命，可永久或临时 |
| AddShield | 添加护盾 |
| RemoveShield | 移除护盾 |
| AddKeyword | 添加关键词 |
| DealDamage | 造成伤害 |
| HealPlayer | 恢复玩家生命 |
| GainGold | 立即获得金币 |
| ScheduleGold | 下个商店阶段获得金币 |
| FreeRefresh | 获得免费刷新次数 |
| DiscoverMinion | 发现随从 |
| DiscoverSpell | 发现法术 |
| CopyMinion | 复制随从 |
| SummonToken | 召唤 Token |
| ImmediateAttack | 立即攻击一次 |
| SetPendingCombatBuff | 设置下一场战斗开始时触发的增益 |

## 9. 目标选择字段

### 9.1 TargetConfig

```json
{
  "side": "Ally",
  "scope": "Self",
  "race": "",
  "includeToken": false,
  "maxTargets": 1,
  "selector": "None"
}
```

### 9.2 side

```json
["Ally", "Enemy", "Both"]
```

### 9.3 scope

```json
[
  "Self",
  "Single",
  "All",
  "Adjacent",
  "Left",
  "Right",
  "LeftMost",
  "RightMost",
  "LowestAttack",
  "LowestHealth",
  "MostCommonRace",
  "Token",
  "NonToken"
]
```

### 9.4 selector

```json
["None", "PlayerChoice", "Random", "LowestAttack", "LowestHealth", "MostCommonRace"]
```

说明：

- 永久强化默认 `includeToken: false`。
- 临时强化可以允许 `includeToken: true`。
- `MostCommonRace` 只统计主种族，不统计旅团。

## 10. 数值字段

### 10.1 ValueConfig

```json
{
  "attack": 0,
  "health": 0,
  "amount": 0,
  "duration": "Permanent",
  "keyword": "",
  "resource": ""
}
```

### 10.2 duration

```json
["Permanent", "Combat", "NextCombat", "ShopPhase", "Run"]
```

说明：

- `Permanent`：永久保留。
- `Combat`：当前战斗。
- `NextCombat`：下一场战斗。
- `ShopPhase`：当前商店阶段。
- `Run`：整局生效，主要用于特殊奖励，MVP 可暂缓。

## 11. 条件字段

### 11.1 ConditionConfig

```json
{
  "type": "PhaseStatAtLeast",
  "race": "",
  "keyword": "",
  "threshold": 2,
  "compare": "GreaterOrEqual",
  "phaseStat": "RefreshCount"
}
```

### 11.2 type

```json
[
  "None",
  "HasKeyword",
  "HasShield",
  "IsGolden",
  "RaceCountAtLeast",
  "PhaseStatAtLeast",
  "TargetAlreadyHasShield",
  "NoBoardSpace",
  "IsMostCommonMainRace",
  "HasGoldenMinion"
]
```

### 11.3 phaseStat

```json
[
  "RefreshCount",
  "SpellUsedCount",
  "SpellBoughtCount",
  "SummonedUnitDeathCount"
]
```

## 12. 限制字段

### 12.1 LimitConfig

```json
{
  "perCombat": 0,
  "perShop": 0,
  "perRun": 0
}
```

说明：

- 0 表示不限制。
- `perCombat`：每场战斗最多触发次数。
- `perShop`：每个商店阶段最多触发次数。
- `perRun`：每局最多触发次数。

## 13. 发现池字段

发现池完全通过效果字段描述，不单独使用全局 `discoverPoolId`。

### 13.1 DiscoverConfig

发现相关动作通过 `EffectConfig.discover` 字段描述。

```json
{
  "discover": {
    "cardType": "Minion",
    "race": "Starbound",
    "minTier": 1,
    "maxTierMode": "CurrentTavernTier",
    "maxTierOffset": 0,
    "count": 3,
    "pick": 1,
    "includeToken": false,
    "includeDisabled": false,
    "requireGolden": false
  }
}
```

### 13.2 字段说明

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| cardType | string | `Minion` 或 `Spell` |
| race | string | 限定种族，空字符串表示不限 |
| minTier | int | 最低等级 |
| maxTierMode | string | 最高等级计算方式 |
| maxTierOffset | int | 最高等级偏移 |
| count | int | 候选数量 |
| pick | int | 选择数量 |
| includeToken | bool | 是否包含 Token，默认 false |
| includeDisabled | bool | 是否包含未启用配置，默认 false |
| requireGolden | bool | 是否只发现特殊金色候选，MVP 可暂缓 |

说明：

- 发现池不支持权重。
- 候选从满足条件的配置中随机抽取。
- 若可用候选少于 `count`，则展示全部可用候选。

### 13.3 maxTierMode

```json
[
  "Fixed",
  "CurrentTavernTier",
  "CurrentTavernTierMinusOne",
  "CurrentTavernTierPlusOffset"
]
```

示例：

- 当前酒馆等级以内：`CurrentTavernTier` + `maxTierOffset: 0`
- 当前酒馆等级 +1：`CurrentTavernTierPlusOffset` + `maxTierOffset: 1`
- 固定 5 级：`Fixed`，并在效果中指定 `maxTier: 5`，该字段后续可扩展。

## 14. 示例：随从

### 14.1 铸魂盾侍

```json
{
  "id": "forge_soul_shield_squire",
  "name": "铸魂盾侍",
  "description": "战斗开始：获得护盾。",
  "goldenDescription": "战斗开始：获得护盾，并使左侧友方随从获得护盾。",
  "tier": 1,
  "race": "ForgeSoul",
  "archetypes": ["ShieldWall"],
  "keywords": ["Taunt"],
  "isToken": false,
  "attack": 1,
  "health": 3,
  "goldenAttack": 2,
  "goldenHealth": 6,
  "artId": "placeholder_card_forge_soul_001",
  "iconId": "icon_forge_soul",
  "audioId": "",
  "effects": [
    {
      "id": "forge_soul_shield_squire_battle_start",
      "trigger": "OnBattleStart",
      "action": "AddShield",
      "target": {
        "side": "Ally",
        "scope": "Self",
        "race": "",
        "includeToken": false,
        "maxTargets": 1,
        "selector": "None"
      },
      "value": {
        "attack": 0,
        "health": 0,
        "amount": 1,
        "duration": "Combat",
        "keyword": "",
        "resource": ""
      },
      "condition": {
        "type": "None",
        "race": "",
        "keyword": "",
        "threshold": 0,
        "compare": "GreaterOrEqual",
        "phaseStat": ""
      },
      "limit": {
        "perCombat": 0,
        "perShop": 0,
        "perRun": 0
      },
      "fallbackEffects": []
    }
  ],
  "goldenEffects": [],
  "tags": ["frontline"],
  "enabled": true,
  "devNote": ""
}
```

### 14.2 幼灵 Token

```json
{
  "id": "token_young_spirit",
  "name": "幼灵",
  "description": "战斗结束后消失。",
  "goldenDescription": "",
  "tier": 0,
  "race": "WildSpirit",
  "archetypes": ["SummonTempo"],
  "keywords": [],
  "isToken": true,
  "attack": 1,
  "health": 1,
  "goldenAttack": 0,
  "goldenHealth": 0,
  "artId": "placeholder_token_young_spirit",
  "iconId": "icon_wild_spirit",
  "audioId": "",
  "effects": [],
  "goldenEffects": [],
  "tags": ["token", "summon"],
  "enabled": true,
  "devNote": "Token 不进入商店，不参与三连。"
}
```

## 15. 示例：法术

### 15.1 小型锻体

```json
{
  "id": "minor_tempering",
  "name": "小型锻体",
  "description": "使一个随从永久获得 +1/+1。",
  "tier": 1,
  "spellType": "Growth",
  "useTiming": ["Shop", "Prep"],
  "rarity": "Common",
  "cost": 3,
  "artId": "placeholder_spell_growth_001",
  "iconId": "icon_spell_growth",
  "audioId": "",
  "effects": [
    {
      "id": "minor_tempering_buff",
      "trigger": "Manual",
      "action": "ModifyStats",
      "target": {
        "side": "Ally",
        "scope": "Single",
        "race": "",
        "includeToken": false,
        "maxTargets": 1,
        "selector": "PlayerChoice"
      },
      "value": {
        "attack": 1,
        "health": 1,
        "amount": 0,
        "duration": "Permanent",
        "keyword": "",
        "resource": ""
      },
      "condition": {
        "type": "None",
        "race": "",
        "keyword": "",
        "threshold": 0,
        "compare": "GreaterOrEqual",
        "phaseStat": ""
      },
      "limit": {
        "perCombat": 0,
        "perShop": 0,
        "perRun": 0
      },
      "fallbackEffects": []
    }
  ],
  "tags": ["permanent_growth"],
  "enabled": true,
  "devNote": ""
}
```

### 15.2 应急补给

```json
{
  "id": "delayed_supply",
  "name": "应急补给",
  "description": "下个商店阶段开始时，获得 2 金币。",
  "tier": 1,
  "spellType": "Economy",
  "useTiming": ["Shop", "Prep"],
  "rarity": "Common",
  "cost": 3,
  "artId": "placeholder_spell_economy_001",
  "iconId": "icon_spell_economy",
  "audioId": "",
  "effects": [
    {
      "id": "delayed_supply_gold",
      "trigger": "Manual",
      "action": "ScheduleGold",
      "target": {
        "side": "Ally",
        "scope": "Self",
        "race": "",
        "includeToken": false,
        "maxTargets": 1,
        "selector": "None"
      },
      "value": {
        "attack": 0,
        "health": 0,
        "amount": 2,
        "duration": "ShopPhase",
        "keyword": "",
        "resource": "Gold"
      },
      "condition": {
        "type": "None",
        "race": "",
        "keyword": "",
        "threshold": 0,
        "compare": "GreaterOrEqual",
        "phaseStat": ""
      },
      "limit": {
        "perCombat": 0,
        "perShop": 0,
        "perRun": 0
      },
      "fallbackEffects": []
    }
  ],
  "tags": ["economy", "delayed"],
  "enabled": true,
  "devNote": ""
}
```

## 16. 存档引用建议

配置 JSON 只保存静态定义。运行时存档不复制整张配置，而是引用 ID 并记录变化。

随从实例建议存档：

```json
{
  "instanceId": "run_minion_0001",
  "configId": "forge_soul_shield_squire",
  "isGolden": false,
  "permanentAttackBonus": 0,
  "permanentHealthBonus": 0,
  "permanentKeywords": [],
  "hasShield": false
}
```

法术实例建议存档：

```json
{
  "instanceId": "run_spell_0001",
  "configId": "minor_tempering"
}
```

## 17. 已确认规则

- 战吼最终绑定 `OnPlay`。
- `OnPlay` 指随从放入阵容时触发。
- `OnPlay` 允许同一个随从多次进出阵容重复触发。
- `Battlecry` 与 `OnPlay` 在 UI 文案中完全等价。
- 战吼类效果在合成金色时重新触发。
- 溅射对攻击目标左右两侧随从造成伤害：普通为 50%，金色为 100%。
- 发现池完全用效果字段描述。
- 发现池无需支持权重。
- 发现候选不足时不重复候选，展示全部可用候选。
- `DiscoverConfig` 作为 `EffectConfig.discover` 字段。
- 金色效果全部写入 `goldenEffects`。
- JSON 解析使用 Newtonsoft Json。
- `fallbackEffects` MVP 阶段不实现，复杂牌先用代码特殊处理。
- 特殊复杂牌的代码处理入口使用 `effectId`。

## 18. 待确认问题

- 暂无。
