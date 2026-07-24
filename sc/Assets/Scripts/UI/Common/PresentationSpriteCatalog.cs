using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpireChess.UI
{
    [CreateAssetMenu(
        fileName = "PresentationSpriteCatalog",
        menuName = "Spire Chess/Presentation/Sprite Catalog")]
    public sealed class PresentationSpriteCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class ArtworkEntry
        {
            [SerializeField] private string id;
            [SerializeField] private Sprite sprite;
            [SerializeField, Range(0f, 1f)] private float focalPointY = 0.5f;

            public string Id => id;
            public Sprite Sprite => sprite;
            public float FocalPointY => Mathf.Clamp01(focalPointY);
        }

        [SerializeField] private Sprite normalCardFrame;
        [SerializeField] private Sprite goldenCardFrame;
        [Header("Card numeric components")]
        [SerializeField] private Sprite cardCostCoin;
        [SerializeField] private Sprite cardTierBookmark;
        [SerializeField] private Sprite cardAttackTag;
        [SerializeField] private Sprite cardHealthTag;
        [Header("Battle standee")]
        [SerializeField] private Sprite battleNormalStandeeFrame;
        [SerializeField] private Sprite battleStandeeFrame;
        [SerializeField] private Sprite battleAttackMedallion;
        [SerializeField] private Sprite battleHealthMedallion;
        [SerializeField] private Sprite battleShieldOverlay;
        [SerializeField] private Sprite battleTauntBase;
        [SerializeField] private Sprite battleDeathrattleSeal;
        [SerializeField] private Sprite battleSplashMark;
        [SerializeField] private ArtworkEntry[] artworks = Array.Empty<ArtworkEntry>();

        private Dictionary<string, ArtworkEntry> artworkById;

        public Sprite NormalCardFrame => normalCardFrame;
        public Sprite GoldenCardFrame => goldenCardFrame;
        public Sprite CardCostCoin => cardCostCoin;
        public Sprite CardTierBookmark => cardTierBookmark;
        public Sprite CardAttackTag => cardAttackTag;
        public Sprite CardHealthTag => cardHealthTag;
        public Sprite BattleNormalStandeeFrame => battleNormalStandeeFrame;
        public Sprite BattleGoldenStandeeFrame => battleStandeeFrame;
        public Sprite BattleAttackMedallion => battleAttackMedallion;
        public Sprite BattleHealthMedallion => battleHealthMedallion;
        public Sprite BattleShieldOverlay => battleShieldOverlay;
        public Sprite BattleTauntBase => battleTauntBase;
        public Sprite BattleDeathrattleSeal => battleDeathrattleSeal;
        public Sprite BattleSplashMark => battleSplashMark;
        public bool HasCompleteCardNumericSet =>
            cardCostCoin != null &&
            cardTierBookmark != null &&
            cardAttackTag != null &&
            cardHealthTag != null;
        public bool HasCompleteBattleStandeeSet =>
            battleNormalStandeeFrame != null &&
            battleStandeeFrame != null &&
            battleAttackMedallion != null &&
            battleHealthMedallion != null &&
            battleShieldOverlay != null &&
            battleTauntBase != null &&
            battleDeathrattleSeal != null &&
            battleSplashMark != null;

        public bool TryGetArtwork(string artId, out Sprite sprite)
        {
            return TryGetArtwork(artId, out sprite, out _);
        }

        public bool TryGetArtwork(
            string artId,
            out Sprite sprite,
            out float focalPointY)
        {
            EnsureLookup();
            sprite = null;
            focalPointY = 0.5f;
            if (string.IsNullOrWhiteSpace(artId) ||
                !artworkById.TryGetValue(artId, out var entry) ||
                entry.Sprite == null)
            {
                return false;
            }

            sprite = entry.Sprite;
            focalPointY = entry.FocalPointY;
            return true;
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
        }
#endif

        private void EnsureLookup()
        {
            if (artworkById == null)
            {
                RebuildLookup();
            }
        }

        private void RebuildLookup()
        {
            artworkById =
                new Dictionary<string, ArtworkEntry>(StringComparer.Ordinal);
            foreach (var entry in artworks ?? Array.Empty<ArtworkEntry>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                {
                    continue;
                }

                artworkById[entry.Id] = entry;
            }
        }
    }
}
