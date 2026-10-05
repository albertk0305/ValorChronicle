using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Marea
{
    public sealed class MareaBluefangBuildModifier
        : ICharacterBuildModifier
    {
        private const double AwakeningStatMultiplier = 1.05d;

        public string CharacterId => MareaBluefangRules.CharacterId;

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

            double maxHpMultiplier = awakening >= 2
                ? AwakeningStatMultiplier
                : 1d;
            double attackMultiplier = awakening >= 1
                ? AwakeningStatMultiplier
                : 1d;
            return new CharacterBuildStatMultipliers(
                maxHpMultiplier,
                attackMultiplier);
        }
    }
}
