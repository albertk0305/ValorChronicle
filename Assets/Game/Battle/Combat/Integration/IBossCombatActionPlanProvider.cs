namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IBossCombatActionPlanProvider
        : IBossCombatActionProvider
    {
        BossActionPlan CreatePlan(BossCombatActionContext context);
    }
}
