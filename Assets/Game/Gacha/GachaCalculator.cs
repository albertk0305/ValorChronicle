using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Gacha
{
    public sealed class GachaCalculator
    {
        public const int TenPullCount = 10;

        private readonly DefinitionDatabase database;
        private readonly IRandomSource randomSource;

        public GachaCalculator(
            DefinitionDatabase database,
            IRandomSource randomSource)
        {
            this.database = database
                ?? throw new ArgumentNullException(nameof(database));
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));

            if (!database.IsInitialized)
            {
                throw new ArgumentException(
                    "Definition database must be initialized.",
                    nameof(database));
            }
        }

        public GachaCalculationResult GenerateSingle(
            string gachaId,
            GachaStateSaveData state)
        {
            return Generate(gachaId, state, 1);
        }

        public GachaCalculationResult GenerateTen(
            string gachaId,
            GachaStateSaveData state)
        {
            return Generate(gachaId, state, TenPullCount);
        }

        public GachaCalculationResult Generate(
            string gachaId,
            GachaStateSaveData state,
            int pullCount)
        {
            if (string.IsNullOrWhiteSpace(gachaId))
            {
                throw new ArgumentException(
                    "Gacha ID cannot be null or whitespace.",
                    nameof(gachaId));
            }

            if (!database.TryGetGacha(gachaId, out GachaDefinition definition))
            {
                throw new KeyNotFoundException(
                    $"Gacha definition was not found: {gachaId}.");
            }

            return GenerateFromDefinition(definition, state, pullCount);
        }

        public GachaCalculationResult GenerateFromDefinition(
            GachaDefinition definition,
            GachaStateSaveData state,
            int pullCount)
        {
            ValidateDefinition(definition);
            ValidateState(definition, state);
            if (pullCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pullCount),
                    pullCount,
                    "Pull count must be positive.");
            }

            if (state.TotalPullCount > long.MaxValue - pullCount)
            {
                throw new OverflowException(
                    "Total Gacha pull count would overflow.");
            }

            var pulls = new GachaPullResult[pullCount];
            int pityCount = state.PityCount;
            long totalPullCount = state.TotalPullCount;
            for (int index = 0; index < pullCount; index++)
            {
                int pityCountBefore = pityCount;
                bool guaranteedByPity =
                    pityCountBefore == definition.PityPullCount - 1;

                GachaRewardKind rewardKind;
                long amount;
                string rewardId;
                if (guaranteedByPity)
                {
                    rewardKind = GetPityRewardKind(definition.GachaKind);
                    amount = 1L;
                    rewardId = SelectContentId(definition, rewardKind);
                }
                else
                {
                    GachaRewardEntry reward = GachaRewardSelector.Select(
                        definition,
                        randomSource);
                    rewardKind = reward.RewardKind;
                    amount = reward.Quantity;
                    rewardId = IsContentReward(rewardKind)
                        ? SelectContentId(definition, rewardKind)
                        : string.Empty;
                }

                pityCount = IsPityTarget(
                    definition.GachaKind,
                    rewardKind)
                    ? 0
                    : checked(pityCount + 1);
                totalPullCount++;
                pulls[index] = new GachaPullResult(
                    index,
                    definition.Id,
                    rewardKind,
                    rewardId,
                    amount,
                    guaranteedByPity,
                    pityCountBefore,
                    pityCount);
            }

            return new GachaCalculationResult(
                pulls,
                definition.Id,
                pityCount,
                totalPullCount);
        }

        private void ValidateDefinition(GachaDefinition definition)
        {
            GachaRewardSelector.ValidateOrThrow(definition);
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException(
                    "Gacha definition ID cannot be empty.",
                    nameof(definition));
            }

            if (!database.TryGetGacha(
                    definition.Id,
                    out GachaDefinition registered)
                || !ReferenceEquals(registered, definition))
            {
                throw new ArgumentException(
                    "Gacha definition must be registered in the database.",
                    nameof(definition));
            }

            if (!Enum.IsDefined(typeof(GachaKind), definition.GachaKind))
            {
                throw new ArgumentException(
                    "Gacha kind is invalid.",
                    nameof(definition));
            }

            if (definition.SinglePullCost <= 0L
                || definition.TenPullCost <= 0L
                || definition.SinglePullCost > long.MaxValue / TenPullCount
                || definition.TenPullCost
                    != definition.SinglePullCost * TenPullCount)
            {
                throw new ArgumentException(
                    "Gacha pull costs are invalid.",
                    nameof(definition));
            }

            if (definition.PityPullCount <= 0)
            {
                throw new ArgumentException(
                    "Gacha pity pull count must be positive.",
                    nameof(definition));
            }

            bool needsCharacters =
                definition.GachaKind == GachaKind.Character;
            bool needsRelics = definition.GachaKind == GachaKind.Relic;
            var rewardKeys = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<GachaRewardEntry> rewards =
                definition.RewardEntries;
            for (int index = 0; index < rewards.Count; index++)
            {
                GachaRewardEntry reward = rewards[index];
                needsCharacters |= reward.RewardKind
                    == GachaRewardKind.RandomCharacter;
                needsRelics |= reward.RewardKind
                    == GachaRewardKind.RandomRelic;
                string rewardKey =
                    $"{(int)reward.RewardKind}\u001f{reward.Quantity}";
                if (!rewardKeys.Add(rewardKey))
                {
                    throw new ArgumentException(
                        "Gacha reward kind and quantity cannot be duplicate.",
                        nameof(definition));
                }
            }

            ValidatePool(
                definition.CharacterPoolIds,
                needsCharacters,
                "character",
                id => database.TryGetCharacter(id, out _),
                definition);
            ValidatePool(
                definition.RelicPoolIds,
                needsRelics,
                "relic",
                id => database.TryGetRelic(id, out _),
                definition);
        }

        private static void ValidateState(
            GachaDefinition definition,
            GachaStateSaveData state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!string.Equals(
                    definition.Id,
                    state.GachaId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Gacha state ID must match the definition ID.",
                    nameof(state));
            }

            if (state.PityCount < 0
                || state.PityCount >= definition.PityPullCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state.PityCount,
                    "Pity count is outside the valid range.");
            }

            if (state.TotalPullCount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state.TotalPullCount,
                    "Total pull count cannot be negative.");
            }

            if (state.PityCount > state.TotalPullCount)
            {
                throw new ArgumentException(
                    "Pity count cannot exceed total pull count.",
                    nameof(state));
            }
        }

        private static void ValidatePool(
            IReadOnlyList<string> poolIds,
            bool isRequired,
            string poolName,
            Func<string, bool> exists,
            GachaDefinition definition)
        {
            if (isRequired && poolIds.Count == 0)
            {
                throw new ArgumentException(
                    $"Gacha {poolName} pool cannot be empty.",
                    nameof(definition));
            }

            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < poolIds.Count; index++)
            {
                string id = poolIds[index];
                if (string.IsNullOrWhiteSpace(id) || !exists(id))
                {
                    throw new ArgumentException(
                        $"Gacha {poolName} pool contains an invalid ID.",
                        nameof(definition));
                }

                if (!uniqueIds.Add(id))
                {
                    throw new ArgumentException(
                        $"Gacha {poolName} pool contains a duplicate ID.",
                        nameof(definition));
                }
            }
        }

        private string SelectContentId(
            GachaDefinition definition,
            GachaRewardKind rewardKind)
        {
            IReadOnlyList<string> pool = rewardKind
                == GachaRewardKind.RandomCharacter
                ? definition.CharacterPoolIds
                : definition.RelicPoolIds;
            int index = randomSource.Next(0, pool.Count);
            return pool[index];
        }

        private static bool IsContentReward(GachaRewardKind rewardKind)
        {
            return rewardKind == GachaRewardKind.RandomCharacter
                || rewardKind == GachaRewardKind.RandomRelic;
        }

        private static GachaRewardKind GetPityRewardKind(GachaKind kind)
        {
            return kind == GachaKind.Character
                ? GachaRewardKind.RandomCharacter
                : GachaRewardKind.RandomRelic;
        }

        private static bool IsPityTarget(
            GachaKind kind,
            GachaRewardKind rewardKind)
        {
            return rewardKind == GetPityRewardKind(kind);
        }
    }
}
