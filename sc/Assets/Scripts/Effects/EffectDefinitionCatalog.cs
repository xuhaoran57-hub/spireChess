using System.Collections.Generic;

namespace SpireChess.Effects
{
    public static class EffectDefinitionCatalog
    {
        public static readonly HashSet<string> Triggers = new HashSet<string>
        {
            "Manual", "OnPlay", "OnSell", "OnRefresh", "OnShopPhaseStart",
            "OnShopPhaseEnd", "OnSpellUsed", "OnBattleStart", "OnAttackBefore",
            "OnKill", "OnShieldGained", "OnShieldLost", "OnSummon", "OnDeath",
            "OnFriendlyDeath", "OnSummonedUnitDeath", "OnEnemySummon",
            "OnSummonFailed", "OnCombatEnd"
        };

        public static readonly HashSet<string> Actions = new HashSet<string>
        {
            "ModifyStats", "AddShield", "RemoveShield", "AddKeyword", "DealDamage",
            "GainGold", "ScheduleGold", "FreeRefresh", "DiscoverMinion",
            "DiscoverSpell", "GrantRandomSpell", "CopyMinion", "SummonToken",
            "ImmediateAttack", "ActivateCardListeners", "SetPendingCombatBuff",
            "SetPostCombatSurvivorBuff", "CopyCombatKeywords", "GainAttackDifference",
            "ModifySelectedPermanentAndOthersCombat", "GainFlourish",
            "GrantRandomMinionAfterCombat"
        };

        public static readonly HashSet<string> Conditions = new HashSet<string>
        {
            "None", "HasKeyword", "HasShield", "TargetAlreadyHasShield", "IsGolden",
            "SubjectIsToken", "SubjectIsNonToken", "SubjectRace", "RaceCountAtLeast",
            "PhaseStatAtLeast", "PhaseStatMultipleOf", "IsMostCommonMainRace", "HasGoldenMinion", "CombatWon",
            "AttackerExists", "NoBoardSpace", "SubjectAdjacent", "AttackBelowHealth",
            "SubjectRaceAndSourceAttackBelowHealth",
            "SubjectIsSelf", "HasAdjacentNonRace", "TriggerCountAtLeast",
            "TriggerCountEquals", "TriggerCountMultipleOf", "EnemyAttackDifferenceAtLeast",
            "HasUnshieldedRaceTarget"
        };

        public static readonly HashSet<string> Durations = new HashSet<string>
        {
            "Permanent", "Combat", "NextCombat", "ShopPhase", "Run"
        };

        public static readonly HashSet<string> ImplementationStatuses = new HashSet<string>
        {
            "Prototype", "Playable", "Disabled"
        };
    }
}
