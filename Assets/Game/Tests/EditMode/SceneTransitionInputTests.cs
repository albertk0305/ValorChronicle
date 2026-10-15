using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Scene;
using ValorChronicle.UI.Navigation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SceneTransitionInputTests
    {
        [Test]
        public async Task FailedLoadRestoresStateAndAllNavigationButtons()
        {
            var service = new SceneService();
            var bootstrapRoot = new GameObject("Bootstrap");
            bootstrapRoot.SetActive(false);
            GameBootstrapper bootstrapper =
                bootstrapRoot.AddComponent<GameBootstrapper>();
            SetProperty(
                bootstrapper,
                nameof(GameBootstrapper.SceneService),
                service);
            GameBootstrapper previous = GameBootstrapper.Instance;
            SetStaticProperty(
                typeof(GameBootstrapper),
                nameof(GameBootstrapper.Instance),
                bootstrapper);

            GameObject firstRoot = CreateNavigationButton(
                "First",
                out Button first);
            GameObject secondRoot = CreateNavigationButton(
                "Second",
                out Button second);
            var observedFirst = new List<bool>();
            var observedSecond = new List<bool>();
            service.LoadingStateChanged += _ =>
            {
                observedFirst.Add(first.interactable);
                observedSecond.Add(second.interactable);
            };

            try
            {
                try
                {
                    await service.LoadAsync((GameScene)999);
                    Assert.Fail("An unknown scene must fail before loading.");
                }
                catch (ArgumentOutOfRangeException)
                {
                }

                Assert.That(service.IsLoading, Is.False);
                Assert.That(observedFirst,
                    Is.EqualTo(new[] { false, true }));
                Assert.That(observedSecond,
                    Is.EqualTo(new[] { false, true }));
                Assert.That(first.interactable, Is.True);
                Assert.That(second.interactable, Is.True);
            }
            finally
            {
                SetStaticProperty(
                    typeof(GameBootstrapper),
                    nameof(GameBootstrapper.Instance),
                    previous);
                UnityEngine.Object.DestroyImmediate(firstRoot);
                UnityEngine.Object.DestroyImmediate(secondRoot);
                UnityEngine.Object.DestroyImmediate(bootstrapRoot);
            }
        }

        private static GameObject CreateNavigationButton(
            string name,
            out Button button)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            root.SetActive(false);
            button = root.GetComponent<Button>();
            SceneNavigationButton navigation =
                root.AddComponent<SceneNavigationButton>();
            Invoke(navigation, "Awake");
            Invoke(navigation, "OnEnable");
            return root;
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName);
            property.SetValue(target, value);
        }

        private static void SetStaticProperty(
            Type type,
            string propertyName,
            object value)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName);
            property.SetValue(null, value);
        }
    }
}
