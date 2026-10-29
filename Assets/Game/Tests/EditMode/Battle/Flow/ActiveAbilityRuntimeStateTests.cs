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
                Is.EqualTo(BattlePhase.PlayerInput));
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

            BattleFlowTestSupport.CompleteTurnWithSingleMatch(coordinator);

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

                BattleFlowTestSupport.CompleteTurnWithSingleMatch(
                    coordinator);
            }

            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
        }

        [Test]
        public void UsedActiveRemainsAppliedWhilePlayerInputContinues()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 2 });
            coordinator.StartBattle();
            coordinator.TryUseActiveAbility(0);

            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(state.RemainingCooldown, Is.EqualTo(2));
            Assert.That(state.UsedThisTurn, Is.True);
        }

        [Test]
        public void PlayerInputAllowsMultipleDistinctActivesBeforeBoardAction()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 2, 3 });
            coordinator.StartBattle();

            Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(coordinator.TryUseActiveAbility(1), Is.True);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(coordinator.Context.ActiveAbilities[0].UsedThisTurn,
                Is.True);
            Assert.That(coordinator.Context.ActiveAbilities[1].UsedThisTurn,
                Is.True);
        }

        [Test]
        public void ReduceRemainingCooldown_ClampsAtZeroAndReportsAppliedAmount()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 4 });
            coordinator.StartBattle();
            coordinator.TryUseActiveAbility(0);
            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];

            Assert.That(state.ReduceRemainingCooldown(1), Is.EqualTo(1));
            Assert.That(state.RemainingCooldown, Is.EqualTo(3));
            Assert.That(state.ReduceRemainingCooldown(5), Is.EqualTo(3));
            Assert.That(state.RemainingCooldown, Is.Zero);
            Assert.That(state.ReduceRemainingCooldown(1), Is.Zero);
            Assert.That(state.RemainingCooldown, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ReduceRemainingCooldown_RejectsNonPositiveAmount(int amount)
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 4 });
            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];

            Assert.That(
                () => state.ReduceRemainingCooldown(amount),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void ReducedToZero_AfterUseRemainsUnavailableUntilNextTurn()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 4 });
            coordinator.StartBattle();
            Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];

            state.ReduceRemainingCooldown(4);

            Assert.That(state.RemainingCooldown, Is.Zero);
            Assert.That(state.UsedThisTurn, Is.True);
            Assert.That(state.CanUse, Is.False);
            Assert.That(coordinator.TryUseActiveAbility(0), Is.False);

            BattleFlowTestSupport.CompleteTurnWithSingleMatch(coordinator);

            Assert.That(state.RemainingCooldown, Is.Zero);
            Assert.That(state.UsedThisTurn, Is.False);
            Assert.That(coordinator.CanUseActiveAbility(0), Is.True);
        }

        [Test]
        public void ReducedDuringMatchResolution_DoesNotBypassPhaseRestriction()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 4 });
            coordinator.StartBattle();
            coordinator.TryUseActiveAbility(0);
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(
                        new[]
                        {
                            BattleFlowTestSupport.Match(
                                ValorChronicle.Data.Definitions.ElementType.Fire,
                                new ValorChronicle.Battle.Board.BoardPosition(0, 0),
                                new ValorChronicle.Battle.Board.BoardPosition(1, 0),
                                new ValorChronicle.Battle.Board.BoardPosition(2, 0))
                        }),
                    consumesTurn: true),
                Is.True);
            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];

            state.ReduceRemainingCooldown(4);

            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
            Assert.That(coordinator.CanUseActiveAbility(0), Is.False);
            Assert.That(coordinator.TryUseActiveAbility(0), Is.False);

            coordinator.ExecuteRemainingMatchEvents();
            coordinator.CompleteBossAction();

            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(coordinator.CanUseActiveAbility(0), Is.True);
        }
    }
}
