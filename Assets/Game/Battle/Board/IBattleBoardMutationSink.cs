using System;

namespace ValorChronicle.Battle.Board
{
    public interface IBattleBoardMutationSink
    {
        bool TryExecuteMutation(
            BattleBoardMutationCommand command,
            Action<BattleBoardMutationCompletion> completion);
    }
}
