using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiBattleIntegrationTests
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
                "miyuki_integration",
                utcUnixSeconds: 1);
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = ShimotsukiMiyukiRules.CharacterId,
                Level = 42,
                Awakening = 3
            });
            profile.Party.Presets[0].CharacterSlotIds[3] =
                ShimotsukiMiyukiRules.CharacterId;

            BattlePartyResolutionResult resolution =
                new BattlePartyResolver(database).Resolve(profile);

            Assert.That(resolution.IsSuccess, Is.True,
                resolution.ErrorMessage);
            BattlePartyMemberInput member = resolution.Members.Single();
            Assert.That(member.CharacterId,
                Is.EqualTo(ShimotsukiMiyukiRules.CharacterId));
            Assert.That(member.PartySlotIndex, Is.EqualTo(3));
            Assert.That(member.Level, Is.EqualTo(42));
            Assert.That(member.Awakening, Is.EqualTo(3));
            Assert.That(member.CharacterDefinition.CombatConfig,
                Is.TypeOf<ShimotsukiMiyukiCombatConfig>());

            BattleSceneCombatComposition composition = CreateComposition(
                resolution.Members);
            Assert.That(composition.Party.Characters.Single().PartySlotIndex,
                Is.EqualTo(3));
            Assert.That(composition.MatchProviders.TryResolve(
                ShimotsukiMiyukiRules.CharacterId,
                out _), Is.True);
            Assert.That(composition.ActiveProviders.TryResolveBoard(
                ShimotsukiMiyukiRules.CharacterId,
                ShimotsukiMiyukiRules.ActiveAbilityId,
                out _), Is.True);

            EffectInstance passive = composition.Party.Effects
                .GetActiveEffects().Single(effect => effect.EffectId
                    == ShimotsukiMiyukiRules.PassiveEffectId);
            Assert.That(passive.Magnitude, Is.EqualTo(0.10d));
            Assert.That(passive.RemainingTurns, Is.Null);
            Assert.That(passive.ElementFilter,
                Is.EqualTo(ElementType.Water));
            Assert.That(composition.Boss.Resources.Get(
                WaterElementResource.Id).MaxAmount,
                Is.EqualTo(5));
        }

        [Test]
        public void BoardActiveAndNextMatchShareFrostEchoBattleState()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 1));
            var sink = new ResolverBoardSink(CreatePlayableBoard());
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            var bridge = composition.CreateBridge(coordinator, sink);
            Assert.That(coordinator.StartBattle(), Is.True);
            var state = composition.CombatStatusSources.Single()
                as ShimotsukiMiyukiBattleState;

            Assert.That(bridge.TryUseActive(0), Is.True);

            Assert.That(sink.RequestedCount, Is.EqualTo(5));
            Assert.That(state, Is.Not.Null);
            Assert.That(state.FrostEchoCount, Is.EqualTo(1));
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(8));

            MatchEventExecution execution = BeginMatch(
                coordinator,
                ElementType.Water,
                blockCount: 3);
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(composition.Boss.Resources.Get(
                WaterElementResource.Id).CurrentAmount,
                Is.EqualTo(2));
            Assert.That(state.FrostEchoCount, Is.Zero);
            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                    .Select(result => result.Action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DamageAction),
                    typeof(AddResourceAction)
                }));
        }

        [Test]
        public void FailedBoardActiveLeavesCooldownAndEchoUnchanged()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 0));
            var sink = new ResolverBoardSink(
                CreateUniformBoard(ElementType.Water));
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            var bridge = composition.CreateBridge(coordinator, sink);
            Assert.That(coordinator.StartBattle(), Is.True);
            var state = (ShimotsukiMiyukiBattleState)composition
                .CombatStatusSources.Single();

            Assert.That(bridge.TryUseActive(0), Is.False);

            Assert.That(state.FrostEchoCount, Is.Zero);
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .UsedThisTurn, Is.False);
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.Zero);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        [Test]
        public void FiveMatchUsesProductionActionOrderAndRefreshPolicy()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 4, partySlotIndex: 0));
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            var bridge = composition.CreateBridge(coordinator);
            Assert.That(coordinator.StartBattle(), Is.True);

            MatchEventExecution execution = BeginMatch(
                coordinator,
                ElementType.Water,
                blockCount: 5);
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                    .Select(result => result.Action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DamageAction),
                    typeof(AddResourceAction),
                    typeof(ApplyEffectAction)
                }));
            EffectInstance effect = composition.Party.Effects
                .GetActiveEffects().Single(item => item.EffectId
                    == ShimotsukiMiyukiRules.Match5EffectId);
            Assert.That(effect.Magnitude, Is.EqualTo(0.12d));
            Assert.That(effect.RemainingTurns, Is.EqualTo(2));
            Assert.That(effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
        }

        private BattleSceneCombatComposition CreateComposition(
            params BattlePartyMemberInput[] members)
        {
            return CreateComposition(
                (IReadOnlyList<BattlePartyMemberInput>)members);
        }

        private BattleSceneCombatComposition CreateComposition(
            IReadOnlyList<BattlePartyMemberInput> members)
        {
            DefinitionDatabase database = Database();
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);
            BossDifficultyStats difficulty = boss.DifficultyStats[0];
            var composition = new BattleSceneCombatComposition(
                members,
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog(),
                boss,
                (KragmorCombatConfig)boss.CombatConfig,
                difficulty,
                new SeededRandomSource(1),
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
                ShimotsukiMiyukiRules.CharacterId,
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
            int blockCount)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(element, positions)
                    }),
                true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            return execution;
        }

        private static BoardState CreatePlayableBoard()
        {
            int[] elements =
            {
                3, 0, 2, 1, 0,
                0, 2, 3, 1, 2,
                4, 4, 0, 2, 4,
                1, 2, 0, 0, 4,
                0, 1, 2, 1, 2,
                0, 4, 4, 0, 3
            };
            var board = new BoardState();
            for (int index = 0; index < elements.Length; index++)
            {
                board.Set(
                    BoardPosition.FromIndex(index),
                    new BoardBlock(
                        index + 1,
                        BoardBlockType.Normal,
                        (ElementType)elements[index]));
            }

            return board;
        }

        private static BoardState CreateUniformBoard(ElementType element)
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                board.Set(
                    BoardPosition.FromIndex(index),
                    new BoardBlock(
                        index + 1,
                        BoardBlockType.Normal,
                        element));
            }

            return board;
        }

        private sealed class ResolverBoardSink
            : IBattleBoardElementConversionSink,
              IBattleBoardMutationSink
        {
            private BoardState board;

            public ResolverBoardSink(BoardState board)
            {
                this.board = board;
            }

            public bool CanAcceptElementConversion => true;
            public int RequestedCount { get; private set; }

            public bool TryConvertElements(
                ElementType targetElement,
                int requestedCount,
                out BoardElementConversionResult result)
            {
                RequestedCount = requestedCount;
                result = new BoardElementConversionResolver(
                    new SeededRandomSource(7),
                    new BoardMoveAnalyzer()).Resolve(
                        board,
                        targetElement,
                        requestedCount);
                if (!result.Succeeded)
                {
                    return false;
                }

                board = result.Board;
                return true;
            }

            public bool TryExecuteMutation(
                BattleBoardMutationCommand command,
                Action<BattleBoardMutationCompletion> completion)
            {
                return false;
            }
        }
    }
}
