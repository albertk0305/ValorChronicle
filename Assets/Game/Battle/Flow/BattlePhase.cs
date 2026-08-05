namespace ValorChronicle.Battle.Flow
{
    public enum BattlePhase
    {
        NotStarted = 0,
        TurnStart = 1,
        ActiveInput = 2,
        PuzzleInput = 3,
        BoardResolving = 4,
        MatchEventResolving = 5,
        BossActing = 6,
        TurnEnd = 7,
        ResultCheck = 8,
        Result = 9
    }
}
