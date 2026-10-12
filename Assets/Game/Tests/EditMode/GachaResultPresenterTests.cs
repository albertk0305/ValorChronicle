using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Gacha.Presentation;
using ValorChronicle.Rewards;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaResultPresenterTests
    {
        private const string ScenePath = "Assets/Scenes/Gacha.unity";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string CharacterId = "character_marea_bluefang";
        private const string RelicId = "relic_victors_broken_sword";

        [Test]
        public void CharacterResultsUseFaceAndPerPullAwakeningOutcome()
        {
            WithPresenter(presenter =>
            {
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomCharacter,
                    CharacterId,
                    RewardGrantKind.NewCharacter,
                    null, 0, null, 0),
                    GachaResultDisplayKind.Character, false);
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomCharacter,
                    CharacterId,
                    RewardGrantKind.CharacterAwakeningIncreased,
                    0, 1, null, 0),
                    GachaResultDisplayKind.Character, true);
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomCharacter,
                    CharacterId,
                    RewardGrantKind.CharacterAwakeningIncreased,
                    5, 6, null, 0),
                    GachaResultDisplayKind.Character, true);
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomCharacter,
                    CharacterId,
                    RewardGrantKind
                        .MaxAwakeningCharacterConvertedToHeroTokens,
                    6, 6, RewardCurrencyKind.HeroTokens, 1),
                    GachaResultDisplayKind.Character, true);
            });
        }

        [Test]
        public void RelicResultsUseIconAndOnlyIncreaseResultsShowAwakening()
        {
            WithPresenter(presenter =>
            {
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomRelic,
                    RelicId,
                    RewardGrantKind.NewRelicInstanceCreated,
                    null, 0, null, 0, false),
                    GachaResultDisplayKind.Relic, false);
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomRelic,
                    RelicId,
                    RewardGrantKind.NewRelicInstanceCreated,
                    null, 0, null, 0, true),
                    GachaResultDisplayKind.Relic, false);
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomRelic,
                    RelicId,
                    RewardGrantKind.RelicAwakeningIncreased,
                    0, 1, null, 0),
                    GachaResultDisplayKind.Relic, true);
                AssertMapped(presenter, CreatePull(
                    GachaRewardKind.RandomRelic,
                    RelicId,
                    RewardGrantKind.RelicAwakeningIncreased,
                    4, 5, null, 0),
                    GachaResultDisplayKind.Relic, true);
            });
        }

        [TestCase(GachaRewardKind.HeroTokens,
            RewardCurrencyKind.HeroTokens, 1)]
        [TestCase(GachaRewardKind.RelicTokens,
            RewardCurrencyKind.RelicTokens, 3)]
        [TestCase(GachaRewardKind.BattleRecords,
            RewardCurrencyKind.BattleRecords, 1000)]
        public void CurrencyResultsUseItemPresentationAndExactAmount(
            GachaRewardKind rewardKind,
            RewardCurrencyKind currencyKind,
            long amount)
        {
            WithPresenter(presenter =>
            {
                GachaTransactionPullResult pull = CreatePull(
                    rewardKind, null, RewardGrantKind.CurrencyGranted,
                    null, null, currencyKind, amount);
                bool success = presenter.TryCreateDisplayEntry(
                    pull, out GachaResultDisplayEntry entry,
                    out string error);

                Assert.That(success, Is.True, error);
                Assert.That(entry.Kind,
                    Is.EqualTo(GachaResultDisplayKind.Item));
                Assert.That(entry.PrimarySprite, Is.Not.Null);
                Assert.That(entry.ItemAmount, Is.EqualTo(amount));
                Assert.That(entry.AwakeningSprite, Is.Null);
            });
        }

        [Test]
        public void InvalidAwakeningTransitionIsRejected()
        {
            WithPresenter(presenter =>
            {
                bool success = presenter.TryCreateDisplayEntry(
                    CreatePull(
                        GachaRewardKind.RandomCharacter,
                        CharacterId,
                        RewardGrantKind.CharacterAwakeningIncreased,
                        0, 2, null, 0),
                    out _, out string error);
                Assert.That(success, Is.False);
                Assert.That(error, Does.Contain("Awakening result"));
            });
        }

        private static void AssertMapped(
            GachaResultPresenter presenter,
            GachaTransactionPullResult pull,
            GachaResultDisplayKind expectedKind,
            bool expectsAwakening)
        {
            bool success = presenter.TryCreateDisplayEntry(
                pull, out GachaResultDisplayEntry entry,
                out string error);
            Assert.That(success, Is.True, error);
            Assert.That(entry.Kind, Is.EqualTo(expectedKind));
            Assert.That(entry.PrimarySprite, Is.Not.Null);
            Assert.That(entry.AwakeningSprite != null,
                Is.EqualTo(expectsAwakening));
        }

        private static void WithPresenter(Action<GachaResultPresenter> body)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath, OpenSceneMode.Single);
                Transform content = scene.GetRootGameObjects()
                    .Single(root => root.name == "GachaCanvas")
                    .transform.Find(
                        "SafeAreaRoot/GamaeViewport/GameContent");
                GachaSceneInstaller installer =
                    content.GetComponent<GachaSceneInstaller>();
                installer.Install();
                DefinitionDatabase database =
                    AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                        DatabasePath);
                database.Initialize();
                installer.ResultPresenter.Initialize(database);
                body(installer.ResultPresenter);
            }
            finally
            {
                if (previous.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }

        private static GachaTransactionPullResult CreatePull(
            GachaRewardKind rewardKind,
            string contentId,
            RewardGrantKind grantKind,
            int? previousAwakening,
            int? newAwakening,
            RewardCurrencyKind? currencyKind,
            long amount,
            bool relicCreatedAfterMax = false)
        {
            string rewardId = contentId;
            var pull = (GachaPullResult)CreateInternal(
                typeof(GachaPullResult),
                0,
                GachaScreenController.CharacterGachaId,
                rewardKind,
                rewardId,
                amount <= 0 ? 1L : amount,
                false,
                0,
                0);
            var request = new RewardGrantRequest(
                0,
                rewardKind,
                rewardId,
                amount <= 0 ? 1L : amount);
            CurrencyGrantResult currency = currencyKind.HasValue
                ? (CurrencyGrantResult)CreateInternal(
                    typeof(CurrencyGrantResult),
                    currencyKind.Value,
                    amount,
                    0L,
                    amount)
                : null;
            var grant = (RewardGrantResult)CreateInternal(
                typeof(RewardGrantResult),
                request,
                grantKind,
                contentId,
                grantKind == RewardGrantKind.NewRelicInstanceCreated
                    ? "relic-instance"
                    : null,
                previousAwakening,
                newAwakening,
                currency,
                relicCreatedAfterMax);
            return (GachaTransactionPullResult)CreateInternal(
                typeof(GachaTransactionPullResult), pull, grant);
        }

        private static object CreateInternal(
            Type type,
            params object[] arguments)
        {
            return Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }
    }
}
