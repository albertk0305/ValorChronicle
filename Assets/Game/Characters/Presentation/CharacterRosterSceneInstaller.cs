using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterRosterSceneInstaller : MonoBehaviour
    {
        [SerializeField]
        private CharacterPresentationCatalog presentationCatalog = null;
        [SerializeField] private ElementIconSet elementIconSet = null;

        public CharacterRosterScreenController Controller
        {
            get;
            private set;
        }

        public CharacterUpgradeScreenController UpgradeController
        {
            get;
            private set;
        }

        public CharacterScreenCoordinator Coordinator
        {
            get;
            private set;
        }

        public bool IsConfigured =>
            presentationCatalog != null && elementIconSet != null;

        private void Awake()
        {
            if (!IsConfigured)
            {
                GameLogger.Error(
                    "[CharacterRosterSceneInstaller] Presentation assets "
                        + "are not configured.",
                    this);
                return;
            }

            try
            {
                CharacterRosterScreenView view = BuildView();
                Controller = GetOrAdd<CharacterRosterScreenController>(
                    gameObject);
                Controller.Configure(
                    view,
                    presentationCatalog,
                    elementIconSet);
                ConfigureUpgradeScreen();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterRosterSceneInstaller] CharacterSelect "
                        + "hierarchy setup failed.",
                    this);
            }
        }

        private void ConfigureUpgradeScreen()
        {
            Transform parent = transform.parent
                ?? throw new InvalidOperationException(
                    "CharacterSelect requires a parent screen container.");
            RectTransform upgradeRoot = RequireTransform(
                parent,
                "CharacterUpgrade");
            CharacterUpgradeScreenView upgradeView =
                GetOrAdd<CharacterUpgradeScreenView>(
                    upgradeRoot.gameObject);
            upgradeView.Configure(
                RequireComponent<TMP_Text>(RequireTransform(
                    upgradeRoot,
                    "CharacterName/NameText")),
                RequireComponent<Image>(RequireTransform(
                    upgradeRoot,
                    "CharacterFull")),
                RequireComponent<TMP_Text>(RequireTransform(
                    upgradeRoot,
                    "Scroll/ScrollText")),
                RequireComponent<Image>(RequireTransform(
                    upgradeRoot,
                    "Stats/TypeIcon")),
                RequireComponent<TMP_Text>(RequireTransform(
                    upgradeRoot,
                    "Stats/LevelText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    upgradeRoot,
                    "Stats/AwakenText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    upgradeRoot,
                    "Stats/ATKText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    upgradeRoot,
                    "Stats/HPText")),
                RequireComponent<Image>(RequireTransform(
                    upgradeRoot,
                    "LevelUp/LevelUpCostIcon")),
                RequireComponent<TMP_Text>(RequireTransform(
                    upgradeRoot,
                    "LevelUp/LevelUpCostText")),
                RequireComponent<Button>(RequireTransform(
                    upgradeRoot,
                    "LevelUp/LevelUpButton")),
                RequireComponent<Button>(RequireTransform(
                    upgradeRoot,
                    "ReturnButton")));

            UpgradeController =
                GetOrAdd<CharacterUpgradeScreenController>(
                    parent.gameObject);
            UpgradeController.Configure(
                upgradeView,
                presentationCatalog,
                elementIconSet);
            Coordinator = GetOrAdd<CharacterScreenCoordinator>(
                parent.gameObject);
            Coordinator.Configure(
                gameObject,
                upgradeRoot.gameObject,
                Controller,
                UpgradeController);
        }

        private CharacterRosterScreenView BuildView()
        {
            RectTransform viewport = RequireTransform(
                transform,
                "CharacterSlots");
            RectTransform content = RequireTransform(
                viewport,
                "CharacterSelectSlots");
            ConfigureViewport(viewport, content);

            var cells = new List<CharacterRosterCellView>(
                content.childCount);
            for (int index = 0; index < content.childCount; index++)
            {
                Transform child = content.GetChild(index);
                if (child.name.StartsWith(
                    "CharacterSlot",
                    StringComparison.Ordinal))
                {
                    cells.Add(ConfigureCell(child));
                }
            }

            if (cells.Count == 0)
            {
                throw new InvalidOperationException(
                    "CharacterSelectSlots has no reusable cells.");
            }

            ScrollRect scrollRect = GetOrAdd<ScrollRect>(
                viewport.gameObject);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            VirtualizedCharacterGridView grid =
                GetOrAdd<VirtualizedCharacterGridView>(
                    viewport.gameObject);
            grid.Configure(
                scrollRect,
                viewport,
                content,
                cells.ToArray(),
                new Vector2(200f, 200f),
                new Vector2(15f, 65f));

            ElementFilterButtonView[] filters =
            {
                ConfigureFilter("OrderingButtons/FireType", ElementType.Fire),
                ConfigureFilter(
                    "OrderingButtons/WaterType",
                    ElementType.Water),
                ConfigureFilter(
                    "OrderingButtons/GrassType",
                    ElementType.Grass),
                ConfigureFilter(
                    "OrderingButtons/LightType",
                    ElementType.Light),
                ConfigureFilter("OrderingButtons/DarkType", ElementType.Dark)
            };

            Button levelSortButton = RequireComponent<Button>(
                RequireTransform(
                    transform,
                    "OrderingButtons/OrderByLevelHighButton"));
            Button awakeningSortButton = RequireComponent<Button>(
                RequireTransform(
                    transform,
                    "OrderingButtons/OrderByStarButton"));
            TMP_Text goldText = RequireComponent<TMP_Text>(
                RequireTransform(transform, "Items/Gold/GoldText"));
            TMP_Text battleRecordsText = RequireComponent<TMP_Text>(
                RequireTransform(transform, "Items/Scroll/ScrollText"));

            CharacterRosterScreenView view =
                GetOrAdd<CharacterRosterScreenView>(gameObject);
            view.Configure(
                grid,
                filters,
                levelSortButton,
                awakeningSortButton,
                goldText,
                battleRecordsText);
            return view;
        }

        private CharacterRosterCellView ConfigureCell(Transform cellRoot)
        {
            Transform characterButton = RequireTransform(
                cellRoot,
                "CharacterButton");
            CharacterRosterCellView cell =
                GetOrAdd<CharacterRosterCellView>(cellRoot.gameObject);
            cell.Configure(
                RequireComponent<Button>(characterButton),
                RequireComponent<Image>(characterButton),
                RequireComponent<Image>(RequireTransform(
                    cellRoot,
                    "TypeImage")),
                RequireTransform(cellRoot, "Border").gameObject,
                RequireComponent<TMP_Text>(RequireTransform(
                    cellRoot,
                    "Level/LevelText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    cellRoot,
                    "Awakening/AwakeningText")),
                configuredAlwaysShowBorder: true);
            return cell;
        }

        private ElementFilterButtonView ConfigureFilter(
            string path,
            ElementType element)
        {
            Transform target = RequireTransform(transform, path);
            var filter = GetOrAdd<ElementFilterButtonView>(
                target.gameObject);
            filter.Configure(
                element,
                RequireComponent<Button>(target),
                RequireComponent<Image>(target));
            return filter;
        }

        private static void ConfigureViewport(
            RectTransform viewport,
            RectTransform content)
        {
            viewport.sizeDelta = new Vector2(1070f, 1400f);
            viewport.anchoredPosition = new Vector2(0f, -150f);
            GetOrAdd<RectMask2D>(viewport.gameObject);

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
            // The ScrollPanel remains raycastable so empty-space drags reach
            // the ScrollRect, while roster buttons must be the top raycast
            // target where a cell is visible.
            content.SetAsLastSibling();
        }

        private static RectTransform RequireTransform(
            Transform root,
            string path)
        {
            Transform found = root.Find(path);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"Required Character scene object is missing: {path}.");
            }

            return found as RectTransform
                ?? throw new InvalidOperationException(
                    $"Character scene object is not a RectTransform: "
                        + $"{path}.");
        }

        private static T RequireComponent<T>(Component target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : throw new InvalidOperationException(
                    $"'{target.name}' requires {typeof(T).Name}.");
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
