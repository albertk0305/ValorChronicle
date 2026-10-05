using System;
using System.Collections.Generic;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Build
{
    public sealed class CharacterBuildResolver
    {
        public const int MinimumAwakening = 0;
        public const int MaximumAwakening = 6;

        private readonly Dictionary<string, ICharacterBuildModifier>
            modifiersByCharacterId;

        public CharacterBuildResolver(
            IReadOnlyList<ICharacterBuildModifier> modifiers)
        {
            if (modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }

            modifiersByCharacterId =
                new Dictionary<string, ICharacterBuildModifier>(
                    modifiers.Count,
                    StringComparer.Ordinal);
            for (int index = 0; index < modifiers.Count; index++)
            {
                ICharacterBuildModifier modifier = modifiers[index];
                if (modifier == null)
                {
                    throw new ArgumentException(
                        $"Build modifier entry {index} is null.",
                        nameof(modifiers));
                }

                if (string.IsNullOrEmpty(modifier.CharacterId))
                {
                    throw new ArgumentException(
                        $"Build modifier entry {index} has no character ID.",
                        nameof(modifiers));
                }

                if (modifiersByCharacterId.ContainsKey(
                    modifier.CharacterId))
                {
                    throw new ArgumentException(
                        $"A build modifier for '{modifier.CharacterId}' "
                            + "is already registered.",
                        nameof(modifiers));
                }

                modifiersByCharacterId.Add(
                    modifier.CharacterId,
                    modifier);
            }
        }

        public ResolvedCharacterBuild Resolve(
            CharacterDefinition definition,
            int level,
            int awakening)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (string.IsNullOrEmpty(definition.Id))
            {
                throw new ArgumentException(
                    "Character definition must have a stable ID.",
                    nameof(definition));
            }

            ValidateAwakening(awakening);
            CharacterStatValues baseStats =
                CharacterStatCalculator.Calculate(definition, level);
            CharacterBuildStatMultipliers multipliers =
                CharacterBuildStatMultipliers.Identity;
            if (modifiersByCharacterId.TryGetValue(
                definition.Id,
                out ICharacterBuildModifier modifier))
            {
                multipliers = modifier.GetStatMultipliers(awakening)
                    ?? throw new InvalidOperationException(
                        $"The build modifier for '{definition.Id}' "
                            + "returned no stat multipliers.");
            }

            long maxHp = CharacterStatCalculator.RoundFinalStat(
                baseStats.MaxHp * multipliers.MaxHpMultiplier);
            long attack = CharacterStatCalculator.RoundFinalStat(
                baseStats.Attack * multipliers.AttackMultiplier);
            return new ResolvedCharacterBuild(
                definition.Id,
                level,
                awakening,
                maxHp,
                attack);
        }

        private static void ValidateAwakening(int awakening)
        {
            if (awakening < MinimumAwakening
                || awakening > MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between {MinimumAwakening} and "
                        + $"{MaximumAwakening}.");
            }
        }
    }
}
