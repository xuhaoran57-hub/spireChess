using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Editor;
using SpireChess.UI;
using UnityEditor;

namespace SpireChess.Tests.EditMode
{
    public sealed class LightStorybookProductionCatalogTests
    {
        private static readonly bool RuntimeIsPromoted =
            LightStorybookRuntimePromotionBuilder.IsPromoted();

        private static readonly string[] BatchOneArtIds =
        {
            "placeholder_card_copper_ring_apprentice",
            "placeholder_card_hearth_core_spark",
            "placeholder_card_stardust_attendant",
            "placeholder_card_stargazing_apprentice",
            "placeholder_card_wandering_swordsman",
            "placeholder_card_rending_cub",
            "placeholder_card_moss_mark_seedling"
        };

        private static readonly string[] BatchTwoArtIds =
        {
            "placeholder_card_ember_engraver",
            "placeholder_card_shieldbreaker_blade_blank",
            "placeholder_card_shieldwall_furnace_keeper",
            "placeholder_card_moon_phase_scribe",
            "placeholder_card_rune_ward_reader",
            "placeholder_card_star_etched_timekeeper",
            "placeholder_card_black_market_vendor",
            "placeholder_card_mercenary_shieldbearer",
            "placeholder_card_root_devourer",
            "placeholder_card_swiftwing_forest_hawk",
            "placeholder_card_two_tailed_fox_spirit"
        };

        private static readonly string[] BatchThreeArtIds =
        {
            "placeholder_card_counterflow_smith",
            "placeholder_card_molten_core_standard",
            "placeholder_card_oathblade_armor",
            "placeholder_card_echo_starchanter",
            "placeholder_card_ancient_moss_hatchling",
            "placeholder_card_many_branch_invoker",
            "placeholder_card_tuskherd_pathrunner"
        };

        private static readonly string[] BatchFourArtIds =
        {
            "placeholder_card_cinder_armor_arbiter",
            "placeholder_card_hearth_core_aegis_officer",
            "placeholder_card_ringing_iron_bastion",
            "placeholder_card_falling_light_arbiter",
            "placeholder_card_star_ring_treasurer",
            "placeholder_card_stargate_lecturer",
            "placeholder_card_formation_breaker_mercenary",
            "placeholder_card_pack_hunt_inspector",
            "placeholder_card_hundred_song_herd",
            "placeholder_card_mountain_belly_soul_eater",
            "placeholder_card_vinecrown_priest"
        };

        private static readonly string[] BatchFiveArtIds =
        {
            "placeholder_card_oathbroken_blade_soul",
            "placeholder_card_thousand_ring_tomb_guardian",
            "placeholder_card_falling_star_prophet",
            "placeholder_card_fate_shuffler",
            "placeholder_card_royal_bounty_hunter",
            "placeholder_card_world_eating_final_bloom"
        };

        private static readonly string[] BatchSixArtIds =
        {
            "placeholder_spell_delayed_supply",
            "placeholder_spell_triple_discovery_reward",
            "placeholder_spell_precise_training",
            "placeholder_spell_thickhide_potion",
            "placeholder_spell_prototype_copy",
            "placeholder_spell_warband_forging",
            "placeholder_spell_bloodline_awakening",
            "placeholder_spell_army_ascension",
            "placeholder_spell_fate_reforging"
        };

