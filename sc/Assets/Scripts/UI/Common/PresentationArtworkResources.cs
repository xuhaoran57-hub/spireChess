using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI
{
    public static class PresentationArtworkResources
    {
        public const string MainMenuBackdropPath =
            "Presentation/Backdrops/backdrop_main_menu";
        public const string ShopBackdropPath =
            "Presentation/Backdrops/backdrop_shop";
        public const string RunMapBackdropPath =
            "Presentation/Backdrops/backdrop_floor_map";
        public const string BattleBackdropPath =
            "Presentation/Backdrops/backdrop_battle";
        public const string EventArtworkRoot = "Presentation/Events/";
        public const string JournalArtworkRoot = "Presentation/Journal/";
        public const string JournalCoverPath =
            JournalArtworkRoot + "journal_cover_v0_4_0";
        public const string JournalContentsPath =
            JournalArtworkRoot + "journal_contents_v0_4_0";
        public const string JournalEndingPath =
            JournalArtworkRoot + "journal_ending_v0_4_0";

        public static string GetBackdropPath(
            PresentationBackdropVariant variant)
        {
            switch (variant)
            {
                case PresentationBackdropVariant.MainMenu:
                    return MainMenuBackdropPath;
                case PresentationBackdropVariant.Shop:
                    return ShopBackdropPath;
                case PresentationBackdropVariant.RunMap:
                    return RunMapBackdropPath;
                case PresentationBackdropVariant.Battle:
                    return BattleBackdropPath;
                default:
                    return string.Empty;
            }
        }

        public static Sprite LoadBackdrop(
            PresentationBackdropVariant variant)
        {
            var path = GetBackdropPath(variant);
            return string.IsNullOrEmpty(path)
                ? null
                : Resources.Load<Sprite>(path);
        }

        public static Sprite LoadEvent(string artworkId)
        {
            return string.IsNullOrWhiteSpace(artworkId)
                ? null
                : Resources.Load<Sprite>(
                    EventArtworkRoot + artworkId.Trim());
        }

        public static Sprite LoadJournalCover()
        {
            return Resources.Load<Sprite>(JournalCoverPath);
        }

        public static Sprite LoadJournalContents()
        {
            return Resources.Load<Sprite>(JournalContentsPath);
        }

        public static Sprite LoadJournalHero(string heroId)
        {
            switch ((heroId ?? string.Empty).Trim())
            {
                case "warrior":
                    return LoadJournal("journal_hero_warrior_v0_4_0");
                case "mage":
                    return LoadJournal("journal_hero_mage_v0_4_0");
                case "rogue":
                    return LoadJournal("journal_hero_rogue_v0_4_0");
                default:
                    return null;
            }
        }

        public static Sprite LoadJournalLockedHero()
        {
            return LoadJournal("journal_hero_locked_v0_4_0");
        }

        public static Sprite LoadJournalChapter(string mapId)
        {
            switch ((mapId ?? string.Empty).Trim())
            {
                case "map_wilderness":
                    return LoadJournal("journal_chapter_wilderness_v0_4_0");
                case "map_startrail_highlands":
                    return LoadJournal(
                        "journal_chapter_startrail_highlands_v0_4_0");
                case "map_soulforge_city":
                    return LoadJournal(
                        "journal_chapter_soulforge_city_v0_4_0");
                default:
                    return null;
            }
        }

        public static Sprite LoadJournalEnding()
        {
            return Resources.Load<Sprite>(JournalEndingPath);
        }

        private static Sprite LoadJournal(string artworkId)
        {
            return Resources.Load<Sprite>(JournalArtworkRoot + artworkId);
        }

        public static Image EnsureImage(
            Transform parent,
            string name,
            Sprite sprite,
            Color tint,
            bool stretch)
        {
            if (parent == null)
            {
                return null;
            }

            var child = parent.Find(name);
            Image image;
            if (child == null)
            {
                var artworkObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                artworkObject.transform.SetParent(parent, false);
                image = artworkObject.GetComponent<Image>();
            }
            else
            {
                image = child.GetComponent<Image>();
            }

            if (image == null)
            {
                return null;
            }

            image.sprite = sprite;
            image.color = tint;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.gameObject.SetActive(sprite != null);
            if (stretch)
            {
                var rect = image.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            image.transform.SetAsFirstSibling();
            return image;
        }
    }
}
