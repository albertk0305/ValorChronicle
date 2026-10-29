using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Flow.Presentation;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class BattleScreenShakePlayModeTests
    {
        [UnityTest]
        public IEnumerator ShakeDurationUsesUnscaledTimeAtAllBattleSpeeds()
        {
            float previousTimeScale = Time.timeScale;
            var owner = new GameObject(
                "BattleScreenShakePlayModeTests",
                typeof(RectTransform));
            var shake = owner.AddComponent<BattleScreenShake>();
            var measuredDurations = new float[3];
            float[] timeScales = { 0.5f, 1f, 1.5f };

            try
            {
                for (int index = 0; index < timeScales.Length; index++)
                {
                    Time.timeScale = timeScales[index];
                    Assert.That(shake.Shake(), Is.True);
                    float startedAt = Time.realtimeSinceStartup;
                    float deadline = startedAt + 1f;
                    while (shake.IsShaking
                        && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }

                    measuredDurations[index] =
                        Time.realtimeSinceStartup - startedAt;
                    Assert.That(shake.IsShaking, Is.False);
                    Assert.That(measuredDurations[index],
                        Is.EqualTo(
                                BattleScreenShake.DefaultDurationSeconds)
                            .Within(0.08f));
                }

                float minimum = Mathf.Min(
                    measuredDurations[0],
                    Mathf.Min(measuredDurations[1], measuredDurations[2]));
                float maximum = Mathf.Max(
                    measuredDurations[0],
                    Mathf.Max(measuredDurations[1], measuredDurations[2]));
                Assert.That(maximum - minimum, Is.LessThanOrEqualTo(0.05f));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.Destroy(owner);
            }
        }
    }
}
