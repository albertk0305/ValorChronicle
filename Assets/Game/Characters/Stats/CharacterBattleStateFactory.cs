using System;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Stats
{
    public static class CharacterBattleStateFactory
    {
        public static CharacterBattleState Create(
            CharacterDefinition definition,
            ResolvedCharacterBuild build,
            int partySlotIndex)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            if (!string.Equals(
                definition.Id,
                build.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The resolved build must match the character "
                        + "definition.",
                    nameof(build));
            }

            return new CharacterBattleState(
                build.CharacterId,
                partySlotIndex,
                definition.Element,
                build.MaxHp,
                build.Attack);
        }
    }
}
