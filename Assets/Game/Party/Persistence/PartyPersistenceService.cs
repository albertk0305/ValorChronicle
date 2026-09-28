using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Party.Persistence
{
    /// <summary>
    /// Applies party changes through the profile transaction boundary.
    /// </summary>
    public sealed class PartyPersistenceService
    {
        private readonly object syncRoot = new object();
        private readonly SaveService saveService;
        private readonly PartyPresetEditor presetEditor;

        public PartyPersistenceService(
            SaveService saveService,
            PartyPresetEditor presetEditor)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
            this.presetEditor = presetEditor
                ?? throw new ArgumentNullException(nameof(presetEditor));
        }

        public PartyPersistenceOperationResult SetActivePreset(
            int presetIndex)
        {
            ValidatePresetIndex(presetIndex);

            lock (syncRoot)
            {
                if (!TryGetCurrentSnapshot(
                    out ProfileSaveData current))
                {
                    return Failed(saveTransactionResult: null);
                }

                if (current.Party.ActivePresetIndex == presetIndex)
                {
                    return NoChange(current, editResult: null);
                }

                SaveTransactionResult transaction =
                    saveService.ExecuteTransaction(profile =>
                    {
                        profile.Party.ActivePresetIndex = presetIndex;
                    });

                return CompleteChangedOperation(
                    transaction,
                    editResult: null);
            }
        }

        public PartyPersistenceOperationResult ConfirmEdit(
            int presetIndex,
            int targetSlotIndex,
            string previewCharacterId)
        {
            ValidatePresetIndex(presetIndex);
            PartyPresetEditor.ValidateSlotIndex(
                targetSlotIndex,
                nameof(targetSlotIndex));
            if (previewCharacterId == null)
            {
                throw new ArgumentNullException(
                    nameof(previewCharacterId));
            }

            lock (syncRoot)
            {
                if (!TryGetCurrentSnapshot(
                    out ProfileSaveData current))
                {
                    return Failed(saveTransactionResult: null);
                }

                if (previewCharacterId != SaveRules.EmptyId
                    && !IsOwnedCharacter(
                        current.Characters,
                        previewCharacterId))
                {
                    return Failed(saveTransactionResult: null);
                }

                PartyPresetEditResult currentEdit = presetEditor.Edit(
                    GetPresetSlots(current, presetIndex),
                    targetSlotIndex,
                    previewCharacterId);
                if (currentEdit.OperationType
                    == PartyPresetEditOperationType.NoChange)
                {
                    return NoChange(current, currentEdit);
                }

                PartyPresetEditResult appliedEdit = null;
                SaveTransactionResult transaction =
                    saveService.ExecuteTransaction(profile =>
                    {
                        if (previewCharacterId != SaveRules.EmptyId
                            && !IsOwnedCharacter(
                                profile.Characters,
                                previewCharacterId))
                        {
                            throw new InvalidOperationException(
                                "The selected party character is not owned.");
                        }

                        PartyPresetSaveData preset =
                            GetPreset(profile, presetIndex);
                        appliedEdit = presetEditor.Edit(
                            preset.CharacterSlotIds,
                            targetSlotIndex,
                            previewCharacterId);
                        preset.CharacterSlotIds = new List<string>(
                            appliedEdit.ResultingSlots);
                    });

                return CompleteChangedOperation(
                    transaction,
                    appliedEdit ?? currentEdit);
            }
        }

        public PartyPersistenceOperationResult ClearSlot(
            int presetIndex,
            int slotIndex)
        {
            ValidatePresetIndex(presetIndex);
            PartyPresetEditor.ValidateSlotIndex(
                slotIndex,
                nameof(slotIndex));

            lock (syncRoot)
            {
                if (!TryGetCurrentSnapshot(
                    out ProfileSaveData current))
                {
                    return Failed(saveTransactionResult: null);
                }

                PartyPresetEditResult currentEdit = presetEditor.Clear(
                    GetPresetSlots(current, presetIndex),
                    slotIndex);
                if (currentEdit.OperationType
                    == PartyPresetEditOperationType.NoChange)
                {
                    return NoChange(current, currentEdit);
                }

                PartyPresetEditResult appliedEdit = null;
                SaveTransactionResult transaction =
                    saveService.ExecuteTransaction(profile =>
                    {
                        PartyPresetSaveData preset =
                            GetPreset(profile, presetIndex);
                        appliedEdit = presetEditor.Clear(
                            preset.CharacterSlotIds,
                            slotIndex);
                        preset.CharacterSlotIds = new List<string>(
                            appliedEdit.ResultingSlots);
                    });

                return CompleteChangedOperation(
                    transaction,
                    appliedEdit ?? currentEdit);
            }
        }

        private PartyPersistenceOperationResult CompleteChangedOperation(
            SaveTransactionResult transaction,
            PartyPresetEditResult editResult)
        {
            if (transaction == null || !transaction.IsSuccess)
            {
                return Failed(transaction, editResult);
            }

            return new PartyPersistenceOperationResult(
                PartyPersistenceStatus.ChangedAndSaved,
                saveService.GetCurrentProfileSnapshot(),
                editResult,
                transaction);
        }

        private bool TryGetCurrentSnapshot(
            out ProfileSaveData profile)
        {
            profile = null;
            if (!saveService.HasCurrentProfile)
            {
                return false;
            }

            try
            {
                profile = saveService.GetCurrentProfileSnapshot();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static IReadOnlyList<string> GetPresetSlots(
            ProfileSaveData profile,
            int presetIndex)
        {
            return GetPreset(profile, presetIndex).CharacterSlotIds;
        }

        private static PartyPresetSaveData GetPreset(
            ProfileSaveData profile,
            int presetIndex)
        {
            if (profile?.Party?.Presets == null
                || profile.Party.Presets.Count
                    != SaveRules.PartyPresetCount
                || profile.Party.Presets[presetIndex] == null)
            {
                throw new InvalidOperationException(
                    "The current profile does not contain a valid party.");
            }

            return profile.Party.Presets[presetIndex];
        }

        private static bool IsOwnedCharacter(
            IReadOnlyList<CharacterSaveData> characters,
            string characterId)
        {
            if (characters == null)
            {
                return false;
            }

            for (int index = 0; index < characters.Count; index++)
            {
                if (string.Equals(
                    characters[index]?.CharacterId,
                    characterId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidatePresetIndex(int presetIndex)
        {
            if (presetIndex < 0
                || presetIndex >= SaveRules.PartyPresetCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presetIndex),
                    presetIndex,
                    $"Party preset index must be between 0 and "
                        + $"{SaveRules.PartyPresetCount - 1}.");
            }
        }

        private static PartyPersistenceOperationResult NoChange(
            ProfileSaveData profile,
            PartyPresetEditResult editResult)
        {
            return new PartyPersistenceOperationResult(
                PartyPersistenceStatus.NoChange,
                profile,
                editResult,
                saveTransactionResult: null);
        }

        private static PartyPersistenceOperationResult Failed(
            SaveTransactionResult saveTransactionResult,
            PartyPresetEditResult editResult = null)
        {
            return new PartyPersistenceOperationResult(
                PartyPersistenceStatus.Failed,
                profileSnapshot: null,
                editResult,
                saveTransactionResult);
        }
    }
}
