using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveMigrationRunnerTests
    {
        private readonly SaveDataCloner cloner = new SaveDataCloner();

        [Test]
        public void Migrate_CurrentVersionReturnsSuccessfulDeepCopy()
        {
            ProfileSaveData source = CreateProfile(SaveRules.CurrentSaveVersion);
            var runner = CreateRunner();

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(result.Status, Is.EqualTo(SaveMigrationStatus.Success));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.SourceVersion, Is.EqualTo(SaveRules.CurrentSaveVersion));
            Assert.That(result.TargetVersion, Is.EqualTo(SaveRules.CurrentSaveVersion));
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data, Is.Not.SameAs(source));
            Assert.That(result.Data.Characters, Is.Not.SameAs(source.Characters));
            Assert.That(result.Data.Characters[0], Is.Not.SameAs(source.Characters[0]));
            Assert.That(result.Data.ProfileId, Is.EqualTo(source.ProfileId));
        }

        [Test]
        public void Migrate_FutureVersionIsRejectedWithoutData()
        {
            ProfileSaveData source = CreateProfile(SaveRules.CurrentSaveVersion + 1);
            var runner = CreateRunner();

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(result.Status, Is.EqualTo(SaveMigrationStatus.FutureVersion));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Data, Is.Null);
        }

        [Test]
        public void Migrate_VersionZeroWithoutStepsIsUnsupported()
        {
            var runner = CreateRunner();

            SaveMigrationResult result = runner.Migrate(CreateProfile(0));

            Assert.That(result.Status, Is.EqualTo(SaveMigrationStatus.UnsupportedOlderVersion));
            Assert.That(result.Data, Is.Null);
        }

        [Test]
        public void Migrate_AppliesSequentialStepsToCopy()
        {
            var first = new FakeMigrationStep(-1, 0, data =>
            {
                data.SaveVersion = 0;
                data.ProfileId += "_zero";
                return data;
            });
            var second = new FakeMigrationStep(0, 1, data =>
            {
                data.SaveVersion = 1;
                data.ProfileId += "_one";
                return data;
            });
            ProfileSaveData source = CreateProfile(-1);
            var runner = CreateRunner(first, second);

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(result.Status, Is.EqualTo(SaveMigrationStatus.Success));
            Assert.That(result.Data.SaveVersion, Is.EqualTo(1));
            Assert.That(result.Data.ProfileId, Is.EqualTo("profile_source_zero_one"));
            Assert.That(first.CallCount, Is.EqualTo(1));
            Assert.That(second.CallCount, Is.EqualTo(1));
            Assert.That(source.SaveVersion, Is.EqualTo(-1));
            Assert.That(source.ProfileId, Is.EqualTo("profile_source"));
        }

        [Test]
        public void Migrate_MissingStepReturnsFailureWithoutPartialData()
        {
            var first = new FakeMigrationStep(-1, 0, data =>
            {
                data.SaveVersion = 0;
                data.ProfileId = "partially_changed";
                return data;
            });
            ProfileSaveData source = CreateProfile(-1);
            var runner = CreateRunner(first);

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(result.Status, Is.EqualTo(SaveMigrationStatus.MissingMigrationStep));
            Assert.That(result.Data, Is.Null);
            Assert.That(source.ProfileId, Is.EqualTo("profile_source"));
        }

        [Test]
        public void Constructor_RejectsDuplicateFromVersion()
        {
            var first = new FakeMigrationStep(0, 1, data => data);
            var duplicate = new FakeMigrationStep(0, 1, data => data);

            Assert.Throws<ArgumentException>(() => CreateRunner(first, duplicate));
        }

        [TestCase(0, 0)]
        [TestCase(0, 2)]
        [TestCase(1, 0)]
        public void Constructor_RejectsStepThatDoesNotAdvanceOneVersion(
            int fromVersion,
            int toVersion)
        {
            var step = new FakeMigrationStep(fromVersion, toVersion, data => data);

            Assert.Throws<ArgumentException>(() => CreateRunner(step));
        }

        [Test]
        public void Migrate_ConvertsStepExceptionToFailureResult()
        {
            var expected = new InvalidOperationException("migration failed");
            var step = new FakeMigrationStep(0, 1, _ => throw expected);
            ProfileSaveData source = CreateProfile(0);
            var runner = CreateRunner(step);

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(result.Status, Is.EqualTo(SaveMigrationStatus.MigrationFailed));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.Exception, Is.SameAs(expected));
            Assert.That(source.SaveVersion, Is.Zero);
            Assert.That(source.ProfileId, Is.EqualTo("profile_source"));
        }

        [Test]
        public void Migrate_RejectsStepResultWithUnexpectedVersion()
        {
            var step = new FakeMigrationStep(0, 1, data => data);
            var runner = CreateRunner(step);

            SaveMigrationResult result = runner.Migrate(CreateProfile(0));

            Assert.That(result.Status, Is.EqualTo(SaveMigrationStatus.MigrationFailed));
            Assert.That(result.Data, Is.Null);
        }

        [Test]
        public void Migrate_RejectsNullSource()
        {
            Assert.Throws<ArgumentNullException>(() => CreateRunner().Migrate(null));
        }

        private SaveMigrationRunner CreateRunner(params ISaveMigrationStep[] steps)
        {
            return new SaveMigrationRunner(cloner, steps);
        }

        private static ProfileSaveData CreateProfile(int version)
        {
            return new ProfileSaveData
            {
                SaveVersion = version,
                ProfileId = "profile_source",
                Characters = new List<CharacterSaveData>
                {
                    new CharacterSaveData { CharacterId = "character_a", Level = 10 }
                }
            };
        }

        private sealed class FakeMigrationStep : ISaveMigrationStep
        {
            private readonly Func<ProfileSaveData, ProfileSaveData> migrate;

            public FakeMigrationStep(
                int fromVersion,
                int toVersion,
                Func<ProfileSaveData, ProfileSaveData> migrate)
            {
                FromVersion = fromVersion;
                ToVersion = toVersion;
                this.migrate = migrate;
            }

            public int FromVersion { get; }
            public int ToVersion { get; }
            public int CallCount { get; private set; }

            public ProfileSaveData Migrate(ProfileSaveData source)
            {
                CallCount++;
                return migrate(source);
            }
        }
    }
}