        [Test]
        public void BatchOneCatalog_AddsSevenExactTierOneArtworks()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch1Builder.CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artId in BatchOneArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite, out _),
                    Is.True,
                    artId);
                Assert.That(sprite, Is.Not.Null, artId);
                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(
                    path,
                    Does.StartWith(
                        "Assets/Art/Presentation/Calibration/" +
                        "LightStorybookProductionV033Batch01/"),
                    artId);
                paths.Add(path);
            }
            Assert.That(paths.Count, Is.EqualTo(BatchOneArtIds.Length));
        }

        [Test]
        public void BatchOneCatalog_DoesNotModifyFrozenV032Catalog()
        {
            var frozen =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookFormalCatalogBuilder.CatalogPath);

            Assert.That(frozen, Is.Not.Null);
            foreach (var artId in BatchOneArtIds)
            {
                Assert.That(
                    frozen.TryGetArtwork(artId, out _),
                    Is.False,
                    artId);
            }
        }

        [Test]
        public void BatchTwoCatalog_AddsElevenExactTierTwoArtworks()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch2Builder.CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artId in BatchTwoArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite, out _),
                    Is.True,
                    artId);
                Assert.That(sprite, Is.Not.Null, artId);
                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(
                    path,
                    Does.StartWith(
                        "Assets/Art/Presentation/Calibration/" +
                        "LightStorybookProductionV033Batch02/"),
                    artId);
                paths.Add(path);
            }
            Assert.That(paths.Count, Is.EqualTo(BatchTwoArtIds.Length));

            foreach (var artId in BatchOneArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
        }

        [Test]
        public void BatchTwoCatalog_DoesNotModifyBatchOne_AndRuntimeMatchesPromotionState()
        {
            var batchOne =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch1Builder.CatalogPath);
            var runtime =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    "Assets/Configs/Presentation/" +
                    "PresentationSpriteCatalog.asset");

            Assert.That(batchOne, Is.Not.Null);
            Assert.That(runtime, Is.Not.Null);
            foreach (var artId in BatchTwoArtIds)
            {
                Assert.That(
                    batchOne.TryGetArtwork(artId, out _),
                    Is.False,
                    artId);
                AssertRuntimeMatchesPromotionState(runtime, artId);
            }
        }

        [Test]
        public void BatchThreeCatalog_AddsSevenExactTierThreeArtworks()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch3Builder.CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artId in BatchThreeArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite, out _),
                    Is.True,
                    artId);
                Assert.That(sprite, Is.Not.Null, artId);
                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(
                    path,
                    Does.StartWith(
                        "Assets/Art/Presentation/Calibration/" +
                        "LightStorybookProductionV033Batch03/"),
                    artId);
                paths.Add(path);
            }
            Assert.That(paths.Count, Is.EqualTo(BatchThreeArtIds.Length));

            foreach (var artId in BatchOneArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchTwoArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
        }

        [Test]
        public void BatchThreeCatalog_DoesNotModifyBatchTwo_AndRuntimeMatchesPromotionState()
        {
            var batchTwo =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch2Builder.CatalogPath);
            var runtime =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    "Assets/Configs/Presentation/" +
                    "PresentationSpriteCatalog.asset");

            Assert.That(batchTwo, Is.Not.Null);
            Assert.That(runtime, Is.Not.Null);
            foreach (var artId in BatchThreeArtIds)
            {
                Assert.That(
                    batchTwo.TryGetArtwork(artId, out _),
                    Is.False,
                    artId);
                AssertRuntimeMatchesPromotionState(runtime, artId);
            }
        }

        [Test]
        public void BatchFourCatalog_AddsElevenExactTierFourArtworks()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch4Builder.CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artId in BatchFourArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite, out _),
                    Is.True,
                    artId);
                Assert.That(sprite, Is.Not.Null, artId);
                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(
                    path,
                    Does.StartWith(
                        "Assets/Art/Presentation/Calibration/" +
                        "LightStorybookProductionV033Batch04/"),
                    artId);
                paths.Add(path);
            }
            Assert.That(paths.Count, Is.EqualTo(BatchFourArtIds.Length));

            foreach (var artId in BatchOneArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchTwoArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchThreeArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
        }

        [Test]
        public void BatchFourCatalog_DoesNotModifyBatchThree_AndRuntimeMatchesPromotionState()
        {
            var batchThree =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch3Builder.CatalogPath);
            var runtime =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    "Assets/Configs/Presentation/" +
                    "PresentationSpriteCatalog.asset");

            Assert.That(batchThree, Is.Not.Null);
            Assert.That(runtime, Is.Not.Null);
            foreach (var artId in BatchFourArtIds)
            {
                Assert.That(
                    batchThree.TryGetArtwork(artId, out _),
                    Is.False,
                    artId);
                AssertRuntimeMatchesPromotionState(runtime, artId);
            }
        }

        [Test]
        public void BatchFiveCatalog_AddsSixExactTierFiveArtworks()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch5Builder.CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artId in BatchFiveArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite, out _),
                    Is.True,
                    artId);
                Assert.That(sprite, Is.Not.Null, artId);
                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(
                    path,
                    Does.StartWith(
                        "Assets/Art/Presentation/Calibration/" +
                        "LightStorybookProductionV033Batch05/"),
                    artId);
                paths.Add(path);
            }
            Assert.That(paths.Count, Is.EqualTo(BatchFiveArtIds.Length));

            foreach (var artId in BatchOneArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchTwoArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchThreeArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchFourArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
        }

        [Test]
        public void BatchFiveCatalog_DoesNotModifyBatchFour_AndRuntimeMatchesPromotionState()
        {
            var batchFour =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch4Builder.CatalogPath);
            var runtime =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    "Assets/Configs/Presentation/" +
                    "PresentationSpriteCatalog.asset");

            Assert.That(batchFour, Is.Not.Null);
            Assert.That(runtime, Is.Not.Null);
            foreach (var artId in BatchFiveArtIds)
            {
                Assert.That(
                    batchFour.TryGetArtwork(artId, out _),
                    Is.False,
                    artId);
                AssertRuntimeMatchesPromotionState(runtime, artId);
            }
        }

        [Test]
        public void BatchSixCatalog_AddsNineExactSpellArtworks()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch6Builder.CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artId in BatchSixArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite, out _),
                    Is.True,
                    artId);
                Assert.That(sprite, Is.Not.Null, artId);
                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(
                    path,
                    Does.StartWith(
                        "Assets/Art/Presentation/Calibration/" +
                        "LightStorybookProductionV033Batch06/"),
                    artId);
                paths.Add(path);
            }
            Assert.That(paths.Count, Is.EqualTo(BatchSixArtIds.Length));

            foreach (var artId in BatchOneArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchTwoArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchThreeArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchFourArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
            foreach (var artId in BatchFiveArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out _),
                    Is.True,
                    artId);
            }
        }

        [Test]
        public void BatchSixCatalog_DoesNotModifyBatchFive_AndRuntimeMatchesPromotionState()
        {
            var batchFive =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch5Builder.CatalogPath);
            var runtime =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    "Assets/Configs/Presentation/" +
                    "PresentationSpriteCatalog.asset");

            Assert.That(batchFive, Is.Not.Null);
            Assert.That(runtime, Is.Not.Null);
            foreach (var artId in BatchSixArtIds)
            {
                Assert.That(
                    batchFive.TryGetArtwork(artId, out _),
                    Is.False,
                    artId);
                AssertRuntimeMatchesPromotionState(runtime, artId);
            }
        }

        [Test]
        public void BatchCatalogs_PreservePinnedIdentity_AndFinalCatalogIsComplete()
        {
            var catalogIdentities = new[]
            {
                new
                {
                    Path = LightStorybookProductionBatch1Builder.CatalogPath,
                    Guid = "d9212ca6f5e4c7bb20693784d5abfc97"
                },
                new
                {
                    Path = LightStorybookProductionBatch2Builder.CatalogPath,
                    Guid = "1200000000000000000000000000000c"
                },
                new
                {
                    Path = LightStorybookProductionBatch3Builder.CatalogPath,
                    Guid = "13000000000000000000000000000008"
                },
                new
                {
                    Path = LightStorybookProductionBatch4Builder.CatalogPath,
                    Guid = "1400000000000000000000000000000c"
                },
                new
                {
                    Path = LightStorybookProductionBatch5Builder.CatalogPath,
                    Guid = "15000000000000000000000000000007"
                },
                new
                {
                    Path = LightStorybookProductionBatch6Builder.CatalogPath,
                    Guid = "1600000000000000000000000000000a"
                }
            };
            foreach (var identity in catalogIdentities)
            {
                Assert.That(
                    AssetDatabase.AssetPathToGUID(identity.Path),
                    Is.EqualTo(identity.Guid),
                    identity.Path);
            }

            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch6Builder.CatalogPath);
            var runtime =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    "Assets/Configs/Presentation/" +
                    "PresentationSpriteCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(runtime, Is.Not.Null);
            var serializedCatalog = new SerializedObject(catalog);
            var artworks = serializedCatalog.FindProperty("artworks");
            Assert.That(artworks, Is.Not.Null);
            Assert.That(artworks.arraySize, Is.EqualTo(86));

            var productionArtIds = AllProductionArtIds();
            Assert.That(productionArtIds.Length, Is.EqualTo(51));
            Assert.That(
                productionArtIds.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(productionArtIds.Length));
            foreach (var artId in productionArtIds)
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite),
                    Is.True,
                    artId);
                Assert.That(sprite, Is.Not.Null, artId);
                AssertRuntimeMatchesPromotionState(runtime, artId);
            }
        }

        [Test]
        public void BatchSixProductionTextures_UsePinnedImportPolicy()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookProductionBatch6Builder.CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            foreach (var artId in AllProductionArtIds())
            {
                Assert.That(
                    catalog.TryGetArtwork(artId, out var sprite),
                    Is.True,
                    artId);
                var path = AssetDatabase.GetAssetPath(sprite);
                var importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, artId);
                Assert.That(importer.mipmapEnabled, Is.False, artId);
                Assert.That(
                    importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed),
                    artId);
                Assert.That(importer.maxTextureSize, Is.EqualTo(2048), artId);
                Assert.That(importer.isReadable, Is.False, artId);
            }
        }

        private static string[] AllProductionArtIds()
        {
            return BatchOneArtIds
                .Concat(BatchTwoArtIds)
                .Concat(BatchThreeArtIds)
                .Concat(BatchFourArtIds)
                .Concat(BatchFiveArtIds)
                .Concat(BatchSixArtIds)
                .ToArray();
        }

        private static void AssertRuntimeMatchesPromotionState(
            PresentationSpriteCatalog runtime,
            string artId)
        {
            var found = runtime.TryGetArtwork(
                artId,
                out var sprite);

            Assert.That(
                found,
                Is.EqualTo(RuntimeIsPromoted),
                artId);
            if (!RuntimeIsPromoted)
            {
                return;
            }

            Assert.That(sprite, Is.Not.Null, artId);
            Assert.That(
                AssetDatabase.GetAssetPath(sprite),
                Is.EqualTo(
                    LightStorybookRuntimePromotionBuilder
                        .GetRuntimeAssetPath(artId)),
                artId);
        }
    }
}
