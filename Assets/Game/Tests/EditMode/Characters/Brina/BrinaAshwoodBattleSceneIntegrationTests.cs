using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodBattleSceneIntegrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        private readonly List<BattleSceneCombatComposition> compositions =
            new List<BattleSceneCombatComposition>();

        [TearDown]
        public void TearDown()
        {
            for (int index = compositions.Count - 1; index >= 0; index--)
            {
                compositions[index].Dispose();
            }

            compositions.Clear();
        }

        [Test]
        public void OwnedPresetMemberFlowsIntoProductionComposition()
        {
            DefinitionDatabase database = Database();
            var profile = new NewProfileFactory().Create(
                "brina_integration",
                utcUnixSeconds: 1);
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = BrinaAshwoodRules.CharacterId,
                Level = 100,
                Awakening = 6
            });
            profile.Party.Presets[0].CharacterSlotIds[2] =
                BrinaAshwoodRules.CharacterId;

            BattlePartyResolutionResult resolution =
                new BattlePartyResolver(database).Resolve(profile);

            Assert.That(resolution.IsSuccess, Is.True,
                resolution.ErrorMessage);
            BattlePartyMemberInput member = resolution.Members.Single();
            Assert.That(member.CharacterId,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(member.PartySlotIndex, Is.EqualTo(2));
            Assert.That(member.Level, Is.EqualTo(100));
            Assert.That(member.Awakening, Is.EqualTo(6));
            Assert.That(member.CharacterDefinition.CombatConfig,
                Is.TypeOf<BrinaAshwoodCombatConfig>());

            BattleSceneCombatComposition composition = CreateComposition(
                resolution.Members);
            CharacterBattleState brina = composition.Party.Characters
                .Single();
            Assert.That(brina.PartySlotIndex, Is.EqualTo(2));
            Assert.That(brina.Element, Is.EqualTo(ElementType.Grass));
            Assert.That(brina.MaxHp, Is.EqualTo(3570));
            Assert.That(brina.Attack, Is.EqualTo(1103d));
            Assert.That(composition.MatchProviders.TryResolve(
                BrinaAshwoodRules.CharacterId,
                out _), Is.True);
            Assert.That(composition.ActiveProviders.TryResolve(
                BrinaAshwoodRules.CharacterId,
                BrinaAshwoodRules.ActiveAbilityId,
                out _), Is.True);
            Assert.That(composition.ActiveBindings.Single().PartySlotIndex,
                Is.EqualTo(2));
            BrinaAshwoodBattleState state = composition
                .CombatStatusSources.Single() as BrinaAshwoodBattleState;
            Assert.That(state, Is.Not.Null);
            Assert.That(state.PartySlotIndex, Is.EqualTo(2));
            Assert.That(state.WoodcutCount, Is.Zero);
            Assert.That(state.IsVisible, Is.True);
        }

        [Test]
        public void ProductionCompositionRunsActiveMatchAndAdditionalFlow()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 2));
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            BattleFlowCombatBridge bridge = composition.CreateBridge(
                coordinator);
            Assert.That(coordinator.StartBattle(), Is.True);
            var state = (BrinaAshwoodBattleState)composition
                .CombatStatusSources.Single();
            state.GainWoodcut(9);

            Assert.That(bridge.TryUseActive(0), Is.True);

            CharacterBattleState brina = composition.Party.Characters
                .Single();
            EffectInstance activeEffect = brina.Effects.FindByEffectId(
                BrinaAshwoodRules.ActiveEffectId).Single();
            Assert.That(activeEffect.Magnitude, Is.Zero);
            Assert.That(activeEffect.RemainingTurns, Is.EqualTo(1));
            Assert.That(activeEffect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(7));

            MatchEventExecution execution = BeginMatch(
                coordinator,
                ElementType.Grass,
                blockCount: 3,
                includeSecondCombo: true);
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            DamageActionResult[] damages = bridge.LastMatchExecutionResult
                .ActionResults.OfType<DamageActionResult>().ToArray();
            Assert.That(damages.Select(item => item.Action.Origin),
                Is.EqualTo(new[]
                {
                    ActionOrigin.Match,
                    ActionOrigin.Additional
                }));
            DamageActionResult match = damages[0];
            DamageActionResult additional = damages[1];
            Assert.That(match.Context.BaseAttack, Is.EqualTo(1103d));
            Assert.That(match.Context.SkillCoefficient,
                Is.EqualTo(1.35d));
            Assert.That(match.Context.AppliesCombo, Is.True);
            Assert.That(match.Context.FinalComboCount, Is.EqualTo(2));
            Assert.That(match.DamageResult.ComboMultiplier,
                Is.GreaterThan(1d));
            Assert.That(additional.Context.SkillCoefficient,
                Is.EqualTo(5.25d));
            Assert.That(additional.Context.AppliesCombo, Is.False);
            Assert.That(additional.Context.CanCritical, Is.True);
            Assert.That(
                ((DamageAction)additional.Action).ContextRequest.AttackType,
                Is.EqualTo(AttackType.Additional));
            Assert.That(((DamageAction)match.Action).ContextRequest.Attacker,
                Is.SameAs(brina));
            Assert.That(
                ((DamageAction)additional.Action).ContextRequest.Attacker,
                Is.SameAs(brina));
            Assert.That(state.WoodcutCount, Is.EqualTo(2));

            Assert.That(coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution secondCombo), Is.True);
            Assert.That(secondCombo.MatchEvent.Element,
                Is.EqualTo(ElementType.Fire));
            Assert.That(bridge.ResolveMatchEvent(secondCombo), Is.True);

            Assert.That(bridge.ResolveBossAction(
                coordinator.Context.CurrentTurn), Is.True);
            Assert.That(brina.Effects.FindByEffectId(
                BrinaAshwoodRules.ActiveEffectId), Is.Empty);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));

            MatchEventExecution nextTurn = BeginMatch(
                coordinator,
                ElementType.Grass,
                blockCount: 3);
            Assert.That(bridge.ResolveMatchEvent(nextTurn), Is.True);
            Assert.That(state.WoodcutCount, Is.EqualTo(4),
                "The next turn must use base +1 Woodcut plus Awakening 6, "
                    + "not the expired Active multiplier.");
        }

        [Test]
        public void SeparateCompositionsDoNotShareBrinaState()
        {
            BattleSceneCombatComposition first = CreateComposition(
                Member(level: 1, awakening: 0, partySlotIndex: 0));
            BattleSceneCombatComposition second = CreateComposition(
                Member(level: 1, awakening: 0, partySlotIndex: 3));
            var firstState = (BrinaAshwoodBattleState)first
                .CombatStatusSources.Single();
            var secondState = (BrinaAshwoodBattleState)second
                .CombatStatusSources.Single();

            firstState.GainWoodcut(7);

            Assert.That(firstState, Is.Not.SameAs(secondState));
            Assert.That(firstState.WoodcutCount, Is.EqualTo(7));
            Assert.That(secondState.WoodcutCount, Is.Zero);
            Assert.That(firstState.LastWoodcutGainTurn, Is.Null);
            Assert.That(secondState.LastWoodcutGainTurn, Is.Null);
        }

        [Test]
        public void LethalMatchStopsBeforeWoodcutTriggerInComposition()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                new[]
                {
                    Member(level: 100, awakening: 6, partySlotIndex: 0)
                },
                bossMaxHp: 1);
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            BattleFlowCombatBridge bridge = composition.CreateBridge(
                coordinator);
            Assert.That(coordinator.StartBattle(), Is.True);
            var state = (BrinaAshwoodBattleState)composition
                .CombatStatusSources.Single();

            MatchEventExecution execution = BeginMatch(
                coordinator,
                ElementType.Grass,
                blockCount: 3);
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(bridge.LastMatchExecutionResult.BossDefeated,
                Is.True);
            Assert.That(bridge.LastMatchExecutionResult.StoppedEarly,
                Is.True);
            Assert.That(state.WoodcutCount, Is.Zero);
            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                .OfType<DamageActionResult>().Count(), Is.EqualTo(1));
        }

        private BattleSceneCombatComposition CreateComposition(
            params BattlePartyMemberInput[] members)
        {
            return CreateComposition(
                (IReadOnlyList<BattlePartyMemberInput>)members,
                bossMaxHp: null);
        }

        private BattleSceneCombatComposition CreateComposition(
            IReadOnlyList<BattlePartyMemberInput> members,
            long? bossMaxHp = null)
        {
            DefinitionDatabase database = Database();
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);
            BossDifficultyStats canonical = boss.DifficultyStats[0];
            BossDifficultyStats difficulty = bossMaxHp.HasValue
                ? new BossDifficultyStats(
                    canonical.DifficultyId,
                    bossMaxHp.Value,
                    canonical.Attack)
                : canonical;
            var composition = new BattleSceneCombatComposition(
                members,
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog(),
                boss,
                (KragmorCombatConfig)boss.CombatConfig,
                difficulty,
                new SeededRandomSource(12345),
                database.BattleResultBalance.CreateBalance());
            compositions.Add(composition);
            return composition;
        }

        private static BattlePartyMemberInput Member(
            int level,
            int awakening,
            int partySlotIndex)
        {
            DefinitionDatabase database = Database();
            Assert.That(database.TryGetCharacter(
                BrinaAshwoodRules.CharacterId,
                out CharacterDefinition definition), Is.True);
            return new BattlePartyMemberInput(
                definition.Id,
                partySlotIndex,
                level,
                awakening,
                definition);
        }

        private static DefinitionDatabase Database()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private static MatchEventExecution BeginMatch(
            BattleFlowCoordinator coordinator,
            ElementType element,
            int blockCount,
            bool includeSecondCombo = false)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            var matches = new List<BattleFlowTestSupport.MatchFixture>
            {
                BattleFlowTestSupport.Match(element, positions)
            };
            if (includeSecondCombo)
            {
                matches.Add(BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 1),
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 1)));
            }

            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(
                    new[] { matches.ToArray() }),
                true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            return execution;
        }
    }
}
