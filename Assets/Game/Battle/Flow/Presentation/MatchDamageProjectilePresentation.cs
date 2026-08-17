using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public enum MatchDamageProjectileCompletion
    {
        Arrived,
        Cancelled
    }

    public sealed class MatchDamageProjectileRequest
    {
        public MatchDamageProjectileRequest(
            int partySlotIndex,
            string characterId,
            ElementType attackElement)
        {
            if (partySlotIndex < 0
                || partySlotIndex >=
                    Battle.Combat.State.PartyBattleState
                        .MaximumCharacterCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partySlotIndex));
            }

            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "Character ID cannot be null or whitespace.",
                    nameof(characterId));
            }

            PartySlotIndex = partySlotIndex;
            CharacterId = characterId;
            AttackElement = attackElement;
        }

        public int PartySlotIndex { get; }
        public string CharacterId { get; }
        public ElementType AttackElement { get; }
    }

    public interface IMatchDamageProjectilePresenter
    {
        bool TryPresent(
            MatchDamageProjectileRequest request,
            Action<MatchDamageProjectileCompletion> completion);

        void CancelActive();
    }

    public enum BossDamageProjectileCompletion
    {
        Arrived,
        Cancelled
    }

    public sealed class BossDamageProjectileRequest
    {
        public BossDamageProjectileRequest(int currentTurn, long actionId)
        {
            if (currentTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentTurn));
            }

            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            CurrentTurn = currentTurn;
            ActionId = actionId;
        }

        public int CurrentTurn { get; }
        public long ActionId { get; }
    }

    public interface IBossDamageProjectilePresenter
    {
        bool TryPresent(
            BossDamageProjectileRequest request,
            Action<BossDamageProjectileCompletion> completion);

        void CancelActive();
    }
}
