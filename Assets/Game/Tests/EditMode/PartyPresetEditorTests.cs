using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Party;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class PartyPresetEditorTests
    {
        private PartyPresetEditor editor;

        [SetUp]
        public void SetUp()
        {
            editor = new PartyPresetEditor();
        }

        [Test]
        public void Edit_SameCharacter_ReturnsNoChange()
        {
            PartyPresetEditResult result = editor.Edit(
                Slots("A", "B", "C", "", ""),
                1,
                "B");

            AssertResult(
                result,
                PartyPresetEditOperationType.NoChange,
                null,
                1,
                "A", "B", "C", "", "");
        }

        [Test]
        public void Edit_NewCharacterIntoEmptySlot_PlacesCharacter()
        {
            PartyPresetEditResult result = editor.Edit(
                Slots("A", "B", "", "", ""),
                2,
                "C");

            AssertResult(
                result,
                PartyPresetEditOperationType.Place,
                null,
                2,
                "A", "B", "C", "", "");
        }

        [Test]
        public void Edit_NewCharacterIntoOccupiedSlot_ReplacesCharacter()
        {
            PartyPresetEditResult result = editor.Edit(
                Slots("A", "B", "C", "", ""),
                1,
                "D");

            AssertResult(
                result,
                PartyPresetEditOperationType.Replace,
                null,
                1,
                "A", "D", "C", "", "");
        }

        [Test]
        public void Edit_ExistingCharacterIntoEmptySlot_MovesCharacter()
        {
            PartyPresetEditResult result = editor.Edit(
                Slots("A", "B", "C", "", ""),
                3,
                "B");

            AssertResult(
                result,
                PartyPresetEditOperationType.Move,
                1,
                3,
                "A", "", "C", "B", "");
        }

        [Test]
        public void Edit_ExistingCharacterIntoOccupiedSlot_SwapsCharacters()
        {
            PartyPresetEditResult result = editor.Edit(
                Slots("A", "B", "C", "", ""),
                0,
                "C");

            AssertResult(
                result,
                PartyPresetEditOperationType.Swap,
                2,
                0,
                "C", "B", "A", "", "");
        }

        [Test]
        public void Edit_LastSlotCharacterIntoFirstEmptySlot_MovesCharacter()
        {
            PartyPresetEditResult result = editor.Edit(
                Slots("", "A", "", "", "B"),
                0,
                "B");

            AssertResult(
                result,
                PartyPresetEditOperationType.Move,
                4,
                0,
                "B", "A", "", "", "");
        }

        [Test]
        public void Edit_AdjacentExistingCharacter_SwapsCharacters()
        {
            PartyPresetEditResult result = editor.Edit(
                Slots("A", "B", "C", "", ""),
                1,
                "C");

            AssertResult(
                result,
                PartyPresetEditOperationType.Swap,
                2,
                1,
                "A", "C", "B", "", "");
        }

        [Test]
        public void Clear_OccupiedSlot_ClearsCharacter()
        {
            PartyPresetEditResult result = editor.Clear(
                Slots("A", "B", "C", "", ""),
                1);

            AssertResult(
                result,
                PartyPresetEditOperationType.Clear,
                null,
                1,
                "A", "", "C", "", "");
        }

        [Test]
        public void Clear_EmptySlot_ReturnsNoChange()
        {
            PartyPresetEditResult result = editor.Clear(
                Slots("A", "B", "C", "", ""),
                3);

            AssertResult(
                result,
                PartyPresetEditOperationType.NoChange,
                null,
                3,
                "A", "B", "C", "", "");
        }

        [Test]
        public void SequentialEdits_KeepFiveUniqueSlots()
        {
            IReadOnlyList<string> slots = Slots(
                "A", "B", "C", "", "");

            slots = editor.Edit(slots, 0, "C").ResultingSlots;
            slots = editor.Edit(slots, 4, "B").ResultingSlots;
            slots = editor.Clear(slots, 2).ResultingSlots;

            Assert.That(
                slots,
                Is.EqualTo(new[] { "C", "", "", "", "B" }));
            Assert.That(slots, Has.Count.EqualTo(5));
            AssertNoDuplicateCharacters(slots);
        }

        [Test]
        public void Edit_DoesNotMutateInputOrExposeMutableResult()
        {
            var input = Slots("A", "B", "C", "", "");
            string[] before = input.ToArray();

            PartyPresetEditResult result = editor.Edit(input, 0, "C");

            Assert.That(input, Is.EqualTo(before));
            Assert.That(
                () => ((IList<string>)result.ResultingSlots)[0] = "changed",
                Throws.TypeOf<NotSupportedException>());
            Assert.That(
                result.ResultingSlots,
                Is.EqualTo(new[] { "C", "B", "A", "", "" }));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void Edit_InvalidSlotIndex_Throws(int slotIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                editor.Edit(Slots("", "", "", "", ""), slotIndex, "A"));
        }

        [Test]
        public void Edit_InvalidSlotCollection_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                editor.Edit(null, 0, "A"));
            Assert.Throws<ArgumentException>(() =>
                editor.Edit(new[] { "", "", "", "" }, 0, "A"));
            Assert.Throws<ArgumentException>(() =>
                editor.Edit(new[] { "A", null, "", "", "" }, 0, "B"));
            Assert.Throws<ArgumentException>(() =>
                editor.Edit(new[] { "A", "", "A", "", "" }, 0, "B"));
        }

        [Test]
        public void Edit_NullPreviewThrowsButEmptyPreviewClears()
        {
            IReadOnlyList<string> slots = Slots(
                "A", "B", "C", "", "");

            Assert.Throws<ArgumentNullException>(() =>
                editor.Edit(slots, 1, null));

            PartyPresetEditResult result = editor.Edit(slots, 1, "");
            Assert.That(
                result.OperationType,
                Is.EqualTo(PartyPresetEditOperationType.Clear));
            Assert.That(
                result.ResultingSlots,
                Is.EqualTo(new[] { "A", "", "C", "", "" }));
        }

        private static List<string> Slots(params string[] characterIds)
        {
            return new List<string>(characterIds);
        }

        private static void AssertResult(
            PartyPresetEditResult result,
            PartyPresetEditOperationType operationType,
            int? sourceSlotIndex,
            int targetSlotIndex,
            params string[] expectedSlots)
        {
            Assert.That(result.OperationType, Is.EqualTo(operationType));
            Assert.That(result.SourceSlotIndex, Is.EqualTo(sourceSlotIndex));
            Assert.That(result.TargetSlotIndex, Is.EqualTo(targetSlotIndex));
            Assert.That(result.ResultingSlots, Is.EqualTo(expectedSlots));
            Assert.That(result.ResultingSlots, Has.Count.EqualTo(5));
            AssertNoDuplicateCharacters(result.ResultingSlots);
        }

        private static void AssertNoDuplicateCharacters(
            IEnumerable<string> slots)
        {
            string[] occupied = slots
                .Where(characterId => characterId != string.Empty)
                .ToArray();
            Assert.That(
                occupied.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(occupied.Length));
        }
    }
}
