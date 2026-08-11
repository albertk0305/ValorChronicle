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

        [SerializeField]
        private Sprite rock;

        public Sprite GetSprite(BoardBlock block)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (block.BlockType == BoardBlockType.Normal
                && block.Element.HasValue)
            {
                return GetSprite(block.Element.Value);
            }

            if (block.BlockType == BoardBlockType.Rock
                && !block.Element.HasValue)
            {
                if (rock == null)
                {
                    throw new InvalidOperationException(
                        "No board sprite is assigned for Rock.");
                }

                return rock;
            }

            throw new NotSupportedException(
                $"Board block type {block.BlockType} with Element "
                    + $"{block.Element?.ToString() ?? "<none>"} is not "
                    + "supported by the sprite set.");
        }

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
            Configure(
                fireSprite,
                waterSprite,
                grassSprite,
                lightSprite,
                darkSprite,
                rockSprite: null);
        }

        public void Configure(
            Sprite fireSprite,
            Sprite waterSprite,
            Sprite grassSprite,
            Sprite lightSprite,
            Sprite darkSprite,
            Sprite rockSprite)
        {
            fire = fireSprite;
            water = waterSprite;
            grass = grassSprite;
            light = lightSprite;
            dark = darkSprite;
            rock = rockSprite;
        }
    }
}
