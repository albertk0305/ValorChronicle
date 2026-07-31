using System;
using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board.Presentation
{
    [CreateAssetMenu(
        fileName = "BoardElementSpriteSet",
        menuName = "Valor Chronicle/Battle/Board Element Sprite Set")]
    public sealed class BoardElementSpriteSet : ScriptableObject
    {
        [SerializeField]
        private Sprite fire;

        [SerializeField]
        private Sprite water;

        [SerializeField]
        private Sprite grass;

        [SerializeField]
        private Sprite light;

        [SerializeField]
        private Sprite dark;

        public Sprite GetSprite(ElementType element)
        {
            Sprite sprite;

            switch (element)
            {
                case ElementType.Fire:
                    sprite = fire;
                    break;
                case ElementType.Water:
                    sprite = water;
                    break;
                case ElementType.Grass:
                    sprite = grass;
                    break;
                case ElementType.Light:
                    sprite = light;
                    break;
                case ElementType.Dark:
                    sprite = dark;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(element),
                        element,
                        "Unsupported board element.");
            }

            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"No board sprite is assigned for {element}.");
            }

            return sprite;
        }

        public void Configure(
            Sprite fireSprite,
            Sprite waterSprite,
            Sprite grassSprite,
            Sprite lightSprite,
            Sprite darkSprite)
        {
            fire = fireSprite;
            water = waterSprite;
            grass = grassSprite;
            light = lightSprite;
            dark = darkSprite;
        }
    }
}
