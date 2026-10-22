using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiActiveBoardPlanProvider
        : IActiveAbilityBoardPlanProvider,
          IActiveAbilityBoardSuccessCommitter
    {
        private readonly ShimotsukiMiyukiBattleState battleState;
        private readonly ShimotsukiMiyukiResolvedCombatRules combatRules;

        public ShimotsukiMiyukiActiveBoardPlanProvider(
            ShimotsukiMiyukiCombatConfig config,
            int awakening,
            ShimotsukiMiyukiBattleState battleState)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = ShimotsukiMiyukiCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.IsFrostEchoEnabled
                != combatRules.HasFrostEcho)
            {
                throw new ArgumentException(
                    "Miyuki battle state must match the resolved awakening.",
                    nameof(battleState));
            }
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
            ActiveAbilityBoardPlanContext context)
        {
            ValidateContext(context);
            return new ActiveAbilityBoardElementConversionPlan(
                ElementType.Water,
                combatRules.ActiveBlockConversionCount);
        }

        public void CommitBoardSuccess(
            ActiveAbilityBoardSuccessContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ValidateContext(context.PlanContext);
            if (context.Plan.TargetElement != ElementType.Water
                || !context.Result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Miyuki can commit only a successful Water conversion.");
            }

            if (combatRules.HasFrostEcho)
            {
                battleState.TryGrantFrostEcho();
            }
        }

        private void ValidateContext(ActiveAbilityBoardPlanContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!ReferenceEquals(context.Character, battleState.Owner)
                || !string.Equals(
                    context.Binding.CharacterId,
                    ShimotsukiMiyukiRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    context.Binding.ActiveAbilityId,
                    ShimotsukiMiyukiRules.ActiveAbilityId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Miyuki Active context does not match its battle state.");
            }
        }
    }
}
