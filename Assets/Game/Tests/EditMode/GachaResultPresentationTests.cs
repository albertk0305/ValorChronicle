using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Gacha.Presentation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaResultPresentationTests
    {
        private const string ScenePath = "Assets/Scenes/Gacha.unity";
        private const string ItemSpritePath =
            "Assets/Sprites/UI/Icon/Scroll.png";
        private const string AwakeningSpritePath =
            "Assets/Sprites/UI/Buttons/Awakening1Button.png";

        [Test]
        public void ResultViewShowsOneOrTenInOrderAndClearsPreviousState()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath, OpenSceneMode.Single);
                Transform content = FindContent(scene);
                GachaSceneInstaller installer =
                    content.GetComponent<GachaSceneInstaller>();
                installer.Install();
                GachaResultView view =
                    content.GetComponent<GachaResultView>();
                Sprite itemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    ItemSpritePath);
                Sprite awakening = AssetDatabase.LoadAssetAtPath<Sprite>(
                    AwakeningSpritePath);

                view.Show(new[]
                {
                    new GachaResultDisplayEntry(
                        0, GachaResultDisplayKind.Character,
                        itemSprite, 0, awakening)
                });
                Assert.That(view.IsOpen, Is.True);
                Assert.That(view.Slots[0].ContentRoot.activeSelf, Is.True);
                Assert.That(view.Slots[0].AwakeningImage.sprite,
                    Is.SameAs(awakening));
                Assert.That(view.Slots.Skip(1).All(
                    slot => !slot.gameObject.activeSelf), Is.True);

                GachaResultDisplayEntry[] ten = Enumerable.Range(0, 10)
                    .Select(index => new GachaResultDisplayEntry(
                        index, GachaResultDisplayKind.Item,
                        itemSprite, (index + 1) * 1000, null))
                    .ToArray();
                view.Show(ten);
                for (int index = 0; index < ten.Length; index++)
                {
                    Assert.That(view.Slots[index].ItemRoot.activeSelf,
                        Is.True);
                    Assert.That(view.Slots[index].ItemText.text,
                        Is.EqualTo($"+{(index + 1) * 1000:N0}"));
                    Assert.That(view.Slots[index].ContentImage.sprite,
                        Is.Null);
                    Assert.That(view.Slots[index].AwakeningImage.sprite,
                        Is.Null);
                }

                view.Show(new[]
                {
                    new GachaResultDisplayEntry(
                        0, GachaResultDisplayKind.Item,
                        itemSprite, 1, null)
                });
                Assert.That(view.Slots[0].ItemText.text, Is.EqualTo("+1"));
                Assert.That(view.Slots.Skip(1).All(
                    slot => !slot.gameObject.activeSelf), Is.True);

                content.Find("GachaPresentation/TouchPanel")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                Assert.That(view.Slots.All(
                    slot => !slot.gameObject.activeSelf), Is.True);
            }
            finally
            {
                Restore(previous);
            }
        }

        [Test]
        public void ResultViewRejectsUnsupportedCountsAndOrder()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath, OpenSceneMode.Single);
                Transform content = FindContent(scene);
                content.GetComponent<GachaSceneInstaller>().Install();
                GachaResultView view =
                    content.GetComponent<GachaResultView>();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    ItemSpritePath);

                Assert.Throws<System.ArgumentException>(() => view.Show(
                    new GachaResultDisplayEntry[0]));
                Assert.Throws<System.ArgumentException>(() => view.Show(
                    new[]
                    {
                        new GachaResultDisplayEntry(
                            1, GachaResultDisplayKind.Item,
                            sprite, 1, null)
                    }));
            }
            finally
            {
                Restore(previous);
            }
        }

        private static Transform FindContent(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Single(root => root.name == "GachaCanvas")
                .transform.Find("SafeAreaRoot/GamaeViewport/GameContent");
        }

        private static void Restore(SceneSetup[] setup)
        {
            if (setup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            else
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
