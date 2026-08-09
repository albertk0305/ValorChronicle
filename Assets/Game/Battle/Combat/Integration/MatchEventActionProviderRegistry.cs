using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class MatchEventActionProviderRegistry
        : IMatchEventActionProvider
    {
        private readonly Dictionary<string, IMatchEventActionProvider>
            providers = new Dictionary<string, IMatchEventActionProvider>(
                StringComparer.Ordinal);

        public int Count => providers.Count;

        public void Register(
            string characterId,
            IMatchEventActionProvider provider)
        {
            ValidateId(characterId, nameof(characterId));
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (providers.ContainsKey(characterId))
            {
                throw new InvalidOperationException(
                    $"A match provider is already registered for "
                        + $"character '{characterId}'.");
            }

            providers.Add(characterId, provider);
        }

        public bool TryResolve(
            string characterId,
            out IMatchEventActionProvider provider)
        {
            ValidateId(characterId, nameof(characterId));
            return providers.TryGetValue(characterId, out provider);
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return providers.TryGetValue(
                context.Character.CharacterId,
                out IMatchEventActionProvider provider)
                    ? provider.CreateRootActions(context)
                    : Array.Empty<CombatAction>();
        }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "ID cannot be null or whitespace.",
                    parameterName);
            }
        }
    }
}
