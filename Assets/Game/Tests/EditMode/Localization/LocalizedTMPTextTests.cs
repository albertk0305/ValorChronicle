using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Localization;
using ValorChronicle.Settings.Integration;
using ValorChronicle.Settings.Services;
using Object = UnityEngine.Object;
using SettingsService = ValorChronicle.Settings.Services.SettingsService;

namespace ValorChronicle.Tests.EditMode.Localization
{
    public sealed class LocalizedTMPTextTests
    {
        private const string PrefabPath =
            "Assets/Game/UI/Settings/SettingsPanel.prefab";
        private const string CatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";

        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                    Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void ActiveText_AppliesCurrentLocaleAndRefreshesOnChanges()
        {
            LocalizationService service = CreateService();
            LocalizedTMPText binding = CreateBinding(
                "label",
                active: true,
                out TMP_Text text);

            binding.Initialize(service);
            Assert.That(text.text, Is.EqualTo("Settings"));

            Assert.That(service.SetLocale("ko"), Is.True);
            Assert.That(text.text, Is.EqualTo("설정"));

            text.text = "unchanged";
            Assert.That(service.SetLocale("ko"), Is.False);
            Assert.That(text.text, Is.EqualTo("unchanged"));

            Assert.That(service.SetLocale("en"), Is.True);
            Assert.That(text.text, Is.EqualTo("Settings"));
        }

        [Test]
        public void InactiveText_AppliesLatestLocaleWhenReenabled()
        {
            LocalizationService service = CreateService();
            LocalizedTMPText binding = CreateBinding(
                "label",
                active: false,
                out TMP_Text text);
            binding.Initialize(service);
            service.SetLocale("ko");

            binding.gameObject.SetActive(true);
            InvokeLifecycle(binding, "OnEnable");
            Assert.That(text.text, Is.EqualTo("설정"));
            Assert.That(SubscriberCount(service), Is.EqualTo(1));

            binding.gameObject.SetActive(false);
            InvokeLifecycle(binding, "OnDisable");
            Assert.That(SubscriberCount(service), Is.Zero);
            service.SetLocale("en");

            binding.gameObject.SetActive(true);
            InvokeLifecycle(binding, "OnEnable");
            Assert.That(text.text, Is.EqualTo("Settings"));
            Assert.That(SubscriberCount(service), Is.EqualTo(1));
        }

        [Test]
        public void Destroy_UnsubscribesLocaleChanged()
        {
            LocalizationService service = CreateService();
            LocalizedTMPText binding = CreateBinding(
                "label",
                active: true,
                out _);
            binding.Initialize(service);
            Assert.That(SubscriberCount(service), Is.EqualTo(1));

            createdObjects.Remove(binding.gameObject);
            InvokeLifecycle(binding, "OnDestroy");
            Object.DestroyImmediate(binding.gameObject);

            Assert.That(SubscriberCount(service), Is.Zero);
            Assert.DoesNotThrow(() => service.SetLocale("ko"));
        }

        [Test]
        public void MissingKeyAndDependencies_AreHandledSafely()
        {
            LocalizationService service = CreateService();
            LocalizedTMPText missingKey = CreateBinding(
                "unknown",
                active: true,
                out TMP_Text missingKeyText);
            missingKey.Initialize(service);
            Assert.That(missingKeyText.text, Is.EqualTo("[[unknown]]"));

            var noTextObject = new GameObject("NoTextBinding");
            createdObjects.Add(noTextObject);
            LocalizedTMPText noText = noTextObject
                .AddComponent<LocalizedTMPText>();
            SetSerialized(noText, null, "label");
            Assert.DoesNotThrow(() => noText.Initialize(service));
            Assert.DoesNotThrow(() => service.SetLocale("ko"));

            LocalizedTMPText noService = CreateBinding(
                "label",
                active: true,
                out _);
            Assert.DoesNotThrow(() => InvokeLifecycle(noService, "Start"));
        }

