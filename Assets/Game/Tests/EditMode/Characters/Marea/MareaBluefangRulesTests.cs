using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Marea
{
    public sealed class MareaBluefangRulesTests
    {
        private const string DefinitionPath =
            "Assets/Data/Characters/marea_bluefang.asset";

        [Test]
        public void AwakeningZeroRulesExposeConfirmedValues()
        {
            MareaBluefangCombatConfig config =
                MareaBluefangTestConfig.Canonical;

            Assert.That(config, Is.Not.Null);
            Assert.That(MareaBluefangRules.CharacterId,
                Is.EqualTo("character_marea_bluefang"));
            Assert.That(MareaBluefangRules.ActiveAbilityId,
                Is.EqualTo("active_marea_charge_order"));
            Assert.That(MareaBluefangRules.ActiveEffectId,
                Is.EqualTo("effect_marea_charge_order"));
            Assert.That(config.Match3Coefficient,
                Is.EqualTo(0.90d));
            Assert.That(config.Match4BaseCoefficient,
                Is.EqualTo(1.50d));
            Assert.That(
                config.Match4WaterElementBonusCoefficient,
                Is.EqualTo(0.40d));
            Assert.That(config.Match5BaseCoefficient,
                Is.EqualTo(2.40d));
            Assert.That(
                config.Match5CoefficientPerWaterElement,
                Is.EqualTo(1.40d));
            Assert.That(config.PassiveDealtDamageIncreaseRate,
                Is.EqualTo(0.15d));
            Assert.That(config.ActiveWaterDamageIncreaseRate,
                Is.EqualTo(0.25d));
            Assert.That(config.ActiveDurationTurns,
                Is.EqualTo(3));
            Assert.That(config.ActiveCooldownTurns,
                Is.EqualTo(8));
            Assert.That(config.WaterElementMaxAmount,
                Is.EqualTo(5));
            Assert.That(config.TryValidate(out _), Is.True);
        }

        [Test]
        public void DefinitionAssetUsesConfirmedIdentityAndStats()
        {
            CharacterDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    DefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Id,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Water));
            Assert.That(definition.Level1Hp, Is.EqualTo(900));
            Assert.That(definition.Level1Attack, Is.EqualTo(180));
            Assert.That(definition.Level100Hp, Is.EqualTo(3400));
            Assert.That(definition.Level100Attack, Is.EqualTo(1050));
            Assert.That(definition.CombatConfig,
                Is.SameAs(MareaBluefangTestConfig.Canonical));
        }
    }
}
