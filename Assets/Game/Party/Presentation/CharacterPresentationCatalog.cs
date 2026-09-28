using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Party.Presentation
{
    [CreateAssetMenu(
        fileName = "CharacterPresentationCatalog",
        menuName = "Valor Chronicle/Party/Character Presentation Catalog")]
    public sealed class CharacterPresentationCatalog : ScriptableObject
    {
        [SerializeField]
        private CharacterPresentationDefinition[] definitions =
            Array.Empty<CharacterPresentationDefinition>();

        private Dictionary<string, CharacterPresentationDefinition>
            definitionsById;

        public IReadOnlyList<CharacterPresentationDefinition> Definitions =>
            Array.AsReadOnly(
                definitions
                    ?? Array.Empty<CharacterPresentationDefinition>());

        public bool IsInitialized => definitionsById != null;

        public void Initialize()
        {
            var lookup =
                new Dictionary<string, CharacterPresentationDefinition>(
                    Definitions.Count,
                    StringComparer.Ordinal);
            for (int index = 0; index < Definitions.Count; index++)
            {
                CharacterPresentationDefinition definition =
                    Definitions[index];
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"Character presentation entry {index} is null.");
                }

                if (string.IsNullOrEmpty(definition.CharacterId))
                {
                    throw new InvalidOperationException(
                        $"Character presentation entry {index} has no ID.");
                }

                if (!lookup.TryAdd(
                    definition.CharacterId,
                    definition))
                {
                    throw new InvalidOperationException(
                        "Duplicate character presentation ID: "
                            + definition.CharacterId);
                }
            }

            definitionsById = lookup;
        }

        public bool TryGet(
            string characterId,
            out CharacterPresentationDefinition definition)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "CharacterPresentationCatalog must be initialized.");
            }

            if (string.IsNullOrEmpty(characterId))
            {
                definition = null;
                return false;
            }

            return definitionsById.TryGetValue(
                characterId,
                out definition);
        }

        private void OnValidate()
        {
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                definitionsById = null;
                GameLogger.Warning(
                    $"[CharacterPresentationCatalog] {exception.Message}",
                    this);
            }
        }
    }
}
