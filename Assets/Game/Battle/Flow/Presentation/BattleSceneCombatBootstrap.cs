using System;
using UnityEngine;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatBootstrap : MonoBehaviour
    {
        private const string DevelopmentBossId = "kragmor";

        [SerializeField]
        private BattleFlowController battleFlowController = null;

        [SerializeField]
        private BossDefinition fallbackBossDefinition = null;

        [SerializeField]
        private CharacterDefinition fallbackMareaDefinition = null;

        [SerializeField, Min(1)]
        private int developmentMareaLevel = 1;

        [SerializeField, Min(1)]
        private long developmentBossMaxHp = 100000;

        [SerializeField, Min(0f)]
        private double developmentBossAttack;

        private bool initializationAttempted;

        public bool HasInitializedFlow { get; private set; }
        public bool HasInitializedCombat => CombatComposition?.Bridge != null;
        public BattleSceneCombatComposition CombatComposition
        {
            get;
            private set;
        }

        private void Awake()
        {
            InitializeFlowOnce();
        }

        private void InitializeFlowOnce()
        {
            if (initializationAttempted)
            {
                return;
            }

            initializationAttempted = true;
            if (battleFlowController == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] BattleFlowController "
                        + "is not assigned.",
                    this);
                return;
            }

            if (battleFlowController.Context != null)
            {
                HasInitializedFlow = true;
                return;
            }

            BossDefinition selectedBoss = ResolveBossDefinition();
            if (selectedBoss == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] No BossDefinition is "
                        + "available.",
                    this);
                return;
            }

            CharacterDefinition selectedMarea = ResolveMareaDefinition();
            if (selectedMarea == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] No Marea "
                        + "CharacterDefinition is available.",
                    this);
                return;
            }

            try
            {
                IRandomSource randomSource =
                    GameBootstrapper.Instance?.RandomSource
                    ?? new UnityRandomSource();
                CombatComposition = new BattleSceneCombatComposition(
                    selectedMarea,
                    developmentMareaLevel,
                    selectedBoss,
                    developmentBossMaxHp,
                    developmentBossAttack,
                    randomSource);
                battleFlowController.Initialize(
                    CombatComposition.FlowSetup,
                    CombatComposition.CreateBridge);
                HasInitializedFlow = true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Battle Flow "
                        + "initialization failed.",
                    this);
            }
        }

        private BossDefinition ResolveBossDefinition()
        {
            DefinitionDatabase database =
                GameBootstrapper.Instance?.DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.TryGetBoss(
                    DevelopmentBossId,
                    out BossDefinition databaseBoss))
            {
                return databaseBoss;
            }

            return fallbackBossDefinition;
        }

        private CharacterDefinition ResolveMareaDefinition()
        {
            DefinitionDatabase database =
                GameBootstrapper.Instance?.DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.TryGetCharacter(
                    MareaBluefangRules.CharacterId,
                    out CharacterDefinition databaseMarea))
            {
                return databaseMarea;
            }

            return fallbackMareaDefinition;
        }
    }
}
