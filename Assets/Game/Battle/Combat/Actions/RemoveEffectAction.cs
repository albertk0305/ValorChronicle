using System;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class RemoveEffectAction : CombatAction
    {
        public RemoveEffectAction(
            long actionId,
            ActionOrigin origin,
            CharacterBattleState target,
            long effectRuntimeId,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Character,
                target,
                null,
                null,
                effectRuntimeId,
                rootActionId,
                sourceActionId)
        {
        }

        public RemoveEffectAction(
            long actionId,
            ActionOrigin origin,
            PartyBattleState target,
            long effectRuntimeId,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Party,
                null,
                target,
                null,
                effectRuntimeId,
                rootActionId,
                sourceActionId)
        {
        }

        public RemoveEffectAction(
            long actionId,
            ActionOrigin origin,
            BossBattleState target,
            long effectRuntimeId,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Boss,
                null,
                null,
                target,
                effectRuntimeId,
                rootActionId,
                sourceActionId)
        {
        }

        private RemoveEffectAction(
            long actionId,
            ActionOrigin origin,
            CombatEffectTargetType targetType,
            CharacterBattleState targetCharacter,
            PartyBattleState targetParty,
            BossBattleState targetBoss,
            long effectRuntimeId,
            long? rootActionId,
            long? sourceActionId)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            if (effectRuntimeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectRuntimeId));
            }

            TargetType = targetType;
            TargetCharacter = targetCharacter;
            TargetParty = targetParty;
            TargetBoss = targetBoss;
            EffectRuntimeId = effectRuntimeId;
            if (TargetEffects == null)
            {
                throw new ArgumentNullException("target");
            }
        }

        public CombatEffectTargetType TargetType { get; }
        public CharacterBattleState TargetCharacter { get; }
        public PartyBattleState TargetParty { get; }
        public BossBattleState TargetBoss { get; }
        public long EffectRuntimeId { get; }

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
