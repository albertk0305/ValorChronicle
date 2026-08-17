using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class DefinitionDatabaseTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Initialize_ResolvesAllRegisteredDefinitionKinds()
        {
            CharacterDefinition character =
                CreateGameDefinition<CharacterDefinition>("character_test");
            BossDefinition boss =
                CreateGameDefinition<BossDefinition>("boss_test");
            SkillDefinition skill =
                CreateGameDefinition<SkillDefinition>("skill_test");
            EffectDefinition effect =
                CreateGameDefinition<EffectDefinition>("effect_test");
            ResourceDefinition resource =
                CreateGameDefinition<ResourceDefinition>("resource_test");
            RelicDefinition relic =
                CreateGameDefinition<RelicDefinition>("relic_test");
            BossPresentationDefinition presentation =
                CreatePresentation("boss_test");
            DefinitionDatabase database = CreateDatabase(
                new[] { character },
                new[] { boss },
                new[] { skill },
                new[] { effect },
                new[] { resource },
                new[] { relic },
                new[] { presentation });

            database.Initialize();

            Assert.That(database.TryGetCharacter(character.Id, out var foundCharacter), Is.True);
            Assert.That(foundCharacter, Is.SameAs(character));
            Assert.That(database.TryGetBoss(boss.Id, out var foundBoss), Is.True);
            Assert.That(foundBoss, Is.SameAs(boss));
            Assert.That(database.TryGetSkill(skill.Id, out var foundSkill), Is.True);
            Assert.That(foundSkill, Is.SameAs(skill));
            Assert.That(database.TryGetEffect(effect.Id, out var foundEffect), Is.True);
            Assert.That(foundEffect, Is.SameAs(effect));
            Assert.That(database.TryGetResource(resource.Id, out var foundResource), Is.True);
            Assert.That(foundResource, Is.SameAs(resource));
            Assert.That(database.TryGetRelic(relic.Id, out var foundRelic), Is.True);
            Assert.That(foundRelic, Is.SameAs(relic));
            Assert.That(database.TryGetBossPresentation(boss.Id, out var foundPresentation), Is.True);
            Assert.That(foundPresentation, Is.SameAs(presentation));
        }

        [Test]
        public void CanonicalDatabaseContainsRuntimePresentationIds()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    "Assets/Data/Database/DefinitionDatabase.asset");
            Assert.That(database, Is.Not.Null);
            database.Initialize();

            string[] bossSkillIds =
            {
                KragmorRules.ColossusIronFistSkillId,
                KragmorRules.RockshardEruptionSkillId,
                KragmorRules.CoreCompressionSkillId,
                KragmorRules.EarthCollapseSkillId
            };
            foreach (string skillId in bossSkillIds)
            {
                Assert.That(database.TryGetSkill(
                    skillId,
                    out SkillDefinition skill), Is.True, skillId);
                Assert.That(skill.SkillKind, Is.EqualTo(SkillKind.BossAction));
            }

            Assert.That(database.TryGetSkill(
                MareaBluefangRules.ActiveAbilityId,
                out SkillDefinition active), Is.True);
            Assert.That(active.SkillKind, Is.EqualTo(SkillKind.Active));
            Assert.That(database.TryGetEffect(
                MareaBluefangRules.ActiveEffectId,
                out _), Is.True);
            Assert.That(database.TryGetEffect(
                KragmorRules.VolcanicCarapaceEffectId,
                out _), Is.True);
            Assert.That(database.TryGetEffect(
                KragmorRules.CoreCompressionEffectId,
                out _), Is.True);
            Assert.That(database.TryGetEffect(
                KragmorRules.CoreExposureEffectId,
                out _), Is.True);
            Assert.That(database.TryGetResource(
                WaterElementResource.Id,
                out _), Is.True);
            Assert.That(database.TryGetBossPresentation(
                KragmorRules.BossId,
                out BossPresentationDefinition presentation), Is.True);
            Assert.That(presentation.DefaultSprite, Is.Not.Null);
            Assert.That(presentation.TryGetStateSprite(
                KragmorRules.CoreExposureVisualStateId,
                out Sprite exposureSprite), Is.True);
            Assert.That(exposureSprite, Is.Not.Null);
            Assert.That(presentation.TryGetStateSprite(
                KragmorRules.CoreCompressionVisualStateId,
                out Sprite compressionSprite), Is.True);
            Assert.That(compressionSprite, Is.Not.Null);
        }

        [Test]
        public void Initialize_WithNoBossPresentations_IsSafe()
        {
            DefinitionDatabase database = CreateDatabase();

            Assert.DoesNotThrow(database.Initialize);
            Assert.That(database.BossPresentations, Is.Empty);
            Assert.That(database.TryGetBossPresentation("boss_missing", out _), Is.False);
        }

        [Test]
        public void Initialize_DuplicateEffectId_Throws()
        {
            EffectDefinition first =
                CreateGameDefinition<EffectDefinition>("effect_duplicate");
            EffectDefinition second =
                CreateGameDefinition<EffectDefinition>("effect_duplicate");
            DefinitionDatabase database = CreateDatabase(
                effects: new[] { first, second });

            Assert.Throws<InvalidOperationException>(database.Initialize);
        }

        [Test]
        public void Initialize_DuplicateSkillId_Throws()
        {
            SkillDefinition first =
                CreateGameDefinition<SkillDefinition>("skill_duplicate");
            SkillDefinition second =
                CreateGameDefinition<SkillDefinition>("skill_duplicate");
            DefinitionDatabase database = CreateDatabase(
                skills: new[] { first, second });

            Assert.Throws<InvalidOperationException>(database.Initialize);
        }

        [Test]
        public void Initialize_DuplicateResourceId_Throws()
        {
            ResourceDefinition first =
                CreateGameDefinition<ResourceDefinition>("resource_duplicate");
            ResourceDefinition second =
                CreateGameDefinition<ResourceDefinition>("resource_duplicate");
            DefinitionDatabase database = CreateDatabase(
                resources: new[] { first, second });

            Assert.Throws<InvalidOperationException>(database.Initialize);
        }

        [Test]
        public void Initialize_DuplicateBossPresentationBossId_Throws()
        {
            BossPresentationDefinition first = CreatePresentation("boss_duplicate");
            BossPresentationDefinition second = CreatePresentation("boss_duplicate");
            DefinitionDatabase database = CreateDatabase(
                bossPresentations: new[] { first, second });

            Assert.Throws<InvalidOperationException>(database.Initialize);
        }

        [Test]
        public void FailedReinitialize_PreservesPreviouslyBuiltLookups()
        {
            EffectDefinition valid =
                CreateGameDefinition<EffectDefinition>("effect_valid");
            DefinitionDatabase database = CreateDatabase(
                effects: new[] { valid });
            database.Initialize();
            SetObjectArray(database, "effects", new EffectDefinition[] { null });

            Assert.Throws<InvalidOperationException>(database.Initialize);
            Assert.That(database.IsInitialized, Is.True);
            Assert.That(database.TryGetEffect(valid.Id, out var found), Is.True);
            Assert.That(found, Is.SameAs(valid));
        }

        private TDefinition CreateGameDefinition<TDefinition>(string id)
            where TDefinition : GameDefinition
        {
            TDefinition definition =
                ScriptableObject.CreateInstance<TDefinition>();
            createdObjects.Add(definition);
            SetString(definition, "id", id);
            return definition;
        }

        private BossPresentationDefinition CreatePresentation(string bossId)
        {
            BossPresentationDefinition definition =
                ScriptableObject.CreateInstance<BossPresentationDefinition>();
            createdObjects.Add(definition);
            SetString(definition, "bossId", bossId);
            return definition;
        }

        private DefinitionDatabase CreateDatabase(
            CharacterDefinition[] characters = null,
            BossDefinition[] bosses = null,
            SkillDefinition[] skills = null,
            EffectDefinition[] effects = null,
            ResourceDefinition[] resources = null,
            RelicDefinition[] relics = null,
            BossPresentationDefinition[] bossPresentations = null)
        {
            DefinitionDatabase database =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetObjectArray(database, "characters", characters);
            SetObjectArray(database, "bosses", bosses);
            SetObjectArray(database, "skills", skills);
            SetObjectArray(database, "effects", effects);
            SetObjectArray(database, "resources", resources);
            SetObjectArray(database, "relics", relics);
            SetObjectArray(database, "bossPresentations", bossPresentations);
            return database;
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray<TObject>(
            UnityEngine.Object target,
            string propertyName,
            TObject[] values)
            where TObject : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            values ??= Array.Empty<TObject>();
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
