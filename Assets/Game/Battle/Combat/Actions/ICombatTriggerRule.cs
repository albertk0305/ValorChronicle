using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.Actions
{
    public interface ICombatTriggerRule
    {
        IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context);
    }
}
