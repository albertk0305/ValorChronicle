using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Modifiers
{
    public static class CombatModifierCollector
    {
        public static AllyDamageModifierSnapshot CollectAllyDamage(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState target,
            ElementType attackElement)
        {
            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            ValidateElement(attackElement);
            var values = new AllyModifierValues();
            CollectAllySource(
                attacker.Effects.GetActiveEffects(),
                attackElement,
                values);
            CollectAllySource(
                party.Effects.GetActiveEffects(),
                attackElement,
                values);
            CollectTargetSource(
                target.Effects.GetActiveEffects(),
                values);
            return new AllyDamageModifierSnapshot(
                values.AttackIncrease,
                values.AttackReduction,
                values.ElementDamageIncrease,
                0d,
                values.DealtDamageIncrease,
                values.DealtDamageReduction,
                values.TargetTakenDamageIncrease,
                values.TargetTakenDamageReduction,
                values.CriticalChanceIncrease,
                values.CriticalDamageIncrease);
        }

        public static BossAttackModifierSnapshot CollectBossAttack(
            BossBattleState boss,
            PartyBattleState party)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            var values = new BossModifierValues();
            IReadOnlyList<EffectInstance> bossEffects =
                boss.Effects.GetActiveEffects();
            for (int index = 0; index < bossEffects.Count; index++)
            {
                EffectInstance effect = bossEffects[index];
                switch (effect.ModifierType)
                {
                    case EffectModifierType.BossAttackIncrease:
                        values.AttackIncrease = Add(
                            values.AttackIncrease,
                            effect);
                        break;
                    case EffectModifierType.BossAttackReduction:
                        values.AttackReduction = Add(
                            values.AttackReduction,
                            effect);
                        break;
                    case EffectModifierType.BossDealtDamageIncrease:
                        values.DealtDamageIncrease = Add(
                            values.DealtDamageIncrease,
                            effect);
                        break;
                    case EffectModifierType.BossDealtDamageReduction:
                        values.DealtDamageReduction = Add(
                            values.DealtDamageReduction,
                            effect);
                        break;
                }
            }

            IReadOnlyList<EffectInstance> partyEffects =
                party.Effects.GetActiveEffects();
            for (int index = 0; index < partyEffects.Count; index++)
            {
                EffectInstance effect = partyEffects[index];
                switch (effect.ModifierType)
                {
                    case EffectModifierType.PartyTakenDamageIncrease:
                        values.PartyTakenDamageIncrease = Add(
                            values.PartyTakenDamageIncrease,
                            effect);
                        break;
                    case EffectModifierType.PartyDamageReduction:
                        values.PartyDamageReduction = Add(
                            values.PartyDamageReduction,
                            effect);
                        break;
                }
            }

            return new BossAttackModifierSnapshot(
                values.AttackIncrease,
                values.AttackReduction,
                values.DealtDamageIncrease,
                values.DealtDamageReduction,
                values.PartyTakenDamageIncrease,
                values.PartyDamageReduction);
        }

        private static void CollectAllySource(
            IReadOnlyList<EffectInstance> effects,
            ElementType attackElement,
            AllyModifierValues values)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                EffectInstance effect = effects[index];
                switch (effect.ModifierType)
                {
                    case EffectModifierType.AttackIncrease:
                        values.AttackIncrease = Add(
                            values.AttackIncrease,
                            effect);
                        break;
                    case EffectModifierType.AttackReduction:
                        values.AttackReduction = Add(
                            values.AttackReduction,
                            effect);
                        break;
                    case EffectModifierType.ElementDamageIncrease:
                        if (effect.ElementFilter == attackElement)
                        {
                            values.ElementDamageIncrease = Add(
                                values.ElementDamageIncrease,
                                effect);
                        }

                        break;
                    case EffectModifierType.DealtDamageIncrease:
                        values.DealtDamageIncrease = Add(
                            values.DealtDamageIncrease,
                            effect);
                        break;
                    case EffectModifierType.DealtDamageReduction:
                        values.DealtDamageReduction = Add(
                            values.DealtDamageReduction,
                            effect);
                        break;
                    case EffectModifierType.CriticalChanceIncrease:
                        values.CriticalChanceIncrease = Add(
                            values.CriticalChanceIncrease,
                            effect);
                        break;
                    case EffectModifierType.CriticalDamageIncrease:
                        values.CriticalDamageIncrease = Add(
                            values.CriticalDamageIncrease,
                            effect);
                        break;
                }
            }
        }

        private static void CollectTargetSource(
            IReadOnlyList<EffectInstance> effects,
            AllyModifierValues values)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                EffectInstance effect = effects[index];
                switch (effect.ModifierType)
                {
                    case EffectModifierType.TargetTakenDamageIncrease:
                        values.TargetTakenDamageIncrease = Add(
                            values.TargetTakenDamageIncrease,
                            effect);
                        break;
                    case EffectModifierType.TargetTakenDamageReduction:
                        values.TargetTakenDamageReduction = Add(
                            values.TargetTakenDamageReduction,
                            effect);
                        break;
                }
            }
        }

        private static double Add(
            double current,
            EffectInstance effect)
        {
            double result = current + effect.EffectiveMagnitude;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException(
                    "Collected modifier sum must remain finite.");
            }

            return result;
        }

        private static void ValidateElement(ElementType element)
        {
            if (!Enum.IsDefined(typeof(ElementType), element))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(element),
                    element,
                    "Attack element must be defined.");
            }
        }

        private sealed class AllyModifierValues
        {
            public double AttackIncrease;
            public double AttackReduction;
            public double ElementDamageIncrease;
            public double DealtDamageIncrease;
            public double DealtDamageReduction;
            public double TargetTakenDamageIncrease;
            public double TargetTakenDamageReduction;
            public double CriticalChanceIncrease;
            public double CriticalDamageIncrease;
        }

        private sealed class BossModifierValues
        {
            public double AttackIncrease;
            public double AttackReduction;
            public double DealtDamageIncrease;
            public double DealtDamageReduction;
            public double PartyTakenDamageIncrease;
            public double PartyDamageReduction;
        }
    }
}
