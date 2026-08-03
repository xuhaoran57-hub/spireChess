using NUnit.Framework;
using SpireChess.UI;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Tests.EditMode
{
    public sealed class JournalPresentationArtworkResourcesTests
    {
        private const string AssetRoot =
            "Assets/Resources/Presentation/Journal/";

        [TestCase("journal_cover_v0_4_0.png", 1086, 1448)]
        [TestCase("journal_contents_v0_4_0.png", 1672, 941)]
        [TestCase("journal_hero_warrior_v0_4_0.png", 1024, 1536)]
        [TestCase("journal_hero_mage_v0_4_0.png", 1024, 1535)]
        [TestCase("journal_hero_rogue_v0_4_0.png", 1024, 1536)]
        [TestCase("journal_hero_locked_v0_4_0.png", 1024, 1536)]
        [TestCase("journal_chapter_wilderness_v0_4_0.png", 1672, 941)]
        [TestCase("journal_chapter_startrail_highlands_v0_4_0.png", 1672, 941)]
        [TestCase("journal_chapter_soulforge_city_v0_4_0.png", 1672, 941)]
        [TestCase("journal_ending_v0_4_0.png", 1672, 941)]
        public void FormalJournalArtwork_IsImportedAsExpectedSprite(
            string fileName,
            int expectedWidth,
            int expectedHeight)
        {
            var path = AssetRoot + fileName;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(sprite, Is.Not.Null, path);
            Assert.That(sprite.texture.width, Is.EqualTo(expectedWidth), path);
            Assert.That(sprite.texture.height, Is.EqualTo(expectedHeight), path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType,
                Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode,
                Is.EqualTo(SpriteImportMode.Single), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.maxTextureSize, Is.EqualTo(2048), path);
        }

        [Test]
        public void JournalArtworkMappings_ResolveEveryFormalUiSlot()
        {
            Assert.That(PresentationArtworkResources.LoadJournalCover(),
                Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalContents(),
                Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalHero("warrior"),
                Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalHero("mage"),
                Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalHero("rogue"),
                Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalLockedHero(),
                Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalChapter(
                    "map_wilderness"), Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalChapter(
                    "map_startrail_highlands"), Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalChapter(
                    "map_soulforge_city"), Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalEnding(),
                Is.Not.Null);
            Assert.That(PresentationArtworkResources.LoadJournalHero("unknown"),
                Is.Null);
            Assert.That(PresentationArtworkResources.LoadJournalChapter(
                    "unknown"), Is.Null);
        }
    }
}
