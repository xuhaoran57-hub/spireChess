using NUnit.Framework;
using SpireChess.UI;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Tests.EditMode
{
    public sealed class PresentationSpriteCatalogTests
    {
        private const string CatalogPath =
            "Assets/Configs/Presentation/PresentationSpriteCatalog.asset";

        private static readonly ArtworkExpectation[] ApprovedArtworks =
        {
            new ArtworkExpectation(
                "placeholder_card_forge_soul_shield_squire",
                "card_minion_forge_soul_shield_squire",
                0.31f),
            new ArtworkExpectation(
                "placeholder_card_undying_furnace_king",
                "card_minion_undying_furnace_king",
                0.18f),
            new ArtworkExpectation(
                "placeholder_card_young_deer_spirit",
                "card_minion_young_deer_spirit",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_ten_thousand_hoof_surge",
                "card_minion_ten_thousand_hoof_surge",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_astrolabe_calibrator",
                "card_minion_astrolabe_calibrator",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_sky_covenant_bearer",
                "card_minion_sky_covenant_bearer",
                0.25f),
            new ArtworkExpectation(
                "placeholder_card_traveling_physician",
                "card_minion_traveling_physician",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_many_arts_apprentice",
                "card_minion_many_arts_apprentice",
                0.27f)
        };

        [Test]
        public void Catalog_ContainsApprovedCardComponentsAndArtworks()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.HasCompleteCardNumericSet, Is.True);
            Assert.That(
                catalog.CardAttackTag.border,
                Is.EqualTo(new Vector4(58f, 16f, 25f, 16f)));
            Assert.That(
                catalog.CardHealthTag.border,
                Is.EqualTo(new Vector4(25f, 16f, 69f, 16f)));
            Assert.That(ApprovedArtworks, Has.Length.EqualTo(8));
            foreach (var expected in ApprovedArtworks)
            {
                var found = catalog.TryGetArtwork(
                    expected.ArtId,
                    out var sprite,
                    out var focalPointY);
                Assert.That(found, Is.True, expected.ArtId);
                Assert.That(sprite, Is.Not.Null, expected.ArtId);
                Assert.That(
                    sprite.name,
                    Is.EqualTo(expected.SpriteName),
                    expected.ArtId);
                Assert.That(
                    focalPointY,
                    Is.EqualTo(expected.FocalPointY).Within(0.0001f),
                    expected.ArtId);
            }
        }

        private readonly struct ArtworkExpectation
        {
            public ArtworkExpectation(
                string artId,
                string spriteName,
                float focalPointY)
            {
                ArtId = artId;
                SpriteName = spriteName;
                FocalPointY = focalPointY;
            }

            public string ArtId { get; }
            public string SpriteName { get; }
            public float FocalPointY { get; }
        }
    }
}
