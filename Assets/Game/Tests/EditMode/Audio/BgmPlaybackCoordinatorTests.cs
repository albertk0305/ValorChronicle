using NUnit.Framework;
using ValorChronicle.Audio.Core;

namespace ValorChronicle.Tests.EditMode.Audio
{
    public sealed class BgmPlaybackCoordinatorTests
    {
        [Test]
        public void InitScene_SelectsInitTrack()
        {
            var output = new RecordingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.ApplyScene(SceneAudioContext.Init);

            Assert.That(coordinator.CurrentTrack, Is.EqualTo(BgmTrackKind.Init));
            Assert.That(output.PlaySpecialCount, Is.EqualTo(1));
            Assert.That(output.LastSpecialTrack, Is.EqualTo(BgmTrackKind.Init));
        }

        [Test]
        public void NormalSceneChanges_DoNotRestartNormalTrack()
        {
            var output = new RecordingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.ApplyScene(SceneAudioContext.Normal);
            coordinator.ApplyScene(SceneAudioContext.Normal);
            coordinator.ApplyScene(SceneAudioContext.Normal);

            Assert.That(output.PlayNormalCount, Is.EqualTo(1));
            Assert.That(output.StopNormalCount, Is.Zero);
        }

        [Test]
        public void NormalBattleNormal_PausesAndResumesNormalTrack()
        {
            var output = new RecordingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.ApplyScene(SceneAudioContext.Normal);
            coordinator.ApplyScene(SceneAudioContext.Battle);
            coordinator.ApplyScene(SceneAudioContext.Normal);

            Assert.That(output.PlayNormalCount, Is.EqualTo(1));
            Assert.That(output.PauseNormalCount, Is.EqualTo(1));
            Assert.That(output.ResumeNormalCount, Is.EqualTo(1));
            Assert.That(output.PlaySpecialCount, Is.EqualTo(1));
            Assert.That(output.LastSpecialTrack, Is.EqualTo(BgmTrackKind.Battle));
            Assert.That(output.StopSpecialCount, Is.EqualTo(1));
        }

