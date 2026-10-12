using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Relics;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicEquipEditorTests
    {
        private RelicEquipEditor editor;

        [SetUp]
        public void SetUp()
        {
            editor = new RelicEquipEditor();
        }

        [Test]
        public void Evaluate_UnequippedInstanceIntoEmptySlot_IsAllowed()
        {
            ProfileSaveData profile = CreateProfile();
            profile.RelicInstances.Add(CreateRelic(
                "orb_a", "relic_orb", 0));

            RelicEquipAvailability result = editor.Evaluate(
                profile, "character_a", 2, "orb_a");

            AssertAllowed(result);
            Assert.That(result.EquippedCharacterId, Is.Empty);
            Assert.That(
                result.EquippedSlotIndex,
                Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
        }

        [Test]
        public void Evaluate_InstanceEquippedByOtherCharacter_IsBlocked()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Characters[1].EquippedRelicInstanceIds[1] =
                "bark_b";

            RelicEquipAvailability result = editor.Evaluate(
                profile, "character_a", 2, "bark_b");

            AssertBlocked(
                result,
                RelicEquipAvailabilityReason.EquippedByOtherCharacter);
            Assert.That(
                result.EquippedCharacterId,
                Is.EqualTo("character_b"));
            Assert.That(result.EquippedSlotIndex, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_InstanceInOtherSlotOfSameCharacter_IsBlocked()
        {
            ProfileSaveData profile = CreateProfile();

            RelicEquipAvailability result = editor.Evaluate(
                profile, "character_a", 3, "bark_a");

            AssertBlocked(
                result,
                RelicEquipAvailabilityReason.EquippedInOtherSlot);
            Assert.That(result.EquippedSlotIndex, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_InstanceAlreadyInTargetSlot_IsAllowed()
        {
            ProfileSaveData profile = CreateProfile();

            RelicEquipAvailability result = editor.Evaluate(
                profile, "character_a", 0, "sword_a");

            AssertAllowed(result);
            Assert.That(
                result.EquippedCharacterId,
                Is.EqualTo("character_a"));
            Assert.That(result.EquippedSlotIndex, Is.Zero);
        }

        [Test]
        public void Evaluate_DuplicateDefinitionInOtherSlot_IsBlocked()
        {
            ProfileSaveData profile = CreateProfile();

            RelicEquipAvailability result = editor.Evaluate(
                profile, "character_a", 2, "sword_b");

            AssertBlocked(
                result,
                RelicEquipAvailabilityReason
                    .DuplicateDefinitionInOtherSlot);
        }

        [Test]
        public void Evaluate_SameDefinitionAsTargetReplacement_IsAllowed()
        {
            ProfileSaveData profile = CreateProfile();
            profile.RelicInstances[1].EquippedCharacterId =
                "character_b";
            profile.RelicInstances[1].EquippedSlotIndex = 3;

            RelicEquipAvailability result = editor.Evaluate(
                profile, "character_a", 0, "sword_b");

            AssertAllowed(result);
        }

        [Test]
        public void Evaluate_SameDefinitionOnOtherCharacterDoesNotBlock()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Characters[1].EquippedRelicInstanceIds[0] =
                "sword_a";
            profile.Characters[0].EquippedRelicInstanceIds[0] =
                SaveRules.EmptyId;

            RelicEquipAvailability result = editor.Evaluate(
                profile, "character_a", 0, "sword_b");

            AssertAllowed(result);
        }

        [Test]
        public void Evaluate_InvalidInputsReturnExplicitReasons()
        {
            ProfileSaveData profile = CreateProfile();

            AssertBlocked(
                editor.Evaluate(
                    profile, "character_a", 2, "missing"),
                RelicEquipAvailabilityReason.RelicInstanceNotFound);
            AssertBlocked(
                editor.Evaluate(
                    profile, "missing", 2, "bark_b"),
                RelicEquipAvailabilityReason.CharacterNotFound);
            AssertBlocked(
                editor.Evaluate(
                    profile, "character_a", 4, "bark_b"),
                RelicEquipAvailabilityReason.InvalidTargetSlot);
        }

        [Test]
        public void Equip_CalculatesOnlyTargetChangeWithoutMutatingProfile()
        {
            ProfileSaveData profile = CreateProfile();
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);

            RelicEquipEditResult result = editor.Equip(
                profile, "character_a", 0, "sword_b");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.OperationType,
                Is.EqualTo(RelicEquipEditOperationType.Equip));
            Assert.That(
                result.ResultingSlots,
                Is.EqualTo(new[]
                {
                    "sword_b", "bark_a", "", ""
                }));
            Assert.That(profile.RelicInstances, Has.Count.EqualTo(4));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [Test]
        public void Equip_FailureIsAtomicAndSameInstanceIsNoChange()
        {
            ProfileSaveData profile = CreateProfile();
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);

            RelicEquipEditResult failed = editor.Equip(
                profile, "character_a", 2, "sword_b");
            RelicEquipEditResult same = editor.Equip(
                profile, "character_a", 0, "sword_a");

            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(
                failed.OperationType,
                Is.EqualTo(RelicEquipEditOperationType.None));
            Assert.That(
                same.OperationType,
                Is.EqualTo(RelicEquipEditOperationType.NoChange));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [Test]
        public void Clear_ReturnsClearOrNoChangeWithoutMutatingProfile()
        {
            ProfileSaveData profile = CreateProfile();

            RelicEquipEditResult populated = editor.Clear(
                profile, "character_a", 0);
            RelicEquipEditResult empty = editor.Clear(
                profile, "character_a", 3);

            Assert.That(
                populated.OperationType,
                Is.EqualTo(RelicEquipEditOperationType.Clear));
            Assert.That(
                populated.ResultingSlots,
                Is.EqualTo(new[] { "", "bark_a", "", "" }));
            Assert.That(
                empty.OperationType,
                Is.EqualTo(RelicEquipEditOperationType.NoChange));
            Assert.That(
                profile.Characters[0].EquippedRelicInstanceIds[0],
                Is.EqualTo("sword_a"));
        }

        [Test]
        public void Index_IgnoresLegacyFieldsAndKeepsFirstMalformedDuplicate()
        {
            ProfileSaveData profile = CreateProfile();
            profile.RelicInstances[3].EquippedCharacterId =
                "character_b";
            profile.RelicInstances[3].EquippedSlotIndex = 3;
            profile.Characters[1].EquippedRelicInstanceIds[3] =
                "sword_a";
            var index = new RelicEquipIndex(profile);

            Assert.That(
                index.TryGetLocation(
                    "bark_b",
                    out RelicEquipLocation legacyOnly),
                Is.False);
            Assert.That(legacyOnly, Is.Null);
            Assert.That(
                index.TryGetLocation(
                    "sword_a",
                    out RelicEquipLocation duplicate),
                Is.True);
            Assert.That(
                duplicate.CharacterId,
                Is.EqualTo("character_a"));
            Assert.That(duplicate.SlotIndex, Is.Zero);
        }

        [Test]
        public void Index_MalformedCharactersAndSlotsAreHandledSafely()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Characters.Insert(0, null);
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "short_slots",
                EquippedRelicInstanceIds = new List<string>
                {
                    null, "", "   "
                }
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "extra_slots",
                EquippedRelicInstanceIds = new List<string>
                {
                    "", "", "", "", "bark_b"
                }
            });

            Assert.DoesNotThrow(() => new RelicEquipIndex(profile));
            Assert.DoesNotThrow(() => new RelicEquipIndex(null));

            var index = new RelicEquipIndex(profile);
            Assert.That(index.TryGetLocation("   ", out _), Is.False);
            Assert.That(index.TryGetLocation("bark_b", out _), Is.False);
        }

        [Test]
        public void Evaluate_MalformedTargetSlotsReturnInvalidTargetSlot()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Characters[0].EquippedRelicInstanceIds = null;

            RelicEquipAvailability nullSlots = editor.Evaluate(
                profile, "character_a", 0, "bark_b");

            AssertBlocked(
                nullSlots,
                RelicEquipAvailabilityReason.InvalidTargetSlot);

            profile.Characters[0].EquippedRelicInstanceIds =
                new List<string> { "", "", "" };
            RelicEquipAvailability shortSlots = editor.Evaluate(
                profile, "character_a", 0, "bark_b");

            AssertBlocked(
                shortSlots,
                RelicEquipAvailabilityReason.InvalidTargetSlot);
        }

        internal static ProfileSaveData CreateProfile()
        {
            ProfileSaveData profile =
                new NewProfileFactory().Create("profile", 1);
            profile.RelicInstances.Add(CreateRelic(
                "sword_a", "relic_sword", 0));
            profile.RelicInstances.Add(CreateRelic(
                "sword_b", "relic_sword", 5));
            profile.RelicInstances.Add(CreateRelic(
                "bark_a", "relic_bark", 0));
            profile.RelicInstances.Add(CreateRelic(
                "bark_b", "relic_bark", 5));
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "character_a",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "sword_a", "bark_a", "", ""
                }
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "character_b",
                Level = 1
            });
            return profile;
        }

        private static RelicInstanceSaveData CreateRelic(
            string instanceId,
            string definitionId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = definitionId,
                Awakening = awakening
            };
        }

        private static void AssertAllowed(
            RelicEquipAvailability availability)
        {
            Assert.That(availability.CanSelect, Is.True);
            Assert.That(
                availability.Reason,
                Is.EqualTo(RelicEquipAvailabilityReason.Allowed));
        }

        private static void AssertBlocked(
            RelicEquipAvailability availability,
            RelicEquipAvailabilityReason expectedReason)
        {
            Assert.That(availability.CanSelect, Is.False);
            Assert.That(availability.Reason, Is.EqualTo(expectedReason));
        }
    }
}
