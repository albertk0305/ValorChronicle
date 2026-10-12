using UnityEngine;

namespace ValorChronicle.Relics.Presentation
{
    public sealed class RelicPresentationModel
    {
        private RelicPresentationModel(
            string instanceId,
            Sprite icon,
            int awakening,
            string description,
            bool isEmpty)
        {
            InstanceId = instanceId;
            Icon = icon;
            Awakening = awakening;
            Description = description ?? string.Empty;
            IsEmpty = isEmpty;
        }

        public string InstanceId { get; }
        public Sprite Icon { get; }
        public int Awakening { get; }
        public string Description { get; }
        public bool IsEmpty { get; }

        public static RelicPresentationModel Empty()
        {
            return new RelicPresentationModel(
                string.Empty,
                icon: null,
                awakening: 0,
                description: string.Empty,
                isEmpty: true);
        }

        public static RelicPresentationModel Equipped(
            string instanceId,
            Sprite icon,
            int awakening,
            string description = "")
        {
            return new RelicPresentationModel(
                instanceId,
                icon,
                awakening,
                description,
                isEmpty: false);
        }
    }
}
