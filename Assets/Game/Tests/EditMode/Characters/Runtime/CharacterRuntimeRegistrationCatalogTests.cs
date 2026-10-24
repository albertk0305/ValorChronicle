using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Fiora;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Runtime
{
    public sealed class CharacterRuntimeRegistrationCatalogTests
    {
        private const string TestCharacterId = "runtime_test_character";

        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void DefaultCatalogRegistersPlayableCharactersExactlyOnce()
        {
            CharacterRuntimeRegistrationCatalog catalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();

            Assert.That(catalog.Registrations.Count, Is.EqualTo(4));
            CharacterRuntimeRegistration registration = catalog
                .Registrations
                .Single(item => item.CharacterId
                    == MareaBluefangRules.CharacterId);
            Assert.That(
                registration.CharacterId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(
                registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(MareaBluefangCombatConfig)));
            Assert.That(
                registration.CreateBuildModifier(),
                Is.TypeOf<MareaBluefangBuildModifier>());
            Assert.That(
                registration.CreateCombatRegistrar(),
                Is.TypeOf<MareaBluefangCombatProviderRegistrar>());
            Assert.That(
                registration.CreateLookupProvider(),
                Is.TypeOf<MareaBluefangLookupDescriptionProvider>());

            CharacterRuntimeRegistration serena = catalog.Registrations
                .Single(item => item.CharacterId
                    == SerenaCoralspearRules.CharacterId);
            Assert.That(
                serena.SupportedCombatConfigType,
                Is.EqualTo(typeof(SerenaCoralspearCombatConfig)));
            Assert.That(
                serena.CreateBuildModifier(),
                Is.TypeOf<SerenaCoralspearBuildModifier>());
            Assert.That(
                serena.CreateCombatRegistrar(),
                Is.TypeOf<SerenaCoralspearCombatProviderRegistrar>());
            Assert.That(
                serena.CreateLookupProvider(),
                Is.TypeOf<SerenaCoralspearLookupDescriptionProvider>());

            CharacterRuntimeRegistration miyuki = catalog.Registrations
                .Single(item => item.CharacterId
                    == ShimotsukiMiyukiRules.CharacterId);
            Assert.That(
                miyuki.SupportedCombatConfigType,
                Is.EqualTo(typeof(ShimotsukiMiyukiCombatConfig)));
            Assert.That(
                miyuki.CreateBuildModifier(),
                Is.TypeOf<ShimotsukiMiyukiBuildModifier>());
            Assert.That(
                miyuki.CreateCombatRegistrar(),
                Is.TypeOf<ShimotsukiMiyukiCombatProviderRegistrar>());
            Assert.That(
                miyuki.CreateLookupProvider(),
                Is.TypeOf<ShimotsukiMiyukiLookupDescriptionProvider>());

            CharacterRuntimeRegistration fiora = catalog.Registrations
                .Single(item => item.CharacterId
                    == FioraAmberwingRules.CharacterId);
            Assert.That(
                fiora.SupportedCombatConfigType,
                Is.EqualTo(typeof(FioraAmberwingCombatConfig)));
            Assert.That(
                fiora.CreateBuildModifier(),
                Is.TypeOf<FioraAmberwingBuildModifier>());
            Assert.That(
                fiora.CreateCombatRegistrar(),
                Is.TypeOf<FioraAmberwingCombatProviderRegistrar>());
            Assert.That(
                fiora.CreateLookupProvider(),
                Is.TypeOf<FioraAmberwingLookupDescriptionProvider>());
            Assert.That(
                catalog.Registrations.Select(item => item.CharacterId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(4));
        }

        [Test]
        public void DefaultSerenaFactoriesCreateIndependentInstances()
        {
            CharacterRuntimeRegistration first =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == SerenaCoralspearRules.CharacterId);
            CharacterRuntimeRegistration second =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == SerenaCoralspearRules.CharacterId);

            Assert.That(first.CreateBuildModifier(), Is.Not.SameAs(
                second.CreateBuildModifier()));
            Assert.That(first.CreateCombatRegistrar(), Is.Not.SameAs(
                second.CreateCombatRegistrar()));
            Assert.That(first.CreateLookupProvider(), Is.Not.SameAs(
                second.CreateLookupProvider()));
        }

        [Test]
        public void CatalogRejectsDuplicateCharacterId()
        {
            CharacterRuntimeRegistration first = Registration(
                "duplicate_character",
                typeof(TestCombatConfig));
            CharacterRuntimeRegistration second = Registration(
                "duplicate_character",
                typeof(AlternateTestCombatConfig));

            Assert.That(
                () => new CharacterRuntimeRegistrationCatalog(
                    new[] { first, second }),
                Throws.ArgumentException.With.Message.Contains(
                    "Duplicate character runtime registration ID"));
        }

        [Test]
        public void CatalogRejectsDuplicateCombatConfigType()
        {
            CharacterRuntimeRegistration first = Registration(
                "first_character",
                typeof(TestCombatConfig));
            CharacterRuntimeRegistration second = Registration(
                "second_character",
                typeof(TestCombatConfig));

            Assert.That(
                () => new CharacterRuntimeRegistrationCatalog(
                    new[] { first, second }),
                Throws.ArgumentException.With.Message.Contains(
                    "Duplicate character CombatConfig registration"));
        }

        [Test]
        public void CatalogRejectsNullRegistration()
        {
            Assert.That(
                () => new CharacterRuntimeRegistrationCatalog(
                    new CharacterRuntimeRegistration[] { null }),
                Throws.ArgumentException.With.Message.Contains(
                    "entry 0 is null"));
        }

        [Test]
        public void RegistrationRejectsNullFactoriesAndFactoryResults()
        {
            Assert.That(
                () => new CharacterRuntimeRegistration(
                    TestCharacterId,
                    null,
                    () => new TestCombatRegistrar(
                        TestCharacterId,
                        typeof(TestCombatConfig)),
                    () => new TestLookupProvider(TestCharacterId)),
                Throws.ArgumentNullException);
            Assert.That(
                () => new CharacterRuntimeRegistration(
                    TestCharacterId,
                    () => null,
                    () => new TestCombatRegistrar(
                        TestCharacterId,
                        typeof(TestCombatConfig)),
                    () => new TestLookupProvider(TestCharacterId)),
                Throws.InvalidOperationException.With.Message.Contains(
                    "Build modifier factory"));
            Assert.That(
                () => new CharacterRuntimeRegistration(
                    TestCharacterId,
                    () => new TestBuildModifier(TestCharacterId),
                    () => null,
                    () => new TestLookupProvider(TestCharacterId)),
                Throws.InvalidOperationException.With.Message.Contains(
                    "Combat registrar factory"));
            Assert.That(
                () => new CharacterRuntimeRegistration(
                    TestCharacterId,
                    () => new TestBuildModifier(TestCharacterId),
                    () => new TestCombatRegistrar(
                        TestCharacterId,
                        typeof(TestCombatConfig)),
                    () => null),
                Throws.InvalidOperationException.With.Message.Contains(
                    "Lookup provider factory"));
        }

        [TestCase(ComponentKind.Build)]
        [TestCase(ComponentKind.Combat)]
        [TestCase(ComponentKind.Lookup)]
        public void RegistrationRejectsComponentCharacterIdMismatch(
            ComponentKind mismatchedComponent)
        {
            const string wrongId = "wrong_character";

            Assert.That(
                () => new CharacterRuntimeRegistration(
                    TestCharacterId,
                    () => new TestBuildModifier(
                        mismatchedComponent == ComponentKind.Build
                            ? wrongId
                            : TestCharacterId),
                    () => new TestCombatRegistrar(
                        mismatchedComponent == ComponentKind.Combat
                            ? wrongId
                            : TestCharacterId,
                        typeof(TestCombatConfig)),
                    () => new TestLookupProvider(
                        mismatchedComponent == ComponentKind.Lookup
                            ? wrongId
                            : TestCharacterId)),
                Throws.InvalidOperationException.With.Message.Contains(
                    "does not match registration ID"));
        }

        [Test]
        public void RegistrationRejectsInvalidCombatConfigType()
        {
            Assert.That(
                () => Registration(TestCharacterId, typeof(string)),
                Throws.InvalidOperationException.With.Message.Contains(
                    "invalid supported CombatConfig type"));
        }

        [Test]
        public void SecondRegistrationIsProjectedIntoAllThreeRuntimePaths()
        {
            CharacterRuntimeRegistration registration = Registration(
                TestCharacterId,
                typeof(TestCombatConfig));
            var catalog = new CharacterRuntimeRegistrationCatalog(
                new[] { registration });
            TestCombatConfig config = Create<TestCombatConfig>();
            CharacterDefinition definition = CreateCharacterDefinition(
                TestCharacterId,
                config);

            CharacterBuildResolver buildResolver =
                catalog.CreateBuildResolver();
            ResolvedCharacterBuild build = buildResolver.Resolve(
                definition,
                level: 1,
                awakening: 0);
            Assert.That(build.MaxHp, Is.EqualTo(20));
            Assert.That(build.Attack, Is.EqualTo(60));

            CharacterCombatProviderRegistrarCatalog combatCatalog =
                catalog.CreateCombatRegistrarCatalog();
            CharacterCombatProviderRegistrationResult combatResult =
                combatCatalog.Register(
                    new BattlePartyMemberInput(
                        TestCharacterId,
                        partySlotIndex: 0,
                        level: 1,
                        awakening: 0,
                        definition),
                    new CharacterBattleState(
                        TestCharacterId,
                        partySlotIndex: 0,
                        ElementType.Water,
                        maxHp: 20,
                        attack: 60d),
                    new BossBattleState(
                        "test_boss",
                        ElementType.Fire,
                        maxHp: 100,
                        attack: 1d),
                    new CombatActionIdSequence(),
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry());
            Assert.That(combatResult, Is.Not.Null);

            DefinitionDatabase database = CreateDatabase(definition);
            LocalizationService localization = new LocalizationService(
                new[]
                {
                    LocalizationTableParser.Parse(
                        "runtime_registration_test",
                        "key\ten\n"
                            + "runtime.title\tRuntime Title\n"
                            + "runtime.body\tRuntime Body\n"
                            + "character.lookup.description_unavailable"
                            + "\tUnavailable")
                });
            CharacterLookupDescriptionResolver lookupResolver =
                catalog.CreateLookupDescriptionResolver(
                    database,
                    localization);
            CharacterLookupDescriptionResult lookup =
                lookupResolver.ResolveSkill(
                    TestCharacterId,
                    currentAwakening: 0,
                    CharacterLookupSkillType.Match3);
            Assert.That(lookup.IsSuccess, Is.True, lookup.Error);
            Assert.That(lookup.Text, Is.EqualTo(
                "Runtime Title\n\nRuntime Body"));
        }

        [Test]
        public void CombatRegistrarInstancesAreIndependentAcrossCatalogs()
        {
            var createdRegistrars = new List<TestCombatRegistrar>();
            var registration = new CharacterRuntimeRegistration(
                TestCharacterId,
                () => new TestBuildModifier(TestCharacterId),
                () =>
                {
                    var registrar = new TestCombatRegistrar(
                        TestCharacterId,
                        typeof(TestCombatConfig));
                    createdRegistrars.Add(registrar);
                    return registrar;
                },
                () => new TestLookupProvider(TestCharacterId));
            var catalog = new CharacterRuntimeRegistrationCatalog(
                new[] { registration });

            CharacterCombatProviderRegistrarCatalog first =
                catalog.CreateCombatRegistrarCatalog();
            CharacterCombatProviderRegistrarCatalog second =
                catalog.CreateCombatRegistrarCatalog();

            Assert.That(createdRegistrars, Has.Count.EqualTo(3));
            Assert.That(createdRegistrars[1], Is.Not.SameAs(
                createdRegistrars[2]));
            Assert.That(first, Is.Not.SameAs(second));
        }

        private CharacterRuntimeRegistration Registration(
            string characterId,
            Type combatConfigType)
        {
            return new CharacterRuntimeRegistration(
                characterId,
                () => new TestBuildModifier(characterId),
                () => new TestCombatRegistrar(
                    characterId,
                    combatConfigType),
                () => new TestLookupProvider(characterId));
        }

        private CharacterDefinition CreateCharacterDefinition(
            string characterId,
            CombatConfigDefinition combatConfig)
        {
            CharacterDefinition definition = Create<CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = characterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Water;
            serialized.FindProperty("level1Hp").intValue = 10;
            serialized.FindProperty("level1Attack").intValue = 20;
            serialized.FindProperty("level100Hp").intValue = 10;
            serialized.FindProperty("level100Attack").intValue = 20;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                combatConfig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private DefinitionDatabase CreateDatabase(
            CharacterDefinition definition)
        {
            DefinitionDatabase database = Create<DefinitionDatabase>();
            var serialized = new SerializedObject(database);
            SerializedProperty characters = serialized.FindProperty(
                "characters");
            characters.arraySize = 1;
            characters.GetArrayElementAtIndex(0).objectReferenceValue =
                definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            database.Initialize();
            return database;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }

        public enum ComponentKind
        {
            Build,
            Combat,
            Lookup
        }

        private sealed class TestBuildModifier : ICharacterBuildModifier
        {
            public TestBuildModifier(string characterId)
            {
                CharacterId = characterId;
            }

            public string CharacterId { get; }

            public CharacterBuildStatMultipliers GetStatMultipliers(
                int awakening)
            {
                return new CharacterBuildStatMultipliers(
                    maxHpMultiplier: 2d,
                    attackMultiplier: 3d);
            }
        }

        private sealed class TestCombatRegistrar
            : ICharacterCombatProviderRegistrar
        {
            public TestCombatRegistrar(
                string characterId,
                Type supportedCombatConfigType)
            {
                CharacterId = characterId;
                SupportedCombatConfigType = supportedCombatConfigType;
            }

            public string CharacterId { get; }
            public Type SupportedCombatConfigType { get; }

            public CharacterCombatProviderRegistrationResult Register(
                BattlePartyMemberInput member,
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                return CharacterCombatProviderRegistrationResult
                    .WithoutActiveAbility();
            }
        }

        private sealed class TestLookupProvider
            : ICharacterLookupDescriptionProvider
        {
            public TestLookupProvider(string characterId)
            {
                CharacterId = characterId;
            }

            public string CharacterId { get; }

            public CharacterLookupDescriptionDefinition ResolveSkill(
                CharacterDefinition definition,
                int awakening,
                CharacterLookupSkillType skillType)
            {
                return Description();
            }

            public CharacterLookupDescriptionDefinition ResolveAwakening(
                CharacterDefinition definition,
                int awakeningStage)
            {
                return Description();
            }

            private static CharacterLookupDescriptionDefinition Description()
            {
                return new CharacterLookupDescriptionDefinition(
                    new CharacterLookupLocalizedText("runtime.title"),
                    new[]
                    {
                        new CharacterLookupLocalizedText("runtime.body")
                    });
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

        private sealed class AlternateTestCombatConfig
            : CombatConfigDefinition
        {
            public override bool TryValidate(out string errorMessage)
            {
                errorMessage = string.Empty;
                return true;
            }
        }
    }
}