        [Test]
        public void SettingsPanel_LocalizesImmediatelyAndOnReopen()
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(CatalogPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            createdObjects.Add(instance);
            LocalizationService localization = catalog.CreateService();
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            settings.LoadOrCreate();
            using var localeBinding = new SettingsLocaleBinding(
                settings,
                localization);

            LocalizedTMPText[] bindings = instance
                .GetComponentsInChildren<LocalizedTMPText>(true);
            Assert.That(bindings, Has.Length.EqualTo(3));
            InitializeAll(bindings, localization);
            AssertLabels(
                bindings,
                "Master Volume",
                "BGM",
                "SFX");

            settings.SetLocale("ko");
            AssertLabels(bindings, "전체 음량", "배경 음악", "효과음");

            instance.SetActive(false);
            InvokeLifecycle(bindings, "OnDisable");
            settings.SetLocale("en");
            AssertLabels(bindings, "전체 음량", "배경 음악", "효과음");

            instance.SetActive(true);
            InvokeLifecycle(bindings, "OnEnable");
            AssertLabels(
                bindings,
                "Master Volume",
                "BGM",
                "SFX");
        }

        [Test]
        public void ReloadedLocale_IsAppliedWhenSettingsPanelIsCreated()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "ValorChronicleLocalizedTMPTextTests",
                Guid.NewGuid().ToString("N"));
            try
            {
                SettingsService writer = SettingsSystemFactory.Create(root);
                writer.LoadOrCreate();
                writer.SetLocale("ko");
                Assert.That(writer.Flush().IsSuccess, Is.True);

                SettingsService loaded = SettingsSystemFactory.Create(root);
                Assert.That(
                    loaded.LoadOrCreate().Status,
                    Is.EqualTo(SettingsLoadStatus.LoadedMain));
                LocalizationService localization = AssetDatabase
                    .LoadAssetAtPath<LocalizationCatalog>(CatalogPath)
                    .CreateService();
                using var localeBinding = new SettingsLocaleBinding(
                    loaded,
                    localization);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabPath);
                GameObject instance = Object.Instantiate(prefab);
                createdObjects.Add(instance);
                LocalizedTMPText[] bindings = instance
                    .GetComponentsInChildren<LocalizedTMPText>(true);

                InitializeAll(bindings, localization);

                Assert.That(localization.CurrentLocale, Is.EqualTo("ko"));
                AssertLabels(bindings, "전체 음량", "배경 음악", "효과음");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private LocalizedTMPText CreateBinding(
            string key,
            bool active,
            out TMP_Text text)
        {
            var gameObject = new GameObject("LocalizedText");
            createdObjects.Add(gameObject);
            gameObject.SetActive(false);
            text = gameObject.AddComponent<TextMeshProUGUI>();
            LocalizedTMPText binding = gameObject
                .AddComponent<LocalizedTMPText>();
            SetSerialized(binding, text, key);
            gameObject.SetActive(active);
            return binding;
        }

        private static void SetSerialized(
            LocalizedTMPText binding,
            TMP_Text text,
            string key)
        {
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("targetText").objectReferenceValue = text;
            serialized.FindProperty("localizationKey").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LocalizationService CreateService()
        {
            LocalizationTable table = LocalizationTableParser.Parse(
                "localized-text-test.tsv",
                "key\ten\tko\nlabel\tSettings\t설정");
            return new LocalizationService(new[] { table });
        }

        private static void InitializeAll(
            IEnumerable<LocalizedTMPText> bindings,
            LocalizationService service)
        {
            foreach (LocalizedTMPText binding in bindings)
                binding.Initialize(service);
        }

        private static void AssertLabels(
            IEnumerable<LocalizedTMPText> bindings,
            string master,
            string bgm,
            string sfx)
        {
            var expected = new Dictionary<string, string>
            {
                ["ui.settings.master_volume"] = master,
                ["ui.settings.bgm_volume"] = bgm,
                ["ui.settings.sfx_volume"] = sfx
            };

            foreach (LocalizedTMPText binding in bindings)
            {
                Assert.That(expected, Contains.Key(binding.LocalizationKey));
                Assert.That(
                    binding.TargetText.text,
                    Is.EqualTo(expected[binding.LocalizationKey]));
            }
        }

        private static int SubscriberCount(LocalizationService service)
        {
            FieldInfo field = typeof(LocalizationService).GetField(
                "LocaleChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var subscribers = field.GetValue(service) as Delegate;
            return subscribers?.GetInvocationList().Length ?? 0;
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static void InvokeLifecycle(
            IEnumerable<LocalizedTMPText> bindings,
            string methodName)
        {
            foreach (LocalizedTMPText binding in bindings)
                InvokeLifecycle(binding, methodName);
        }
    }
}
