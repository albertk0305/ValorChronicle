using System;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public enum CombatEffectTargetType
    {
        Character,
        Party,
        Boss
    }

    public sealed class ApplyEffectAction : CombatAction
    {
        public ApplyEffectAction(
            long actionId,
            ActionOrigin origin,
            CharacterBattleState target,
            EffectInstance effect,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Character,
                target,
                null,
                null,
                effect,
                rootActionId,
                sourceActionId)
        {
        }

        public ApplyEffectAction(
            long actionId,
            ActionOrigin origin,
            PartyBattleState target,
            EffectInstance effect,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Party,
                null,
                target,
                null,
                effect,
                rootActionId,
                sourceActionId)
        {
        }

        public ApplyEffectAction(
            long actionId,
            ActionOrigin origin,
            BossBattleState target,
            EffectInstance effect,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Boss,
                null,
                null,
                target,
                effect,
                rootActionId,
                sourceActionId)
        {
        }

        private ApplyEffectAction(
            long actionId,
            ActionOrigin origin,
            CombatEffectTargetType targetType,
            CharacterBattleState targetCharacter,
            PartyBattleState targetParty,
            BossBattleState targetBoss,
            EffectInstance effect,
            long? rootActionId,
            long? sourceActionId)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            TargetType = targetType;
            TargetCharacter = targetCharacter;
            TargetParty = targetParty;
            TargetBoss = targetBoss;
            Effect = effect
                ?? throw new ArgumentNullException(nameof(effect));
            if (TargetEffects == null)
            {
                throw new ArgumentNullException("target");
            }
        }

        public CombatEffectTargetType TargetType { get; }
        public CharacterBattleState TargetCharacter { get; }
        public PartyBattleState TargetParty { get; }
        public BossBattleState TargetBoss { get; }
        public EffectInstance Effect { get; }

        internal EffectCollection TargetEffects
        {
            get
            {
                switch (TargetType)
                {
                    case CombatEffectTargetType.Character:
                        return TargetCharacter?.Effects;
                    case CombatEffectTargetType.Party:
                        return TargetParty?.Effects;
                    case CombatEffectTargetType.Boss:
                        return TargetBoss?.Effects;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
