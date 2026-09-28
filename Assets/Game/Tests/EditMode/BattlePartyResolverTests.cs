using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class BattlePartyResolverTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Resolve_UsesOnlyActivePresetAndPreservesSlotOrderAndData()
        {
            CharacterDefinition a = Definition("a", ElementType.Water);
            CharacterDefinition b = Definition("b", ElementType.Water);
            CharacterDefinition c = Definition("c", ElementType.Water);
            CharacterDefinition inactive = Definition(
                "inactive",
                ElementType.Fire);
            ProfileSaveData profile = Profile(
                activePresetIndex: 3,
                activeSlots: Slots("a", "", "b", "c", ""),
                Character("a", level: 11, awakening: 1),
                Character("b", level: 22, awakening: 2),
                Character("c", level: 33, awakening: 3),
                Character("inactive", level: 99, awakening: 6));
            profile.Party.Presets[0].CharacterSlotIds = Slots(
                "inactive",
                "",
                "",
                "",
                "");

            BattlePartyResolutionResult result = Resolver(
                a,
                b,
                c,
                inactive).Resolve(profile);

            Assert.That(
                result.Status,
                Is.EqualTo(BattlePartyResolutionStatus.Success));
            Assert.That(
                result.Members.Select(member => member.CharacterId),
                Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(
                result.Members.Select(member => member.PartySlotIndex),
                Is.EqualTo(new[] { 0, 2, 3 }));
            Assert.That(
                result.Members.Select(member => member.Level),
                Is.EqualTo(new[] { 11, 22, 33 }));
            Assert.That(
                result.Members.Select(member => member.Awakening),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(result.Members[0].CharacterDefinition, Is.SameAs(a));
            Assert.That(result.Members[1].CharacterDefinition, Is.SameAs(b));
            Assert.That(result.Members[2].CharacterDefinition, Is.SameAs(c));
            Assert.That(
                result.Members.Any(
                    member => member.CharacterId == "inactive"),
                Is.False);
        }

        [Test]
        public void Resolve_PreservesLeadingAndMiddleEmptySlotIndices()
        {
            ProfileSaveData profile = Profile(
                activePresetIndex: 0,
                activeSlots: Slots("", "a", "", "", "b"),
                Character("a", 1, 0),
                Character("b", 1, 0));

            BattlePartyResolutionResult result = Resolver(
                Definition("a", ElementType.Light),
                Definition("b", ElementType.Light)).Resolve(profile);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Members.Select(member => member.PartySlotIndex),
                Is.EqualTo(new[] { 1, 4 }));
        }

        [Test]
        public void Resolve_EmptyActivePresetReturnsEmptyParty()
        {
            BattlePartyResolutionResult result = Resolver().Resolve(
                Profile(
                    activePresetIndex: 0,
                    activeSlots: Slots("", "", "", "", "")));

            Assert.That(
                result.Status,
                Is.EqualTo(BattlePartyResolutionStatus.EmptyParty));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Members, Is.Empty);
        }

        [Test]
        public void Resolve_UnownedActiveCharacterReturnsInvalidParty()
        {
            BattlePartyResolutionResult result = Resolver(
                Definition("a", ElementType.Fire)).Resolve(
                Profile(
                    activePresetIndex: 0,
                    activeSlots: Slots("a", "", "", "", "")));

            AssertInvalid(result, "not owned");
        }

        [Test]
        public void Resolve_MissingCharacterDefinitionReturnsInvalidParty()
        {
            BattlePartyResolutionResult result = Resolver().Resolve(
                Profile(
                    activePresetIndex: 0,
                    activeSlots: Slots("a", "", "", "", ""),
                    Character("a", 1, 0)));

            AssertInvalid(result, "CharacterDefinition");
        }

        [Test]
        public void Resolve_DuplicateCharacterInActivePresetReturnsInvalidParty()
        {
            BattlePartyResolutionResult result = Resolver(
                Definition("a", ElementType.Fire)).Resolve(
                Profile(
                    activePresetIndex: 0,
                    activeSlots: Slots("a", "", "a", "", ""),
                    Character("a", 1, 0)));

            AssertInvalid(result, "duplicate");
        }

        [Test]
        public void Resolve_InvalidOwnedCharacterProgressReturnsInvalidParty()
        {
            BattlePartyResolutionResult result = Resolver(
                Definition("a", ElementType.Fire)).Resolve(
                Profile(
                    activePresetIndex: 0,
                    activeSlots: Slots("a", "", "", "", ""),
                    Character("a", level: 0, awakening: 7)));

            AssertInvalid(result, "progress values");
        }

        [TestCase(-1)]
        [TestCase(SaveRules.PartyPresetCount)]
        public void Resolve_InvalidActivePresetIndexReturnsInvalidParty(
            int activePresetIndex)
        {
            BattlePartyResolutionResult result = Resolver().Resolve(
                Profile(
                    activePresetIndex,
                    Slots("", "", "", "", "")));

            AssertInvalid(result, "ActivePresetIndex");
        }

        [Test]
        public void Resolve_DoesNotMutateProfileAndReturnsReadOnlyMembers()
        {
            CharacterSaveData character = Character("a", 17, 4);
            ProfileSaveData profile = Profile(
                activePresetIndex: 2,
                activeSlots: Slots("", "a", "", "", ""),
                character);
            PartySaveData originalParty = profile.Party;
            PartyPresetSaveData originalPreset = profile.Party.Presets[2];
            string[] originalSlots = originalPreset.CharacterSlotIds.ToArray();

            BattlePartyResolutionResult result = Resolver(
                Definition("a", ElementType.Dark)).Resolve(profile);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(profile.Party, Is.SameAs(originalParty));
            Assert.That(profile.Party.Presets[2], Is.SameAs(originalPreset));
            Assert.That(
                profile.Party.Presets[2].CharacterSlotIds,
                Is.EqualTo(originalSlots));
            Assert.That(profile.Characters[0], Is.SameAs(character));
            Assert.That(character.Level, Is.EqualTo(17));
            Assert.That(character.Awakening, Is.EqualTo(4));

            var members = (IList<BattlePartyMemberInput>)result.Members;
            Assert.That(members.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => members.RemoveAt(0));
        }

        [Test]
        public void Resolve_InvalidProfileStructuresReturnInvalidParty()
        {
            BattlePartyResolver resolver = Resolver();

            AssertInvalid(resolver.Resolve(null), "Profile snapshot");

            ProfileSaveData missingParty = Profile(
                0,
                Slots("", "", "", "", ""));
            missingParty.Party = null;
            AssertInvalid(resolver.Resolve(missingParty), "Party data");

            ProfileSaveData invalidSlots = Profile(
                0,
                Slots("", "", "", "", ""));
            invalidSlots.Party.Presets[0].CharacterSlotIds =
                Slots("", "");
            AssertInvalid(resolver.Resolve(invalidSlots), "exactly 5 slots");
        }

        [Test]
        public void Resolve_UninitializedDefinitionDatabaseReturnsInvalidParty()
        {
            var database = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            createdObjects.Add(database);
            var resolver = new BattlePartyResolver(database);

            BattlePartyResolutionResult result = resolver.Resolve(
                Profile(0, Slots("", "", "", "", "")));

            AssertInvalid(result, "not initialized");
        }

        private BattlePartyResolver Resolver(
            params CharacterDefinition[] definitions)
        {
            var database = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            createdObjects.Add(database);
            SetArray(database, "characters", definitions);
            database.Initialize();
            return new BattlePartyResolver(database);
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

        private static ProfileSaveData Profile(
            int activePresetIndex,
            List<string> activeSlots,
            params CharacterSaveData[] characters)
        {
            var presets = new List<PartyPresetSaveData>(
                SaveRules.PartyPresetCount);
            for (int index = 0;
                index < SaveRules.PartyPresetCount;
                index++)
            {
                presets.Add(new PartyPresetSaveData
                {
                    PresetId = $"party_{index + 1}",
                    CharacterSlotIds = Slots("", "", "", "", "")
                });
            }

            if (activePresetIndex >= 0
                && activePresetIndex < presets.Count)
            {
                presets[activePresetIndex].CharacterSlotIds = activeSlots;
            }

            return new ProfileSaveData
            {
                Characters = new List<CharacterSaveData>(characters),
                Party = new PartySaveData
                {
                    ActivePresetIndex = activePresetIndex,
                    Presets = presets,
                    LastBossId = string.Empty,
                    LastDifficultyId = string.Empty
                }
            };
        }

        private static CharacterSaveData Character(
            string id,
            int level,
            int awakening)
        {
            return new CharacterSaveData
            {
                CharacterId = id,
                Level = level,
                Awakening = awakening
            };
        }

        private static List<string> Slots(params string[] characterIds)
        {
            return new List<string>(characterIds);
        }

        private static void AssertInvalid(
            BattlePartyResolutionResult result,
            string errorFragment)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(BattlePartyResolutionStatus.InvalidParty));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Members, Is.Empty);
            Assert.That(result.ErrorMessage, Does.Contain(errorFragment));
        }

        private static void SetArray<T>(
            UnityEngine.Object target,
            string propertyName,
            T[] values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
