using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "CharacterPresentationDefinition",
        menuName = "Valor Chronicle/Definitions/Character Presentation")]
    public sealed class CharacterPresentationDefinition : ScriptableObject
    {
        [SerializeField]
        private string characterId;

        [SerializeField]
        private Sprite faceSprite;

        [SerializeField]
        private Sprite previewSprite;

        public string CharacterId => characterId;
        public Sprite FaceSprite => faceSprite;
        public Sprite PreviewSprite => previewSprite;
    }
}
