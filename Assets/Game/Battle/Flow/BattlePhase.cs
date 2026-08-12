namespace ValorChronicle.Battle.Flow
{
    public enum BattlePhase
    {
        NotStarted = 0,
        TurnStart = 1,
        PlayerInput = 2,
        BoardResolving = 3,
        MatchEventResolving = 4,
        BossActing = 5,
        TurnEnd = 6,
        ResultCheck = 7,
        Result = 8
    }
}
