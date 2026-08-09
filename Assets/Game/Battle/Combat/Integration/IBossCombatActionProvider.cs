using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IBossCombatActionProvider
    {
        IReadOnlyList<CombatAction> CreateRootActions(
            BossCombatActionContext context);
    }
}
