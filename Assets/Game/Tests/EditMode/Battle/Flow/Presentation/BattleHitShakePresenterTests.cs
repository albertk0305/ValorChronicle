using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleHitShakePresenterTests
    {
        private GameObject owner;
        private BattleScreenShake shake;
        private BattleHitShakePresenter presenter;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject(
                "BattleHitShakePresenterTests",
                typeof(RectTransform));
            shake = owner.AddComponent<BattleScreenShake>();
            presenter = new BattleHitShakePresenter(shake);
            SetAutoProperty(presenter, "IsInitialized", true);
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [TestCase(1L, 0L)]
        [TestCase(0L, 1L)]
        [TestCase(1L, 1L)]
        public void AppliedPartyDamage_StartsOneShake(
            long hpDamage,
            long shieldDamage)
        {
            InvokeResult(CreatePartyDamageResult(hpDamage, shieldDamage));

            Assert.That(shake.IsShaking, Is.True);
            Assert.That(shake.CurrentOffset.magnitude,
                Is.EqualTo(BattleScreenShake.DefaultMaximumOffset)
                    .Within(0.0001f));
        }

        [Test]
        public void ZeroPartyDamage_DoesNotShake()
        {
            InvokeResult(CreatePartyDamageResult(0, 0));

            Assert.That(shake.IsShaking, Is.False);
        }

        [Test]
        public void AllyDamageResult_DoesNotShake()
        {
            InvokeResult(CreateBossDamageResult(1));

            Assert.That(shake.IsShaking, Is.False);
        }

        [Test]
        public void NullResult_DoesNotShake()
        {
            InvokeResult(null);

            Assert.That(shake.IsShaking, Is.False);
        }

        [Test]
        public void PauseStopsShakeAndResumeAllowsNextHit()
        {
            InvokeResult(CreatePartyDamageResult(1, 0));
            Assert.That(shake.IsShaking, Is.True);

            InvokePrivate(presenter, "HandlePauseStateChanged", true);
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(presenter.IsPaused, Is.True);
            InvokeResult(CreatePartyDamageResult(1, 0));
            Assert.That(shake.IsShaking, Is.False);

            InvokePrivate(presenter, "HandlePauseStateChanged", false);
            InvokeResult(CreatePartyDamageResult(1, 0));
            Assert.That(shake.IsShaking, Is.True);
        }

        [Test]
        public void BattleResultStopsShakeAndBlocksLaterHits()
        {
            InvokeResult(CreatePartyDamageResult(1, 0));
            Assert.That(shake.IsShaking, Is.True);

            InvokePrivate(
                presenter,
                "HandleBattleResultReached",
                BattleResultKind.Defeat);
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(presenter.HasBattleEnded, Is.True);

            InvokeResult(CreatePartyDamageResult(1, 0));
            Assert.That(shake.IsShaking, Is.False);
        }

        [Test]
        public void SameDamageResult_DoesNotRestartShake()
        {
            BossDamageActionResult result = CreatePartyDamageResult(1, 0);
            InvokeResult(result);
            shake.Advance(0.05f);

            InvokeResult(result);
            shake.Advance(0.101f);

            Assert.That(shake.IsShaking, Is.False);
        }

        [Test]
        public void DistinctDamageResults_RestartCurrentShake()
        {
            InvokeResult(CreatePartyDamageResult(1, 0));
            shake.Advance(0.05f);

            InvokeResult(CreatePartyDamageResult(1, 0));
            shake.Advance(0.101f);

            Assert.That(shake.IsShaking, Is.True);
            Assert.That(shake.CurrentOffset.magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void UninitializedPresenter_IgnoresDamage()
        {
            SetAutoProperty(presenter, "IsInitialized", false);

            InvokeResult(CreatePartyDamageResult(1, 0));

            Assert.That(shake.IsShaking, Is.False);
        }

        private void InvokeResult(CombatActionResult result)
        {
            InvokePrivate(presenter, "HandleCombatActionResult", result);
        }

        private static DamageActionResult CreateBossDamageResult(
            long appliedDamage)
        {
            BossDamageApplicationResult application =
                CreateNonPublic<BossDamageApplicationResult>(
                    appliedDamage,
                    appliedDamage,
                    0L,
                    100L,
                    100L - appliedDamage,
                    false,
                    false);
            return CreateNonPublic<DamageActionResult>(
                null,
                1,
                null,
                null,
                application);
        }

        private static BossDamageActionResult CreatePartyDamageResult(
            long hpDamage,
            long shieldDamage)
        {
            long requestedDamage = hpDamage + shieldDamage;
            PartyDamageApplicationResult application =
                CreateNonPublic<PartyDamageApplicationResult>(
                    requestedDamage,
                    shieldDamage,
                    hpDamage,
                    0L,
                    100L,
                    100L - hpDamage,
                    shieldDamage,
                    0L,
                    false,
                    false);
            return CreateNonPublic<BossDamageActionResult>(
                null,
                1,
                null,
                null,
                application);
        }

        private static T CreateNonPublic<T>(params object[] arguments)
        {
            ConstructorInfo constructor = typeof(T)
                .GetConstructors(
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(value =>
                    value.GetParameters().Length == arguments.Length);
            return (T)constructor.Invoke(arguments);
        }

        private static void SetAutoProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName);
            property.SetValue(target, value);
        }

        private static object InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(target, arguments);
        }
    }
}
