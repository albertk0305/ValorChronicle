using NUnit.Framework;
using ValorChronicle.Characters.Progression;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode.Characters.Progression
{
    public sealed class CharacterLevelUpProfileUpdaterTests
    {
        [Test]
        public void TryApply_InsufficientRecords_DoesNotPartiallyMutateProfile()
        {
            ProfileSaveData profile = Profile(level: 10, battleRecords: 100);

            bool applied = CharacterLevelUpProfileUpdater.TryApply(
                profile,
                "hero_a",
                11,
                out CharacterLevelUpResult result,
                out CharacterLevelUpPersistenceStatus status);

            Assert.That(applied, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(
                status,
                Is.EqualTo(CharacterLevelUpPersistenceStatus
                    .InsufficientBattleRecords));
            Assert.That(profile.Characters[0].Level, Is.EqualTo(10));
            Assert.That(profile.Currencies.BattleRecords, Is.EqualTo(100));
        }

        [Test]
        public void TryApply_InvalidStoredValues_AreRejectedWithoutSanitizing()
        {
            ProfileSaveData profile = Profile(level: 0, battleRecords: 500);

            bool applied = CharacterLevelUpProfileUpdater.TryApply(
                profile,
                "hero_a",
                2,
                out CharacterLevelUpResult result,
                out CharacterLevelUpPersistenceStatus status);

            Assert.That(applied, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(
                status,
                Is.EqualTo(CharacterLevelUpPersistenceStatus.InvalidProfile));
            Assert.That(profile.Characters[0].Level, Is.Zero);
            Assert.That(profile.Currencies.BattleRecords, Is.EqualTo(500));
        }

        [Test]
        public void TryApply_SuccessChangesOnlyLevelAndBattleRecords()
        {
            ProfileSaveData profile = Profile(level: 1, battleRecords: 500);
            profile.Currencies.GachaCurrency = 11;
            profile.Currencies.HeroTokens = 22;
            profile.Currencies.RelicTokens = 33;

            bool applied = CharacterLevelUpProfileUpdater.TryApply(
                profile,
                "hero_a",
                2,
                out CharacterLevelUpResult result,
                out CharacterLevelUpPersistenceStatus status);

            Assert.That(applied, Is.True);
            Assert.That(status, Is.EqualTo(
                CharacterLevelUpPersistenceStatus.Success));
            Assert.That(result.SpentBattleRecords, Is.EqualTo(120));
            Assert.That(profile.Characters[0].Level, Is.EqualTo(2));
            Assert.That(profile.Currencies.BattleRecords, Is.EqualTo(380));
            Assert.That(profile.Currencies.GachaCurrency, Is.EqualTo(11));
            Assert.That(profile.Currencies.HeroTokens, Is.EqualTo(22));
            Assert.That(profile.Currencies.RelicTokens, Is.EqualTo(33));
        }

        private static ProfileSaveData Profile(int level, long battleRecords)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_a",
                Level = level
            });
            profile.Currencies.BattleRecords = battleRecords;
            return profile;
        }
    }
}
