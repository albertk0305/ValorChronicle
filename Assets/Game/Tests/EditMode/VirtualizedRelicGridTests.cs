using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Relics;
using ValorChronicle.Relics.Inventory;
using ValorChronicle.Relics.Presentation;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class VirtualizedRelicGridTests
    {
        private GameObject testRoot;
        private Texture2D iconTexture;
        private Sprite iconSprite;

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }

            if (iconSprite != null)
            {
                Object.DestroyImmediate(iconSprite);
            }

            if (iconTexture != null)
            {
                Object.DestroyImmediate(iconTexture);
            }
        }

        [Test]
        public void Binding_RendersIconAwakeningBorderDimAndInteractable()
        {
            GridFixture fixture = CreateGrid();
            iconTexture = new Texture2D(2, 2);
            iconSprite = Sprite.Create(
                iconTexture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            RelicInventoryEntry[] entries =
            {
                Entry("normal", 0, dimmed: false, interactable: true),
                Entry("blocked", 1, dimmed: true, interactable: false),
                Entry("replace", 5, dimmed: true, interactable: true)
            };

            fixture.Grid.SetItems(entries, _ => iconSprite, true);

            Assert.That(fixture.Grid.Items, Is.EqualTo(entries));
            Assert.That(
                fixture.Grid.PoolCells
                    .Where(cell => cell.IsBound)
                    .Select(cell => cell.InstanceId),
                Is.EqualTo(entries.Select(entry => entry.InstanceId)));
            RelicInventoryCellView normal = Bound(fixture, "normal");
            RelicInventoryCellView blocked = Bound(fixture, "blocked");
            RelicInventoryCellView replace = Bound(fixture, "replace");
            Assert.That(normal.IconImage.sprite, Is.SameAs(iconSprite));
            Assert.That(normal.AwakeningText.text, Is.EqualTo("0"));
            Assert.That(replace.AwakeningText.text, Is.EqualTo("5"));
            Assert.That(normal.Border.activeSelf, Is.True);
            Assert.That(blocked.Border.activeSelf, Is.True);
            Assert.That(replace.Border.activeSelf, Is.True);
            Assert.That(normal.IconImage.color.a, Is.EqualTo(1f));
            Assert.That(blocked.IconImage.color.a,
                Is.EqualTo(RelicInventoryCellView.DimmedAlpha));
            Assert.That(replace.IconImage.color.a,
                Is.EqualTo(RelicInventoryCellView.DimmedAlpha));
            Assert.That(normal.Button.interactable, Is.True);
            Assert.That(blocked.Button.interactable, Is.False);
            Assert.That(replace.Button.interactable, Is.True);

            var requests = new List<string>();
            fixture.Grid.RelicRequested += requests.Add;
            blocked.Button.onClick.Invoke();
            normal.Button.onClick.Invoke();
            replace.Button.onClick.Invoke();
            Assert.That(requests, Is.EqualTo(new[] { "normal", "replace" }));
        }

        [TestCase(0, 950f)]
        [TestCase(1, 950f)]
        [TestCase(24, 1055f)]
        [TestCase(25, 1055f)]
        [TestCase(26, 1265f)]
        [TestCase(30, 1265f)]
        [TestCase(31, 1475f)]
        [TestCase(55, 2315f)]
        public void InventoryBoundaries_CalculateHeightAndResetUnusedCells(
            int itemCount,
            float expectedContentHeight)
        {
            GridFixture fixture = CreateGrid();

            fixture.Grid.SetItems(Entries(itemCount), _ => null, true);

            Assert.That(fixture.Grid.ItemCount, Is.EqualTo(itemCount));
            Assert.That(
                fixture.Grid.BoundItemCount,
                Is.EqualTo(Math.Min(itemCount, 25)));
            Assert.That(
                fixture.Grid.PoolCells.Count(cell => cell.IsBound),
                Is.EqualTo(Math.Min(itemCount, 25)));
            Assert.That(
                fixture.Grid.PoolCells
                    .Where(cell => !cell.IsBound)
                    .All(cell => !cell.gameObject.activeSelf),
                Is.True);
            Assert.That(
                fixture.Grid.ContentHeight,
                Is.EqualTo(expectedContentHeight));
        }

        [Test]
        public void LargeInventory_ScrollsAndRebindsPoolToCurrentInstances()
        {
            GridFixture fixture = CreateGrid();
            IReadOnlyList<RelicInventoryEntry> entries = Entries(55);

            fixture.Grid.SetItems(entries, _ => null, true);

            Assert.That(fixture.Grid.PoolSize, Is.EqualTo(30));
            Assert.That(fixture.Content.childCount, Is.EqualTo(30));
            Assert.That(fixture.Grid.ContentHeight, Is.GreaterThan(950f));
            fixture.Grid.SetScrollOffsetForTesting(float.MaxValue);
            Assert.That(fixture.Grid.FirstBoundRow, Is.GreaterThan(0));
            Assert.That(fixture.Grid.BoundItemCount, Is.GreaterThan(0));
            foreach (RelicInventoryCellView cell in
                fixture.Grid.PoolCells.Where(cell => cell.IsBound))
            {
                Assert.That(entries.Any(
                    entry => entry.InstanceId == cell.InstanceId), Is.True);
            }
        }

        [Test]
        public void ReconfigureAndRebind_DoNotAccumulateClickListeners()
        {
            GridFixture fixture = CreateGrid();
            int childCount = fixture.Content.childCount;
            Configure(fixture);
            Configure(fixture);
            Assert.That(fixture.Grid.PoolSize, Is.EqualTo(30));
            Assert.That(fixture.Content.childCount, Is.EqualTo(childCount));
            int requests = 0;
            fixture.Grid.RelicRequested += _ => requests++;

            fixture.Grid.SetItems(Entries(55), _ => null, true);
            fixture.Grid.SetScrollOffsetForTesting(1000f);
            RelicInventoryCellView cell = fixture.Grid.PoolCells
                .First(item => item.IsBound && item.Button.interactable);
            cell.Button.onClick.Invoke();

            Assert.That(requests, Is.EqualTo(1));
        }

        private GridFixture CreateGrid()
        {
            testRoot = new GameObject(
                "VirtualizedRelicGridTest",
                typeof(RectTransform));
            RectTransform viewport = Rect("Viewport", testRoot.transform);
            viewport.sizeDelta = new Vector2(1060f, 950f);
            RectTransform content = Rect("Content", viewport);
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            var initialCells = new RelicInventoryCellView[25];
            for (int index = 0; index < initialCells.Length; index++)
            {
                initialCells[index] = Cell(index, content);
            }

            var grid = viewport.gameObject.AddComponent<
                VirtualizedRelicGridView>();
            var fixture = new GridFixture(
                grid,
                scrollRect,
                viewport,
                content,
                initialCells);
            Configure(fixture);
            return fixture;
        }

        private static void Configure(GridFixture fixture)
        {
            fixture.Grid.Configure(
                fixture.ScrollRect,
                fixture.Viewport,
                fixture.Content,
                fixture.InitialCells,
                new Vector2(185f, 185f),
                new Vector2(25f, 25f),
                configuredTopPadding: 15f,
                configuredBottomPadding: 15f,
                configuredBufferRows: 0);
        }

        private static RelicInventoryCellView Bound(
            GridFixture fixture,
            string instanceId)
        {
            return fixture.Grid.PoolCells.Single(
                cell => cell.IsBound && cell.InstanceId == instanceId);
        }

        private static RelicInventoryCellView Cell(
            int index,
            RectTransform parent)
        {
            RectTransform root = Rect($"Cell {index}", parent);
            var cell = root.gameObject.AddComponent<
                RelicInventoryCellView>();
            RectTransform buttonRoot = Rect("ArtifactButton", root);
            Image icon = buttonRoot.gameObject.AddComponent<Image>();
            Button button = buttonRoot.gameObject.AddComponent<Button>();
            GameObject border = Rect("Border", root).gameObject;
            border.AddComponent<Image>();
            TMP_Text awakening = Rect("Awakening", root).gameObject
                .AddComponent<TextMeshProUGUI>();
            cell.Configure(button, icon, border, awakening);
            return cell;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static IReadOnlyList<RelicInventoryEntry> Entries(int count)
        {
            var entries = new List<RelicInventoryEntry>(count);
            for (int index = 0; index < count; index++)
            {
                entries.Add(Entry(
                    $"instance_{index:D3}",
                    index % 6,
                    dimmed: false,
                    interactable: true));
            }

            return entries;
        }

        private static RelicInventoryEntry Entry(
            string instanceId,
            int awakening,
            bool dimmed,
            bool interactable)
        {
            return (RelicInventoryEntry)Activator.CreateInstance(
                typeof(RelicInventoryEntry),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    instanceId,
                    "relic_test",
                    awakening,
                    instanceId,
                    false,
                    false,
                    dimmed,
                    string.Empty,
                    -1,
                    dimmed && interactable,
                    dimmed,
                    interactable,
                    interactable
                        ? RelicEquipAvailabilityReason.Allowed
                        : RelicEquipAvailabilityReason
                            .EquippedByOtherCharacter
                },
                culture: null);
        }

        private sealed class GridFixture
        {
            public GridFixture(
                VirtualizedRelicGridView grid,
                ScrollRect scrollRect,
                RectTransform viewport,
                RectTransform content,
                RelicInventoryCellView[] initialCells)
            {
                Grid = grid;
                ScrollRect = scrollRect;
                Viewport = viewport;
                Content = content;
                InitialCells = initialCells;
            }

            public VirtualizedRelicGridView Grid { get; }
            public ScrollRect ScrollRect { get; }
            public RectTransform Viewport { get; }
            public RectTransform Content { get; }
            public RelicInventoryCellView[] InitialCells { get; }
        }
    }
}
