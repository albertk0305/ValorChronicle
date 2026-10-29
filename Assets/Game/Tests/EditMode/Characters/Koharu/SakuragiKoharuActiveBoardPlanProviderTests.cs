using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuActiveBoardPlanProviderTests
    {
        private static readonly int[] GrassDeadBoardElements =
        {
            3, 0, 1, 2, 0,
            0, 1, 3, 2, 1,
            4, 4, 0, 1, 4,
            2, 1, 0, 0, 4,
            0, 2, 1, 2, 1,
            0, 4, 4, 0, 3
        };

        private static readonly int[] GrassFallbackBoardElements =
        {
            2, 4, 4, 0, 1, 2,
            1, 0, 1, 2, 3, 4,
            0, 2, 4, 3, 0, 0,
            3, 1, 3, 2, 0, 0,
            4, 1, 1, 4, 2, 1
        };

        [Test]
        public void SuccessfulUseRequestsTwoGrassConversionsFromConfig()
        {
            using (var battle = TestBattle.Success(awakening: 0))
            {
                Assert.That(battle.Provider.CooldownTurns, Is.EqualTo(4));

                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                Assert.That(battle.Sink.RequestCount, Is.EqualTo(1));
                Assert.That(battle.Sink.Target, Is.EqualTo(ElementType.Grass));
                Assert.That(battle.Sink.RequestedCount, Is.EqualTo(2));
                Assert.That(battle.Sink.LastResult.Succeeded, Is.True);
                Assert.That(battle.Sink.LastResult.ConvertedCount,
                    Is.EqualTo(2));
                Assert.That(battle.Sink.LastResult.Entries
                        .Select(entry => entry.Position)
                        .Distinct()
                        .Count(),
                    Is.EqualTo(2));
            }
        }

        [Test]
        public void ConversionExcludesGrassRockSpecialAndLockedBlocks()
        {
            BoardState board = CreateBoard(GrassDeadBoardElements);
            BoardBlock rock = ReplaceBlock(
                board,
                5,
                2,
                BoardBlockType.Rock,
                null);
            BoardBlock special = ReplaceBlock(
                board,
                5,
                3,
                BoardBlockType.Special,
                ElementType.Fire);
            BoardBlock locked = ReplaceBlock(
                board,
                5,
                4,
                BoardBlockType.Locked,
                ElementType.Light);

            using (var battle = new TestBattle(0, board))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                foreach (BoardElementConversionEntry entry
                    in battle.Sink.LastResult.Entries)
                {
                    Assert.That(entry.BlockType,
                        Is.EqualTo(BoardBlockType.Normal));
                    Assert.That(entry.PreviousElement,
                        Is.Not.EqualTo(ElementType.Grass));
                    Assert.That(entry.NewElement,
                        Is.EqualTo(ElementType.Grass));
                }

                Assert.That(battle.Sink.Board.Get(
                    new BoardPosition(5, 2)), Is.SameAs(rock));
                Assert.That(battle.Sink.Board.Get(
                    new BoardPosition(5, 3)), Is.SameAs(special));
                Assert.That(battle.Sink.Board.Get(
                    new BoardPosition(5, 4)), Is.SameAs(locked));
            }
        }

        [Test]
        public void OneCandidateIsTriedOnceThenReportsNoPlayableCombination()
        {
            BoardState board = CreateUniformBoard(ElementType.Grass);
            SetElement(board, 0, 0, ElementType.Fire);
            var resolver = new BoardElementConversionResolver(
                new SequenceRandomSource(0),
                new BoardMoveAnalyzer(),
                maxRandomSelectionAttempts: 1);

            BoardElementConversionResult result = resolver.Resolve(
                board,
                ElementType.Grass,
                requestedCount: 2);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure,
                Is.EqualTo(
                    BoardElementConversionFailure.NoPlayableCombination));
            Assert.That(result.RandomAttemptCount, Is.EqualTo(1));
            Assert.That(result.DeterministicCombinationCount, Is.EqualTo(1));
        }

        [Test]
        public void NoCandidatesPreservesBoardRuntimeSpiritAndBuffState()
        {
            BoardState board = CreateUniformBoard(ElementType.Grass);
            BoardBlock[] before = CaptureBoard(board);
            using (var battle = new TestBattle(
                awakening: 6,
                board: board,
                initialSpiritEnergy: 25))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.False);

                Assert.That(battle.Sink.LastResult.Failure,
                    Is.EqualTo(BoardElementConversionFailure.NoCandidates));
                AssertBoardContainsReferences(battle.Sink.Board, before);
                Assert.That(battle.ActiveState.UsedThisTurn, Is.False);
                Assert.That(battle.ActiveState.RemainingCooldown, Is.Zero);
                Assert.That(battle.State.SpiritEnergy, Is.EqualTo(25));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
                Assert.That(battle.ActionIds.LastIssuedId, Is.Zero);
                Assert.That(battle.Executor.CanExecute(0), Is.True);
            }
        }

        [Test]
        public void NoPlayableCombinationPreservesBoardAndCanRetry()
        {
            BoardState board = CreateUniformBoard(ElementType.Grass);
            SetElement(board, 0, 0, ElementType.Fire);
            BoardBlock[] before = CaptureBoard(board);
            using (var battle = new TestBattle(
                awakening: 0,
                board: board,
                initialSpiritEnergy: 6,
                maxRandomSelectionAttempts: 1))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.False);

                Assert.That(battle.Sink.LastResult.Failure,
                    Is.EqualTo(
                        BoardElementConversionFailure.NoPlayableCombination));
                AssertBoardContainsReferences(battle.Sink.Board, before);
                Assert.That(battle.ActiveState.UsedThisTurn, Is.False);
                Assert.That(battle.ActiveState.RemainingCooldown, Is.Zero);
                Assert.That(battle.State.SpiritEnergy, Is.EqualTo(6));
                Assert.That(battle.Executor.CanExecute(0), Is.True);

                battle.Sink.SetBoard(CreateBoard(GrassDeadBoardElements));
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
            }
        }

        [Test]
        public void ResolverRetriesAndFallsBackToFiniteCombinationSearch()
        {
            BoardState board = CreateBoard(GrassFallbackBoardElements);
            var resolver = new BoardElementConversionResolver(
                new SequenceRandomSource(0, 1),
                new BoardMoveAnalyzer(),
                maxRandomSelectionAttempts: 1);

            BoardElementConversionResult result = resolver.Resolve(
                board,
                ElementType.Grass,
                requestedCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RandomAttemptCount, Is.EqualTo(1));
            Assert.That(result.UsedDeterministicSearch, Is.True);
            Assert.That(result.DeterministicCombinationCount,
                Is.EqualTo(2));
        }

        [Test]
        public void SuccessfulConversionDoesNotResolveMatchesImmediately()
        {
            using (var battle = TestBattle.Success(awakening: 0))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                Assert.That(battle.Sink.Board, Is.Not.Null);
                Assert.That(battle.Sink.LastResult.Entries, Is.Not.Empty);
                foreach (BoardElementConversionEntry entry
                    in battle.Sink.LastResult.Entries)
                {
                    BoardBlock converted = battle.Sink.Board.Get(
                        entry.Position);
                    Assert.That(converted.RuntimeId,
                        Is.EqualTo(entry.RuntimeId));
                    Assert.That(converted.Element,
                        Is.EqualTo(ElementType.Grass));
                }
            }
        }

        [Test]
        public void InvalidSwapRestoresOnlySwapAndKeepsActiveConversion()
        {
            using (var battle = TestBattle.Success(awakening: 0))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
                BoardState convertedBoard = battle.Sink.Board;
                BoardBlock[] convertedContents = CaptureBoard(convertedBoard);
                BoardSwap invalidSwap = FindInvalidSwappablePair(
                    convertedBoard);
                var random = new SeededRandomSource(20261029);
                var moveAnalyzer = new BoardMoveAnalyzer();
                var refiller = new BoardRefiller(
                    random,
                    new BoardBlockIdGenerator(1000));
                var swapResolver = new BoardSwapActionResolver(
                    moveAnalyzer,
                    new BoardCascadeResolver(
                        new BoardCascadeStepResolver(refiller)),
                    new BoardShuffler(random, moveAnalyzer));

                BoardSwapActionResult result = swapResolver.Resolve(
                    convertedBoard,
                    invalidSwap);

                Assert.That(result.Status,
                    Is.EqualTo(BoardSwapActionStatus.NoMatch));
                Assert.That(result.ConsumesTurn, Is.False);
                Assert.That(result.RequiresSwapBack, Is.True);
                AssertBoardContainsReferences(result.Board, convertedContents);
                Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
            }
        }

        [TestCase(0, 0L, 4, 0L)]
        [TestCase(0, 5L, 4, 5L)]
        [TestCase(0, 6L, 3, 0L)]
        [TestCase(0, 12L, 2, 0L)]
        [TestCase(0, 30L, 0, 6L)]
        [TestCase(5, 5L, 3, 0L)]
        [TestCase(5, 12L, 2, 2L)]
        [TestCase(6, 25L, 0, 5L)]
        public void SuccessCommitsCooldownThenConsumesAvailableSpirit(
            int awakening,
            long initialEnergy,
            int expectedCooldown,
            long expectedEnergy)
        {
            using (var battle = new TestBattle(
                awakening,
                CreateBoard(GrassDeadBoardElements),
                initialEnergy))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                Assert.That(battle.ActiveState.RemainingCooldown,
                    Is.EqualTo(expectedCooldown));
                Assert.That(battle.ActiveState.UsedThisTurn, Is.True);
                Assert.That(battle.State.SpiritEnergy,
                    Is.EqualTo(expectedEnergy));
                Assert.That(battle.ActiveState.CanUse, Is.False);
            }
        }

        [Test]
        public void CooldownZeroStillBlocksSameTurnAndAllowsNextTurn()
        {
            using (var battle = new TestBattle(
                awakening: 0,
                board: CreateBoard(GrassDeadBoardElements),
                initialSpiritEnergy: 30))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
                Assert.That(battle.ActiveState.RemainingCooldown, Is.Zero);
                Assert.That(battle.Executor.TryExecute(0, out _), Is.False);

                BeginRuntimeTurn(battle.ActiveState);

                Assert.That(battle.Executor.CanExecute(0), Is.True);
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
            }
        }

        [Test]
        public void UnboundHandleFailsBeforeRuntimeAndBoardMutation()
        {
            BoardState board = CreateBoard(GrassDeadBoardElements);
            BoardBlock[] before = CaptureBoard(board);
            using (var battle = new TestBattle(
                awakening: 0,
                board: board,
                bindHandle: false))
            {
                Assert.Throws<InvalidOperationException>(() =>
                    battle.Executor.TryExecute(0, out _));

                Assert.That(battle.Sink.RequestCount, Is.Zero);
                AssertBoardContainsReferences(battle.Sink.Board, before);
                Assert.That(battle.ActiveState.UsedThisTurn, Is.False);
                Assert.That(battle.ActiveState.RemainingCooldown, Is.Zero);
                Assert.That(battle.State.SpiritEnergy, Is.Zero);
            }
        }

        [Test]
        public void ConstructorRejectsWrongCharacterOrPartySlotHandle()
        {
            SakuragiKoharuCombatConfig config =
                SakuragiKoharuTestConfig.Create();
            try
            {
                var owner = new CharacterBattleState(
                    SakuragiKoharuRules.CharacterId,
                    2,
                    ElementType.Grass,
                    1100L,
                    1000d);
                var state = new SakuragiKoharuBattleState(owner);

                Assert.Throws<ArgumentException>(() =>
                    new SakuragiKoharuActiveBoardPlanProvider(
                        config,
                        0,
                        state,
                        new ActiveAbilityCooldownReductionHandle(
                            "character_other",
                            owner.PartySlotIndex)));
                Assert.Throws<ArgumentException>(() =>
                    new SakuragiKoharuActiveBoardPlanProvider(
                        config,
                        0,
                        state,
                        new ActiveAbilityCooldownReductionHandle(
                            owner.CharacterId,
                            owner.PartySlotIndex + 1)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void MatchTriggerAndActiveCommitShareOneSpiritState()
        {
            using (var battle = TestBattle.Success(awakening: 0))
            {
                var spiritRule = new SakuragiKoharuSpiritEnergyRule(
                    battle.State,
                    battle.Config,
                    0,
                    battle.Handle);
                var matchProvider = new SakuragiKoharuMatchActionProvider(
                    battle.Config,
                    0);
                var matchExecutor = new CombatActionExecutor(
                    battle.Boss,
                    battle.Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(
                        new ICombatTriggerRule[] { spiritRule }));
                for (int index = 0; index < 2; index++)
                {
                    IReadOnlyList<CombatAction> actions =
                        matchProvider.CreateActions(
                            battle.Character,
                            battle.Party,
                            battle.Boss,
                            BoardMatchTier.FiveOrMore,
                            1,
                            battle.ActionIds);
                    matchExecutor.Execute(new CombatActionQueue(actions));
                }

                Assert.That(battle.State.SpiritEnergy, Is.EqualTo(6));
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
                Assert.That(battle.ActiveState.RemainingCooldown,
                    Is.EqualTo(3));
                Assert.That(battle.State.SpiritEnergy, Is.Zero);
            }
        }

        [TestCase(0)]
        [TestCase(5)]
        public void AwakeningBelowSixCreatesNoSuccessAction(int awakening)
        {
            using (var battle = TestBattle.Success(awakening))
            {
                Assert.That(
                    battle.Executor.TryExecute(0, out var result),
                    Is.True);

                Assert.That(result.ActionResults, Is.Empty);
                Assert.That(battle.Party.Effects.Count, Is.Zero);
                Assert.That(battle.ActionIds.LastIssuedId, Is.Zero);
            }
        }

        [Test]
        public void AwakeningSixAppliesActiveOriginPartyBuffAndRecordsResult()
        {
            using (var battle = TestBattle.Success(awakening: 6))
            {
                Assert.That(
                    battle.Executor.TryExecute(0, out var result),
                    Is.True);

                Assert.That(result.ActionResults, Has.Count.EqualTo(1));
                var effectResult = result.ActionResults[0]
                    as ApplyEffectActionResult;
                Assert.That(effectResult, Is.Not.Null);
                Assert.That(effectResult.Action.ActionId, Is.EqualTo(1));
                Assert.That(effectResult.Action.RootActionId, Is.EqualTo(1));
                Assert.That(effectResult.Action.SourceActionId, Is.Null);
                Assert.That(effectResult.Action.Origin,
                    Is.EqualTo(ActionOrigin.Active));
                EffectInstance effect = effectResult.AppliedEffect;
                Assert.That(effect.EffectId,
                    Is.EqualTo(
                        SakuragiKoharuRules.Awakening6ActiveEffectId));
                Assert.That(effect.Magnitude, Is.EqualTo(0.10d));
                Assert.That(effect.RemainingTurns, Is.EqualTo(1));
                Assert.That(effect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(effect.AttackTypeMask,
                    Is.EqualTo(
                        AttackTypeMask.Additional | AttackTypeMask.Chase));
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void AwakeningSixBuffAppliesOnlyToAdditionalAndChase()
        {
            using (var battle = TestBattle.Success(awakening: 6))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
                AttackType[] attackTypes =
                {
                    AttackType.Additional,
                    AttackType.Chase,
                    AttackType.Match,
                    AttackType.Active,
                    AttackType.DamageOverTime
                };
                var actions = attackTypes
                    .Select(type => battle.CreateSystemDamage(type))
                    .Cast<CombatAction>()
                    .ToArray();

                CombatActionExecutionResult result = battle.CombatExecutor
                    .Execute(new CombatActionQueue(actions));
                double[] rates = result.ActionResults
                    .Cast<DamageActionResult>()
                    .Select(item => item.Context
                        .AttackTypeDamageIncreaseRateSum)
                    .ToArray();

                Assert.That(rates,
                    Is.EqualTo(new[] { 0.10d, 0.10d, 0d, 0d, 0d }));
            }
        }

        [Test]
        public void Match5AndAwakeningSixBuffsAddToTwentyFivePercent()
        {
            using (var battle = TestBattle.Success(awakening: 6))
            {
                var matchProvider = new SakuragiKoharuMatchActionProvider(
                    battle.Config,
                    awakening: 4);
                battle.CombatExecutor.Execute(new CombatActionQueue(
                    matchProvider.CreateActions(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.FiveOrMore,
                        1,
                        battle.ActionIds)));
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                DamageActionResult additional = (DamageActionResult)
                    battle.CombatExecutor.Execute(new CombatActionQueue(
                        new CombatAction[]
                        {
                            battle.CreateSystemDamage(
                                AttackType.Additional)
                        })).ActionResults.Single();

                Assert.That(additional.Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.25d));
            }
        }

        [Test]
        public void AwakeningSixBuffExpiresAtCurrentTurnEnd()
        {
            using (var battle = TestBattle.Success(awakening: 6))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
                new BattleTurnEndProcessor(
                    battle.Party,
                    battle.Boss).ProcessTurnEnd();

                Assert.That(battle.Party.Effects.FindByEffectId(
                    SakuragiKoharuRules.Awakening6ActiveEffectId), Is.Empty);
            }
        }

        [Test]
        public void AwakeningSixEffectRefreshesWithoutStacking()
        {
            using (var battle = new TestBattle(
                awakening: 6,
                board: CreateBoard(GrassDeadBoardElements),
                initialSpiritEnergy: 50))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
                BeginRuntimeTurn(battle.ActiveState);
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);

                IReadOnlyList<EffectInstance> effects =
                    battle.Party.Effects.FindByEffectId(
                        SakuragiKoharuRules.Awakening6ActiveEffectId);
                Assert.That(effects, Has.Count.EqualTo(1));
                Assert.That(effects[0].Magnitude, Is.EqualTo(0.10d));
                Assert.That(effects[0].RemainingTurns, Is.EqualTo(1));
            }
        }

        [Test]
        public void BaseCooldownAllowsSevenUsesAcrossTwentyFiveTurns()
        {
            var coordinator = new BattleFlowCoordinator(
                25,
                new[] { 4 });
            coordinator.StartBattle();
            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];
            int useCount = 0;
            for (int turn = 1; turn <= 25; turn++)
            {
                if (TryUseForTest(state))
                {
                    useCount++;
                }

                if (turn < 25)
                {
                    BeginRuntimeTurn(state);
                }
            }

            Assert.That(useCount, Is.EqualTo(7));
        }

        private static void BeginRuntimeTurn(
            ActiveAbilityRuntimeState state)
        {
            MethodInfo method = typeof(ActiveAbilityRuntimeState).GetMethod(
                "BeginTurn",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(state, Array.Empty<object>());
        }

        private static bool TryUseForTest(
            ActiveAbilityRuntimeState state)
        {
            MethodInfo method = typeof(ActiveAbilityRuntimeState).GetMethod(
                "TryUse",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(state, Array.Empty<object>());
        }

        private static BoardSwap FindInvalidSwappablePair(BoardState board)
        {
            var analyzer = new BoardMoveAnalyzer();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var first = new BoardPosition(x, y);
                    if (x + 1 < BoardConstants.Width)
                    {
                        var second = new BoardPosition(x + 1, y);
                        if (analyzer.CanSwap(board, first, second)
                            && !analyzer.IsValidSwap(board, first, second))
                        {
                            return new BoardSwap(first, second);
                        }
                    }

                    if (y + 1 < BoardConstants.Height)
                    {
                        var second = new BoardPosition(x, y + 1);
                        if (analyzer.CanSwap(board, first, second)
                            && !analyzer.IsValidSwap(board, first, second))
                        {
                            return new BoardSwap(first, second);
                        }
                    }
                }
            }

            throw new AssertionException(
                "Expected at least one swappable invalid pair.");
        }

        private static BoardState CreateBoard(int[] elements)
        {
            var board = new BoardState();
            for (int index = 0;
                index < BoardConstants.CellCount;
                index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                board.Set(position, new BoardBlock(
                    index + 1,
                    BoardBlockType.Normal,
                    (ElementType)elements[index]));
            }

            return board;
        }

        private static BoardState CreateUniformBoard(ElementType element)
        {
            var elements = new int[BoardConstants.CellCount];
            for (int index = 0; index < elements.Length; index++)
            {
                elements[index] = (int)element;
            }

            return CreateBoard(elements);
        }

        private static BoardBlock ReplaceBlock(
            BoardState board,
            int x,
            int y,
            BoardBlockType blockType,
            ElementType? element)
        {
            var position = new BoardPosition(x, y);
            var block = new BoardBlock(
                board.Get(position).RuntimeId,
                blockType,
                element);
            board.Set(position, block);
            return block;
        }

        private static void SetElement(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            ReplaceBlock(
                board,
                x,
                y,
                BoardBlockType.Normal,
                element);
        }

        private static BoardBlock[] CaptureBoard(BoardState board)
        {
            var result = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return result;
        }

        private static void AssertBoardContainsReferences(
            BoardState board,
            IReadOnlyList<BoardBlock> expected)
        {
            for (int index = 0; index < expected.Count; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(expected[index]));
            }
        }

        private sealed class TestBattle : IDisposable
        {
            public static TestBattle Success(int awakening)
            {
                return new TestBattle(
                    awakening,
                    CreateBoard(GrassDeadBoardElements));
            }

            public TestBattle(
                int awakening,
                BoardState board,
                long initialSpiritEnergy = 0,
                bool bindHandle = true,
                int maxRandomSelectionAttempts =
                    BoardElementConversionResolver
                        .DefaultMaxRandomSelectionAttempts)
            {
                Config = SakuragiKoharuTestConfig.Create();
                Character = new CharacterBattleState(
                    SakuragiKoharuRules.CharacterId,
                    2,
                    ElementType.Grass,
                    1100L,
                    1000d);
                State = new SakuragiKoharuBattleState(Character);
                if (initialSpiritEnergy > 0)
                {
                    State.GainSpiritEnergy(initialSpiritEnergy);
                }

                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "koharu_active_test_boss",
                    ElementType.Water,
                    1000000L,
                    100d);
                Coordinator = new BattleFlowCoordinator(
                    25,
                    new[] { Config.ActiveCooldownTurns });
                Handle = new ActiveAbilityCooldownReductionHandle(
                    Character.CharacterId,
                    Character.PartySlotIndex);
                if (bindHandle)
                {
                    Bind(
                        Handle,
                        Coordinator.Context.ActiveAbilities[0]);
                }

                Provider = new SakuragiKoharuActiveBoardPlanProvider(
                    Config,
                    awakening,
                    State,
                    Handle);
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.RegisterBoard(
                    SakuragiKoharuRules.CharacterId,
                    SakuragiKoharuRules.ActiveAbilityId,
                    Provider);
                Sink = new ResolverBoardSink(
                    board,
                    maxRandomSelectionAttempts);
                ActionIds = new CombatActionIdSequence();
                CombatExecutor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));
                Executor = new ActiveAbilityCombatExecutor(
                    Coordinator,
                    Party,
                    Boss,
                    CombatExecutor,
                    ActionIds,
                    new ActiveAbilityBinding[]
                    {
                        new ActiveAbilityBinding(
                            0,
                            Character.PartySlotIndex,
                            Character.CharacterId,
                            SakuragiKoharuRules.ActiveAbilityId)
                    },
                    registry,
                    Sink);
                Coordinator.StartBattle();
            }

            public SakuragiKoharuCombatConfig Config { get; }
            public CharacterBattleState Character { get; }
            public SakuragiKoharuBattleState State { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public BattleFlowCoordinator Coordinator { get; }
            public ActiveAbilityCooldownReductionHandle Handle { get; }
            public SakuragiKoharuActiveBoardPlanProvider Provider { get; }
            public ResolverBoardSink Sink { get; }
            public CombatActionIdSequence ActionIds { get; }
            public CombatActionExecutor CombatExecutor { get; }
            public ActiveAbilityCombatExecutor Executor { get; }
            public ActiveAbilityRuntimeState ActiveState =>
                Coordinator.Context.ActiveAbilities[0];

            public DamageAction CreateSystemDamage(AttackType attackType)
            {
                return new DamageAction(
                    ActionIds.Next(),
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Grass,
                        attackType,
                        AttackTag.None,
                        0.10d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Config);
            }

            private static void Bind(
                ActiveAbilityCooldownReductionHandle handle,
                ActiveAbilityRuntimeState state)
            {
                MethodInfo method = typeof(
                        ActiveAbilityCooldownReductionHandle)
                    .GetMethod(
                        "Bind",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                method.Invoke(handle, new object[] { state });
            }
        }

        private sealed class ResolverBoardSink
            : IBattleBoardElementConversionSink
        {
            private readonly int maxRandomSelectionAttempts;
            private int seed;

            public ResolverBoardSink(
                BoardState board,
                int maxRandomSelectionAttempts)
            {
                Board = board ?? throw new ArgumentNullException(nameof(board));
                this.maxRandomSelectionAttempts =
                    maxRandomSelectionAttempts;
                seed = 20261029;
            }

            public bool CanAcceptElementConversion => true;
            public BoardState Board { get; private set; }
            public int RequestCount { get; private set; }
            public ElementType Target { get; private set; }
            public int RequestedCount { get; private set; }
            public BoardElementConversionResult LastResult { get; private set; }

            public void SetBoard(BoardState board)
            {
                Board = board ?? throw new ArgumentNullException(nameof(board));
            }

            public bool TryConvertElements(
                ElementType targetElement,
                int requestedCount,
                out BoardElementConversionResult result)
            {
                RequestCount++;
                Target = targetElement;
                RequestedCount = requestedCount;
                result = new BoardElementConversionResolver(
                    new SeededRandomSource(seed++),
                    new BoardMoveAnalyzer(),
                    maxRandomSelectionAttempts).Resolve(
                        Board,
                        targetElement,
                        requestedCount);
                LastResult = result;
                if (result.Succeeded)
                {
                    Board = result.Board;
                }

                return result.Succeeded;
            }
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public SequenceRandomSource(params int[] values)
            {
                this.values = new Queue<int>(values);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                int value = values.Dequeue();
                Assert.That(value,
                    Is.InRange(minInclusive, maxExclusive - 1));
                return value;
            }

            public float NextFloat()
            {
                return 0f;
            }
        }
    }
}
