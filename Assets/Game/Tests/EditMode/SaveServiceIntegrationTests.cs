using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveServiceIntegrationTests
    {
        private readonly List<string> roots = new List<string>();
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (string root in roots)
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            roots.Clear();

            foreach (UnityEngine.Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void RealRepository_CreateRotateAndReload_PreservesPreviousMainAsBackup()
        {
            (SaveRepository repository, SavePaths paths) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(10, 20));

            SaveLoadResult created = first.LoadOrCreate("profile_real");
            string firstMain = File.ReadAllText(paths.MainPath);
            SaveTransactionResult saved = first.ExecuteTransaction(profile => profile.Currencies.GachaCurrency = 7);

            Assert.That(created.Status, Is.EqualTo(SaveLoadStatus.CreatedNewProfile));
            Assert.That(saved.Status, Is.EqualTo(SaveTransactionStatus.Success));
            Assert.That(File.Exists(paths.MainPath), Is.True);
            Assert.That(File.Exists(paths.BackupPath), Is.True);
            Assert.That(File.ReadAllText(paths.BackupPath), Is.EqualTo(firstMain));

            SaveService second = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(30));
            SaveLoadResult reloaded = second.LoadOrCreate("ignored");
            Assert.That(reloaded.Status, Is.EqualTo(SaveLoadStatus.LoadedMain));
            Assert.That(reloaded.ProfileSnapshot.Currencies.GachaCurrency, Is.EqualTo(7));
        }

        [Test]
        public void RealRepository_CorruptMain_RecoversKnownGoodBackup()
        {
            (SaveRepository repository, SavePaths paths) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(10, 20));
            Assert.That(first.LoadOrCreate("profile_real").IsSuccess, Is.True);
            Assert.That(first.ExecuteTransaction(profile => profile.Currencies.GachaCurrency = 7).IsSuccess, Is.True);
            File.WriteAllText(paths.MainPath, "corrupt-main");

            SaveService second = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(30));
            SaveLoadResult recovered = second.LoadOrCreate("ignored");

            Assert.That(recovered.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(recovered.ProfileSnapshot.Currencies.GachaCurrency, Is.Zero);
            Assert.That(second.CanWriteCurrentProfile, Is.True);
            Assert.That(File.Exists(paths.TempPath), Is.False);
        }

        [Test]
        public void RealRepository_PartyMutation_PersistsAcrossServiceInstances()
        {
            (SaveRepository repository, _) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(10, 20));
            Assert.That(first.LoadOrCreate("profile_party").IsSuccess, Is.True);

            SaveTransactionResult transaction = first.ExecuteTransaction(profile =>
            {
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = "character_party_test",
                    Level = 1
                });
                profile.Party.Presets[0].CharacterSlotIds[0] =
                    "character_party_test";
            });

            Assert.That(transaction.IsSuccess, Is.True);
            SaveService second = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(30));
            Assert.That(second.LoadOrCreate("ignored").Status,
                Is.EqualTo(SaveLoadStatus.LoadedMain));
            Assert.That(
                second.GetCurrentProfileSnapshot().Party.Presets[0].CharacterSlotIds[0],
                Is.EqualTo("character_party_test"));
        }

        [Test]
        public void RealRepository_PartyEditsReloadAndResolveWithOriginalSlots()
        {
            (SaveRepository repository, _) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(10, 20, 30, 40, 50, 60, 70, 80));
            Assert.That(first.LoadOrCreate("profile_party_e2e").IsSuccess,
                Is.True);
            Assert.That(first.ExecuteTransaction(profile =>
            {
                profile.Characters.Add(Character("character_a", 11));
                profile.Characters.Add(Character("character_b", 22));
                profile.Characters.Add(Character("character_c", 33));
            }).IsSuccess, Is.True);

            var persistence = new PartyPersistenceService(
                first,
                new PartyPresetEditor());
            AssertSaved(persistence.ConfirmEdit(0, 0, "character_a"));
            AssertSaved(persistence.ConfirmEdit(0, 1, "character_b"));
            AssertSaved(persistence.ConfirmEdit(0, 2, "character_c"));
            AssertSaved(persistence.ConfirmEdit(0, 0, "character_c"));
            AssertSaved(persistence.ConfirmEdit(1, 0, "character_a"));
            AssertSaved(persistence.ConfirmEdit(1, 1, "character_b"));
            AssertSaved(persistence.ConfirmEdit(1, 2, "character_c"));
            AssertSaved(persistence.ConfirmEdit(1, 3, "character_b"));
            AssertSaved(persistence.SetActivePreset(1));

            BattlePartyResolver resolver = CreateResolver(
                Definition("character_a", ElementType.Fire),
                Definition("character_b", ElementType.Water),
                Definition("character_c", ElementType.Grass));
            BattlePartyResolutionResult beforeReload = resolver.Resolve(
                first.GetCurrentProfileSnapshot());

            SaveService second = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(90));
            Assert.That(second.LoadOrCreate("ignored").Status,
                Is.EqualTo(SaveLoadStatus.LoadedMain));
            ProfileSaveData reloaded = second.GetCurrentProfileSnapshot();
            BattlePartyResolutionResult afterReload =
                resolver.Resolve(reloaded);

            Assert.That(reloaded.Party.ActivePresetIndex, Is.EqualTo(1));
            Assert.That(
                reloaded.Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(new[]
                {
                    "character_c", "character_b", "character_a", "", ""
                }));
            Assert.That(
                reloaded.Party.Presets[1].CharacterSlotIds,
                Is.EqualTo(new[]
                {
                    "character_a", "", "character_c", "character_b", ""
                }));
            for (int presetIndex = 2; presetIndex < 5; presetIndex++)
            {
                Assert.That(
                    reloaded.Party.Presets[presetIndex].CharacterSlotIds,
                    Is.EqualTo(new[] { "", "", "", "", "" }));
            }

            AssertResolvedParty(beforeReload);
            AssertResolvedParty(afterReload);
        }

        private (SaveRepository Repository, SavePaths Paths) CreateRepository()
        {
            string root = Path.Combine(Path.GetTempPath(), "valor_chronicle_service_tests", Guid.NewGuid().ToString("N"));
            roots.Add(root);
            var paths = new SavePaths(root);
            return (new SaveRepository(paths), paths);
        }

        private static CharacterSaveData Character(string id, int level)
        {
            return new CharacterSaveData
            {
                CharacterId = id,
                Level = level
            };
        }

        private CharacterDefinition Definition(
            string id,
            ElementType element)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("element").enumValueIndex = (int)element;
            serialized.FindProperty("level1Hp").intValue = 100;
            serialized.FindProperty("level1Attack").intValue = 10;
            serialized.FindProperty("level100Hp").intValue = 1000;
            serialized.FindProperty("level100Attack").intValue = 100;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private BattlePartyResolver CreateResolver(
            params CharacterDefinition[] definitions)
        {
            var database = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            createdObjects.Add(database);
            var serialized = new SerializedObject(database);
            SerializedProperty characters =
                serialized.FindProperty("characters");
            characters.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++)
            {
                characters.GetArrayElementAtIndex(index)
                    .objectReferenceValue = definitions[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            database.Initialize();
            return new BattlePartyResolver(database);
        }

        private static void AssertSaved(
            PartyPersistenceOperationResult result)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(PartyPersistenceStatus.ChangedAndSaved));
        }

        private static void AssertResolvedParty(
            BattlePartyResolutionResult result)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(BattlePartyResolutionStatus.Success));
            Assert.That(
                result.Members.Select(member => member.CharacterId),
                Is.EqualTo(new[]
                {
                    "character_a", "character_c", "character_b"
                }));
            Assert.That(
                result.Members.Select(member => member.PartySlotIndex),
                Is.EqualTo(new[] { 0, 2, 3 }));
            Assert.That(
                result.Members.Select(member => member.Level),
                Is.EqualTo(new[] { 11, 33, 22 }));
        }
    }
}
