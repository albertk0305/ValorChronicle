using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IMatchEventActionProvider
    {
        IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context);
    }
}
