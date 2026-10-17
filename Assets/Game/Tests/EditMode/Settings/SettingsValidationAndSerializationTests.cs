using NUnit.Framework;
using ValorChronicle.Localization;
using ValorChronicle.Settings.DTO;
using ValorChronicle.Settings.Serialization;
using ValorChronicle.Settings.Validation;

namespace ValorChronicle.Tests.EditMode.Settings
{
    public sealed class SettingsValidationAndSerializationTests
    {
        [Test]
        public void Defaults_MatchVersionOneContract()
        {
            GameSettingsData settings = GameSettingsData.CreateDefault();

            Assert.That(settings.SchemaVersion, Is.EqualTo(1));
            Assert.That(settings.MasterVolume, Is.EqualTo(100));
            Assert.That(settings.BgmVolume, Is.EqualTo(100));
            Assert.That(settings.SfxVolume, Is.EqualTo(100));
            Assert.That(settings.LocaleId,
                Is.EqualTo(LocalizationService.EnglishLocale));
            Assert.That(settings.BattleSpeed, Is.EqualTo(1));
        }

        [Test]
        public void Validator_NormalizesSupportedSchemaWithoutMutatingSource()
        {
            var source = new GameSettingsData
            {
                MasterVolume = -20,
                BgmVolume = 101,
                SfxVolume = 45,
                LocaleId = "unsupported",
                BattleSpeed = 99
            };

            SettingsValidationResult result =
                new SettingsValidator().Validate(source);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.WasModified, Is.True);
            Assert.That(result.Settings.MasterVolume, Is.Zero);
            Assert.That(result.Settings.BgmVolume, Is.EqualTo(100));
            Assert.That(result.Settings.SfxVolume, Is.EqualTo(45));
            Assert.That(result.Settings.LocaleId, Is.EqualTo("en"));
            Assert.That(result.Settings.BattleSpeed, Is.EqualTo(1));
            Assert.That(source.MasterVolume, Is.EqualTo(-20));
        }

        [Test]
        public void Validator_RejectsUnsupportedSchema()
        {
            var source = new GameSettingsData { SchemaVersion = 2 };

            SettingsValidationResult result =
                new SettingsValidator().Validate(source);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Settings, Is.Null);
        }

        [TestCase(0, 0)]
        [TestCase(100, 100)]
        [TestCase(-1, 0)]
        [TestCase(101, 100)]
        public void VolumeNormalization_HandlesBoundariesAndOverflow(
            int source,
            int expected)
        {
            Assert.That(SettingsValidator.NormalizeVolume(source),
                Is.EqualTo(expected));
        }

        [TestCase("en", "en")]
        [TestCase("ko", "ko")]
        [TestCase("ja", "en")]
        [TestCase(null, "en")]
        public void LocaleNormalization_UsesExistingIdsAndEnglishFallback(
            string source,
            string expected)
        {
            Assert.That(SettingsValidator.NormalizeLocaleId(source),
                Is.EqualTo(expected));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(0, 1)]
        [TestCase(4, 1)]
        public void BattleSpeedNormalization_AllowsOneTwoAndThree(
            int source,
            int expected)
        {
            Assert.That(SettingsValidator.NormalizeBattleSpeed(source),
                Is.EqualTo(expected));
        }

        [Test]
        public void Serializer_UsesPlainJsonWithoutTypeMetadata()
        {
            var serializer = new NewtonsoftJsonSettingsSerializer();

            string json = serializer.Serialize(GameSettingsData.CreateDefault());
            GameSettingsData roundTrip = serializer.Deserialize(json);

            Assert.That(json, Does.Not.Contain("$type"));
            Assert.That(roundTrip.SchemaVersion, Is.EqualTo(1));
        }

        [Test]
        public void Serializer_MissingMembersRetainDtoDefaults()
        {
            var serializer = new NewtonsoftJsonSettingsSerializer();

            GameSettingsData data = serializer.Deserialize(
                "{\"SchemaVersion\":1,\"LocaleId\":\"ko\"}");

            Assert.That(data.MasterVolume, Is.EqualTo(100));
            Assert.That(data.BgmVolume, Is.EqualTo(100));
            Assert.That(data.SfxVolume, Is.EqualTo(100));
            Assert.That(data.LocaleId, Is.EqualTo("ko"));
            Assert.That(data.BattleSpeed, Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Serializer_RoundTripsSupportedBattleSpeed(int speed)
        {
            var serializer = new NewtonsoftJsonSettingsSerializer();
            var source = GameSettingsData.CreateDefault();
            source.BattleSpeed = speed;

            GameSettingsData data = serializer.Deserialize(
                serializer.Serialize(source));

            Assert.That(data.BattleSpeed, Is.EqualTo(speed));
        }
    }
}
