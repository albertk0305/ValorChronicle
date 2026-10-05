using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class VirtualizedCharacterGridTests
    {
        private GameObject testRoot;

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(31, 7)]
        [TestCase(32, 7)]
        [TestCase(100, 20)]
        public void RowCount_UsesFiveColumns(int itemCount, int expected)
        {
            Assert.That(
                VirtualizedGridLayout.GetRowCount(itemCount, 5),
                Is.EqualTo(expected));
        }

        [Test]
        public void BoundRange_CoversTopMiddleAndBottom()
        {
            VirtualizedGridRange top = BoundRange(100, 0f);
            VirtualizedGridRange middle = BoundRange(100, 1500f);
            VirtualizedGridRange bottom = BoundRange(100, 3047f);

            Assert.That(top.FirstRow, Is.Zero);
            Assert.That(top.RowCount, Is.EqualTo(7));
            Assert.That(middle.FirstRow, Is.GreaterThan(0));
            Assert.That(middle.LastRowExclusive, Is.LessThan(20));
            Assert.That(bottom.LastRowExclusive, Is.EqualTo(20));
        }

        [Test]
        public void PoolRows_AreConstantForViewportSize()
        {
            int rows = VirtualizedGridLayout.GetRequiredPoolRowCount(
                viewportHeight: 1175f,
                cellHeight: 100f,
                rowStride: 210f,
                bufferRows: 1);

            Assert.That(rows, Is.EqualTo(9));
            Assert.That(rows * 5, Is.EqualTo(45));
        }

        [TestCase(0, 1175f)]
        [TestCase(1, 1175f)]
        [TestCase(5, 1175f)]
        [TestCase(6, 1175f)]
        [TestCase(32, 1382f)]
        public void ContentHeight_IsSafeForSmallAndPartialRosters(
            int itemCount,
            float expectedHeight)
        {
            float height = VirtualizedGridLayout.GetContentHeight(
                itemCount,
                columnCount: 5,
                viewportHeight: 1175f,
                cellHeight: 100f,
                rowSpacing: 110f,
                topPadding: 11f,
                bottomPadding: 11f);

            Assert.That(height, Is.EqualTo(expectedHeight));
        }

        [Test]
        public void Reconfigure_ReusesRuntimeCellsWithoutDuplicateEvents()
        {
            testRoot = new GameObject("VirtualizedGridTestRoot");
            RectTransform viewport = Rect("Viewport", testRoot.transform);
            viewport.sizeDelta = new Vector2(500f, 1400f);
            RectTransform content = Rect("Content", viewport);
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            var initialCells = new CharacterRosterCellView[30];
            for (int index = 0; index < initialCells.Length; index++)
            {
                initialCells[index] = Cell(index, content);
            }

            var grid = viewport.gameObject.AddComponent<
                VirtualizedCharacterGridView>();
            Configure(grid, scrollRect, viewport, content, initialCells);
            Assert.That(grid.PoolSize, Is.EqualTo(40));
            int firstChildCount = content.childCount;
            int firstRuntimeCount = RuntimeCellCount(content);
            Assert.That(firstChildCount, Is.EqualTo(40));
            Assert.That(firstRuntimeCount, Is.EqualTo(10));

            Configure(grid, scrollRect, viewport, content, initialCells);
            Assert.That(grid.PoolSize, Is.EqualTo(40));
            Assert.That(content.childCount, Is.EqualTo(firstChildCount));
            Assert.That(RuntimeCellCount(content),
                Is.EqualTo(firstRuntimeCount));

            Configure(grid, scrollRect, viewport, content, initialCells);
            Assert.That(grid.PoolSize, Is.EqualTo(40));
            Assert.That(content.childCount, Is.EqualTo(firstChildCount));
            Assert.That(RuntimeCellCount(content),
                Is.EqualTo(firstRuntimeCount));

            viewport.sizeDelta = new Vector2(500f, 300f);
            Configure(grid, scrollRect, viewport, content, initialCells);
            Assert.That(grid.PoolSize, Is.EqualTo(40));
            Assert.That(content.childCount, Is.EqualTo(firstChildCount));

            viewport.sizeDelta = new Vector2(500f, 1400f);
            Configure(grid, scrollRect, viewport, content, initialCells);
            Assert.That(grid.PoolSize, Is.EqualTo(40));
            Assert.That(content.childCount, Is.EqualTo(firstChildCount));
            Assert.That(RuntimeCellCount(content),
                Is.EqualTo(firstRuntimeCount));

            grid.SetItems(
                Roster(100),
                string.Empty,
                _ => null,
                _ => null,
                resetScroll: true);
            grid.SetScrollOffsetForTesting(1000f);
            Assert.That(grid.FirstBoundRow, Is.GreaterThan(0));
            Assert.That(grid.BoundItemCount, Is.GreaterThan(0));

            int requestCount = 0;
            grid.CharacterRequested += _ => requestCount++;
            CharacterRosterCellView boundCell = grid.PoolCells
                .First(cell => cell.IsBound);
            boundCell.Button.onClick.Invoke();
            Assert.That(requestCount, Is.EqualTo(1));
        }

        private static VirtualizedGridRange BoundRange(
            int itemCount,
            float offset)
        {
            return VirtualizedGridLayout.GetBoundRowRange(
                itemCount,
                columnCount: 5,
                scrollOffset: offset,
                viewportHeight: 1175f,
                cellHeight: 100f,
                rowSpacing: 110f,
                topPadding: 11f,
                bufferRows: 1);
        }

        private static void Configure(
            VirtualizedCharacterGridView grid,
            ScrollRect scrollRect,
            RectTransform viewport,
            RectTransform content,
            CharacterRosterCellView[] initialCells)
        {
            grid.Configure(
                scrollRect,
                viewport,
                content,
                initialCells,
                new Vector2(100f, 100f),
                new Vector2(115f, 165f));
        }

        private static CharacterRosterCellView Cell(
            int index,
            Transform parent)
        {
            RectTransform root = Rect($"CharacterSlot{index}", parent);
            Image face = root.gameObject.AddComponent<Image>();
            Button button = root.gameObject.AddComponent<Button>();
            Image type = new GameObject(
                "Type",
                typeof(RectTransform),
                typeof(Image)).GetComponent<Image>();
            type.transform.SetParent(root, false);
            GameObject border = new GameObject(
                "Border",
                typeof(RectTransform));
            border.transform.SetParent(root, false);
            var cell = root.gameObject.AddComponent<
                CharacterRosterCellView>();
            cell.Configure(button, face, type, border);
            return cell;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static int RuntimeCellCount(Transform content)
        {
            return content.GetComponentsInChildren<CharacterRosterCellView>(
                    includeInactive: true)
                .Count(cell => cell.name.StartsWith(
                    "Character (Virtual ",
                    StringComparison.Ordinal));
        }

        private static IReadOnlyList<CharacterRosterEntry> Roster(int count)
        {
            var roster = new List<CharacterRosterEntry>(count);
            for (int index = 0; index < count; index++)
            {
                roster.Add(new CharacterRosterEntry(
                    $"character_{index}",
                    ElementType.Water,
                    1,
                    0,
                    1,
                    1,
                    index));
            }

            return roster;
        }
    }
}
