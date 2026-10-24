using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingBuildModifier : ICharacterBuildModifier
    {
        public string CharacterId => FioraAmberwingRules.CharacterId;

        public CharacterBuildStatMultipliers GetStatMultipliers(int awakening)
        {
            if (awakening < CharacterBuildResolver.MinimumAwakening || awakening > CharacterBuildResolver.MaximumAwakening)
                throw new ArgumentOutOfRangeException(nameof(awakening));

            return new CharacterBuildStatMultipliers(
                maxHpMultiplier: awakening >= 1 ? 1.05d : 1d,
                attackMultiplier: 1d);
        }
    }
}
