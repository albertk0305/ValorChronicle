using System;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Stats
{
    public static class CharacterBattleStateFactory
    {
        public static CharacterBattleState Create(
            CharacterDefinition definition,
            int level,
            int partySlotIndex)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            CharacterStatValues stats =
                CharacterStatCalculator.Calculate(definition, level);
            return new CharacterBattleState(
                definition.Id,
                partySlotIndex,
                definition.Element,
                stats.MaxHp,
                stats.Attack);
        }
    }
}
