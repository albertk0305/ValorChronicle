using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Localization;
using ValorChronicle.Redeem;
using ValorChronicle.Settings.Services;
using ValorChronicle.UI.Settings;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode.Settings
{
    public sealed class RedeemCodePresenterTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                    Object.DestroyImmediate(created[index]);
            }

            created.Clear();
        }

        [Test]
        public void Open_ResetsOverlayAndLocalizesPlaceholder()
        {
            Fixture fixture = CreateFixture();
            fixture.View.ShowRedeemResult("old result");

            fixture.Presenter.Open();

            Assert.That(fixture.View.IsRedeemResultVisible, Is.False);
            Assert.That(fixture.Input.interactable, Is.True);
            Assert.That(fixture.Confirm.interactable, Is.True);
            Assert.That(((TMP_Text)fixture.Input.placeholder).text,
                Is.EqualTo("Enter a redeem code"));
        }

        [Test]
        public void Confirm_SucceedsOnceUntilOverlayIsDismissed()
        {
            Fixture fixture = CreateFixture();
            fixture.Service.Result = Success();
            fixture.Presenter.Open();
            fixture.Input.text = "NEAT3";

            fixture.Confirm.onClick.Invoke();
            fixture.Confirm.onClick.Invoke();

            Assert.That(fixture.Service.CallCount, Is.EqualTo(1));
            Assert.That(fixture.View.IsRedeemResultVisible, Is.True);
            Assert.That(fixture.ResultText.text,
                Is.EqualTo("Code redeemed successfully!\nGold +10,000"));
            Assert.That(fixture.Confirm.interactable, Is.False);

            fixture.Dismiss.onClick.Invoke();
            Assert.That(fixture.View.IsRedeemResultVisible, Is.False);
            Assert.That(fixture.Confirm.interactable, Is.True);
            fixture.Confirm.onClick.Invoke();
            Assert.That(fixture.Service.CallCount, Is.EqualTo(2));
        }

        [TestCase(RedeemCodeStatus.EmptyCode,
            "Please enter a redeem code.")]
        [TestCase(RedeemCodeStatus.InvalidCode,
            "Invalid redeem code.")]
        [TestCase(RedeemCodeStatus.PersistenceFailed,
            "Failed to save your reward. Please try again.")]
        public void Failure_ShowsDistinctLocalizedResult(
            RedeemCodeStatus status,
            string expected)
        {
            Fixture fixture = CreateFixture();
            fixture.Service.Result = new RedeemCodeResult(status);
            fixture.Presenter.Open();

            fixture.Confirm.onClick.Invoke();

            Assert.That(fixture.ResultText.text, Is.EqualTo(expected));
            Assert.That(fixture.View.IsRedeemResultVisible, Is.True);
        }

        [Test]
        public void VisibleResult_RefreshesWhenLocaleChanges()
        {
            Fixture fixture = CreateFixture();
            fixture.Service.Result = Success();
            fixture.Presenter.Open();
            fixture.Confirm.onClick.Invoke();

            fixture.Localization.SetLocale("ko");

            Assert.That(fixture.ResultText.text,
                Is.EqualTo("코드 교환 성공!\n골드 +10,000"));
            Assert.That(((TMP_Text)fixture.Input.placeholder).text,
                Is.EqualTo("리딤코드를 입력해 주세요"));
        }

        [Test]
        public void ReentrantConfirm_DoesNotSubmitTwice()
        {
            Fixture fixture = CreateFixture();
            fixture.Service.Result = Success();
            fixture.Service.DuringRedeem = () =>
                fixture.Confirm.onClick.Invoke();
            fixture.Presenter.Open();

            fixture.Confirm.onClick.Invoke();

            Assert.That(fixture.Service.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void CloseAndReopen_DoesNotKeepPreviousResult()
        {
            Fixture fixture = CreateFixture();
            fixture.Service.Result = Success();
            fixture.Presenter.Open();
            fixture.Confirm.onClick.Invoke();
            Assert.That(fixture.View.IsRedeemResultVisible, Is.True);

            fixture.Presenter.Close();
            fixture.Presenter.Open();

            Assert.That(fixture.View.IsRedeemResultVisible, Is.False);
            Assert.That(fixture.Confirm.interactable, Is.True);
        }

        private Fixture CreateFixture()
        {
            var root = new GameObject("RedeemPresenterTest");
            created.Add(root);
            var view = root.AddComponent<SettingsPanelView>();
            Slider master = CreateChild<Slider>(root, "Master");
            Slider bgm = CreateChild<Slider>(root, "Bgm");
            Slider sfx = CreateChild<Slider>(root, "Sfx");
            TMP_Text masterText = CreateChild<TextMeshProUGUI>(
                root,
                "MasterText");
            TMP_Text bgmText = CreateChild<TextMeshProUGUI>(root, "BgmText");
            TMP_Text sfxText = CreateChild<TextMeshProUGUI>(root, "SfxText");
            TMP_Dropdown language = CreateChild<TMP_Dropdown>(
                root,
                "Language");
            language.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("English"),
                new TMP_Dropdown.OptionData("한국어")
            };
            Toggle speed2 = CreateChild<Toggle>(root, "Speed2");
            Toggle speed3 = CreateChild<Toggle>(root, "Speed3");
            Button close = CreateChild<Button>(root, "Close");
            view.Configure(
                master,
                bgm,
                sfx,
                masterText,
                bgmText,
                sfxText,
                language,
                speed2,
                speed3,
                close);

            TMP_InputField input = CreateChild<TMP_InputField>(
                root,
                "CodeInput");
            input.textComponent = CreateChild<TextMeshProUGUI>(
                input.gameObject,
                "Text");
            input.placeholder = CreateChild<TextMeshProUGUI>(
                input.gameObject,
                "Placeholder");
            Button confirm = CreateChild<Button>(root, "Confirm");
            GameObject overlay = new GameObject("ResultOverlay");
            overlay.transform.SetParent(root.transform, false);
            created.Add(overlay);
            Button dismiss = CreateChild<Button>(overlay, "Dismiss");
            TMP_Text resultText = CreateChild<TextMeshProUGUI>(
                overlay,
                "ResultText");
            view.ConfigureRedeem(
                input,
                confirm,
                overlay,
                dismiss,
                resultText);

            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            settings.LoadOrCreate();
            LocalizationService localization = CreateLocalization();
            var service = new FakeRedeemCodeService();
            var presenter = new SettingsPanelPresenter(
                settings,
                view,
                service,
                localization);
            return new Fixture(
                view,
                presenter,
                localization,
                service,
                input,
                confirm,
                dismiss,
                resultText);
        }

        private T CreateChild<T>(GameObject parent, string name)
            where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            created.Add(child);
            return child.AddComponent<T>();
        }

        private static LocalizationService CreateLocalization()
        {
            const string tsv =
                "key\ten\tko\n"
                + "ui.settings.redeem_code_placeholder\tEnter a redeem code\t리딤코드를 입력해 주세요\n"
                + "ui.settings.redeem_success\tCode redeemed successfully!\t코드 교환 성공!\n"
                + "ui.settings.redeem_reward_gacha_currency\tGold +{amount}\t골드 +{amount}\n"
                + "ui.settings.redeem_invalid\tInvalid redeem code.\t유효하지 않은 코드입니다.\n"
                + "ui.settings.redeem_empty\tPlease enter a redeem code.\t리딤코드를 입력해 주세요.\n"
                + "ui.settings.redeem_save_failed\tFailed to save your reward. Please try again.\t보상 저장에 실패했습니다. 다시 시도해 주세요.";
            return new LocalizationService(new[]
            {
                LocalizationTableParser.Parse("redeem_test.tsv", tsv)
            });
        }

        private static RedeemCodeResult Success()
        {
            return new RedeemCodeResult(
                RedeemCodeStatus.Success,
                new RedeemCodeDefinition(
                    "NEAT3",
                    RedeemRewardType.GachaCurrency,
                    10000L));
        }

        private sealed class FakeRedeemCodeService : IRedeemCodeService
        {
            public int CallCount { get; private set; }
            public Action DuringRedeem { get; set; }
            public RedeemCodeResult Result { get; set; } =
                new RedeemCodeResult(RedeemCodeStatus.InvalidCode);

            public RedeemCodeResult Redeem(string input)
            {
                CallCount++;
                DuringRedeem?.Invoke();
                return Result;
            }
        }

        private sealed class Fixture
        {
            public Fixture(
                SettingsPanelView view,
                SettingsPanelPresenter presenter,
                LocalizationService localization,
                FakeRedeemCodeService service,
                TMP_InputField input,
                Button confirm,
                Button dismiss,
                TMP_Text resultText)
            {
                View = view;
                Presenter = presenter;
                Localization = localization;
                Service = service;
                Input = input;
                Confirm = confirm;
                Dismiss = dismiss;
                ResultText = resultText;
            }

            public SettingsPanelView View { get; }
            public SettingsPanelPresenter Presenter { get; }
            public LocalizationService Localization { get; }
            public FakeRedeemCodeService Service { get; }
            public TMP_InputField Input { get; }
            public Button Confirm { get; }
            public Button Dismiss { get; }
            public TMP_Text ResultText { get; }
        }
    }
}
