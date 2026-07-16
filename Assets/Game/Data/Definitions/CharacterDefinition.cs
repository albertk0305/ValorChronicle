using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "CharacterDefinition",
        menuName = "Valor Chronicle/Definitions/Character")]
    public sealed class CharacterDefinition : GameDefinition
    {
        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private ElementType element;

        [SerializeField]
        private int level1Hp;

        [SerializeField]
        private int level1Attack;

        [SerializeField]
        private int level100Hp;

        [SerializeField]
        private int level100Attack;

        [SerializeField]
        private string[] skillIds = Array.Empty<string>();

        public string DisplayNameKey => displayNameKey;
        public ElementType Element => element;
        public int Level1Hp => level1Hp;
        public int Level1Attack => level1Attack;
        public int Level100Hp => level100Hp;
        public int Level100Attack => level100Attack;
        public IReadOnlyList<string> SkillIds =>
            Array.AsReadOnly(skillIds ?? Array.Empty<string>());
    }
}
