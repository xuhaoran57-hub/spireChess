using NUnit.Framework;
using SpireChess.UI.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class MainMenuUiPrefabTests
    {
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
