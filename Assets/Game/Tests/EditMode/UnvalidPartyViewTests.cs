using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Localization;
using ValorChronicle.Main.Presentation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class UnvalidPartyViewTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase("en", UnvalidPartyView.EmptyPartyMessage)]
        [TestCase("ko", "파티에 캐릭터가 한 명 이상 필요합니다.")]
        public void ShowEmptyParty_DisplaysLocalizedMessage(
            string locale,
            string expected)
        {
            UnvalidPartyView view = CreateView(
                out TMP_Text message,
                out _);

            view.ShowEmptyParty(CreateLocalization(locale));

            Assert.That(root.activeSelf, Is.True);
            Assert.That(message.text, Is.EqualTo(expected));
        }

        [Test]
        public void BackgroundClick_HidesPopupWithoutDuplicateListeners()
        {
            UnvalidPartyView view = CreateView(
                out TMP_Text message,
                out Button dismissButton);
            view.Configure(message, dismissButton);
            LocalizationService localization = CreateLocalization("en");
            view.ShowEmptyParty(localization);

            dismissButton.onClick.Invoke();

            Assert.That(root.activeSelf, Is.False);

            view.ShowEmptyParty(localization);
            dismissButton.onClick.Invoke();
            Assert.That(root.activeSelf, Is.False);
        }

        [Test]
        public void ShowEmptyParty_MissingKey_UsesEnglishFallback()
        {
            UnvalidPartyView view = CreateView(
                out TMP_Text message,
                out _);
            LocalizationService localization = new LocalizationService(
                new[]
                {
                    LocalizationTableParser.Parse(
                        "missing_main_test",
                        "key\ten\tko\nplaceholder\tValue\tValue")
                });
            LogAssert.Expect(
                LogType.Error,
                "[UnvalidPartyView] Empty-party localization is missing. "
                    + "Using the English fallback.");

            view.ShowEmptyParty(localization);

            Assert.That(message.text,
                Is.EqualTo(UnvalidPartyView.EmptyPartyMessage));
            Assert.That(message.text, Does.Not.StartWith("[["));
        }

        private static LocalizationService CreateLocalization(string locale)
        {
            LocalizationTable table = LocalizationTableParser.Parse(
                "main_test",
                "key\ten\tko\n"
                    + $"{UnvalidPartyView.EmptyPartyMessageKey}\t"
                    + $"{UnvalidPartyView.EmptyPartyMessage}\t"
                    + "파티에 캐릭터가 한 명 이상 필요합니다.");
            var localization = new LocalizationService(
                new[] { table });
            localization.SetLocale(locale);
            return localization;
        }

        private UnvalidPartyView CreateView(
            out TMP_Text message,
            out Button dismissButton)
        {
            root = new GameObject(
                "UnvalidParty",
                typeof(RectTransform));
            root.SetActive(false);
            UnvalidPartyView view = root.AddComponent<UnvalidPartyView>();

            var messageObject = new GameObject(
                "UnvalidText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            messageObject.transform.SetParent(root.transform, false);
            message = messageObject.GetComponent<TMP_Text>();

            var buttonObject = new GameObject(
                "BackgroundPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(root.transform, false);
            dismissButton = buttonObject.GetComponent<Button>();
            view.Configure(message, dismissButton);
            return view;
        }
    }
}
