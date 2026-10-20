using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class CharacterCombatProviderRegistrationResultTests
    {
        [Test]
        public void ExistingFactoriesPreserveMetadataAndReturnEmptyContributions()
        {
            CharacterCombatProviderRegistrationResult active =
                CharacterCombatProviderRegistrationResult.WithActiveAbility(
                    "active_id",
                    8);
            CharacterCombatProviderRegistrationResult passive =
                CharacterCombatProviderRegistrationResult
                    .WithoutActiveAbility();

            Assert.That(active.HasActiveAbility, Is.True);
            Assert.That(active.ActiveAbilityId, Is.EqualTo("active_id"));
            Assert.That(active.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(active.TriggerRules, Is.Empty);
            Assert.That(active.ResourceRequirements, Is.Empty);
            Assert.That(active.CombatStatusSources, Is.Empty);
            Assert.That(passive.HasActiveAbility, Is.False);
            Assert.That(passive.ActiveAbilityId, Is.Empty);
            Assert.That(passive.ActiveCooldownTurns, Is.Zero);
            Assert.That(passive.TriggerRules, Is.Empty);
            Assert.That(passive.ResourceRequirements, Is.Empty);
            Assert.That(passive.CombatStatusSources, Is.Empty);
        }

        [Test]
        public void ExtendedFactoryCopiesTriggerAndResourceContributions()
        {
            var rule = new EmptyTriggerRule();
            var requirement = new CombatResourceRequirement(
                "water_element",
                5);
            ICombatTriggerRule[] triggerRules = { rule };
            CombatResourceRequirement[] resourceRequirements =
                { requirement };
            var statusSource = new TestCombatStatusSource();
            ICharacterCombatStatusSource[] statusSources = { statusSource };

            CharacterCombatProviderRegistrationResult result =
                CharacterCombatProviderRegistrationResult.WithActiveAbility(
                    "active_id",
                    8,
                    triggerRules,
                    resourceRequirements,
                    statusSources);
            triggerRules[0] = new EmptyTriggerRule();
            resourceRequirements[0] = new CombatResourceRequirement(
                "other_resource",
                1);
            statusSources[0] = new TestCombatStatusSource();

            Assert.That(result.TriggerRules, Has.Count.EqualTo(1));
            Assert.That(result.TriggerRules[0], Is.SameAs(rule));
            Assert.That(result.ResourceRequirements,
                Has.Count.EqualTo(1));
            Assert.That(result.ResourceRequirements[0],
                Is.SameAs(requirement));
            Assert.That(result.CombatStatusSources, Has.Count.EqualTo(1));
            Assert.That(result.CombatStatusSources[0],
                Is.SameAs(statusSource));
        }

        [Test]
        public void ExtendedFactoriesRejectNullCollectionsAndEntries()
        {
            var rules = new ICombatTriggerRule[] { new EmptyTriggerRule() };
            var requirements = new[]
            {
                new CombatResourceRequirement("water_element", 5)
            };

            Assert.Throws<ArgumentNullException>(() =>
                CharacterCombatProviderRegistrationResult.WithActiveAbility(
                    "active_id",
                    8,
                    null,
                    requirements));
            Assert.Throws<ArgumentNullException>(() =>
                CharacterCombatProviderRegistrationResult.WithActiveAbility(
                    "active_id",
                    8,
                    rules,
                    null));
            Assert.Throws<ArgumentException>(() =>
                CharacterCombatProviderRegistrationResult
                    .WithoutActiveAbility(
                        new ICombatTriggerRule[] { null },
                        requirements));
            Assert.Throws<ArgumentException>(() =>
                CharacterCombatProviderRegistrationResult
                    .WithoutActiveAbility(
                        rules,
                        new CombatResourceRequirement[] { null }));
            Assert.Throws<ArgumentNullException>(() =>
                CharacterCombatProviderRegistrationResult.WithActiveAbility(
                    "active_id",
                    8,
                    rules,
                    requirements,
                    null));
            Assert.Throws<ArgumentException>(() =>
                CharacterCombatProviderRegistrationResult
                    .WithoutActiveAbility(
                        rules,
                        requirements,
                        new ICharacterCombatStatusSource[] { null }));
        }

        [Test]
        public void ResourceRequirementValidatesAndExposesValues()
        {
            var requirement = new CombatResourceRequirement(
                "water_element",
                5);

            Assert.That(requirement.ResourceId,
                Is.EqualTo("water_element"));
            Assert.That(requirement.MaxAmount, Is.EqualTo(5));
            Assert.Throws<ArgumentException>(() =>
                new CombatResourceRequirement(" ", 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CombatResourceRequirement("water_element", 0));
        }

        [Test]
        public void CatalogPassesSharedActionIdsAndReturnsContributions()
        {
            var config = ScriptableObject.CreateInstance<TestCombatConfig>();
            CharacterDefinition definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            try
            {
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("id").stringValue = "character_test";
                serialized.FindProperty("combatConfig").objectReferenceValue =
                    config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var rule = new EmptyTriggerRule();
                var requirement = new CombatResourceRequirement(
                    "water_element",
                    5);
                CharacterCombatProviderRegistrationResult expected =
                    CharacterCombatProviderRegistrationResult
                        .WithoutActiveAbility(
                            new ICombatTriggerRule[] { rule },
                            new[] { requirement });
                var registrar = new CapturingRegistrar(expected);
                var catalog = new CharacterCombatProviderRegistrarCatalog(
                    new ICharacterCombatProviderRegistrar[] { registrar });
                var member = new BattlePartyMemberInput(
                    "character_test",
                    partySlotIndex: 0,
                    level: 1,
                    awakening: 0,
                    definition);
                var character = new CharacterBattleState(
                    "character_test",
                    0,
                    ElementType.Fire,
                    100,
                    10d);
                var boss = new BossBattleState(
                    "boss_test",
                    ElementType.Grass,
                    1000,
                    10d);
                var actionIds = new CombatActionIdSequence();

                CharacterCombatProviderRegistrationResult actual =
                    catalog.Register(
                        member,
                        character,
                        boss,
                        actionIds,
                        new MatchEventActionProviderRegistry(),
                        new ActiveAbilityActionProviderRegistry());

                Assert.That(actual, Is.SameAs(expected));
                Assert.That(actual.TriggerRules[0], Is.SameAs(rule));
                Assert.That(actual.ResourceRequirements[0],
                    Is.SameAs(requirement));
                Assert.That(registrar.ReceivedActionIds,
                    Is.SameAs(actionIds));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private sealed class EmptyTriggerRule : ICombatTriggerRule
        {
            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                return Array.Empty<CombatAction>();
            }
        }

        private sealed class TestCombatStatusSource
            : ICharacterCombatStatusSource
        {
            public string StatusId => "status_test";
            public string CharacterId => "character_test";
            public int PartySlotIndex => 0;
            public int CurrentCount => 1;
            public bool IsVisible => true;
        }

        private sealed class CapturingRegistrar
            : ICharacterCombatProviderRegistrar
        {
            private readonly CharacterCombatProviderRegistrationResult result;

            public CapturingRegistrar(
                CharacterCombatProviderRegistrationResult result)
            {
                this.result = result;
            }

            public Type SupportedCombatConfigType => typeof(TestCombatConfig);
            public string CharacterId => "test_character";
            public CombatActionIdSequence ReceivedActionIds { get; private set; }

            public CharacterCombatProviderRegistrationResult Register(
                BattlePartyMemberInput member,
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                ReceivedActionIds = actionIds;
                return result;
            }
        }

        private sealed class TestCombatConfig : CombatConfigDefinition
        {
            public override bool TryValidate(out string errorMessage)
            {
                errorMessage = string.Empty;
                return true;
            }
        }
    }
}
