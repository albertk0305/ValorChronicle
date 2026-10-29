using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Pause;
using ValorChronicle.Localization;
using ValorChronicle.Settings.Services;
using ValorChronicle.UI.Navigation;
using ValorChronicle.UI.Audio;
using ValorChronicle.UI.Settings;
using Object = UnityEngine.Object;
using SettingsService = ValorChronicle.Settings.Services.SettingsService;

namespace ValorChronicle.Tests.EditMode.Settings
{
    public sealed class SettingsPanelTests
    {
        private const string PrefabPath =
            "Assets/Game/UI/Settings/SettingsPanel.prefab";

        private readonly List<GameObject> createdObjects = new List<GameObject>();

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
        public void Prefab_ConfiguresRedeemControlsAndFullPanelDismissOverlay()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            SettingsPanelView view = prefab.GetComponent<SettingsPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsRedeemConfigured, Is.True);
            Assert.That(view.RedeemCodeInputField, Is.Not.Null);
            Assert.That(view.RedeemConfirmButton, Is.Not.Null);
            Assert.That(view.RedeemResultOverlay.activeSelf, Is.False);
            Assert.That(view.RedeemResultOverlay.transform.parent,
                Is.SameAs(prefab.transform));
            Assert.That(view.RedeemResultOverlay.transform.GetSiblingIndex(),
                Is.EqualTo(prefab.transform.childCount - 1));

