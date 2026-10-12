using System;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Acquisition;
using ValorChronicle.Data.Database;
using ValorChronicle.Rewards;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class CharacterAcquisitionProfileUpdaterTests
    {
        private const string CharacterId = "character_marea_bluefang";
        private CharacterAcquisitionProfileUpdater updater;

        [SetUp]
        public void SetUp()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    "Assets/Data/Database/DefinitionDatabase.asset");
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            updater = new CharacterAcquisitionProfileUpdater(database);
        }

        [Test]
        public void FirstAcquisition_CreatesCanonicalNewCharacter()
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();

            CharacterAcquisitionResult result = updater.Apply(
                profile,
                CharacterId);

            Assert.That(result.Kind,
                Is.EqualTo(CharacterAcquisitionKind.NewCharacter));
            Assert.That(result.CharacterId, Is.EqualTo(CharacterId));
            Assert.That(result.PreviousAwakening, Is.Null);
            Assert.That(result.NewAwakening, Is.Zero);
            Assert.That(result.CurrencyGrant, Is.Null);
            Assert.That(profile.Characters, Has.Count.EqualTo(1));
            CharacterSaveData character = profile.Characters[0];
            Assert.That(character.Level,
                Is.EqualTo(SaveRules.CharacterMinLevel));
            Assert.That(character.Awakening,
                Is.EqualTo(SaveRules.CharacterMinAwakening));
            Assert.That(character.IsNew, Is.True);
            Assert.That(character.IsFavorite, Is.False);
            Assert.That(character.EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "", "", "", "" }));
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(5, 6)]
        public void DuplicateBelowMax_IncreasesAwakeningOnly(
            int before,
            int after)
        {
            ProfileSaveData profile = ProfileWithCharacter(before);
            CharacterSaveData character = profile.Characters[0];
            character.Level = 37;
            character.IsFavorite = true;
            character.IsNew = false;
            character.EquippedRelicInstanceIds[0] = "relic_instance_a";

            CharacterAcquisitionResult result = updater.Apply(
                profile,
                CharacterId);

            Assert.That(result.Kind,
                Is.EqualTo(CharacterAcquisitionKind.AwakeningIncreased));
            Assert.That(result.PreviousAwakening, Is.EqualTo(before));
            Assert.That(result.NewAwakening, Is.EqualTo(after));
            Assert.That(profile.Characters, Has.Count.EqualTo(1));
            Assert.That(character.Level, Is.EqualTo(37));
            Assert.That(character.Awakening, Is.EqualTo(after));
            Assert.That(character.IsFavorite, Is.True);
            Assert.That(character.IsNew, Is.False);
            Assert.That(character.EquippedRelicInstanceIds[0],
                Is.EqualTo("relic_instance_a"));
        }

        [Test]
        public void DuplicateAtMax_ConvertsToSixtyHeroTokens()
        {
            ProfileSaveData profile = ProfileWithCharacter(
                SaveRules.CharacterMaxAwakening);
            profile.Currencies.HeroTokens = 3L;
            CharacterSaveData character = profile.Characters[0];
            character.Level = 88;
            character.EquippedRelicInstanceIds[2] = "relic_equipped";

            CharacterAcquisitionResult result = updater.Apply(
                profile,
                CharacterId);

            Assert.That(
                result.Kind,
                Is.EqualTo(CharacterAcquisitionKind
                    .MaxAwakeningConvertedToHeroTokens));
            Assert.That(result.PreviousAwakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            Assert.That(result.NewAwakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            Assert.That(result.CurrencyGrant.CurrencyKind,
                Is.EqualTo(RewardCurrencyKind.HeroTokens));
            Assert.That(result.CurrencyGrant.Amount, Is.EqualTo(60L));
            Assert.That(profile.Currencies.HeroTokens, Is.EqualTo(63L));
            Assert.That(character.Awakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            Assert.That(character.Level, Is.EqualTo(88));
            Assert.That(character.EquippedRelicInstanceIds[2],
                Is.EqualTo("relic_equipped"));
        }

        [Test]
        public void DuplicateAtMax_OverflowFailsWithoutCharacterMutation()
        {
            ProfileSaveData profile = ProfileWithCharacter(
                SaveRules.CharacterMaxAwakening);
            profile.Currencies.HeroTokens = long.MaxValue - 59L;

            Assert.Throws<OverflowException>(() =>
                updater.Apply(profile, CharacterId));
            Assert.That(profile.Characters[0].Awakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            Assert.That(profile.Currencies.HeroTokens,
                Is.EqualTo(long.MaxValue - 59L));
        }

        [Test]
        public void InvalidCharacterId_IsRejectedWithoutMutation()
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();

            Assert.Throws<ArgumentException>(() =>
                updater.Apply(profile, string.Empty));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => updater.Apply(profile, "character_missing"));
            Assert.That(profile.Characters, Is.Empty);
        }

        private static ProfileSaveData ProfileWithCharacter(int awakening)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = CharacterId,
                Level = SaveRules.CharacterMinLevel,
                Awakening = awakening
            });
            return profile;
        }
    }
}
