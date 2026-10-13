using System;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Exchange.Presentation
{
    public sealed class ExchangeSpriteResolver
    {
        private readonly CharacterPresentationCatalog characterCatalog;
        private readonly DefinitionDatabase database;
        private readonly Sprite heroTokenSprite;
        private readonly Sprite relicTokenSprite;

        public ExchangeSpriteResolver(
            CharacterPresentationCatalog characterCatalog,
            DefinitionDatabase database,
            Sprite heroTokenSprite,
            Sprite relicTokenSprite)
        {
            this.characterCatalog = characterCatalog
                ?? throw new ArgumentNullException(
                    nameof(characterCatalog));
            this.database = database
                ?? throw new ArgumentNullException(nameof(database));
            if (!database.IsInitialized)
            {
                throw new ArgumentException(
                    "Definition database must be initialized.",
                    nameof(database));
            }

            if (!characterCatalog.IsInitialized)
                characterCatalog.Initialize();

            this.heroTokenSprite = heroTokenSprite;
            this.relicTokenSprite = relicTokenSprite;
        }

        public Sprite ResolveContent(ExchangeListItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.ContentKind == ExchangeContentKind.Character)
            {
                return characterCatalog.TryGet(
                    item.ContentId,
                    out CharacterPresentationDefinition presentation)
                        ? presentation.FaceSprite
                        : null;
            }

            return database.TryGetRelicPresentation(
                item.ContentId,
                out RelicPresentationDefinition relicPresentation)
                    ? relicPresentation.Icon
                    : null;
        }

        public Sprite ResolveCurrency(ExchangeCurrencyKind currencyKind)
        {
            switch (currencyKind)
            {
                case ExchangeCurrencyKind.HeroTokens:
                    return heroTokenSprite;
                case ExchangeCurrencyKind.RelicTokens:
                    return relicTokenSprite;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(currencyKind),
                        currencyKind,
                        "Exchange currency kind is invalid.");
            }
        }
    }
}
