using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Flow.Presentation;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleScreenShakeTests
    {
        private GameObject owner;
        private RectTransform rectTransform;
        private BattleScreenShake shake;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject(
                "BattleScreenShakeTests",
                typeof(RectTransform));
            rectTransform = owner.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(12f, -5f);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, 7f);
            rectTransform.localScale = new Vector3(1.1f, 0.9f, 1f);
            shake = owner.AddComponent<BattleScreenShake>();
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Shake_MovesBothAxesWithinMaximumAndRestoresAtDuration()
        {
            Vector2 origin = rectTransform.anchoredPosition;
            Quaternion rotation = rectTransform.localRotation;
            Vector3 scale = rectTransform.localScale;

            Assert.That(shake.Shake(), Is.True);

            Assert.That(shake.IsShaking, Is.True);
            Assert.That(Mathf.Abs(shake.CurrentOffset.x), Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(shake.CurrentOffset.y), Is.GreaterThan(0f));
            Assert.That(shake.CurrentOffset.magnitude,
                Is.LessThanOrEqualTo(
                    BattleScreenShake.DefaultMaximumOffset + 0.0001f));

            shake.Advance(BattleScreenShake.DefaultDurationSeconds);

            Assert.That(shake.IsShaking, Is.False);
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(origin));
            Assert.That(rectTransform.localRotation, Is.EqualTo(rotation));
            Assert.That(rectTransform.localScale, Is.EqualTo(scale));
        }

        [Test]
        public void Advance_UsesDecayingEnvelope()
        {
            Assert.That(shake.Shake(), Is.True);
            float initialMagnitude = shake.CurrentOffset.magnitude;

            shake.Advance(0.10f);
            float lateMagnitude = shake.CurrentOffset.magnitude;

            Assert.That(initialMagnitude,
                Is.EqualTo(BattleScreenShake.DefaultMaximumOffset)
                    .Within(0.0001f));
            Assert.That(lateMagnitude, Is.LessThan(initialMagnitude));
            Assert.That(lateMagnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void RepeatedShake_DoesNotAccumulateDisplacedPosition()
        {
            Vector2 origin = rectTransform.anchoredPosition;
            Assert.That(shake.Shake(), Is.True);
            shake.Advance(0.05f);
            Assert.That(rectTransform.anchoredPosition,
                Is.Not.EqualTo(origin));

            Assert.That(shake.Shake(), Is.True);
            shake.Advance(BattleScreenShake.DefaultDurationSeconds);

            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(origin));
        }

        [Test]
        public void NewShake_CapturesExternalIdlePositionChange()
        {
            Vector2 updatedOrigin = new Vector2(-18f, 23f);
            rectTransform.anchoredPosition = updatedOrigin;

            Assert.That(shake.Shake(), Is.True);
            shake.Advance(BattleScreenShake.DefaultDurationSeconds);

            Assert.That(rectTransform.anchoredPosition,
                Is.EqualTo(updatedOrigin));
        }

        [Test]
        public void StopAndRestore_IsImmediateAndIdempotent()
        {
            Vector2 origin = rectTransform.anchoredPosition;
            Assert.That(shake.Shake(), Is.True);

            shake.StopAndRestore();
            shake.StopAndRestore();

            Assert.That(shake.IsShaking, Is.False);
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(origin));
        }

        [Test]
        public void ExplicitDisableAndDestroyLifecycle_RestorePosition()
        {
            Vector2 origin = rectTransform.anchoredPosition;
            Assert.That(shake.Shake(), Is.True);
            InvokePrivate(shake, "OnDisable");
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(origin));

            Assert.That(shake.Shake(), Is.True);
            InvokePrivate(shake, "OnDestroy");
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(origin));
            Assert.That(shake.IsShaking, Is.False);
        }

        [Test]
        public void DisabledComponent_DoesNotStartShake()
        {
            Vector2 origin = rectTransform.anchoredPosition;
            shake.enabled = false;

            Assert.That(shake.Shake(), Is.False);
            Assert.That(shake.IsShaking, Is.False);
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(origin));
        }

        [Test]
        public void InvalidSerializedValues_FallBackToSafeDefaults()
        {
            SetField(shake, "durationSeconds", float.NaN);
            SetField(shake, "maximumOffset", -1f);
            Vector2 origin = rectTransform.anchoredPosition;

            Assert.That(shake.Shake(), Is.True);
            Assert.That(shake.DurationSeconds,
                Is.EqualTo(BattleScreenShake.DefaultDurationSeconds));
            Assert.That(shake.MaximumOffset,
                Is.EqualTo(BattleScreenShake.DefaultMaximumOffset));
            Assert.That(shake.CurrentOffset.magnitude,
                Is.LessThanOrEqualTo(
                    BattleScreenShake.DefaultMaximumOffset + 0.0001f));
            shake.Advance(BattleScreenShake.DefaultDurationSeconds);
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(origin));
        }

        [Test]
        public void InvalidAdvanceDelta_DoesNotCorruptShakeState()
        {
            Assert.That(shake.Shake(), Is.True);
            Vector2 position = rectTransform.anchoredPosition;

            shake.Advance(float.NaN);
            shake.Advance(float.PositiveInfinity);
            shake.Advance(-1f);

            Assert.That(shake.IsShaking, Is.True);
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(position));
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }
    }
}
