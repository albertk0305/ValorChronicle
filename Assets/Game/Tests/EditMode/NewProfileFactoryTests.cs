using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class NewProfileFactoryTests
    {
        [Test]
        public void Create_BuildsExpectedEmptyProfile()
        {
            const string profileId = "profile_test";
            const long timestamp = 1700000000;
            var factory = new NewProfileFactory();

            ProfileSaveData profile = factory.Create(profileId, timestamp);

            Assert.That(profile.SaveVersion, Is.EqualTo(SaveSchema.CurrentVersion));
            Assert.That(profile.ProfileId, Is.EqualTo(profileId));
            Assert.That(profile.CreatedAtUtcUnixSeconds, Is.EqualTo(timestamp));
            Assert.That(profile.LastSavedAtUtcUnixSeconds, Is.EqualTo(timestamp));
            Assert.That(profile.Currencies.GachaCurrency, Is.Zero);
            Assert.That(profile.Currencies.BattleRecords, Is.Zero);
            Assert.That(profile.Currencies.HeroTokens, Is.Zero);
            Assert.That(profile.Currencies.RelicTokens, Is.Zero);
            Assert.That(profile.Characters, Is.Empty);
            Assert.That(profile.RelicInstances, Is.Empty);
            Assert.That(profile.GachaStates, Is.Empty);
            Assert.That(profile.BossRecords, Is.Empty);
            Assert.That(profile.UnlockedContentIds, Is.Empty);
            Assert.That(profile.CompletedTutorialIds, Is.Empty);
        }

        [Test]
        public void Create_BuildsOneEmptyFiveSlotPartyPreset()
        {
            var factory = new NewProfileFactory();

            ProfileSaveData profile = factory.Create("profile_test", 1);

            Assert.That(profile.Party.ActivePresetIndex, Is.Zero);
            Assert.That(profile.Party.Presets, Has.Count.EqualTo(1));
            Assert.That(
                profile.Party.Presets[0].PresetId,
                Is.EqualTo(SaveRules.DefaultPartyPresetId));
            Assert.That(
                profile.Party.Presets[0].CharacterSlotIds,
                Has.Count.EqualTo(SaveRules.PartySlotCount));
            Assert.That(profile.Party.Presets[0].CharacterSlotIds.All(string.IsNullOrEmpty), Is.True);
            Assert.That(profile.Party.LastBossId, Is.Empty);
            Assert.That(profile.Party.LastDifficultyId, Is.Empty);
        }

        [Test]
        public void Create_DoesNotGrantStartingContent()
        {
            var factory = new NewProfileFactory();

            ProfileSaveData profile = factory.Create("profile_test", 1);

            Assert.That(profile.Characters, Is.Empty);
            Assert.That(profile.RelicInstances, Is.Empty);
            Assert.That(profile.UnlockedContentIds, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Create_RejectsInvalidProfileId(string profileId)
        {
            var factory = new NewProfileFactory();

            Assert.Throws<System.ArgumentException>(() => factory.Create(profileId, 1));
        }
    }
}
