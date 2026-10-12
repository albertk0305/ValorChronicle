using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Gacha.Presentation;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaScreenPresentationTests
    {
        private const string DatabaseAssetPath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        [Test]
        public void DisplayDataMapsCurrenciesCostsAndMissingStates()
        {
            DefinitionDatabase database = LoadDatabase();
            ProfileSaveData profile = CreateProfile(
                gachaCurrency: 1234567890123,
                heroTokens: 17,
                relicTokens: 29);

            bool success = GachaScreenController.TryCreateDisplayData(
                database,
                profile,
                out GachaScreenDisplayData data,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(data.HeroTokens, Is.EqualTo(17));
            Assert.That(data.RelicTokens, Is.EqualTo(29));
            Assert.That(data.GachaCurrency,
                Is.EqualTo(1234567890123));
            Assert.That(data.CharacterBanner.PityCount, Is.Zero);
            Assert.That(data.CharacterBanner.PityMaximum, Is.EqualTo(50));
            Assert.That(data.RelicBanner.PityCount, Is.Zero);
            Assert.That(data.RelicBanner.PityMaximum, Is.EqualTo(20));
            Assert.That(data.CharacterBanner.SinglePullCost,
                Is.EqualTo(100));
            Assert.That(data.CharacterBanner.TenPullCost,
                Is.EqualTo(1000));
            Assert.That(data.RelicBanner.SinglePullCost,
                Is.EqualTo(100));
            Assert.That(data.RelicBanner.TenPullCost,
                Is.EqualTo(1000));
        }

        [Test]
        public void DisplayDataKeepsBannerCountersIndependent()
        {
            ProfileSaveData profile = CreateProfile(2000, 0, 0);
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = GachaScreenController.CharacterGachaId,
                PityCount = 17,
                TotalPullCount = 31
            });
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = GachaScreenController.RelicGachaId,
                PityCount = 6,
                TotalPullCount = 12
            });

            bool success = GachaScreenController.TryCreateDisplayData(
                LoadDatabase(),
                profile,
                out GachaScreenDisplayData data,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(data.CharacterBanner.PityCount, Is.EqualTo(17));
            Assert.That(data.RelicBanner.PityCount, Is.EqualTo(6));
        }

        [TestCase(-1, 10)]
        [TestCase(50, 50)]
        [TestCase(11, 10)]
        public void DisplayDataRejectsInvalidExistingCharacterState(
            int pityCount,
            long totalPullCount)
        {
            ProfileSaveData profile = CreateProfile(1000, 0, 0);
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = GachaScreenController.CharacterGachaId,
                PityCount = pityCount,
                TotalPullCount = totalPullCount
            });

            bool success = GachaScreenController.TryCreateDisplayData(
                LoadDatabase(),
                profile,
                out GachaScreenDisplayData data,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(data, Is.Null);
            Assert.That(error, Does.Contain("is invalid"));
        }

        [Test]
        public void DisplayDataRejectsDuplicateBannerState()
        {
            ProfileSaveData profile = CreateProfile(1000, 0, 0);
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = GachaScreenController.RelicGachaId,
                PityCount = 1,
                TotalPullCount = 1
            });
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = GachaScreenController.RelicGachaId,
                PityCount = 2,
                TotalPullCount = 2
            });

            bool success = GachaScreenController.TryCreateDisplayData(
                LoadDatabase(),
                profile,
                out _,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("duplicated"));
        }

        [Test]
        public void DisplayDataRejectsMissingDefinitions()
        {
            DefinitionDatabase emptyDatabase =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            try
            {
                emptyDatabase.Initialize();

                bool success =
                    GachaScreenController.TryCreateDisplayData(
                        emptyDatabase,
                        CreateProfile(1000, 0, 0),
                        out _,
                        out string error);

                Assert.That(success, Is.False);
                Assert.That(error, Does.Contain("was not found"));
            }
            finally
            {
                Object.DestroyImmediate(emptyDatabase);
            }
        }

        [Test]
        public void DisplayDataDoesNotMutateProfile()
        {
            ProfileSaveData profile = CreateProfile(1000, 3, 4);
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = GachaScreenController.CharacterGachaId,
                PityCount = 9,
                TotalPullCount = 12
            });

            bool success = GachaScreenController.TryCreateDisplayData(
                LoadDatabase(),
                profile,
                out _,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(profile.Currencies.GachaCurrency, Is.EqualTo(1000));
            Assert.That(profile.Currencies.HeroTokens, Is.EqualTo(3));
            Assert.That(profile.Currencies.RelicTokens, Is.EqualTo(4));
            Assert.That(profile.GachaStates, Has.Count.EqualTo(1));
            Assert.That(profile.GachaStates[0].PityCount, Is.EqualTo(9));
            Assert.That(profile.GachaStates[0].TotalPullCount,
                Is.EqualTo(12));
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabaseAssetPath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private static ProfileSaveData CreateProfile(
            long gachaCurrency,
            long heroTokens,
            long relicTokens)
        {
            return new ProfileSaveData
            {
                Currencies = new CurrencySaveData
                {
                    GachaCurrency = gachaCurrency,
                    HeroTokens = heroTokens,
                    RelicTokens = relicTokens
                },
                GachaStates = new List<GachaStateSaveData>()
            };
        }
    }
}