        [Test]
        public void DirectBattleThenNormal_StartsNormalFromBeginning()
        {
            var output = new RecordingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.ApplyScene(SceneAudioContext.Battle);
            coordinator.ApplyScene(SceneAudioContext.Normal);

            Assert.That(output.PauseNormalCount, Is.Zero);
            Assert.That(output.ResumeNormalCount, Is.Zero);
            Assert.That(output.PlayNormalCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateBattleEvents_DoNotRestartBattleTrack()
        {
            var output = new RecordingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.ApplyScene(SceneAudioContext.Battle);
            coordinator.ApplyScene(SceneAudioContext.Battle);

            Assert.That(output.PlaySpecialCount, Is.EqualTo(1));
        }

        [Test]
        public void ManualPause_DoesNotChangePitchOrScenePolicyState()
        {
            var output = new RecordingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);
            coordinator.ApplyScene(SceneAudioContext.Battle);

            coordinator.PauseCurrent();
            coordinator.ResumeCurrent();

            Assert.That(output.PauseSpecialCount, Is.EqualTo(1));
            Assert.That(output.ResumeSpecialCount, Is.EqualTo(1));
            Assert.That(coordinator.CurrentTrack,
                Is.EqualTo(BgmTrackKind.Battle));
        }

        [Test]
        public void FadingBackend_CrossfadesAndPreservesNormalForBattleOnly()
        {
            var output = new RecordingFadingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.ApplyScene(SceneAudioContext.Normal);
            coordinator.ApplyScene(SceneAudioContext.Battle);

            Assert.That(output.CrossfadeToSpecialCount, Is.EqualTo(1));
            Assert.That(output.LastSpecialTrack,
                Is.EqualTo(BgmTrackKind.Battle));
            Assert.That(output.LastPreserveNormal, Is.True);
            Assert.That(output.LastDuration,
                Is.EqualTo(0.8f).Within(0.0001f));

            coordinator.ApplyScene(SceneAudioContext.Normal);
            Assert.That(output.CrossfadeToNormalCount, Is.EqualTo(1));
            coordinator.ApplyScene(SceneAudioContext.Init);
            Assert.That(output.CrossfadeToSpecialCount, Is.EqualTo(2));
            Assert.That(output.LastSpecialTrack, Is.EqualTo(BgmTrackKind.Init));
            Assert.That(output.LastPreserveNormal, Is.False);
        }

        [Test]
        public void FadingBackend_DuplicateContextsDoNotDuplicateCrossfade()
        {
            var output = new RecordingFadingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.ApplyScene(SceneAudioContext.Normal);
            coordinator.ApplyScene(SceneAudioContext.Normal);
            coordinator.ApplyScene(SceneAudioContext.Battle);
            coordinator.ApplyScene(SceneAudioContext.Battle);

            Assert.That(output.PlayNormalCount, Is.EqualTo(1));
            Assert.That(output.CrossfadeToSpecialCount, Is.EqualTo(1));
        }

        [Test]
        public void InitialInit_UsesDedicatedFadeOnlyOnce()
        {
            var output = new RecordingFadingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            Assert.That(coordinator.StartInitialInit(), Is.True);
            Assert.That(coordinator.StartInitialInit(), Is.False);

            Assert.That(output.InitialInitCount, Is.EqualTo(1));
            Assert.That(output.PlaySpecialCount, Is.Zero);
            Assert.That(output.LastDuration, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(coordinator.CurrentTrack, Is.EqualTo(BgmTrackKind.Init));
        }

        [Test]
        public void StartupFadeClock_IgnoresTimeBeforePlaybackAndFirstStartFrame()
        {
            var clock = new BgmStartupFadeClock();
            clock.Begin(0.6f);

            clock.Advance(5f);
            Assert.That(clock.Gain, Is.Zero);
            Assert.That(clock.TryStartPlayback(isReady: false), Is.False);
            Assert.That(clock.TryStartPlayback(isReady: true), Is.True);
            Assert.That(clock.Gain, Is.Zero);

            clock.Advance(0.3f);
            Assert.That(clock.Gain, Is.EqualTo(0.5f).Within(0.0001f));
            clock.Advance(0.3f);
            Assert.That(clock.Gain, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(clock.IsFading, Is.False);
        }

        [Test]
        public void BattleResultFade_IsIdempotentAndResetsForNextBattle()
        {
            var output = new RecordingFadingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);
            coordinator.ApplyScene(SceneAudioContext.Battle);

            Assert.That(coordinator.FadeOutBattleResult(), Is.True);
            Assert.That(coordinator.FadeOutBattleResult(), Is.False);
            Assert.That(output.ResultFadeCount, Is.EqualTo(1));
            Assert.That(output.LastDuration,
                Is.EqualTo(1.2f).Within(0.0001f));

            coordinator.ApplyScene(SceneAudioContext.Normal);
            coordinator.ApplyScene(SceneAudioContext.Battle);
            Assert.That(coordinator.FadeOutBattleResult(), Is.True);
            Assert.That(output.ResultFadeCount, Is.EqualTo(2));
        }

        [Test]
        public void FadeAdvance_UsesCallerSuppliedUnscaledTime()
        {
            var output = new RecordingFadingAudioOutput();
            var coordinator = new BgmPlaybackCoordinator(output);

            coordinator.Advance(0.25f);
            coordinator.Advance(0.5f);

            Assert.That(output.AdvancedSeconds,
                Is.EqualTo(0.75f).Within(0.0001f));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => coordinator.Advance(-0.01f));
        }

        [Test]
        public void LoopGain_FadesBoundariesAndStaysFullInMiddle()
        {
            var settings = new BgmLoopFadeSettings(true, 0.6f, 0.6f);

            Assert.That(BgmFadeMath.EvaluateLoopGain(0f, 10f, settings),
                Is.Zero);
            Assert.That(BgmFadeMath.EvaluateLoopGain(0.3f, 10f, settings),
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(BgmFadeMath.EvaluateLoopGain(5f, 10f, settings),
                Is.EqualTo(1f));
            Assert.That(BgmFadeMath.EvaluateLoopGain(9.7f, 10f, settings),
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(BgmFadeMath.EvaluateLoopGain(10f, 10f, settings),
                Is.Zero);
        }

        [Test]
        public void ShortClip_ScalesLoopFadesToMeetAtFullGain()
        {
            var settings = new BgmLoopFadeSettings(true, 0.6f, 0.6f);

            Assert.That(BgmFadeMath.EvaluateLoopGain(0f, 0.3f, settings),
                Is.Zero);
            Assert.That(BgmFadeMath.EvaluateLoopGain(0.15f, 0.3f, settings),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BgmFadeMath.EvaluateLoopGain(0.3f, 0.3f, settings),
                Is.Zero);
        }

        [Test]
        public void DisabledLoopFade_AlwaysReturnsFullGain()
        {
            var settings = new BgmLoopFadeSettings(false, 0.6f, 0.6f);

            Assert.That(BgmFadeMath.EvaluateLoopGain(0f, 10f, settings),
                Is.EqualTo(1f));
            Assert.That(BgmFadeMath.EvaluateLoopGain(10f, 10f, settings),
                Is.EqualTo(1f));
        }

        [Test]
        public void TransitionGain_CanReverseFromCapturedGain()
        {
            float halfway = BgmFadeMath.EvaluateLinear(1f, 0f, 0.4f, 0.8f);
            float reversed = BgmFadeMath.EvaluateLinear(
                halfway,
                1f,
                0.4f,
                0.8f);

            Assert.That(halfway, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(reversed, Is.EqualTo(0.75f).Within(0.0001f));
        }
    }
}
