using System;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuBattleStateTests
    {
        [Test]
        public void NewStateStartsEmptyAndStatesAreIndependent()
        {
            var first = new SakuragiKoharuBattleState(Character(slot: 0));
            var second = new SakuragiKoharuBattleState(Character(slot: 4));

            first.GainSpiritEnergy(7);

            Assert.That(first.SpiritEnergy, Is.EqualTo(7));
            Assert.That(second.SpiritEnergy, Is.Zero);
            Assert.That(first.Owner.PartySlotIndex, Is.EqualTo(0));
            Assert.That(second.Owner.PartySlotIndex, Is.EqualTo(4));
        }

        [Test]
        public void StatusSourceUsesOwnerAndShowsZeroWithoutCaching()
        {
            var state = new SakuragiKoharuBattleState(Character(slot: 3));

            Assert.That(state.StatusId,
                Is.EqualTo(SakuragiKoharuRules.SpiritEnergyStatusId));
            Assert.That(state.CharacterId,
                Is.EqualTo(SakuragiKoharuRules.CharacterId));
            Assert.That(state.PartySlotIndex, Is.EqualTo(3));
            Assert.That(state.CurrentCount, Is.Zero);
            Assert.That(state.IsVisible, Is.True);

            state.GainSpiritEnergy(7);
            Assert.That(state.CurrentCount, Is.EqualTo(7));
            state.TryConsumeSpiritEnergy(5);
            Assert.That(state.CurrentCount, Is.EqualTo(2));
        }

        [Test]
        public void StatusDisplayClampsLongValueWithoutChangingBattleValue()
        {
            var state = new SakuragiKoharuBattleState(Character());
            state.GainSpiritEnergy((long)int.MaxValue + 123L);

            Assert.That(state.CurrentCount, Is.EqualTo(int.MaxValue));
            Assert.That(state.SpiritEnergy,
                Is.EqualTo((long)int.MaxValue + 123L));
        }

        [Test]
        public void GainHasNoGameplayCapAndRejectsTechnicalOverflow()
        {
            var state = new SakuragiKoharuBattleState(Character());

            Assert.That(state.GainSpiritEnergy(long.MaxValue),
                Is.EqualTo(long.MaxValue));
            Assert.Throws<OverflowException>(() =>
                state.GainSpiritEnergy(1));
            Assert.That(state.SpiritEnergy, Is.EqualTo(long.MaxValue));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GainRejectsNonPositiveAmount(long amount)
        {
            var state = new SakuragiKoharuBattleState(Character());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.GainSpiritEnergy(amount));
            Assert.That(state.SpiritEnergy, Is.Zero);
        }

        [Test]
        public void ConsumeRequiresEnoughEnergyAndNeverBecomesNegative()
        {
            var state = new SakuragiKoharuBattleState(Character());
            state.GainSpiritEnergy(5);

            Assert.That(state.TryConsumeSpiritEnergy(6), Is.False);
            Assert.That(state.SpiritEnergy, Is.EqualTo(5));
            Assert.That(state.TryConsumeSpiritEnergy(5), Is.True);
            Assert.That(state.SpiritEnergy, Is.Zero);
            Assert.That(state.TryConsumeSpiritEnergy(1), Is.False);
            Assert.That(state.SpiritEnergy, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ConsumeRejectsNonPositiveAmount(int amount)
        {
            var state = new SakuragiKoharuBattleState(Character());
            state.GainSpiritEnergy(5);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.TryConsumeSpiritEnergy(amount));
            Assert.That(state.SpiritEnergy, Is.EqualTo(5));
        }

        [TestCase(12, 4, 6, 2, 0, 2)]
        [TestCase(12, 1, 6, 0, 6, 1)]
        [TestCase(5, 4, 6, 4, 5, 0)]
        [TestCase(12, 4, 5, 2, 2, 2)]
        [TestCase(12, 1, 5, 0, 7, 1)]
        [TestCase(4, 4, 5, 4, 4, 0)]
        public void ReductionConsumesOnlyForAppliedCooldownReduction(
            long initialEnergy,
            int initialCooldown,
            int spiritCost,
            int expectedCooldown,
            long expectedEnergy,
            int expectedReductionCount)
        {
            RuntimeFixture fixture = Runtime(initialCooldown, useActive: true);
            fixture.BattleState.GainSpiritEnergy(initialEnergy);

            int reductionCount =
                SakuragiKoharuSpiritCooldownReductionRule
                    .ApplyAvailableReductions(
                        fixture.BattleState,
                        fixture.Reducer,
                        spiritCost);

            Assert.That(reductionCount,
                Is.EqualTo(expectedReductionCount));
            Assert.That(fixture.RuntimeState.RemainingCooldown,
                Is.EqualTo(expectedCooldown));
            Assert.That(fixture.BattleState.SpiritEnergy,
                Is.EqualTo(expectedEnergy));
        }

        [Test]
        public void ReadyActiveConsumesNoSpiritEnergy()
        {
            RuntimeFixture fixture = Runtime(
                cooldownTurns: 4,
                useActive: false);
            fixture.BattleState.GainSpiritEnergy(12);

            int reductionCount =
                SakuragiKoharuSpiritCooldownReductionRule
                    .ApplyAvailableReductions(
                        fixture.BattleState,
                        fixture.Reducer,
                        spiritEnergyCost: 6);

            Assert.That(reductionCount, Is.Zero);
            Assert.That(fixture.RuntimeState.RemainingCooldown, Is.Zero);
            Assert.That(fixture.BattleState.SpiritEnergy, Is.EqualTo(12));
        }

        [Test]
        public void ActiveUseReductionPreservesUsedThisTurn()
        {
            RuntimeFixture fixture = Runtime(
                cooldownTurns: 4,
                useActive: true);
            fixture.BattleState.GainSpiritEnergy(12);

            SakuragiKoharuSpiritCooldownReductionRule
                .ApplyAvailableReductions(
                    fixture.BattleState,
                    fixture.Reducer,
                    spiritEnergyCost: 5);

            Assert.That(fixture.RuntimeState.RemainingCooldown,
                Is.EqualTo(2));
            Assert.That(fixture.RuntimeState.UsedThisTurn, Is.True);
            Assert.That(fixture.RuntimeState.CanUse, Is.False);
            Assert.That(fixture.Coordinator.TryUseActiveAbility(0), Is.False);
        }

        [Test]
        public void FailedReductionPreservesSpiritEnergy()
        {
            var state = new SakuragiKoharuBattleState(Character());
            state.GainSpiritEnergy(12);

            int reductionCount =
                SakuragiKoharuSpiritCooldownReductionRule
                    .ApplyAvailableReductions(
                        state,
                        new FixedResultReducer(0),
                        spiritEnergyCost: 6);

            Assert.That(reductionCount, Is.Zero);
            Assert.That(state.SpiritEnergy, Is.EqualTo(12));
        }

        [Test]
        public void UnboundHandleThrowsBeforeSpiritConsumption()
        {
            var state = new SakuragiKoharuBattleState(Character());
            state.GainSpiritEnergy(6);
            var handle = new ActiveAbilityCooldownReductionHandle(
                SakuragiKoharuRules.CharacterId,
                state.Owner.PartySlotIndex);

            Assert.Throws<InvalidOperationException>(() =>
                SakuragiKoharuSpiritCooldownReductionRule
                    .ApplyAvailableReductions(
                        state,
                        handle,
                        spiritEnergyCost: 6));
            Assert.That(state.SpiritEnergy, Is.EqualTo(6));
        }

        [Test]
        public void SeparateBattleReducersChangeOnlyTheirOwnRuntime()
        {
            RuntimeFixture first = Runtime(4, useActive: true, slot: 0);
            RuntimeFixture second = Runtime(4, useActive: true, slot: 4);
            first.BattleState.GainSpiritEnergy(6);
            second.BattleState.GainSpiritEnergy(6);

            SakuragiKoharuSpiritCooldownReductionRule
                .ApplyAvailableReductions(
                    first.BattleState,
                    first.Reducer,
                    spiritEnergyCost: 6);

            Assert.That(first.RuntimeState.RemainingCooldown, Is.EqualTo(3));
            Assert.That(first.BattleState.SpiritEnergy, Is.Zero);
            Assert.That(second.RuntimeState.RemainingCooldown, Is.EqualTo(4));
            Assert.That(second.BattleState.SpiritEnergy, Is.EqualTo(6));
        }

        [Test]
        public void StateRejectsAnotherCharacter()
        {
            var other = new CharacterBattleState(
                "other_character",
                0,
                ElementType.Grass,
                1000,
                100d);

            Assert.Throws<ArgumentException>(() =>
                new SakuragiKoharuBattleState(other));
        }

        private static RuntimeFixture Runtime(
            int cooldownTurns,
            bool useActive,
            int slot = 2)
        {
            var coordinator = new BattleFlowCoordinator(
                25,
                new[] { cooldownTurns });
            coordinator.StartBattle();
            if (useActive)
            {
                Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
            }

            var battleState = new SakuragiKoharuBattleState(
                Character(slot));
            ActiveAbilityRuntimeState runtimeState =
                coordinator.Context.ActiveAbilities[0];
            var reducer = new ActiveAbilityCooldownReductionHandle(
                SakuragiKoharuRules.CharacterId,
                slot);
            MethodInfo bindMethod = typeof(
                    ActiveAbilityCooldownReductionHandle)
                .GetMethod(
                    "Bind",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);
            bindMethod.Invoke(reducer, new object[] { runtimeState });
            return new RuntimeFixture(
                coordinator,
                battleState,
                runtimeState,
                reducer);
        }

        private static CharacterBattleState Character(int slot = 2)
        {
            return new CharacterBattleState(
                SakuragiKoharuRules.CharacterId,
                slot,
                ElementType.Grass,
                1100,
                120d);
        }

        private sealed class FixedResultReducer
            : IActiveAbilityCooldownReducer
        {
            private readonly int result;

            public FixedResultReducer(int result)
            {
                this.result = result;
            }

            public int ReduceRemainingCooldown(int amount)
            {
                return result;
            }
        }

        private sealed class RuntimeFixture
        {
            public RuntimeFixture(
                BattleFlowCoordinator coordinator,
                SakuragiKoharuBattleState battleState,
                ActiveAbilityRuntimeState runtimeState,
                IActiveAbilityCooldownReducer reducer)
            {
                Coordinator = coordinator;
                BattleState = battleState;
                RuntimeState = runtimeState;
                Reducer = reducer;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public SakuragiKoharuBattleState BattleState { get; }
            public ActiveAbilityRuntimeState RuntimeState { get; }
            public IActiveAbilityCooldownReducer Reducer { get; }
        }
    }
}
