using NUnit.Framework;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Flow
{
    public sealed class ActiveAbilityRuntimeStateTests
    {
        [Test]
        public void Use_SetsCooldownAndPreventsSameTurnReuse()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 8 });
            coordinator.StartBattle();
            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];

            Assert.That(state.RemainingCooldown, Is.Zero);
            Assert.That(state.CanUse, Is.True);
            Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
            Assert.That(state.RemainingCooldown, Is.EqualTo(8));
            Assert.That(state.UsedThisTurn, Is.True);
            Assert.That(coordinator.TryUseActiveAbility(0), Is.False);
        }

        [Test]
        public void DifferentAbilities_CanBeUsedInSameTurn()
        {
            var coordinator = new BattleFlowCoordinator(
                25,
                new[] { 3, 5 });
            coordinator.StartBattle();

            Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
            Assert.That(coordinator.TryUseActiveAbility(1), Is.True);
        }

        [Test]
        public void NextTurn_DecrementsWithoutGoingBelowZeroAndResetsUsage()
        {
            var coordinator = new BattleFlowCoordinator(3, new[] { 1, 0 });
            coordinator.StartBattle();
            ActiveAbilityRuntimeState first =
                coordinator.Context.ActiveAbilities[0];
            ActiveAbilityRuntimeState second =
                coordinator.Context.ActiveAbilities[1];
            Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
            Assert.That(coordinator.TryUseActiveAbility(1), Is.True);

            BattleFlowTestSupport.CompleteTurnWithoutMatchEvents(coordinator);

            Assert.That(first.RemainingCooldown, Is.Zero);
            Assert.That(first.UsedThisTurn, Is.False);
            Assert.That(first.CanUse, Is.True);
            Assert.That(second.RemainingCooldown, Is.Zero);
            Assert.That(second.UsedThisTurn, Is.False);
        }

        [Test]
        public void CooldownEight_IsUsableOnTurnsOneNineSeventeenAndTwentyFive()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 8 });
            coordinator.StartBattle();

            for (int turn = 1; turn <= 25; turn++)
            {
                bool expectedUsable = turn == 1
                    || turn == 9
                    || turn == 17
                    || turn == 25;
                Assert.That(
                    coordinator.Context.ActiveAbilities[0].CanUse,
                    Is.EqualTo(expectedUsable),
                    $"Turn {turn}");
                if (expectedUsable)
                {
                    Assert.That(
                        coordinator.TryUseActiveAbility(0),
                        Is.True,
                        $"Turn {turn}");
                }

                BattleFlowTestSupport.CompleteTurnWithoutMatchEvents(
                    coordinator);
            }

            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
        }

        [Test]
        public void NonConsumingBoardAction_DoesNotRestoreUsedActive()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 2 });
            coordinator.StartBattle();
            coordinator.TryUseActiveAbility(0);
            coordinator.CompleteActiveInput();
            coordinator.NotifyBoardActionStarted();

            Assert.That(
                coordinator.NotifyBoardActionResolved(null, false),
                Is.True);

            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PuzzleInput));
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(state.RemainingCooldown, Is.EqualTo(2));
            Assert.That(state.UsedThisTurn, Is.True);
        }
    }
}
