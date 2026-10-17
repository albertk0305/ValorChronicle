using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Exchange;
using ValorChronicle.Exchange.Persistence;
using ValorChronicle.Exchange.Presentation;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Gacha.Presentation;
using ValorChronicle.Localization;
using ValorChronicle.Redeem;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaScreenTransactionConnectionTests
    {
        private const string ScenePath = "Assets/Scenes/Gacha.unity";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";

        [TestCase(99L, false, false)]
        [TestCase(100L, true, false)]
        [TestCase(999L, true, false)]
        [TestCase(1000L, true, true)]
        public void GoldControlsSingleAndTenPullButtons(
            long gold,
            bool expectedSingle,
            bool expectedTen)
        {
            WithContext(gold, context =>
            {
                Assert.That(
                    context.View.CharacterSingleButton.interactable,
                    Is.EqualTo(expectedSingle));
                Assert.That(
                    context.View.RelicSingleButton.interactable,
                    Is.EqualTo(expectedSingle));
                Assert.That(
                    context.View.CharacterTenButton.interactable,
                    Is.EqualTo(expectedTen));
                Assert.That(
                    context.View.RelicTenButton.interactable,
                    Is.EqualTo(expectedTen));
                Assert.That(context.View.SettingButton.interactable,
                    Is.True);
                Assert.That(context.View.ExchangeButton.interactable,
                    Is.True);
            });
        }

        [Test]
        public void RedeemTransaction_RefreshesVisibleGachaCurrency()
        {
            WithContext(100L, context =>
            {
                var redeem = new RedeemCodeService(context.SaveService);

                RedeemCodeResult result = redeem.Redeem("NEAT3");

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(context.View.GachaCurrencyText.text,
                    Is.EqualTo("10100"));
                Assert.That(
                    context.View.CharacterTenButton.interactable,
                    Is.True);
                Assert.That(context.View.RelicTenButton.interactable,
                    Is.True);
            });
        }

        [Test]
        public void NewlyInitializedExchangeTryOpenDisplaysHeroSlots()
        {
            WithContext(2000L, context =>
            {
                Assert.That(context.Exchange.IsOpen, Is.False);

                Assert.That(context.Exchange.TryOpen(), Is.True);

                Assert.That(context.Exchange.IsOpen, Is.True);
                Assert.That(context.Exchange.SelectedTab,
                    Is.EqualTo(ExchangeTab.Hero));
                Assert.That(context.Exchange.Grid.VisibleKind,
                    Is.EqualTo(ExchangeContentKind.Character));
                Assert.That(context.Exchange.Grid.ItemCount,
                    Is.EqualTo(context.Exchange.CurrentCatalog
                        .Characters.Count));
                Assert.That(context.Exchange.Grid.BoundItemCount,
                    Is.GreaterThan(0));
                Assert.That(context.Exchange.Grid.PoolSlots.Any(slot =>
                    slot.IsBound && slot.gameObject.activeSelf), Is.True);
                Assert.That(context.Exchange.Grid.IsAtTop, Is.True);
            });
        }

        [Test]
        public void ExchangeOpensOnHeroTabSwitchesAndClosesWithoutSaving()
        {
            WithContext(2000L, context =>
            {
                ProfileSaveData initial = context.SaveService
                    .GetCurrentProfileSnapshot();
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                int opened = 0;
                int closed = 0;
                context.Exchange.Opened += () => opened++;
                context.Exchange.Closed += () => closed++;

                context.View.ExchangeButton.onClick.Invoke();

                Assert.That(context.Exchange.IsOpen, Is.True);
                Assert.That(context.Exchange.SelectedTab,
                    Is.EqualTo(ExchangeTab.Hero));
                Assert.That(context.Exchange.Grid.VisibleKind,
                    Is.EqualTo(ExchangeContentKind.Character));
                Assert.That(context.Exchange.Grid.ItemCount,
                    Is.EqualTo(context.Exchange.CurrentCatalog
                        .Characters.Count));
                Assert.That(context.Exchange.Grid.BoundItemCount,
                    Is.GreaterThan(0));
                Assert.That(context.Exchange.Grid.PoolSlots.Count(slot =>
                    slot.IsBound && slot.gameObject.activeSelf),
                    Is.EqualTo(context.Exchange.Grid.BoundItemCount));
                Assert.That(context.Exchange.Grid.IsAtTop, Is.True);
                Assert.That(context.Exchange.View.ConfirmRoot.activeSelf,
                    Is.False);
                Assert.That(context.Exchange.View.HeroTokensText.text,
                    Is.EqualTo(initial.Currencies.HeroTokens.ToString(
                        CultureInfo.InvariantCulture)));
                Assert.That(context.Exchange.View.RelicTokensText.text,
                    Is.EqualTo(initial.Currencies.RelicTokens.ToString(
                        CultureInfo.InvariantCulture)));
                Assert.That(context.Exchange.View.GachaCurrencyText.text,
                    Is.EqualTo("2000"));
                AssertAllPullButtons(context.View, false);
                Assert.That(context.View.ExchangeButton.interactable,
                    Is.False);
                Button infoButton = context.Content.Find(
                    "HeroesPickup/InfoButton").GetComponent<Button>();
                Assert.That(infoButton.interactable, Is.False);
                infoButton.onClick.Invoke();
                Assert.That(context.View.InfoPanel.activeSelf, Is.False);
                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
                int selections = 0;
                context.Exchange.SelectionRequested += (_, __) =>
                    selections++;
                ExchangeSlotView selectable = context.Exchange.Grid
                    .PoolSlots.First(slot => slot.IsBound
                        && slot.Button.interactable);
                Assert.That(context.Exchange.TrySelect(
                    selectable.ContentKind,
                    selectable.ContentId), Is.True);
                Assert.That(selections, Is.EqualTo(1));
                Assert.That(context.Exchange.View.ConfirmRoot.activeSelf,
                    Is.True);
                Assert.That(context.Exchange.Grid.IsInteractionEnabled,
                    Is.False);
                Assert.That(context.Exchange.View.HeroButton.interactable,
                    Is.False);
                Assert.That(context.Exchange.View.ExitButton.interactable,
                    Is.False);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                context.Exchange.ConfirmView.CancelButton.onClick.Invoke();
                Assert.That(context.Exchange.View.ConfirmRoot.activeSelf,
                    Is.False);
                Assert.That(context.Exchange.Grid.IsInteractionEnabled,
                    Is.True);
                Assert.That(context.Exchange.SelectedTab,
                    Is.EqualTo(ExchangeTab.Hero));

                ExchangeCatalogData heroCatalog =
                    context.Exchange.CurrentCatalog;
                int heroBoundCount = context.Exchange.Grid.BoundItemCount;
                context.Exchange.View.HeroButton.onClick.Invoke();
                Assert.That(context.Exchange.CurrentCatalog,
                    Is.SameAs(heroCatalog));
                Assert.That(context.Exchange.Grid.BoundItemCount,
                    Is.EqualTo(heroBoundCount));

                context.Exchange.View.ArtifactButton.onClick.Invoke();
                Assert.That(context.Exchange.SelectedTab,
                    Is.EqualTo(ExchangeTab.Artifact));
                Assert.That(context.Exchange.Grid.VisibleKind,
                    Is.EqualTo(ExchangeContentKind.Relic));
                Assert.That(context.Exchange.Grid.IsAtTop, Is.True);
                ExchangeCatalogData catalog =
                    context.Exchange.CurrentCatalog;
                context.Exchange.View.ArtifactButton.onClick.Invoke();
                Assert.That(context.Exchange.CurrentCatalog,
                    Is.SameAs(catalog));

                context.Exchange.View.HeroButton.onClick.Invoke();
                Assert.That(context.Exchange.SelectedTab,
                    Is.EqualTo(ExchangeTab.Hero));
                Assert.That(context.Exchange.Grid.VisibleKind,
                    Is.EqualTo(ExchangeContentKind.Character));
                Assert.That(context.Exchange.Grid.BoundItemCount,
                    Is.GreaterThan(0));

                context.Exchange.View.ExitButton.onClick.Invoke();
                Assert.That(context.Exchange.IsOpen, Is.False);
                Assert.That(context.Exchange.HasSelectedTab, Is.False);
                Assert.That(context.View.ExchangeButton.interactable,
                    Is.True);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                context.View.ExchangeButton.onClick.Invoke();
                Assert.That(context.Exchange.SelectedTab,
                    Is.EqualTo(ExchangeTab.Hero));
                Assert.That(context.Exchange.Grid.IsAtTop, Is.True);
                Assert.That(context.Exchange.Grid.VisibleKind,
                    Is.EqualTo(ExchangeContentKind.Character));
                Assert.That(context.Exchange.Grid.BoundItemCount,
                    Is.GreaterThan(0));
                Assert.That(opened, Is.EqualTo(2));
                Assert.That(closed, Is.EqualTo(1));
            });
        }

        [Test]
        public void ExchangeReentryUsesLatestProfileSnapshot()
        {
            WithContext(2000L, context =>
            {
                context.View.ExchangeButton.onClick.Invoke();
                string characterId = context.Exchange.CurrentCatalog
                    .Characters.First().CharacterId;
                context.Exchange.View.ExitButton.onClick.Invoke();
                var result = context.SaveService.ExecuteTransaction(
                    profile =>
                    {
                        profile.Currencies.HeroTokens = 601;
                        profile.Currencies.RelicTokens = 252;
                        profile.Currencies.GachaCurrency = 3456;
                        CharacterSaveData character = profile.Characters
                            .FirstOrDefault(item =>
                                item.CharacterId == characterId);
                        if (character == null)
                        {
                            character = new CharacterSaveData
                            {
                                CharacterId = characterId,
                                Level = 1
                            };
                            profile.Characters.Add(character);
                        }

                        character.Awakening = 4;
                    });
                Assert.That(result.IsSuccess, Is.True, result.Message);

                context.View.ExchangeButton.onClick.Invoke();

                Assert.That(context.Exchange.View.HeroTokensText.text,
                    Is.EqualTo("601"));
                Assert.That(context.Exchange.View.RelicTokensText.text,
                    Is.EqualTo("252"));
                Assert.That(context.Exchange.View.GachaCurrencyText.text,
                    Is.EqualTo("3456"));
                Assert.That(context.Exchange.SelectedTab,
                    Is.EqualTo(ExchangeTab.Hero));
                Assert.That(context.Exchange.Grid.VisibleKind,
                    Is.EqualTo(ExchangeContentKind.Character));
                Assert.That(context.Exchange.Grid.BoundItemCount,
                    Is.GreaterThan(0));
                CharacterExchangeListItem refreshed = context.Exchange
                    .CurrentCatalog.Characters.Single(item =>
                        item.CharacterId == characterId);
                Assert.That(refreshed.IsOwned, Is.True);
                Assert.That(refreshed.CurrentAwakening, Is.EqualTo(4));
                ExchangeSlotView refreshedSlot = context.Exchange.Grid
                    .PoolSlots.Single(slot =>
                        slot.IsBound && slot.ContentId == characterId);
                Assert.That(refreshedSlot.AwakeningText.text,
                    Is.EqualTo("4 / 6"));
            });
        }

        [Test]
        public void CharacterExchangeConfirmsOnceRefreshesAndKeepsPity()
        {
            WithContext(
                2000L,
                context =>
                {
                    context.View.ExchangeButton.onClick.Invoke();
                    CharacterExchangeListItem selected = context.Exchange
                        .CurrentCatalog.Characters.First(item =>
                            !item.IsMaximumAwakening);
                    ProfileSaveData before = context.SaveService
                        .GetCurrentProfileSnapshot();
                    string pityBefore = PitySignature(before);
                    int expectedAwakening = selected.IsOwned
                        ? selected.CurrentAwakening + 1
                        : 0;
                    float scrollBefore = context.Exchange.Grid.ScrollOffset;

                    Assert.That(context.Exchange.TrySelect(
                        ExchangeContentKind.Character,
                        selected.CharacterId), Is.True);
                    Assert.That(context.Exchange.IsConfirmOpen, Is.True);
                    context.Exchange.ConfirmView.ConfirmButton.onClick
                        .Invoke();
                    context.Exchange.ConfirmView.ConfirmButton.onClick
                        .Invoke();

                    ProfileSaveData after = context.SaveService
                        .GetCurrentProfileSnapshot();
                    CharacterSaveData awarded = after.Characters.Single(
                        item => item.CharacterId == selected.CharacterId);
                    Assert.That(awarded.Awakening,
                        Is.EqualTo(expectedAwakening));
                    Assert.That(after.Currencies.HeroTokens, Is.Zero);
                    Assert.That(PitySignature(after), Is.EqualTo(pityBefore));
                    Assert.That(context.Repository.Count(
                        nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
                    Assert.That(context.Exchange.IsOpen, Is.True);
                    Assert.That(context.Exchange.IsConfirmOpen, Is.False);
                    Assert.That(context.Exchange.SelectedTab,
                        Is.EqualTo(ExchangeTab.Hero));
                    Assert.That(context.Exchange.Grid.ScrollOffset,
                        Is.EqualTo(scrollBefore).Within(0.01f));
                    Assert.That(context.Exchange.View.HeroTokensText.text,
                        Is.EqualTo("0"));
                },
                profile => profile.Currencies.HeroTokens = 60L);
        }

        [Test]
        public void RelicExchangeConfirmsOnceAndRefreshesCurrentTab()
        {
            WithContext(
                2000L,
                context =>
                {
                    context.View.ExchangeButton.onClick.Invoke();
                    Assert.That(context.Exchange.SelectTab(
                        ExchangeTab.Artifact), Is.True);
                    RelicExchangeListItem selected = context.Exchange
                        .CurrentCatalog.Relics.First();
                    ProfileSaveData before = context.SaveService
                        .GetCurrentProfileSnapshot();
                    int countBefore = before.RelicInstances.Count(item =>
                        item.RelicDefinitionId == selected.RelicId);

                    Assert.That(context.Exchange.TrySelect(
                        ExchangeContentKind.Relic,
                        selected.RelicId), Is.True);
                    context.Exchange.ConfirmView.ConfirmButton.onClick
                        .Invoke();

                    ProfileSaveData after = context.SaveService
                        .GetCurrentProfileSnapshot();
                    Assert.That(after.Currencies.RelicTokens, Is.Zero);
                    if (selected.WillCreateNewInstance)
                    {
                        Assert.That(after.RelicInstances.Count(item =>
                            item.RelicDefinitionId == selected.RelicId),
                            Is.EqualTo(countBefore + 1));
                        Assert.That(after.RelicInstances.Any(item =>
                            item.RelicDefinitionId == selected.RelicId
                            && item.Awakening == 0), Is.True);
                    }
                    else
                    {
                        RelicInstanceSaveData target = after.RelicInstances
                            .Single(item => item.InstanceId
                                == selected.AwakeningTargetInstanceId);
                        Assert.That(target.Awakening,
                            Is.EqualTo(selected.TargetAwakening + 1));
                    }
                    Assert.That(context.Repository.Count(
                        nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
                    Assert.That(context.Exchange.IsOpen, Is.True);
                    Assert.That(context.Exchange.IsConfirmOpen, Is.False);
                    Assert.That(context.Exchange.SelectedTab,
                        Is.EqualTo(ExchangeTab.Artifact));
                    Assert.That(context.Exchange.View.RelicTokensText.text,
                        Is.EqualTo("0"));
                },
                profile => profile.Currencies.RelicTokens = 25L);
        }

        [Test]
        public void InsufficientCurrencyOpensConfirmButCannotExecute()
        {
            WithContext(
                2000L,
                context =>
                {
                    context.View.ExchangeButton.onClick.Invoke();
                    CharacterExchangeListItem selected = context.Exchange
                        .CurrentCatalog.Characters.First(item =>
                            !item.IsMaximumAwakening);

                    Assert.That(context.Exchange.TrySelect(
                        ExchangeContentKind.Character,
                        selected.CharacterId), Is.True);
                    Assert.That(context.Exchange.IsConfirmOpen, Is.True);
                    Assert.That(context.Exchange.ConfirmView.ConfirmButton
                        .interactable, Is.False);
                    Assert.That(context.Exchange.TryExecuteSelected(),
                        Is.False);
                    Assert.That(context.Repository.Count(
                        nameof(FakeSaveRepository.WriteTemp)), Is.Zero);
                    Assert.That(context.Exchange.IsConfirmOpen, Is.True);
                },
                profile => profile.Currencies.HeroTokens = 0L);
        }

        [Test]
        public void PersistenceFailureKeepsConfirmAndAllowsExplicitRetry()
        {
            WithContext(
                2000L,
                context =>
                {
                    context.View.ExchangeButton.onClick.Invoke();
                    CharacterExchangeListItem selected = context.Exchange
                        .CurrentCatalog.Characters.First(item =>
                            !item.IsMaximumAwakening);
                    Assert.That(context.Exchange.TrySelect(
                        ExchangeContentKind.Character,
                        selected.CharacterId), Is.True);
                    context.Repository.FailWriteTemp = true;
                    LogAssert.Expect(
                        LogType.Exception,
                        new Regex(
                            "InvalidOperationException: Injected temp "
                                + "write failure\\."));
                    LogAssert.Expect(
                        LogType.Error,
                        new Regex(
                            "Exchange transaction failed\\. Status="
                                + "PersistenceFailed"));

                    context.Exchange.ConfirmView.ConfirmButton.onClick
                        .Invoke();

                    Assert.That(context.Exchange.IsConfirmOpen, Is.True);
                    Assert.That(context.Exchange.IsTransactionInProgress,
                        Is.False);
                    Assert.That(context.SaveService
                        .GetCurrentProfileSnapshot()
                        .Currencies.HeroTokens, Is.EqualTo(60L));
                    Assert.That(context.Installer.InfoPresenter.IsOpen,
                        Is.True);

                    context.Repository.FailWriteTemp = false;
                    context.Content.Find("InfoPanel/ExitButton")
                        .GetComponent<Button>().onClick.Invoke();
                    Assert.That(context.Exchange.TryExecuteSelected(),
                        Is.True);
                    Assert.That(context.Exchange.IsConfirmOpen, Is.False);
                    Assert.That(context.SaveService
                        .GetCurrentProfileSnapshot()
                        .Currencies.HeroTokens, Is.Zero);
                },
                profile => profile.Currencies.HeroTokens = 60L);
        }

        [Test]
        public void PersistedStateMismatchBlocksAllFurtherExchangeRequests()
        {
            WithContext(
                2000L,
                context =>
                {
                    context.View.ExchangeButton.onClick.Invoke();
                    CharacterExchangeListItem selected = context.Exchange
                        .CurrentCatalog.Characters.First(item =>
                            !item.IsMaximumAwakening);
                    var executor = new PersistedMismatchExecutor(
                        context.Installer.ExchangeTransactionService);
                    SetPrivateField(
                        context.Exchange,
                        "transactionService",
                        executor);
                    Assert.That(context.Exchange.TrySelect(
                        ExchangeContentKind.Character,
                        selected.CharacterId), Is.True);
                    LogAssert.Expect(
                        LogType.Error,
                        new Regex(
                            "Exchange transaction failed\\. Status="
                                + "PersistedStateMismatch"));

                    context.Exchange.ConfirmView.ConfirmButton.onClick
                        .Invoke();

                    Assert.That(executor.CallCount, Is.EqualTo(1));
                    Assert.That(context.Exchange.IsConfirmOpen, Is.False);
                    Assert.That(context.Exchange.IsReady, Is.False);
                    Assert.That(context.Exchange.Grid.IsInteractionEnabled,
                        Is.False);
                    Assert.That(context.Exchange.View.ExitButton.interactable,
                        Is.True);
                    Assert.That(context.Installer.InfoPresenter.IsOpen,
                        Is.True);
                    Assert.That(context.Exchange.TryExecuteSelected(),
                        Is.False);
                    Assert.That(context.Exchange.TrySelect(
                        ExchangeContentKind.Character,
                        selected.CharacterId), Is.False);
                    Assert.That(executor.CallCount, Is.EqualTo(1));
                    Assert.That(context.Repository.Count(
                        nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
                    AssertReloadedCharacterExchange(
                        context,
                        selected.CharacterId);
                },
                profile => profile.Currencies.HeroTokens = 60L);
        }

        [Test]
        public void SavedExchangeWithRefreshFailureCannotExecuteAgain()
        {
            WithContext(
                2000L,
                context =>
                {
                    context.View.ExchangeButton.onClick.Invoke();
                    CharacterExchangeListItem selected = context.Exchange
                        .CurrentCatalog.Characters.First(item =>
                            !item.IsMaximumAwakening);
                    Assert.That(context.Exchange.TrySelect(
                        ExchangeContentKind.Character,
                        selected.CharacterId), Is.True);
                    SetPrivateField(context.Exchange, "spriteResolver", null);
                    LogAssert.Expect(
                        LogType.Exception,
                        new Regex("ArgumentNullException"));
                    LogAssert.Expect(
                        LogType.Error,
                        new Regex(
                            "Exchange data could not be displayed"));
                    LogAssert.Expect(
                        LogType.Error,
                        new Regex(
                            "Exchange was saved, but the refreshed UI "
                                + "could not be displayed"));

                    context.Exchange.ConfirmView.ConfirmButton.onClick
                        .Invoke();

                    Assert.That(context.Exchange.IsOpen, Is.False);
                    Assert.That(context.Exchange.IsReady, Is.False);
                    Assert.That(context.Exchange.IsConfirmOpen, Is.False);
                    Assert.That(context.Installer.InfoPresenter.IsOpen,
                        Is.True);
                    Assert.That(context.Exchange.TryExecuteSelected(),
                        Is.False);
                    Assert.That(context.Repository.Count(
                        nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
                    AssertReloadedCharacterExchange(
                        context,
                        selected.CharacterId);
                },
                profile => profile.Currencies.HeroTokens = 60L);
        }

        [Test]
        public void FourButtonsExecuteOnePersistedTransactionWithCorrectBanner()
        {
            WithContext(10000L, context =>
            {
                int openingCount = 0;
                int finishedCount = 0;
                context.Presentation.StateChanged += state =>
                {
                    if (state == GachaPresentationState.Opening)
                    {
                        openingCount++;
                    }
                };
                context.Presentation.Finished += _ => finishedCount++;
                ClickAndAssert(
                    context,
                    context.View.CharacterSingleButton,
                    GachaScreenController.CharacterGachaId,
                    1,
                    9900L,
                    1);
                ClickAndAssert(
                    context,
                    context.View.CharacterTenButton,
                    GachaScreenController.CharacterGachaId,
                    10,
                    8900L,
                    2);
                ClickAndAssert(
                    context,
                    context.View.RelicSingleButton,
                    GachaScreenController.RelicGachaId,
                    1,
                    8800L,
                    3);
                ClickAndAssert(
                    context,
                    context.View.RelicTenButton,
                    GachaScreenController.RelicGachaId,
                    10,
                    7800L,
                    4);

                ProfileSaveData profile = context.SaveService
                    .GetCurrentProfileSnapshot();
                Assert.That(profile.GachaStates.Single(state =>
                    state.GachaId
                        == GachaScreenController.CharacterGachaId)
                    .TotalPullCount, Is.EqualTo(11));
                Assert.That(profile.GachaStates.Single(state =>
                    state.GachaId
                        == GachaScreenController.RelicGachaId)
                    .TotalPullCount, Is.EqualTo(11));
                Assert.That(openingCount, Is.EqualTo(4));
                Assert.That(finishedCount, Is.EqualTo(4));
            });
        }

        [Test]
        public void InfoPanelAndResultScreenBlockTransactionsUntilClosed()
        {
            WithContext(2000L, context =>
            {
                context.Content.Find("HeroesPickup/InfoButton")
                    .GetComponent<Button>().onClick.Invoke();
                AssertAllPullButtons(context.View, false);
                Assert.That(context.View.ExchangeButton.interactable,
                    Is.False);
                context.View.ExchangeButton.onClick.Invoke();
                Assert.That(context.Exchange.IsOpen, Is.False);
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                context.Content.Find("InfoPanel/ExitButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(
                    context.View.CharacterSingleButton.interactable,
                    Is.True);

                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.ResultView.IsOpen, Is.True);
                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Opening));
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);
                AssertAllPullButtons(context.View, false);
                writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                context.View.RelicSingleButton.onClick.Invoke();
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                CloseResult(context);
                Assert.That(context.ResultView.IsOpen, Is.False);
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
                Assert.That(
                    context.View.CharacterSingleButton.interactable,
                    Is.True);
            });
        }

        [Test]
        public void InfoLocaleChangeDoesNotMutateProfileOrStartPull()
        {
            WithContext(2000L, context =>
            {
                ProfileSaveData before = context.SaveService
                    .GetCurrentProfileSnapshot();
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                context.Content.Find("HeroesPickup/InfoButton")
                    .GetComponent<Button>().onClick.Invoke();

                context.Localization.SetLocale("ko");

                ProfileSaveData after = context.SaveService
                    .GetCurrentProfileSnapshot();
                Assert.That(context.Installer.InfoPresenter.IsOpen,
                    Is.True);
                Assert.That(context.Content.Find("InfoPanel/InfoText")
                    .GetComponent<TMPro.TMP_Text>().text,
                    Does.Contain("일반 캐릭터 소환"));
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
                Assert.That(context.Presentation.IsPresenting, Is.False);
                Assert.That(context.ResultView.IsOpen, Is.False);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
                Assert.That(
                    new SaveDataValueComparer().Equals(before, after),
                    Is.True);
            });
        }

        [Test]
        public void UnavailableIndividualPresentationUsesDirectFinalSummary()
        {
            WithContext(100L, context =>
            {
                typeof(GachaScreenController).GetField(
                    "individualPresentationReady",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(context.Controller, false);

                context.View.CharacterSingleButton.onClick.Invoke();

                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Idle));
                Assert.That(context.ResultView.IsOpen, Is.True);
                Assert.That(context.ResultView.Slots.Count(slot =>
                    slot.gameObject.activeSelf), Is.EqualTo(1));
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);
                GachaResultSlotView slot = context.ResultView.Slots
                    .Single(item => item.gameObject.activeSelf);
                Sprite contentSprite = slot.ContentImage.sprite;
                Sprite itemSprite = slot.ItemImage.sprite;
                Sprite awakeningSprite = slot.AwakeningImage.sprite;
                string itemText = slot.ItemText.text;
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));

                context.Localization.SetLocale("ko");

                Assert.That(context.ResultView.IsOpen, Is.True);
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);
                Assert.That(slot.ContentImage.sprite,
                    Is.SameAs(contentSprite));
                Assert.That(slot.ItemImage.sprite, Is.SameAs(itemSprite));
                Assert.That(slot.AwakeningImage.sprite,
                    Is.SameAs(awakeningSprite));
                Assert.That(slot.ItemText.text, Is.EqualTo(itemText));
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
                CloseResult(context);
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
            });
        }

        [Test]
        public void DisablingScreenCancelsPresentationAndIgnoresOldCompletion()
        {
            WithContext(200L, context =>
            {
                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);
                Assert.That(context.Presentation.IsPresenting, Is.True);

                context.Controller.enabled = false;
                InvokeLifecycle(context.Controller, "OnDisable");

                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Idle));
                Assert.That(context.Presentation.IsPresenting, Is.False);
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(100L));

                context.Controller.enabled = true;
                InvokeLifecycle(context.Controller, "OnEnable");
                Assert.That(context.View.CharacterSingleButton.interactable,
                    Is.True);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
            });
        }

        [Test]
        public void DisablingPresentationEndsLockWithoutRepeatingTransaction()
        {
            WithContext(200L, context =>
            {
                context.View.CharacterSingleButton.onClick.Invoke();
                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.True);

                context.Presentation.enabled = false;
                InvokeLifecycle(context.Presentation, "OnDisable");

                Assert.That(context.Controller.IsTransactionInProgress,
                    Is.False);
                Assert.That(context.Presentation.State,
                    Is.EqualTo(GachaPresentationState.Finished));
                Assert.That(context.Installer.InfoPresenter.IsOpen, Is.True);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(100L));
            });
        }

        [Test]
        public void PersistenceFailureShowsMessageWithoutOpeningResult()
        {
            WithContext(100L, context =>
            {
                string before = context.Repository.MainText;
                context.Repository.FailWriteTemp = true;
                LogAssert.Expect(
                    LogType.Exception,
                    new Regex("Injected temp write failure"));
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("Gacha transaction failed"));

                context.View.CharacterSingleButton.onClick.Invoke();

                Assert.That(context.ResultView.IsOpen, Is.False);
                Assert.That(context.Installer.InfoPresenter.IsOpen, Is.True);
                Assert.That(context.Installer.InfoPresenter
                    .GetComponent<GachaInfoPanelView>().InfoText.text,
                    Does.Contain("could not be completed"));
                Assert.That(context.Repository.MainText, Is.EqualTo(before));
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(100L));
            });
        }

        [Test]
        public void InsufficientGoldGuardDoesNotCallSaveService()
        {
            WithContext(99L, context =>
            {
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));

                bool started = context.Controller.TryExecutePull(
                    GachaScreenController.CharacterGachaId,
                    1);

                Assert.That(started, Is.False);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
                Assert.That(context.SaveService
                    .GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency, Is.EqualTo(99L));
            });
        }

        [Test]
        public void MissingRequiredResultSpriteKeepsTransactionsDisabled()
        {
            WithContext(1000L, context =>
            {
                ProfileSaveData before = context.SaveService
                    .GetCurrentProfileSnapshot();
                int writesBefore = context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));
                SetPrivateField(
                    context.Installer.ResultPresenter,
                    "heroTokenSprite",
                    null);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("Result resource validation failed"));
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        "The Gacha screen view is not configured\\."));

                context.Controller.Initialize(
                    context.SaveService,
                    context.Database,
                    context.Localization,
                    context.TransactionService);

                bool started = context.Controller.TryExecutePull(
                    GachaScreenController.CharacterGachaId,
                    1);
                ProfileSaveData after = context.SaveService
                    .GetCurrentProfileSnapshot();

                AssertAllPullButtons(context.View, false);
                Assert.That(context.Installer.InfoPresenter.IsOpen, Is.True);
                Assert.That(started, Is.False);
                Assert.That(context.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
                Assert.That(after.Currencies.GachaCurrency,
                    Is.EqualTo(before.Currencies.GachaCurrency));
                Assert.That(after.Currencies.HeroTokens,
                    Is.EqualTo(before.Currencies.HeroTokens));
                Assert.That(after.Currencies.RelicTokens,
                    Is.EqualTo(before.Currencies.RelicTokens));
                Assert.That(after.Currencies.BattleRecords,
                    Is.EqualTo(before.Currencies.BattleRecords));
                Assert.That(
                    new SaveDataValueComparer().Equals(before, after),
                    Is.True);
                Assert.That(context.ResultView.IsOpen, Is.False);
                Assert.That(context.Presentation.IsPresenting, Is.False);
            });
        }

        private static void ClickAndAssert(
            Context context,
            Button button,
            string gachaId,
            long pullCount,
            long expectedGold,
            int expectedWrites)
        {
            button.onClick.Invoke();
            Assert.That(context.ResultView.IsOpen, Is.True);
            Assert.That(context.Presentation.State,
                Is.EqualTo(GachaPresentationState.Opening));
            Assert.That(context.Presentation.ActiveTransaction, Is.Not.Null);
            Assert.That(context.Controller.IsTransactionInProgress, Is.True);
            Assert.That(context.ResultView.Slots.Count(slot =>
                slot.gameObject.activeSelf), Is.Zero);
            context.Presentation.View.SkipButton.onClick.Invoke();
            Assert.That(context.Presentation.State,
                Is.EqualTo(GachaPresentationState.FinalSummary));
            Assert.That(context.ResultView.Slots.Count(slot =>
                slot.gameObject.activeSelf), Is.EqualTo(pullCount));
            AssertAllPullButtons(context.View, false);
            Assert.That(context.Repository.Count(
                nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(expectedWrites));
            ProfileSaveData profile = context.SaveService
                .GetCurrentProfileSnapshot();
            Assert.That(profile.Currencies.GachaCurrency,
                Is.EqualTo(expectedGold));
            string expectedGoldText = expectedGold.ToString(
                CultureInfo.InvariantCulture);
            Assert.That(context.View.GachaCurrencyText.text,
                Is.EqualTo(expectedGoldText));
            Assert.That(profile.GachaStates.Any(state =>
                state.GachaId == gachaId), Is.True);
            CloseResult(context);
            Assert.That(context.View.GachaCurrencyText.text,
                Is.EqualTo(expectedGoldText));
        }

        private static void CloseResult(Context context)
        {
            if (context.Presentation.IsPresenting
                && context.Presentation.State
                    != GachaPresentationState.FinalSummary)
            {
                context.Presentation.View.SkipButton.onClick.Invoke();
            }

            context.Content.Find("GachaPresentation/TouchPanel")
                .GetComponent<Button>().onClick.Invoke();
        }

        private static void AssertAllPullButtons(
            GachaScreenView view,
            bool expected)
        {
            Assert.That(view.CharacterSingleButton.interactable,
                Is.EqualTo(expected));
            Assert.That(view.CharacterTenButton.interactable,
                Is.EqualTo(expected));
            Assert.That(view.RelicSingleButton.interactable,
                Is.EqualTo(expected));
            Assert.That(view.RelicTenButton.interactable,
                Is.EqualTo(expected));
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

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static string PitySignature(ProfileSaveData profile)
        {
            return string.Join(
                "|",
                profile.GachaStates.Select(state =>
                    state.GachaId + ":" + state.PityCount + ":"
                        + state.TotalPullCount));
        }

        private static void AssertReloadedCharacterExchange(
            Context context,
            string characterId)
        {
            SaveService reloaded = SaveServiceTestFactory.Create(
                context.Repository,
                new FixedUnixTimeProvider(500L));
            SaveLoadResult load = reloaded.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            ProfileSaveData profile = reloaded
                .GetCurrentProfileSnapshot();
            Assert.That(profile.Currencies.HeroTokens, Is.Zero);
            Assert.That(profile.Characters.Any(item =>
                item.CharacterId == characterId), Is.True);
        }

        private static void WithContext(
            long gold,
            Action<Context> body,
            Action<ProfileSaveData> configureProfile = null)
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
                LocalizationService localization = AssetDatabase
                    .LoadAssetAtPath<LocalizationCatalog>(LocalizationPath)
                    .CreateService();
                ProfileSaveData profile = SaveTestDataBuilder.Valid();
                profile.Currencies.GachaCurrency = gold;
                configureProfile?.Invoke(profile);
                var repository = new FakeSaveRepository
                {
                    MainText = SaveTestDataBuilder.Json(profile)
                };
                SaveService saveService = SaveServiceTestFactory.Create(
                    repository,
                    new FixedUnixTimeProvider(100, 101, 102, 103, 104));
                SaveLoadResult load = saveService.LoadOrCreate("ignored");
                Assert.That(load.CanUseProfile, Is.True, load.Message);
                repository.Calls.Clear();
                var transactionService = new GachaTransactionService(
                    saveService,
                    database,
                    new MinimumRandomSource(),
                    new SequentialRelicIdProvider());
                installer.Controller.Initialize(
                    saveService,
                    database,
                    localization,
                    transactionService);

                body(new Context(
                    content,
                    installer,
                    database,
                    localization,
                    repository,
                    saveService,
                    transactionService));
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

        private sealed class Context
        {
            public Context(
                Transform content,
                GachaSceneInstaller installer,
                DefinitionDatabase database,
                LocalizationService localization,
                FakeSaveRepository repository,
                SaveService saveService,
                GachaTransactionService transactionService)
            {
                Content = content;
                Installer = installer;
                Database = database;
                Localization = localization;
                Repository = repository;
                SaveService = saveService;
                TransactionService = transactionService;
            }

            public Transform Content { get; }
            public GachaSceneInstaller Installer { get; }
            public GachaScreenController Controller =>
                Installer.Controller;
            public GachaScreenView View => Installer.View;
            public GachaPresentationController Presentation =>
                Installer.PresentationController;
            public GachaResultView ResultView =>
                Installer.ResultPresenter.View;
            public ExchangeScreenController Exchange =>
                Installer.ExchangeController;
            public DefinitionDatabase Database { get; }
            public LocalizationService Localization { get; }
            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public GachaTransactionService TransactionService { get; }
        }

        private sealed class MinimumRandomSource : IRandomSource
        {
            public int Next(int minInclusive, int maxExclusive) =>
                minInclusive;

            public float NextFloat() => 0f;
        }

        private sealed class PersistedMismatchExecutor
            : IExchangeTransactionExecutor
        {
            private readonly IExchangeTransactionExecutor inner;

            public PersistedMismatchExecutor(
                IExchangeTransactionExecutor inner)
            {
                this.inner = inner;
            }

            public int CallCount { get; private set; }

            public ExchangeTransactionOperationResult Execute(
                ExchangeTransactionRequest request)
            {
                CallCount++;
                ExchangeTransactionOperationResult persisted =
                    inner.Execute(request);
                Assert.That(
                    persisted.IsSuccess,
                    Is.True,
                    persisted.Message);
                ConstructorInfo constructor = typeof(
                        ExchangeTransactionOperationResult)
                    .GetConstructors(
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single();
                return (ExchangeTransactionOperationResult)
                    constructor.Invoke(new object[]
                    {
                        ExchangeTransactionStatus.PersistedStateMismatch,
                        null,
                        persisted.SaveTransactionResult,
                        null,
                        "Injected persisted state mismatch."
                    });
            }
        }

        private sealed class SequentialRelicIdProvider
            : IRelicInstanceIdProvider
        {
            private int next;

            public string CreateRelicInstanceId() =>
                "gacha_test_relic_" + next++;
        }
    }
}
