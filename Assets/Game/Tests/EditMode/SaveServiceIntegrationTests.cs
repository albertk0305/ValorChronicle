using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Party;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Persistence;
using ValorChronicle.Relics;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Relics.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
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

        [Test]
        public void RealRepository_SwordEquipReloadsIntoBattleAttackSnapshot()
        {
            (SaveRepository repository, _) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(10, 20, 30));
            Assert.That(first.LoadOrCreate("profile_relic_e2e").IsSuccess,
                Is.True);
            Assert.That(first.ExecuteTransaction(profile =>
            {
                profile.Characters.Add(Character("character_a", 1));
                profile.RelicInstances.Add(new RelicInstanceSaveData
                {
                    InstanceId = "sword_instance",
                    RelicDefinitionId = "relic_victors_broken_sword",
                    Awakening = 5
                });
                profile.Party.Presets[0].CharacterSlotIds[0] =
                    "character_a";
            }).IsSuccess, Is.True);
            var equipSession = new RelicEquipEditSession(
                new RelicEquipEditor(),
                first.GetCurrentProfileSnapshot(),
                "character_a",
                0);
            Assert.That(
                equipSession.TrySetPreview("sword_instance").CanSelect,
                Is.True);
            var equipPersistence = new RelicEquipPersistenceService(
                first,
                new RelicEquipEditor());
            Assert.That(
                equipPersistence.Execute(
                    equipSession.CreateEquipCommand()).WasChanged,
                Is.True);

            SaveService second = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(40));
            Assert.That(second.LoadOrCreate("ignored").Status,
                Is.EqualTo(SaveLoadStatus.LoadedMain));
            CharacterDefinition characterDefinition = Definition(
                "character_a",
                ElementType.Fire);
            RelicDefinition sword = Relic(
                "relic_victors_broken_sword",
                RelicEffectType.AttackIncrease);
            BattlePartyResolutionResult result = CreateResolver(
                new[] { characterDefinition },
                new[] { sword }).Resolve(
                    second.GetCurrentProfileSnapshot());
            Assert.That(result.IsSuccess, Is.True);
            BattlePartyMemberInput member = result.Members.Single();
            ResolvedCharacterBuild build =
                CharacterBuildResolverFactory.CreateDefault().Resolve(
                    member.CharacterDefinition,
                    member.Level,
                    member.Awakening,
                    member.RelicStatBonuses);
            CharacterBattleState state = CharacterBattleStateFactory.Create(
                member.CharacterDefinition,
                build,
                member.PartySlotIndex,
                member.RelicStatBonuses);

            Assert.That(build.Attack, Is.EqualTo(10));
            Assert.That(state.Attack, Is.EqualTo(10d));
            Assert.That(state.PermanentAttackIncreaseRate,
                Is.EqualTo(0.10d));
        }

        [Test]
        public void RealRepository_V2RelicEquipsMigrateAndRoundTripThroughLoadPipeline()
        {
            (SaveRepository repository, SavePaths paths) = CreateRepository();
            repository.EnsureRootDirectory();
            ProfileSaveData legacy = new NewProfileFactory().Create(
                "profile_v2_relics",
                1);
            legacy.SaveVersion = 2;
            legacy.Characters.Add(Character("character_a", 1));
            legacy.Characters.Add(Character("character_b", 1));
            legacy.RelicInstances.Add(LegacyRelic(
                "sword_a",
                "relic_victors_broken_sword",
                "character_a",
                0));
            legacy.RelicInstances.Add(LegacyRelic(
                "bark_b",
                "relic_world_tree_bark",
                "character_a",
                2));
            legacy.RelicInstances.Add(LegacyRelic(
                "sword_c",
                "relic_victors_broken_sword",
                "character_b",
                1));
            File.WriteAllText(
                paths.MainPath,
                new NewtonsoftJsonSaveSerializer().Serialize(legacy));

            SaveService migratedService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(10));
            SaveLoadResult migrated = migratedService.LoadOrCreate(
                "ignored");

            Assert.That(
                migrated.Status,
                Is.EqualTo(SaveLoadStatus.LoadedAndRepairedMain));
            AssertMigratedRelicSlots(
                migratedService.GetCurrentProfileSnapshot());

            SaveService reloadedService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(20));
            SaveLoadResult reloaded = reloadedService.LoadOrCreate(
                "ignored");

            Assert.That(
                reloaded.Status,
                Is.EqualTo(SaveLoadStatus.LoadedMain));
            AssertMigratedRelicSlots(
                reloadedService.GetCurrentProfileSnapshot());
        }

        [Test]
        public void RealRepository_AcquireEquipAwakenOverflowAndReloadPreservesCanonicalState()
        {
            (SaveRepository repository, _) = CreateRepository();
            SaveService service = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(10));
            Assert.That(service.LoadOrCreate("profile_relic_flow").IsSuccess,
                Is.True);
            Assert.That(service.ExecuteTransaction(profile =>
            {
                profile.Characters.Add(Character("character_a", 1));
            }).IsSuccess, Is.True);

            var acquisition = new RelicAcquisitionService(
                service,
                new FakeSaveContentCatalog(),
                new RelicAcquisitionProfileUpdaterTests
                    .SequentialIdProvider(
                        "sword_primary",
                        "sword_overflow",
                        "bark_primary"));
            Assert.That(
                acquisition.GrantRelic(
                    "relic_victors_broken_sword").IsSuccess,
                Is.True);
            Assert.That(service.ExecuteTransaction(profile =>
            {
                RelicInstanceSaveData sword = profile.RelicInstances.Single(
                    relic => relic.InstanceId == "sword_primary");
                sword.IsLocked = true;
                sword.IsNew = false;
            }).IsSuccess, Is.True);

            var equipPersistence = new RelicEquipPersistenceService(
                service,
                new RelicEquipEditor());
            AssertEquipped(
                service,
                equipPersistence,
                slotIndex: 0,
                "sword_primary");

            for (int awakening = 1;
                awakening <= SaveRules.RelicMaxAwakening;
                awakening++)
            {
                RelicAcquisitionPersistenceOperationResult result =
                    acquisition.GrantRelic(
                        "relic_victors_broken_sword");
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.AcquisitionResult.AffectedInstanceId,
                    Is.EqualTo("sword_primary"));
                Assert.That(result.AcquisitionResult.NewAwakening,
                    Is.EqualTo(awakening));
                Assert.That(
                    result.ProfileSnapshot.Characters[0]
                        .EquippedRelicInstanceIds[0],
                    Is.EqualTo("sword_primary"));
            }

            RelicAcquisitionPersistenceOperationResult overflow =
                acquisition.GrantRelic(
                    "relic_victors_broken_sword");
            Assert.That(overflow.IsSuccess, Is.True);
            Assert.That(
                overflow.AcquisitionResult.Kind,
                Is.EqualTo(RelicAcquisitionKind
                    .NewInstanceCreatedAfterMaxAwakening));
            Assert.That(overflow.AcquisitionResult.AffectedInstanceId,
                Is.EqualTo("sword_overflow"));
            Assert.That(
                overflow.ProfileSnapshot.Characters[0]
                    .EquippedRelicInstanceIds[0],
                Is.EqualTo("sword_primary"));

            Assert.That(
                acquisition.GrantRelic("relic_world_tree_bark")
                    .IsSuccess,
                Is.True);
            RelicAcquisitionPersistenceOperationResult barkAwakened =
                acquisition.GrantRelic("relic_world_tree_bark");
            Assert.That(barkAwakened.IsSuccess, Is.True);
            Assert.That(barkAwakened.AcquisitionResult.AffectedInstanceId,
                Is.EqualTo("bark_primary"));
            Assert.That(barkAwakened.AcquisitionResult.NewAwakening,
                Is.EqualTo(1));
            AssertEquipped(
                service,
                equipPersistence,
                slotIndex: 2,
                "bark_primary");

            SaveService reloadedService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(20));
            Assert.That(
                reloadedService.LoadOrCreate("ignored").Status,
                Is.EqualTo(SaveLoadStatus.LoadedMain));
            ProfileSaveData reloaded =
                reloadedService.GetCurrentProfileSnapshot();
            CharacterSaveData character = reloaded.Characters.Single(
                value => value.CharacterId == "character_a");
            Assert.That(
                character.EquippedRelicInstanceIds,
                Is.EqualTo(new[]
                {
                    "sword_primary", "", "bark_primary", ""
                }));
            RelicInstanceSaveData primary = reloaded.RelicInstances.Single(
                relic => relic.InstanceId == "sword_primary");
            Assert.That(primary.RelicDefinitionId,
                Is.EqualTo("relic_victors_broken_sword"));
            Assert.That(primary.Awakening,
                Is.EqualTo(SaveRules.RelicMaxAwakening));
            Assert.That(primary.IsLocked, Is.True);
            Assert.That(primary.IsNew, Is.False);
            Assert.That(reloaded.RelicInstances.Single(
                    relic => relic.InstanceId == "sword_overflow")
                .Awakening,
                Is.Zero);
            Assert.That(reloaded.RelicInstances.Single(
                    relic => relic.InstanceId == "bark_primary")
                .Awakening,
                Is.EqualTo(1));
            Assert.That(reloaded.RelicInstances.All(
                    relic => relic.EquippedCharacterId == SaveRules.EmptyId
                        && relic.EquippedSlotIndex
                            == SaveRules.UnequippedRelicSlotIndex),
                Is.True);
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

        private static RelicInstanceSaveData LegacyRelic(
            string instanceId,
            string definitionId,
            string characterId,
            int slotIndex)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = definitionId,
                Awakening = 0,
                EquippedCharacterId = characterId,
                EquippedSlotIndex = slotIndex
            };
        }

        private static void AssertMigratedRelicSlots(
            ProfileSaveData profile)
        {
            Assert.That(profile.SaveVersion,
                Is.EqualTo(SaveRules.CurrentSaveVersion));
            Assert.That(profile.RelicInstances, Has.Count.EqualTo(3));
            Assert.That(
                profile.Characters.Single(
                    character => character.CharacterId == "character_a")
                    .EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "sword_a", "", "bark_b", "" }));
            Assert.That(
                profile.Characters.Single(
                    character => character.CharacterId == "character_b")
                    .EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "", "sword_c", "", "" }));
            Assert.That(profile.RelicInstances.All(
                    relic => relic.EquippedCharacterId == SaveRules.EmptyId
                        && relic.EquippedSlotIndex
                            == SaveRules.UnequippedRelicSlotIndex),
                Is.True);
        }

        private static void AssertEquipped(
            SaveService service,
            RelicEquipPersistenceService persistence,
            int slotIndex,
            string instanceId)
        {
            var session = new RelicEquipEditSession(
                new RelicEquipEditor(),
                service.GetCurrentProfileSnapshot(),
                "character_a",
                slotIndex);
            Assert.That(session.TrySetPreview(instanceId).CanSelect,
                Is.True);
            Assert.That(
                persistence.Execute(session.CreateEquipCommand()).IsSuccess,
                Is.True);
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
            return CreateResolver(
                definitions,
                Array.Empty<RelicDefinition>());
        }

        private BattlePartyResolver CreateResolver(
            CharacterDefinition[] definitions,
            RelicDefinition[] relicDefinitions)
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
            SerializedProperty relics = serialized.FindProperty("relics");
            relics.arraySize = relicDefinitions.Length;
            for (int index = 0; index < relicDefinitions.Length; index++)
            {
                relics.GetArrayElementAtIndex(index).objectReferenceValue =
                    relicDefinitions[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            database.Initialize();
            return new BattlePartyResolver(database);
        }

        private RelicDefinition Relic(
            string id,
            RelicEffectType effectType)
        {
            var definition = ScriptableObject.CreateInstance<RelicDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("effectType").enumValueIndex =
                (int)effectType;
            SerializedProperty rates = serialized.FindProperty("effectRates");
            rates.arraySize = RelicDefinition.AwakeningValueCount;
            for (int index = 0; index < rates.arraySize; index++)
            {
                rates.GetArrayElementAtIndex(index).doubleValue =
                    0.05d + index * 0.01d;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
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
