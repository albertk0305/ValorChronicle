using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Naira
{
    public sealed class NairaWindhornActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly NairaWindhornResolvedCombatRules combatRules;

        public NairaWindhornActiveActionProvider(
            NairaWindhornCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public NairaWindhornActiveActionProvider(
            NairaWindhornCombatConfig config,
            int awakening)
        {
            combatRules = NairaWindhornCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return CreateActions(
                context.Character,
                context.Party,
                context.PlayerTurnNumber,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState actor,
            PartyBattleState party,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(actor, party, playerTurnNumber, actionIds);

            long shieldActionId = actionIds.Next();
            var shieldAction = new ShieldAction(
                shieldActionId,
                ActionOrigin.Active,
                new ShieldGenerationContextBuildRequest(
                    actor,
                    party,
                    combatRules.ActiveShieldMaxHpRate,
                    combatRules.ActiveShieldUsesComboMultiplier,
                    finalComboCount: 0),
                new ShieldGrantRequest(
                    runtimeId: shieldActionId,
                    sourceId: NairaWindhornRules.ActiveAbilityId,
                    createdTurn: playerTurnNumber,
                    remainingTurns: combatRules.ShieldDurationTurns,
                    creationOrder: shieldActionId));

            long effectActionId = actionIds.Next();
            var whiteStagGrace = new EffectInstance(
                effectActionId,
                NairaWindhornRules.WhiteStagGraceEffectId,
                actor.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.NextBossHitDamageReduction,
                combatRules.WhiteStagGraceDamageReductionRate,
                remainingTurns: null,
                creationOrder: effectActionId,
                stackPolicy: EffectStackPolicy.Unique);
            var effectAction = new ApplyEffectAction(
                effectActionId,
                ActionOrigin.Active,
                party,
                whiteStagGrace);

            return new CombatAction[]
            {
                shieldAction,
                effectAction
            };
        }

        private static void ValidateInputs(
            CharacterBattleState actor,
            PartyBattleState party,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!string.Equals(
                    actor.CharacterId,
                    NairaWindhornRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Grass)
            {
                throw new ArgumentException(
                    "Actor must be Naira Windhorn with Grass element.",
                    nameof(actor));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            bool belongsToParty = false;
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], actor))
                {
                    belongsToParty = true;
                    break;
                }
            }

            if (!belongsToParty)
            {
                throw new ArgumentException(
                    "Actor must belong to the target party.",
                    nameof(actor));
            }

            if (playerTurnNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerTurnNumber));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
