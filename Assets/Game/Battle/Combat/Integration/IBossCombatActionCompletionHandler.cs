namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IBossCombatActionCompletionHandler
    {
        bool TryCommitCompletedAction();
    }
}
