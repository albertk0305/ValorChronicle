using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveSystemFactoryTests
    {
        private readonly List<string> roots = new List<string>();
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object value in objects)
                UnityEngine.Object.DestroyImmediate(value);
            objects.Clear();
            foreach (string root in roots)
                if (Directory.Exists(root)) Directory.Delete(root, true);
            roots.Clear();
        }

        [Test]
        public void Create_InitializedDatabase_ComposesUsableServiceAndRealRepository()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "valor_chronicle_composition_tests",
                Guid.NewGuid().ToString("N"));
            roots.Add(root);
            DefinitionDatabase database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            objects.Add(database);
            database.Initialize();

            SaveService service = SaveSystemFactory.Create(root, database);
            SaveLoadResult result = service.LoadOrCreate("profile_composed");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.CreatedNewProfile));
            Assert.That(service.HasCurrentProfile, Is.True);
            Assert.That(service.CanWriteCurrentProfile, Is.True);
            Assert.That(File.Exists(Path.Combine(root, "profile.save")), Is.True);
        }

        [Test]
        public void Create_UninitializedDatabase_ThrowsBeforeSaveIo()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "valor_chronicle_composition_tests",
                Guid.NewGuid().ToString("N"));
            roots.Add(root);
            DefinitionDatabase database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            objects.Add(database);

            Assert.Throws<InvalidOperationException>(
                () => SaveSystemFactory.Create(root, database));
            Assert.That(Directory.Exists(root), Is.False);
        }
    }
}
