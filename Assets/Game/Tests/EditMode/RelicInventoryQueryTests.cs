using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Relics;
using ValorChronicle.Relics.Inventory;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicInventoryQueryTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private DefinitionDatabase database;
        private LocalizationService localization;
        private RelicInventoryQuery query;

        [SetUp]
        public void SetUp()
        {
            database = CreateDatabase(
                Definition("relic_sword", "relic.sword"),
                Definition("relic_bark", "relic.bark"),
                Definition("relic_orb", "relic.orb"),
                Definition("relic_alpha", "relic.alpha"),
                Definition("relic_beta", "relic.beta"));
            localization = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(
                    "relics.tsv",
                    "key\ten\tko\n"
                    + "relic.sword\tSword\t검\n"
                    + "relic.bark\tBark\t수피\n"
                    + "relic.orb\tOrb\t구슬\n"
                    + "relic.alpha\tZulu\t가\n"
                    + "relic.beta\tAlpha\t나")
            });
            query = new RelicInventoryQuery(
                database,
                localization,
                new RelicEquipEditor());
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(
                    createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void BuildEntries_IncludesEveryOwnedInstanceOnce()
        {
            ProfileSaveData profile = Profile(
                Relic("alpha", "relic_alpha", 0),
                Relic("beta", "relic_beta", 2),
                Relic("sword", "relic_sword", 1));
            profile.Characters.Add(Character("character_a"));

            IReadOnlyList<RelicInventoryEntry> entries =
                query.BuildEntries(profile, "character_a", 0, "en");

            Assert.That(entries.Count, Is.EqualTo(3));
            Assert.That(
                entries.Select(entry => entry.InstanceId),
                Is.EquivalentTo(new[] { "alpha", "beta", "sword" }));
            Assert.That(
                entries.Select(entry => entry.InstanceId).Distinct().Count(),
                Is.EqualTo(3));
        }

        [Test]
        public void BuildEntries_DoesNotMutateOrReorderProfile()
        {
            ProfileSaveData profile = Profile(
                Relic("first", "relic_alpha", 0),
                Relic("second", "relic_beta", 5),
                Relic("third", "relic_sword", 3));
            profile.Characters.Add(Character("character_a"));
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);

            query.BuildEntries(profile, "character_a", 0, "en");

            Assert.That(
                profile.RelicInstances.Select(relic => relic.InstanceId),
                Is.EqualTo(new[] { "first", "second", "third" }));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [Test]
        public void BuildEntries_SortsByAwakeningDescending()
        {
            ProfileSaveData profile = Profile();
            for (int awakening = 0; awakening <= 5; awakening++)
            {
                profile.RelicInstances.Add(Relic(
                    $"relic_{awakening}",
                    "relic_sword",
                    awakening));
            }
            profile.Characters.Add(Character("character_a"));

            IReadOnlyList<RelicInventoryEntry> entries =
                query.BuildEntries(profile, "character_a", 0, "en");

            Assert.That(
                entries.Select(entry => entry.Awakening),
                Is.EqualTo(new[] { 5, 4, 3, 2, 1, 0 }));
        }

        [Test]
        public void BuildEntries_UsesKoreanDisplayNameOrder()
        {
            ProfileSaveData profile = Profile(
                Relic("beta", "relic_beta", 2),
                Relic("alpha", "relic_alpha", 2));
            profile.Characters.Add(Character("character_a"));

            IReadOnlyList<RelicInventoryEntry> entries =
                query.BuildEntries(profile, "character_a", 0, "ko");

            Assert.That(
                entries.Select(entry => entry.DisplayName),
                Is.EqualTo(new[] { "가", "나" }));
            Assert.That(localization.CurrentLocale, Is.EqualTo("en"));
        }

        [Test]
        public void BuildEntries_UsesEnglishDisplayNameOrder()
        {
            ProfileSaveData profile = Profile(
                Relic("alpha", "relic_alpha", 2),
                Relic("beta", "relic_beta", 2));
            profile.Characters.Add(Character("character_a"));

            IReadOnlyList<RelicInventoryEntry> entries =
                query.BuildEntries(profile, "character_a", 0, "en");

            Assert.That(
                entries.Select(entry => entry.DisplayName),
                Is.EqualTo(new[] { "Alpha", "Zulu" }));
        }

        [Test]
        public void BuildEntries_UsesOrdinalInstanceIdTieBreak()
        {
            ProfileSaveData profile = Profile(
                Relic("instance_z", "relic_sword", 2),
                Relic("instance_a", "relic_sword", 2));
            profile.Characters.Add(Character("character_a"));

            IReadOnlyList<RelicInventoryEntry> entries =
                query.BuildEntries(profile, "character_a", 0, "en");

            Assert.That(
                entries.Select(entry => entry.InstanceId),
                Is.EqualTo(new[] { "instance_a", "instance_z" }));
        }

        [Test]
        public void BuildEntries_OrderingIgnoresAvailabilityAndEquipment()
        {
            ProfileSaveData profile = Profile(
                Relic("alpha", "relic_alpha", 2),
                Relic("beta", "relic_beta", 2),
                Relic("sword", "relic_sword", 2));
            profile.Characters.Add(Character("character_a"));
            profile.Characters.Add(Character("character_b"));
            string[] before = query.BuildEntries(
                    profile, "character_a", 0, "en")
                .Select(entry => entry.InstanceId).ToArray();
            profile.Characters[1].EquippedRelicInstanceIds[0] = "beta";

            string[] after = query.BuildEntries(
                    profile, "character_a", 0, "en")
                .Select(entry => entry.InstanceId).ToArray();

            Assert.That(after, Is.EqualTo(before));
            Assert.That(
                query.BuildEntries(profile, "character_a", 0, "en")
                    .Single(entry => entry.InstanceId == "beta")
                    .IsInteractable,
                Is.False);
        }

        [Test]
        public void BuildEntries_ProjectsEquipAvailabilityAndDimStates()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_target", "relic_sword", 0),
                Relic("sword_replacement", "relic_sword", 5),
                Relic("bark_equipped", "relic_bark", 1),
                Relic("bark_candidate", "relic_bark", 4),
                Relic("orb_other", "relic_orb", 2),
                Relic("plain", "relic_alpha", 3, true, true));
            profile.Characters.Add(Character(
                "character_a",
                "sword_target",
                "bark_equipped"));
            profile.Characters.Add(Character(
                "character_b",
                "orb_other"));

            IReadOnlyList<RelicInventoryEntry> entries =
                query.BuildEntries(profile, "character_a", 0, "en");

            AssertEntry(
                entries, "orb_other", true, false, true,
                RelicEquipAvailabilityReason.EquippedByOtherCharacter);
            AssertEntry(
                entries, "bark_equipped", true, false, true,
                RelicEquipAvailabilityReason.EquippedInOtherSlot);
            AssertEntry(
                entries, "bark_candidate", true, false, false,
                RelicEquipAvailabilityReason
                    .DuplicateDefinitionInOtherSlot);
            AssertEntry(
                entries, "sword_replacement", true, true, false,
                RelicEquipAvailabilityReason.Allowed);

            RelicInventoryEntry target = entries.Single(
                entry => entry.InstanceId == "sword_target");
            Assert.That(target.IsEquippedInTargetSlot, Is.True);
            Assert.That(target.IsEquipped, Is.True);
            Assert.That(target.EquippedCharacterId,
                Is.EqualTo("character_a"));
            Assert.That(target.EquippedSlotIndex, Is.Zero);
            Assert.That(target.IsDimmed, Is.True);
            Assert.That(target.IsInteractable, Is.True);
            Assert.That(target.AvailabilityReason,
                Is.EqualTo(RelicEquipAvailabilityReason.Allowed));

            RelicInventoryEntry plain = entries.Single(
                entry => entry.InstanceId == "plain");
            Assert.That(plain.IsDimmed, Is.False);
            Assert.That(plain.IsInteractable, Is.True);
            Assert.That(plain.IsLocked, Is.True);
            Assert.That(plain.IsNew, Is.True);

            RelicInventoryEntry other = entries.Single(
                entry => entry.InstanceId == "orb_other");
            Assert.That(other.EquippedCharacterId,
                Is.EqualTo("character_b"));
            Assert.That(other.EquippedSlotIndex, Is.Zero);
        }

        [Test]
        public void BuildEntries_OtherCharacterSameDefinitionCopyIsAllowed()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_other", "relic_sword", 5),
                Relic("sword_candidate", "relic_sword", 0));
            profile.Characters.Add(Character("character_a"));
            profile.Characters.Add(Character(
                "character_b",
                "sword_other"));

            RelicInventoryEntry candidate = query.BuildEntries(
                    profile, "character_a", 0, "en")
                .Single(entry => entry.InstanceId == "sword_candidate");

            Assert.That(candidate.IsInteractable, Is.True);
            Assert.That(candidate.IsDimmed, Is.False);
            Assert.That(candidate.AvailabilityReason,
                Is.EqualTo(RelicEquipAvailabilityReason.Allowed));
        }

        [Test]
        public void BuildEntries_IgnoresLegacyEquipFields()
        {
            RelicInstanceSaveData relic = Relic(
                "sword", "relic_sword", 0);
            relic.EquippedCharacterId = "fake_character";
            relic.EquippedSlotIndex = 3;
            ProfileSaveData profile = Profile(relic);
            profile.Characters.Add(Character("character_a"));

            RelicInventoryEntry entry = query.BuildEntries(
                    profile, "character_a", 0, "en")
                .Single();

            Assert.That(entry.IsEquipped, Is.False);
            Assert.That(entry.EquippedCharacterId, Is.Empty);
            Assert.That(entry.EquippedSlotIndex,
                Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
            Assert.That(entry.IsInteractable, Is.True);
        }

        [Test]
        public void BuildEntries_MalformedDataDoesNotThrowOrRepairProfile()
        {
            ProfileSaveData profile = Profile(
                null,
                Relic("", "relic_sword", 0),
                Relic("missing_definition", "missing", 0),
                Relic("valid", "relic_sword", 0));
            profile.Characters = null;
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);
            IReadOnlyList<RelicInventoryEntry> entries = null;

            Assert.DoesNotThrow(() => entries = query.BuildEntries(
                profile, "missing", 8, "en"));

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].InstanceId, Is.EqualTo("valid"));
            Assert.That(entries[0].IsInteractable, Is.False);
            Assert.That(entries[0].IsDimmed, Is.True);
            Assert.That(entries[0].AvailabilityReason,
                Is.EqualTo(RelicEquipAvailabilityReason.CharacterNotFound));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
            Assert.That(
                query.BuildEntries(null, "missing", 0, "en"),
                Is.Empty);
        }

        [Test]
        public void ExplicitLocaleLookupUsesFallbackWithoutChangingState()
        {
            LocalizationService service = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(
                    "fallback.tsv",
                    "key\ten\tko\nname\tEnglish\t")
            });

            Assert.That(service.GetText("name", "ko"),
                Is.EqualTo("English"));
            Assert.That(service.CurrentLocale, Is.EqualTo("en"));
            Assert.That(
                LocalizationCultureResolver.Resolve("ko").Name,
                Is.EqualTo("ko-KR"));
            Assert.That(
                LocalizationCultureResolver.Resolve("en").Name,
                Is.EqualTo("en-US"));
        }

        private RelicDefinition Definition(string id, string nameKey)
        {
            RelicDefinition definition =
                ScriptableObject.CreateInstance<RelicDefinition>();
            createdObjects.Add(definition);
            SetField(definition, typeof(GameDefinition), "id", id);
            SetField(
                definition,
                typeof(RelicDefinition),
                "displayNameKey",
                nameKey);
            return definition;
        }

        private DefinitionDatabase CreateDatabase(
            params RelicDefinition[] relics)
        {
            DefinitionDatabase result =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(result);
            SetField(
                result,
                typeof(DefinitionDatabase),
                "relics",
                relics);
            result.Initialize();
            return result;
        }

        private static void SetField(
            object target,
            Type ownerType,
            string fieldName,
            object value)
        {
            FieldInfo field = ownerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static ProfileSaveData Profile(
            params RelicInstanceSaveData[] relics)
        {
            ProfileSaveData profile = new NewProfileFactory().Create(
                "profile",
                1);
            profile.RelicInstances.AddRange(relics);
            return profile;
        }

        private static RelicInstanceSaveData Relic(
            string instanceId,
            string definitionId,
            int awakening,
            bool isLocked = false,
            bool isNew = false)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = definitionId,
                Awakening = awakening,
                IsLocked = isLocked,
                IsNew = isNew
            };
        }

        private static CharacterSaveData Character(
            string characterId,
            params string[] relicInstanceIds)
        {
            var character = new CharacterSaveData
            {
                CharacterId = characterId,
                Level = 1
            };
            for (int index = 0;
                index < relicInstanceIds.Length;
                index++)
            {
                character.EquippedRelicInstanceIds[index] =
                    relicInstanceIds[index];
            }

            return character;
        }

        private static void AssertEntry(
            IReadOnlyList<RelicInventoryEntry> entries,
            string instanceId,
            bool expectedDimmed,
            bool expectedInteractable,
            bool expectedEquipped,
            RelicEquipAvailabilityReason expectedReason)
        {
            RelicInventoryEntry entry = entries.Single(
                candidate => candidate.InstanceId == instanceId);
            Assert.That(entry.IsDimmed, Is.EqualTo(expectedDimmed));
            Assert.That(entry.IsInteractable,
                Is.EqualTo(expectedInteractable));
            Assert.That(entry.IsEquipped, Is.EqualTo(expectedEquipped));
            Assert.That(entry.AvailabilityReason,
                Is.EqualTo(expectedReason));
        }
    }
}
