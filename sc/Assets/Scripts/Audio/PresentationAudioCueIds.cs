using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SpireChess.Audio
{
    public enum PresentationAudioBus
    {
        Music,
        Sfx,
        Ui
    }

    public static class PresentationAudioCueIds
    {
        public const string BgmMainMenu = "bgm_main_menu";
        public const string BgmRunShop = "bgm_run_shop";
        public const string BgmBattleNormal = "bgm_battle_normal";

        public const string UiClick = "ui_click";
        public const string UiConfirm = "ui_confirm";
        public const string UiCancel = "ui_cancel";
        public const string UiError = "ui_error";

        public const string ShopRefresh = "shop_refresh";
        public const string ShopBuy = "shop_buy";
        public const string ShopSell = "shop_sell";
        public const string ShopPlay = "shop_play";
        public const string ShopSpell = "shop_spell";
        public const string ShopTriple = "shop_triple";
        public const string ShopDiscoverOpen = "shop_discover_open";
        public const string ShopDiscoverPick = "shop_discover_pick";
        public const string ShopUpgrade = "shop_upgrade";

        public const string BattleAttackLight = "battle_attack_light";
        public const string BattleHit = "battle_hit";
        public const string BattleShieldGain = "battle_shield_gain";
        public const string BattleShieldBreak = "battle_shield_break";
        public const string BattleStatUp = "battle_stat_up";
        public const string BattleDeath = "battle_death";
        public const string BattleTokenDeath = "battle_token_death";
        public const string BattleSummon = "battle_summon";
        public const string BattleVictory = "battle_victory";
        public const string BattleDefeat = "battle_defeat";

        public const string RunNodeSelect = "run_node_select";
        public const string RunReward = "run_reward";

        private static readonly string[] RequiredMusicValues =
        {
            BgmMainMenu,
            BgmRunShop,
            BgmBattleNormal
        };

        private static readonly string[] RequiredUiValues =
        {
            UiClick,
            UiConfirm,
            UiCancel,
            UiError
        };

        private static readonly string[] RequiredSfxValues =
        {
            ShopRefresh,
            ShopBuy,
            ShopSell,
            ShopPlay,
            ShopSpell,
            ShopTriple,
            ShopDiscoverOpen,
            ShopDiscoverPick,
            ShopUpgrade,
            BattleAttackLight,
            BattleHit,
            BattleShieldGain,
            BattleShieldBreak,
            BattleStatUp,
            BattleDeath,
            BattleTokenDeath,
            BattleSummon,
            BattleVictory,
            BattleDefeat,
            RunNodeSelect,
            RunReward
        };

        private static readonly string[] AllRequiredValues = BuildAllRequired();
        private static readonly HashSet<string> RequiredMusicSet =
            new HashSet<string>(RequiredMusicValues, StringComparer.Ordinal);
        private static readonly HashSet<string> RequiredUiSet =
            new HashSet<string>(RequiredUiValues, StringComparer.Ordinal);
        private static readonly HashSet<string> RequiredSfxSet =
            new HashSet<string>(RequiredSfxValues, StringComparer.Ordinal);
        private static readonly HashSet<string> AllRequiredSet =
            new HashSet<string>(AllRequiredValues, StringComparer.Ordinal);
        private static readonly Dictionary<string, int>
            RequiredVariantCountById =
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    { BgmMainMenu, 1 },
                    { BgmRunShop, 1 },
                    { BgmBattleNormal, 1 },
                    { UiClick, 3 },
                    { UiConfirm, 2 },
                    { UiCancel, 2 },
                    { UiError, 2 },
                    { ShopRefresh, 3 },
                    { ShopBuy, 3 },
                    { ShopSell, 3 },
                    { ShopPlay, 3 },
                    { ShopSpell, 3 },
                    { ShopTriple, 1 },
                    { ShopDiscoverOpen, 1 },
                    { ShopDiscoverPick, 2 },
                    { ShopUpgrade, 1 },
                    { BattleAttackLight, 4 },
                    { BattleHit, 4 },
                    { BattleShieldGain, 3 },
                    { BattleShieldBreak, 3 },
                    { BattleStatUp, 3 },
                    { BattleDeath, 4 },
                    { BattleTokenDeath, 3 },
                    { BattleSummon, 4 },
                    { BattleVictory, 1 },
                    { BattleDefeat, 1 },
                    { RunNodeSelect, 3 },
                    { RunReward, 2 }
                };

        public static readonly ReadOnlyCollection<string> RequiredMusic =
            Array.AsReadOnly(RequiredMusicValues);

        public static readonly ReadOnlyCollection<string> RequiredUi =
            Array.AsReadOnly(RequiredUiValues);

        public static readonly ReadOnlyCollection<string> RequiredSfx =
            Array.AsReadOnly(RequiredSfxValues);

        public static readonly ReadOnlyCollection<string> AllRequired =
            Array.AsReadOnly(AllRequiredValues);

        public static bool IsRequired(string cueId)
        {
            return cueId != null && AllRequiredSet.Contains(cueId);
        }

        public static bool IsRequiredMusic(string cueId)
        {
            return cueId != null && RequiredMusicSet.Contains(cueId);
        }

        public static bool TryGetRequiredVariantCount(
            string cueId,
            out int variantCount)
        {
            if (cueId == null)
            {
                variantCount = 0;
                return false;
            }

            return RequiredVariantCountById.TryGetValue(
                cueId,
                out variantCount);
        }

        public static bool TryGetExpectedBus(
            string cueId,
            out PresentationAudioBus bus)
        {
            if (cueId != null && RequiredMusicSet.Contains(cueId))
            {
                bus = PresentationAudioBus.Music;
                return true;
            }

            if (cueId != null && RequiredUiSet.Contains(cueId))
            {
                bus = PresentationAudioBus.Ui;
                return true;
            }

            if (cueId != null && RequiredSfxSet.Contains(cueId))
            {
                bus = PresentationAudioBus.Sfx;
                return true;
            }

            bus = default(PresentationAudioBus);
            return false;
        }

        private static string[] BuildAllRequired()
        {
            var values = new string[
                RequiredMusicValues.Length +
                RequiredUiValues.Length +
                RequiredSfxValues.Length];
            var offset = 0;
            Array.Copy(
                RequiredMusicValues,
                0,
                values,
                offset,
                RequiredMusicValues.Length);
            offset += RequiredMusicValues.Length;
            Array.Copy(
                RequiredUiValues,
                0,
                values,
                offset,
                RequiredUiValues.Length);
            offset += RequiredUiValues.Length;
            Array.Copy(
                RequiredSfxValues,
                0,
                values,
                offset,
                RequiredSfxValues.Length);
            return values;
        }
    }
}
