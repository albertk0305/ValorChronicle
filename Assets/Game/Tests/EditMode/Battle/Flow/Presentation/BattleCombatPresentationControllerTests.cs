using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleCombatPresentationControllerTests
    {
        [Test]
        public void ElementColorsUseConfiguredElementAndWhiteFallback()
        {
            var root = new GameObject("PresentationController");
            try
            {
                var controller = root.AddComponent<
                    BattleCombatPresentationController>();

                Assert.That(controller.GetElementColor(ElementType.Fire),
                    Is.EqualTo(new Color(1f, 0.25f, 0.2f, 1f)));
                Assert.That(controller.GetElementColor(ElementType.Water),
                    Is.EqualTo(new Color(0.2f, 0.55f, 1f, 1f)));
                Assert.That(controller.GetElementColor(ElementType.Grass),
                    Is.EqualTo(new Color(0.25f, 0.8f, 0.35f, 1f)));
                Assert.That(controller.GetElementColor(ElementType.Light),
                    Is.EqualTo(new Color(1f, 0.85f, 0.2f, 1f)));
                Assert.That(controller.GetElementColor(ElementType.Dark),
                    Is.EqualTo(new Color(0.65f, 0.3f, 1f, 1f)));
                Assert.That(controller.GetElementColor((ElementType)999),
                    Is.EqualTo(Color.white));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RequestPreservesActorAndAttackElement()
        {
            var request = new MatchDamageProjectileRequest(
                4,
                "character_test",
                ElementType.Dark);

            Assert.That(request.PartySlotIndex, Is.EqualTo(4));
            Assert.That(request.CharacterId, Is.EqualTo("character_test"));
            Assert.That(request.AttackElement, Is.EqualTo(ElementType.Dark));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MatchDamageProjectileRequest(
                    5,
                    "character_test",
                    ElementType.Fire));
            Assert.Throws<ArgumentException>(() =>
                new MatchDamageProjectileRequest(
                    0,
                    " ",
                    ElementType.Fire));
        }

        [Test]
        public void ActiveRequestPreservesActorAndAttackElement()
        {
            var request = new ActiveDamageProjectileRequest(
                3,
                "active_character",
                ElementType.Water);

            Assert.That(request.PartySlotIndex, Is.EqualTo(3));
            Assert.That(request.CharacterId, Is.EqualTo("active_character"));
            Assert.That(request.AttackElement, Is.EqualTo(ElementType.Water));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ActiveDamageProjectileRequest(
                    -1,
                    "active_character",
                    ElementType.Fire));
            Assert.Throws<ArgumentException>(() =>
                new ActiveDamageProjectileRequest(
                    0,
                    string.Empty,
                    ElementType.Fire));
        }

        [Test]
        public void BossRequestPreservesIdentityAndUsesWhiteProjectile()
        {
            var request = new BossDamageProjectileRequest(3, 17);
            var root = new GameObject("PresentationController");
            try
            {
                var controller = root.AddComponent<
                    BattleCombatPresentationController>();

                Assert.That(request.CurrentTurn, Is.EqualTo(3));
                Assert.That(request.ActionId, Is.EqualTo(17));
                Assert.That(controller.BossProjectileColor,
                    Is.EqualTo(Color.white));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new BossDamageProjectileRequest(0, 17));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new BossDamageProjectileRequest(3, 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PoolReusesAndResetsNonRaycastProjectileView()
        {
            var texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            var prefabObject = new GameObject(
                "ProjectilePrefab",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(AttackProjectileView));
            var poolObject = new GameObject(
                "ProjectilePool",
                typeof(RectTransform),
                typeof(AttackProjectilePool));
            try
            {
                Image prefabImage = prefabObject.GetComponent<Image>();
                AttackProjectileView prefab =
                    prefabObject.GetComponent<AttackProjectileView>();
                prefab.Configure(prefabImage);
                prefabObject.SetActive(false);
                var pool = poolObject.GetComponent<AttackProjectilePool>();
                pool.Configure(
                    prefab,
                    poolObject.GetComponent<RectTransform>(),
                    1);
                pool.Initialize();

                AttackProjectileView first = pool.Acquire();
                first.Prepare(
                    sprite,
                    Color.red,
                    new Vector2(25f, 30f));
                Assert.That(first.Image.raycastTarget, Is.False);
                pool.Release(first);

                AttackProjectileView second = pool.Acquire();
                Assert.That(second, Is.SameAs(first));
                Assert.That(second.Image.raycastTarget, Is.False);
                Assert.That(second.Image.color, Is.EqualTo(Color.white));
                Assert.That(second.RectTransform.anchoredPosition,
                    Is.EqualTo(Vector2.zero));
                Assert.That(pool.TotalCreatedCount, Is.EqualTo(1));

                pool.Release(second);
                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(pool.AvailableCount, Is.EqualTo(1));
                Assert.That(first.gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(poolObject);
                UnityEngine.Object.DestroyImmediate(prefabObject);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void DamageNumberPoolSupportsConcurrentLeaseAndFullReset()
        {
            var prefabObject = new GameObject(
                "DamageNumberPrefab",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(DamageNumberView));
            var poolObject = new GameObject(
                "DamageNumberPool",
                typeof(RectTransform),
                typeof(DamageNumberPool));
            try
            {
                TextMeshProUGUI prefabText =
                    prefabObject.GetComponent<TextMeshProUGUI>();
                DamageNumberView prefab =
                    prefabObject.GetComponent<DamageNumberView>();
                prefab.Configure(prefabText);
                prefabObject.SetActive(false);
                var pool = poolObject.GetComponent<DamageNumberPool>();
                pool.Configure(
                    prefab,
                    poolObject.GetComponent<RectTransform>(),
                    1);
                pool.Initialize();

                DamageNumberView first = pool.Acquire();
                DamageNumberView second = pool.Acquire();
                first.Prepare(
                    "123!",
                    56f,
                    Color.yellow,
                    Color.white,
                    0.2f,
                    new Vector2(10f, 20f));
                second.Prepare(
                    "75",
                    42f,
                    Color.green,
                    Color.red,
                    0.2f,
                    new Vector2(-10f, -20f));

                Assert.That(pool.ActiveCount, Is.EqualTo(2));
                Assert.That(pool.TotalCreatedCount, Is.EqualTo(2));
                Assert.That(first.Text.raycastTarget, Is.False);
                Assert.That(first.Text.text, Is.EqualTo("123!"));
                Assert.That(first.Text.fontSize, Is.EqualTo(56f));
                Assert.That(first.Text.color, Is.EqualTo(Color.yellow));
                Assert.That(first.Text.outlineColor,
                    Is.EqualTo((Color32)Color.white));

                pool.Release(first);
                DamageNumberView reused = pool.Acquire();
                Assert.That(reused, Is.SameAs(first));
                Assert.That(reused.Text.text, Is.Empty);
                Assert.That(reused.Text.color, Is.EqualTo(Color.white));
                Assert.That(reused.Text.outlineColor,
                    Is.EqualTo((Color32)Color.black));
                Assert.That(reused.Text.outlineWidth, Is.Zero);
                Assert.That(reused.Text.alpha, Is.EqualTo(1f));
                Assert.That(reused.RectTransform.anchoredPosition,
                    Is.EqualTo(Vector2.zero));
                Assert.That(reused.Text.raycastTarget, Is.False);

                pool.Release(reused);
                pool.Release(second);
                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(pool.AvailableCount, Is.EqualTo(2));

                pool.Acquire();
                pool.Acquire();
                Assert.That(pool.ActiveCount, Is.EqualTo(2));
                pool.ReleaseAll();
                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(pool.AvailableCount, Is.EqualTo(2));
                poolObject.SetActive(false);
                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(pool.AvailableCount, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(poolObject);
                UnityEngine.Object.DestroyImmediate(prefabObject);
            }
        }

        [Test]
        public void PresentationRandomSourceIsSeedDeterministicAndBounded()
        {
            var first = new SystemPresentationRandomSource(1729);
            var second = new SystemPresentationRandomSource(1729);

            for (int index = 0; index < 8; index++)
            {
                float firstValue = first.NextUnitValue();
                Assert.That(firstValue, Is.InRange(0f, 1f));
                Assert.That(second.NextUnitValue(), Is.EqualTo(firstValue));
            }
        }

        [Test]
        public void PresentationRandomDoesNotConsumeGameplayRandomSequence()
        {
            var gameplay = new SeededRandomSource(4021);
            var untouchedGameplay = new SeededRandomSource(4021);
            var presentation = new SystemPresentationRandomSource(4021);

            for (int index = 0; index < 8; index++)
            {
                presentation.NextUnitValue();
            }

            Assert.That(gameplay.NextFloat(),
                Is.EqualTo(untouchedGameplay.NextFloat()));
            Assert.That(gameplay.Next(0, 10000),
                Is.EqualTo(untouchedGameplay.Next(0, 10000)));
        }

        [Test]
        public void DamageNumberOffsetUsesOnlyConfiguredPresentationRange()
        {
            var root = new GameObject("PresentationController");
            try
            {
                var controller = root.AddComponent<
                    BattleCombatPresentationController>();
                controller.SetDamageNumberRandomSource(
                    new SequencePresentationRandomSource(0f, 1f));
                MethodInfo createOffset = typeof(
                        BattleCombatPresentationController)
                    .GetMethod(
                        "CreateDamageNumberRandomOffset",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(createOffset, Is.Not.Null);

                Vector2 offset = (Vector2)createOffset.Invoke(
                    controller,
                    null);

                Assert.That(offset.x, Is.EqualTo(-32f));
                Assert.That(offset.y, Is.EqualTo(24f));
                Assert.That(Mathf.Abs(offset.x),
                    Is.LessThanOrEqualTo(
                        controller.DamageTextRandomOffset.x));
                Assert.That(Mathf.Abs(offset.y),
                    Is.LessThanOrEqualTo(
                        controller.DamageTextRandomOffset.y));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResolvedResultsDriveNormalWeakCriticalBossAndHealStyles()
        {
            var root = new GameObject("PresentationController");
            try
            {
                var controller = root.AddComponent<
                    BattleCombatPresentationController>();

                DamageActionResult normal = ExecutePlayerDamage(
                    ElementType.Fire,
                    canCritical: false);
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    normal,
                    out DamageNumberPresentation normalPresentation),
                    Is.True);
                Assert.That(normalPresentation.Text,
                    Is.EqualTo(normal.AppliedDamage.ToString()));
                Assert.That(normalPresentation.FontSize, Is.EqualTo(42f));
                Assert.That(normalPresentation.FaceColor,
                    Is.EqualTo(Color.white));
                Assert.That(normalPresentation.OutlineColor,
                    Is.EqualTo(Color.red));
                Assert.That(normalPresentation.UseBossAnchor, Is.True);

                DamageActionResult weakness = ExecutePlayerDamage(
                    ElementType.Grass,
                    canCritical: false);
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    weakness,
                    out DamageNumberPresentation weaknessPresentation),
                    Is.True);
                Assert.That(weaknessPresentation.Text,
                    Is.EqualTo(weakness.AppliedDamage.ToString()));
                Assert.That(weaknessPresentation.FontSize, Is.EqualTo(48f));
                Assert.That(weaknessPresentation.FaceColor,
                    Is.EqualTo(Color.yellow));
                Assert.That(weaknessPresentation.OutlineColor,
                    Is.EqualTo(Color.white));

                DamageActionResult critical = ExecutePlayerDamage(
                    ElementType.Fire,
                    canCritical: true);
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    critical,
                    out DamageNumberPresentation normalCriticalPresentation),
                    Is.True);
                Assert.That(normalCriticalPresentation.Text,
                    Is.EqualTo(critical.AppliedDamage + "!"));
                Assert.That(normalCriticalPresentation.FontSize,
                    Is.EqualTo(56f));
                Assert.That(normalCriticalPresentation.FaceColor,
                    Is.EqualTo(Color.white));
                Assert.That(normalCriticalPresentation.OutlineColor,
                    Is.EqualTo(Color.red));

                DamageActionResult weakCritical = ExecutePlayerDamage(
                    ElementType.Grass,
                    canCritical: true);
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    weakCritical,
                    out DamageNumberPresentation criticalPresentation),
                    Is.True);
                Assert.That(weakCritical.WasWeakness, Is.True);
                Assert.That(weakCritical.WasCritical, Is.True);
                Assert.That(criticalPresentation.Text,
                    Is.EqualTo(weakCritical.AppliedDamage + "!"));
                Assert.That(criticalPresentation.FontSize, Is.EqualTo(56f));
                Assert.That(criticalPresentation.FaceColor,
                    Is.EqualTo(Color.yellow));
                Assert.That(criticalPresentation.OutlineColor,
                    Is.EqualTo(Color.white));

                BossDamageActionResult absorbed =
                    ExecuteFullyAbsorbedBossDamage();
                Assert.That(absorbed.ApplicationResult.ShieldAbsorbedDamage,
                    Is.GreaterThan(0));
                Assert.That(absorbed.AppliedHpDamage, Is.Zero);
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    absorbed,
                    out DamageNumberPresentation bossPresentation),
                    Is.True);
                Assert.That(bossPresentation.Text, Is.EqualTo("0"));
                Assert.That(bossPresentation.FaceColor,
                    Is.EqualTo(Color.white));
                Assert.That(bossPresentation.OutlineColor,
                    Is.EqualTo(Color.red));
                Assert.That(bossPresentation.UseBossAnchor, Is.False);

                HealActionResult healing = ExecuteHealing(damageFirst: true);
                Assert.That(healing.AppliedHealing, Is.GreaterThan(0));
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    healing,
                    out DamageNumberPresentation healPresentation),
                    Is.True);
                Assert.That(healPresentation.Text,
                    Is.EqualTo(healing.AppliedHealing.ToString()));
                Assert.That(healPresentation.Text, Does.Not.StartWith("+"));
                Assert.That(healPresentation.FaceColor,
                    Is.EqualTo(Color.green));
                Assert.That(healPresentation.OutlineColor,
                    Is.EqualTo(Color.red));
                Assert.That(healPresentation.UseBossAnchor, Is.False);

                DamageActionResult lethal = ExecutePlayerDamage(
                    ElementType.Fire,
                    canCritical: false,
                    bossMaxHp: 1);
                Assert.That(lethal.BecameDefeated, Is.True);
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    lethal,
                    out DamageNumberPresentation lethalPresentation),
                    Is.True);
                Assert.That(lethalPresentation.Text, Is.EqualTo("1"));

                HealActionResult zeroHealing = ExecuteHealing(
                    damageFirst: false);
                Assert.That(zeroHealing.AppliedHealing, Is.Zero);
                Assert.That(controller.TryCreateDamageNumberPresentation(
                    zeroHealing,
                    out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static DamageActionResult ExecutePlayerDamage(
            ElementType bossElement,
            bool canCritical,
            long bossMaxHp = 10000)
        {
            CreateBattle(
                bossElement,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor,
                bossMaxHp);
            var action = new DamageAction(
                1,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    character,
                    party,
                    boss,
                    ElementType.Fire,
                    AttackType.Match,
                    AttackTag.None,
                    1d,
                    false,
                    0,
                    canCritical,
                    canCritical ? 1d : 0d));
            return (DamageActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[] { action }))
                .ActionResults[0];
        }

        private static BossDamageActionResult ExecuteFullyAbsorbedBossDamage()
        {
            CreateBattle(
                ElementType.Fire,
                out _,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            party.Shields.Add(new ShieldInstance(
                1,
                "test",
                10000,
                1,
                null,
                1));
            var action = new BossDamageAction(
                1,
                new BossDamageContextBuildRequest(
                    boss,
                    party,
                    1d,
                    AttackTag.None));
            return (BossDamageActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[] { action }))
                .ActionResults[0];
        }

        private static HealActionResult ExecuteHealing(bool damageFirst)
        {
            CreateBattle(
                ElementType.Fire,
                out CharacterBattleState character,
                out PartyBattleState party,
                out _,
                out CombatActionExecutor executor);
            if (damageFirst)
            {
                PartyDamageApplier.Apply(
                    party,
                    BossDamageCalculator.Calculate(
                        new BossDamageContext(
                            400d,
                            0d,
                            0d,
                            1d,
                            0d,
                            0d,
                            0d,
                            0d)));
            }

            var action = new HealAction(
                1,
                ActionOrigin.Active,
                new HealingContextBuildRequest(
                    character,
                    party,
                    0.25d,
                    false,
                    0));
            return (HealActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[] { action }))
                .ActionResults[0];
        }

        private static void CreateBattle(
            ElementType bossElement,
            out CharacterBattleState character,
            out PartyBattleState party,
            out BossBattleState boss,
            out CombatActionExecutor executor,
            long bossMaxHp = 10000)
        {
            character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                1000,
                1000d);
            party = new PartyBattleState(new[] { character });
            boss = new BossBattleState(
                "boss",
                bossElement,
                bossMaxHp,
                500d);
            executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
        }

        private sealed class SequencePresentationRandomSource
            : IPresentationRandomSource
        {
            private readonly float[] values;
            private int index;

            public SequencePresentationRandomSource(params float[] values)
            {
                this.values = values;
            }

            public float NextUnitValue()
            {
                return values[index++];
            }
        }
    }
}
