using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Main.Presentation;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class MainGoldFormatterTests
    {
        [TestCase(0L, "0")]
        [TestCase(100L, "100")]
        [TestCase(999L, "999")]
        [TestCase(1000L, "1,000")]
        [TestCase(1234567L, "1,234,567")]
        [TestCase(100000000L, "100,000,000")]
        [TestCase(long.MaxValue, "9,223,372,036,854,775,807")]
        public void FormatGold_UsesInvariantThousandsSeparators(
            long value,
            string expected)
        {
            Assert.That(
                MainScreenController.FormatGold(value),
                Is.EqualTo(expected));
        }

        [Test]
        public void FormatGold_DoesNotMutateProfileCurrency()
        {
            var profile = new ProfileSaveData
            {
                Currencies = new CurrencySaveData
                {
                    GachaCurrency = 1234567L
                }
            };

            string formatted = MainScreenController.FormatGold(
                profile.Currencies.GachaCurrency);

            Assert.That(formatted, Is.EqualTo("1,234,567"));
            Assert.That(profile.Currencies.GachaCurrency,
                Is.EqualTo(1234567L));
        }

        [Test]
        public void RefreshGoldDisplay_MissingTextIsSafeAndLogsError()
        {
            var root = new GameObject("MainScreenControllerTest");
            root.SetActive(false);
            MainScreenController controller =
                root.AddComponent<MainScreenController>();
            LogAssert.Expect(
                LogType.Error,
                new Regex("GoldText is not configured"));

            Assert.DoesNotThrow(controller.RefreshGoldDisplay);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void RefreshGoldDisplay_MissingBootstrapShowsUnavailableText()
        {
            var root = new GameObject("MainScreenControllerTest");
            root.SetActive(false);
            MainScreenController controller =
                root.AddComponent<MainScreenController>();
            TMP_Text goldText = new GameObject(
                "GoldText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            goldText.transform.SetParent(root.transform, false);
            SetField(controller, "goldText", goldText);
            GameBootstrapper previous = GameBootstrapper.Instance;
            SetProperty(
                typeof(GameBootstrapper),
                null,
                nameof(GameBootstrapper.Instance),
                null);
            LogAssert.Expect(
                LogType.Error,
                new Regex("GameBootstrapper is missing"));

            try
            {
                Assert.DoesNotThrow(controller.RefreshGoldDisplay);
                Assert.That(goldText.text, Is.EqualTo("-"));
            }
            finally
            {
                SetProperty(
                    typeof(GameBootstrapper),
                    null,
                    nameof(GameBootstrapper.Instance),
                    previous);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshGoldDisplay_MissingSaveServiceIsSafe()
        {
            var root = new GameObject("MainScreenControllerTest");
            root.SetActive(false);
            MainScreenController controller =
                root.AddComponent<MainScreenController>();
            TMP_Text goldText = new GameObject(
                "GoldText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            goldText.transform.SetParent(root.transform, false);
            SetField(controller, "goldText", goldText);

            var bootstrapRoot = new GameObject("GameBootstrapperTest");
            bootstrapRoot.SetActive(false);
            GameBootstrapper bootstrapper =
                bootstrapRoot.AddComponent<GameBootstrapper>();
            GameBootstrapper previous = GameBootstrapper.Instance;
            SetProperty(
                typeof(GameBootstrapper),
                null,
                nameof(GameBootstrapper.Instance),
                bootstrapper);
            LogAssert.Expect(
                LogType.Error,
                new Regex("SaveService is unavailable"));

            try
            {
                Assert.DoesNotThrow(controller.RefreshGoldDisplay);
                Assert.That(goldText.text, Is.EqualTo("-"));
            }
            finally
            {
                SetProperty(
                    typeof(GameBootstrapper),
                    null,
                    nameof(GameBootstrapper.Instance),
                    previous);
                Object.DestroyImmediate(bootstrapRoot);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshGoldDisplay_RequeriesLatestProfileSnapshot()
        {
            var root = new GameObject("MainScreenControllerTest");
            root.SetActive(false);
            MainScreenController controller =
                root.AddComponent<MainScreenController>();
            TMP_Text goldText = new GameObject(
                "GoldText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            goldText.transform.SetParent(root.transform, false);
            SetField(controller, "goldText", goldText);

            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.GachaCurrency = 1000L;
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository);
            Assert.That(saveService.LoadOrCreate("ignored").CanUseProfile,
                Is.True);

            var bootstrapRoot = new GameObject("GameBootstrapperTest");
            bootstrapRoot.SetActive(false);
            GameBootstrapper bootstrapper =
                bootstrapRoot.AddComponent<GameBootstrapper>();
            SetProperty(
                typeof(GameBootstrapper),
                bootstrapper,
                nameof(GameBootstrapper.SaveService),
                saveService);
            GameBootstrapper previous = GameBootstrapper.Instance;
            SetProperty(
                typeof(GameBootstrapper),
                null,
                nameof(GameBootstrapper.Instance),
                bootstrapper);

            try
            {
                controller.RefreshGoldDisplay();
                Assert.That(goldText.text, Is.EqualTo("1,000"));

                Assert.That(saveService.ExecuteTransaction(
                    current => current.Currencies.GachaCurrency = 999L)
                    .IsSuccess, Is.True);
                controller.RefreshGoldDisplay();

                Assert.That(goldText.text, Is.EqualTo("999"));
                Assert.That(saveService.GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(999L));
            }
            finally
            {
                SetProperty(
                    typeof(GameBootstrapper),
                    null,
                    nameof(GameBootstrapper.Instance),
                    previous);
                Object.DestroyImmediate(bootstrapRoot);
                Object.DestroyImmediate(root);
            }
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void SetProperty(
            System.Type type,
            object target,
            string propertyName,
            object value)
        {
            var property = type.GetProperty(
                propertyName,
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.Public);
            Assert.That(property, Is.Not.Null, propertyName);
            property.SetValue(target, value);
        }
    }
}
