using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveDataValueComparerTests
    {
        private readonly SaveDataValueComparer comparer =
            new SaveDataValueComparer();

        [Test]
        public void Equals_MatchingRelicSlotsAreEqual()
        {
            ProfileSaveData first = CreateProfile(
                "relic_a", "relic_b", "", "");
            ProfileSaveData second = CreateProfile(
                "relic_a", "relic_b", "", "");

            Assert.That(comparer.Equals(first, second), Is.True);
        }

        [Test]
        public void Equals_DifferentRelicInstanceIsNotEqual()
        {
            ProfileSaveData first = CreateProfile(
                "relic_a", "relic_b", "", "");
            ProfileSaveData second = CreateProfile(
                "relic_a", "relic_c", "", "");

            Assert.That(comparer.Equals(first, second), Is.False);
        }

        [Test]
        public void Equals_SameRelicsInDifferentSlotsAreNotEqual()
        {
            ProfileSaveData first = CreateProfile(
                "relic_a", "relic_b", "", "");
            ProfileSaveData second = CreateProfile(
                "relic_b", "relic_a", "", "");

            Assert.That(comparer.Equals(first, second), Is.False);
        }

        [Test]
        public void Equals_IgnoresLegacyRelicEquipFields()
        {
            ProfileSaveData first = CreateProfile(
                "relic_a", "", "", "");
            ProfileSaveData second = CreateProfile(
                "relic_a", "", "", "");
            first.RelicInstances = new List<RelicInstanceSaveData>
            {
                CreateRelic("legacy_character", 2)
            };
            second.RelicInstances = new List<RelicInstanceSaveData>
            {
                CreateRelic("", -1)
            };

            Assert.That(comparer.Equals(first, second), Is.True);
        }

        private static ProfileSaveData CreateProfile(params string[] slots)
        {
            return new ProfileSaveData
            {
                ProfileId = "profile",
                Characters = new List<CharacterSaveData>
                {
                    new CharacterSaveData
                    {
                        CharacterId = "hero",
                        EquippedRelicInstanceIds =
                            new List<string>(slots)
                    }
                }
            };
        }

        private static RelicInstanceSaveData CreateRelic(
            string legacyCharacterId,
            int legacySlotIndex)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = "relic_a",
                RelicDefinitionId = "sword",
                EquippedCharacterId = legacyCharacterId,
                EquippedSlotIndex = legacySlotIndex
            };
        }
    }
}
