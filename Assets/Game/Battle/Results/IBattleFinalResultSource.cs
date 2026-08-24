using System;

namespace ValorChronicle.Battle.Results
{
    public interface IBattleFinalResultSource
    {
        event Action<BattleFinalResult> ResultFinalized;
    }
}
