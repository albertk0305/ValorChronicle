using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Progression;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Characters.Presentation
{
    public sealed class CharacterUpgradeScreenTests
    {
        private const string MareaId = "character_marea_bluefang";

        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [TestCase(1, 0, 900L, 180L)]
        [TestCase(100, 0, 3400L, 1050L)]
        [TestCase(100, 1, 3400L, 1103L)]
        [TestCase(100, 2, 3570L, 1103L)]
        [TestCase(100, 3, 3570L, 1103L)]
        [TestCase(100, 4, 3570L, 1103L)]
        [TestCase(100, 5, 3570L, 1103L)]
        [TestCase(100, 6, 3570L, 1103L)]
        public void TryPresent_UsesCharacterBuildForMareaStats(
            int level,
            int awakening,
            long expectedHp,
            long expectedAttack)
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, level, awakening, long.MaxValue),
                new[] { MareaDefinition() });

            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            Assert.That(fixture.UpgradeController.CurrentModel.MaxHp,
                Is.EqualTo(expectedHp));
            Assert.That(fixture.UpgradeController.CurrentModel.Attack,
                Is.EqualTo(expectedAttack));
            Assert.That(fixture.UpgradeView.HpText.text,
                Is.EqualTo(expectedHp.ToString()));
            Assert.That(fixture.UpgradeView.AttackText.text,
                Is.EqualTo(expectedAttack.ToString()));
        }

        [TestCase(40, 6)]
        [TestCase(100, 2)]
        public void CharacterUpgradeAndPartyRoster_ResolveSameSavedStats(
            int level,
            int awakening)
        {
            CharacterDefinition marea = MareaDefinition();
            ProfileSaveData profile = Profile(
                MareaId,
                level,
                awakening,
                long.MaxValue);
            Fixture fixture = CreateFixture(profile, new[] { marea });
            var partyRosterBuilder = new PartyCharacterRosterBuilder(
                Database(new[] { marea }));

            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            CharacterRosterEntry partyEntry = partyRosterBuilder.Build(
                profile.Characters).Single();

            Assert.That(partyEntry.MaxHp,
                Is.EqualTo(fixture.UpgradeController.CurrentModel.MaxHp));
            Assert.That(partyEntry.Attack,
                Is.EqualTo(fixture.UpgradeController.CurrentModel.Attack));
        }

        [Test]
        public void TryPresent_BindsSavedValuesPresentationAndLongCurrency()
        {
            CharacterDefinition definition = MareaDefinition();
            Fixture fixture = CreateFixture(
                Profile(MareaId, 40, 3, long.MaxValue - 7),
                new[] { definition });

            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            CharacterUpgradePresentationModel model =
                fixture.UpgradeController.CurrentModel;
            Assert.That(model.CharacterId, Is.EqualTo(MareaId));
            Assert.That(model.DisplayName, Is.EqualTo("Marea Bluefang"));
            Assert.That(model.Level, Is.EqualTo(40));
            Assert.That(model.Awakening, Is.EqualTo(3));
            Assert.That(model.BattleRecords, Is.EqualTo(long.MaxValue - 7));
            Assert.That(model.FullArtSprite, Is.SameAs(fixture.FullArt));
            Assert.That(model.ElementIcon, Is.SameAs(fixture.WaterIcon));
            Assert.That(fixture.UpgradeView.CharacterNameText.text,
                Is.EqualTo("Marea Bluefang"));
            Assert.That(fixture.UpgradeView.LevelText.text, Is.EqualTo("40"));
            Assert.That(fixture.UpgradeView.AwakeningText.text,
                Is.EqualTo("3"));
            Assert.That(fixture.UpgradeView.BattleRecordsText.text,
                Is.EqualTo((long.MaxValue - 7).ToString()));
            Assert.That(fixture.UpgradeView.CharacterFullImage.sprite,
                Is.SameAs(fixture.FullArt));
            Assert.That(fixture.UpgradeView.TypeIcon.sprite,
                Is.SameAs(fixture.WaterIcon));
        }

        [Test]
        public void LocaleChangeRelocalizesOpenDetailWithoutChangingStats()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 40, 3, 777L),
                new[] { MareaDefinition() });
            fixture.UpgradeRoot.SetActive(true);
            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            long hp = fixture.UpgradeController.CurrentModel.MaxHp;
            long attack = fixture.UpgradeController.CurrentModel.Attack;

            fixture.LocalizationService.SetLocale("ko");

            Assert.That(fixture.UpgradeController.CurrentModel.DisplayName,
                Is.EqualTo("마레아 블루팽"));
            Assert.That(fixture.UpgradeView.CharacterNameText.text,
                Is.EqualTo("마레아 블루팽"));
            Assert.That(fixture.UpgradeController.CurrentModel.MaxHp,
                Is.EqualTo(hp));
            Assert.That(fixture.UpgradeController.CurrentModel.Attack,
                Is.EqualTo(attack));
            Assert.That(fixture.UpgradeController.CurrentModel.BattleRecords,
                Is.EqualTo(777L));
        }

        [Test]
        public void UpgradeLookupButtonsSwitchModesAndReturnToSameCharacter()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 40, 3, 777L),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            fixture.SelectRoot.SetActive(false);
            fixture.UpgradeRoot.SetActive(true);

            fixture.UpgradeView.AwakeningLookupButton.onClick.Invoke();

            Assert.That(fixture.SelectRoot.activeSelf, Is.False);
            Assert.That(fixture.UpgradeRoot.activeSelf, Is.False);
            Assert.That(fixture.LookupRoot.activeSelf, Is.True);
            Assert.That(fixture.LookupController.CurrentCharacterId,
                Is.EqualTo(MareaId));
            Assert.That(fixture.LookupController.Mode,
                Is.EqualTo(CharacterLookupMode.Awakening));
            Assert.That(fixture.LookupController.SelectedAwakeningStage,
                Is.EqualTo(1));
            Assert.That(fixture.LookupController.SelectedSkillType,
                Is.EqualTo(CharacterLookupSkillType.Match3));
            Assert.That(fixture.LookupView.AwakeningLookupRoot.activeSelf,
                Is.True);
            Assert.That(fixture.LookupView.SkillLookupRoot.activeSelf,
                Is.False);
            Assert.That(fixture.LookupView.DescriptionText.text,
                Does.StartWith("Awakening 1 — Sharpened Edge\n\n"));
            Assert.That(fixture.LookupView.AwakeningModeButton.interactable,
                Is.False);
            Assert.That(fixture.LookupView.SkillsModeButton.interactable,
                Is.True);
            Assert.That(fixture.LookupView.AwakeningButtons[0].interactable,
                Is.False);

            fixture.LookupView.SkillsModeButton.onClick.Invoke();

            Assert.That(fixture.LookupController.Mode,
                Is.EqualTo(CharacterLookupMode.Skills));
            Assert.That(fixture.LookupView.DescriptionText.text,
                Does.Contain("90%"));
            Assert.That(fixture.LookupView.AwakeningLookupRoot.activeSelf,
                Is.False);
            Assert.That(fixture.LookupView.SkillLookupRoot.activeSelf,
                Is.True);
            Assert.That(fixture.LookupView.AwakeningModeButton.interactable,
                Is.True);
            Assert.That(fixture.LookupView.SkillsModeButton.interactable,
                Is.False);
            Assert.That(fixture.LookupView.SkillButtons[0].interactable,
                Is.False);

            fixture.LookupView.AwakeningModeButton.onClick.Invoke();

            Assert.That(fixture.LookupController.Mode,
                Is.EqualTo(CharacterLookupMode.Awakening));
            Assert.That(fixture.LookupView.AwakeningLookupRoot.activeSelf,
                Is.True);
            Assert.That(fixture.LookupView.SkillLookupRoot.activeSelf,
                Is.False);

            fixture.LookupView.SkillsModeButton.onClick.Invoke();

            fixture.LookupView.ReturnButton.onClick.Invoke();

            Assert.That(fixture.LookupRoot.activeSelf, Is.False);
            Assert.That(fixture.UpgradeRoot.activeSelf, Is.True);
            Assert.That(fixture.SelectRoot.activeSelf, Is.False);
            Assert.That(fixture.UpgradeController.CurrentModel.CharacterId,
                Is.EqualTo(MareaId));

            fixture.UpgradeView.SkillsLookupButton.onClick.Invoke();

            Assert.That(fixture.LookupRoot.activeSelf, Is.True);
            Assert.That(fixture.UpgradeRoot.activeSelf, Is.False);
            Assert.That(fixture.LookupController.Mode,
                Is.EqualTo(CharacterLookupMode.Skills));
        }

        [Test]
        public void LookupSelectionEventsFireOnceAndStateSurvivesReopen()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 40, 3, 777L),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            fixture.SelectRoot.SetActive(false);
            fixture.UpgradeRoot.SetActive(true);
            fixture.UpgradeView.AwakeningLookupButton.onClick.Invoke();
            var awakeningEvents = new List<int>();
            var skillEvents = new List<CharacterLookupSkillType>();
            fixture.LookupController.AwakeningStageSelected +=
                awakeningEvents.Add;
            fixture.LookupController.SkillTypeSelected += skillEvents.Add;

            for (int index = 0;
                index < fixture.LookupView.AwakeningButtons.Count;
                index++)
            {
                fixture.LookupView.AwakeningButtons[index].onClick.Invoke();
            }

            fixture.LookupView.SkillsModeButton.onClick.Invoke();
            for (int index = 0;
                index < fixture.LookupView.SkillButtons.Count;
                index++)
            {
                fixture.LookupView.SkillButtons[index].onClick.Invoke();
            }

            Assert.That(awakeningEvents,
                Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(skillEvents, Is.EqualTo(new[]
            {
                CharacterLookupSkillType.Match3,
                CharacterLookupSkillType.Match4,
                CharacterLookupSkillType.Match5,
                CharacterLookupSkillType.Passive,
                CharacterLookupSkillType.Active
            }));
            Assert.That(fixture.LookupController.SelectedAwakeningStage,
                Is.EqualTo(6));
            Assert.That(fixture.LookupController.SelectedSkillType,
                Is.EqualTo(CharacterLookupSkillType.Active));

            for (int iteration = 0; iteration < 3; iteration++)
            {
                fixture.LookupView.ReturnButton.onClick.Invoke();
                fixture.UpgradeView.AwakeningLookupButton.onClick.Invoke();
            }

            int eventCountBeforeClick = awakeningEvents.Count;
            fixture.LookupView.AwakeningButtons[3].onClick.Invoke();
            Assert.That(awakeningEvents.Count,
                Is.EqualTo(eventCountBeforeClick + 1));
            Assert.That(fixture.LookupController.SelectedAwakeningStage,
                Is.EqualTo(4));
            Assert.That(fixture.LookupController.SelectedSkillType,
                Is.EqualTo(CharacterLookupSkillType.Active));
            Assert.That(fixture.LookupController.CurrentCharacterId,
                Is.EqualTo(MareaId));
        }

        [Test]
        public void LookupLocaleRefreshPreservesProfileAndSelection()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 40, 3, 777L),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            fixture.SelectRoot.SetActive(false);
            fixture.UpgradeRoot.SetActive(true);
            fixture.UpgradeView.AwakeningLookupButton.onClick.Invoke();
            fixture.LookupView.AwakeningButtons[4].onClick.Invoke();
            fixture.Repository.Calls.Clear();
            ProfileSaveData before =
                fixture.SaveService.GetCurrentProfileSnapshot();
            string englishDescription =
                fixture.LookupView.DescriptionText.text;
            int presentationCount = 0;
            fixture.LookupController.PresentationChanged += _ =>
                presentationCount++;

            fixture.LocalizationService.SetLocale("ko");

            ProfileSaveData after =
                fixture.SaveService.GetCurrentProfileSnapshot();
            Assert.That(presentationCount, Is.EqualTo(1));
            Assert.That(fixture.LookupController.CurrentCharacterId,
                Is.EqualTo(MareaId));
            Assert.That(fixture.LookupController.SelectedAwakeningStage,
                Is.EqualTo(5));
            Assert.That(fixture.LookupController.SelectedSkillType,
                Is.EqualTo(CharacterLookupSkillType.Match3));
            Assert.That(fixture.LookupView.DescriptionText.text,
                Does.Contain("\n\nKO "));
            Assert.That(fixture.LookupView.DescriptionText.text,
                Does.StartWith("Awakening 5 — Storm Devour\n\n"));
            Assert.That(fixture.LookupView.DescriptionText.text,
                Is.Not.EqualTo(englishDescription));
            Assert.That(after.Characters[0].Level,
                Is.EqualTo(before.Characters[0].Level));
            Assert.That(after.Characters[0].Awakening,
                Is.EqualTo(before.Characters[0].Awakening));
            Assert.That(after.Currencies.BattleRecords,
                Is.EqualTo(before.Currencies.BattleRecords));
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.Zero);
        }

        [Test]
        public void LookupDescriptionsFollowSelectionAndLatestSavedAwakening()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 40, 0, 777L),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            fixture.SelectRoot.SetActive(false);
            fixture.UpgradeRoot.SetActive(true);

            fixture.UpgradeView.SkillsLookupButton.onClick.Invoke();

            Assert.That(fixture.LookupView.DescriptionText.text,
                Does.Contain("90%"));
            var descriptions = new HashSet<string>();
            for (int index = 0;
                index < fixture.LookupView.SkillButtons.Count;
                index++)
            {
                fixture.LookupView.SkillButtons[index].onClick.Invoke();
                descriptions.Add(fixture.LookupView.DescriptionText.text);
            }

            Assert.That(descriptions, Has.Count.EqualTo(5));

            fixture.LookupView.AwakeningModeButton.onClick.Invoke();
            string[] awakeningNames =
            {
                "Sharpened Edge",
                "Tough Mariner",
                "Blood Banquet",
                "Razor Cuts",
                "Storm Devour",
                "Lingering Surge"
            };
            for (int index = 0;
                index < fixture.LookupView.AwakeningButtons.Count;
                index++)
            {
                fixture.LookupView.AwakeningButtons[index].onClick.Invoke();
                Assert.That(fixture.LookupView.DescriptionText.text,
                    Does.StartWith(
                        $"Awakening {index + 1} — "
                            + $"{awakeningNames[index]}\n\n"));
            }

            fixture.LookupView.SkillsModeButton.onClick.Invoke();
            fixture.LookupView.SkillButtons[0].onClick.Invoke();
            fixture.LookupView.ReturnButton.onClick.Invoke();
            SaveTransactionResult transaction = fixture.SaveService
                .ExecuteTransaction(profile =>
                    profile.Characters[0].Awakening = 6);
            Assert.That(transaction.IsSuccess, Is.True);

            fixture.UpgradeView.SkillsLookupButton.onClick.Invoke();

            Assert.That(fixture.LookupController.SelectedSkillType,
                Is.EqualTo(CharacterLookupSkillType.Match3));
            Assert.That(fixture.LookupView.DescriptionText.text,
                Does.Contain("100%"));
            Assert.That(fixture.LookupView.DescriptionText.text,
                Does.Contain("Lingering Surge"));
        }

        [TestCase(1, 120L)]
        [TestCase(40, 900L)]
        [TestCase(99, 2080L)]
        public void TryPresent_ShowsNextCostAndAffordability(
            int level,
            long expectedCost)
        {
            Fixture affordable = CreateFixture(
                Profile(MareaId, level, 0, expectedCost),
                new[] { MareaDefinition() });
            Assert.That(affordable.UpgradeController.TryPresent(MareaId),
                Is.True);
            Assert.That(affordable.UpgradeController.CurrentModel.NextLevelCost,
                Is.EqualTo(expectedCost));
            Assert.That(affordable.UpgradeController.CurrentModel.CanLevelUp,
                Is.True);
            Assert.That(affordable.UpgradeView.LevelUpButton.interactable,
                Is.True);
            Assert.That(affordable.UpgradeView.LevelUpCostText.text,
                Is.EqualTo(expectedCost.ToString()));
            Assert.That(affordable.UpgradeView.LevelUpCostIcon.gameObject.activeSelf,
                Is.True);

            Fixture insufficient = CreateFixture(
                Profile(MareaId, level, 0, expectedCost - 1),
                new[] { MareaDefinition() });
            Assert.That(insufficient.UpgradeController.TryPresent(MareaId),
                Is.True);
            Assert.That(insufficient.UpgradeController.CurrentModel.CanLevelUp,
                Is.False);
            Assert.That(insufficient.UpgradeView.LevelUpButton.interactable,
                Is.False);
            Assert.That(insufficient.UpgradeView.LevelUpCostText.gameObject.activeSelf,
                Is.True);
            Assert.That(insufficient.UpgradeView.LevelUpCostIcon.gameObject.activeSelf,
                Is.True);
            Assert.That(insufficient.UpgradeView.LevelUpPressHoldInput
                .TryBeginPress(99), Is.False);
        }

        [Test]
        public void TryPresent_MaximumLevelHidesCostAndDisablesButton()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 100, 6, long.MaxValue),
                new[] { MareaDefinition() });

            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            Assert.That(fixture.UpgradeController.CurrentModel.CanLevelUp,
                Is.False);
            Assert.That(fixture.UpgradeController.CurrentModel.ShowLevelUpCost,
                Is.False);
            Assert.That(fixture.UpgradeView.LevelUpButton.interactable,
                Is.False);
            Assert.That(fixture.UpgradeView.LevelUpCostIcon.gameObject.activeSelf,
                Is.False);
            Assert.That(fixture.UpgradeView.LevelUpCostText.gameObject.activeSelf,
                Is.False);
        }

        [Test]
        public void TryPresent_ReadsLatestAuthoritativeSnapshot()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 1, 0, 119),
                new[] { MareaDefinition() });
            SaveTransactionResult transaction = fixture.SaveService
                .ExecuteTransaction(profile =>
                {
                    profile.Characters[0].Level = 40;
                    profile.Characters[0].Awakening = 2;
                    profile.Currencies.BattleRecords = 900;
                });
            Assert.That(transaction.IsSuccess, Is.True);

            Assert.That(fixture.UpgradeController.TryPresent(MareaId),
                Is.True);
            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(40));
            Assert.That(fixture.UpgradeController.CurrentModel.Awakening,
                Is.EqualTo(2));
            Assert.That(fixture.UpgradeController.CurrentModel.BattleRecords,
                Is.EqualTo(900));
            Assert.That(fixture.UpgradeController.CurrentModel.CanLevelUp,
                Is.True);
        }

        [Test]
        public void TryPresent_MissingCharacterOrDefinitionFailsSafely()
        {
            Fixture missingCharacter = CreateFixture(
                Profile(MareaId, 1, 0, 1000),
                new[] { MareaDefinition() });
            Assert.That(
                missingCharacter.UpgradeController.TryPresent("missing"),
                Is.False);
            Assert.That(missingCharacter.UpgradeController.CurrentModel,
                Is.Null);

            Fixture missingDefinition = CreateFixture(
                Profile(MareaId, 1, 0, 1000),
                Array.Empty<CharacterDefinition>(),
                includePresentation: false);
            Assert.That(
                missingDefinition.UpgradeController.TryPresent(MareaId),
                Is.False);
            Assert.That(missingDefinition.UpgradeController.CurrentModel,
                Is.Null);
        }

        [Test]
        public void RosterSelectionAndReturn_ToggleScreensAndPreserveState()
        {
            CharacterDefinition marea = MareaDefinition();
            CharacterDefinition fire = Definition(
                "character_fire",
                "fire",
                ElementType.Fire);
            var definitions = new List<CharacterDefinition>
            {
                marea,
                fire
            };
            var savedCharacters = new List<(string, int, int)>
            {
                (MareaId, 40, 3),
                (fire.Id, 20, 5)
            };
            for (int index = 0; index < 34; index++)
            {
                CharacterDefinition water = Definition(
                    $"character_water_{index:D2}",
                    $"water_{index:D2}",
                    ElementType.Water);
                definitions.Add(water);
                savedCharacters.Add((water.Id, index + 1, 0));
            }

            Fixture fixture = CreateFixture(
                Profile(savedCharacters.ToArray(), 1000),
                definitions.ToArray());
            ElementFilterButtonView waterFilter = fixture.RosterView.FilterButtons
                .Single(item => item.Element == ElementType.Water);
            waterFilter.Button.onClick.Invoke();
            fixture.RosterView.AwakeningSortButton.onClick.Invoke();
            fixture.RosterView.CharacterGrid.SetScrollOffsetForTesting(330f);
            float scrollPosition = fixture.RosterView.CharacterGrid
                .ScrollOffset;
            Assert.That(scrollPosition, Is.GreaterThan(0f));
            CharacterRosterCellView cell = fixture.RosterView.CharacterGrid
                .PoolCells.First(item => item.IsBound);
            string selectedCharacterId = cell.CharacterId;

            cell.Button.onClick.Invoke();

            Assert.That(fixture.RosterController.SelectedCharacterId,
                Is.EqualTo(selectedCharacterId));
            Assert.That(fixture.SelectRoot.activeSelf, Is.False);
            Assert.That(fixture.UpgradeRoot.activeSelf, Is.True);
            Assert.That(fixture.UpgradeController.CurrentModel.CharacterId,
                Is.EqualTo(selectedCharacterId));
            int startingDetailLevel =
                fixture.UpgradeController.CurrentModel.Level;

            Assert.That(fixture.UpgradeView.LevelUpPressHoldInput
                .TryBeginPress(99), Is.True);
            fixture.UpgradeView.LevelUpPressHoldInput.Release(99);

            Assert.That(fixture.RosterController.FullRoster.Single(
                item => item.CharacterId == selectedCharacterId).Level,
                Is.EqualTo(startingDetailLevel + 1));
            Assert.That(fixture.RosterController.SelectedElement,
                Is.EqualTo(ElementType.Water));
            Assert.That(fixture.RosterController.SortMode,
                Is.EqualTo(CharacterRosterSortMode.Awakening));
            Assert.That(fixture.RosterView.CharacterGrid.ScrollOffset,
                Is.EqualTo(scrollPosition));

            VirtualizedCharacterGridView grid =
                fixture.RosterView.CharacterGrid;
            InvokeLifecycle(grid, "OnEnable");
            Assert.That(grid.ItemCount, Is.EqualTo(35));
            Assert.That(
                grid.PoolCells.Count(item => item.IsBound),
                Is.Zero);

            fixture.UpgradeView.ReturnButton.onClick.Invoke();

            Assert.That(fixture.SelectRoot.activeSelf, Is.True);
            Assert.That(fixture.UpgradeRoot.activeSelf, Is.False);
            Assert.That(fixture.RosterController.SelectedElement,
                Is.EqualTo(ElementType.Water));
            Assert.That(fixture.RosterController.SortMode,
                Is.EqualTo(CharacterRosterSortMode.Awakening));
            Assert.That(fixture.RosterView.CharacterGrid.ScrollOffset,
                Is.EqualTo(scrollPosition));
            Assert.That(grid.ItemCount, Is.EqualTo(35));
            Assert.That(grid.BoundItemCount, Is.GreaterThan(0));
            Assert.That(
                fixture.RosterController.FullRoster.Single(
                    item => item.CharacterId == selectedCharacterId).Level,
                Is.EqualTo(startingDetailLevel + 1));
            Assert.That(
                fixture.RosterController.SelectedCharacterId,
                Is.EqualTo(selectedCharacterId));
        }

        [Test]
        public void RepeatedScreenSwitching_DoesNotDuplicateListeners()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 1, 0, 1000),
                new[] { MareaDefinition() });
            fixture.Repository.Calls.Clear();
            VirtualizedCharacterGridView grid =
                fixture.RosterView.CharacterGrid;
            int poolSize = grid.PoolSize;

            for (int index = 0; index < 3; index++)
            {
                CharacterRosterCellView cell = grid.PoolCells.Single(
                    item => item.IsBound);
                cell.Button.onClick.Invoke();
                Assert.That(fixture.UpgradeView.LevelUpPressHoldInput
                    .TryBeginPress(index + 1), Is.True);
                fixture.UpgradeView.LevelUpPressHoldInput.Release(index + 1);
                InvokeLifecycle(grid, "OnEnable");
                Assert.That(grid.ItemCount, Is.EqualTo(1));
                Assert.That(
                    grid.PoolCells.Count(item => item.IsBound),
                    Is.Zero);
                fixture.UpgradeView.ReturnButton.onClick.Invoke();

                Assert.That(grid.ItemCount, Is.EqualTo(1));
                Assert.That(grid.BoundItemCount, Is.EqualTo(1));
                Assert.That(
                    grid.PoolCells.Single(item => item.IsBound).CharacterId,
                    Is.EqualTo(MareaId));
                Assert.That(grid.PoolSize, Is.EqualTo(poolSize));
            }

            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(4));
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.EqualTo(3));
        }

        [Test]
        public void ShortPress_PreviewsImmediatelyAndCommitsExactlyOnce()
        {
            ProfileSaveData profile = Profile(MareaId, 1, 0, 1000);
            profile.Currencies.GachaCurrency = 91;
            profile.Currencies.HeroTokens = 92;
            profile.Currencies.RelicTokens = 93;
            Fixture fixture = CreateFixture(
                profile,
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);
            fixture.Repository.Calls.Clear();
            bool secondPressAcceptedWhileBusy = true;
            fixture.UpgradeController.AuthoritativeProfileRefreshRequested +=
                () =>
                {
                    secondPressAcceptedWhileBusy = fixture.UpgradeView
                        .LevelUpPressHoldInput.TryBeginPress(11);
                };

            Assert.That(fixture.UpgradeView.LevelUpPressHoldInput
                .TryBeginPress(10), Is.True);
            Assert.That(fixture.UpgradeController.PreviewSession.CharacterId,
                Is.EqualTo(MareaId));
            Assert.That(fixture.UpgradeController.PreviewSession.StartingLevel,
                Is.EqualTo(1));
            Assert.That(fixture.UpgradeController.PreviewSession
                .StartingBattleRecords, Is.EqualTo(1000));
            Assert.That(fixture.UpgradeController.PreviewSession.Awakening,
                Is.Zero);
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(2));
            Assert.That(fixture.UpgradeView.LevelText.text, Is.EqualTo("2"));
            Assert.That(fixture.UpgradeView.BattleRecordsText.text,
                Is.EqualTo("880"));
            Assert.That(fixture.UpgradeView.ReturnButton.interactable,
                Is.False);
            fixture.UpgradeView.LevelUpPressHoldInput
                .AdvanceTimeForTesting(0.44f);
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(2));

            fixture.UpgradeView.LevelUpPressHoldInput.Release(10);
            fixture.UpgradeView.LevelUpButton.onClick.Invoke();

            ProfileSaveData saved = fixture.SaveService
                .GetCurrentProfileSnapshot();
            Assert.That(saved.Characters[0].Level, Is.EqualTo(2));
            Assert.That(saved.Currencies.BattleRecords, Is.EqualTo(880));
            Assert.That(saved.Currencies.GachaCurrency, Is.EqualTo(91));
            Assert.That(saved.Currencies.HeroTokens, Is.EqualTo(92));
            Assert.That(saved.Currencies.RelicTokens, Is.EqualTo(93));
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.EqualTo(1));
            Assert.That(fixture.UpgradeController.PreviewSession, Is.Null);
            Assert.That(secondPressAcceptedWhileBusy, Is.False);
        }

        [Test]
        public void Hold_UsesConfiguredTimingAndCommitsFinalTargetOnce()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 20, 0, 100000),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);
            CharacterLevelUpPressHoldInput input =
                fixture.UpgradeView.LevelUpPressHoldInput;
            fixture.Repository.Calls.Clear();

            Assert.That(input.InitialRepeatDelay, Is.EqualTo(0.45f));
            Assert.That(input.RepeatInterval, Is.EqualTo(0.12f));
            Assert.That(input.TryBeginPress(1), Is.True);
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(21));
            input.AdvanceTimeForTesting(0.449f);
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(21));
            input.AdvanceTimeForTesting(0.002f);
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(22));
            input.AdvanceTimeForTesting(0.25f);
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(24));
            Assert.That(input.TryBeginPress(2), Is.False);

            input.Release(1);

            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(24));
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.EqualTo(1));
        }

        [Test]
        public void Hold_StopsAtAffordableTargetAndUsesExactPreviewBalance()
        {
            long exactCost = CharacterLevelUpCostCalculator.GetTotalCost(10, 14);
            Fixture fixture = CreateFixture(
                Profile(MareaId, 10, 0, exactCost),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);
            CharacterLevelUpPressHoldInput input =
                fixture.UpgradeView.LevelUpPressHoldInput;

            Assert.That(input.TryBeginPress(1), Is.True);
            input.AdvanceTimeForTesting(5f);

            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(14));
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewRemainingBattleRecords, Is.Zero);
            Assert.That(fixture.UpgradeView.BattleRecordsText.text,
                Is.EqualTo("0"));
            input.Release(1);
            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(14));
        }

        [Test]
        public void Level99_PreviewsAndCommitsMaximumLevelThenRejectsNewPress()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 99, 0, 2080),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);
            CharacterLevelUpPressHoldInput input =
                fixture.UpgradeView.LevelUpPressHoldInput;

            Assert.That(input.TryBeginPress(1), Is.True);
            input.AdvanceTimeForTesting(5f);
            Assert.That(fixture.UpgradeController.PreviewSession
                .PreviewTargetLevel, Is.EqualTo(100));
            Assert.That(fixture.UpgradeView.LevelUpCostIcon.gameObject.activeSelf,
                Is.False);
            Assert.That(fixture.UpgradeView.LevelUpCostText.gameObject.activeSelf,
                Is.False);
            input.Release(1);

            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(100));
            Assert.That(input.TryBeginPress(2), Is.False);
        }

        [TestCase(0, 3400L, 1050L)]
        [TestCase(1, 3400L, 1103L)]
        [TestCase(2, 3570L, 1103L)]
        public void PreviewStats_UseCharacterBuildResolver(
            int awakening,
            long expectedHp,
            long expectedAttack)
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 99, awakening, 2080),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);

            fixture.UpgradeView.LevelUpPressHoldInput.TryBeginPress(1);

            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(100));
            Assert.That(fixture.UpgradeController.CurrentModel.MaxHp,
                Is.EqualTo(expectedHp));
            Assert.That(fixture.UpgradeController.CurrentModel.Attack,
                Is.EqualTo(expectedAttack));
            fixture.UpgradeView.LevelUpPressHoldInput.Release(1);
            Assert.That(fixture.UpgradeController.CurrentModel.MaxHp,
                Is.EqualTo(expectedHp));
            Assert.That(fixture.UpgradeController.CurrentModel.Attack,
                Is.EqualTo(expectedAttack));
        }

        [Test]
        public void CancelAndDisable_RestoreAuthoritativeStateWithoutSaving()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 10, 0, 10000),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);
            CharacterUpgradePresentationModel authoritative =
                fixture.UpgradeController.CurrentModel;
            fixture.SelectRoot.SetActive(false);
            fixture.UpgradeRoot.SetActive(true);
            CharacterLevelUpPressHoldInput input =
                fixture.UpgradeView.LevelUpPressHoldInput;
            fixture.Repository.Calls.Clear();

            input.TryBeginPress(1);
            input.AdvanceTimeForTesting(1f);
            input.Cancel(1);
            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(authoritative.Level));
            Assert.That(fixture.UpgradeController.CurrentModel.MaxHp,
                Is.EqualTo(authoritative.MaxHp));
            Assert.That(fixture.UpgradeController.CurrentModel.Attack,
                Is.EqualTo(authoritative.Attack));
            Assert.That(fixture.UpgradeController.CurrentModel.BattleRecords,
                Is.EqualTo(authoritative.BattleRecords));
            Assert.That(fixture.UpgradeController.PreviewSession, Is.Null);
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.Zero);

            input.TryBeginPress(2);
            input.AdvanceTimeForTesting(1f);
            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(16));
            CharacterUpgradePresentationModel inactivePreviewModel =
                fixture.UpgradeView.CurrentModel;
            fixture.UpgradeRoot.SetActive(false);
            InvokeLifecycle(fixture.UpgradeView, "OnDisable");
            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(authoritative.Level));
            Assert.That(fixture.UpgradeController.CurrentModel.MaxHp,
                Is.EqualTo(authoritative.MaxHp));
            Assert.That(fixture.UpgradeController.CurrentModel.Attack,
                Is.EqualTo(authoritative.Attack));
            Assert.That(fixture.UpgradeController.CurrentModel.BattleRecords,
                Is.EqualTo(authoritative.BattleRecords));
            Assert.That(fixture.UpgradeView.CurrentModel,
                Is.SameAs(inactivePreviewModel));
            Assert.That(fixture.UpgradeController.PreviewSession, Is.Null);
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.Zero);
            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(10));

            InvokeLifecycle(fixture.UpgradeController, "OnDisable");
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.Zero);
            InvokeLifecycle(fixture.UpgradeController, "OnEnable");
            fixture.UpgradeRoot.SetActive(true);
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);
            Assert.That(fixture.UpgradeView.CurrentModel.Level,
                Is.EqualTo(10));

            Assert.That(input.TryBeginPress(3), Is.True);
            input.AdvanceTimeForTesting(1f);
            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(16));
            InvokeLifecycle(fixture.UpgradeController, "OnDisable");
            fixture.UpgradeRoot.SetActive(false);
            InvokeLifecycle(fixture.UpgradeView, "OnDisable");
            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(10));
            Assert.That(fixture.UpgradeController.PreviewSession, Is.Null);
            Assert.That(fixture.Repository.Count("WriteTemp"), Is.Zero);
            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(10));
        }

        [Test]
        public void PersistenceFailure_RollsBackPreviewAndAllowsRetry()
        {
            Fixture fixture = CreateFixture(
                Profile(MareaId, 1, 0, 1000),
                new[] { MareaDefinition() });
            Assert.That(fixture.UpgradeController.TryPresent(MareaId), Is.True);
            CharacterLevelUpPressHoldInput input =
                fixture.UpgradeView.LevelUpPressHoldInput;
            fixture.Repository.FailWriteTemp = true;

            input.TryBeginPress(1);
            input.Release(1);

            Assert.That(fixture.UpgradeController.LastPersistenceResult
                .IsSuccess, Is.False);
            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(1));
            Assert.That(fixture.UpgradeController.CurrentModel.Level,
                Is.EqualTo(1));
            Assert.That(fixture.UpgradeController.IsOperationInProgress,
                Is.False);

            fixture.Repository.FailWriteTemp = false;
            Assert.That(input.TryBeginPress(2), Is.True);
            input.Release(2);
            Assert.That(fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].Level, Is.EqualTo(2));
        }

        private Fixture CreateFixture(
            ProfileSaveData profile,
            CharacterDefinition[] definitions,
            bool includePresentation = true)
        {
            DefinitionDatabase database = Database(definitions);
            SaveService saveService = SaveServiceFor(
                profile,
                out FakeSaveRepository repository);
            Sprite fullArt = Sprite("FullArt");
            Sprite waterIcon = Sprite("WaterIcon");
            CharacterPresentationCatalog catalog = Catalog(
                includePresentation ? definitions : Array.Empty<CharacterDefinition>(),
                fullArt);
            ElementIconSet icons = Icons(waterIcon);
            TextAsset localizationAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/Data/Localization/characters.tsv");
            Assert.That(localizationAsset, Is.Not.Null);
            string localizationTsv = localizationAsset.text.Replace(
                "marea\tMarea Bluefang\t",
                "marea\tMarea Bluefang\t마레아 블루팽");
            localizationTsv = localizationTsv.Replace(
                "marea.awakening.5.description\tDevour's coefficient per "
                    + "consumed Water Element changes from "
                    + "{per_water_before}% to {per_water_after}% ATK.\t",
                "marea.awakening.5.description\tDevour's coefficient per "
                    + "consumed Water Element changes from "
                    + "{per_water_before}% to {per_water_after}% ATK.\t"
                    + "KO {per_water_before}% to {per_water_after}%");
            LocalizationService localizationService =
                new LocalizationService(new[]
                {
                    LocalizationTableParser.Parse(
                        "characters.tsv",
                        localizationTsv)
                });

            GameObject container = Object("GameContent");
            GameObject selectRoot = Object("CharacterSelect", container.transform);
            GameObject upgradeRoot = Object(
                "CharacterUpgrade",
                container.transform);
            GameObject lookupRoot = Object(
                "CharacterLookup",
                container.transform);
            CharacterRosterScreenView rosterView = RosterView(selectRoot);
            var rosterController = selectRoot.AddComponent<
                CharacterRosterScreenController>();
            rosterController.Configure(rosterView, catalog, icons);
            rosterController.Initialize(saveService, database);

            CharacterUpgradeScreenView upgradeView = UpgradeView(upgradeRoot);
            var upgradeController = container.AddComponent<
                CharacterUpgradeScreenController>();
            upgradeController.Configure(upgradeView, catalog, icons);
            upgradeController.Initialize(
                saveService,
                database,
                initializedLocalizationService: localizationService);
            CharacterLookupScreenView lookupView = LookupView(lookupRoot);
            var lookupController = container.AddComponent<
                CharacterLookupScreenController>();
            lookupController.Configure(lookupView);
            lookupController.Initialize(
                localizationService,
                saveService,
                CharacterLookupDescriptionResolverFactory.CreateDefault(
                    database,
                    localizationService));
            var coordinator = container.AddComponent<CharacterScreenCoordinator>();
            coordinator.Configure(
                selectRoot,
                upgradeRoot,
                rosterController,
                upgradeController,
                lookupRoot,
                lookupController);

            return new Fixture(
                selectRoot,
                upgradeRoot,
                rosterController,
                rosterView,
                upgradeController,
                upgradeView,
                saveService,
                repository,
                fullArt,
                waterIcon,
                localizationService,
                lookupRoot,
                lookupController,
                lookupView);
        }

        private CharacterRosterScreenView RosterView(GameObject root)
        {
            RectTransform viewport = Rect("Viewport", root.transform);
            viewport.sizeDelta = new Vector2(500f, 300f);
            RectTransform content = Rect("Content", viewport);
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            var cells = new CharacterRosterCellView[5];
            for (int index = 0; index < cells.Length; index++)
            {
                GameObject cellRoot = Object($"Cell{index}", content);
                Image face = cellRoot.AddComponent<Image>();
                Button button = cellRoot.AddComponent<Button>();
                Image type = Object("Type", cellRoot.transform)
                    .AddComponent<Image>();
                GameObject border = Object("Border", cellRoot.transform);
                cells[index] = cellRoot.AddComponent<CharacterRosterCellView>();
                cells[index].Configure(
                    button,
                    face,
                    type,
                    border,
                    Text("Level", cellRoot.transform),
                    Text("Awakening", cellRoot.transform));
            }

            var grid = viewport.gameObject.AddComponent<
                VirtualizedCharacterGridView>();
            grid.Configure(
                scrollRect,
                viewport,
                content,
                cells,
                new Vector2(80f, 80f),
                new Vector2(10f, 10f),
                configuredTopPadding: 0f,
                configuredBottomPadding: 0f,
                configuredBufferRows: 0);
            var filters = new ElementFilterButtonView[5];
            for (int index = 0; index < filters.Length; index++)
            {
                GameObject filterRoot = Object($"Filter{index}", root.transform);
                Image image = filterRoot.AddComponent<Image>();
                Button button = filterRoot.AddComponent<Button>();
                filters[index] = filterRoot.AddComponent<ElementFilterButtonView>();
                filters[index].Configure((ElementType)index, button, image);
            }

            var view = root.AddComponent<CharacterRosterScreenView>();
            view.Configure(
                grid,
                filters,
                ButtonObject("LevelSort", root.transform),
                ButtonObject("AwakeningSort", root.transform),
                Text("Gold", root.transform),
                Text("Records", root.transform));
            return view;
        }

        private CharacterUpgradeScreenView UpgradeView(GameObject root)
        {
            var view = root.AddComponent<CharacterUpgradeScreenView>();
            view.Configure(
                Text("Name", root.transform),
                ImageObject("FullArt", root.transform),
                Text("Records", root.transform),
                ImageObject("Type", root.transform),
                Text("Level", root.transform),
                Text("Awakening", root.transform),
                Text("Attack", root.transform),
                Text("Hp", root.transform),
                ImageObject("CostIcon", root.transform),
                Text("Cost", root.transform),
                ButtonObject("LevelUp", root.transform),
                ButtonObject("Return", root.transform),
                ButtonObject("AwakeningLookup", root.transform),
                ButtonObject("SkillsLookup", root.transform));
            return view;
        }

        private CharacterLookupScreenView LookupView(GameObject root)
        {
            GameObject awakeningRoot = Object(
                "AwakeningLookUp",
                root.transform);
            GameObject skillRoot = Object("SkillLookUp", root.transform);
            var awakeningButtons = new Button[6];
            for (int index = 0; index < awakeningButtons.Length; index++)
            {
                awakeningButtons[index] = ButtonObject(
                    $"Awakening{index + 1}",
                    awakeningRoot.transform);
            }

            var skillButtons = new Button[5];
            for (int index = 0; index < skillButtons.Length; index++)
            {
                skillButtons[index] = ButtonObject(
                    $"Skill{index}",
                    skillRoot.transform);
            }

            var view = root.AddComponent<CharacterLookupScreenView>();
            view.Configure(
                ButtonObject("Return", root.transform),
                ButtonObject("AwakeningMode", root.transform),
                ButtonObject("SkillsMode", root.transform),
                Text("Description", root.transform),
                awakeningRoot,
                skillRoot,
                awakeningButtons,
                skillButtons);
            return view;
        }

        private CharacterDefinition MareaDefinition()
        {
            CharacterDefinition definition = Definition(
                MareaId,
                "marea",
                ElementType.Water);
            SetField(definition, typeof(CharacterDefinition), "level1Hp", 900);
            SetField(definition, typeof(CharacterDefinition), "level1Attack", 180);
            SetField(definition, typeof(CharacterDefinition), "level100Hp", 3400);
            SetField(definition, typeof(CharacterDefinition), "level100Attack", 1050);
            MareaBluefangTestConfig.Assign(
                definition,
                MareaBluefangTestConfig.Canonical);
            return definition;
        }

        private CharacterDefinition Definition(
            string id,
            string displayName,
            ElementType element)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            createdObjects.Add(definition);
            SetField(definition, typeof(GameDefinition), "id", id);
            SetField(
                definition,
                typeof(CharacterDefinition),
                "displayNameKey",
                displayName);
            SetField(definition, typeof(CharacterDefinition), "element", element);
            SetField(definition, typeof(CharacterDefinition), "level1Hp", 100);
            SetField(definition, typeof(CharacterDefinition), "level1Attack", 10);
            SetField(definition, typeof(CharacterDefinition), "level100Hp", 1000);
            SetField(definition, typeof(CharacterDefinition), "level100Attack", 100);
            return definition;
        }

        private DefinitionDatabase Database(CharacterDefinition[] definitions)
        {
            var database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetArray(database, "characters", definitions);
            database.Initialize();
            return database;
        }

        private CharacterPresentationCatalog Catalog(
            CharacterDefinition[] definitions,
            Sprite fullArt)
        {
            var entries = new CharacterPresentationDefinition[definitions.Length];
            for (int index = 0; index < entries.Length; index++)
            {
                entries[index] = ScriptableObject.CreateInstance<
                    CharacterPresentationDefinition>();
                createdObjects.Add(entries[index]);
                SetField(
                    entries[index],
                    typeof(CharacterPresentationDefinition),
                    "characterId",
                    definitions[index].Id);
                SetField(
                    entries[index],
                    typeof(CharacterPresentationDefinition),
                    "previewSprite",
                    fullArt);
            }

            var catalog = ScriptableObject.CreateInstance<
                CharacterPresentationCatalog>();
            createdObjects.Add(catalog);
            SetArray(catalog, "definitions", entries);
            return catalog;
        }

        private ElementIconSet Icons(Sprite waterIcon)
        {
            var icons = ScriptableObject.CreateInstance<ElementIconSet>();
            createdObjects.Add(icons);
            foreach (string field in new[]
            {
                "fire", "water", "grass", "light", "dark"
            })
            {
                SetField(
                    icons,
                    typeof(ElementIconSet),
                    field,
                    field == "water" ? waterIcon : Sprite(field));
            }

            return icons;
        }

        private SaveService SaveServiceFor(
            ProfileSaveData profile,
            out FakeSaveRepository repository)
        {
            repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService service = SaveServiceTestFactory.Create(repository);
            Assert.That(service.LoadOrCreate("ignored").CanUseProfile, Is.True);
            return service;
        }

        private static ProfileSaveData Profile(
            string id,
            int level,
            int awakening,
            long battleRecords)
        {
            return Profile(new[] { (id, level, awakening) }, battleRecords);
        }

        private static ProfileSaveData Profile(
            (string id, int level, int awakening)[] characters,
            long battleRecords)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.BattleRecords = battleRecords;
            foreach ((string id, int level, int awakening) character in characters)
            {
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = character.id,
                    Level = character.level,
                    Awakening = character.awakening
                });
            }

            return profile;
        }

        private Sprite Sprite(string name)
        {
            var texture = new Texture2D(1, 1) { name = name + "Texture" };
            createdObjects.Add(texture);
            Sprite sprite = UnityEngine.Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            sprite.name = name;
            createdObjects.Add(sprite);
            return sprite;
        }

        private GameObject Object(string name, Transform parent = null)
        {
            var created = new GameObject(name, typeof(RectTransform));
            createdObjects.Add(created);
            if (parent != null)
            {
                created.transform.SetParent(parent, false);
            }

            return created;
        }

        private RectTransform Rect(string name, Transform parent)
        {
            return (RectTransform)Object(name, parent).transform;
        }

        private TMP_Text Text(string name, Transform parent)
        {
            return Object(name, parent).AddComponent<TextMeshProUGUI>();
        }

        private Image ImageObject(string name, Transform parent)
        {
            return Object(name, parent).AddComponent<Image>();
        }

        private Button ButtonObject(string name, Transform parent)
        {
            GameObject created = Object(name, parent);
            created.AddComponent<Image>();
            return created.AddComponent<Button>();
        }

        private static void SetArray<T>(
            UnityEngine.Object target,
            string fieldName,
            T[] values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(
            UnityEngine.Object target,
            Type declaringType,
            string fieldName,
            object value)
        {
            var field = declaringType.GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private sealed class Fixture
        {
            public Fixture(
                GameObject selectRoot,
                GameObject upgradeRoot,
                CharacterRosterScreenController rosterController,
                CharacterRosterScreenView rosterView,
                CharacterUpgradeScreenController upgradeController,
                CharacterUpgradeScreenView upgradeView,
                SaveService saveService,
                FakeSaveRepository repository,
                Sprite fullArt,
                Sprite waterIcon,
                LocalizationService localizationService,
                GameObject lookupRoot,
                CharacterLookupScreenController lookupController,
                CharacterLookupScreenView lookupView)
            {
                SelectRoot = selectRoot;
                UpgradeRoot = upgradeRoot;
                RosterController = rosterController;
                RosterView = rosterView;
                UpgradeController = upgradeController;
                UpgradeView = upgradeView;
                SaveService = saveService;
                Repository = repository;
                FullArt = fullArt;
                WaterIcon = waterIcon;
                LocalizationService = localizationService;
                LookupRoot = lookupRoot;
                LookupController = lookupController;
                LookupView = lookupView;
            }

            public GameObject SelectRoot { get; }
            public GameObject UpgradeRoot { get; }
            public CharacterRosterScreenController RosterController { get; }
            public CharacterRosterScreenView RosterView { get; }
            public CharacterUpgradeScreenController UpgradeController { get; }
            public CharacterUpgradeScreenView UpgradeView { get; }
            public SaveService SaveService { get; }
            public FakeSaveRepository Repository { get; }
            public Sprite FullArt { get; }
            public Sprite WaterIcon { get; }
            public LocalizationService LocalizationService { get; }
            public GameObject LookupRoot { get; }
            public CharacterLookupScreenController LookupController { get; }
            public CharacterLookupScreenView LookupView { get; }
        }
    }
}
