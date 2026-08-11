using System.Collections.Generic;

namespace ValorChronicle.Bosses.Kragmor
{
    public interface IKragmorBossIntentSource
    {
        KragmorBossIntent NextIntent { get; }

        IReadOnlyList<KragmorBossIntentPreview> GetIntentForecast(
            int count);
    }
}
