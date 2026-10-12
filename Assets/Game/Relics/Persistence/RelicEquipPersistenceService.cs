using System;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Relics.Persistence
{
    /// <summary>
    /// Revalidates and persists relic equip commands against the latest profile.
    /// </summary>
    public sealed class RelicEquipPersistenceService
    {
        private readonly object syncRoot = new object();
        private readonly SaveService saveService;
        private readonly RelicEquipEditor editor;

        public RelicEquipPersistenceService(
            SaveService saveService,
            RelicEquipEditor editor)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
            this.editor = editor
                ?? throw new ArgumentNullException(nameof(editor));
        }

        public RelicEquipPersistenceOperationResult Execute(
            RelicEquipSessionCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!Enum.IsDefined(
                    typeof(RelicEquipSessionCommandType),
                    command.CommandType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(command),
                    command.CommandType,
                    "The relic equip command type is invalid.");
            }

            lock (syncRoot)
            {
                if (!TryGetCurrentSnapshot(
                        out ProfileSaveData current))
                {
                    return CreateResult(
                        RelicEquipPersistenceStatus.PersistenceFailed,
                        profileSnapshot: null,
                        editResult: null,
                        saveTransactionResult: null);
                }

                OperationEvaluation currentEvaluation = Evaluate(
                    current,
                    command);
                if (currentEvaluation.Status
                    != RelicEquipPersistenceStatus.ChangedAndSaved)
                {
                    return CreateResult(
                        currentEvaluation.Status,
                        currentEvaluation.Status
                            == RelicEquipPersistenceStatus.NoChange
                                ? current
                                : null,
                        currentEvaluation.EditResult,
                        saveTransactionResult: null);
                }

                OperationEvaluation appliedEvaluation = null;
                SaveTransactionResult transaction =
                    saveService.ExecuteTransaction(profile =>
                    {
                        appliedEvaluation = Evaluate(profile, command);
                        if (appliedEvaluation.Status
                            != RelicEquipPersistenceStatus
                                .ChangedAndSaved)
                        {
                            throw new CommandRejectedException(
                                appliedEvaluation);
                        }

                        Apply(profile, command, appliedEvaluation.EditResult);
                    });

                if (transaction.IsSuccess)
                {
                    return CreateResult(
                        RelicEquipPersistenceStatus.ChangedAndSaved,
                        saveService.GetCurrentProfileSnapshot(),
                        appliedEvaluation.EditResult,
                        transaction);
                }

                if (transaction.Exception
                    is CommandRejectedException rejected)
                {
                    return CreateResult(
                        rejected.Evaluation.Status,
                        profileSnapshot: null,
                        rejected.Evaluation.EditResult,
                        transaction);
                }

                RelicEquipPersistenceStatus failureStatus =
                    transaction.Status == SaveTransactionStatus.SaveFailed
                        ? RelicEquipPersistenceStatus.SaveFailed
                        : RelicEquipPersistenceStatus.PersistenceFailed;
                return CreateResult(
                    failureStatus,
                    profileSnapshot: null,
                    appliedEvaluation?.EditResult
                        ?? currentEvaluation.EditResult,
                    transaction);
            }
        }

        private OperationEvaluation Evaluate(
            ProfileSaveData profile,
            RelicEquipSessionCommand command)
        {
            if (!RelicEquipEditor.TryGetTargetCharacter(
                    profile,
                    command.CharacterId,
                    command.TargetSlotIndex,
                    out CharacterSaveData character,
                    out _))
            {
                RelicEquipEditResult failed =
                    command.CommandType
                        == RelicEquipSessionCommandType.EquipPreview
                        ? editor.Equip(
                            profile,
                            command.CharacterId,
                            command.TargetSlotIndex,
                            command.RelicInstanceId)
                        : editor.Clear(
                            profile,
                            command.CharacterId,
                            command.TargetSlotIndex);
                return new OperationEvaluation(
                    RelicEquipPersistenceStatus.Rejected,
                    failed);
            }

            string currentInstanceId =
                character.EquippedRelicInstanceIds[
                    command.TargetSlotIndex];
            if (!string.Equals(
                    currentInstanceId,
                    command.ExpectedOriginalInstanceId,
                    StringComparison.Ordinal))
            {
                return new OperationEvaluation(
                    RelicEquipPersistenceStatus.StaleSession,
                    editResult: null);
            }

            if (command.CommandType
                == RelicEquipSessionCommandType.EquipPreview)
            {
                if (string.IsNullOrEmpty(command.RelicInstanceId))
                {
                    return new OperationEvaluation(
                        RelicEquipPersistenceStatus.NoRelicSelected,
                        editResult: null);
                }

                RelicEquipEditResult equip = editor.Equip(
                    profile,
                    command.CharacterId,
                    command.TargetSlotIndex,
                    command.RelicInstanceId);
                if (!equip.IsSuccess)
                {
                    return new OperationEvaluation(
                        RelicEquipPersistenceStatus.Rejected,
                        equip);
                }

                return new OperationEvaluation(
                    equip.OperationType
                        == RelicEquipEditOperationType.NoChange
                            ? RelicEquipPersistenceStatus.NoChange
                            : RelicEquipPersistenceStatus.ChangedAndSaved,
                    equip);
            }

            RelicEquipEditResult clear = editor.Clear(
                profile,
                command.CharacterId,
                command.TargetSlotIndex);
            return new OperationEvaluation(
                clear.OperationType
                    == RelicEquipEditOperationType.NoChange
                        ? RelicEquipPersistenceStatus.NoChange
                        : RelicEquipPersistenceStatus.ChangedAndSaved,
                clear);
        }

        private static void Apply(
            ProfileSaveData profile,
            RelicEquipSessionCommand command,
            RelicEquipEditResult editResult)
        {
            RelicEquipEditor.TryGetTargetCharacter(
                profile,
                command.CharacterId,
                command.TargetSlotIndex,
                out CharacterSaveData character,
                out _);
            character.EquippedRelicInstanceIds[
                command.TargetSlotIndex] = editResult.ResultingSlots[
                    command.TargetSlotIndex];
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

        private static RelicEquipPersistenceOperationResult CreateResult(
            RelicEquipPersistenceStatus status,
            ProfileSaveData profileSnapshot,
            RelicEquipEditResult editResult,
            SaveTransactionResult saveTransactionResult)
        {
            return new RelicEquipPersistenceOperationResult(
                status,
                profileSnapshot,
                editResult,
                saveTransactionResult);
        }

        private sealed class OperationEvaluation
        {
            public OperationEvaluation(
                RelicEquipPersistenceStatus status,
                RelicEquipEditResult editResult)
            {
                Status = status;
                EditResult = editResult;
            }

            public RelicEquipPersistenceStatus Status { get; }
            public RelicEquipEditResult EditResult { get; }
        }

        private sealed class CommandRejectedException
            : InvalidOperationException
        {
            public CommandRejectedException(
                OperationEvaluation evaluation)
                : base(
                    $"Relic equip command was rejected: "
                    + $"{evaluation.Status}.")
            {
                Evaluation = evaluation;
            }

            public OperationEvaluation Evaluation { get; }
        }
    }
}
