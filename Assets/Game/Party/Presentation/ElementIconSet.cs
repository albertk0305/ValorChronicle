using System;
using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Party.Presentation
{
    [CreateAssetMenu(
        fileName = "ElementIconSet",
        menuName = "Valor Chronicle/Party/Element Icon Set")]
    public sealed class ElementIconSet : ScriptableObject
    {
        [SerializeField] private Sprite fire;
        [SerializeField] private Sprite water;
        [SerializeField] private Sprite grass;
        [SerializeField] private Sprite light;
        [SerializeField] private Sprite dark;

        public bool IsConfigured =>
            fire != null
            && water != null
            && grass != null
            && light != null
            && dark != null;

        public Sprite GetIcon(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire:
                    return fire;
                case ElementType.Water:
                    return water;
                case ElementType.Grass:
                    return grass;
                case ElementType.Light:
                    return light;
                case ElementType.Dark:
                    return dark;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(element),
                        element,
                        "Unsupported element icon.");
            }
        }
    }
}
