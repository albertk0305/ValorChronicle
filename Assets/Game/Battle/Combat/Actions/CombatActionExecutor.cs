using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatActionExecutor
    {
        private readonly BossBattleState boss;
        private readonly PartyBattleState party;
        private readonly DamageContextFactory damageContextFactory;
        private readonly CombatTriggerResolver triggerResolver;

        public CombatActionExecutor(
            BossBattleState boss,
            PartyBattleState party,
            DamageContextFactory damageContextFactory)
            : this(
                boss,
                party,
                damageContextFactory,
                new CombatTriggerResolver(
                    Array.Empty<ICombatTriggerRule>()))
        {
        }

        public CombatActionExecutor(
            BossBattleState boss,
            PartyBattleState party,
            DamageContextFactory damageContextFactory,
            CombatTriggerResolver triggerResolver)
        {
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.damageContextFactory = damageContextFactory
                ?? throw new ArgumentNullException(nameof(damageContextFactory));
            this.triggerResolver = triggerResolver
                ?? throw new ArgumentNullException(nameof(triggerResolver));
        }

        public CombatActionExecutionResult Execute(CombatActionQueue queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            var results = new List<CombatActionResult>();
            if (boss.IsDefeated || party.IsIncapacitated)
            {
                bool cleared = queue.Count > 0;
                queue.Clear();
                return BuildResult(results, true, cleared);
            }

            while (queue.TryDequeue(out CombatAction action))
            {
                int executionOrder = results.Count + 1;
                CombatActionResult completedResult =
                    ExecuteAction(action, executionOrder);
                results.Add(completedResult);
                if (boss.IsDefeated || party.IsIncapacitated)
                {
                    bool cleared = queue.Count > 0;
                    queue.Clear();
                    return BuildResult(results, true, cleared);
                }

                var history = new CombatActionExecutionHistory(results);
                var triggerContext = new CombatActionTriggerContext(
                    completedResult,
                    boss,
                    party,
                    history);
                IReadOnlyList<CombatAction> derivedActions =
                    triggerResolver.Resolve(triggerContext);
                queue.EnqueueNextRange(derivedActions);
            }

            return BuildResult(results, false, false);
        }

        private CombatActionResult ExecuteAction(
            CombatAction action,
            int executionOrder)
        {
            if (action is DamageAction damageAction)
            {
                return ExecuteDamage(damageAction, executionOrder);
            }

            if (action is BossDamageAction bossDamageAction)
            {
                return ExecuteBossDamage(bossDamageAction, executionOrder);
            }

            if (action is HealAction healAction)
            {
                return ExecuteHealing(healAction, executionOrder);
            }

            if (action is ShieldAction shieldAction)
            {
                return ExecuteShield(shieldAction, executionOrder);
            }

            if (action is ApplyEffectAction effectAction)
            {
                return ExecuteApplyEffect(effectAction, executionOrder);
            }

            if (action is AddResourceAction addResourceAction)
            {
                return ExecuteAddResource(
                    addResourceAction,
                    executionOrder);
            }

            if (action is ConsumeResourceAction consumeResourceAction)
            {
                return ExecuteConsumeResource(
                    consumeResourceAction,
                    executionOrder);
            }

            throw new NotSupportedException(
                $"Unsupported combat action type: {action.GetType().Name}.");
        }

        private DamageActionResult ExecuteDamage(
            DamageAction action,
            int executionOrder)
        {
            DamageContextBuildRequest request = action.ContextRequest;
            ValidateBattleReferences(
                request.Party,
                request.TargetBoss);
            ValidatePartyCharacter(request.Attacker);
            DamageContext context = damageContextFactory.Build(request);
            DamageResult damageResult = DamageCalculator.Calculate(context);
            BossDamageApplicationResult applicationResult =
                BossHealthDamageApplier.Apply(boss, damageResult);
            return new DamageActionResult(
                action,
                executionOrder,
                context,
                damageResult,
                applicationResult);
        }

        private BossDamageActionResult ExecuteBossDamage(
            BossDamageAction action,
            int executionOrder)
        {
            BossDamageContextBuildRequest request = action.ContextRequest;
            ValidateBattleReferences(request.Party, request.Boss);
            BossDamageContext context =
                BossDamageContextFactory.Build(request);
            BossDamageResult damageResult =
                BossDamageCalculator.Calculate(context);
            PartyDamageApplicationResult applicationResult =
                PartyDamageApplier.Apply(party, damageResult);
            return new BossDamageActionResult(
                action,
                executionOrder,
                context,
                damageResult,
                applicationResult);
        }

        private HealActionResult ExecuteHealing(
            HealAction action,
            int executionOrder)
        {
            HealingContextBuildRequest request = action.ContextRequest;
            ValidatePartyReference(request.Party);
            ValidatePartyCharacter(request.Source);
            HealingContext context = HealingContextFactory.Build(request);
            HealingResult healingResult =
                HealingCalculator.Calculate(context);
            PartyHealingApplicationResult applicationResult =
                PartyHealingApplier.Apply(party, healingResult);
            return new HealActionResult(
                action,
                executionOrder,
                context,
                healingResult,
                applicationResult);
        }

        private ShieldActionResult ExecuteShield(
            ShieldAction action,
            int executionOrder)
        {
            ShieldGenerationContextBuildRequest request =
                action.ContextRequest;
            ValidatePartyReference(request.Party);
            ValidatePartyCharacter(request.Source);
            ShieldGenerationContext context =
                ShieldGenerationContextFactory.Build(request);
            ShieldGenerationResult generationResult =
                ShieldGenerationCalculator.Calculate(context);
            ShieldGrantApplicationResult applicationResult =
                ShieldGrantApplier.Apply(
                    party,
                    generationResult,
                    action.GrantRequest);
            return new ShieldActionResult(
                action,
                executionOrder,
                context,
                generationResult,
                applicationResult);
        }

        private ApplyEffectActionResult ExecuteApplyEffect(
            ApplyEffectAction action,
            int executionOrder)
        {
            ValidateEffectTarget(action);
            EffectInstance appliedEffect =
                action.TargetEffects.ApplyEffect(action.Effect);
            return new ApplyEffectActionResult(
                action,
                executionOrder,
                appliedEffect);
        }

        private AddResourceActionResult ExecuteAddResource(
            AddResourceAction action,
            int executionOrder)
        {
            ValidateBossReference(action.TargetBoss);
            ResourceAddResult addResult = boss.Resources.Add(
                action.ResourceId,
                action.Amount);
            return new AddResourceActionResult(
                action,
                executionOrder,
                addResult);
        }

        private ConsumeResourceActionResult ExecuteConsumeResource(
            ConsumeResourceAction action,
            int executionOrder)
        {
            ValidateBossReference(action.TargetBoss);
            ResourceConsumeResult consumeResult =
                action.Mode == ResourceConsumptionMode.All
                    ? boss.Resources.ConsumeAll(action.ResourceId)
                    : boss.Resources.Consume(
                        action.ResourceId,
                        action.Amount);
            ResourceConsumptionRecord consumptionRecord =
                consumeResult.ConsumedAmount > 0
                    ? new ResourceConsumptionRecord(
                        consumeResult,
                        action.ConsumerId,
                        boss.BossId)
                    : null;
            return new ConsumeResourceActionResult(
                action,
                executionOrder,
                consumeResult,
                consumptionRecord);
        }

        private void ValidateEffectTarget(ApplyEffectAction action)
        {
            switch (action.TargetType)
            {
                case CombatEffectTargetType.Character:
                    ValidatePartyCharacter(action.TargetCharacter);
                    break;
                case CombatEffectTargetType.Party:
                    ValidatePartyReference(action.TargetParty);
                    break;
                case CombatEffectTargetType.Boss:
                    ValidateBossReference(action.TargetBoss);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(action),
                        action.TargetType,
                        "Effect target type must be defined.");
            }
        }

        private void ValidateBattleReferences(
            PartyBattleState requestParty,
            BossBattleState requestBoss)
        {
            ValidatePartyReference(requestParty);
            ValidateBossReference(requestBoss);
        }

        private void ValidatePartyReference(PartyBattleState requestParty)
        {
            if (!ReferenceEquals(party, requestParty))
            {
                throw new InvalidOperationException(
                    "Action party does not belong to this executor.");
            }
        }

        private void ValidateBossReference(BossBattleState requestBoss)
        {
            if (!ReferenceEquals(boss, requestBoss))
            {
                throw new InvalidOperationException(
                    "Action boss does not belong to this executor.");
            }
        }

        private void ValidatePartyCharacter(CharacterBattleState character)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], character))
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "Action character does not belong to this executor's party.");
        }

        private CombatActionExecutionResult BuildResult(
            IReadOnlyList<CombatActionResult> results,
            bool stoppedEarly,
            bool clearedRemainingActions)
        {
            return new CombatActionExecutionResult(
                results,
                stoppedEarly,
                boss.IsDefeated,
                party.IsIncapacitated,
                clearedRemainingActions);
        }
    }
}
