using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Editor.Validation
{
    public static class CharacterContentRegistrationValidator
    {
        public static IReadOnlyList<string> Validate(
            DefinitionDatabase database,
            CharacterPresentationCatalog presentationCatalog,
            CharacterRuntimeRegistrationCatalog runtimeCatalog)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (presentationCatalog == null)
            {
                throw new ArgumentNullException(
                    nameof(presentationCatalog));
            }

            if (runtimeCatalog == null)
            {
                throw new ArgumentNullException(nameof(runtimeCatalog));
            }

            var issues = new List<string>();
            Dictionary<string, CharacterDefinition> definitions =
                CollectDefinitions(database, issues);
            Dictionary<string, SkillDefinition> skills = CollectSkills(
                database);
            Dictionary<string, CharacterRuntimeRegistration> registrations =
                CollectRegistrations(runtimeCatalog, issues);
            Dictionary<string, CharacterPresentationDefinition>
                presentations = CollectPresentations(
                    presentationCatalog,
                    definitions,
                    issues);
            Dictionary<string, string> acquisitionSources =
                CollectAcquisitionSources(database);

            var playableCharacterIds = new HashSet<string>(
                registrations.Keys,
                StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> acquisition in
                acquisitionSources)
            {
                playableCharacterIds.Add(acquisition.Key);
                if (!registrations.ContainsKey(acquisition.Key))
                {
                    issues.Add(
                        $"Character '{acquisition.Key}' is obtainable from "
                            + $"{acquisition.Value}, but has no default "
                            + "RuntimeRegistration.");
                }
            }

            foreach (CharacterRuntimeRegistration registration in
                runtimeCatalog.Registrations)
            {
                ValidateRegistrationFactories(registration, issues);
                if (!definitions.ContainsKey(registration.CharacterId))
                {
                    issues.Add(
                        $"RuntimeRegistration character "
                            + $"'{registration.CharacterId}' has no "
                            + "CharacterDefinition in DefinitionDatabase "
                            + $"'{AssetPath(database)}'.");
                }
            }

            foreach (string characterId in playableCharacterIds)
            {
                if (!definitions.TryGetValue(
                        characterId,
                        out CharacterDefinition definition))
                {
                    if (acquisitionSources.TryGetValue(
                            characterId,
                            out string source))
                    {
                        issues.Add(
                            $"Obtainable character '{characterId}' from "
                                + $"{source} has no CharacterDefinition in "
                                + $"DefinitionDatabase '{AssetPath(database)}'.");
                    }

                    continue;
                }

                registrations.TryGetValue(
                    characterId,
                    out CharacterRuntimeRegistration registration);
                ValidateCombatConfig(
                    definition,
                    registration,
                    issues);
                ValidateSkills(definition, skills, database, issues);
                ValidatePresentation(
                    definition,
                    presentations,
                    presentationCatalog,
                    issues);
            }

            return issues.AsReadOnly();
        }

        private static Dictionary<string, CharacterDefinition>
            CollectDefinitions(
                DefinitionDatabase database,
                ICollection<string> issues)
        {
            var result = new Dictionary<string, CharacterDefinition>(
                StringComparer.Ordinal);
            for (int index = 0; index < database.Characters.Count; index++)
            {
                CharacterDefinition definition = database.Characters[index];
                if (definition == null || string.IsNullOrWhiteSpace(
                        definition.Id))
                {
                    continue;
                }

                if (!result.TryAdd(definition.Id, definition))
                {
                    issues.Add(
                        $"Duplicate CharacterDefinition ID "
                            + $"'{definition.Id}' in DefinitionDatabase "
                            + $"'{AssetPath(database)}'.");
                }
            }

            return result;
        }

        private static Dictionary<string, SkillDefinition> CollectSkills(
            DefinitionDatabase database)
        {
            var result = new Dictionary<string, SkillDefinition>(
                StringComparer.Ordinal);
            for (int index = 0; index < database.Skills.Count; index++)
            {
                SkillDefinition definition = database.Skills[index];
                if (definition != null
                    && !string.IsNullOrWhiteSpace(definition.Id))
                {
                    result.TryAdd(definition.Id, definition);
                }
            }

            return result;
        }

        private static Dictionary<string, CharacterRuntimeRegistration>
            CollectRegistrations(
                CharacterRuntimeRegistrationCatalog catalog,
                ICollection<string> issues)
        {
            var result = new Dictionary<
                string,
                CharacterRuntimeRegistration>(StringComparer.Ordinal);
            for (int index = 0; index < catalog.Registrations.Count; index++)
            {
                CharacterRuntimeRegistration registration =
                    catalog.Registrations[index];
                if (!result.TryAdd(registration.CharacterId, registration))
                {
                    issues.Add(
                        $"Duplicate RuntimeRegistration character ID "
                            + $"'{registration.CharacterId}'.");
                }
            }

            return result;
        }

        private static Dictionary<
                string,
                CharacterPresentationDefinition>
            CollectPresentations(
                CharacterPresentationCatalog catalog,
                IReadOnlyDictionary<string, CharacterDefinition> definitions,
                ICollection<string> issues)
        {
            var result = new Dictionary<
                string,
                CharacterPresentationDefinition>(StringComparer.Ordinal);
            for (int index = 0; index < catalog.Definitions.Count; index++)
            {
                CharacterPresentationDefinition presentation =
                    catalog.Definitions[index];
                if (presentation == null)
                {
                    issues.Add(
                        $"CharacterPresentationCatalog "
                            + $"'{AssetPath(catalog)}' has a broken/null "
                            + $"entry at index {index}.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(presentation.CharacterId))
                {
                    issues.Add(
                        $"CharacterPresentationDefinition "
                            + $"'{AssetPath(presentation)}' has no "
                            + "CharacterId.");
                    continue;
                }

                if (!result.TryAdd(
                        presentation.CharacterId,
                        presentation))
                {
                    issues.Add(
                        $"Duplicate CharacterPresentationDefinition ID "
                            + $"'{presentation.CharacterId}' in catalog "
                            + $"'{AssetPath(catalog)}'.");
                }

                if (!definitions.ContainsKey(presentation.CharacterId))
                {
                    issues.Add(
                        $"CharacterPresentationDefinition "
                            + $"'{AssetPath(presentation)}' references "
                            + $"unknown CharacterId "
                            + $"'{presentation.CharacterId}'.");
                }
            }

            return result;
        }

        private static Dictionary<string, string> CollectAcquisitionSources(
            DefinitionDatabase database)
        {
            var sources = new Dictionary<string, string>(
                StringComparer.Ordinal);
            for (int gachaIndex = 0;
                gachaIndex < database.Gachas.Count;
                gachaIndex++)
            {
                GachaDefinition gacha = database.Gachas[gachaIndex];
                if (gacha == null)
                {
                    continue;
                }

                for (int poolIndex = 0;
                    poolIndex < gacha.CharacterPoolIds.Count;
                    poolIndex++)
                {
                    string characterId = gacha.CharacterPoolIds[poolIndex];
                    AddAcquisitionSource(
                        sources,
                        characterId,
                        $"GachaDefinition '{AssetPath(gacha)}'");
                }
            }

            ExchangeCatalogDefinition exchange = database.ExchangeCatalog;
            if (exchange != null)
            {
                for (int index = 0;
                    index < exchange.CharacterIds.Count;
                    index++)
                {
                    AddAcquisitionSource(
                        sources,
                        exchange.CharacterIds[index],
                        $"ExchangeCatalogDefinition "
                            + $"'{AssetPath(exchange)}'");
                }
            }

            return sources;
        }

        private static void AddAcquisitionSource(
            IDictionary<string, string> sources,
            string characterId,
            string source)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            if (sources.TryGetValue(characterId, out string existing))
            {
                if (existing.IndexOf(source, StringComparison.Ordinal) < 0)
                {
                    sources[characterId] = existing + " and " + source;
                }

                return;
            }

            sources.Add(characterId, source);
        }

        private static void ValidateRegistrationFactories(
            CharacterRuntimeRegistration registration,
            ICollection<string> issues)
        {
            try
            {
                registration.CreateBuildModifier();
                registration.CreateCombatRegistrar();
                registration.CreateLookupProvider();
            }
            catch (Exception exception)
            {
                issues.Add(
                    $"RuntimeRegistration character "
                        + $"'{registration.CharacterId}' cannot create all "
                        + $"runtime components. {exception.Message}");
            }
        }

        private static void ValidateCombatConfig(
            CharacterDefinition definition,
            CharacterRuntimeRegistration registration,
            ICollection<string> issues)
        {
            if (definition.CombatConfig == null)
            {
                issues.Add(
                    $"Playable character '{definition.Id}' has no "
                        + "CombatConfig. CharacterDefinition: "
                        + $"'{AssetPath(definition)}'.");
                return;
            }

            if (registration == null)
            {
                return;
            }

            Type actualType = definition.CombatConfig.GetType();
            Type expectedType = registration.SupportedCombatConfigType;
            if (actualType != expectedType)
            {
                issues.Add(
                    $"Playable character '{definition.Id}' has mismatched "
                        + $"CombatConfig type in "
                        + $"'{AssetPath(definition)}'. Expected "
                        + $"'{expectedType.FullName}', actual "
                        + $"'{actualType.FullName}' from "
                        + $"'{AssetPath(definition.CombatConfig)}'.");
            }
        }

        private static void ValidateSkills(
            CharacterDefinition definition,
            IReadOnlyDictionary<string, SkillDefinition> skills,
            DefinitionDatabase database,
            ICollection<string> issues)
        {
            if (definition.SkillIds.Count == 0)
            {
                issues.Add(
                    $"Playable character '{definition.Id}' has no skill "
                        + $"references in CharacterDefinition "
                        + $"'{AssetPath(definition)}'.");
                return;
            }

            for (int index = 0; index < definition.SkillIds.Count; index++)
            {
                string skillId = definition.SkillIds[index];
                if (string.IsNullOrWhiteSpace(skillId)
                    || !skills.ContainsKey(skillId))
                {
                    issues.Add(
                        $"Playable character '{definition.Id}' references "
                            + $"missing SkillDefinition "
                            + $"'{skillId ?? "<null>"}' at index {index}. "
                            + $"CharacterDefinition: "
                            + $"'{AssetPath(definition)}'; database: "
                            + $"'{AssetPath(database)}'.");
                }
            }
        }

        private static void ValidatePresentation(
            CharacterDefinition definition,
            IReadOnlyDictionary<
                string,
                CharacterPresentationDefinition> presentations,
            CharacterPresentationCatalog catalog,
            ICollection<string> issues)
        {
            if (!presentations.TryGetValue(
                    definition.Id,
                    out CharacterPresentationDefinition presentation))
            {
                issues.Add(
                    $"Playable character '{definition.Id}' has no "
                        + "CharacterPresentationDefinition in catalog "
                        + $"'{AssetPath(catalog)}'. CharacterDefinition: "
                        + $"'{AssetPath(definition)}'.");
                return;
            }

            if (presentation.FaceSprite == null)
            {
                issues.Add(
                    $"Playable character '{definition.Id}' has no "
                        + $"FaceSprite in presentation "
                        + $"'{AssetPath(presentation)}'.");
            }
        }

        private static string AssetPath(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "<null>";
            }

            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path)
                ? $"<in-memory:{asset.GetType().Name}>"
                : path;
        }
    }
}
