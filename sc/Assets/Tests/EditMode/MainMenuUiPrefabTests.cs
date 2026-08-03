using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.UI;
using SpireChess.UI.Common;
using SpireChess.UI.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class MainMenuUiPrefabTests
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        [Test]
        public void FormalMainMenuPrefab_HasRequiredActionsAndConfirmationDialog()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/MainMenu/PF_MainMenuScreen.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<MainMenuScreenView>(), Is.Not.Null);
            Assert.That(Find(prefab, "NewGameButton"), Is.Not.Null);
            Assert.That(Find(prefab, "ContinueButton"), Is.Not.Null);
            Assert.That(Find(prefab, "SettingsButton"), Is.Not.Null);
            Assert.That(Find(prefab, "DeleteButton"), Is.Not.Null);
            Assert.That(Find(prefab, "QuitButton"), Is.Not.Null);
            Assert.That(Find(prefab, "PF_ConfirmDialog"), Is.Not.Null);
            Assert.That(
                Find(prefab, "AudioSettingsPanel")
                    .GetComponent<AudioSettingsPanelView>()
                    .HasCompleteBindings,
                Is.True);
            Assert.That(Find(prefab, "MasterSlider"), Is.Not.Null);
            Assert.That(Find(prefab, "MusicSlider"), Is.Not.Null);
            Assert.That(Find(prefab, "SFXSlider"), Is.Not.Null);
            Assert.That(Find(prefab, "UISlider"), Is.Not.Null);
            Assert.That(
                Find(prefab, "BackdropArt")
                    .GetComponent<PresentationBackdropGraphic>(),
                Is.Not.Null);
            Assert.That(
                Find(prefab, "SettingsButton")
                    .GetComponentInChildren<Text>().text,
                Is.EqualTo("设置"));
            Assert.That(
                prefab.GetComponentsInChildren<Text>(true)
                    .All(text => text.font != null &&
                                 text.font.name.Contains("NotoSansCJK")),
                Is.True);
        }

        [Test]
        public void ConfirmationDialog_IsAlsoPublishedAsReusablePrefab()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/MainMenu/PF_ConfirmDialog.prefab"),
                Is.Not.Null);
        }

        [TestCase(RunSaveLoadStatus.Missing, false)]
        [TestCase(RunSaveLoadStatus.Valid, true)]
        [TestCase(RunSaveLoadStatus.CorruptJson, true)]
        public void Render_DeleteActionMatchesSavePresence(
            RunSaveLoadStatus status,
            bool expected)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/MainMenu/PF_MainMenuScreen.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                instance.GetComponent<MainMenuScreenView>().Render(
                    new MainMenuScreenState
                    {
                        SaveStatus = status
                    });

                Assert.That(
                    Find(instance, "DeleteButton").GetComponent<Button>().interactable,
                    Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void HeroSelection_ShowsThreeFixedRolesAndLocksUnavailableChoices()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/MainMenu/PF_MainMenuScreen.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<MainMenuScreenView>();
                view.Render(new MainMenuScreenState
                {
                    SaveStatus = RunSaveLoadStatus.Missing,
                    HeroSelectionVisible = true,
                    SelectedHeroId = HeroIds.Warrior,
                    HeroOptions = HeroCatalog.All.Select(hero =>
                        new HeroSelectionOptionState
                        {
                            HeroId = hero.Id,
                            DisplayName = hero.DisplayName,
                            PassiveName = hero.PassiveName,
                            PassiveDescription = hero.PassiveDescription,
                            UnlockCondition = hero.UnlockCondition,
                            IsUnlocked = hero.Id == HeroIds.Warrior,
                            IsSelected = hero.Id == HeroIds.Warrior
                        }).ToArray()
                });

                Assert.That(view.HeroSelectionVisible, Is.True);
                Assert.That(view.IsHeroInteractable(HeroIds.Warrior), Is.True);
                Assert.That(view.IsHeroInteractable(HeroIds.Mage), Is.False);
                Assert.That(view.IsHeroInteractable(HeroIds.Rogue), Is.False);
                Assert.That(Find(instance, "ConfirmHeroButton"), Is.Not.Null);
                Assert.That(
                    Find(instance, HeroIds.Warrior)
                        .GetComponentsInChildren<Text>(true)
                        .Any(text => text.text == "坚甲启程"),
                    Is.True);
                Assert.That(
                    Find(instance, HeroIds.Warrior)
                        .Find("PortraitPlaceholder/NeutralPortraitArtwork")
                        .GetComponent<Image>().sprite,
                    Is.Not.Null);
                Assert.That(
                    Find(instance, HeroIds.Warrior)
                        .Find("PortraitPlaceholder").GetComponent<Text>().text,
                    Is.Empty);
                Assert.That(
                    Find(instance, HeroIds.Mage)
                        .Find("PortraitPlaceholder/NeutralPortraitArtwork")
                        .GetComponent<Image>().sprite,
                    Is.Not.Null);
                Assert.That(
                    Find(instance, HeroIds.Mage)
                        .GetComponentsInChildren<Text>(true)
                        .Any(text => text.text.Contains("击败“荒野”Boss")),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void JournalPages_RuntimeFallbackCreatesCoverAndLocksContentsInputs()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/MainMenu/PF_MainMenuScreen.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<MainMenuScreenView>();
                view.Render(new MainMenuScreenState
                {
                    Page = JournalMenuPage.Cover,
                    SaveStatus = RunSaveLoadStatus.Missing
                });

                Assert.That(Find(instance, "CoverPage"), Is.Not.Null);
                Assert.That(Find(instance, "NeutralCoverArtwork"), Is.Not.Null);
                Assert.That(
                    Find(instance, "NeutralCoverArtwork")
                        .GetComponent<Image>().sprite,
                    Is.Not.Null);
                Assert.That(
                    Find(instance, "NeutralCoverArtwork")
                        .Find("Label").gameObject.activeSelf,
                    Is.False);
                Assert.That(Find(instance, "MapTransitionPage"), Is.Not.Null);
                Assert.That(Find(instance, "NeutralMapArtwork"), Is.Not.Null);
                Assert.That(
                    Find(instance, "NeutralMapArtwork")
                        .GetComponent<Image>().sprite,
                    Is.Not.Null);
                Assert.That(
                    Find(instance, "NeutralMapArtwork")
                        .Find("Label").gameObject.activeSelf,
                    Is.False);
                Assert.That(Find(instance, "JournalContentsArtwork"), Is.Not.Null);
                Assert.That(
                    Find(instance, "JournalContentsArtwork")
                        .GetComponent<Image>().sprite,
                    Is.Not.Null);
                Assert.That(Find(instance, "OpenJournalButton"), Is.Not.Null);
                Assert.That(Find(instance, "CoverSkipButton"), Is.Not.Null);
                Assert.That(Find(instance, "SkipPageTurnButton"), Is.Not.Null);

                view.Render(new MainMenuScreenState
                {
                    Page = JournalMenuPage.Map,
                    SaveStatus = RunSaveLoadStatus.Missing
                });
                Assert.That(Find(instance, "MapTransitionPage").activeSelf, Is.True);
                Assert.That(
                    Find(instance, "NewGameButton").GetComponent<Button>()
                        .interactable,
                    Is.False);

                view.Render(new MainMenuScreenState
                {
                    Page = JournalMenuPage.Contents,
                    IsInputLocked = true,
                    SaveStatus = RunSaveLoadStatus.Valid,
                    ContinueEnabled = true
                });
                Assert.That(
                    Find(instance, "NewGameButton").GetComponent<Button>()
                        .interactable,
                    Is.False);
                Assert.That(
                    Find(instance, "ContinueButton").GetComponent<Button>()
                        .interactable,
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void ContinueSummary_UsesLocalizedLabelsForEveryRunPhase()
        {
            var method = typeof(MainMenuController).GetMethod(
                "ToPhaseLabel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var expected = new Dictionary<RunPhase, string>
            {
                { RunPhase.MapSelection, "地图选择" },
                { RunPhase.EnteringNode, "进入节点" },
                { RunPhase.Shop, "商店" },
                { RunPhase.Battle, "战斗" },
                { RunPhase.BattleResult, "战斗结算" },
                { RunPhase.RewardChoice, "奖励选择" },
                { RunPhase.RelicChoice, "遗珍选择" },
                { RunPhase.EventChoice, "事件选择" },
                { RunPhase.EnhanceChoice, "强化选择" },
                { RunPhase.RestChoice, "休整选择" },
                { RunPhase.FloorComplete, "章节完成" },
                { RunPhase.RunWon, "单局胜利" },
                { RunPhase.RunLost, "单局失败" }
            };

            foreach (RunPhase phase in Enum.GetValues(typeof(RunPhase)))
            {
                Assert.That(expected.ContainsKey(phase), Is.True, phase.ToString());
                Assert.That(
                    method.Invoke(null, new object[] { phase }),
                    Is.EqualTo(expected[phase]),
                    phase.ToString());
            }
        }

        [Test]
        public void ContinueSummary_ShowsHeroChapterHealthAndCurrentArmor()
        {
            var method = typeof(MainMenuController).GetMethod(
                "BuildSummary",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            var summary = new RunSaveSummaryV1
            {
                HeroName = "战士",
                MapName = "荒野",
                Floor = 1,
                Health = 17,
                MaxHealth = 20,
                Armor = 4,
                ShopTurn = 3,
                Phase = RunPhase.MapSelection
            };

            var text = (string)method.Invoke(
                null,
                new object[]
                {
                    new RunSaveLoadResult(RunSaveLoadStatus.Valid),
                    summary
                });

            Assert.That(text, Does.Contain("战士"));
            Assert.That(text, Does.Contain("荒野"));
            Assert.That(text, Does.Contain("生命 17/20"));
            Assert.That(text, Does.Contain("护甲 4"));
        }

        [Test]
        public void MainMenuAndAudioSettings_CopyHasNonCollapsingTextGeometry()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/MainMenu/PF_MainMenuScreen.prefab");

            Assert.That(prefab, Is.Not.Null);
            AssertText(
                prefab.transform.Find("Background/ContentsPage/MenuCard/Title") ??
                    prefab.transform.Find("Background/MenuCard/Title"),
                "旅团日记",
                104f);
            AssertText(
                prefab.transform.Find(
                    "AudioSettingsPanel/SettingsCard/Title"),
                "音频设置",
                76f);
            AssertText(
                prefab.transform.Find(
                    "AudioSettingsPanel/SettingsCard/Hint"),
                "设置保存在本机，与单局存档相互独立",
                48f);
            Assert.That(
                prefab.transform.Find(
                        "AudioSettingsPanel/SettingsCard")
                    .GetComponent<RectTransform>().sizeDelta.y,
                Is.GreaterThanOrEqualTo(700f));

            Assert.That(
                prefab.transform.Find(
                        "AudioSettingsPanel/SettingsCard/MasterRow/Label")
                    .GetComponent<Text>().text,
                Is.EqualTo("总音量"));
            Assert.That(
                prefab.transform.Find(
                        "AudioSettingsPanel/SettingsCard/MusicRow/Label")
                    .GetComponent<Text>().text,
                Is.EqualTo("音乐"));
            Assert.That(
                prefab.transform.Find(
                        "AudioSettingsPanel/SettingsCard/SFXRow/Label")
                    .GetComponent<Text>().text,
                Is.EqualTo("音效"));
            Assert.That(
                prefab.transform.Find(
                        "AudioSettingsPanel/SettingsCard/UIRow/Label")
                    .GetComponent<Text>().text,
                Is.EqualTo("界面"));
        }

        [Test]
        public void MainMenuScene_HasDedicatedClearCameraAndOverlayCanvas()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var cameras = roots.SelectMany(value =>
                    value.GetComponentsInChildren<Camera>(true)).ToArray();
                var canvases = roots.SelectMany(value =>
                    value.GetComponentsInChildren<Canvas>(true)).ToArray();

                Assert.That(cameras, Has.Length.EqualTo(1));
                var camera = cameras[0];
                Assert.That(camera.name, Is.EqualTo("MainMenuCamera"));
                Assert.That(camera.enabled, Is.True);
                Assert.That(camera.targetDisplay, Is.Zero);
                Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
                Assert.That(camera.backgroundColor.r, Is.EqualTo(0.035f).Within(0.0001f));
                Assert.That(camera.backgroundColor.g, Is.EqualTo(0.05f).Within(0.0001f));
                Assert.That(camera.backgroundColor.b, Is.EqualTo(0.08f).Within(0.0001f));
                Assert.That(camera.cullingMask, Is.Zero);
                Assert.That(camera.orthographic, Is.True);

                Assert.That(canvases, Has.Length.EqualTo(1));
                Assert.That(canvases[0].renderMode,
                    Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvases[0].worldCamera, Is.Null);
                Assert.That(
                    roots.SelectMany(value =>
                        value.GetComponentsInChildren<AudioListener>(true))
                        .Count(listener => listener.enabled),
                    Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Transform Find(GameObject root, string name)
        {
            foreach (var value in root.GetComponentsInChildren<Transform>(true))
            {
                if (value.name == name)
                {
                    return value;
                }
            }

            return null;
        }

        private static void AssertText(
            Transform transform,
            string expected,
            float expectedMinimumHeight)
        {
            Assert.That(transform, Is.Not.Null, expected);
            Assert.That(
                transform.GetComponent<Text>().text,
                Is.EqualTo(expected));
            var layout = transform.GetComponent<LayoutElement>();
            Assert.That(layout, Is.Not.Null, expected);
            Assert.That(
                layout.minHeight,
                Is.GreaterThanOrEqualTo(expectedMinimumHeight),
                expected);
        }
    }
}
