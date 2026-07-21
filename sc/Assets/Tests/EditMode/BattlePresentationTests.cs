using System.Collections.Generic;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.UI.Battle;

namespace SpireChess.Tests
{
    public sealed class BattlePresentationTests
    {
        [Test]
        public void Factory_MapsRuntimeStateWithoutCardSpecificData()
        {
            var config = new MinionConfig
            {
                Id = "presentation",
                Name = "表现测试",
                Race = "Starbound",
                Tier = 4,
                Attack = 3,
                Health = 5,
                GoldenAttack = 6,
                GoldenHealth = 10,
                Description = "普通描述",
                GoldenDescription = "金色描述",
                Keywords = new List<string> { "Taunt", "Shield" },
                GoldenEffects = new List<EffectConfig>
                {
                    new EffectConfig
                    {
                        Id = "presentation-only",
                        Trigger = "OnDeath",
                        Action = "ModifyStats"
                    }
                }
            };
            var runtime = new BattleMinionRuntime(
                config,
                true,
                initialAttack: 9,
                initialHealth: 12,
                runtimeInstanceId: "runtime-1");

            var model = BattleCardViewModelFactory.FromRuntime(
                runtime,
                BattleSide.Player,
                0);

            Assert.That(model.InstanceId, Is.EqualTo("runtime-1"));
            Assert.That(model.Name, Is.EqualTo("表现测试"));
            Assert.That(model.Description, Is.EqualTo("金色描述"));
            Assert.That(model.Attack, Is.EqualTo(9));
            Assert.That(model.Health, Is.EqualTo(12));
            Assert.That(model.BaseAttack, Is.EqualTo(6));
            Assert.That(model.BaseHealth, Is.EqualTo(10));
            Assert.That(model.IsGolden, Is.True);
            Assert.That(model.HasShield, Is.True);
            Assert.That(model.Keywords, Is.EquivalentTo(new[] { "嘲讽", "护盾" }));
        }

        [Test]
        public void StateBuilder_MapsFiveSlotsAndFormalControls()
        {
            var board = new BattleBoardState();
            board.Player[1] = CreateRuntime("player", 2, 3);
            board.Enemy[4] = CreateRuntime("enemy", 4, 5);

            var state = BattleScreenStateBuilder.Build(
                board,
                "测试战斗",
                "战斗播放中",
                new[] { "第 2 轮。" },
                true,
                true,
                false,
                2f);

            Assert.That(state.PlayerCards, Has.Length.EqualTo(5));
            Assert.That(state.EnemyCards, Has.Length.EqualTo(5));
            Assert.That(state.PlayerCards[1].Name, Is.EqualTo("player"));
            Assert.That(state.EnemyCards[4].Name, Is.EqualTo("enemy"));
            Assert.That(state.RoundText, Is.EqualTo("第 2 轮。"));
            Assert.That(state.Start.IsInteractable, Is.False);
            Assert.That(state.Skip.IsVisible, Is.True);
            Assert.That(state.Speed.Label, Is.EqualTo("速度 2×"));
            Assert.That(state.Preset.IsVisible, Is.False);
            Assert.That(state.Return.IsVisible, Is.False);
        }

        private static BattleMinionRuntime CreateRuntime(
            string id,
            int attack,
            int health)
        {
            return new BattleMinionRuntime(new MinionConfig
            {
                Id = id,
                Name = id,
                Race = "Wayfarer",
                Tier = 1,
                Attack = attack,
                Health = health,
                GoldenAttack = attack * 2,
                GoldenHealth = health * 2
            });
        }
    }
}
