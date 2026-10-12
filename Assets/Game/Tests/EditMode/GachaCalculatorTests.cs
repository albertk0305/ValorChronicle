using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaCalculatorTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void RewardSelector_UsesInclusiveLowerAndExclusiveUpperBounds()
        {
            GachaDefinition definition = CreateGacha(
                rewards: new[]
                {
                    Reward(GachaRewardKind.HeroTokens, 1, 100),
                    Reward(GachaRewardKind.RelicTokens, 2, 200),
                    Reward(GachaRewardKind.BattleRecords, 3, 700)
                });
            var random = new QueueRandomSource(
                0,
                99,
                100,
                299,
                300,
                999);

            long[] amounts = new long[6];
            for (int index = 0; index < amounts.Length; index++)
            {
                amounts[index] = GachaRewardSelector.Select(
                    definition,
                    random).Quantity;
            }

            Assert.That(amounts, Is.EqualTo(new long[]
            {
                1, 1, 2, 2, 3, 3
            }));
            Assert.That(
                random.Ranges.All(range =>
                    range.Item1 == 0
                    && range.Item2
                        == GachaDefinition.RequiredTotalWeight),
                Is.True);
        }

        [Test]
        public void RewardSelector_CanReachEveryConfiguredEntry()
        {
            GachaDefinition definition = CreateGacha(
                rewards: new[]
                {
                    Reward(GachaRewardKind.HeroTokens, 1, 1),
                    Reward(GachaRewardKind.HeroTokens, 2, 1),
                    Reward(GachaRewardKind.HeroTokens, 3, 998)
                });
            var random = new QueueRandomSource(0, 1, 2);

            Assert.That(
                GachaRewardSelector.Select(definition, random).Quantity,
                Is.EqualTo(1L));
            Assert.That(
                GachaRewardSelector.Select(definition, random).Quantity,
                Is.EqualTo(2L));
            Assert.That(
                GachaRewardSelector.Select(definition, random).Quantity,
                Is.EqualTo(3L));
        }

        [Test]
        public void RewardSelector_RejectsInvalidWeightsAndRandomSource()
        {
            GachaDefinition nonPositive = CreateGacha(
                "gacha_non_positive",
                rewards: new[]
                {
                    Reward(GachaRewardKind.HeroTokens, 1, 0),
                    Reward(GachaRewardKind.BattleRecords, 1, 1000)
                });
            GachaDefinition wrongTotal = CreateGacha(
                "gacha_wrong_total",
                rewards: new[]
                {
                    Reward(GachaRewardKind.BattleRecords, 1, 999)
                });

            Assert.Throws<ArgumentException>(() =>
                GachaRewardSelector.Select(
                    nonPositive,
                    new QueueRandomSource(0)));
            Assert.Throws<ArgumentException>(() =>
                GachaRewardSelector.Select(
                    wrongTotal,
                    new QueueRandomSource(0)));
            Assert.Throws<ArgumentNullException>(() =>
                GachaRewardSelector.Select(CreateGacha(), null));
        }

        [Test]
        public void CharacterPool_OneEntryAlwaysSelectsThatEntry()
        {
            GachaDefinition definition = CreateGacha(
                rewards: CharacterRewardTable(),
                characterPoolIds: new[] { "character_marea" });
            DefinitionDatabase database = CreateDatabase(
                new[] { definition },
                new[] { "character_marea" });
            var random = new QueueRandomSource(0, 0);

            GachaPullResult pull = new GachaCalculator(database, random)
                .GenerateSingle(definition.Id, State(definition.Id)).Pulls[0];

            Assert.That(pull.RewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(pull.RewardId, Is.EqualTo("character_marea"));
            Assert.That(random.Ranges[1], Is.EqualTo(Tuple.Create(0, 1)));
        }

        [Test]
        public void ContentPools_MapFirstAndLastIndicesInStableOrder()
        {
            GachaDefinition characterGacha = CreateGacha(
                "gacha_character_pool",
                rewards: CharacterRewardTable(),
                characterPoolIds: new[]
                {
                    "character_first",
                    "character_middle",
                    "character_last"
                },
                relicPoolIds: new[] { "relic_first" });
            GachaDefinition relicGacha = CreateGacha(
                "gacha_relic_pool",
                GachaKind.Relic,
                20,
                RelicRewardTable(),
                characterPoolIds: new[] { "character_first" },
                relicPoolIds: new[]
                {
                    "relic_first",
                    "relic_middle",
                    "relic_last"
                });
            DefinitionDatabase database = CreateDatabase(
                new[] { characterGacha, relicGacha },
                new[]
                {
                    "character_first",
                    "character_middle",
                    "character_last"
                },
                new[] { "relic_first", "relic_middle", "relic_last" });

            string firstCharacter = new GachaCalculator(
                database,
                new QueueRandomSource(0, 0))
                .GenerateSingle(
                    characterGacha.Id,
                    State(characterGacha.Id)).Pulls[0].RewardId;
            string lastCharacter = new GachaCalculator(
                database,
                new QueueRandomSource(0, 2))
                .GenerateSingle(
                    characterGacha.Id,
                    State(characterGacha.Id)).Pulls[0].RewardId;
            string firstRelic = new GachaCalculator(
                database,
                new QueueRandomSource(0, 0))
                .GenerateSingle(
                    relicGacha.Id,
                    State(relicGacha.Id)).Pulls[0].RewardId;
            string lastRelic = new GachaCalculator(
                database,
                new QueueRandomSource(0, 2))
                .GenerateSingle(
                    relicGacha.Id,
                    State(relicGacha.Id)).Pulls[0].RewardId;

            Assert.That(firstCharacter, Is.EqualTo("character_first"));
            Assert.That(lastCharacter, Is.EqualTo("character_last"));
            Assert.That(firstRelic, Is.EqualTo("relic_first"));
            Assert.That(lastRelic, Is.EqualTo("relic_last"));
        }

        [Test]
        public void CharacterPity_NonTargetIncrementsAndNaturalTargetResets()
        {
            GachaDefinition missDefinition = CreateGacha(
                "gacha_character_miss",
                rewards: NonContentRewardTable());
            GachaDefinition hitDefinition = CreateGacha(
                "gacha_character_hit",
                rewards: CharacterRewardTable());
            DefinitionDatabase database = CreateDatabase(
                new[] { missDefinition, hitDefinition });

            GachaCalculationResult miss = new GachaCalculator(
                database,
                new QueueRandomSource(0)).GenerateSingle(
                    missDefinition.Id,
                    State(missDefinition.Id));
            GachaCalculationResult hit = new GachaCalculator(
                database,
                new QueueRandomSource(0, 0)).GenerateSingle(
                    hitDefinition.Id,
                    State(hitDefinition.Id, 20, 20));

            Assert.That(miss.Pulls[0].PityCountAfter, Is.EqualTo(1));
            Assert.That(miss.ResultingState.TotalPullCount, Is.EqualTo(1L));
            Assert.That(hit.Pulls[0].PityCountAfter, Is.Zero);
            Assert.That(hit.Pulls[0].WasGuaranteedByPity, Is.False);
        }

        [Test]
        public void CharacterPity_FiftiethPullReplacesNormalReward()
        {
            GachaDefinition definition = CreateGacha(
                rewards: NonContentRewardTable());
            DefinitionDatabase database = CreateDatabase(
                new[] { definition });
            var random = new QueueRandomSource(0);

            GachaCalculationResult result = new GachaCalculator(
                database,
                random).GenerateSingle(
                    definition.Id,
                    State(definition.Id, 49, 49));
            GachaPullResult pull = result.Pulls[0];

            Assert.That(result.Pulls, Has.Count.EqualTo(1));
            Assert.That(pull.RewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(pull.Amount, Is.EqualTo(1L));
            Assert.That(pull.WasGuaranteedByPity, Is.True);
            Assert.That(pull.PityCountBefore, Is.EqualTo(49));
            Assert.That(pull.PityCountAfter, Is.Zero);
            Assert.That(random.Ranges, Has.Count.EqualTo(1));
            Assert.That(random.Ranges[0], Is.EqualTo(Tuple.Create(0, 1)));
        }

        [Test]
        public void CharacterPity_RelicDoesNotResetAndEarlyCharacterDoes()
        {
            GachaDefinition relicDefinition = CreateGacha(
                "gacha_character_relic",
                rewards: RelicRewardTable());
            GachaDefinition characterDefinition = CreateGacha(
                "gacha_character_early",
                rewards: CharacterRewardTable());
            DefinitionDatabase database = CreateDatabase(
                new[] { relicDefinition, characterDefinition });

            GachaPullResult relic = new GachaCalculator(
                database,
                new QueueRandomSource(0, 0)).GenerateSingle(
                    relicDefinition.Id,
                    State(relicDefinition.Id, 10, 10)).Pulls[0];
            GachaPullResult character = new GachaCalculator(
                database,
                new QueueRandomSource(0, 0)).GenerateSingle(
                    characterDefinition.Id,
                    State(characterDefinition.Id, 48, 48)).Pulls[0];

            Assert.That(relic.PityCountAfter, Is.EqualTo(11));
            Assert.That(character.PityCountAfter, Is.Zero);
            Assert.That(character.WasGuaranteedByPity, Is.False);
        }

        [Test]
        public void RelicPity_TwentiethPullReplacesNormalReward()
        {
            GachaDefinition definition = CreateGacha(
                "gacha_relic_pity",
                GachaKind.Relic,
                20,
                NonContentRewardTable());
            DefinitionDatabase database = CreateDatabase(
                new[] { definition });

            GachaPullResult pull = new GachaCalculator(
                database,
                new QueueRandomSource(0)).GenerateSingle(
                    definition.Id,
                    State(definition.Id, 19, 19)).Pulls[0];

            Assert.That(pull.RewardKind,
                Is.EqualTo(GachaRewardKind.RandomRelic));
            Assert.That(pull.RewardId, Is.EqualTo("relic_a"));
            Assert.That(pull.WasGuaranteedByPity, Is.True);
            Assert.That(pull.PityCountAfter, Is.Zero);
        }

        [Test]
        public void RelicPity_NaturalRelicResetsAndCharacterDoesNot()
        {
            GachaDefinition relicDefinition = CreateGacha(
                "gacha_relic_hit",
                GachaKind.Relic,
                20,
                RelicRewardTable());
            GachaDefinition characterDefinition = CreateGacha(
                "gacha_relic_character",
                GachaKind.Relic,
                20,
                CharacterRewardTable());
            DefinitionDatabase database = CreateDatabase(
                new[] { relicDefinition, characterDefinition });

            GachaPullResult relic = new GachaCalculator(
                database,
                new QueueRandomSource(0, 0)).GenerateSingle(
                    relicDefinition.Id,
                    State(relicDefinition.Id, 10, 10)).Pulls[0];
            GachaPullResult character = new GachaCalculator(
                database,
                new QueueRandomSource(0, 0)).GenerateSingle(
                    characterDefinition.Id,
                    State(characterDefinition.Id, 10, 10)).Pulls[0];

            Assert.That(relic.PityCountAfter, Is.Zero);
            Assert.That(relic.WasGuaranteedByPity, Is.False);
            Assert.That(character.PityCountAfter, Is.EqualTo(11));
        }

        [Test]
        public void TenPull_TriggersMidSequencePityThenContinuesNormally()
        {
            GachaDefinition definition = CreateGacha(
                rewards: NonContentRewardTable());
            DefinitionDatabase database = CreateDatabase(
                new[] { definition });
            var random = new QueueRandomSource(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0);

            GachaCalculationResult result = new GachaCalculator(
                database,
                random).GenerateTen(
                    definition.Id,
                    State(definition.Id, 48, 48));

            Assert.That(result.Pulls, Has.Count.EqualTo(10));
            Assert.That(
                result.Pulls.Select(pull => pull.Index),
                Is.EqualTo(Enumerable.Range(0, 10)));
            Assert.That(result.Pulls[0].PityCountAfter, Is.EqualTo(49));
            Assert.That(result.Pulls[1].WasGuaranteedByPity, Is.True);
            Assert.That(result.Pulls[1].RewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(result.Pulls[1].PityCountAfter, Is.Zero);
            Assert.That(result.Pulls[2].WasGuaranteedByPity, Is.False);
            Assert.That(result.Pulls[2].RewardKind,
                Is.EqualTo(GachaRewardKind.BattleRecords));
            Assert.That(result.ResultingState.PityCount, Is.EqualTo(8));
            Assert.That(result.ResultingState.TotalPullCount,
                Is.EqualTo(58L));
        }

        [Test]
        public void TenPull_MatchesTenSequentialSinglePullsWithSameSeed()
        {
            GachaDefinition definition = CreateGacha(
                rewards: new[]
                {
                    Reward(GachaRewardKind.RandomCharacter, 1, 500),
                    Reward(GachaRewardKind.BattleRecords, 1000, 500)
                },
                characterPoolIds: new[]
                {
                    "character_a",
                    "character_b"
                });
            DefinitionDatabase database = CreateDatabase(
                new[] { definition },
                new[] { "character_a", "character_b" });
            GachaCalculationResult batch = new GachaCalculator(
                database,
                new SeededRandomSource(9917)).GenerateTen(
                    definition.Id,
                    State(definition.Id));
            var singles = new List<GachaPullResult>();
            var singleCalculator = new GachaCalculator(
                database,
                new SeededRandomSource(9917));
            GachaStateSaveData state = State(definition.Id);
            for (int index = 0; index < 10; index++)
            {
                GachaCalculationResult single =
                    singleCalculator.GenerateSingle(definition.Id, state);
                singles.Add(single.Pulls[0]);
                state = single.ResultingState;
            }

            AssertPullValuesEqual(batch.Pulls, singles);
            Assert.That(batch.ResultingState.PityCount,
                Is.EqualTo(state.PityCount));
            Assert.That(batch.ResultingState.TotalPullCount,
                Is.EqualTo(state.TotalPullCount));
        }

        [Test]
        public void SeededRandomSource_ReproducesGachaSequence()
        {
            GachaDefinition definition = CreateGacha(
                rewards: new[]
                {
                    Reward(GachaRewardKind.HeroTokens, 1, 500),
                    Reward(GachaRewardKind.BattleRecords, 1000, 500)
                });
            DefinitionDatabase database = CreateDatabase(
                new[] { definition });

            GachaCalculationResult first = new GachaCalculator(
                database,
                new SeededRandomSource(1234)).GenerateTen(
                    definition.Id,
                    State(definition.Id));
            GachaCalculationResult second = new GachaCalculator(
                database,
                new SeededRandomSource(1234)).GenerateTen(
                    definition.Id,
                    State(definition.Id));

            AssertPullValuesEqual(first.Pulls, second.Pulls);
        }

        [Test]
        public void Calculation_DoesNotMutateInputStateOrDefinition()
        {
            GachaRewardEntry reward = Reward(
                GachaRewardKind.BattleRecords,
                1000,
                1000);
            GachaDefinition definition = CreateGacha(
                rewards: new[] { reward });
            DefinitionDatabase database = CreateDatabase(
                new[] { definition });
            GachaStateSaveData state = State(definition.Id, 3, 7);
            string[] characterPoolBefore =
                definition.CharacterPoolIds.ToArray();

            GachaCalculationResult result = new GachaCalculator(
                database,
                new QueueRandomSource(0)).GenerateSingle(
                    definition.Id,
                    state);
            GachaStateSaveData returnedState = result.ResultingState;
            returnedState.PityCount = 999;

            Assert.That(state.PityCount, Is.EqualTo(3));
            Assert.That(state.TotalPullCount, Is.EqualTo(7L));
            Assert.That(definition.RewardEntries[0], Is.SameAs(reward));
            Assert.That(definition.RewardEntries[0].Weight,
                Is.EqualTo(1000));
            Assert.That(definition.CharacterPoolIds,
                Is.EqualTo(characterPoolBefore));
            Assert.That(result.ResultingState.PityCount, Is.EqualTo(4));
        }

        [Test]
        public void DifferentBannerStatesRemainIndependent()
        {
            GachaDefinition character = CreateGacha(
                "gacha_character_independent");
            GachaDefinition relic = CreateGacha(
                "gacha_relic_independent",
                GachaKind.Relic,
                20);
            DefinitionDatabase database = CreateDatabase(
                new[] { character, relic });
            GachaStateSaveData characterState = State(character.Id, 4, 9);
            GachaStateSaveData relicState = State(relic.Id, 7, 12);

            GachaCalculationResult result = new GachaCalculator(
                database,
                new QueueRandomSource(0)).GenerateSingle(
                    character.Id,
                    characterState);

            Assert.That(result.ResultingState.GachaId,
                Is.EqualTo(character.Id));
            Assert.That(relicState.PityCount, Is.EqualTo(7));
            Assert.That(relicState.TotalPullCount, Is.EqualTo(12L));
        }

        [Test]
        public void Calculator_RejectsMissingDependenciesAndDefinitions()
        {
            DefinitionDatabase database = CreateDatabase(
                new[] { CreateGacha() });

            Assert.Throws<ArgumentNullException>(() =>
                new GachaCalculator(null, new QueueRandomSource(0)));
            Assert.Throws<ArgumentNullException>(() =>
                new GachaCalculator(database, null));
            Assert.Throws<KeyNotFoundException>(() =>
                new GachaCalculator(database, new QueueRandomSource(0))
                    .GenerateSingle(
                        "gacha_missing",
                        State("gacha_missing")));
            Assert.Throws<ArgumentNullException>(() =>
                new GachaCalculator(database, new QueueRandomSource(0))
                    .GenerateFromDefinition(null, State("gacha_test"), 1));
        }

        [Test]
        public void Calculator_RejectsInvalidStateAndPullCount()
        {
            GachaDefinition definition = CreateGacha();
            DefinitionDatabase database = CreateDatabase(
                new[] { definition });
            var calculator = new GachaCalculator(
                database,
                new QueueRandomSource(0));

            Assert.Throws<ArgumentNullException>(() =>
                calculator.GenerateSingle(definition.Id, null));
            Assert.Throws<ArgumentException>(() =>
                calculator.GenerateSingle(
                    definition.Id,
                    State("gacha_other")));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                calculator.GenerateSingle(
                    definition.Id,
                    State(definition.Id, -1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                calculator.GenerateSingle(
                    definition.Id,
                    State(definition.Id, 50, 50)));
            Assert.Throws<ArgumentException>(() =>
                calculator.GenerateSingle(
                    definition.Id,
                    State(definition.Id, 2, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                calculator.GenerateSingle(
                    definition.Id,
                    State(definition.Id, 0, -1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                calculator.Generate(definition.Id, State(definition.Id), 0));
        }

        [Test]
        public void Calculator_RejectsOverflowBeforeConsumingRandomness()
        {
            GachaDefinition definition = CreateGacha();
            DefinitionDatabase database = CreateDatabase(
                new[] { definition });
            var random = new QueueRandomSource(0);

            Assert.Throws<OverflowException>(() =>
                new GachaCalculator(database, random).GenerateTen(
                    definition.Id,
                    State(definition.Id, 0, long.MaxValue - 9L)));
            Assert.That(random.Ranges, Is.Empty);
        }

        [Test]
        public void Calculator_RejectsEmptyOrUnknownContentPools()
        {
            GachaDefinition empty = CreateGacha(
                "gacha_empty_pool",
                rewards: CharacterRewardTable(),
                characterPoolIds: Array.Empty<string>());
            GachaDefinition missing = CreateGacha(
                "gacha_missing_pool_id",
                rewards: RelicRewardTable(),
                relicPoolIds: new[] { "relic_missing" });
            DefinitionDatabase database = CreateDatabase(
                new[] { empty, missing });

            Assert.Throws<ArgumentException>(() =>
                new GachaCalculator(database, new QueueRandomSource(0))
                    .GenerateSingle(empty.Id, State(empty.Id)));
            Assert.Throws<ArgumentException>(() =>
                new GachaCalculator(database, new QueueRandomSource(0))
                    .GenerateSingle(missing.Id, State(missing.Id)));
        }

        private DefinitionDatabase CreateDatabase(
            GachaDefinition[] gachas,
            string[] characterIds = null,
            string[] relicIds = null)
        {
            characterIds ??= new[] { "character_a" };
            relicIds ??= new[] { "relic_a" };
            DefinitionDatabase database =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetValue(
                database,
                "characters",
                characterIds.Select(id =>
                    CreateDefinition<CharacterDefinition>(id)).ToArray());
            SetValue(
                database,
                "relics",
                relicIds.Select(id =>
                    CreateDefinition<RelicDefinition>(id)).ToArray());
            SetValue(database, "gachas", gachas);
            database.Initialize();
            return database;
        }

        private GachaDefinition CreateGacha(
            string id = "gacha_test",
            GachaKind kind = GachaKind.Character,
            int pity = 50,
            GachaRewardEntry[] rewards = null,
            string[] characterPoolIds = null,
            string[] relicPoolIds = null)
        {
            GachaDefinition definition =
                CreateDefinition<GachaDefinition>(id);
            SetValue(definition, "gachaKind", kind);
            SetValue(definition, "singlePullCost", 100L);
            SetValue(definition, "tenPullCost", 1000L);
            SetValue(definition, "pityPullCount", pity);
            SetValue(
                definition,
                "rewardEntries",
                rewards ?? NonContentRewardTable());
            SetValue(
                definition,
                "characterPoolIds",
                characterPoolIds ?? new[] { "character_a" });
            SetValue(
                definition,
                "relicPoolIds",
                relicPoolIds ?? new[] { "relic_a" });
            return definition;
        }

        private T CreateDefinition<T>(string id)
            where T : GameDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(definition);
            SetValue(definition, "id", id);
            return definition;
        }

        private static GachaRewardEntry[] CharacterRewardTable()
        {
            return new[]
            {
                Reward(GachaRewardKind.RandomCharacter, 1, 1000)
            };
        }

        private static GachaRewardEntry[] RelicRewardTable()
        {
            return new[]
            {
                Reward(GachaRewardKind.RandomRelic, 1, 1000)
            };
        }

        private static GachaRewardEntry[] NonContentRewardTable()
        {
            return new[]
            {
                Reward(GachaRewardKind.BattleRecords, 1000, 1000)
            };
        }

        private static GachaRewardEntry Reward(
            GachaRewardKind kind,
            long quantity,
            int weight)
        {
            var reward = new GachaRewardEntry();
            SetValue(reward, "rewardKind", kind);
            SetValue(reward, "quantity", quantity);
            SetValue(reward, "weight", weight);
            return reward;
        }

        private static GachaStateSaveData State(
            string gachaId,
            int pityCount = 0,
            long totalPullCount = 0L)
        {
            return new GachaStateSaveData
            {
                GachaId = gachaId,
                PityCount = pityCount,
                TotalPullCount = totalPullCount
            };
        }

        private static void AssertPullValuesEqual(
            IReadOnlyList<GachaPullResult> expected,
            IReadOnlyList<GachaPullResult> actual)
        {
            Assert.That(actual, Has.Count.EqualTo(expected.Count));
            for (int index = 0; index < expected.Count; index++)
            {
                Assert.That(actual[index].RewardKind,
                    Is.EqualTo(expected[index].RewardKind));
                Assert.That(actual[index].RewardId,
                    Is.EqualTo(expected[index].RewardId));
                Assert.That(actual[index].Amount,
                    Is.EqualTo(expected[index].Amount));
                Assert.That(actual[index].WasGuaranteedByPity,
                    Is.EqualTo(expected[index].WasGuaranteedByPity));
                Assert.That(actual[index].PityCountBefore,
                    Is.EqualTo(expected[index].PityCountBefore));
                Assert.That(actual[index].PityCountAfter,
                    Is.EqualTo(expected[index].PityCountAfter));
            }
        }

        private static void SetValue(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field was not found: {fieldName}");
        }

        private sealed class QueueRandomSource : IRandomSource
        {
            private readonly Queue<int> values;
            private readonly List<Tuple<int, int>> ranges =
                new List<Tuple<int, int>>();

            public QueueRandomSource(params int[] values)
            {
                this.values = new Queue<int>(values);
            }

            public IReadOnlyList<Tuple<int, int>> Ranges => ranges;

            public int Next(int minInclusive, int maxExclusive)
            {
                ranges.Add(Tuple.Create(minInclusive, maxExclusive));
                if (values.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No queued random value remains.");
                }

                int value = values.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Queued value {value} is outside "
                            + $"[{minInclusive}, {maxExclusive}).");
                }

                return value;
            }

            public float NextFloat()
            {
                throw new InvalidOperationException(
                    "Gacha calculation must not request floating-point "
                        + "randomness.");
            }
        }
    }
}
