using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearBuildModifier
        : ICharacterBuildModifier
    {
        private const double Awakening1AttackMultiplier = 1.05d;

        public string CharacterId => SerenaCoralspearRules.CharacterId;

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
                maxHpMultiplier: 1d,
                attackMultiplier: awakening >= 1
                    ? Awakening1AttackMultiplier
                    : 1d);
        }
    }
}
