using System.Collections;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.UI.MainMenu;
using SpireChess.UI.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpireChess.Tests
{
    public sealed class SaveResumePlayModeTests
    {
        [UnityTest]
        public IEnumerator Boot_EntersFormalMainMenuWithoutImplicitRun()
        {
            yield return EnsureGameApp();
            GameApp.Instance.ClearInMemoryRunForAutomatedTests();

            SceneManager.LoadScene("Boot");
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(GameApp.Instance.Run, Is.Null);
            Assert.That(Object.FindObjectsOfType<MainMenuController>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<MainMenuScreenView>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<Canvas>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<EventSystem>(), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ExplicitNewRun_WritesInitialBoundaryAndRoutesToRunScreen()
        {
            yield return EnsureGameApp();
            GameApp.Instance.ClearInMemoryRunForAutomatedTests();
            SceneManager.LoadScene("MainMenu");
            yield return null;

            GameApp.Instance.StartNewRun(9001001);
            Assert.That(GameApp.Instance.Run, Is.Not.Null);
            Assert.That(GameApp.Instance.Persistence.CurrentRevision, Is.EqualTo(1));
            Assert.That(GameApp.Instance.Persistence.HasUnsavedChanges, Is.False);
            GameApp.Instance.Router.GoToCurrentRunPhase(GameApp.Instance.Run);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("RunTest"));
            Assert.That(Object.FindObjectsOfType<RunTestController>(), Has.Length.EqualTo(1));
        }

        private static IEnumerator EnsureGameApp()
        {
            if (GameApp.Instance == null)
            {
                yield return null;
            }

            Assert.That(GameApp.Instance, Is.Not.Null);
            Assert.That(GameApp.Instance.Configs, Is.Not.Null);
        }
    }
}
