namespace ValorChronicle.Battle.Flow.Presentation
{
    public static class BattleTimeScalePolicy
    {
        public const float NormalSceneTimeScale = 1f;
        public const float OneTimesTimeScale = 0.5f;
        public const float TwoTimesTimeScale = 1f;
        public const float ThreeTimesTimeScale = 1.5f;

        public static float Resolve(int battleSpeed)
        {
            switch (battleSpeed)
            {
                case 2:
                    return TwoTimesTimeScale;
                case 3:
                    return ThreeTimesTimeScale;
                default:
                    return OneTimesTimeScale;
            }
        }
    }
}
