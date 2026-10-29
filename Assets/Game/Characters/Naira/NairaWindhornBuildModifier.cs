using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Naira
{
    public sealed class NairaWindhornBuildModifier
        : ICharacterBuildModifier
    {
        private const double AwakeningStatMultiplier = 1.05d;

        public string CharacterId => NairaWindhornRules.CharacterId;

        public CharacterBuildStatMultipliers GetStatMultipliers(
            int awakening)
        {
            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between "
                        + $"{CharacterBuildResolver.MinimumAwakening} and "
                        + $"{CharacterBuildResolver.MaximumAwakening}.");
            }

            return new CharacterBuildStatMultipliers(
                maxHpMultiplier: awakening >= 1
                    ? AwakeningStatMultiplier
                    : 1d,
                attackMultiplier: awakening >= 2
                    ? AwakeningStatMultiplier
                    : 1d);
        }
    }
}

