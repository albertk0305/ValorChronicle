using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Characters.Fiora;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornBattleSceneIntegrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        private readonly List<BattleSceneCombatComposition> compositions =
            new List<BattleSceneCombatComposition>();

        [TearDown]
        public void TearDown()
        {
            for (int index = compositions.Count - 1; index >= 0; index--)
            {
                compositions[index].Dispose();
            }

            compositions.Clear();
        }

        [Test]
        public void FiveMemberProfileFlowsIntoProductionCompositionInSlotOrder()
        {
            DefinitionDatabase database = Database();
            var profile = new NewProfileFactory().Create(
                "naira_integration",
                utcUnixSeconds: 1);
            string[] characterIds =
            {
                NairaWindhornRules.CharacterId,
                MareaBluefangRules.CharacterId,
                SerenaCoralspearRules.CharacterId,
                FioraAmberwingRules.CharacterId,
                BrinaAshwoodRules.CharacterId
            };
            for (int index = 0; index < characterIds.Length; index++)
            {
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = characterIds[index],
                    Level = characterIds[index]
                        == NairaWindhornRules.CharacterId ? 100 : 1,
                    Awakening = characterIds[index]
                        == NairaWindhornRules.CharacterId ? 6 : 0
                });
                profile.Party.Presets[0].CharacterSlotIds[index] =
                    characterIds[index];
            }

            BattlePartyResolutionResult resolution =
                new BattlePartyResolver(database).Resolve(profile);

            Assert.That(resolution.IsSuccess, Is.True,
                resolution.ErrorMessage);
            Assert.That(resolution.Members.Select(item => item.CharacterId),
                Is.EqualTo(characterIds));
            Assert.That(resolution.Members.Select(item =>
                    item.PartySlotIndex),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            BattlePartyMemberInput nairaInput = resolution.Members[0];
            Assert.That(nairaInput.Level, Is.EqualTo(100));
            Assert.That(nairaInput.Awakening, Is.EqualTo(6));
            Assert.That(nairaInput.CharacterDefinition.CombatConfig,
                Is.TypeOf<NairaWindhornCombatConfig>());

            BattleSceneCombatComposition composition = CreateComposition(
                resolution.Members);
            CharacterBattleState naira = composition.Party.Characters[0];
            Assert.That(naira.CharacterId,
                Is.EqualTo(NairaWindhornRules.CharacterId));
            Assert.That(naira.MaxHp, Is.EqualTo(6510));
            Assert.That(naira.Attack, Is.EqualTo(446d));
            Assert.That(composition.Party.MaxHp,
                Is.EqualTo(composition.Party.Characters.Sum(item =>
                    item.MaxHp)));
            Assert.That(composition.MatchProviders.TryResolve(
                NairaWindhornRules.CharacterId,
                out _), Is.True);
            Assert.That(composition.ActiveProviders.TryResolve(
                NairaWindhornRules.CharacterId,
                NairaWindhornRules.ActiveAbilityId,
                out _), Is.True);
            Assert.That(composition.ActiveBindings.Any(binding =>
                    binding.PartySlotIndex == 0
                    && binding.CharacterId
                        == NairaWindhornRules.CharacterId),
                Is.True);
            Assert.That(composition.Boss.Effects.FindByEffectId(
                NairaWindhornRules.SpiritWardEffectId), Is.Empty);

            BattleFlowCombatBridge bridge = Start(composition);
            Assert.That(bridge.ResolveMatchEvent(BeginMatch(
                bridge.Coordinator,
                ElementType.Water,
                blockCount: 3)), Is.True);
            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                    .OfType<DamageActionResult>()
                    .Select(result => result.ContextRequestAttackerId()),
                Is.EqualTo(new[]
                {
                    MareaBluefangRules.CharacterId,
                    SerenaCoralspearRules.CharacterId
                }));
        }

        [Test]
        public void ThreeMatchCreatesShieldAndImmediatelyEnablesSpiritWard()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 2));
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(bridge.ResolveMatchEvent(BeginMatch(
                bridge.Coordinator,
                ElementType.Grass,
                blockCount: 3)), Is.True);

            ShieldActionResult shield = bridge.LastMatchExecutionResult
                .ActionResults.OfType<ShieldActionResult>().Single();
            Assert.That(bridge.LastMatchExecutionResult.ActionResults.Count,
                Is.EqualTo(1));
            Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(520));
            Assert.That(composition.Party.Shields.TotalShield,
                Is.EqualTo(520));
            Assert.That(composition.Party.Shields.ActiveShields.Single()
                .RemainingTurns, Is.EqualTo(3));
            EffectInstance spiritWard = composition.Boss.Effects
                .FindByEffectId(NairaWindhornRules.SpiritWardEffectId)
                .Single();
            Assert.That(spiritWard.Magnitude, Is.EqualTo(0.06d));
            Assert.That(spiritWard.RemainingTurns, Is.Null);
        }

        [Test]
        public void FourMatchUsesPostShieldChargeAndSpiritWardForDamage()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 0));
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(bridge.ResolveMatchEvent(BeginMatch(
                bridge.Coordinator,
                ElementType.Grass,
                blockCount: 4)), Is.True);

            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(ShieldActionResult),
                    typeof(DamageActionResult)
                }));
            ShieldActionResult shield = bridge.LastMatchExecutionResult
                .ActionResults[0] as ShieldActionResult;
            DamageActionResult damage = bridge.LastMatchExecutionResult
                .ActionResults[1] as DamageActionResult;
            Assert.That(shield, Is.Not.Null);
            Assert.That(damage, Is.Not.Null);
            Assert.That(shield.ApplicationResult.TotalShieldAfter,
                Is.EqualTo(846));
            Assert.That(damage.Context.SkillCoefficient,
                Is.EqualTo(1.2d * (1d + ((846d / 6510d) * 0.5d)))
                    .Within(0.000000001d));
            Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.06d));
            var damageAction = (DamageAction)damage.Action;
            Assert.That(damageAction.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(damage.Context.AttackElement,
                Is.EqualTo(ElementType.Grass));
        }

        [Test]
        public void SixMatchAtShieldCapGrantsGuardianBeforeMaximumDamage()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 0));
            AddShield(composition, composition.Party.MaxHp, runtimeId: 9001);
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(bridge.ResolveMatchEvent(BeginMatch(
                bridge.Coordinator,
                ElementType.Grass,
                blockCount: 6)), Is.True);

            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(ShieldActionResult),
                    typeof(ApplyEffectActionResult),
                    typeof(DamageActionResult)
                }));
            ShieldActionResult shield = bridge.LastMatchExecutionResult
                .ActionResults[0] as ShieldActionResult;
            ApplyEffectActionResult guardian = bridge
                .LastMatchExecutionResult.ActionResults[1]
                as ApplyEffectActionResult;
            DamageActionResult damage = bridge.LastMatchExecutionResult
                .ActionResults[2] as DamageActionResult;
            Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                Is.Zero);
            Assert.That(shield.ApplicationResult.CreatedShieldRuntimeId,
                Is.Null);
            Assert.That(guardian.AppliedEffect.EffectId,
                Is.EqualTo(NairaWindhornRules.AncientGuardianEffectId));
            Assert.That(guardian.AppliedEffect.Magnitude,
                Is.EqualTo(0.15d));
            Assert.That(damage.Context.SkillCoefficient,
                Is.EqualTo(4.4d));
            Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.06d));
        }

        [Test]
        public void FiveMatchBelowFullChargeDoesNotGrantGuardian()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 0));
            AddShield(
                composition,
                amount: composition.Party.MaxHp - 1303,
                runtimeId: 9002);
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(bridge.ResolveMatchEvent(BeginMatch(
                bridge.Coordinator,
                ElementType.Grass,
                blockCount: 5)), Is.True);

            Assert.That(composition.Party.Shields.TotalShield,
                Is.EqualTo(composition.Party.MaxHp - 1));
            Assert.That(composition.Party.Effects.FindByEffectId(
                NairaWindhornRules.AncientGuardianEffectId), Is.Empty);
            DamageActionResult damage = bridge.LastMatchExecutionResult
                .ActionResults.OfType<DamageActionResult>().Single();
            Assert.That(damage.Context.SkillCoefficient,
                Is.LessThan(4.4d));
        }

        [Test]
        public void ActiveAtShieldCapStillGrantsGraceAndStartsCooldown()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 0));
            AddShield(composition, composition.Party.MaxHp, runtimeId: 9003);
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(bridge.CanUseActive(0), Is.True);
            Assert.That(bridge.TryUseActive(0), Is.True);

            Assert.That(bridge.LastActiveExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(ShieldActionResult),
                    typeof(ApplyEffectActionResult)
                }));
            ShieldActionResult shield = bridge.LastActiveExecutionResult
                .ActionResults[0] as ShieldActionResult;
            Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                Is.Zero);
            EffectInstance grace = composition.Party.Effects
                .FindByEffectId(NairaWindhornRules.WhiteStagGraceEffectId)
                .Single();
            Assert.That(grace.Magnitude, Is.EqualTo(0.40d));
            Assert.That(grace.RemainingTurns, Is.Null);
            Assert.That(bridge.Coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(12));
            Assert.That(bridge.Coordinator.Context.ActiveAbilities[0]
                .UsedThisTurn, Is.True);
            Assert.That(bridge.TryUseActive(0), Is.False);
            Assert.That(bridge.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        [Test]
        public void DisposedCompositionRemovesWardAndNewBattleIsIsolated()
        {
            BattleSceneCombatComposition first = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 0));
            BattleSceneCombatComposition second = CreateComposition(
                Member(level: 100, awakening: 6, partySlotIndex: 0));
            AddShield(first, amount: 100, runtimeId: 9101);

            Assert.That(first.Boss.Effects.FindByEffectId(
                    NairaWindhornRules.SpiritWardEffectId).Count,
                Is.EqualTo(1));
            Assert.That(second.Boss.Effects.FindByEffectId(
                NairaWindhornRules.SpiritWardEffectId), Is.Empty);
            Assert.That(first.Party.Characters[0],
                Is.Not.SameAs(second.Party.Characters[0]));
            Assert.That(first.Party.Shields,
                Is.Not.SameAs(second.Party.Shields));

            first.Dispose();

            Assert.That(first.Boss.Effects.FindByEffectId(
                NairaWindhornRules.SpiritWardEffectId), Is.Empty);
            AddShield(first, amount: 50, runtimeId: 9102);
            Assert.That(first.Boss.Effects.FindByEffectId(
                NairaWindhornRules.SpiritWardEffectId), Is.Empty);
            Assert.That(second.Party.Shields.TotalShield, Is.Zero);
            Assert.That(second.Party.Effects.GetActiveEffects(), Is.Empty);
        }

        private BattleSceneCombatComposition CreateComposition(
            params BattlePartyMemberInput[] members)
        {
            return CreateComposition(
                (IReadOnlyList<BattlePartyMemberInput>)members);
        }

        private BattleSceneCombatComposition CreateComposition(
            IReadOnlyList<BattlePartyMemberInput> members)
        {
            DefinitionDatabase database = Database();
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);
            BossDifficultyStats difficulty = boss.DifficultyStats[0];
            var composition = new BattleSceneCombatComposition(
                members,
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog(),
                boss,
                (KragmorCombatConfig)boss.CombatConfig,
                difficulty,
                new SeededRandomSource(12345),
                database.BattleResultBalance.CreateBalance());
            compositions.Add(composition);
            return composition;
        }

        private static BattlePartyMemberInput Member(
            int level,
            int awakening,
            int partySlotIndex)
        {
            DefinitionDatabase database = Database();
            Assert.That(database.TryGetCharacter(
                NairaWindhornRules.CharacterId,
                out CharacterDefinition definition), Is.True);
            return new BattlePartyMemberInput(
                definition.Id,
                partySlotIndex,
                level,
                awakening,
                definition);
        }

        private static BattleFlowCombatBridge Start(
            BattleSceneCombatComposition composition)
        {
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            BattleFlowCombatBridge bridge = composition.CreateBridge(
                coordinator);
            Assert.That(coordinator.StartBattle(), Is.True);
            return bridge;
        }

        private static MatchEventExecution BeginMatch(
            BattleFlowCoordinator coordinator,
            ElementType element,
            int blockCount)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(element, positions)
                    }),
                true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            return execution;
        }

        private static void AddShield(
            BattleSceneCombatComposition composition,
            long amount,
            long runtimeId)
        {
            composition.Party.Shields.Add(new ShieldInstance(
                runtimeId,
                "naira_integration_existing_shield",
                amount,
                createdTurn: 1,
                remainingTurns: 3,
                creationOrder: runtimeId));
        }

        private static DefinitionDatabase Database()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }
    }

    internal static class NairaWindhornIntegrationDamageResultExtensions
    {
        public static string ContextRequestAttackerId(
            this DamageActionResult result)
        {
            return ((DamageAction)result.Action)
                .ContextRequest.Attacker.CharacterId;
        }
    }
}
