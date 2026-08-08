using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public abstract class CombatActionResult
    {
        protected CombatActionResult(
            CombatAction action,
            int executionOrder)
        {
            Action = action;
            ExecutionOrder = executionOrder;
        }

        public CombatAction Action { get; }
        public int ExecutionOrder { get; }
    }

    public sealed class DamageActionResult : CombatActionResult
    {
        internal DamageActionResult(
            DamageAction action,
            int executionOrder,
            DamageContext context,
            DamageResult damageResult,
            BossDamageApplicationResult applicationResult)
            : base(action, executionOrder)
        {
            Context = context;
            DamageResult = damageResult;
            ApplicationResult = applicationResult;
        }

        public DamageContext Context { get; }
        public DamageResult DamageResult { get; }
        public BossDamageApplicationResult ApplicationResult { get; }
        public bool BecameDefeated => ApplicationResult.BecameDefeated;
    }

    public sealed class BossDamageActionResult : CombatActionResult
    {
        internal BossDamageActionResult(
            BossDamageAction action,
            int executionOrder,
            BossDamageContext context,
            BossDamageResult damageResult,
            PartyDamageApplicationResult applicationResult)
            : base(action, executionOrder)
        {
            Context = context;
            DamageResult = damageResult;
            ApplicationResult = applicationResult;
        }

        public BossDamageContext Context { get; }
        public BossDamageResult DamageResult { get; }
        public PartyDamageApplicationResult ApplicationResult { get; }
        public bool BecameIncapacitated =>
            ApplicationResult.BecameIncapacitated;
    }

    public sealed class HealActionResult : CombatActionResult
    {
        internal HealActionResult(
            HealAction action,
            int executionOrder,
            HealingContext context,
            HealingResult healingResult,
            PartyHealingApplicationResult applicationResult)
            : base(action, executionOrder)
        {
            Context = context;
            HealingResult = healingResult;
            ApplicationResult = applicationResult;
        }

        public HealingContext Context { get; }
        public HealingResult HealingResult { get; }
        public PartyHealingApplicationResult ApplicationResult { get; }
        public long FinalHealing => HealingResult.FinalHealing;
        public long AppliedHealing => ApplicationResult.AppliedHealing;
        public long OverhealAmount => ApplicationResult.OverhealAmount;
    }

    public sealed class ShieldActionResult : CombatActionResult
    {
        internal ShieldActionResult(
            ShieldAction action,
            int executionOrder,
            ShieldGenerationContext context,
            ShieldGenerationResult generationResult,
            ShieldGrantApplicationResult applicationResult)
            : base(action, executionOrder)
        {
            Context = context;
            GenerationResult = generationResult;
            ApplicationResult = applicationResult;
        }

        public ShieldGenerationContext Context { get; }
        public ShieldGenerationResult GenerationResult { get; }
        public ShieldGrantApplicationResult ApplicationResult { get; }
    }

    public sealed class ApplyEffectActionResult : CombatActionResult
    {
        internal ApplyEffectActionResult(
            ApplyEffectAction action,
            int executionOrder,
            EffectInstance appliedEffect)
            : base(action, executionOrder)
        {
            AppliedEffect = appliedEffect;
        }

        public EffectInstance AppliedEffect { get; }
    }

    public sealed class AddResourceActionResult : CombatActionResult
    {
        internal AddResourceActionResult(
            AddResourceAction action,
            int executionOrder,
            ResourceAddResult addResult)
            : base(action, executionOrder)
        {
            AddResult = addResult;
        }

        public ResourceAddResult AddResult { get; }
    }

    public sealed class ConsumeResourceActionResult : CombatActionResult
    {
        internal ConsumeResourceActionResult(
            ConsumeResourceAction action,
            int executionOrder,
            ResourceConsumeResult consumeResult,
            ResourceConsumptionRecord consumptionRecord)
            : base(action, executionOrder)
        {
            ConsumeResult = consumeResult;
            ConsumptionRecord = consumptionRecord;
        }

        public ResourceConsumeResult ConsumeResult { get; }
        public ResourceConsumptionRecord ConsumptionRecord { get; }
    }
}
