using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Editor.SaveDebug
{
    public sealed class SaveDebugWindow : EditorWindow
    {
        private const string MenuPath =
            "Tools/Valor Chronicle/Save Debug";

        private readonly Dictionary<SaveDebugCurrency, string> setInputs =
            new Dictionary<SaveDebugCurrency, string>();
        private readonly Dictionary<SaveDebugCurrency, string> addInputs =
            new Dictionary<SaveDebugCurrency, string>();
        private readonly Dictionary<string, string> levelInputs =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> awakeningInputs =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private Vector2 scrollPosition;
        private ProfileSaveData snapshot;
        private DefinitionDatabase definitionDatabase;
        private SaveDebugProfileService debugService;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.Info;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            GetWindow<SaveDebugWindow>("Save Debug");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        private void OnGUI()
        {
            DrawHeader();

            if (snapshot == null || debugService == null)
            {
                EditorGUILayout.HelpBox(
                    statusMessage,
                    string.IsNullOrEmpty(statusMessage)
                        ? MessageType.Info
                        : statusType);
                return;
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawProfile();
            DrawCurrencies();
            DrawCharacters();
            DrawParty();
            DrawPresets();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Valor Chronicle Save Debug", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Editor-only tool. All mutations use the current production "
                + "SaveService transaction path.",
                MessageType.Info);

            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }
        }

        private void DrawProfile()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Save Version", snapshot.SaveVersion.ToString());
            EditorGUILayout.SelectableLabel(
                Path.Combine(Application.persistentDataPath, "profile.save"),
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private void DrawCurrencies()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Currencies", EditorStyles.boldLabel);

            DrawCurrency(
                SaveDebugCurrency.GachaCurrency,
                snapshot.Currencies.GachaCurrency);
            DrawCurrency(
                SaveDebugCurrency.BattleRecords,
                snapshot.Currencies.BattleRecords);
            DrawCurrency(
                SaveDebugCurrency.HeroTokens,
                snapshot.Currencies.HeroTokens);
            DrawCurrency(
                SaveDebugCurrency.RelicTokens,
                snapshot.Currencies.RelicTokens);
        }

        private void DrawCurrency(SaveDebugCurrency currency, long currentValue)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(currency.ToString(), currentValue.ToString());

            EditorGUILayout.BeginHorizontal();
            setInputs[currency] = EditorGUILayout.TextField(
                "Set",
                GetOrCreate(setInputs, currency, currentValue.ToString()));
            if (GUILayout.Button("Apply", GUILayout.Width(70f)))
            {
                if (!long.TryParse(setInputs[currency], out long value))
                {
                    ShowInputError("Set value must be a valid Int64 value.");
                }
                else
                {
                    Run(() => debugService.SetCurrency(currency, value));
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            addInputs[currency] = EditorGUILayout.TextField(
                "Add",
                GetOrCreate(addInputs, currency, "0"));
            if (GUILayout.Button("Apply", GUILayout.Width(70f)))
            {
                if (!long.TryParse(addInputs[currency], out long amount))
                {
                    ShowInputError("Add value must be a valid Int64 value.");
                }
                else
                {
                    Run(() => debugService.AddCurrency(currency, amount));
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCharacters()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);

            IReadOnlyList<CharacterDefinition> definitions =
                definitionDatabase.Characters;
            if (definitions.Count == 0)
            {
                EditorGUILayout.LabelField("No registered characters.");
                return;
            }

            for (int index = 0; index < definitions.Count; index++)
            {
                CharacterDefinition definition = definitions[index];
                if (definition == null)
                {
                    continue;
                }

                CharacterSaveData owned = FindCharacter(definition.Id);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(definition.Id, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "Display Name Key",
                    definition.DisplayNameKey ?? string.Empty);
                EditorGUILayout.LabelField("Owned", (owned != null).ToString());

                if (owned == null)
                {
                    if (GUILayout.Button("Grant"))
                    {
                        string characterId = definition.Id;
                        Run(() => debugService.GrantCharacter(characterId));
                    }
                }
                else
                {
                    DrawOwnedCharacter(definition.Id, owned);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawOwnedCharacter(
            string characterId,
            CharacterSaveData character)
        {
            EditorGUILayout.BeginHorizontal();
            levelInputs[characterId] = EditorGUILayout.TextField(
                "Level",
                GetOrCreate(
                    levelInputs,
                    characterId,
                    character.Level.ToString()));
            if (GUILayout.Button("Set", GUILayout.Width(70f)))
            {
                if (!int.TryParse(levelInputs[characterId], out int level))
                {
                    ShowInputError("Level must be a valid Int32 value.");
                }
                else
                {
                    Run(() => debugService.SetCharacterLevel(characterId, level));
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            awakeningInputs[characterId] = EditorGUILayout.TextField(
                "Awakening",
                GetOrCreate(
                    awakeningInputs,
                    characterId,
                    character.Awakening.ToString()));
            if (GUILayout.Button("Set", GUILayout.Width(70f)))
            {
                if (!int.TryParse(
                    awakeningInputs[characterId],
                    out int awakening))
                {
                    ShowInputError("Awakening must be a valid Int32 value.");
                }
                else
                {
                    Run(() => debugService.SetCharacterAwakening(
                        characterId,
                        awakening));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawParty()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Party", EditorStyles.boldLabel);
            PartySaveData party = snapshot.Party;
            EditorGUILayout.LabelField(
                "Active Preset Index",
                party.ActivePresetIndex.ToString());

            if (party.Presets == null
                || party.ActivePresetIndex < 0
                || party.ActivePresetIndex >= party.Presets.Count)
            {
                EditorGUILayout.HelpBox(
                    "The active party preset is unavailable.",
                    MessageType.Warning);
                return;
            }

            PartyPresetSaveData preset = party.Presets[party.ActivePresetIndex];
            EditorGUILayout.LabelField(
                "Preset ID",
                preset?.PresetId ?? string.Empty);
            for (int slot = 0; slot < SaveRules.PartySlotCount; slot++)
            {
                string characterId = preset?.CharacterSlotIds != null
                    && slot < preset.CharacterSlotIds.Count
                    ? preset.CharacterSlotIds[slot]
                    : string.Empty;
                EditorGUILayout.LabelField(
                    $"Slot {slot + 1}",
                    string.IsNullOrEmpty(characterId) ? "(Empty)" : characterId);
            }
        }

        private void DrawPresets()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stage 10 Presets", EditorStyles.boldLabel);

            DrawPresetButton("Stage10 - Fresh Marea", Stage10SavePreset.FreshMarea);
            DrawPresetButton("Stage10 - Insufficient", Stage10SavePreset.Insufficient);
            DrawPresetButton("Stage10 - Lv99 Boundary", Stage10SavePreset.Level99Boundary);
            DrawPresetButton("Stage10 - Max Awakening", Stage10SavePreset.MaxAwakening);
        }

        private void DrawPresetButton(string label, Stage10SavePreset preset)
        {
            if (GUILayout.Button(label))
            {
                Run(() => debugService.ApplyStage10Preset(preset));
            }
        }

        private void Run(Func<SaveDebugOperationResult> operation)
        {
            SaveDebugOperationResult result;
            try
            {
                result = operation();
            }
            catch (Exception exception)
            {
                statusMessage = $"Operation failed: {exception.Message}";
                statusType = MessageType.Error;
                Repaint();
                return;
            }

            statusMessage = result.Message;
            statusType = result.IsSuccess
                ? MessageType.Info
                : result.Status == SaveDebugOperationStatus.NoChange
                    ? MessageType.Warning
                    : MessageType.Error;

            if (result.IsSuccess)
            {
                Refresh(preserveStatus: true);
            }
            else
            {
                Repaint();
            }
        }

        private void Refresh(bool preserveStatus = false)
        {
            if (!preserveStatus)
            {
                statusMessage = string.Empty;
                statusType = MessageType.Info;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (!EditorApplication.isPlaying
                || bootstrapper == null
                || bootstrapper.SaveService == null
                || !bootstrapper.SaveService.HasCurrentProfile)
            {
                snapshot = null;
                definitionDatabase = null;
                debugService = null;
                if (!preserveStatus || string.IsNullOrEmpty(statusMessage))
                {
                    statusMessage =
                        "Enter Play Mode and wait for GameBootstrapper to load "
                        + "the current profile, then press Refresh.";
                    statusType = MessageType.Info;
                }
                Repaint();
                return;
            }

            definitionDatabase = bootstrapper.DefinitionDatabase;
            if (definitionDatabase == null)
            {
                snapshot = null;
                debugService = null;
                statusMessage = "GameBootstrapper has no DefinitionDatabase.";
                statusType = MessageType.Error;
                Repaint();
                return;
            }

            snapshot = bootstrapper.SaveService.GetCurrentProfileSnapshot();
            debugService = new SaveDebugProfileService(
                bootstrapper.SaveService,
                definitionDatabase.Characters
                    .Where(definition => definition != null)
                    .Select(definition => definition.Id));
            ResetInputsFromSnapshot();
            Repaint();
        }

        private void ResetInputsFromSnapshot()
        {
            setInputs[SaveDebugCurrency.GachaCurrency] =
                snapshot.Currencies.GachaCurrency.ToString();
            setInputs[SaveDebugCurrency.BattleRecords] =
                snapshot.Currencies.BattleRecords.ToString();
            setInputs[SaveDebugCurrency.HeroTokens] =
                snapshot.Currencies.HeroTokens.ToString();
            setInputs[SaveDebugCurrency.RelicTokens] =
                snapshot.Currencies.RelicTokens.ToString();

            foreach (SaveDebugCurrency currency in
                Enum.GetValues(typeof(SaveDebugCurrency)))
            {
                addInputs[currency] = "0";
            }

            levelInputs.Clear();
            awakeningInputs.Clear();
            if (snapshot.Characters == null)
            {
                return;
            }

            for (int index = 0; index < snapshot.Characters.Count; index++)
            {
                CharacterSaveData character = snapshot.Characters[index];
                if (character == null || string.IsNullOrEmpty(character.CharacterId))
                {
                    continue;
                }

                levelInputs[character.CharacterId] = character.Level.ToString();
                awakeningInputs[character.CharacterId] =
                    character.Awakening.ToString();
            }
        }

        private CharacterSaveData FindCharacter(string characterId)
        {
            if (snapshot.Characters == null)
            {
                return null;
            }

            return snapshot.Characters.Find(character =>
                character != null
                && string.Equals(
                    character.CharacterId,
                    characterId,
                    StringComparison.Ordinal));
        }

        private void ShowInputError(string message)
        {
            statusMessage = message;
            statusType = MessageType.Error;
            Repaint();
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode
                || state == PlayModeStateChange.EnteredPlayMode)
            {
                Refresh();
            }
        }

        private static string GetOrCreate<TKey>(
            IDictionary<TKey, string> values,
            TKey key,
            string defaultValue)
        {
            if (!values.TryGetValue(key, out string value))
            {
                value = defaultValue;
                values[key] = value;
            }

            return value;
        }
    }
}
