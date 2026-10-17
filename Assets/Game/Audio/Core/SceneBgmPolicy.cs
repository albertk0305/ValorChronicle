using System;
using ValorChronicle.Core.Scene;

namespace ValorChronicle.Audio.Core
{
    public sealed class SceneBgmPolicy
    {
        public SceneAudioContext Resolve(string sceneName)
        {
            if (string.Equals(sceneName, GameSceneNames.GetName(GameScene.Init),
                    StringComparison.Ordinal))
                return SceneAudioContext.Init;
            if (string.Equals(sceneName, GameSceneNames.GetName(GameScene.Battle),
                    StringComparison.Ordinal))
                return SceneAudioContext.Battle;

            if (string.Equals(sceneName, GameSceneNames.GetName(GameScene.Main),
                    StringComparison.Ordinal)
                || string.Equals(sceneName, GameSceneNames.GetName(GameScene.Party),
                    StringComparison.Ordinal)
                || string.Equals(sceneName, GameSceneNames.GetName(GameScene.Character),
                    StringComparison.Ordinal)
                || string.Equals(sceneName, GameSceneNames.GetName(GameScene.Gacha),
                    StringComparison.Ordinal))
            {
                return SceneAudioContext.Normal;
            }

            return SceneAudioContext.Unknown;
        }
    }
}
