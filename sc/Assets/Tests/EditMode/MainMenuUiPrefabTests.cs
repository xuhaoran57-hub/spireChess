using System.Linq;
using NUnit.Framework;
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
        }

        [Test]
        public void ConfirmationDialog_IsAlsoPublishedAsReusablePrefab()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/MainMenu/PF_ConfirmDialog.prefab"),
                Is.Not.Null);
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
    }
}
