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
            Assert.That(MareaBluefangRules.CharacterId,
                Is.EqualTo("character_marea_bluefang"));
            Assert.That(MareaBluefangRules.ActiveEffectId,
                Is.EqualTo("effect_marea_charge_order"));
            Assert.That(MareaBluefangRules.Match3Coefficient,
                Is.EqualTo(0.90d));
            Assert.That(MareaBluefangRules.Match4BaseCoefficient,
                Is.EqualTo(1.50d));
            Assert.That(
                MareaBluefangRules.Match4WaterElementBonusCoefficient,
                Is.EqualTo(0.40d));
            Assert.That(MareaBluefangRules.Match5BaseCoefficient,
                Is.EqualTo(2.40d));
            Assert.That(
                MareaBluefangRules.Match5CoefficientPerWaterElement,
                Is.EqualTo(1.40d));
            Assert.That(MareaBluefangRules.PassiveDealtDamageIncreaseRate,
                Is.EqualTo(0.15d));
            Assert.That(MareaBluefangRules.ActiveWaterDamageIncreaseRate,
                Is.EqualTo(0.25d));
            Assert.That(MareaBluefangRules.ActiveDurationTurns,
                Is.EqualTo(3));
            Assert.That(MareaBluefangRules.ActiveCooldownTurns,
                Is.EqualTo(8));
            Assert.That(MareaBluefangRules.WaterElementMaxAmount,
                Is.EqualTo(5));
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
        }
    }
}
