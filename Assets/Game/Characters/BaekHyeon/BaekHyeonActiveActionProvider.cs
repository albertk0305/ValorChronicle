using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.BaekHyeon
{
    public sealed class BaekHyeonActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly BaekHyeonResolvedCombatRules combatRules;

        public BaekHyeonActiveActionProvider(BaekHyeonCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public BaekHyeonActiveActionProvider(
            BaekHyeonCombatConfig config,
            int awakening)
        {
            combatRules = BaekHyeonCombatRuleResolver.Resolve(
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
                context.Boss,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(attacker, party, boss, actionIds);

            long actionId = actionIds.Next();
            return new CombatAction[]
            {
                new DamageAction(
                    actionId,
                    ActionOrigin.Active,
                    new DamageContextBuildRequest(
                        attacker,
                        party,
                        boss,
                        ElementType.Grass,
                        combatRules.ActiveAttackType,
                        AttackTag.None,
                        combatRules.ActiveBaseCoefficient,
                        combatRules.ActiveUsesCombo,
                        finalComboCount: 1,
                        canCritical: true,
                        skillCoefficientResolver: request =>
                            combatRules.CalculateActiveCoefficient(
                                BaekHyeonMarkEffectFactory.GetMarkCount(
                                    request.TargetBoss))))
            };
        }

        private static void ValidateInputs(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            if (!string.Equals(
                    attacker.CharacterId,
                    BaekHyeonRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Grass)
            {
                throw new ArgumentException(
                    "Attacker must be Baek Hyeon with Grass element.",
                    nameof(attacker));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
