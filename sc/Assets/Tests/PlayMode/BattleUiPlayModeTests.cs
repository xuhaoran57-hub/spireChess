using System.Collections;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.Audio;
using SpireChess.Battle;
using SpireChess.Run;
using SpireChess.UI.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpireChess.Tests
{
    public sealed class BattleUiPlayModeTests
    {
        [UnityTest]
        public IEnumerator FormalBattleScene_RoutesDragAndSpeedButtonInputs()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(701);
            SceneManager.LoadScene("BattleTest");
            yield return null;

            var controller = Object.FindObjectOfType<BattleTestController>();
            var screen = Object.FindObjectOfType<BattleScreenView>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(controller.UsesFormalView, Is.True);
            Assert.That(Object.FindObjectsOfType<BattleTestController>(),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<BattleScreenView>(),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<Canvas>(),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<EventSystem>(),
                Has.Length.EqualTo(1));

            var firstId = controller.SetupState.Player[0].RuntimeInstanceId;
            var secondId = controller.SetupState.Player[1].RuntimeInstanceId;
            var firstContent = screen.transform.Find(
                "SafeArea/Board/PlayerRow/Slots/Slot1/Content");
            var secondSlot = screen.transform.Find(
                    "SafeArea/Board/PlayerRow/Slots/Slot2")
                .GetComponent<BattleSlotView>();
            var standee = firstContent.GetComponentInChildren<BattleStandeeView>();
            Assert.That(standee, Is.Not.Null);
            var drag = new PointerEventData(EventSystem.current)
            {
                pointerDrag = standee.gameObject
            };

            standee.OnBeginDrag(drag);
            Assert.That(standee.transform.parent, Is.SameAs(screen.transform));
            secondSlot.OnDrop(drag);
            standee.OnEndDrag(drag);
            yield return null;

            Assert.That(controller.SetupState.Player[0].RuntimeInstanceId,
                Is.EqualTo(secondId));
            Assert.That(controller.SetupState.Player[1].RuntimeInstanceId,
                Is.EqualTo(firstId));

            var speed = screen.transform.Find(
                    "SafeArea/TopBar/Actions/Speed")
                .GetComponent<Button>();
            speed.onClick.Invoke();
            Assert.That(controller.PlaybackSpeed, Is.EqualTo(2f));
            Assert.That(
                BattleScreenView.GetDurationScale(controller.PlaybackSpeed),
                Is.EqualTo(0.5f).Within(0.001f));
            speed.onClick.Invoke();
            Assert.That(controller.PlaybackSpeed, Is.EqualTo(1f));
            Assert.That(
                BattleScreenView.GetDurationScale(controller.PlaybackSpeed),
                Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SkipPlayback_SettlesRunBattleExactlyOnce()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(702);
            var run = GameApp.Instance.Run;
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            var settledBefore = run.State.Statistics.BattlesWon +
                                run.State.Statistics.BattlesNotWon;

            SceneManager.LoadScene("BattleTest");
            yield return null;

            var controller = Object.FindObjectOfType<BattleTestController>();
            var screen = Object.FindObjectOfType<BattleScreenView>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(controller.IsRunBattle, Is.True);
            Assert.That(controller.UsesFormalView, Is.True);

            ClickThroughEventSystem(screen.transform.Find(
                    "SafeArea/TopBar/Actions/Start")
                .GetComponent<Button>());
            yield return null;
            Assert.That(controller.IsBattleLocked, Is.True);
            screen.transform.Find("SafeArea/TopBar/Actions/Skip")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(screen.IsAnimationPlaying, Is.False);
            Assert.That(screen.ActiveFeedbackFxCount, Is.Zero);
            Assert.That(screen.ActiveImpactFxCount, Is.Zero);

            for (var step = 0; step < 10 && controller.LastResult == null; step++)
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Assert.That(controller.LastResult, Is.Not.Null);
            Assert.That(run.LastBattleResult, Is.SameAs(controller.LastResult));
            Assert.That(screen.IsResultVisible, Is.True);
            Assert.That(screen.ResultTitle, Is.Not.Empty);
            AssertStandeesAtRest();
            var settledAfter = run.State.Statistics.BattlesWon +
                               run.State.Statistics.BattlesNotWon;
            Assert.That(settledAfter, Is.EqualTo(settledBefore + 1));

            controller.SkipPlayback();
            controller.StartBattle();
            yield return null;
            yield return null;
            Assert.That(run.State.Statistics.BattlesWon +
                        run.State.Statistics.BattlesNotWon,
                Is.EqualTo(settledAfter));
            Assert.That(run.LastBattleResult, Is.SameAs(controller.LastResult));
        }

        [UnityTest]
        public IEnumerator CancelMidDeath_RestoresVisualStateAndClearsFx()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(703);
            SceneManager.LoadScene("BattleTest");
            yield return null;

            var controller = Object.FindObjectOfType<BattleTestController>();
            var screen = Object.FindObjectOfType<BattleScreenView>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);

            var standee = screen.transform.Find(
                    "SafeArea/Board/PlayerRow/Slots/Slot1/Content")
                .GetComponentInChildren<BattleStandeeView>();
            Assert.That(standee, Is.Not.Null);
            var playbackEvent = new BattlePlaybackEvent(
                BattlePlaybackEventKind.UnitDied,
                controller.SetupState,
                "中断阵亡演出",
                targetSide: BattleSide.Player,
                targetIndex: 0,
                targetInstanceId: standee.InstanceId);

            screen.StartCoroutine(screen.PlayEvent(playbackEvent, 1f));
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(screen.IsAnimationPlaying, Is.True);

            screen.SnapAndClear();
            yield return null;

            Assert.That(screen.IsAnimationPlaying, Is.False);
            Assert.That(screen.ActiveFeedbackFxCount, Is.Zero);
            Assert.That(screen.ActiveImpactFxCount, Is.Zero);
            Assert.That(standee.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                Vector3.Distance(standee.transform.localScale, Vector3.one),
                Is.LessThan(0.001f));
            Assert.That(
                Vector2.Distance(
                    standee.RectTransform.anchoredPosition,
                    Vector2.zero),
                Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator CombatEnded_ShowsPersistentNonBlockingResultLayer()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(704);
            SceneManager.LoadScene("BattleTest");
            yield return null;

            var controller = Object.FindObjectOfType<BattleTestController>();
            var screen = Object.FindObjectOfType<BattleScreenView>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);
            var playbackEvent = new BattlePlaybackEvent(
                BattlePlaybackEventKind.CombatEnded,
                controller.SetupState,
                "测试结算");

            yield return screen.PlayEvent(
                playbackEvent,
                2f,
                BattleSide.Player);

            Assert.That(screen.IsAnimationPlaying, Is.False);
            Assert.That(screen.IsResultVisible, Is.True);
            Assert.That(screen.ResultTitle, Is.EqualTo("战斗胜利"));
            Assert.That(
                screen.LastAudioCueId,
                Is.EqualTo(PresentationAudioCueIds.BattleVictory));
            var resultGroup = screen.transform.Find("SafeArea/ResultLayer")
                .GetComponent<CanvasGroup>();
            Assert.That(resultGroup.blocksRaycasts, Is.False);
            Assert.That(resultGroup.interactable, Is.False);
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

        private static void ClickThroughEventSystem(Button expectedButton)
        {
            Assert.That(expectedButton, Is.Not.Null);
            Assert.That(expectedButton.interactable, Is.True);
            Canvas.ForceUpdateCanvases();
            var screen = expectedButton.GetComponentInParent<BattleScreenView>();
            var logPanel = screen.transform.Find("SafeArea/LogPanel")
                .GetComponent<RectTransform>();
            var topBar = screen.transform.Find("SafeArea/TopBar")
                .GetComponent<RectTransform>();
            var logImage = logPanel.GetComponent<Image>();
            Assert.That(logImage.raycastTarget, Is.False);
            var logCorners = new Vector3[4];
            var topCorners = new Vector3[4];
            logPanel.GetWorldCorners(logCorners);
            topBar.GetWorldCorners(topCorners);
            Assert.That(logCorners[2].y, Is.LessThan(topCorners[0].y));
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(
                    null,
                    expectedButton.transform.position)
            };
            ExecuteEvents.ExecuteHierarchy(
                expectedButton.gameObject,
                pointer,
                ExecuteEvents.pointerClickHandler);
        }

        private static void AssertStandeesAtRest()
        {
            foreach (var standee in Object.FindObjectsOfType<BattleStandeeView>())
            {
                Assert.That(standee.GetComponent<CanvasGroup>().alpha,
                    Is.EqualTo(1f).Within(0.001f));
                Assert.That(
                    Vector3.Distance(standee.transform.localScale, Vector3.one),
                    Is.LessThan(0.001f));
                Assert.That(
                    Vector2.Distance(
                        standee.RectTransform.anchoredPosition,
                        Vector2.zero),
                    Is.LessThan(0.001f));
            }
        }
    }
}