            var overlayRect = (RectTransform)view.RedeemResultOverlay.transform;
            Assert.That(overlayRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(overlayRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(overlayRect.sizeDelta, Is.EqualTo(Vector2.zero));

            Transform touch = view.RedeemResultOverlay.transform.Find(
                "TouchPanel");
            Assert.That(touch, Is.Not.Null);
            Assert.That(touch.GetComponent<Button>(), Is.Not.Null);
            Assert.That(touch.GetComponent<ButtonPressSfx>(), Is.Null);
            ButtonPressSfx[] buttonPressSfx =
                prefab.GetComponentsInChildren<ButtonPressSfx>(true);
            Assert.That(buttonPressSfx, Has.Length.EqualTo(4));
            Assert.That(
                buttonPressSfx,
                Has.All.Matches<ButtonPressSfx>(component =>
                    component.GetComponent<Button>() != null));
            Assert.That(touch.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(view.RedeemResultOverlay.transform.Find(
                "CodeSuccessPanel").GetComponent<Image>().raycastTarget,
                Is.False);
            Assert.That(view.RedeemResultText.raycastTarget, Is.False);

            LocalizedTMPText redeemLabel = null;
            foreach (LocalizedTMPText localized in
                prefab.GetComponentsInChildren<LocalizedTMPText>(true))
            {
                if (localized.gameObject.name == "RedeemCodeText")
                {
                    redeemLabel = localized;
                    break;
                }
            }

            Assert.That(redeemLabel, Is.Not.Null);
            Assert.That(redeemLabel.LocalizationKey,
                Is.EqualTo("ui.settings.redeem_code"));
        }

        [Test]
        public void Open_DisplaysCurrentSettingsWithoutCreatingChanges()
        {
            SettingsService service = SettingsService.CreateInMemoryDefaults();
            service.LoadOrCreate();
            service.SetMasterVolume(73);
            service.SetBgmVolume(62);
            service.SetSfxVolume(51);
            service.SetLocale(LocalizationCultureResolver.KoreanLocale);
            service.SetBattleSpeed(2);
            ViewFixture fixture = CreateView();
            using var presenter = new SettingsPanelPresenter(service, fixture.View);
            int changes = 0;
            service.SettingsChanged += _ => changes++;

            presenter.Open();

            Assert.That(fixture.Master.value, Is.EqualTo(73));
            Assert.That(fixture.Bgm.value, Is.EqualTo(62));
            Assert.That(fixture.Sfx.value, Is.EqualTo(51));
            Assert.That(fixture.MasterText.text, Is.EqualTo("73"));
            Assert.That(fixture.BgmText.text, Is.EqualTo("62"));
            Assert.That(fixture.SfxText.text, Is.EqualTo("51"));
            Assert.That(fixture.Language.value, Is.EqualTo(1));
            Assert.That(fixture.Speed2X.isOn, Is.True);
            Assert.That(fixture.Speed3X.isOn, Is.False);
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void UserInput_UpdatesSettingsAndExternalChangesRefreshView()
        {
            SettingsService service = SettingsService.CreateInMemoryDefaults();
            service.LoadOrCreate();
            ViewFixture fixture = CreateView();
            using var presenter = new SettingsPanelPresenter(service, fixture.View);
            presenter.Open();

            fixture.Master.value = 41f;
            fixture.Bgm.value = 32f;
            fixture.Sfx.value = 23f;
            fixture.Language.value = 1;
            fixture.Speed2X.isOn = true;

            Assert.That(service.Current.MasterVolume, Is.EqualTo(41));
            Assert.That(service.Current.BgmVolume, Is.EqualTo(32));
            Assert.That(service.Current.SfxVolume, Is.EqualTo(23));
            Assert.That(service.Current.LocaleId,
                Is.EqualTo(LocalizationCultureResolver.KoreanLocale));
            Assert.That(service.Current.BattleSpeed, Is.EqualTo(2));
            Assert.That(fixture.MasterText.text, Is.EqualTo("41"));

            service.SetMasterVolume(87);

            Assert.That(fixture.Master.value, Is.EqualTo(87));
            Assert.That(fixture.MasterText.text, Is.EqualTo("87"));

            fixture.Speed3X.isOn = true;
            Assert.That(fixture.Speed2X.isOn, Is.False);
            Assert.That(service.Current.BattleSpeed, Is.EqualTo(3));

            fixture.Speed2X.isOn = true;
            Assert.That(fixture.Speed3X.isOn, Is.False);
            Assert.That(service.Current.BattleSpeed, Is.EqualTo(2));

            fixture.Speed2X.isOn = false;
            Assert.That(service.Current.BattleSpeed, Is.EqualTo(1));
        }

        [Test]
        public void Reopen_DisplaysPersistedThreeTimesSelection()
        {
            SettingsService service = SettingsService.CreateInMemoryDefaults();
            service.LoadOrCreate();
            ViewFixture fixture = CreateView();
            using var presenter = new SettingsPanelPresenter(service, fixture.View);
            presenter.Open();
            fixture.Speed3X.isOn = true;

            fixture.Close.onClick.Invoke();
            presenter.Open();

            Assert.That(service.Current.BattleSpeed, Is.EqualTo(3));
            Assert.That(fixture.Speed2X.isOn, Is.False);
            Assert.That(fixture.Speed3X.isOn, Is.True);
        }

        [Test]
        public void Reopen_ReusesViewAndDoesNotDuplicateInputCallbacks()
        {
            SettingsService service = SettingsService.CreateInMemoryDefaults();
            service.LoadOrCreate();
            ViewFixture fixture = CreateView();
            using var presenter = new SettingsPanelPresenter(service, fixture.View);
            int callbacks = 0;
            fixture.View.MasterVolumeChanged += _ => callbacks++;

            presenter.Open();
            fixture.Close.onClick.Invoke();
            presenter.Open();
            fixture.Close.onClick.Invoke();
            presenter.Open();
            fixture.Master.value = 45f;

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(service.Current.MasterVolume, Is.EqualTo(45));
        }

        [Test]
        public void Close_FlushesDirtySettingsAndTheyReload()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "ValorChronicleSettingsPanelTests",
                Guid.NewGuid().ToString("N"));
            try
            {
                SettingsService service = SettingsSystemFactory.Create(root);
                service.LoadOrCreate();
                ViewFixture fixture = CreateView();
                using var presenter = new SettingsPanelPresenter(service, fixture.View);
                presenter.Open();
                fixture.Master.value = 29f;

                fixture.Close.onClick.Invoke();

                Assert.That(service.IsDirty, Is.False);
                SettingsService reloaded = SettingsSystemFactory.Create(root);
                reloaded.LoadOrCreate();
                Assert.That(reloaded.Current.MasterVolume, Is.EqualTo(29));
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [Test]
        public void Prefab_HasConfiguredViewAndFullScreenInputBlocker()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            SettingsPanelView view = prefab.GetComponent<SettingsPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsConfigured, Is.True);
            var serializedView = new SerializedObject(view);
            Toggle speed2X = (Toggle)serializedView
                .FindProperty("speedUp2XToggle").objectReferenceValue;
            Toggle speed3X = (Toggle)serializedView
                .FindProperty("speedUp3XToggle").objectReferenceValue;
            Assert.That(speed2X, Is.Not.Null);
            Assert.That(speed3X, Is.Not.Null);
            Assert.That(speed2X, Is.Not.SameAs(speed3X));
            Assert.That(speed2X.isOn, Is.False);
            Assert.That(speed3X.isOn, Is.False);

            RectTransform root = prefab.GetComponent<RectTransform>();
            Assert.That(root.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(root.anchorMax, Is.EqualTo(Vector2.one));
            Transform background = prefab.transform.Find("SettingBackground");
            Assert.That(background, Is.Not.Null);
            Assert.That(background.GetComponent<Image>().raycastTarget, Is.True);
        }

        [TestCase("Init")]
        [TestCase("Main")]
        [TestCase("Party")]
        [TestCase("Character")]
        [TestCase("Gacha")]
        public void GeneralScene_HasOneConfiguredSettingsHost(string sceneName)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                SettingsPanelHost[] hosts = FindComponents<SettingsPanelHost>(scene);
                Assert.That(hosts, Has.Length.EqualTo(1));
                var serialized = new SerializedObject(hosts[0]);
                Assert.That(serialized.FindProperty("settingButton").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(serialized.FindProperty("settingsPanelPrefab").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(serialized.FindProperty("panelParent").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serialized.FindProperty("pauseWhileOpen").boolValue,
                    Is.False);

                Button button = (Button)serialized.FindProperty("settingButton")
                    .objectReferenceValue;
                Assert.That(button.GetComponent<SceneNavigationButton>(), Is.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GachaSettingButton_EnablesAndReusesPanelAfterBootstrap()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/Gacha.unity",
                OpenSceneMode.Additive);
            GameBootstrapper previousBootstrapper = GameBootstrapper.Instance;
            GameObject bootstrapObject = null;
            SettingsPanelHost host = null;
            try
            {
                bootstrapObject = new GameObject("SettingsPanelTestBootstrap");
                var bootstrapper = bootstrapObject.AddComponent<GameBootstrapper>();
                SettingsService service = SettingsService.CreateInMemoryDefaults();
                service.LoadOrCreate();
                SetProperty(bootstrapper, "SettingsService", service);
                SetStaticProperty("Instance", bootstrapper);

                SettingsPanelHost[] hosts = FindComponents<SettingsPanelHost>(scene);
                Assert.That(hosts, Has.Length.EqualTo(1));
                host = hosts[0];
                var serializedHost = new SerializedObject(host);
                Button button = (Button)serializedHost
                    .FindProperty("settingButton").objectReferenceValue;

                InvokeLifecycle(host, "OnEnable");
                InvokeLifecycle(host, "Start");

                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
                SettingsPanelView[] panels =
                    FindComponents<SettingsPanelView>(scene);
                Assert.That(panels, Has.Length.EqualTo(1));
                SettingsPanelView panel = panels[0];
                Assert.That(panel.IsOpen, Is.True);

                InvokeLifecycle(panel, "OnEnable");
                var serializedPanel = new SerializedObject(panel);
                Button closeButton = (Button)serializedPanel
                    .FindProperty("closeButton").objectReferenceValue;
                closeButton.onClick.Invoke();
                Assert.That(panel.IsOpen, Is.False);

                button.onClick.Invoke();
                Assert.That(FindComponents<SettingsPanelView>(scene),
                    Has.Length.EqualTo(1));
                Assert.That(panel.IsOpen, Is.True);
            }
            finally
            {
                if (host != null)
                {
                    InvokeLifecycle(host, "OnDisable");
                    InvokeLifecycle(host, "OnDestroy");
                }

                if (bootstrapObject != null)
                    Object.DestroyImmediate(bootstrapObject);
                SetStaticProperty("Instance", previousBootstrapper);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BattleScene_HasConfiguredPauseAwareSettingsHost()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/Battle.unity",
                OpenSceneMode.Additive);
            try
            {
                GameObject settingButton = FindGameObject(scene, "SettingButton");
                Assert.That(settingButton, Is.Not.Null);
                Button button = settingButton.GetComponent<Button>();
                Assert.That(button.interactable, Is.True);
                Assert.That(button.onClick.GetPersistentEventCount(), Is.Zero);
                Assert.That(settingButton.GetComponent<SceneNavigationButton>(), Is.Null);
                SettingsPanelHost[] hosts =
                    FindComponents<SettingsPanelHost>(scene);
                Assert.That(hosts, Has.Length.EqualTo(1));
                var serializedHost = new SerializedObject(hosts[0]);
                Assert.That(serializedHost.FindProperty("settingButton")
                    .objectReferenceValue, Is.SameAs(button));
                Assert.That(serializedHost.FindProperty("settingsPanelPrefab")
                    .objectReferenceValue, Is.Not.Null);
                Assert.That(serializedHost.FindProperty("panelParent")
                    .objectReferenceValue, Is.Not.Null);
                Assert.That(serializedHost.FindProperty("pauseWhileOpen")
                    .boolValue, Is.True);

                BattleSettingsPanelBinding[] bindings =
                    FindComponents<BattleSettingsPanelBinding>(scene);
                Assert.That(bindings, Has.Length.EqualTo(1));
                Assert.That(bindings[0].IsConfigured, Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BattleSettingsPanel_PausesOnceFlushesAndPreservesOtherPause()
        {
            string settingsRoot = Path.Combine(
                Path.GetTempPath(),
                "ValorChronicleBattleSettingsPanelTests",
                Guid.NewGuid().ToString("N"));
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/Battle.unity",
                OpenSceneMode.Additive);
            GameBootstrapper previousBootstrapper = GameBootstrapper.Instance;
            GameObject bootstrapObject = null;
            SettingsPanelHost host = null;
            IDisposable externalPause = null;
            try
            {
                float timeScale = 1.25f;
                var pauseService = new GamePauseService(
                    () => timeScale,
                    value => timeScale = value);
                SettingsService settingsService =
                    SettingsSystemFactory.Create(settingsRoot);
                settingsService.LoadOrCreate();
                bootstrapObject = new GameObject(
                    "BattleSettingsPanelTestBootstrap");
                var bootstrapper =
                    bootstrapObject.AddComponent<GameBootstrapper>();
                SetProperty(bootstrapper, "SettingsService", settingsService);
                SetProperty(bootstrapper, "GamePauseService", pauseService);
                SetStaticProperty("Instance", bootstrapper);

                host = FindComponents<SettingsPanelHost>(scene)[0];
                var serializedHost = new SerializedObject(host);
                Button button = (Button)serializedHost
                    .FindProperty("settingButton").objectReferenceValue;
                InvokeLifecycle(host, "OnEnable");
                InvokeLifecycle(host, "Start");
                Assert.That(button.interactable, Is.True);

                BattleSettingsPanelBinding binding =
                    FindComponents<BattleSettingsPanelBinding>(scene)[0];
                InvokeLifecycle(binding, "OnEnable");
                InvokeLifecycle(binding, "Start");
                Assert.That(timeScale, Is.EqualTo(0.5f));

                externalPause = pauseService.RequestPause();
                button.onClick.Invoke();
                SettingsPanelView[] panels =
                    FindComponents<SettingsPanelView>(scene);
                Assert.That(panels, Has.Length.EqualTo(1));
                SettingsPanelView panel = panels[0];
                Assert.That(panel.IsOpen, Is.True);
                Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(2));
                Assert.That(timeScale, Is.Zero);

                settingsService.SetBattleSpeed(3);
                Assert.That(timeScale, Is.Zero);

                button.onClick.Invoke();
                Assert.That(FindComponents<SettingsPanelView>(scene),
                    Has.Length.EqualTo(1));
                Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(2));

                InvokeLifecycle(panel, "OnEnable");
                var serializedPanel = new SerializedObject(panel);
                Slider master = (Slider)serializedPanel
                    .FindProperty("masterVolumeSlider").objectReferenceValue;
                Button close = (Button)serializedPanel
                    .FindProperty("closeButton").objectReferenceValue;
                master.value = 29f;
                Assert.That(settingsService.IsDirty, Is.True);

                close.onClick.Invoke();

                Assert.That(panel.IsOpen, Is.False);
                Assert.That(settingsService.IsDirty, Is.False);
                Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
                Assert.That(pauseService.IsPaused, Is.True);
                Assert.That(timeScale, Is.Zero);

                externalPause.Dispose();
                externalPause = null;
                Assert.That(pauseService.IsPaused, Is.False);
                Assert.That(timeScale, Is.EqualTo(1.5f));

                button.onClick.Invoke();
                Assert.That(panel.IsOpen, Is.True);
                Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
                InvokeLifecycle(host, "OnDisable");
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(pauseService.IsPaused, Is.False);
                Assert.That(timeScale, Is.EqualTo(1.5f));

                InvokeLifecycle(host, "OnEnable");
                button.onClick.Invoke();
                Assert.That(panel.IsOpen, Is.True);
                Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
                InvokePrivate(
                    binding,
                    "HandleBattleResultReached",
                    BattleResultKind.Victory);

                Assert.That(panel.IsOpen, Is.False);
                Assert.That(pauseService.IsPaused, Is.False);
                Assert.That(button.interactable, Is.False);
                Assert.That(timeScale, Is.EqualTo(1f));

                settingsService.SetBattleSpeed(2);
                Assert.That(timeScale, Is.EqualTo(1f));

                button.onClick.Invoke();
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(pauseService.IsPaused, Is.False);
                Assert.That(FindComponents<SettingsPanelView>(scene),
                    Has.Length.EqualTo(1));
            }
            finally
            {
                externalPause?.Dispose();
                if (host != null)
                {
                    InvokeLifecycle(host, "OnDisable");
                    InvokeLifecycle(host, "OnDestroy");
                }

                if (bootstrapObject != null)
                    Object.DestroyImmediate(bootstrapObject);
                SetStaticProperty("Instance", previousBootstrapper);
                EditorSceneManager.CloseScene(scene, true);
                if (Directory.Exists(settingsRoot))
                    Directory.Delete(settingsRoot, true);
            }
        }

        private ViewFixture CreateView()
        {
            // EditMode does not reproduce the PlayMode OnEnable callback when
            // this fixture is activated, so configure it while it is active.
            var root = new GameObject("SettingsPanelTest");
            createdObjects.Add(root);
            var view = root.AddComponent<SettingsPanelView>();
            Slider master = CreateChild<Slider>(root, "Master");
            Slider bgm = CreateChild<Slider>(root, "Bgm");
            Slider sfx = CreateChild<Slider>(root, "Sfx");
            TMP_Text masterText = CreateChild<TextMeshProUGUI>(root, "MasterText");
            TMP_Text bgmText = CreateChild<TextMeshProUGUI>(root, "BgmText");
            TMP_Text sfxText = CreateChild<TextMeshProUGUI>(root, "SfxText");
            TMP_Dropdown language = CreateChild<TMP_Dropdown>(root, "Language");
            language.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("English"),
                new TMP_Dropdown.OptionData("한국어")
            };
            Toggle speed2X = CreateChild<Toggle>(root, "Speed2X");
            Toggle speed3X = CreateChild<Toggle>(root, "Speed3X");
            Button close = CreateChild<Button>(root, "Close");
            view.Configure(
                master,
                bgm,
                sfx,
                masterText,
                bgmText,
                sfxText,
                language,
                speed2X,
                speed3X,
                close);

            return new ViewFixture(
                view, master, bgm, sfx, masterText, bgmText, sfxText,
                language, speed2X, speed3X, close);
        }

        private static T CreateChild<T>(GameObject parent, string name)
            where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        private static T[] FindComponents<T>(Scene scene)
            where T : Component
        {
            var results = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                results.AddRange(root.GetComponentsInChildren<T>(true));
            return results.ToArray();
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

        private static void InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, arguments);
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = property?.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null, propertyName);
            setter.Invoke(target, new[] { value });
        }

