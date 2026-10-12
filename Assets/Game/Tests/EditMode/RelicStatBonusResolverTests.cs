using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Stats;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicStatBonusResolverTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [TestCase(0, 0.05d)]
        [TestCase(5, 0.10d)]
        public void Resolve_BarkUsesEquippedInstanceAwakening(
            int awakening,
            double expectedRate)
        {
            RelicDefinition bark = Definition(
                "relic_world_tree_bark",
                RelicEffectType.MaxHpIncrease,
                Rates(0.05d));
            CharacterSaveData character = Character("bark_instance");
            ProfileSaveData profile = Profile(
                character,
                Instance("bark_instance", bark.Id, awakening));

            RelicStatBonusSummary result = Resolver(bark).Resolve(
                profile,
                character);

            Assert.That(result.MaxHpIncreaseRateSum, Is.EqualTo(expectedRate));
            Assert.That(result.AttackIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void Resolve_NoEquipOrSwordOnlyHasNoMaxHpBonus()
        {
            RelicDefinition sword = Definition(
                "relic_victors_broken_sword",
                RelicEffectType.AttackIncrease,
                Rates(0.05d));
            CharacterSaveData empty = Character();
            CharacterSaveData swordUser = Character("sword_instance");
            ProfileSaveData profile = Profile(
                swordUser,
                Instance("sword_instance", sword.Id, 5));
            RelicStatBonusResolver resolver = Resolver(sword);

            Assert.That(
                resolver.Resolve(profile, empty).MaxHpIncreaseRateSum,
                Is.Zero);
            RelicStatBonusSummary swordResult = resolver.Resolve(
                profile,
                swordUser);
            Assert.That(swordResult.MaxHpIncreaseRateSum, Is.Zero);
            Assert.That(swordResult.AttackIncreaseRateSum, Is.EqualTo(0.10d));
        }

        [TestCase(0, 0.05d)]
        [TestCase(5, 0.10d)]
        public void Resolve_SwordUsesEquippedInstanceAwakening(
            int awakening,
            double expectedRate)
        {
            RelicDefinition sword = Definition(
                "relic_victors_broken_sword",
                RelicEffectType.AttackIncrease,
                Rates(0.05d));
            CharacterSaveData character = Character("sword_instance");
            ProfileSaveData profile = Profile(
                character,
                Instance("sword_instance", sword.Id, awakening));

            RelicStatBonusSummary result = Resolver(sword).Resolve(
                profile,
                character);

            Assert.That(result.AttackIncreaseRateSum,
                Is.EqualTo(expectedRate));
            Assert.That(result.MaxHpIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void Resolve_OtherCharactersSwordDoesNotLeak()
        {
            RelicDefinition sword = Definition(
                "sword",
                RelicEffectType.AttackIncrease,
                Rates(0.05d));
            CharacterSaveData target = Character();
            CharacterSaveData other = Character("sword_instance");
            other.CharacterId = "character_b";
            ProfileSaveData profile = Profile(
                target,
                Instance("sword_instance", sword.Id, 5));
            profile.Characters.Add(other);

            RelicStatBonusSummary result = Resolver(sword).Resolve(
                profile,
                target);

            Assert.That(result.AttackIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void Resolve_MultipleAttackDefinitionsSumRates()
        {
            RelicDefinition first = Definition(
                "attack_first",
                RelicEffectType.AttackIncrease,
                Rates(0.01d));
            RelicDefinition second = Definition(
                "attack_second",
                RelicEffectType.AttackIncrease,
                Rates(0.02d));
            CharacterSaveData character = Character("first", "second");
            ProfileSaveData profile = Profile(
                character,
                Instance("first", first.Id, 0),
                Instance("second", second.Id, 5));

            RelicStatBonusSummary result = Resolver(first, second).Resolve(
                profile,
                character);

            Assert.That(
                result.AttackIncreaseRateSum,
                Is.EqualTo(0.08d).Within(1e-12d));
        }

        [Test]
        public void Resolve_OtherCharactersEquipDoesNotLeakAndSlotOrderDoesNotMatter()
        {
            RelicDefinition bark = Definition(
                "bark",
                RelicEffectType.MaxHpIncrease,
                Rates(0.05d));
            CharacterSaveData target = Character();
            CharacterSaveData other = Character("bark_a", "bark_b");
            ProfileSaveData profile = Profile(
                target,
                Instance("bark_a", bark.Id, 0),
                Instance("bark_b", bark.Id, 5));
            profile.Characters.Add(other);
            RelicStatBonusResolver resolver = Resolver(bark);

            Assert.That(
                resolver.Resolve(profile, target).MaxHpIncreaseRateSum,
                Is.Zero);
            double first = resolver.Resolve(profile, other)
                .MaxHpIncreaseRateSum;
            other.EquippedRelicInstanceIds[0] = "bark_b";
            other.EquippedRelicInstanceIds[1] = "bark_a";
            Assert.That(first, Is.EqualTo(0.15d).Within(1e-12d));
            Assert.That(
                resolver.Resolve(profile, other).MaxHpIncreaseRateSum,
                Is.EqualTo(first).Within(1e-12d));
        }

        [Test]
        public void Resolve_MultipleMaxHpDefinitionsSumBeforeBuild()
        {
            RelicDefinition first = Definition(
                "max_hp_first",
                RelicEffectType.MaxHpIncrease,
                Rates(0.01d));
            RelicDefinition second = Definition(
                "max_hp_second",
                RelicEffectType.MaxHpIncrease,
                Rates(0.02d));
            CharacterSaveData character = Character("first", "second");
            ProfileSaveData profile = Profile(
                character,
                Instance("first", first.Id, 0),
                Instance("second", second.Id, 5));

            RelicStatBonusSummary result = Resolver(first, second).Resolve(
                profile,
                character);

            Assert.That(
                result.MaxHpIncreaseRateSum,
                Is.EqualTo(0.08d).Within(1e-12d));
        }

        [Test]
        public void Resolve_IgnoresLegacyEquipFields()
        {
            RelicDefinition bark = Definition(
                "bark",
                RelicEffectType.MaxHpIncrease,
                Rates(0.05d));
            CharacterSaveData character = Character();
            RelicInstanceSaveData legacy = Instance("bark", bark.Id, 5);
            legacy.EquippedCharacterId = "character_a";
            legacy.EquippedSlotIndex = 0;

            RelicStatBonusSummary result = Resolver(bark).Resolve(
                Profile(character, legacy),
                character);

            Assert.That(result.MaxHpIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void Resolve_MalformedEntriesAreIgnoredWithoutMutation()
        {
            RelicDefinition invalid = Definition(
                "invalid",
                RelicEffectType.MaxHpIncrease,
                new[] { -0.1d });
            CharacterSaveData character = Character(
                "missing",
                "bad_awake",
                "duplicate",
                "invalid_rate");
            ProfileSaveData profile = Profile(
                character,
                Instance("bad_awake", invalid.Id, 99),
                Instance("duplicate", invalid.Id, 0),
                Instance("duplicate", invalid.Id, 0),
                Instance("invalid_rate", invalid.Id, 0));
            string[] originalSlots = character.EquippedRelicInstanceIds.ToArray();

            RelicStatBonusSummary result = Resolver(invalid).Resolve(
                profile,
                character);

            Assert.That(result.MaxHpIncreaseRateSum, Is.Zero);
            Assert.That(character.EquippedRelicInstanceIds,
                Is.EqualTo(originalSlots));
            Assert.That(profile.RelicInstances.Count, Is.EqualTo(4));
        }

        private RelicStatBonusResolver Resolver(
            params RelicDefinition[] definitions)
        {
            DefinitionDatabase database = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            createdObjects.Add(database);
            SetField(database, "relics", definitions);
            database.Initialize();
            return new RelicStatBonusResolver(database);
        }

        private RelicDefinition Definition(
            string id,
            RelicEffectType effectType,
            double[] rates)
        {
            RelicDefinition definition = ScriptableObject.CreateInstance<
                RelicDefinition>();
            createdObjects.Add(definition);
            SetField(definition, typeof(GameDefinition), "id", id);
            SetField(definition, typeof(RelicDefinition), "effectType", effectType);
            SetField(definition, typeof(RelicDefinition), "effectRates", rates);
            return definition;
        }

        private static CharacterSaveData Character(params string[] slots)
        {
            List<string> equipped = SaveRules.CreateEmptyRelicInstanceIdSlots();
            for (int index = 0; index < slots.Length; index++)
            {
                equipped[index] = slots[index];
            }

            return new CharacterSaveData
            {
                CharacterId = "character_a",
                Level = 1,
                Awakening = 0,
                EquippedRelicInstanceIds = equipped
            };
        }

        private static ProfileSaveData Profile(
            CharacterSaveData character,
            params RelicInstanceSaveData[] instances)
        {
            return new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { character },
                RelicInstances = new List<RelicInstanceSaveData>(instances)
            };
        }

        private static RelicInstanceSaveData Instance(
            string instanceId,
            string definitionId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = definitionId,
                Awakening = awakening
            };
        }

        private static double[] Rates(double first)
        {
            return new[]
            {
                first,
                first + 0.01d,
                first + 0.02d,
                first + 0.03d,
                first + 0.04d,
                first + 0.05d
            };
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            SetField(target, target.GetType(), fieldName, value);
        }

        private static void SetField(
            object target,
            Type declaringType,
            string fieldName,
            object value)
        {
            FieldInfo field = declaringType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
