using System;
using System.Collections.Generic;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Processing;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    internal sealed class FakeSaveRepository : ISaveRepository
    {
        public string MainText { get; set; }
        public string BackupText { get; set; }
        public string TempText { get; set; }
        public string TempReadOverride { get; set; }
        public bool FailReadMain { get; set; }
        public bool FailReadBackup { get; set; }
        public bool FailReadTemp { get; set; }
        public bool FailWriteTemp { get; set; }
        public bool FailCopyMainToBackup { get; set; }
        public bool FailPromoteTempToMain { get; set; }
        public bool FailDeleteTemp { get; set; }
        public List<string> Calls { get; } = new List<string>();

        public bool MainExists => MainText != null;
        public bool BackupExists => BackupText != null;
        public bool TempExists => TempText != null;

        public int Count(string operation) => Calls.FindAll(value => value == operation).Count;

        public void EnsureRootDirectory() => Calls.Add(nameof(EnsureRootDirectory));

        public string ReadMain()
        {
            Calls.Add(nameof(ReadMain));
            if (FailReadMain) throw new InvalidOperationException("Injected main read failure.");
            return MainText;
        }

        public string ReadBackup()
        {
            Calls.Add(nameof(ReadBackup));
            if (FailReadBackup) throw new InvalidOperationException("Injected backup read failure.");
            return BackupText;
        }

        public string ReadTemp()
        {
            Calls.Add(nameof(ReadTemp));
            if (FailReadTemp) throw new InvalidOperationException("Injected temp read failure.");
            return TempReadOverride ?? TempText;
        }

        public void WriteTemp(string contents)
        {
            Calls.Add(nameof(WriteTemp));
            if (FailWriteTemp) throw new InvalidOperationException("Injected temp write failure.");
            TempText = contents;
        }

        public void CopyMainToBackup()
        {
            Calls.Add(nameof(CopyMainToBackup));
            if (FailCopyMainToBackup) throw new InvalidOperationException("Injected backup rotation failure.");
            BackupText = MainText;
        }

        public void PromoteTempToMain()
        {
            Calls.Add(nameof(PromoteTempToMain));
            if (FailPromoteTempToMain) throw new InvalidOperationException("Injected promote failure.");
            MainText = TempText;
            TempText = null;
        }

        public void DeleteTempIfExists()
        {
            Calls.Add(nameof(DeleteTempIfExists));
            if (FailDeleteTemp) throw new InvalidOperationException("Injected temp delete failure.");
            TempText = null;
        }
    }

    internal sealed class FixedUnixTimeProvider : IUnixTimeProvider
    {
        private readonly Queue<long> values;
        private long lastValue;

        public FixedUnixTimeProvider(params long[] values)
        {
            if (values == null || values.Length == 0) throw new ArgumentException("At least one time value is required.", nameof(values));
            this.values = new Queue<long>(values);
            lastValue = values[values.Length - 1];
        }

        public int CallCount { get; private set; }

        public long GetUtcUnixTimeSeconds()
        {
            CallCount++;
            if (values.Count > 0) lastValue = values.Dequeue();
            return lastValue;
        }
    }

    internal static class SaveTestDataBuilder
    {
        public static ProfileSaveData Valid(string profileId = "profile_test", long timestamp = 10)
        {
            return new NewProfileFactory().Create(profileId, timestamp);
        }

        public static ProfileSaveData FatalDuplicateCharacter()
        {
            ProfileSaveData profile = Valid();
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero_a", Level = 1 });
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero_a", Level = 1 });
            return profile;
        }

        public static ProfileSaveData RepairableNegativeCurrency()
        {
            ProfileSaveData profile = Valid();
            profile.Currencies.GachaCurrency = -10;
            return profile;
        }

        public static string Json(ProfileSaveData profile) => new NewtonsoftJsonSaveSerializer().Serialize(profile);
        public static string ValidJson(string profileId = "profile_test", long timestamp = 10) => Json(Valid(profileId, timestamp));
        public static string CorruptJson() => "{not-json";

        public static string FutureJson()
        {
            ProfileSaveData profile = Valid();
            profile.SaveVersion++;
            return Json(profile);
        }

        public static string UnsupportedOlderJson()
        {
            ProfileSaveData profile = Valid();
            profile.SaveVersion = 0;
            return Json(profile);
        }
    }

    internal static class SaveServiceTestFactory
    {
        public static SaveService Create(
            FakeSaveRepository repository,
            FixedUnixTimeProvider timeProvider = null)
        {
            return Create((ISaveRepository)repository, timeProvider ?? new FixedUnixTimeProvider(100));
        }

        public static SaveService Create(ISaveRepository repository, IUnixTimeProvider timeProvider)
        {
            var cloner = new SaveDataCloner();
            return new SaveService(
                repository,
                new NewtonsoftJsonSaveSerializer(),
                new NewProfileFactory(),
                cloner,
                new SaveMigrationRunner(cloner, Array.Empty<ISaveMigrationStep>()),
                new SaveValidationProcessor(),
                new FakeSaveContentCatalog(),
                timeProvider);
        }
    }
}