        private static void SetStaticProperty(
            string propertyName,
            object value)
        {
            PropertyInfo property = typeof(GameBootstrapper).GetProperty(
                propertyName,
                BindingFlags.Static | BindingFlags.Public);
            MethodInfo setter = property?.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null, propertyName);
            setter.Invoke(null, new[] { value });
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == name)
                        return candidate.gameObject;
                }
            }

            return null;
        }

        private sealed class ViewFixture
        {
            public ViewFixture(
                SettingsPanelView view,
                Slider master,
                Slider bgm,
                Slider sfx,
                TMP_Text masterText,
                TMP_Text bgmText,
                TMP_Text sfxText,
                TMP_Dropdown language,
                Toggle speed2X,
                Toggle speed3X,
                Button close)
            {
                View = view;
                Master = master;
                Bgm = bgm;
                Sfx = sfx;
                MasterText = masterText;
                BgmText = bgmText;
                SfxText = sfxText;
                Language = language;
                Speed2X = speed2X;
                Speed3X = speed3X;
                Close = close;
            }

            public SettingsPanelView View { get; }
            public Slider Master { get; }
            public Slider Bgm { get; }
            public Slider Sfx { get; }
            public TMP_Text MasterText { get; }
            public TMP_Text BgmText { get; }
            public TMP_Text SfxText { get; }
            public TMP_Dropdown Language { get; }
            public Toggle Speed2X { get; }
            public Toggle Speed3X { get; }
            public Button Close { get; }
        }
    }
}
