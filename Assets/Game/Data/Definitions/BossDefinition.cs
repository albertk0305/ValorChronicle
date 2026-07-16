using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "BossDefinition",
        menuName = "Valor Chronicle/Definitions/Boss")]
    public sealed class BossDefinition : GameDefinition
    {
        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private ElementType element;

        [SerializeField]
        private int turnLimit;

        [SerializeField]
        private string[] actionOrSkillIds = Array.Empty<string>();

        public string DisplayNameKey => displayNameKey;
        public ElementType Element => element;
        public int TurnLimit => turnLimit;
        public IReadOnlyList<string> ActionOrSkillIds =>
            Array.AsReadOnly(actionOrSkillIds ?? Array.Empty<string>());
    }
}
