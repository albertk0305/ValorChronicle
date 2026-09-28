using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Party;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class PartyEditSessionTests
    {
        private PartyPresetEditor editor;

        [SetUp]
        public void SetUp()
        {
            editor = new PartyPresetEditor();
        }

        [Test]
        public void Open_CopiesOriginalAndPreviewFromSelectedSlot()
        {
            var session = new PartyEditSession(
                editor,
                3,
                1,
                Slots("A", "B", "C", "", ""));

            Assert.That(session.PresetIndex, Is.EqualTo(3));
            Assert.That(session.SlotIndex, Is.EqualTo(1));
            Assert.That(session.OriginalCharacterId, Is.EqualTo("B"));
            Assert.That(session.PreviewCharacterId, Is.EqualTo("B"));
            Assert.That(
                session.SecondaryAction,
                Is.EqualTo(PartyEditSecondaryAction.Clear));
        }

        [Test]
        public void PreviewChangeThenRevert_RestoresOriginalWithoutMutation()
        {
            var slots = Slots("A", "B", "C", "", "");
            var session = new PartyEditSession(editor, 0, 0, slots);

            session.SetPreviewCharacterId("B");

            Assert.That(session.PreviewCharacterId, Is.EqualTo("B"));
            Assert.That(
                session.SecondaryAction,
                Is.EqualTo(PartyEditSecondaryAction.Revert));
            Assert.That(slots, Is.EqualTo(new[] { "A", "B", "C", "", "" }));

            session.Revert();

            Assert.That(session.PreviewCharacterId, Is.EqualTo("A"));
            Assert.That(
                session.SecondaryAction,
                Is.EqualTo(PartyEditSecondaryAction.Clear));
            Assert.That(slots, Is.EqualTo(new[] { "A", "B", "C", "", "" }));
        }

        [Test]
        public void EmptySession_UsesCloseWithoutChangeState()
        {
            var session = new PartyEditSession(
                editor,
                0,
                3,
                Slots("A", "B", "C", "", ""));

            Assert.That(session.OriginalCharacterId, Is.Empty);
            Assert.That(session.PreviewCharacterId, Is.Empty);
            Assert.That(
                session.SecondaryAction,
                Is.EqualTo(PartyEditSecondaryAction.CloseWithoutChange));
        }

        [Test]
        public void Confirm_UsesSnapshotAndReturnsCalculatedEdit()
        {
            var slots = Slots("A", "B", "C", "", "");
            var session = new PartyEditSession(editor, 2, 0, slots);
            slots[2] = "changed_after_open";
            session.SetPreviewCharacterId("C");

            PartyPresetEditResult result = session.Confirm();

            Assert.That(
                result.OperationType,
                Is.EqualTo(PartyPresetEditOperationType.Swap));
            Assert.That(result.SourceSlotIndex, Is.EqualTo(2));
            Assert.That(result.TargetSlotIndex, Is.Zero);
            Assert.That(
                result.ResultingSlots,
                Is.EqualTo(new[] { "C", "B", "A", "", "" }));
            Assert.That(
                slots,
                Is.EqualTo(new[]
                {
                    "A", "B", "changed_after_open", "", ""
                }));
        }

        [Test]
        public void SetPreview_NullCharacterIdThrows()
        {
            var session = new PartyEditSession(
                editor,
                0,
                0,
                Slots("A", "B", "C", "", ""));

            Assert.Throws<ArgumentNullException>(() =>
                session.SetPreviewCharacterId(null));
            Assert.That(session.PreviewCharacterId, Is.EqualTo("A"));
        }

        [TestCase(-1, 0)]
        [TestCase(5, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 5)]
        public void Open_InvalidPresetOrSlotIndexThrows(
            int presetIndex,
            int slotIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PartyEditSession(
                    editor,
                    presetIndex,
                    slotIndex,
                    Slots("", "", "", "", "")));
        }

        [Test]
        public void Open_InvalidDependenciesOrSlotsThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PartyEditSession(
                    null,
                    0,
                    0,
                    Slots("", "", "", "", "")));
            Assert.Throws<ArgumentNullException>(() =>
                new PartyEditSession(editor, 0, 0, null));
            Assert.Throws<ArgumentException>(() =>
                new PartyEditSession(
                    editor,
                    0,
                    0,
                    Slots("", "", "", "")));
        }

        private static List<string> Slots(params string[] characterIds)
        {
            return new List<string>(characterIds);
        }
    }
}
