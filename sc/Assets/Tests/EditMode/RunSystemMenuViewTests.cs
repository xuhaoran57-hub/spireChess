using NUnit.Framework;
using SpireChess.Audio;
using SpireChess.UI.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class RunSystemMenuViewTests
    {
        [Test]
        public void Attach_BuildsSharedSkinnedAudioSettingsWithoutOpeningIt()
        {
            var canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var screenObject = new GameObject(
                    "Screen",
                    typeof(RectTransform),
                    typeof(Text));
                screenObject.transform.SetParent(canvasObject.transform, false);
                var pinnedFont = AssetDatabase.LoadAssetAtPath<Font>(
                    "Assets/Art/Fonts/NotoSansCJKsc-Regular.otf");
                screenObject.GetComponent<Text>().font = pinnedFont;

                var menu = RunSystemMenuView.Attach(
                    screenObject.GetComponent<Text>());

                Assert.That(menu, Is.Not.Null);
                Assert.That(menu.IsOpen, Is.False);
                Assert.That(menu.SettingsOpen, Is.False);
                Assert.That(menu.HasAudioSettings, Is.True);
                Assert.That(
                    menu.transform.Find(
                        "AudioSettingsPanel/SettingsCard/MasterRow/MasterSlider"),
                    Is.Not.Null);
                foreach (var text in menu.GetComponentsInChildren<Text>(true))
                {
                    Assert.That(text.font, Is.SameAs(pinnedFont));
                }

                menu.transform.Find("MenuButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(menu.IsOpen, Is.True);
                menu.transform.Find(
                        "SystemMenuOverlay/SystemMenuCard/AudioSettingsButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(menu.SettingsOpen, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void SaveAndReturnCue_UsesConfirmOnSuccessAndErrorOnFailure()
        {
            Assert.That(
                RunSystemMenuView.ResolveSaveAndReturnCue(true),
                Is.EqualTo(PresentationAudioCueIds.UiConfirm));
            Assert.That(
                RunSystemMenuView.ResolveSaveAndReturnCue(false),
                Is.EqualTo(PresentationAudioCueIds.UiError));
        }
    }
}
