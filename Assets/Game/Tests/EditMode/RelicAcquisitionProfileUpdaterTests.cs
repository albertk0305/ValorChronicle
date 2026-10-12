using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicAcquisitionProfileUpdaterTests
    {
        [Test]
        public void FirstAcquisition_CreatesCanonicalZeroAwakeningInstance()
        {
            ProfileSaveData profile = Profile();
            var provider = new SequentialIdProvider("instance_sword");
            var updater = new RelicAcquisitionProfileUpdater(provider);

            bool applied = updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out _);

            Assert.That(applied, Is.True);
            Assert.That(
                result.Kind,
                Is.EqualTo(RelicAcquisitionKind.NewInstanceCreated));
            Assert.That(result.RelicDefinitionId, Is.EqualTo("relic_sword"));
            Assert.That(result.AffectedInstanceId, Is.EqualTo("instance_sword"));
            Assert.That(result.PreviousAwakening, Is.Null);
            Assert.That(result.NewAwakening, Is.Zero);
            Assert.That(profile.RelicInstances, Has.Count.EqualTo(1));
            AssertNewInstance(profile.RelicInstances[0], "instance_sword");
        }

        [Test]
        public void DuplicateAcquisition_IncreasesOnlyAwakening()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_a", "relic_sword", 0, true, false));
            profile.Characters.Add(Character("character_a", "sword_a"));
            var provider = new SequentialIdProvider("unused");
            var updater = new RelicAcquisitionProfileUpdater(provider);

            bool applied = updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out _);

            Assert.That(applied, Is.True);
            Assert.That(result.Kind,
                Is.EqualTo(RelicAcquisitionKind.AwakeningIncreased));
            Assert.That(result.AffectedInstanceId, Is.EqualTo("sword_a"));
            Assert.That(result.PreviousAwakening, Is.Zero);
            Assert.That(result.NewAwakening, Is.EqualTo(1));
            Assert.That(profile.RelicInstances, Has.Count.EqualTo(1));
            Assert.That(profile.RelicInstances[0].Awakening, Is.EqualTo(1));
            Assert.That(profile.RelicInstances[0].IsLocked, Is.True);
            Assert.That(profile.RelicInstances[0].IsNew, Is.False);
            Assert.That(
                profile.RelicInstances[0].EquippedCharacterId,
                Is.Empty);
            Assert.That(
                profile.RelicInstances[0].EquippedSlotIndex,
                Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
            Assert.That(
                profile.Characters[0].EquippedRelicInstanceIds[0],
                Is.EqualTo("sword_a"));
            Assert.That(provider.CallCount, Is.Zero);
        }

        [Test]
        public void RepeatedDuplicates_GrowSameInstanceFromOneToFive()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_a", "relic_sword", 1));
            var provider = new SequentialIdProvider("unused");
            var updater = new RelicAcquisitionProfileUpdater(provider);

            for (int expected = 2;
                expected <= SaveRules.RelicMaxAwakening;
                expected++)
            {
                Assert.That(
                    updater.TryApply(
                        profile,
                        "relic_sword",
                        out RelicAcquisitionResult result,
                        out _),
                    Is.True);
                Assert.That(result.AffectedInstanceId, Is.EqualTo("sword_a"));
                Assert.That(result.NewAwakening, Is.EqualTo(expected));
            }

            Assert.That(profile.RelicInstances, Has.Count.EqualTo(1));
            Assert.That(
                profile.RelicInstances[0].Awakening,
                Is.EqualTo(SaveRules.RelicMaxAwakening));
            Assert.That(provider.CallCount, Is.Zero);
        }

        [Test]
        public void MaxAwakening_CreatesExtraZeroAwakeningCopy()
        {
            ProfileSaveData profile = Profile(
                Relic(
                    "sword_a",
                    "relic_sword",
                    SaveRules.RelicMaxAwakening));
            var updater = new RelicAcquisitionProfileUpdater(
                new SequentialIdProvider("sword_b"));

            Assert.That(
                updater.TryApply(
                    profile,
                    "relic_sword",
                    out RelicAcquisitionResult result,
                    out _),
                Is.True);

            Assert.That(
                result.Kind,
                Is.EqualTo(RelicAcquisitionKind
                    .NewInstanceCreatedAfterMaxAwakening));
            Assert.That(profile.RelicInstances, Has.Count.EqualTo(2));
            Assert.That(profile.RelicInstances[0].Awakening,
                Is.EqualTo(SaveRules.RelicMaxAwakening));
            AssertNewInstance(profile.RelicInstances[1], "sword_b");
        }

        [Test]
        public void ExtraCopyBelowMax_IsAwakenedBeforeCreatingAnotherCopy()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_a", "relic_sword", 5),
                Relic("sword_b", "relic_sword", 0));
            var updater = new RelicAcquisitionProfileUpdater(
                new SequentialIdProvider("unused"));

            updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out _);

            Assert.That(result.AffectedInstanceId, Is.EqualTo("sword_b"));
            Assert.That(profile.RelicInstances[0].Awakening, Is.EqualTo(5));
            Assert.That(profile.RelicInstances[1].Awakening, Is.EqualTo(1));
            Assert.That(profile.RelicInstances, Has.Count.EqualTo(2));
        }

        [Test]
        public void AllCopiesAtMax_CreateThirdCopyWithoutChangingEquipment()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_a", "relic_sword", 5),
                Relic("sword_b", "relic_sword", 5));
            profile.Characters.Add(Character("character_a", "sword_a"));
            profile.Characters.Add(Character("character_b", "sword_b"));
            var updater = new RelicAcquisitionProfileUpdater(
                new SequentialIdProvider("sword_c"));

            updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out _);

            Assert.That(result.AffectedInstanceId, Is.EqualTo("sword_c"));
            Assert.That(profile.RelicInstances, Has.Count.EqualTo(3));
            Assert.That(
                profile.Characters[0].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "sword_a", "", "", "" }));
            Assert.That(
                profile.Characters[1].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "sword_b", "", "", "" }));
        }

        [Test]
        public void MultipleSubMaxCopies_SelectHighestAwakening()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_a", "relic_sword", 2),
                Relic("sword_b", "relic_sword", 4));
            var updater = new RelicAcquisitionProfileUpdater(
                new SequentialIdProvider("unused"));

            updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out _);

            Assert.That(result.AffectedInstanceId, Is.EqualTo("sword_b"));
            Assert.That(profile.RelicInstances[0].Awakening, Is.EqualTo(2));
            Assert.That(profile.RelicInstances[1].Awakening, Is.EqualTo(5));
        }

        [Test]
        public void EqualAwakening_SelectsEarlierListEntry()
        {
            ProfileSaveData profile = Profile(
                Relic("sword_z", "relic_sword", 3),
                Relic("sword_a", "relic_sword", 3));
            var updater = new RelicAcquisitionProfileUpdater(
                new SequentialIdProvider("unused"));

            updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out _);

            Assert.That(result.AffectedInstanceId, Is.EqualTo("sword_z"));
            Assert.That(profile.RelicInstances[0].Awakening, Is.EqualTo(4));
            Assert.That(profile.RelicInstances[1].Awakening, Is.EqualTo(3));
        }

        [Test]
        public void Acquisition_DoesNotChangeOtherDefinitionsOrAutoEquip()
        {
            ProfileSaveData profile = Profile(
                Relic("bark_a", "relic_bark", 4, true, false));
            profile.Characters.Add(Character("character_a"));
            var updater = new RelicAcquisitionProfileUpdater(
                new SequentialIdProvider("sword_a"));

            updater.TryApply(
                profile,
                "relic_sword",
                out _,
                out _);

            Assert.That(profile.RelicInstances[0].Awakening, Is.EqualTo(4));
            Assert.That(profile.RelicInstances[0].IsLocked, Is.True);
            Assert.That(profile.RelicInstances[0].IsNew, Is.False);
            Assert.That(
                profile.Characters[0].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "", "", "", "" }));
        }

        [Test]
        public void InstanceIdCollision_RetriesUntilUniqueId()
        {
            ProfileSaveData profile = Profile(
                Relic("duplicate", "relic_bark", 0));
            var provider = new SequentialIdProvider(
                "duplicate",
                "unique");
            var updater = new RelicAcquisitionProfileUpdater(provider);

            bool applied = updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out _);

            Assert.That(applied, Is.True);
            Assert.That(result.AffectedInstanceId, Is.EqualTo("unique"));
            Assert.That(provider.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void ExhaustedInstanceIdAttempts_FailWithoutMutation()
        {
            ProfileSaveData profile = Profile(
                Relic("duplicate", "relic_bark", 0));
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);
            var provider = new SequentialIdProvider("duplicate");
            var updater = new RelicAcquisitionProfileUpdater(provider);

            bool applied = updater.TryApply(
                profile,
                "relic_sword",
                out RelicAcquisitionResult result,
                out RelicAcquisitionPersistenceStatus failure);

            Assert.That(applied, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(
                failure,
                Is.EqualTo(RelicAcquisitionPersistenceStatus
                    .InstanceIdGenerationFailed));
            Assert.That(
                provider.CallCount,
                Is.EqualTo(RelicAcquisitionProfileUpdater
                    .InstanceIdGenerationAttemptLimit));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [Test]
        public void GuidProvider_ReturnsLowercaseNFormatGuid()
        {
            string instanceId = new GuidRelicInstanceIdProvider()
                .CreateRelicInstanceId();

            Assert.That(instanceId, Has.Length.EqualTo(32));
            Assert.That(instanceId, Does.Match("^[0-9a-f]{32}$"));
        }

        private static ProfileSaveData Profile(
            params RelicInstanceSaveData[] relics)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.RelicInstances.AddRange(relics);
            return profile;
        }

        private static RelicInstanceSaveData Relic(
            string instanceId,
            string definitionId,
            int awakening,
            bool isLocked = false,
            bool isNew = true)
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
            params string[] equippedInstanceIds)
        {
            var character = new CharacterSaveData
            {
                CharacterId = characterId,
                Level = 1
            };
            for (int index = 0;
                index < equippedInstanceIds.Length;
                index++)
            {
                character.EquippedRelicInstanceIds[index] =
                    equippedInstanceIds[index];
            }

            return character;
        }

        private static void AssertNewInstance(
            RelicInstanceSaveData relic,
            string expectedInstanceId)
        {
            Assert.That(relic.InstanceId, Is.EqualTo(expectedInstanceId));
            Assert.That(relic.RelicDefinitionId, Is.EqualTo("relic_sword"));
            Assert.That(relic.Awakening, Is.Zero);
            Assert.That(relic.IsLocked, Is.False);
            Assert.That(relic.IsNew, Is.True);
            Assert.That(relic.EquippedCharacterId, Is.Empty);
            Assert.That(
                relic.EquippedSlotIndex,
                Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
        }

        internal sealed class SequentialIdProvider
            : IRelicInstanceIdProvider
        {
            private readonly Queue<string> ids;
            private string lastId;

            public SequentialIdProvider(params string[] ids)
            {
                this.ids = new Queue<string>(ids);
                lastId = ids.Length > 0 ? ids[ids.Length - 1] : null;
            }

            public int CallCount { get; private set; }

            public string CreateRelicInstanceId()
            {
                CallCount++;
                if (ids.Count > 0)
                {
                    lastId = ids.Dequeue();
                }

                return lastId;
            }
        }
    }
}
