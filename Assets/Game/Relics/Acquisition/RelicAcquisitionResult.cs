namespace ValorChronicle.Relics.Acquisition
{
    public enum RelicAcquisitionKind
    {
        NewInstanceCreated = 0,
        AwakeningIncreased = 1,
        NewInstanceCreatedAfterMaxAwakening = 2
    }

    /// <summary>Describes the profile change made by one relic grant.</summary>
    public sealed class RelicAcquisitionResult
    {
        internal RelicAcquisitionResult(
            RelicAcquisitionKind kind,
            string relicDefinitionId,
            string affectedInstanceId,
            int? previousAwakening,
            int newAwakening)
        {
            Kind = kind;
            RelicDefinitionId = relicDefinitionId;
            AffectedInstanceId = affectedInstanceId;
            PreviousAwakening = previousAwakening;
            NewAwakening = newAwakening;
        }

        public RelicAcquisitionKind Kind { get; }
        public string RelicDefinitionId { get; }
        public string AffectedInstanceId { get; }
        public int? PreviousAwakening { get; }
        public int NewAwakening { get; }
    }
}
