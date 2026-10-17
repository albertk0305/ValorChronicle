using NUnit.Framework;
using ValorChronicle.Audio.Core;

namespace ValorChronicle.Tests.EditMode.Audio
{
    public sealed class SceneBgmPolicyTests
    {
        [TestCase("Init", SceneAudioContext.Init)]
        [TestCase("Battle", SceneAudioContext.Battle)]
        [TestCase("Main", SceneAudioContext.Normal)]
        [TestCase("Party", SceneAudioContext.Normal)]
        [TestCase("Character", SceneAudioContext.Normal)]
        [TestCase("Gacha", SceneAudioContext.Normal)]
        [TestCase("UnknownScene", SceneAudioContext.Unknown)]
        public void Resolve_MapsActualProjectSceneNames(
            string sceneName,
            SceneAudioContext expected)
        {
            Assert.That(new SceneBgmPolicy().Resolve(sceneName),
                Is.EqualTo(expected));
        }
    }
}
