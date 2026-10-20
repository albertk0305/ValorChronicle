using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Runtime
{
    public sealed class CharacterContentRegistrationValidationTests
    {
        private const string CharacterId = "character_validation_test";
        private const string SkillId = "skill_validation_test";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";

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
        public void CanonicalCharacterContentRegistrationIsComplete()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            CharacterPresentationCatalog presentationCatalog =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationCatalog>(
                    PresentationCatalogPath);
            TextAsset localizationAsset = AssetDatabase.LoadAssetAtPath<
                TextAsset>(LocalizationPath);
            Assert.That(database, Is.Not.Null, DatabasePath);
            Assert.That(presentationCatalog, Is.Not.Null,
                PresentationCatalogPath);
            Assert.That(localizationAsset, Is.Not.Null, LocalizationPath);

            database.Initialize();
            presentationCatalog.Initialize();
            CharacterRuntimeRegistrationCatalog runtimeCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();

            ValidationReport dataReport = DataValidator.Validate(database);
            Assert.That(
                dataReport.Issues.Where(issue =>
                    issue.Severity == ValidationSeverity.Error),
                Is.Empty);
            Assert.That(
                CharacterContentRegistrationValidator.Validate(
                    database,
                    presentationCatalog,
                    runtimeCatalog),
                Is.Empty);

            Assert.That(
                database.TryGetCharacter(
                    MareaBluefangRules.CharacterId,
                    out CharacterDefinition marea),
                Is.True);
            Assert.That(
                marea.CombatConfig.GetType(),
                Is.EqualTo(typeof(MareaBluefangCombatConfig)));
            Assert.That(
                presentationCatalog.TryGet(
                    MareaBluefangRules.CharacterId,
                    out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation.FaceSprite, Is.Not.Null);

            Assert.That(
                database.TryGetCharacter(
                    SerenaCoralspearRules.CharacterId,
                    out CharacterDefinition serena),
                Is.True);
            Assert.That(
                serena.CombatConfig.GetType(),
                Is.EqualTo(typeof(SerenaCoralspearCombatConfig)));
            Assert.That(serena.SkillIds.Count, Is.EqualTo(5));
            for (int index = 0; index < serena.SkillIds.Count; index++)
            {
                Assert.That(
                    database.TryGetSkill(serena.SkillIds[index], out _),
                    Is.True,
                    serena.SkillIds[index]);
            }

            Assert.That(
                database.TryGetEffect(
                    SerenaCoralspearRules.ActiveEffectId,
                    out EffectDefinition encoreAria),
                Is.True);
            Assert.That(encoreAria.Icon, Is.Not.Null);
            Assert.That(
                presentationCatalog.TryGet(
                    SerenaCoralspearRules.CharacterId,
                    out CharacterPresentationDefinition serenaPresentation),
                Is.True);
            Assert.That(serenaPresentation.FaceSprite, Is.Not.Null);
            Assert.That(serenaPresentation.PreviewSprite, Is.Not.Null);

            Assert.That(
                CharacterBuildResolverFactory.CreateDefault().Resolve(
                    marea,
                    level: 1,
                    awakening: 0),
                Is.Not.Null);
            Assert.That(
                runtimeCatalog.CreateCombatRegistrarCatalog(),
                Is.Not.Null);
            var localization = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(
                    localizationAsset.name,
                    localizationAsset.text)
            });
            CharacterLookupDescriptionResult lookup =
                CharacterLookupDescriptionResolverFactory.CreateDefault(
                        database,
                        localization)
                    .ResolveSkill(
                        MareaBluefangRules.CharacterId,
                        currentAwakening: 0,
                        CharacterLookupSkillType.Match3);
            Assert.That(lookup.IsSuccess, Is.True, lookup.Error);

            CharacterLookupDescriptionResolver serenaLookupResolver =
                CharacterLookupDescriptionResolverFactory.CreateDefault(
                    database,
                    localization);
            var serenaSkillTypes = new[]
            {
                CharacterLookupSkillType.Match3,
                CharacterLookupSkillType.Match4,
                CharacterLookupSkillType.Match5,
                CharacterLookupSkillType.Passive,
                CharacterLookupSkillType.Active
            };
            for (int index = 0; index < serenaSkillTypes.Length; index++)
            {
                CharacterLookupDescriptionResult result =
                    serenaLookupResolver.ResolveSkill(
                        SerenaCoralspearRules.CharacterId,
                        currentAwakening: 6,
                        serenaSkillTypes[index]);
                Assert.That(result.IsSuccess, Is.True, result.Error);
                Assert.That(result.Text, Does.Not.Contain("[["));
                Assert.That(result.Text, Does.Not.Contain("{"));
            }

            for (int stage = 1; stage <= 6; stage++)
            {
                CharacterLookupDescriptionResult result =
                    serenaLookupResolver.ResolveAwakening(
                        SerenaCoralspearRules.CharacterId,
                        stage);
                Assert.That(result.IsSuccess, Is.True, result.Error);
                Assert.That(result.Text, Does.Not.Contain("[["));
                Assert.That(result.Text, Does.Not.Contain("{"));
            }

            CharacterLookupDescriptionResult serenaEnglish =
                serenaLookupResolver.ResolveSkill(
                    SerenaCoralspearRules.CharacterId,
                    currentAwakening: 6,
                    CharacterLookupSkillType.Active);
            Assert.That(serenaEnglish.IsSuccess,
                Is.True,
                serenaEnglish.Error);
            Assert.That(serenaEnglish.Text,
                Does.Contain("Encore Aria"));
            Assert.That(serenaEnglish.Text, Does.Contain("45%"));
            Assert.That(localization.GetText(serena.DisplayNameKey),
                Is.EqualTo("Serena Coralspear"));
            Assert.That(localization.GetText(encoreAria.DisplayNameKey),
                Is.EqualTo("Encore Aria"));
            Assert.That(localization.GetText(encoreAria.DescriptionKey),
                Does.Contain("Additional and Chase"));

            localization.SetLocale("ko");
            CharacterLookupDescriptionResult serenaKorean =
                serenaLookupResolver.ResolveAwakening(
                    SerenaCoralspearRules.CharacterId,
                    awakeningStage: 6);
            Assert.That(serenaKorean.IsSuccess,
                Is.True,
                serenaKorean.Error);
            Assert.That(serenaKorean.Text,
                Does.Contain("영원한 리프레인"));
            Assert.That(serenaKorean.Text, Does.Contain("1개"));
            Assert.That(localization.GetText(serena.DisplayNameKey),
                Is.EqualTo("세레나 코랄스피어"));
            Assert.That(localization.GetText(encoreAria.DisplayNameKey),
                Is.EqualTo("앙코르 아리아"));
        }

        [Test]
        public void RuntimeRegistrationWithoutCharacterDefinitionIsReported()
        {
            IReadOnlyList<string> issues = Validate(
                CreateDatabase(),
                CreatePresentationCatalog(),
                CreateRuntimeCatalog());

            AssertIssue(issues, CharacterId, "has no CharacterDefinition");
        }

        [Test]
        public void ObtainableCharacterWithoutRuntimeRegistrationIsReported()
        {
            TestCombatConfig config = Create<TestCombatConfig>();
            CharacterDefinition character = CreateCharacter(config, SkillId);
            SkillDefinition skill = CreateSkill();
            GachaDefinition gacha = CreateGacha(
                "gacha_validation_test",
                CharacterId);

            IReadOnlyList<string> issues = Validate(
                CreateDatabase(
                    new[] { character },
                    new[] { skill },
                    new[] { gacha }),
                CreatePresentationCatalog(
                    CreatePresentation(CharacterId, CreateSprite())),
                EmptyRuntimeCatalog());

            AssertIssue(
                issues,
                CharacterId,
                "has no default RuntimeRegistration");
        }

        [Test]
        public void ExchangeCharacterWithoutRuntimeRegistrationIsReported()
        {
            TestCombatConfig config = Create<TestCombatConfig>();
            CharacterDefinition character = CreateCharacter(config, SkillId);
            ExchangeCatalogDefinition exchange = CreateExchange(CharacterId);

            IReadOnlyList<string> issues = Validate(
                CreateDatabase(
                    new[] { character },
                    new[] { CreateSkill() },
                    exchangeCatalog: exchange),
                CreatePresentationCatalog(
                    CreatePresentation(CharacterId, CreateSprite())),
                EmptyRuntimeCatalog());

            AssertIssue(
                issues,
                CharacterId,
                "ExchangeCatalogDefinition",
                "has no default RuntimeRegistration");
        }

        [Test]
        public void MissingAndMismatchedCombatConfigsAreReported()
        {
            CharacterDefinition missing = CreateCharacter(
                (CombatConfigDefinition)null,
                SkillId);
            IReadOnlyList<string> missingIssues = Validate(
                CreateDatabase(
                    new[] { missing },
                    new[] { CreateSkill() }),
                CreatePresentationCatalog(
                    CreatePresentation(CharacterId, CreateSprite())),
                CreateRuntimeCatalog());
            AssertIssue(missingIssues, CharacterId, "has no CombatConfig");

            AlternateTestCombatConfig wrongConfig =
                Create<AlternateTestCombatConfig>();
            CharacterDefinition mismatched = CreateCharacter(
                wrongConfig,
                SkillId);
            IReadOnlyList<string> mismatchIssues = Validate(
                CreateDatabase(
                    new[] { mismatched },
                    new[] { CreateSkill() }),
                CreatePresentationCatalog(
                    CreatePresentation(CharacterId, CreateSprite())),
                CreateRuntimeCatalog());
            AssertIssue(
                mismatchIssues,
                typeof(TestCombatConfig).FullName,
                typeof(AlternateTestCombatConfig).FullName);
        }

        [Test]
        public void MissingSkillPresentationAndFaceSpriteAreReported()
        {
            CharacterDefinition character = CreateCharacter(
                Create<TestCombatConfig>(),
                SkillId);
            CharacterPresentationDefinition noFace = CreatePresentation(
                CharacterId,
                faceSprite: null);

            IReadOnlyList<string> issues = Validate(
                CreateDatabase(new[] { character }),
                CreatePresentationCatalog(noFace),
                CreateRuntimeCatalog());

            AssertIssue(issues, CharacterId, "missing SkillDefinition");
            AssertIssue(issues, CharacterId, "has no FaceSprite");

            IReadOnlyList<string> noPresentationIssues = Validate(
                CreateDatabase(
                    new[] { character },
                    new[] { CreateSkill() }),
                CreatePresentationCatalog(),
                CreateRuntimeCatalog());
            AssertIssue(
                noPresentationIssues,
                CharacterId,
                "has no CharacterPresentationDefinition");
        }

        [Test]
        public void UnknownAndDuplicatePresentationIdsAreReported()
        {
            CharacterDefinition character = CreateCharacter(
                Create<TestCombatConfig>(),
                SkillId);
            CharacterPresentationDefinition first = CreatePresentation(
                CharacterId,
                CreateSprite());
            CharacterPresentationDefinition duplicate = CreatePresentation(
                CharacterId,
                CreateSprite());
            CharacterPresentationDefinition unknown = CreatePresentation(
                "character_unknown",
                CreateSprite());

            IReadOnlyList<string> issues = Validate(
                CreateDatabase(
                    new[] { character },
                    new[] { CreateSkill() }),
                CreatePresentationCatalog(
                    first,
                    duplicate,
                    unknown,
                    null),
                CreateRuntimeCatalog());

            AssertIssue(
                issues,
                CharacterId,
                "Duplicate CharacterPresentationDefinition ID");
            AssertIssue(
                issues,
                "character_unknown",
                "unknown CharacterId");
            AssertIssue(issues, "broken/null", "index 3");
        }

        [Test]
        public void UnreleasedDefinitionIsNotForcedToBePlayable()
        {
            CharacterDefinition unfinished = CreateCharacter(
                characterId: "character_unfinished",
                combatConfig: null);

            IReadOnlyList<string> issues = Validate(
                CreateDatabase(new[] { unfinished }),
                CreatePresentationCatalog(),
                EmptyRuntimeCatalog());

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void CharacterMayBeAbsentFromGachaOrSharedAcrossGachas()
        {
            CharacterDefinition character = CreateCharacter(
                Create<TestCombatConfig>(),
                SkillId);
            SkillDefinition skill = CreateSkill();
            CharacterPresentationCatalog presentationCatalog =
                CreatePresentationCatalog(
                    CreatePresentation(CharacterId, CreateSprite()));
            CharacterRuntimeRegistrationCatalog runtimeCatalog =
                CreateRuntimeCatalog();

            Assert.That(
                Validate(
                    CreateDatabase(
                        new[] { character },
                        new[] { skill }),
                    presentationCatalog,
                    runtimeCatalog),
                Is.Empty);

            Assert.That(
                Validate(
                    CreateDatabase(
                        new[] { character },
                        new[] { skill },
                        new[]
                        {
                            CreateGacha("gacha_first", CharacterId),
                            CreateGacha("gacha_second", CharacterId)
                        }),
                    presentationCatalog,
                    runtimeCatalog),
                Is.Empty);
        }

        [Test]
        public void ExistingDataValidatorOwnsSkillAndGachaReferenceRules()
        {
            CharacterDefinition character = CreateCharacter(
                Create<TestCombatConfig>(),
                "skill_missing");
            GachaDefinition gacha = CreateGacha(
                "gacha_missing_character",
                "character_missing");
            DefinitionDatabase database = CreateDatabase(
                new[] { character },
                gachas: new[] { gacha });

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains(
                    "Missing skill reference")), Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Message.Contains(
                    "Gacha character pool references missing ID")),
                Is.True);
        }

        private IReadOnlyList<string> Validate(
            DefinitionDatabase database,
            CharacterPresentationCatalog presentationCatalog,
            CharacterRuntimeRegistrationCatalog runtimeCatalog)
        {
            return CharacterContentRegistrationValidator.Validate(
                database,
                presentationCatalog,
                runtimeCatalog);
        }

        private CharacterRuntimeRegistrationCatalog CreateRuntimeCatalog()
        {
            return new CharacterRuntimeRegistrationCatalog(
                new[]
                {
                    new CharacterRuntimeRegistration(
                        CharacterId,
                        () => new TestBuildModifier(CharacterId),
                        () => new TestCombatRegistrar(CharacterId),
                        () => new TestLookupProvider(CharacterId))
                });
        }

        private static CharacterRuntimeRegistrationCatalog
            EmptyRuntimeCatalog()
        {
            return new CharacterRuntimeRegistrationCatalog(
                Array.Empty<CharacterRuntimeRegistration>());
        }

        private DefinitionDatabase CreateDatabase(
            CharacterDefinition[] characters = null,
            SkillDefinition[] skills = null,
            GachaDefinition[] gachas = null,
            ExchangeCatalogDefinition exchangeCatalog = null)
        {
            DefinitionDatabase database = Create<DefinitionDatabase>();
            var serialized = new SerializedObject(database);
            SetObjectArray(
                serialized.FindProperty("characters"),
                characters);
            SetObjectArray(serialized.FindProperty("skills"), skills);
            SetObjectArray(serialized.FindProperty("gachas"), gachas);
            serialized.FindProperty("exchangeCatalog").objectReferenceValue =
                exchangeCatalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private CharacterDefinition CreateCharacter(
            CombatConfigDefinition combatConfig,
            params string[] skillIds)
        {
            return CreateCharacter(CharacterId, combatConfig, skillIds);
        }

        private CharacterDefinition CreateCharacter(
            string characterId,
            CombatConfigDefinition combatConfig,
            params string[] skillIds)
        {
            CharacterDefinition character = Create<CharacterDefinition>();
            var serialized = new SerializedObject(character);
            serialized.FindProperty("id").stringValue = characterId;
            serialized.FindProperty("displayNameKey").stringValue =
                "character.validation";
            serialized.FindProperty("level1Hp").intValue = 100;
            serialized.FindProperty("level1Attack").intValue = 20;
            serialized.FindProperty("level100Hp").intValue = 200;
            serialized.FindProperty("level100Attack").intValue = 40;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                combatConfig;
            SetStringArray(
                serialized.FindProperty("skillIds"),
                skillIds);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return character;
        }

        private SkillDefinition CreateSkill()
        {
            SkillDefinition skill = Create<SkillDefinition>();
            var serialized = new SerializedObject(skill);
            serialized.FindProperty("id").stringValue = SkillId;
            serialized.FindProperty("displayNameKey").stringValue =
                "skill.validation.name";
            serialized.FindProperty("descriptionKey").stringValue =
                "skill.validation.description";
            serialized.FindProperty("skillKind").enumValueIndex =
                (int)SkillKind.Match;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }

        private GachaDefinition CreateGacha(
            string gachaId,
            string characterId)
        {
            GachaDefinition gacha = Create<GachaDefinition>();
            var serialized = new SerializedObject(gacha);
            serialized.FindProperty("id").stringValue = gachaId;
            SetStringArray(
                serialized.FindProperty("characterPoolIds"),
                new[] { characterId });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gacha;
        }

        private ExchangeCatalogDefinition CreateExchange(
            params string[] characterIds)
        {
            ExchangeCatalogDefinition exchange =
                Create<ExchangeCatalogDefinition>();
            var serialized = new SerializedObject(exchange);
            SetStringArray(
                serialized.FindProperty("characterIds"),
                characterIds);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return exchange;
        }

        private CharacterPresentationDefinition CreatePresentation(
            string characterId,
            Sprite faceSprite)
        {
            CharacterPresentationDefinition presentation =
                Create<CharacterPresentationDefinition>();
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("characterId").stringValue = characterId;
            serialized.FindProperty("faceSprite").objectReferenceValue =
                faceSprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return presentation;
        }

        private CharacterPresentationCatalog CreatePresentationCatalog(
            params CharacterPresentationDefinition[] definitions)
        {
            CharacterPresentationCatalog catalog =
                Create<CharacterPresentationCatalog>();
            var serialized = new SerializedObject(catalog);
            SetObjectArray(
                serialized.FindProperty("definitions"),
                definitions);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                Vector2.zero);
            createdObjects.Add(sprite);
            createdObjects.Add(texture);
            return sprite;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }

        private static void SetObjectArray<TObject>(
            SerializedProperty property,
            TObject[] values)
            where TObject : UnityEngine.Object
        {
            values ??= Array.Empty<TObject>();
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }
        }

        private static void SetStringArray(
            SerializedProperty property,
            string[] values)
        {
            values ??= Array.Empty<string>();
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue =
                    values[index];
            }
        }

        private static void AssertIssue(
            IEnumerable<string> issues,
            params string[] fragments)
        {
            Assert.That(
                issues.Any(issue => fragments.All(fragment =>
                    issue.Contains(fragment))),
                Is.True,
                "Expected issue containing: "
                    + string.Join(", ", fragments)
                    + "\nActual issues:\n"
                    + string.Join("\n", issues));
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
                return CharacterBuildStatMultipliers.Identity;
            }
        }

        private sealed class TestCombatRegistrar
            : ICharacterCombatProviderRegistrar
        {
            public TestCombatRegistrar(string characterId)
            {
                CharacterId = characterId;
            }

            public string CharacterId { get; }
            public Type SupportedCombatConfigType =>
                typeof(TestCombatConfig);

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
                    new CharacterLookupLocalizedText("test.title"),
                    new[]
                    {
                        new CharacterLookupLocalizedText("test.body")
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
