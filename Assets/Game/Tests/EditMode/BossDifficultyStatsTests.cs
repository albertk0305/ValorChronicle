using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class BossDifficultyStatsTests
    {
        private const string BossAssetPath =
            "Assets/Data/Bosses/boss_kragmor.asset";
        private const string MareaAssetPath =
            "Assets/Data/Characters/marea_bluefang.asset";

        [TestCase("difficulty_intro", 33000L, 500d)]
        [TestCase("difficulty_normal", 66000L, 850d)]
        [TestCase("difficulty_advanced", 99000L, 1200d)]
        [TestCase("difficulty_hard", 138000L, 1550d)]
        [TestCase("difficulty_challenge", 198000L, 2100d)]
        public void KragmorAssetProvidesExpectedDifficultyStats(
            string difficultyId,
            long expectedMaxHp,
            double expectedAttack)
        {
            BossDefinition boss = LoadBoss();

            bool found = boss.TryGetDifficultyStats(
                difficultyId,
                out BossDifficultyStats stats);

            Assert.That(found, Is.True);
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.DifficultyId, Is.EqualTo(difficultyId));
            Assert.That(stats.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(stats.Attack, Is.EqualTo(expectedAttack));
        }

        [Test]
        public void KragmorAssetContainsExactlyTheFiveRequiredDifficulties()
        {
            BossDefinition boss = LoadBoss();

            Assert.That(boss.DifficultyStats, Has.Count.EqualTo(5));
            Assert.That(
                new HashSet<string>
                {
                    boss.DifficultyStats[0].DifficultyId,
                    boss.DifficultyStats[1].DifficultyId,
                    boss.DifficultyStats[2].DifficultyId,
                    boss.DifficultyStats[3].DifficultyId,
                    boss.DifficultyStats[4].DifficultyId
                },
                Is.EquivalentTo(new[]
                {
                    "difficulty_intro",
                    "difficulty_normal",
                    "difficulty_advanced",
                    "difficulty_hard",
                    "difficulty_challenge"
                }));
        }

        [Test]
        public void MissingDifficultyLookupReturnsFalseAndNull()
        {
            BossDefinition boss = LoadBoss();

            bool found = boss.TryGetDifficultyStats(
                "difficulty_missing",
                out BossDifficultyStats stats);

            Assert.That(found, Is.False);
            Assert.That(stats, Is.Null);
        }

        [Test]
        public void DifficultyCollectionCannotBeMutatedThroughPublicApi()
        {
            BossDefinition boss = LoadBoss();
            var list = (IList<BossDifficultyStats>)boss.DifficultyStats;

            Assert.Throws<NotSupportedException>(() => list.Add(
                new BossDifficultyStats("difficulty_test", 1, 0d)));
        }

        [TestCase("difficulty_intro", 33000L, 500d)]
        [TestCase("difficulty_normal", 66000L, 850d)]
        [TestCase("difficulty_advanced", 99000L, 1200d)]
        [TestCase("difficulty_hard", 138000L, 1550d)]
        [TestCase("difficulty_challenge", 198000L, 2100d)]
        public void CompositionCopiesSelectedStatsIntoRuntimeBoss(
            string difficultyId,
            long expectedMaxHp,
            double expectedAttack)
        {
            BossDefinition boss = LoadBoss();
            CharacterDefinition marea = AssetDatabase.LoadAssetAtPath<
                CharacterDefinition>(MareaAssetPath);
            Assert.That(marea, Is.Not.Null);
            Assert.That(
                boss.TryGetDifficultyStats(
                    difficultyId,
                    out BossDifficultyStats stats),
                Is.True);

            var composition = CreateComposition(
                marea,
                1,
                boss,
                (KragmorCombatConfig)boss.CombatConfig,
                stats,
                new SeededRandomSource(1),
                BattleResultBalanceDefaults.Create());

            Assert.That(composition.Boss.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(composition.Boss.CurrentHp,
                Is.EqualTo(expectedMaxHp));
            Assert.That(composition.Boss.Attack,
                Is.EqualTo(expectedAttack));
        }

        [Test]
        public void RuntimeBossDamageDoesNotChangeDefinitionStats()
        {
            BossDefinition boss = LoadBoss();
            CharacterDefinition marea = AssetDatabase.LoadAssetAtPath<
                CharacterDefinition>(MareaAssetPath);
            Assert.That(
                boss.TryGetDifficultyStats(
                    "difficulty_normal",
                    out BossDifficultyStats stats),
                Is.True);
            var composition = CreateComposition(
                marea,
                1,
                boss,
                (KragmorCombatConfig)boss.CombatConfig,
                stats,
                new SeededRandomSource(1),
                BattleResultBalanceDefaults.Create());
            MethodInfo applyDamage = composition.Boss.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);

            applyDamage.Invoke(composition.Boss, new object[] { 1000L });

            Assert.That(composition.Boss.CurrentHp, Is.EqualTo(65000));
            Assert.That(stats.MaxHp, Is.EqualTo(66000));
            Assert.That(stats.Attack, Is.EqualTo(850d));
            Assert.That(
                boss.TryGetDifficultyStats(
                    "difficulty_normal",
                    out BossDifficultyStats after),
                Is.True);
            Assert.That(after, Is.SameAs(stats));
            Assert.That(after.MaxHp, Is.EqualTo(66000));
        }

        [Test]
        public void CompositionUsesInjectedCooldownAndWaterMaximum()
        {
            CharacterDefinition source = AssetDatabase.LoadAssetAtPath<
                CharacterDefinition>(MareaAssetPath);
            CharacterDefinition marea = UnityEngine.Object.Instantiate(
                source);
            MareaBluefangCombatConfig config =
                MareaBluefangTestConfig.Create(
                    activeCooldownTurns: 4,
                    waterElementMaxAmount: 7);
            MareaBluefangTestConfig.Assign(marea, config);
            BossDefinition boss = LoadBoss();
            boss.TryGetDifficultyStats(
                "difficulty_normal",
                out BossDifficultyStats stats);

            try
            {
                var composition = CreateComposition(
                    marea,
                    1,
                    boss,
                    (KragmorCombatConfig)boss.CombatConfig,
                    stats,
                    new SeededRandomSource(1),
                    BattleResultBalanceDefaults.Create());

                Assert.That(composition.Boss.Resources.Get(
                        WaterElementResource.Id).MaxAmount,
                    Is.EqualTo(7));
                Assert.That(composition.FlowSetup.ActiveAbilityCooldowns,
                    Is.EqualTo(new[] { 4 }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(marea);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static BossDefinition LoadBoss()
        {
            BossDefinition boss = AssetDatabase.LoadAssetAtPath<
                BossDefinition>(BossAssetPath);
            Assert.That(boss, Is.Not.Null);
            return boss;
        }

        private static BattleSceneCombatComposition CreateComposition(
            CharacterDefinition character,
            int level,
            BossDefinition boss,
            KragmorCombatConfig kragmorConfig,
            BossDifficultyStats stats,
            IRandomSource randomSource,
            BattleResultBalance resultBalance)
        {
            return new BattleSceneCombatComposition(
                new[]
                {
                    new BattlePartyMemberInput(
                        character.Id,
                        partySlotIndex: 0,
                        level: level,
                        awakening: 0,
                        characterDefinition: character)
                },
                new CharacterCombatProviderRegistrarCatalog(
                    new ICharacterCombatProviderRegistrar[]
                    {
                        new MareaBluefangCombatProviderRegistrar()
                    }),
                boss,
                kragmorConfig,
                stats,
                randomSource,
                resultBalance);
        }
    }
}
