using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Audio.Core;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Flow.Presentation;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleSfxPresenterTests
    {
        [Test]
        public void MatchRemoval_RequestsOneBlockPopPerMatchGroup()
        {
            var cues = new List<SfxCue>();
            object presenter = CreateRecordingPresenter(cues);

            InvokePrivate(
                presenter,
                "HandleMatchGroupsRemovalStarted",
                3);

            Assert.That(cues, Is.EqualTo(new[]
            {
                SfxCue.BlockPop,
                SfxCue.BlockPop,
                SfxCue.BlockPop
            }));
        }

        [Test]
        public void EmptyMatchRemoval_RequestsNoBlockPop()
        {
            var cues = new List<SfxCue>();
            object presenter = CreateRecordingPresenter(cues);

            InvokePrivate(
                presenter,
                "HandleMatchGroupsRemovalStarted",
                0);

            Assert.That(cues, Is.Empty);
        }

        [TestCase(0L, false)]
        [TestCase(1L, true)]
        public void BossDamage_RequestsHitOnlyForAppliedDamage(
            long appliedDamage,
            bool expectsHit)
        {
            var cues = new List<SfxCue>();
            object presenter = CreateRecordingPresenter(cues);
            DamageActionResult result = CreateBossDamageResult(appliedDamage);

            InvokePrivate(
                presenter,
                "HandleCombatActionResult",
                result);

            Assert.That(cues.Contains(SfxCue.Hit), Is.EqualTo(expectsHit));
        }

        [TestCase(0L, 0L, false)]
        [TestCase(0L, 1L, true)]
        [TestCase(1L, 0L, true)]
        public void PartyDamage_RequestsHitForHpOrShieldDamage(
            long hpDamage,
            long shieldDamage,
            bool expectsHit)
        {
            var cues = new List<SfxCue>();
            object presenter = CreateRecordingPresenter(cues);
            BossDamageActionResult result = CreatePartyDamageResult(
                hpDamage,
                shieldDamage);

            InvokePrivate(
                presenter,
                "HandleCombatActionResult",
                result);

            Assert.That(cues.Contains(SfxCue.Hit), Is.EqualTo(expectsHit));
        }

        [Test]
        public void MissingAudioService_IgnoresPresentationRequests()
        {
            var presenter = new BattleSfxPresenter(audioService: null);

            Assert.DoesNotThrow(() => InvokePrivate(
                presenter,
                "HandleMatchGroupsRemovalStarted",
                1));
            Assert.DoesNotThrow(() => InvokePrivate(
                presenter,
                "HandleCombatActionResult",
                CreateBossDamageResult(1)));
        }

        [Test]
        public void SameDamageResult_IsRequestedOnlyOnce()
        {
            var cues = new List<SfxCue>();
            object presenter = CreateRecordingPresenter(cues);
            DamageActionResult result = CreateBossDamageResult(1);

            InvokePrivate(
                presenter,
                "HandleCombatActionResult",
                result);
            InvokePrivate(
                presenter,
                "HandleCombatActionResult",
                result);

            Assert.That(cues, Is.EqualTo(new[] { SfxCue.Hit }));
        }

        private static object CreateRecordingPresenter(List<SfxCue> cues)
        {
            ConstructorInfo constructor = typeof(BattleSfxPresenter)
                .GetConstructors(
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(value =>
                {
                    ParameterInfo[] parameters = value.GetParameters();
                    return parameters.Length == 1
                        && parameters[0].ParameterType
                            == typeof(Action<SfxCue>);
                });
            return constructor.Invoke(new object[]
            {
                new Action<SfxCue>(cues.Add)
            });
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
