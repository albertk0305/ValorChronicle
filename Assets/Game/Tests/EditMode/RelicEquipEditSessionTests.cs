using System;
using NUnit.Framework;
using ValorChronicle.Relics;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicEquipEditSessionTests
    {
        private RelicEquipEditor editor;

        [SetUp]
        public void SetUp()
        {
            editor = new RelicEquipEditor();
        }

        [Test]
        public void Open_PopulatedSlotCopiesOriginalToPreview()
        {
            ProfileSaveData profile =
                RelicEquipEditorTests.CreateProfile();

            var session = new RelicEquipEditSession(
                editor, profile, "character_a", 0);

            Assert.That(
                session.CharacterId,
                Is.EqualTo("character_a"));
            Assert.That(session.TargetSlotIndex, Is.Zero);
            Assert.That(session.OriginalInstanceId, Is.EqualTo("sword_a"));
            Assert.That(session.PreviewInstanceId, Is.EqualTo("sword_a"));
        }

        [Test]
        public void Open_EmptySlotUsesCanonicalEmptyId()
        {
            ProfileSaveData profile =
                RelicEquipEditorTests.CreateProfile();

            var session = new RelicEquipEditSession(
                editor, profile, "character_a", 3);

            Assert.That(
                session.OriginalInstanceId,
                Is.EqualTo(SaveRules.EmptyId));
            Assert.That(
                session.PreviewInstanceId,
                Is.EqualTo(SaveRules.EmptyId));
        }

        [Test]
        public void PreviewChange_DoesNotMutateProfile()
        {
            ProfileSaveData profile =
                RelicEquipEditorTests.CreateProfile();
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);
            var session = new RelicEquipEditSession(
                editor, profile, "character_a", 0);

            RelicEquipAvailability availability =
                session.TrySetPreview("sword_b");

            Assert.That(availability.CanSelect, Is.True);
            Assert.That(session.PreviewInstanceId, Is.EqualTo("sword_b"));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [Test]
        public void InvalidCandidate_DoesNotChangePreview()
        {
            ProfileSaveData profile =
                RelicEquipEditorTests.CreateProfile();
            var session = new RelicEquipEditSession(
                editor, profile, "character_a", 2);

            RelicEquipAvailability availability =
                session.TrySetPreview("sword_b");

            Assert.That(availability.CanSelect, Is.False);
            Assert.That(
                availability.Reason,
                Is.EqualTo(RelicEquipAvailabilityReason
                    .DuplicateDefinitionInOtherSlot));
            Assert.That(session.PreviewInstanceId, Is.Empty);
        }

        [Test]
        public void Commands_SeparateEquipPreviewFromClearTarget()
        {
            ProfileSaveData profile =
                RelicEquipEditorTests.CreateProfile();
            var session = new RelicEquipEditSession(
                editor, profile, "character_a", 0);
            session.TrySetPreview("sword_b");

            RelicEquipSessionCommand equip =
                session.CreateEquipCommand();
            RelicEquipSessionCommand clear =
                session.CreateClearCommand();

            Assert.That(
                equip.CommandType,
                Is.EqualTo(RelicEquipSessionCommandType.EquipPreview));
            Assert.That(equip.RelicInstanceId, Is.EqualTo("sword_b"));
            Assert.That(
                equip.ExpectedOriginalInstanceId,
                Is.EqualTo("sword_a"));
            Assert.That(
                clear.CommandType,
                Is.EqualTo(RelicEquipSessionCommandType.ClearTargetSlot));
            Assert.That(clear.RelicInstanceId, Is.Empty);
            Assert.That(
                clear.ExpectedOriginalInstanceId,
                Is.EqualTo("sword_a"));
            Assert.That(clear.CharacterId, Is.EqualTo("character_a"));
            Assert.That(clear.TargetSlotIndex, Is.Zero);
        }

        [Test]
        public void EmptyOriginalWithPreview_ClearCommandStillMeansClear()
        {
            ProfileSaveData profile =
                RelicEquipEditorTests.CreateProfile();
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "orb_a",
                RelicDefinitionId = "relic_orb",
                Awakening = 0
            });
            var session = new RelicEquipEditSession(
                editor, profile, "character_a", 3);
            session.TrySetPreview("orb_a");

            RelicEquipSessionCommand clear =
                session.CreateClearCommand();

            Assert.That(session.OriginalInstanceId, Is.Empty);
            Assert.That(session.PreviewInstanceId, Is.EqualTo("orb_a"));
            Assert.That(clear.RelicInstanceId, Is.Empty);
        }

        [Test]
        public void Open_RejectsMissingCharacterOrInvalidSlot()
        {
            ProfileSaveData profile =
                RelicEquipEditorTests.CreateProfile();

            Assert.Throws<ArgumentException>(() =>
                new RelicEquipEditSession(
                    editor, profile, "missing", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RelicEquipEditSession(
                    editor, profile, "character_a", 4));
            Assert.Throws<ArgumentNullException>(() =>
                new RelicEquipEditSession(
                    editor, null, "character_a", 0));
        }
    }
}
