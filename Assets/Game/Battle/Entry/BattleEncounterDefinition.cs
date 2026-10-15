using System;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Entry
{
    /// <summary>Contains validated content required to compose one battle.</summary>
    public sealed class BattleEncounterDefinition
    {
        internal BattleEncounterDefinition(
            BossDefinition boss,
            BossDifficultyStats difficulty,
            KragmorCombatConfig kragmorConfig)
        {
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Difficulty = difficulty
                ?? throw new ArgumentNullException(nameof(difficulty));
            KragmorConfig = kragmorConfig
                ?? throw new ArgumentNullException(nameof(kragmorConfig));
        }

        public BossDefinition Boss { get; }
        public BossDifficultyStats Difficulty { get; }
        public KragmorCombatConfig KragmorConfig { get; }
    }
}
