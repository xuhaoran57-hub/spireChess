using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Diagnostics;
using SpireChess.Run;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class G4VisualSliceAcceptanceTests
    {
        [Test]
        public void VisualSliceRuntimeFlag_IsStable()
        {
            Assert.That(
                G4RuntimeArguments.VisualSliceFlag,
                Is.EqualTo("-g4VisualSlice"));
        }

        [Test]
        public void SeedTenEventFixture_SelectsTranquilGroveArtwork()
        {
            var configs = new ConfigService(
                new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();
            var run = new RunSession(configs, 10);
            var statuses = run.State.CurrentMap.Nodes.ToDictionary(
                node => node.Id,
                _ => RunNodeStatus.Locked);
            statuses["f1_event"] = RunNodeStatus.Reachable;

            var restoreStatuses = typeof(MapProgressState).GetMethod(
                "RestoreStatuses",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(restoreStatuses, Is.Not.Null);
            restoreStatuses.Invoke(
                run.State.MapProgress,
                new object[]
                {
                    (IReadOnlyDictionary<string, RunNodeStatus>)statuses
                });

            var result = run.EnterNode("f1_event");

            Assert.That(result.Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.EventChoice));
            Assert.That(run.State.PendingEventChoice, Is.Not.Null);
            Assert.That(
                run.State.PendingEventChoice.Config.Id,
                Is.EqualTo("tranquil_grove"));
            Assert.That(
                run.State.PendingEventChoice.Config.ArtId,
                Is.EqualTo("event_tranquil_grove"));
        }
    }
}
