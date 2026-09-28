using System;

namespace ValorChronicle.Core.Scene
{
    public static class GameSceneNames
    {
        public static string GetName(GameScene scene)
        {
            return scene switch
            {
                GameScene.Init => "Init",
                GameScene.Main => "Main",
                GameScene.Battle => "Battle",
                GameScene.Party => "Party",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(scene),
                    scene,
                    "Unknown game scene.")
            };
        }
    }
}
