using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Data.Database
{
    [CreateAssetMenu(
        fileName = "DefinitionDatabase",
        menuName = "Valor Chronicle/Data/Definition Database")]
    public sealed class DefinitionDatabase : ScriptableObject
    {
        [SerializeField]
        private CharacterDefinition[] characters = Array.Empty<CharacterDefinition>();

        [SerializeField]
        private BossDefinition[] bosses = Array.Empty<BossDefinition>();

        [SerializeField]
        private SkillDefinition[] skills = Array.Empty<SkillDefinition>();

        [SerializeField]
        private EffectDefinition[] effects = Array.Empty<EffectDefinition>();

        [SerializeField]
        private ResourceDefinition[] resources =
            Array.Empty<ResourceDefinition>();

        [SerializeField]
        private RelicDefinition[] relics = Array.Empty<RelicDefinition>();

        [SerializeField]
        private BossPresentationDefinition[] bossPresentations =
            Array.Empty<BossPresentationDefinition>();

        private Dictionary<string, CharacterDefinition> charactersById;
        private Dictionary<string, BossDefinition> bossesById;
        private Dictionary<string, SkillDefinition> skillsById;
        private Dictionary<string, EffectDefinition> effectsById;
        private Dictionary<string, ResourceDefinition> resourcesById;
        private Dictionary<string, RelicDefinition> relicsById;
        private Dictionary<string, BossPresentationDefinition>
            bossPresentationsByBossId;

        public IReadOnlyList<CharacterDefinition> Characters =>
            Array.AsReadOnly(characters ?? Array.Empty<CharacterDefinition>());

        public IReadOnlyList<BossDefinition> Bosses =>
            Array.AsReadOnly(bosses ?? Array.Empty<BossDefinition>());

        public IReadOnlyList<SkillDefinition> Skills =>
            Array.AsReadOnly(skills ?? Array.Empty<SkillDefinition>());

        public IReadOnlyList<EffectDefinition> Effects =>
            Array.AsReadOnly(effects ?? Array.Empty<EffectDefinition>());

        public IReadOnlyList<ResourceDefinition> Resources =>
            Array.AsReadOnly(
                resources ?? Array.Empty<ResourceDefinition>());

        public IReadOnlyList<RelicDefinition> Relics =>
            Array.AsReadOnly(relics ?? Array.Empty<RelicDefinition>());

        public IReadOnlyList<BossPresentationDefinition> BossPresentations =>
            Array.AsReadOnly(
                bossPresentations
                    ?? Array.Empty<BossPresentationDefinition>());

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            Dictionary<string, CharacterDefinition> newCharacters =
                BuildLookup(Characters, nameof(CharacterDefinition));
            Dictionary<string, BossDefinition> newBosses =
                BuildLookup(Bosses, nameof(BossDefinition));
            Dictionary<string, SkillDefinition> newSkills =
                BuildLookup(Skills, nameof(SkillDefinition));
            Dictionary<string, EffectDefinition> newEffects =
                BuildLookup(Effects, nameof(EffectDefinition));
            Dictionary<string, ResourceDefinition> newResources =
                BuildLookup(Resources, nameof(ResourceDefinition));
            Dictionary<string, RelicDefinition> newRelics =
                BuildLookup(Relics, nameof(RelicDefinition));
            Dictionary<string, BossPresentationDefinition>
                newBossPresentations = BuildBossPresentationLookup(
                    BossPresentations);

            charactersById = newCharacters;
            bossesById = newBosses;
            skillsById = newSkills;
            effectsById = newEffects;
            resourcesById = newResources;
            relicsById = newRelics;
            bossPresentationsByBossId = newBossPresentations;
            IsInitialized = true;
        }

        public bool TryGetCharacter(string id, out CharacterDefinition definition)
        {
            EnsureInitialized();
            return TryGet(charactersById, id, out definition);
        }

        public bool TryGetBoss(string id, out BossDefinition definition)
        {
            EnsureInitialized();
            return TryGet(bossesById, id, out definition);
        }

        public bool TryGetSkill(string id, out SkillDefinition definition)
        {
            EnsureInitialized();
            return TryGet(skillsById, id, out definition);
        }

        public bool TryGetRelic(string id, out RelicDefinition definition)
        {
            EnsureInitialized();
            return TryGet(relicsById, id, out definition);
        }

        public bool TryGetEffect(string id, out EffectDefinition definition)
        {
            EnsureInitialized();
            return TryGet(effectsById, id, out definition);
        }

        public bool TryGetResource(
            string id,
            out ResourceDefinition definition)
        {
            EnsureInitialized();
            return TryGet(resourcesById, id, out definition);
        }

        public bool TryGetBossPresentation(
            string bossId,
            out BossPresentationDefinition definition)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(bossId))
            {
                definition = null;
                return false;
            }

            return bossPresentationsByBossId.TryGetValue(
                bossId,
                out definition);
        }

        private static Dictionary<string, TDefinition> BuildLookup<TDefinition>(
            IReadOnlyList<TDefinition> definitions,
            string definitionTypeName)
            where TDefinition : GameDefinition
        {
            var lookup = new Dictionary<string, TDefinition>(
                definitions.Count,
                StringComparer.Ordinal);

            for (int i = 0; i < definitions.Count; i++)
            {
                TDefinition definition = definitions[i];

                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot initialize {definitionTypeName} lookup: entry {i} is null.");
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"Cannot initialize {definitionTypeName} lookup: entry {i} has no ID.");
                }

                if (lookup.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"Cannot initialize {definitionTypeName} lookup: duplicate ID '{definition.Id}'.");
                }

                lookup.Add(definition.Id, definition);
            }

            return lookup;
        }

        private static bool TryGet<TDefinition>(
            IReadOnlyDictionary<string, TDefinition> lookup,
            string id,
            out TDefinition definition)
            where TDefinition : GameDefinition
        {
            if (string.IsNullOrEmpty(id))
            {
                definition = null;
                return false;
            }

            return lookup.TryGetValue(id, out definition);
        }

        private static Dictionary<string, BossPresentationDefinition>
            BuildBossPresentationLookup(
                IReadOnlyList<BossPresentationDefinition> definitions)
        {
            var lookup =
                new Dictionary<string, BossPresentationDefinition>(
                    definitions.Count,
                    StringComparer.Ordinal);

            for (int index = 0; index < definitions.Count; index++)
            {
                BossPresentationDefinition definition = definitions[index];
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        "Cannot initialize BossPresentationDefinition "
                            + $"lookup: entry {index} is null.");
                }

                if (string.IsNullOrEmpty(definition.BossId))
                {
                    throw new InvalidOperationException(
                        "Cannot initialize BossPresentationDefinition "
                            + $"lookup: entry {index} has no Boss ID.");
                }

                if (lookup.ContainsKey(definition.BossId))
                {
                    throw new InvalidOperationException(
                        "Cannot initialize BossPresentationDefinition "
                            + $"lookup: duplicate Boss ID "
                            + $"'{definition.BossId}'.");
                }

                lookup.Add(definition.BossId, definition);
            }

            return lookup;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "DefinitionDatabase must be initialized before content lookup.");
            }
        }
    }
}
