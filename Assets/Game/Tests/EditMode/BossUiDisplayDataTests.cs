using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class BossUiDisplayDataTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private const string ElementIconSetPath =
            "Assets/Data/Party/ElementIconSet.asset";
        private const string FaceSpritePath =
            "Assets/Sprites/Boss/Kragmor/KragmorFace.png";
        private const string DefaultSpritePath =
            "Assets/Sprites/Boss/Kragmor/KragmorIdle.png";
        private const string ExposedSpritePath =
            "Assets/Sprites/Boss/Kragmor/KragmorExposed.png";
        private const string CompressionSpritePath =
            "Assets/Sprites/Boss/Kragmor/KragmorEnergy.png";

        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void CanonicalDatabase_ProvidesKragmorUiDefinitions()
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.That(database.Bosses, Is.Not.Empty);
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);
            Assert.That(boss.DisplayNameKey, Is.EqualTo(KragmorRules.BossId));
            Assert.That(database.Bosses, Does.Contain(boss));

            Assert.That(database.TryGetBossPresentation(
                boss.Id,
                out BossPresentationDefinition presentation), Is.True);
            Assert.That(presentation.FaceSprite, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
        }

        [Test]
        public void CanonicalPresentation_PreservesCombatSprites()
        {
            DefinitionDatabase database = LoadDatabase();
            Assert.That(database.TryGetBossPresentation(
                KragmorRules.BossId,
                out BossPresentationDefinition presentation), Is.True);

            Assert.That(
                AssetDatabase.GetAssetPath(presentation.DefaultSprite),
                Is.EqualTo(DefaultSpritePath));
            Assert.That(presentation.TryGetStateSprite(
                KragmorRules.CoreExposureVisualStateId,
                out Sprite exposed), Is.True);
            Assert.That(
                AssetDatabase.GetAssetPath(exposed),
                Is.EqualTo(ExposedSpritePath));
            Assert.That(presentation.TryGetStateSprite(
                KragmorRules.CoreCompressionVisualStateId,
                out Sprite compression), Is.True);
            Assert.That(
                AssetDatabase.GetAssetPath(compression),
                Is.EqualTo(CompressionSpritePath));
        }

        [Test]
        public void CanonicalLocalization_ResolvesKragmorInEnglishAndKorean()
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(LocalizationCatalogPath);

            Assert.That(catalog, Is.Not.Null);
            LocalizationService service = catalog.CreateService();
            Assert.That(
                service.GetText(KragmorRules.BossId, "en"),
                Is.EqualTo("Kragmor"));
            Assert.That(
                service.GetText(KragmorRules.BossId, "ko"),
                Is.EqualTo("용광거신 크라그모르"));
        }

        [Test]
        public void ElementIconSet_ResolvesIconFromBossElement()
        {
            DefinitionDatabase database = LoadDatabase();
            ElementIconSet icons = AssetDatabase.LoadAssetAtPath<
                ElementIconSet>(ElementIconSetPath);
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);

            Assert.That(icons, Is.Not.Null);
            Assert.That(icons.IsConfigured, Is.True);
            Assert.That(icons.GetIcon(boss.Element), Is.Not.Null);
        }

        [Test]
        public void SupportedBossFilter_AcceptsCanonicalKragmor()
        {
            DefinitionDatabase database = LoadDatabase();
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);

            Assert.That(
                BattleEncounterFactory.IsSupportedBoss(boss, out string error),
                Is.True,
                error);
        }

        [Test]
        public void SupportedBossFilter_RejectsUnimplementedBoss()
        {
            BossDefinition boss = CreateBoss("unimplemented_boss");

            Assert.That(
                BattleEncounterFactory.IsSupportedBoss(boss, out string error),
                Is.False);
            Assert.That(error, Does.Contain("no implemented battle composition"));
        }

        [Test]
        public void SupportedBossFilter_RejectsKragmorWithoutCombatConfig()
        {
            BossDefinition boss = CreateBoss(KragmorRules.BossId);

            Assert.That(
                BattleEncounterFactory.IsSupportedBoss(boss, out string error),
                Is.False);
            Assert.That(error, Does.Contain("KragmorCombatConfig"));
        }

        [Test]
        public void MissingPresentationData_IsSafeAndDoesNotGateCombatSupport()
        {
            BossPresentationDefinition presentation =
                ScriptableObject.CreateInstance<BossPresentationDefinition>();
            createdObjects.Add(presentation);

            Assert.That(presentation.FaceSprite, Is.Null);
            Assert.That(presentation.DefaultSprite, Is.Null);
            Assert.That(presentation.TryGetStateSprite(
                KragmorRules.CoreExposureVisualStateId,
                out Sprite stateSprite), Is.False);
            Assert.That(stateSprite, Is.Null);

            DefinitionDatabase database = LoadDatabase();
            Assert.That(database.TryGetBossPresentation(
                "unimplemented_boss",
                out BossPresentationDefinition missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);
            Assert.That(
                BattleEncounterFactory.IsSupportedBoss(boss, out string error),
                Is.True,
                error);
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private BossDefinition CreateBoss(string id)
        {
            BossDefinition boss = ScriptableObject.CreateInstance<
                BossDefinition>();
            createdObjects.Add(boss);
            var serializedObject = new SerializedObject(boss);
            serializedObject.FindProperty("id").stringValue = id;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return boss;
        }
    }
}
