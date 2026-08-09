using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IActiveAbilityActionProvider
    {
        IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context);
    }
}
