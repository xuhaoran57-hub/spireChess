using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Editor;
using SpireChess.UI;
using SpireChess.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpireChess.Tests.EditMode
{
    public sealed class PresentationSpriteCatalogTests
    {
        private const string CatalogPath =
            "Assets/Configs/Presentation/PresentationSpriteCatalog.asset";

        private const string DiagnosticArtworkPath =
            "Assets/Art/Presentation/UI/Diagnostics/fallback_missing_art.png";

        private const int SampleArtworkCount = 22;

        private static readonly (string ConfigId, string ArtId)[] SampleMinions =
        {
            ("forge_soul_shield_squire", "placeholder_card_forge_soul_shield_squire"),
            ("tempering_mender", "placeholder_card_tempering_mender"),
            ("cracked_armor_avenger", "placeholder_card_cracked_armor_avenger"),
            ("undying_furnace_king", "placeholder_card_undying_furnace_king"),
            ("young_deer_spirit", "placeholder_card_young_deer_spirit"),
            ("rotleaf_heir", "placeholder_card_rotleaf_heir"),
            ("fox_den_matriarch", "placeholder_card_fox_den_matriarch"),
            ("ten_thousand_hoof_surge", "placeholder_card_ten_thousand_hoof_surge"),
            ("astrolabe_calibrator", "placeholder_card_astrolabe_calibrator"),
            ("secret_page_refractor", "placeholder_card_secret_page_refractor"),
            ("star_map_broker", "placeholder_card_star_map_broker"),
            ("sky_covenant_bearer", "placeholder_card_sky_covenant_bearer"),
            ("token_young_spirit", "placeholder_token_young_spirit"),
            (
                "token_two_tailed_fox_shadow",
                "placeholder_token_two_tailed_fox_shadow"
            ),
            ("token_swift_young_spirit", "placeholder_token_swift_young_spirit")
        };

        private static readonly (string ConfigId, string ArtId)[] SampleSpells =
        {
            ("minor_tempering", "placeholder_spell_minor_tempering"),
            ("free_refresh", "placeholder_spell_free_refresh"),
            ("advanced_discovery", "placeholder_spell_advanced_discovery"),
            ("prebattle_benediction", "placeholder_spell_prebattle_benediction")
        };

        private static readonly (string ConfigId, string IconId)[] SampleRelics =
        {
            ("crown_echo_bell", "icon_relic_crown_echo_bell"),
            ("crown_thousand_shields", "icon_relic_crown_thousand_shields"),
            ("curio_refresh_gear", "icon_relic_curio_refresh_gear")
        };

        private static readonly ArtworkExpectation[] ApprovedArtworks =
        {
            // G2 sample scope: 12 core minions.
            new ArtworkExpectation(
                "placeholder_card_forge_soul_shield_squire",
                "card_minion_forge_soul_shield_squire",
                "Assets/Art/Presentation/Cards/Minions/ForgeSoul/" +
                "card_minion_forge_soul_shield_squire.png",
                0.31f),
            new ArtworkExpectation(
                "placeholder_card_undying_furnace_king",
                "card_minion_undying_furnace_king",
                "Assets/Art/Presentation/Cards/Minions/ForgeSoul/" +
                "card_minion_undying_furnace_king.png",
                0.18f),
            new ArtworkExpectation(
                "placeholder_card_young_deer_spirit",
                "card_minion_young_deer_spirit",
                "Assets/Art/Presentation/Cards/Minions/WildSpirit/" +
                "card_minion_young_deer_spirit.png",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_ten_thousand_hoof_surge",
                "card_minion_ten_thousand_hoof_surge",
                "Assets/Art/Presentation/Cards/Minions/WildSpirit/" +
                "card_minion_ten_thousand_hoof_surge.png",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_astrolabe_calibrator",
                "card_minion_astrolabe_calibrator",
                "Assets/Art/Presentation/Cards/Minions/Starbound/" +
                "card_minion_astrolabe_calibrator.png",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_sky_covenant_bearer",
                "card_minion_sky_covenant_bearer",
                "Assets/Art/Presentation/Cards/Minions/Starbound/" +
                "card_minion_sky_covenant_bearer.png",
                0.25f),
            new ArtworkExpectation(
                "placeholder_card_tempering_mender",
                "card_minion_tempering_mender",
                "Assets/Art/Presentation/Cards/Minions/ForgeSoul/" +
                "card_minion_tempering_mender.png",
                0.31f),
            new ArtworkExpectation(
                "placeholder_card_cracked_armor_avenger",
                "card_minion_cracked_armor_avenger",
                "Assets/Art/Presentation/Cards/Minions/ForgeSoul/" +
                "card_minion_cracked_armor_avenger.png",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_rotleaf_heir",
                "card_minion_rotleaf_heir",
                "Assets/Art/Presentation/Cards/Minions/WildSpirit/" +
                "card_minion_rotleaf_heir.png",
                0.29f),
            new ArtworkExpectation(
                "placeholder_card_fox_den_matriarch",
                "card_minion_fox_den_matriarch",
                "Assets/Art/Presentation/Cards/Minions/WildSpirit/" +
                "card_minion_fox_den_matriarch.png",
                0.50f),
            new ArtworkExpectation(
                "placeholder_card_secret_page_refractor",
                "card_minion_secret_page_refractor",
                "Assets/Art/Presentation/Cards/Minions/Starbound/" +
                "card_minion_secret_page_refractor.png",
                0.28f),
            new ArtworkExpectation(
                "placeholder_card_star_map_broker",
                "card_minion_star_map_broker",
                "Assets/Art/Presentation/Cards/Minions/Starbound/" +
                "card_minion_star_map_broker.png",
                0.30f),

            // G2 sample scope: 3 tokens.
            new ArtworkExpectation(
                "placeholder_token_young_spirit",
                "card_token_token_young_spirit",
                "Assets/Art/Presentation/Cards/Tokens/" +
                "card_token_token_young_spirit.png",
                0.31f),
            new ArtworkExpectation(
                "placeholder_token_two_tailed_fox_shadow",
                "card_token_token_two_tailed_fox_shadow",
                "Assets/Art/Presentation/Cards/Tokens/" +
                "card_token_token_two_tailed_fox_shadow.png",
                0.50f),
            new ArtworkExpectation(
                "placeholder_token_swift_young_spirit",
                "card_token_token_swift_young_spirit",
                "Assets/Art/Presentation/Cards/Tokens/" +
                "card_token_token_swift_young_spirit.png",
                0.50f),

            // G2 sample scope: 4 spells.
            new ArtworkExpectation(
                "placeholder_spell_minor_tempering",
                "card_spell_minor_tempering",
                "Assets/Art/Presentation/Cards/Spells/" +
                "card_spell_minor_tempering.png",
                0.42f),
            new ArtworkExpectation(
                "placeholder_spell_free_refresh",
                "card_spell_free_refresh",
                "Assets/Art/Presentation/Cards/Spells/" +
                "card_spell_free_refresh.png",
                0.42f),
            new ArtworkExpectation(
                "placeholder_spell_advanced_discovery",
                "card_spell_advanced_discovery",
                "Assets/Art/Presentation/Cards/Spells/" +
                "card_spell_advanced_discovery.png",
                0.38f),
            new ArtworkExpectation(
                "placeholder_spell_prebattle_benediction",
                "card_spell_prebattle_benediction",
                "Assets/Art/Presentation/Cards/Spells/" +
                "card_spell_prebattle_benediction.png",
                0.36f),

            // G2 sample scope: 3 relic uiIconIds.
            new ArtworkExpectation(
                "icon_relic_crown_echo_bell",
                "icon_relic_crown_echo_bell",
                "Assets/Art/Presentation/Icons/Relics/" +
                "icon_relic_crown_echo_bell.png",
                0.50f),
            new ArtworkExpectation(
                "icon_relic_crown_thousand_shields",
                "icon_relic_crown_thousand_shields",
                "Assets/Art/Presentation/Icons/Relics/" +
                "icon_relic_crown_thousand_shields.png",
                0.50f),
            new ArtworkExpectation(
                "icon_relic_curio_refresh_gear",
                "icon_relic_curio_refresh_gear",
                "Assets/Art/Presentation/Icons/Relics/" +
                "icon_relic_curio_refresh_gear.png",
                0.50f),

            // Existing Wayfarer art remains as two approved blind-spot anchors.
            new ArtworkExpectation(
                "placeholder_card_traveling_physician",
                "card_minion_traveling_physician",
                "Assets/Art/Presentation/Cards/Minions/Wayfarer/" +
                "card_minion_traveling_physician.png",
                0.27f),
            new ArtworkExpectation(
                "placeholder_card_many_arts_apprentice",
                "card_minion_many_arts_apprentice",
                "Assets/Art/Presentation/Cards/Minions/Wayfarer/" +
                "card_minion_many_arts_apprentice.png",
                0.27f)
        };

        [Test]
        public void Catalog_SampleScopeAndBlindSpotAnchorsResolveExactly()
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
            Assert.That(
                ApprovedArtworks,
                Has.Length.EqualTo(SampleArtworkCount + 2));
            var promotedArtIds =
                new HashSet<string>(StringComparer.Ordinal);
            if (LightStorybookRuntimePromotionBuilder.IsPromoted())
            {
                foreach (var entry in
                    LightStorybookRuntimePromotionBuilder
                        .CreatePlan()
                        .Entries)
                {
                    promotedArtIds.Add(entry.ArtId);
                }
            }
            foreach (var expected in ApprovedArtworks)
            {
                // TryGetArtwork is intentionally used here. ResolveArtwork
                // would allow a fallback to mask a missing sample asset.
                var found = catalog.TryGetArtwork(
                    expected.ArtId,
                    out var sprite,
                    out var focalPointY);
                Assert.That(found, Is.True, expected.ArtId);
                Assert.That(sprite, Is.Not.Null, expected.ArtId);
                var isRuntimePromotion =
                    promotedArtIds.Contains(expected.ArtId);
                Assert.That(
                    sprite.name,
                    Is.EqualTo(
                        isRuntimePromotion
                            ? expected.ArtId
                            : expected.SpriteName),
                    expected.ArtId);
                Assert.That(
                    AssetDatabase.GetAssetPath(sprite),
                    Is.EqualTo(
                        isRuntimePromotion
                            ? LightStorybookRuntimePromotionBuilder
                                .GetRuntimeAssetPath(expected.ArtId)
                            : expected.AssetPath),
                    expected.ArtId);
                Assert.That(
                    focalPointY,
                    Is.EqualTo(
                            isRuntimePromotion
                                ? 0.5f
                                : expected.FocalPointY)
                        .Within(0.0001f),
                    expected.ArtId);
            }
        }

        [Test]
        public void Catalog_CurrentSampleConfigIdsResolveExactly()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();

            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join("\n", validation.Errors));

            foreach (var expected in SampleMinions)
            {
                Assert.That(
                    configs.TryGetMinion(expected.ConfigId, out var minion),
                    Is.True,
                    expected.ConfigId);
                Assert.That(minion.ArtId, Is.EqualTo(expected.ArtId));
                Assert.That(
                    catalog.TryGetArtwork(minion.ArtId, out _),
                    Is.True,
                    expected.ConfigId);
            }

            foreach (var expected in SampleSpells)
            {
                Assert.That(
                    configs.TryGetSpell(expected.ConfigId, out var spell),
                    Is.True,
                    expected.ConfigId);
                Assert.That(spell.ArtId, Is.EqualTo(expected.ArtId));
                Assert.That(
                    catalog.TryGetArtwork(spell.ArtId, out _),
                    Is.True,
                    expected.ConfigId);
            }

            foreach (var expected in SampleRelics)
            {
                Assert.That(
                    configs.TryGetRelic(expected.ConfigId, out var relic),
                    Is.True,
                    expected.ConfigId);
                Assert.That(relic.UiIconId, Is.EqualTo(expected.IconId));
                Assert.That(
                    catalog.TryGetArtwork(relic.UiIconId, out _),
                    Is.True,
                    expected.ConfigId);
            }
        }

        [Test]
        public void Catalog_SerializedArtworkEntriesAreCompleteAndUnique()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var serializedCatalog = new SerializedObject(catalog);
            serializedCatalog.Update();
            var artworks = serializedCatalog.FindProperty("artworks");
            Assert.That(artworks, Is.Not.Null);
            Assert.That(artworks.isArray, Is.True);

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < artworks.arraySize; index++)
            {
                var entry = artworks.GetArrayElementAtIndex(index);
                var id = entry.FindPropertyRelative("id").stringValue;
                var sprite =
                    entry.FindPropertyRelative("sprite").objectReferenceValue;

                Assert.That(
                    id,
                    Is.Not.Null.And.Not.Empty,
                    $"Artwork entry {index} has no semantic ID.");
                Assert.That(
                    seenIds.Add(id),
                    Is.True,
                    $"Artwork ID '{id}' is registered more than once.");
                Assert.That(
                    sprite,
                    Is.Not.Null,
                    $"Artwork ID '{id}' has no sprite.");
            }
        }

        [Test]
        public void Catalog_InvalidIdResolvesToCommittedDiagnosticArtwork()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var missingId = "test_missing_art_" + Guid.NewGuid().ToString("N");
            LogAssert.Expect(
                LogType.Warning,
                new Regex(
                    "Presentation artwork '" +
                    Regex.Escape(missingId) +
                    "' is missing"));

            var resolution = catalog.ResolveArtwork(
                missingId,
                "test_missing_fallback_" + Guid.NewGuid().ToString("N"),
                out var sprite,
                out var focalPointY);

            Assert.That(resolution, Is.EqualTo(ArtworkResolution.Diagnostic));
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.name, Is.EqualTo("fallback_missing_art"));
            Assert.That(
                AssetDatabase.GetAssetPath(sprite),
                Is.EqualTo(DiagnosticArtworkPath));
            Assert.That(focalPointY, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ResolveArtwork_UsesExactFallbackAndDiagnosticInOrder()
        {
            var catalog = ScriptableObject.CreateInstance<PresentationSpriteCatalog>();
            var exact = CreateSprite("exact");
            var fallback = CreateSprite("fallback");
            var diagnostic = CreateSprite("diagnostic");
            try
            {
                SetArtworks(
                    catalog,
                    ("exact_id", exact, 0.2f),
                    ("fallback_minion_starbound", fallback, 0.7f));
                SetPrivateField(catalog, "missingArtwork", diagnostic);

                Assert.That(
                    catalog.ResolveArtwork(
                        "exact_id",
                        "fallback_minion_starbound",
                        out var resolved,
                        out var focalPointY),
                    Is.EqualTo(ArtworkResolution.Exact));
                Assert.That(resolved, Is.SameAs(exact));
                Assert.That(focalPointY, Is.EqualTo(0.2f).Within(0.0001f));

                LogAssert.Expect(
                    LogType.Warning,
                    new Regex("Presentation artwork 'unknown' is missing"));
                Assert.That(
                    catalog.ResolveArtwork(
                        "unknown",
                        "fallback_minion_starbound",
                        out resolved,
                        out focalPointY),
                    Is.EqualTo(ArtworkResolution.Fallback));
                Assert.That(resolved, Is.SameAs(fallback));
                Assert.That(focalPointY, Is.EqualTo(0.7f).Within(0.0001f));

                // A repeated render of the same missing ArtId must not log again.
                Assert.That(
                    catalog.ResolveArtwork(
                        "unknown",
                        "fallback_minion_starbound",
                        out resolved,
                        out focalPointY),
                    Is.EqualTo(ArtworkResolution.Fallback));

                LogAssert.Expect(
                    LogType.Warning,
                    new Regex("Presentation artwork 'invalid' is missing"));
                Assert.That(
                    catalog.ResolveArtwork(
                        "invalid",
                        "fallback_not_registered",
                        out resolved,
                        out focalPointY),
                    Is.EqualTo(ArtworkResolution.Diagnostic));
                Assert.That(resolved, Is.SameAs(diagnostic));
                Assert.That(focalPointY, Is.EqualTo(0.5f));
            }
            finally
            {
                var exactTexture = exact.texture;
                var fallbackTexture = fallback.texture;
                var diagnosticTexture = diagnostic.texture;
                UnityEngine.Object.DestroyImmediate(exact);
                UnityEngine.Object.DestroyImmediate(fallback);
                UnityEngine.Object.DestroyImmediate(diagnostic);
                UnityEngine.Object.DestroyImmediate(exactTexture);
                UnityEngine.Object.DestroyImmediate(fallbackTexture);
                UnityEngine.Object.DestroyImmediate(diagnosticTexture);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void FallbackIds_MapRaceAndSpellTypeToStableCatalogIds()
        {
            Assert.That(
                PresentationArtworkFallbackIds.ForMinion("ForgeSoul"),
                Is.EqualTo("fallback_minion_forge_soul"));
            Assert.That(
                PresentationArtworkFallbackIds.ForMinion("WildSpirit"),
                Is.EqualTo("fallback_minion_wild_spirit"));
            Assert.That(
                PresentationArtworkFallbackIds.ForSpell("CombatBuff"),
                Is.EqualTo("fallback_spell_combat_buff"));
            Assert.That(
                PresentationArtworkFallbackIds.ForSpell(null),
                Is.EqualTo("fallback_spell_generic"));
        }

        private static Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(2, 2)
            {
                name = name + "_texture"
            };
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            sprite.name = name;
            return sprite;
        }

        private static void SetArtworks(
            PresentationSpriteCatalog catalog,
            params (string Id, Sprite Sprite, float FocalPointY)[] values)
        {
            var entryType = typeof(PresentationSpriteCatalog).GetNestedType(
                "ArtworkEntry",
                BindingFlags.NonPublic);
            var entries = Array.CreateInstance(entryType, values.Length);
            for (var index = 0; index < values.Length; index++)
            {
                var entry = Activator.CreateInstance(entryType);
                SetPrivateField(entry, "id", values[index].Id);
                SetPrivateField(entry, "sprite", values[index].Sprite);
                SetPrivateField(entry, "focalPointY", values[index].FocalPointY);
                entries.SetValue(entry, index);
            }

            SetPrivateField(catalog, "artworks", entries);
            typeof(PresentationSpriteCatalog)
                .GetMethod("RebuildLookup", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(catalog, null);
        }

        private static void SetPrivateField(
            object target,
            string name,
            object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private readonly struct ArtworkExpectation
        {
            public ArtworkExpectation(
                string artId,
                string spriteName,
                string assetPath,
                float focalPointY)
            {
                ArtId = artId;
                SpriteName = spriteName;
                AssetPath = assetPath;
                FocalPointY = focalPointY;
            }

            public string ArtId { get; }
            public string SpriteName { get; }
            public string AssetPath { get; }
            public float FocalPointY { get; }
        }
    }
}
