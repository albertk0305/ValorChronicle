using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.Bootstrap;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class BattleEntryServiceTests
    {
        [Test]
        public void Request_WithValidInternalIds_PreservesValues()
        {
            var request = new BattleEntryRequest(
                "kragmor",
                BattleDifficultyIds.Normal);

            Assert.That(request.BossId, Is.EqualTo("kragmor"));
            Assert.That(
                request.DifficultyId,
                Is.EqualTo(BattleDifficultyIds.Normal));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Request_WithInvalidBossId_Throws(string bossId)
        {
            Assert.That(
                () => new BattleEntryRequest(
                    bossId,
                    BattleDifficultyIds.Normal),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Request_WithInvalidDifficultyId_Throws(
            string difficultyId)
        {
            Assert.That(
                () => new BattleEntryRequest("kragmor", difficultyId),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void TryRegister_WhenEmpty_StoresRequest()
        {
            var service = new BattleEntryService();
            BattleEntryRequest request = CreateRequest("kragmor");

            Assert.That(service.TryRegister(request), Is.True);
            Assert.That(service.HasPendingRequest, Is.True);
            Assert.That(service.TryGetCurrent(out BattleEntryRequest current),
                Is.True);
            Assert.That(current, Is.SameAs(request));
        }

        [Test]
        public void TryGetCurrent_DoesNotConsumeRequest()
        {
            var service = new BattleEntryService();
            BattleEntryRequest request = CreateRequest("kragmor");
            service.TryRegister(request);

            Assert.That(service.TryGetCurrent(out BattleEntryRequest first),
                Is.True);
            Assert.That(service.TryGetCurrent(out BattleEntryRequest second),
                Is.True);
            Assert.That(first, Is.SameAs(request));
            Assert.That(second, Is.SameAs(request));
            Assert.That(service.HasPendingRequest, Is.True);
        }

        [Test]
        public void TryConsume_WithCurrentRequest_ReturnsAndRemovesRequest()
        {
            var service = new BattleEntryService();
            BattleEntryRequest request = CreateRequest("kragmor");
            service.TryRegister(request);

            bool consumed = service.TryConsume(
                request,
                out BattleEntryRequest consumedRequest);

            Assert.That(consumed, Is.True);
            Assert.That(consumedRequest, Is.SameAs(request));
            Assert.That(service.HasPendingRequest, Is.False);
            Assert.That(service.TryGetCurrent(out _), Is.False);
        }

        [Test]
        public void TryCancel_WithCurrentRequest_RemovesRequest()
        {
            var service = new BattleEntryService();
            BattleEntryRequest request = CreateRequest("kragmor");
            service.TryRegister(request);

            Assert.That(service.TryCancel(request), Is.True);
            Assert.That(service.HasPendingRequest, Is.False);
        }

        [Test]
        public void ConsumeAndCancel_WhenEmpty_AreSafe()
        {
            var service = new BattleEntryService();
            BattleEntryRequest request = CreateRequest("kragmor");

            Assert.That(
                service.TryConsume(request, out BattleEntryRequest consumed),
                Is.False);
            Assert.That(consumed, Is.Null);
            Assert.That(service.TryCancel(request), Is.False);
            Assert.That(service.TryConsume(null, out _), Is.False);
            Assert.That(service.TryCancel(null), Is.False);
            Assert.That(service.HasPendingRequest, Is.False);
        }

        [Test]
        public void TryRegister_WithPendingRequest_RejectsDuplicate()
        {
            var service = new BattleEntryService();
            BattleEntryRequest first = CreateRequest("kragmor");
            BattleEntryRequest duplicate = CreateRequest("another_boss");

            Assert.That(service.TryRegister(first), Is.True);
            Assert.That(service.TryRegister(duplicate), Is.False);
            Assert.That(service.TryGetCurrent(out BattleEntryRequest current),
                Is.True);
            Assert.That(current, Is.SameAs(first));
        }

        [Test]
        public void TryConsume_WithDifferentRequest_DoesNotRemoveCurrent()
        {
            var service = new BattleEntryService();
            BattleEntryRequest current = CreateRequest("kragmor");
            BattleEntryRequest different = CreateRequest("another_boss");
            service.TryRegister(current);

            Assert.That(service.TryConsume(different, out _), Is.False);
            Assert.That(service.TryCancel(different), Is.False);
            Assert.That(service.TryGetCurrent(out BattleEntryRequest remaining),
                Is.True);
            Assert.That(remaining, Is.SameAs(current));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RemovedRequest_AllowsIndependentNextRequest(bool consume)
        {
            var service = new BattleEntryService();
            BattleEntryRequest first = CreateRequest("kragmor");
            var second = new BattleEntryRequest(
                "next_boss",
                BattleDifficultyIds.Challenge);
            service.TryRegister(first);

            if (consume)
            {
                Assert.That(service.TryConsume(first, out _), Is.True);
            }
            else
            {
                Assert.That(service.TryCancel(first), Is.True);
            }

            Assert.That(service.TryRegister(second), Is.True);
            Assert.That(service.TryGetCurrent(out BattleEntryRequest current),
                Is.True);
            Assert.That(current, Is.SameAs(second));
            Assert.That(current.BossId, Is.EqualTo("next_boss"));
            Assert.That(
                current.DifficultyId,
                Is.EqualTo(BattleDifficultyIds.Challenge));
        }

        [Test]
        public void Bootstrapper_RuntimeServicesRemainStableWhenInitializedAgain()
        {
            var gameObject = new GameObject("Bootstrap Service Test");
            gameObject.SetActive(false);
            GameBootstrapper bootstrapper =
                gameObject.AddComponent<GameBootstrapper>();

            try
            {
                InvokeInitializeRuntimeServices(bootstrapper);
                BattleEntryService firstBattleEntryService =
                    bootstrapper.BattleEntryService;
                object firstSceneService = bootstrapper.SceneService;
                object firstRandomSource = bootstrapper.RandomSource;

                InvokeInitializeRuntimeServices(bootstrapper);

                Assert.That(firstBattleEntryService, Is.Not.Null);
                Assert.That(bootstrapper.BattleEntryService,
                    Is.SameAs(firstBattleEntryService));
                Assert.That(bootstrapper.SceneService,
                    Is.SameAs(firstSceneService));
                Assert.That(bootstrapper.RandomSource,
                    Is.SameAs(firstRandomSource));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static BattleEntryRequest CreateRequest(string bossId)
        {
            return new BattleEntryRequest(
                bossId,
                BattleDifficultyIds.Normal);
        }

        private static void InvokeInitializeRuntimeServices(
            GameBootstrapper bootstrapper)
        {
            typeof(GameBootstrapper).GetMethod(
                "InitializeRuntimeServices",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
                    bootstrapper,
                    null);
        }
    }
}
