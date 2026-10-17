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
    }
}
