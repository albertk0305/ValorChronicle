using ValorChronicle.Rewards;

namespace ValorChronicle.Characters.Acquisition
{
    public enum CharacterAcquisitionKind
    {
        NewCharacter = 0,
        AwakeningIncreased = 1,
        MaxAwakeningConvertedToHeroTokens = 2
    }

    public sealed class CharacterAcquisitionResult
    {
        internal CharacterAcquisitionResult(
            CharacterAcquisitionKind kind,
            string characterId,
            int? previousAwakening,
            int newAwakening,
            CurrencyGrantResult currencyGrant)
        {
            Kind = kind;
            CharacterId = characterId;
            PreviousAwakening = previousAwakening;
            NewAwakening = newAwakening;
            CurrencyGrant = currencyGrant;
        }

        public CharacterAcquisitionKind Kind { get; }
        public string CharacterId { get; }
        public int? PreviousAwakening { get; }
        public int NewAwakening { get; }
        public CurrencyGrantResult CurrencyGrant { get; }
    }
}
