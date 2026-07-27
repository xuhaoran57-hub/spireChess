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
