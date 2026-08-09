using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class MatchEventCombatActionFactory
    {
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly IMatchEventActionProvider provider;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> createdActionIds =
            new HashSet<long>();

        public MatchEventCombatActionFactory(
            PartyBattleState party,
            BossBattleState boss,
            IMatchEventActionProvider provider,
            CombatActionIdSequence actionIds)
        {
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.provider = provider
                ?? throw new ArgumentNullException(nameof(provider));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventExecution execution)
        {
            if (execution == null)
            {
                throw new ArgumentNullException(nameof(execution));
            }

            var rootActions = new List<CombatAction>();
            for (int characterIndex = 0;
                characterIndex < party.Characters.Count;
                characterIndex++)
            {
                CharacterBattleState character =
                    party.Characters[characterIndex];
                if (character.Element != execution.MatchEvent.Element)
                {
                    continue;
                }

                IReadOnlyList<CombatAction> characterActions =
                    CreateCharacterRootActions(execution, character);
                for (int actionIndex = 0;
                    actionIndex < characterActions.Count;
                    actionIndex++)
                {
                    rootActions.Add(characterActions[actionIndex]);
                }
            }

            return rootActions.AsReadOnly();
        }

        public IReadOnlyList<CombatAction> CreateCharacterRootActions(
            MatchEventExecution execution,
            CharacterBattleState character)
        {
            if (execution == null)
            {
                throw new ArgumentNullException(nameof(execution));
            }

            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            ValidatePartyCharacter(character);
            if (character.Element != execution.MatchEvent.Element)
            {
                return Array.Empty<CombatAction>();
            }

            var context = new MatchEventActionContext(
                character,
                execution,
                ResolveAttackTag(execution.MatchEvent.Tier),
                party,
                boss,
                actionIds);
            long idBeforeProvider = actionIds.LastIssuedId;
            IReadOnlyList<CombatAction> characterActions =
                provider.CreateRootActions(context);
            ValidateProviderActions(
                characterActions,
                context,
                idBeforeProvider);
            return characterActions;
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
                "Match character must belong to the battle party.");
        }

        private void ValidateProviderActions(
            IReadOnlyList<CombatAction> actions,
            MatchEventActionContext context,
            long idBeforeProvider)
        {
            if (actions == null)
            {
                throw new InvalidOperationException(
                    "Match action providers must return an action collection.");
            }

            long allocatedCount = actionIds.LastIssuedId - idBeforeProvider;
            if (allocatedCount != actions.Count)
            {
                throw new InvalidOperationException(
                    "Match action providers must allocate exactly one "
                        + "battle-scoped ID per returned action.");
            }

            for (int actionIndex = 0;
                actionIndex < actions.Count;
                actionIndex++)
            {
                CombatAction action = actions[actionIndex]
                    ?? throw new InvalidOperationException(
                        "Match action providers cannot return null actions.");
                long expectedActionId = checked(
                    idBeforeProvider + actionIndex + 1);
                bool isRoot = action.RootActionId == action.ActionId
                    && !action.SourceActionId.HasValue;
                bool isDerivedFromEarlierSequenceAction =
                    action.RootActionId > idBeforeProvider
                    && action.RootActionId < action.ActionId
                    && action.SourceActionId.HasValue
                    && action.SourceActionId.Value > idBeforeProvider
                    && action.SourceActionId.Value < action.ActionId;
                if (action.ActionId != expectedActionId
                    || (!isRoot && !isDerivedFromEarlierSequenceAction)
                    || !createdActionIds.Add(action.ActionId))
                {
                    throw new InvalidOperationException(
                        "Match action sequences must preserve provider order, "
                            + "use new battle-scoped IDs, and use only root "
                            + "or earlier in-sequence lineage.");
                }

                ValidateMatchDamageAction(action, context);
            }
        }

        private static void ValidateMatchDamageAction(
            CombatAction action,
            MatchEventActionContext context)
        {
            if (action is BossDamageAction)
            {
                throw new InvalidOperationException(
                    "Match providers cannot create boss-to-party damage.");
            }

            if (!(action is DamageAction damageAction))
            {
                return;
            }

            var request = damageAction.ContextRequest;
            if (damageAction.Origin != ActionOrigin.Match
                || request.AttackType != AttackType.Match
                || !ReferenceEquals(request.Attacker, context.Character)
                || !ReferenceEquals(request.Party, context.Party)
                || !ReferenceEquals(request.TargetBoss, context.Boss)
                || request.AttackElement != context.MatchEvent.Element
                || !request.AppliesCombo
                || request.FinalComboCount != context.FinalComboCount
                || (request.AttackTags & context.MatchAttackTag) == 0)
            {
                throw new InvalidOperationException(
                    "Match damage roots must use the current character, "
                        + "battle, element, tier tag, and final combo count.");
            }
        }

        private static AttackTag ResolveAttackTag(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return AttackTag.Match3;
                case BoardMatchTier.Four:
                    return AttackTag.Match4;
                case BoardMatchTier.FiveOrMore:
                    return AttackTag.Match5Plus;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(tier),
                        tier,
                        "Match tier must be defined.");
            }
        }
    }
}
