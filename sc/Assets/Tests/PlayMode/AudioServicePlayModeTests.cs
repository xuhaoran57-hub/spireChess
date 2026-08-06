using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpireChess.Tests
{
    public sealed class AudioServicePlayModeTests
    {
        [UnityTest]
        public IEnumerator MusicTransitionsAndStop_ClearBothMusicSources()
        {
            var service = AudioService.EnsurePresent();
            var originalCatalog = service.Catalog;
            var catalog =
                ScriptableObject.CreateInstance<PresentationAudioCatalog>();
            var clipA = CreateClip("music_a");
            var clipB = CreateClip("music_b");
            var clipC = CreateClip("music_c");

            try
            {
                SetPrivateField(
                    catalog,
                    "cues",
                    new[]
                    {
                        CreateMusicCue("music_a", clipA),
                        CreateMusicCue("music_b", clipB),
                        CreateMusicCue("music_c", clipC)
                    });
                InvokePrivate(catalog, "RebuildLookup");
                service.StopMusic(0f);
                service.Configure(catalog);

                Assert.That(service.PlayMusic("music_a", 0f), Is.True);
                Assert.That(service.PlayMusic("music_b", 0.25f), Is.True);
                yield return null;

                var sourceA = GetPrivateField<AudioSource>(
                    service,
                    "musicSourceA");
                var sourceB = GetPrivateField<AudioSource>(
                    service,
                    "musicSourceB");
                service.StopMusic(0.05f);

                Assert.That(
                    new[] { sourceA.clip, sourceB.clip },
                    Has.Exactly(1).Null,
                    "Stopping an interrupted cross-fade must retire the old " +
                    "music voice immediately.");
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(sourceA.clip, Is.Null);
                Assert.That(sourceB.clip, Is.Null);
                Assert.That(service.CurrentMusicCueId, Is.Null);

                Assert.That(service.PlayMusic("music_a", 0f), Is.True);
                Assert.That(service.PlayMusic("music_b", 0.15f), Is.True);
                yield return null;
                Assert.That(service.PlayMusic("music_c", 0.05f), Is.True);
                yield return new WaitForSecondsRealtime(0.1f);

                Assert.That(service.CurrentMusicCueId, Is.EqualTo("music_c"));
                Assert.That(
                    new[] { sourceA.clip, sourceB.clip },
                    Has.Exactly(1).SameAs(clipC));
                Assert.That(
                    new[] { sourceA.clip, sourceB.clip },
                    Has.Exactly(1).Null,
                    "Rapid music changes must converge to one active voice.");
            }
            finally
            {
                service.StopMusic(0f);
                service.Configure(originalCatalog);
                Object.Destroy(catalog);
                Object.Destroy(clipA);
                Object.Destroy(clipB);
                Object.Destroy(clipC);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator StopAllTransientCues_ReleasesActiveCueCapacity()
        {
            var service = AudioService.EnsurePresent();
            var originalCatalog = service.Catalog;
            var catalog =
                ScriptableObject.CreateInstance<PresentationAudioCatalog>();
            var clip = CreateClip("showcase_impact");

            try
            {
                SetPrivateField(
                    catalog,
                    "cues",
                    new[]
                    {
                        new PresentationAudioCueDefinition(
                            "showcase_impact",
                            PresentationAudioBus.Sfx,
                            new[] { clip },
                            concurrencyLimit: 1,
                            assetStatus:
                                PresentationAudioCueAssetStatus.Placeholder)
                    });
                InvokePrivate(catalog, "RebuildLookup");
                service.StopAllTransientCues();
                service.Configure(catalog);

                Assert.That(service.PlayCue("showcase_impact"), Is.True);
                Assert.That(
                    service.PlayCue("showcase_impact"),
                    Is.False,
                    "The test cue has a single active-voice slot.");

                service.StopAllTransientCues();

                Assert.That(
                    service.PlayCue("showcase_impact"),
                    Is.True,
                    "Skipping must stop the voice and release its limiter slot.");
            }
            finally
            {
                service.StopAllTransientCues();
                service.Configure(originalCatalog);
                Object.Destroy(catalog);
                Object.Destroy(clip);
            }

            yield return null;
        }

        private static PresentationAudioCueDefinition CreateMusicCue(
            string id,
            AudioClip clip)
        {
            return new PresentationAudioCueDefinition(
                id,
                PresentationAudioBus.Music,
                new[] { clip },
                loop: true,
                assetStatus:
                    PresentationAudioCueAssetStatus.Placeholder);
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(
                name,
                44100,
                1,
                44100,
                false);
        }

        private static T GetPrivateField<T>(object target, string name)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(
            object target,
            string name,
            object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string name)
        {
            var method = target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }
    }
}
