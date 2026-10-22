using System;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IActiveAbilityBoardPlanProvider
    {
        /// <summary>
        /// Creates a side-effect-free plan. Runtime state must only be changed
        /// after the common Active execution path reports success.
        /// </summary>
        ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
            ActiveAbilityBoardPlanContext context);
    }

    /// <summary>
    /// Receives the single success commit for a Board-only Active after the
    /// sink has returned a validated successful result. Implementations must
    /// limit this callback to non-throwing battle-local state changes.
    /// </summary>
    public interface IActiveAbilityBoardSuccessCommitter
    {
        void CommitBoardSuccess(
            ActiveAbilityBoardSuccessContext context);
    }

    public sealed class ActiveAbilityBoardPlanContext
    {
        internal ActiveAbilityBoardPlanContext(
            ActiveAbilityBinding binding,
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss)
        {
            Binding = binding
                ?? throw new ArgumentNullException(nameof(binding));
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            Party = party
                ?? throw new ArgumentNullException(nameof(party));
            Boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
        }

        public ActiveAbilityBinding Binding { get; }
        public CharacterBattleState Character { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
    }

    public sealed class ActiveAbilityBoardElementConversionPlan
    {
        public ActiveAbilityBoardElementConversionPlan(
            ElementType targetElement,
            int requestedCount)
        {
            if (!Enum.IsDefined(typeof(ElementType), targetElement))
            {
                throw new ArgumentOutOfRangeException(nameof(targetElement));
            }

            if (requestedCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            TargetElement = targetElement;
            RequestedCount = requestedCount;
        }

        public ElementType TargetElement { get; }
        public int RequestedCount { get; }
    }

    public sealed class ActiveAbilityBoardSuccessContext
    {
        internal ActiveAbilityBoardSuccessContext(
            ActiveAbilityBoardPlanContext planContext,
            ActiveAbilityBoardElementConversionPlan plan,
            BoardElementConversionResult result)
        {
            PlanContext = planContext
                ?? throw new ArgumentNullException(nameof(planContext));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public ActiveAbilityBoardPlanContext PlanContext { get; }
        public ActiveAbilityBoardElementConversionPlan Plan { get; }
        public BoardElementConversionResult Result { get; }
        public ActiveAbilityBinding Binding => PlanContext.Binding;
        public CharacterBattleState Character => PlanContext.Character;
        public PartyBattleState Party => PlanContext.Party;
        public BossBattleState Boss => PlanContext.Boss;
    }
}
