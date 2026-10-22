using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Board;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiCombatProviderRegistrarTests
    {
        private ShimotsukiMiyukiCombatConfig config;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            config = ShimotsukiMiyukiTestConfig.Create();
            definition = ShimotsukiMiyukiTestConfig.Definition(config);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(0, 0.08d)]
        [TestCase(2, 0.08d)]
        [TestCase(3, 0.10d)]
        [TestCase(6, 0.10d)]
        public void RegistrationDeclaresIndefiniteWaterPartyPassive(
            int awakening,
            double expectedRate)
        {
            Registration registration = Register(awakening);
            InitialPartyEffectDefinition passive = registration.Result
                .InitialPartyEffects.Single();

            Assert.That(passive.EffectId,
                Is.EqualTo(ShimotsukiMiyukiRules.PassiveEffectId));
            Assert.That(passive.SourceId,
                Is.EqualTo(ShimotsukiMiyukiRules.CharacterId));
            Assert.That(passive.Category, Is.EqualTo(EffectCategory.Buff));
            Assert.That(passive.ModifierType,
                Is.EqualTo(EffectModifierType.ElementDamageIncrease));
            Assert.That(passive.Magnitude, Is.EqualTo(expectedRate));
            Assert.That(passive.RemainingTurns, Is.Null);
            Assert.That(passive.StackPolicy, Is.EqualTo(EffectStackPolicy.Unique));
            Assert.That(passive.ElementFilter, Is.EqualTo(ElementType.Water));
        }

        [Test]
        public void RegistrationProvidesMatchBoardActiveStateAndWater()
        {
            Registration registration = Register(awakening: 0);

            Assert.That(registration.Result.HasActiveAbility, Is.True);
            Assert.That(registration.Result.ActiveAbilityId,
                Is.EqualTo(ShimotsukiMiyukiRules.ActiveAbilityId));
            Assert.That(registration.Result.ActiveCooldownTurns,
                Is.EqualTo(8));
            Assert.That(registration.ActiveProviders.Count, Is.EqualTo(1));
            Assert.That(registration.ActiveProviders.TryResolveBoard(
                ShimotsukiMiyukiRules.CharacterId,
                ShimotsukiMiyukiRules.ActiveAbilityId,
                out IActiveAbilityBoardPlanProvider active), Is.True);
            Assert.That(active,
                Is.TypeOf<ShimotsukiMiyukiActiveBoardPlanProvider>());
            Assert.That(registration.MatchProviders.TryResolve(
                ShimotsukiMiyukiRules.CharacterId,
                out IMatchEventActionProvider provider), Is.True);
            Assert.That(provider,
                Is.TypeOf<ShimotsukiMiyukiMatchActionProvider>());
            CombatResourceRequirement resource = registration.Result
                .ResourceRequirements.Single();
            Assert.That(resource.ResourceId,
                Is.EqualTo(WaterElementResource.Id));
            Assert.That(resource.MaxAmount,
                Is.EqualTo(WaterElementResource.MaxAmount));
            Assert.That(registration.Result.TriggerRules.Single(),
                Is.TypeOf<ShimotsukiMiyukiFrostEchoConsumptionRule>());
            ICharacterCombatStatusSource status = registration.Result
                .CombatStatusSources.Single();
            Assert.That(status,
                Is.TypeOf<ShimotsukiMiyukiBattleState>());
            Assert.That(status.IsVisible, Is.False);
        }

        [Test]
        public void RegistrationDoesNotConsumeActionId()
        {
            var ids = new CombatActionIdSequence();

            Register(awakening: 0, ids);

            Assert.That(ids.LastIssuedId, Is.Zero);
        }

        [Test]
        public void MatchProviderAndStatusSourceShareBattleState()
        {
            Registration registration = Register(awakening: 6);
            var state = (ShimotsukiMiyukiBattleState)registration.Result
                .CombatStatusSources.Single();
            registration.MatchProviders.TryResolve(
                ShimotsukiMiyukiRules.CharacterId,
                out IMatchEventActionProvider resolved);
            var match = (ShimotsukiMiyukiMatchActionProvider)resolved;
            var party = new PartyBattleState(new[] { registration.Character });
            BossBattleState boss = Boss();
            WaterElementResource.Register(
                boss.Resources,
                WaterElementResource.MaxAmount);

            Assert.That(state.TryGrantFrostEcho(), Is.True);
            var actions = match.CreateActions(
                registration.Character,
                party,
                boss,
                BoardMatchTier.Three,
                1,
                new CombatActionIdSequence());

            Assert.That(((AddResourceAction)actions[1]).Amount,
                Is.EqualTo(2));
        }

        [Test]
        public void UniquePassiveCannotDuplicateInOnePartyCollection()
        {
            Registration registration = Register(awakening: 0);
            InitialPartyEffectDefinition definition = registration.Result
                .InitialPartyEffects.Single();
            var party = new PartyBattleState(new[] { registration.Character });

            EffectInstance first = party.Effects.ApplyEffect(
                CreateEffect(definition, 100));
            EffectInstance second = party.Effects.ApplyEffect(
                CreateEffect(definition, 101));

            Assert.That(party.Effects.Count, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
            party.Effects.ProcessTurnEnd();
            Assert.That(first.RemainingTurns, Is.Null);
        }

        [Test]
        public void PassiveFiltersWaterAndSumsWithOtherWaterEffect()
        {
            Registration registration = Register(awakening: 0);
            var party = new PartyBattleState(new[] { registration.Character });
            BossBattleState boss = Boss();
            InitialPartyEffectDefinition passive = registration.Result
                .InitialPartyEffects.Single();
            party.Effects.ApplyEffect(CreateEffect(passive, 100));
            party.Effects.ApplyEffect(new EffectInstance(
                101,
                "effect_test_other_water_increase",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                0.25d,
                remainingTurns: 3,
                creationOrder: 101,
                EffectStackPolicy.RefreshDuration,
                elementFilter: ElementType.Water));

            Assert.That(CombatModifierCollector.CollectAllyDamage(
                    registration.Character,
                    party,
                    boss,
                    ElementType.Water).ElementDamageIncreaseRateSum,
                Is.EqualTo(0.33d).Within(0.000000001d));
            Assert.That(CombatModifierCollector.CollectAllyDamage(
                    registration.Character,
                    party,
                    boss,
                    ElementType.Fire).ElementDamageIncreaseRateSum,
                Is.Zero);
        }

        [Test]
        public void SeparateBattlesReceiveSeparatePassiveInstances()
        {
            Registration firstRegistration = Register(awakening: 0);
            Registration secondRegistration = Register(awakening: 0);
            var firstParty = new PartyBattleState(
                new[] { firstRegistration.Character });
            var secondParty = new PartyBattleState(
                new[] { secondRegistration.Character });

            EffectInstance first = firstParty.Effects.ApplyEffect(
                CreateEffect(
                    firstRegistration.Result.InitialPartyEffects.Single(),
                    100));
            EffectInstance second = secondParty.Effects.ApplyEffect(
                CreateEffect(
                    secondRegistration.Result.InitialPartyEffects.Single(),
                    100));

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(firstParty.Effects.Count, Is.EqualTo(1));
            Assert.That(secondParty.Effects.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvalidConfigIsRejectedBeforeProviderRegistration()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
            config = ShimotsukiMiyukiTestConfig.Create(activeCooldown: -1);
            definition = ShimotsukiMiyukiTestConfig.Definition(config);
            var registrar = new ShimotsukiMiyukiCombatProviderRegistrar();
            var matches = new MatchEventActionProviderRegistry();

            Assert.Throws<InvalidOperationException>(() => registrar.Register(
                Member(definition, 0),
                Character(),
                Boss(),
                new CombatActionIdSequence(),
                matches,
                new ActiveAbilityActionProviderRegistry()));
            Assert.That(matches.Count, Is.Zero);
        }

        private Registration Register(
            int awakening,
            CombatActionIdSequence ids = null)
        {
            var registrar = new ShimotsukiMiyukiCombatProviderRegistrar();
            var character = Character();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrationResult result =
                registrar.Register(
                    Member(definition, awakening),
                    character,
                    Boss(),
                    ids ?? new CombatActionIdSequence(),
                    matches,
                    actives);
            return new Registration(
                character,
                result,
                matches,
                actives);
        }

        private static EffectInstance CreateEffect(
            InitialPartyEffectDefinition source,
            long id)
        {
            return new EffectInstance(
                id,
                source.EffectId,
                source.SourceId,
                source.Category,
                source.ModifierType,
                source.Magnitude,
                source.RemainingTurns,
                id,
                source.StackPolicy,
                source.StackCount,
                source.MaxStackCount,
                source.ElementFilter,
                source.AttackTypeMask,
                source.RequiredAttackTags);
        }

        private static BattlePartyMemberInput Member(
            CharacterDefinition value,
            int awakening)
        {
            return new BattlePartyMemberInput(
                ShimotsukiMiyukiRules.CharacterId,
                0,
                level: 100,
                awakening,
                value);
        }

        private static CharacterBattleState Character()
        {
            return new CharacterBattleState(
                ShimotsukiMiyukiRules.CharacterId,
                0,
                ElementType.Water,
                4515,
                683d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "miyuki_registrar_test_boss",
                ElementType.Fire,
                1000000,
                100d);
        }

        private sealed class Registration
        {
            public Registration(
                CharacterBattleState character,
                CharacterCombatProviderRegistrationResult result,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                Character = character;
                Result = result;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
            }

            public CharacterBattleState Character { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }
        }
    }
}
