using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Exchange;
using ValorChronicle.Exchange.Presentation;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class ExchangePresentationTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private readonly List<GameObject> roots = new List<GameObject>();
        private Sprite faceSprite;
        private Sprite relicSprite;
        private Sprite heroTokenSprite;
        private Sprite relicTokenSprite;

        [SetUp]
        public void SetUp()
        {
            faceSprite = CreateSprite(Color.blue);
            relicSprite = CreateSprite(Color.red);
            heroTokenSprite = CreateSprite(Color.yellow);
            relicTokenSprite = CreateSprite(Color.cyan);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < roots.Count; index++)
                UnityEngine.Object.DestroyImmediate(roots[index]);
            roots.Clear();
            for (int index = 0; index < createdObjects.Count; index++)
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            createdObjects.Clear();
        }

        [Test]
        public void CharacterSlotShowsOwnershipAwakeningSpritesAndCost()
        {
            TestData data = CreateData(1, 0);
            GridFixture fixture = CreateGridFixture();
            ProfileSaveData profile = Profile(0L, 0L);

            fixture.Grid.BindCharacters(
                ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    profile),
                data.Resolver,
                resetScroll: true);
            ExchangeSlotView slot = BoundSlot(fixture);
            Assert.That(slot.ContentId, Is.EqualTo("character_000"));
            Assert.That(slot.ContentImage.sprite, Is.SameAs(faceSprite));
            Assert.That(slot.PriceIcon.sprite,
                Is.SameAs(heroTokenSprite));
            Assert.That(slot.PriceText.text, Is.EqualTo("X60"));
            Assert.That(slot.AwakeningIcon.activeSelf, Is.False);
            Assert.That(slot.AwakeningText.text,
                Is.EqualTo(ExchangeSlotView.UnownedText));
            Assert.That(slot.Button.interactable, Is.True);

            profile.Characters.Add(Character("character_000", 0));
            for (int awakening = 0; awakening <= 6; awakening++)
            {
                profile.Characters[0].Awakening = awakening;
                fixture.Grid.BindCharacters(
                    ExchangeCatalogBuilder.Build(
                        data.Database,
                        data.Catalog,
                        profile),
                    data.Resolver,
                    resetScroll: false);
                slot = BoundSlot(fixture);
                Assert.That(slot.AwakeningIcon.activeSelf, Is.True);
                Assert.That(slot.AwakeningText.text,
                    Is.EqualTo($"{awakening} / 6"));
                Assert.That(slot.Button.interactable,
                    Is.EqualTo(awakening < 6));
            }

            Assert.That(slot.CanvasGroup.alpha,
                Is.EqualTo(ExchangeSlotView.MaximumCharacterAlpha));
        }

        [Test]
        public void RelicSlotShowsResolverAwakeningAndMaximumRemainsClickable()
        {
            TestData data = CreateData(0, 1);
            GridFixture fixture = CreateGridFixture();
            ProfileSaveData profile = Profile(0L, 0L);

            fixture.Grid.BindRelics(
                ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    profile),
                data.Resolver,
                resetScroll: true);
            ExchangeSlotView slot = BoundSlot(fixture);
            Assert.That(slot.ContentImage.sprite, Is.SameAs(relicSprite));
            Assert.That(slot.PriceIcon.sprite,
                Is.SameAs(relicTokenSprite));
            Assert.That(slot.PriceText.text, Is.EqualTo("X25"));
            Assert.That(slot.AwakeningIcon.activeSelf, Is.False);
            Assert.That(slot.AwakeningText.text,
                Is.EqualTo(ExchangeSlotView.UnownedText));
            Assert.That(slot.Button.interactable, Is.True);

            profile.RelicInstances.Add(Relic(
                "target",
                "relic_000",
                0));
            for (int awakening = 0; awakening <= 4; awakening++)
            {
                profile.RelicInstances[0].Awakening = awakening;
                fixture.Grid.BindRelics(
                    ExchangeCatalogBuilder.Build(
                        data.Database,
                        data.Catalog,
                        profile),
                    data.Resolver,
                    resetScroll: false);
                slot = BoundSlot(fixture);
                Assert.That(slot.AwakeningText.text,
                    Is.EqualTo($"{awakening} / 5"));
                Assert.That(slot.Button.interactable, Is.True);
            }

            profile.RelicInstances.Insert(
                0,
                Relic("low", "relic_000", 2));
            fixture.Grid.BindRelics(
                ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    profile),
                data.Resolver,
                resetScroll: false);
            Assert.That(fixture.Grid.ItemCount, Is.EqualTo(1));
            Assert.That(BoundSlot(fixture).AwakeningText.text,
                Is.EqualTo("4 / 5"));

            profile.RelicInstances[0].Awakening = 5;
            profile.RelicInstances[1].Awakening = 5;
            fixture.Grid.BindRelics(
                ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    profile),
                data.Resolver,
                resetScroll: false);
            slot = BoundSlot(fixture);
            Assert.That(slot.AwakeningText.text, Is.EqualTo("5 / 5"));
            Assert.That(slot.AwakeningIcon.activeSelf, Is.True);
            Assert.That(slot.Button.interactable, Is.True);
            Assert.That(slot.CanvasGroup.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void ConfirmShowsProgressCostAndAffordabilityRules()
        {
            TestData data = CreateData(1, 1);
            ConfirmFixture fixture = CreateConfirmFixture();
            ProfileSaveData profile = Profile(60L, 25L);
            ExchangeCatalogData catalog = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile);

            Assert.That(catalog.Characters[0].Progress.Kind,
                Is.EqualTo(ExchangeProgressKind.Unowned));
            Assert.That(catalog.Relics[0].Progress.Kind,
                Is.EqualTo(ExchangeProgressKind.Unowned));
            Assert.That(ExchangeConfirmView.UnownedText,
                Is.EqualTo("New"));
            Assert.That(ExchangeSlotView.UnownedText,
                Is.EqualTo(ExchangeConfirmView.UnownedText));

            fixture.View.Open(
                catalog.Characters[0].CreateConfirmDisplayData(),
                faceSprite,
                heroTokenSprite);
            Assert.That(fixture.Root.activeSelf, Is.True);
            Assert.That(fixture.ContentImage.sprite, Is.SameAs(faceSprite));
            Assert.That(fixture.PriceIcon.sprite,
                Is.SameAs(heroTokenSprite));
            Assert.That(fixture.AwakeningText.text,
                Is.EqualTo(ExchangeConfirmView.UnownedText));
            Assert.That(fixture.AwakeningIcon.activeSelf, Is.False);
            Assert.That(fixture.PriceText.text, Is.EqualTo("X60"));
            Assert.That(fixture.ConfirmButton.interactable, Is.True);

            fixture.View.Open(
                catalog.Relics[0].CreateConfirmDisplayData(),
                relicSprite,
                relicTokenSprite);
            Assert.That(fixture.AwakeningText.text,
                Is.EqualTo(ExchangeConfirmView.UnownedText));
            Assert.That(fixture.AwakeningIcon.activeSelf, Is.False);
            Assert.That(fixture.PriceText.text, Is.EqualTo("X25"));
            Assert.That(fixture.ContentImage.sprite,
                Is.SameAs(relicSprite));
            Assert.That(fixture.PriceIcon.sprite,
                Is.SameAs(relicTokenSprite));

            profile.Characters.Add(Character("character_000", 2));
            profile.Currencies.HeroTokens = 0L;
            catalog = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile);
            fixture.View.Open(
                catalog.Characters[0].CreateConfirmDisplayData(),
                faceSprite,
                heroTokenSprite);
            Assert.That(fixture.AwakeningText.text, Is.EqualTo("2 → 3"));
            Assert.That(fixture.AwakeningIcon.activeSelf, Is.True);
            Assert.That(fixture.ConfirmButton.interactable, Is.False);
            Assert.That(fixture.CancelButton.interactable, Is.True);

            profile.RelicInstances.Add(Relic(
                "maximum",
                "relic_000",
                5));
            catalog = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile);
            fixture.View.Open(
                catalog.Relics[0].CreateConfirmDisplayData(),
                relicSprite,
                relicTokenSprite);
            Assert.That(fixture.AwakeningText.text,
                Is.EqualTo("5 → New 0"));
            Assert.That(fixture.AwakeningIcon.activeSelf, Is.True);
            Assert.That(fixture.PriceText.text, Is.EqualTo("X25"));
            Assert.That(fixture.ConfirmButton.interactable, Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void CharacterConfirmShowsEachAwakeningIncrease(
            int awakening)
        {
            TestData data = CreateData(1, 0);
            ConfirmFixture fixture = CreateConfirmFixture();
            ProfileSaveData profile = Profile(60L, 0L);
            profile.Characters.Add(Character(
                "character_000",
                awakening));
            CharacterExchangeListItem item = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile).Characters[0];

            fixture.View.Open(
                item.CreateConfirmDisplayData(),
                faceSprite,
                heroTokenSprite);

            Assert.That(fixture.AwakeningText.text,
                Is.EqualTo($"{awakening} → {awakening + 1}"));
            Assert.That(fixture.AwakeningIcon.activeSelf, Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void RelicConfirmShowsResolverAwakeningIncrease(
            int awakening)
        {
            TestData data = CreateData(0, 1);
            ConfirmFixture fixture = CreateConfirmFixture();
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic(
                "target",
                "relic_000",
                awakening));
            RelicExchangeListItem item = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile).Relics[0];

            fixture.View.Open(
                item.CreateConfirmDisplayData(),
                relicSprite,
                relicTokenSprite);

            Assert.That(fixture.AwakeningText.text,
                Is.EqualTo($"{awakening} → {awakening + 1}"));
            Assert.That(fixture.AwakeningIcon.activeSelf, Is.True);
        }

        [Test]
        public void MaximumCharacterCannotOpenConfirm()
        {
            TestData data = CreateData(1, 0);
            ConfirmFixture fixture = CreateConfirmFixture();
            ProfileSaveData profile = Profile(60L, 0L);
            profile.Characters.Add(Character("character_000", 6));
            CharacterExchangeListItem item = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile).Characters[0];

            Assert.Throws<InvalidOperationException>(() =>
                fixture.View.Open(
                    item.CreateConfirmDisplayData(),
                    faceSprite,
                    heroTokenSprite));
            Assert.That(fixture.Root.activeSelf, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(11)]
        public void GridSupportsRequiredItemCounts(int itemCount)
        {
            TestData data = CreateData(itemCount, 0);
            GridFixture fixture = CreateGridFixture();

            fixture.Grid.BindCharacters(
                ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    Profile(1000L, 0L)),
                data.Resolver,
                resetScroll: true);

            Assert.That(fixture.Grid.ItemCount, Is.EqualTo(itemCount));
            Assert.That(fixture.Grid.IsAtTop, Is.True);
            Assert.That(fixture.Grid.PoolSlots.Count(slot => slot.IsBound),
                Is.EqualTo(Math.Min(itemCount, 6)));
        }

        [Test]
        public void GridKeepsOrdinalOrderAndRebindsAtBottom()
        {
            TestData data = CreateData(12, 0, reverseCatalog: true);
            GridFixture fixture = CreateGridFixture();
            ExchangeCatalogData catalogData = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                Profile(1000L, 0L));

            fixture.Grid.BindCharacters(
                catalogData,
                data.Resolver,
                resetScroll: true);
            Assert.That(
                fixture.Grid.PoolSlots
                    .Where(slot => slot.IsBound)
                    .Select(slot => slot.ContentId),
                Is.EqualTo(Enumerable.Range(0, 6)
                    .Select(index => $"character_{index:D3}")));

            SetScrollOffset(fixture.Grid, fixture.Viewport, float.MaxValue);
            Assert.That(fixture.Grid.IsAtTop, Is.False);
            Assert.That(fixture.Grid.PoolSlots.Any(slot =>
                slot.IsBound && slot.ContentId == "character_011"),
                Is.True);
            Assert.That(fixture.Grid.ScrollOffset,
                Is.EqualTo(
                    fixture.Grid.ContentHeight
                        - fixture.Viewport.rect.height).Within(0.01f));

            float preservedOffset = fixture.Grid.ScrollOffset;
            fixture.Grid.BindCharacters(
                catalogData,
                data.Resolver,
                resetScroll: false);
            Assert.That(fixture.Grid.ScrollOffset,
                Is.EqualTo(preservedOffset).Within(0.01f));
            fixture.Grid.BindCharacters(
                catalogData,
                data.Resolver,
                resetScroll: true);
            Assert.That(fixture.Grid.IsAtTop, Is.True);
        }

        [Test]
        public void RebindingDoesNotDuplicateListenersAndEmptySlotsDoNotClick()
        {
            TestData data = CreateData(1, 0);
            GridFixture fixture = CreateGridFixture();
            ExchangeCatalogData catalogData = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                Profile(100L, 0L));
            int clickCount = 0;
            fixture.Grid.SelectionRequested += (_, __) => clickCount++;

            fixture.Grid.BindCharacters(
                catalogData,
                data.Resolver,
                resetScroll: true);
            fixture.Grid.BindCharacters(
                catalogData,
                data.Resolver,
                resetScroll: false);
            BoundSlot(fixture).Button.onClick.Invoke();
            Assert.That(clickCount, Is.EqualTo(1));

            fixture.Grid.Clear();
            foreach (ExchangeSlotView slot in fixture.Grid.PoolSlots)
                slot.Button.onClick.Invoke();
            Assert.That(clickCount, Is.EqualTo(1));
            Assert.That(fixture.Grid.BoundItemCount, Is.Zero);
        }

        [Test]
        public void GridExpandsPoolOnlyWhenViewportNeedsMoreThanTenSlots()
        {
            GridFixture fixture = CreateGridFixture();
            TestData data = CreateData(11, 0);
            fixture.Grid.BindCharacters(
                ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    Profile(1000L, 0L)),
                data.Resolver,
                resetScroll: true);
            Assert.That(fixture.Grid.PoolSize, Is.EqualTo(10));

            fixture.Viewport.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                2000f);
            InvokePrivate(
                fixture.Grid,
                "OnRectTransformDimensionsChange");

            Assert.That(fixture.Grid.PoolSize, Is.GreaterThan(10));
            Assert.That(fixture.Grid.ContentHeight,
                Is.EqualTo(2000f).Within(0.01f));
            ExchangeSlotView first = fixture.Grid.PoolSlots
                .Single(slot => slot.IsBound
                    && slot.ContentId == "character_000");
            ExchangeSlotView second = fixture.Grid.PoolSlots
                .Single(slot => slot.IsBound
                    && slot.ContentId == "character_001");
            Assert.That(((RectTransform)first.transform).anchoredPosition,
                Is.EqualTo(new Vector2(-265f, -50f)));
            Assert.That(((RectTransform)second.transform).anchoredPosition,
                Is.EqualTo(new Vector2(265f, -50f)));
        }

        private GridFixture CreateGridFixture()
        {
            var root = new GameObject("ExchangeTestRoot", typeof(RectTransform));
            roots.Add(root);
            RectTransform viewport = Rect("ScrollPanel", root.transform);
            viewport.sizeDelta = new Vector2(1080f, 500f);
            viewport.gameObject.AddComponent<Image>();
            viewport.gameObject.AddComponent<RectMask2D>();
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            RectTransform content = Rect("Slots", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            RectTransform background = Rect("SlotsPanel", content);
            background.gameObject.AddComponent<Image>().raycastTarget = false;
            var slots = new ExchangeSlotView[10];
            for (int index = 0; index < slots.Length; index++)
                slots[index] = Slot(content, index);

            var grid = viewport.gameObject.AddComponent<
                VirtualizedExchangeGridView>();
            grid.Configure(
                scrollRect,
                viewport,
                content,
                background,
                slots,
                new Vector2(100f, 100f),
                new Vector2(430f, 100f),
                0f,
                0f,
                0f);
            return new GridFixture(viewport, grid);
        }

        private ConfirmFixture CreateConfirmFixture()
        {
            var host = new GameObject("ExchangeConfirmHost");
            roots.Add(host);
            var root = new GameObject(
                "Confirm",
                typeof(RectTransform),
                typeof(CanvasGroup));
            root.transform.SetParent(host.transform, false);
            Image contentImage = Image("Image", root.transform);
            GameObject awakeningIcon = Image(
                "Awakening Icon",
                root.transform).gameObject;
            TMP_Text awakeningText = Text(
                "AwakeningText",
                root.transform);
            Image priceIcon = Image("PriceIcon", root.transform);
            TMP_Text priceText = Text("PriceText", root.transform);
            Button confirmButton = Image("OButton", root.transform)
                .gameObject.AddComponent<Button>();
            Button cancelButton = Image("XButton", root.transform)
                .gameObject.AddComponent<Button>();
            var view = host.AddComponent<ExchangeConfirmView>();
            view.Configure(
                root,
                contentImage,
                awakeningIcon,
                awakeningText,
                priceIcon,
                priceText,
                confirmButton,
                cancelButton,
                root.GetComponent<CanvasGroup>());
            return new ConfirmFixture(
                view,
                root,
                contentImage,
                awakeningIcon,
                awakeningText,
                priceIcon,
                priceText,
                confirmButton,
                cancelButton);
        }

        private ExchangeSlotView Slot(RectTransform content, int index)
        {
            RectTransform root = Rect($"Slot ({index})", content);
            Image contentImage = Image("Button", root);
            var button = contentImage.gameObject.AddComponent<Button>();
            GameObject awakeningIcon = Image(
                "AwakeningIcon",
                root).gameObject;
            TMP_Text awakeningText = Text("AwakeningText", root);
            Image priceIcon = Image("PriceIcon", root);
            TMP_Text priceText = Text("PriceText", root);
            CanvasGroup canvasGroup = root.gameObject
                .AddComponent<CanvasGroup>();
            var slot = root.gameObject.AddComponent<ExchangeSlotView>();
            slot.Configure(
                button,
                contentImage,
                awakeningIcon,
                awakeningText,
                priceIcon,
                priceText,
                canvasGroup);
            return slot;
        }

        private TestData CreateData(
            int characterCount,
            int relicCount,
            bool reverseCatalog = false)
        {
            var characters = new CharacterDefinition[characterCount];
            var characterPresentations =
                new CharacterPresentationDefinition[characterCount];
            var characterIds = new string[characterCount];
            for (int index = 0; index < characterCount; index++)
            {
                string id = $"character_{index:D3}";
                characterIds[index] = id;
                characters[index] = Definition<CharacterDefinition>(id);
                CharacterPresentationDefinition presentation =
                    ScriptableObject.CreateInstance<
                        CharacterPresentationDefinition>();
                createdObjects.Add(presentation);
                SetField(presentation, "characterId", id);
                SetField(presentation, "faceSprite", faceSprite);
                characterPresentations[index] = presentation;
            }

            var relics = new RelicDefinition[relicCount];
            var relicPresentations =
                new RelicPresentationDefinition[relicCount];
            var relicIds = new string[relicCount];
            for (int index = 0; index < relicCount; index++)
            {
                string id = $"relic_{index:D3}";
                relicIds[index] = id;
                relics[index] = Definition<RelicDefinition>(id);
                RelicPresentationDefinition presentation =
                    ScriptableObject.CreateInstance<
                        RelicPresentationDefinition>();
                createdObjects.Add(presentation);
                SetField(presentation, "relicId", id);
                SetField(presentation, "icon", relicSprite);
                relicPresentations[index] = presentation;
            }

            var presentationCatalog = ScriptableObject
                .CreateInstance<CharacterPresentationCatalog>();
            createdObjects.Add(presentationCatalog);
            SetField(
                presentationCatalog,
                "definitions",
                characterPresentations);
            presentationCatalog.Initialize();

            var catalog = ScriptableObject
                .CreateInstance<ExchangeCatalogDefinition>();
            createdObjects.Add(catalog);
            SetField(
                catalog,
                "characterIds",
                reverseCatalog
                    ? characterIds.Reverse().ToArray()
                    : characterIds);
            SetField(catalog, "relicIds", relicIds);

            var database = ScriptableObject
                .CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetField(database, "characters", characters);
            SetField(database, "relics", relics);
            SetField(database, "relicPresentations", relicPresentations);
            SetField(database, "exchangeCatalog", catalog);
            database.Initialize();
            var resolver = new ExchangeSpriteResolver(
                presentationCatalog,
                database,
                heroTokenSprite,
                relicTokenSprite);
            return new TestData(database, catalog, resolver);
        }

        private T Definition<T>(string id) where T : GameDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(definition);
            SetField(definition, "id", id);
            return definition;
        }

        private Sprite CreateSprite(Color color)
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            createdObjects.Add(sprite);
            return sprite;
        }

        private static ProfileSaveData Profile(
            long heroTokens,
            long relicTokens)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.HeroTokens = heroTokens;
            profile.Currencies.RelicTokens = relicTokens;
            return profile;
        }

        private static CharacterSaveData Character(
            string id,
            int awakening)
        {
            return new CharacterSaveData
            {
                CharacterId = id,
                Level = 1,
                Awakening = awakening
            };
        }

        private static RelicInstanceSaveData Relic(
            string instanceId,
            string relicId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = relicId,
                Awakening = awakening
            };
        }

        private static ExchangeSlotView BoundSlot(GridFixture fixture)
        {
            return fixture.Grid.PoolSlots.Single(slot => slot.IsBound);
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static Image Image(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent);
            return rect.gameObject.AddComponent<Image>();
        }

        private static TMP_Text Text(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent);
            return rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance
                        | BindingFlags.NonPublic
                        | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static void SetScrollOffset(
            VirtualizedExchangeGridView grid,
            RectTransform viewport,
            float offset)
        {
            RectTransform content = viewport.GetComponent<ScrollRect>()
                .content;
            float maximum = Mathf.Max(
                0f,
                content.rect.height - viewport.rect.height);
            Vector2 position = content.anchoredPosition;
            position.y = Mathf.Clamp(offset, 0f, maximum);
            content.anchoredPosition = position;
            grid.RefreshCurrent(resetScroll: false);
        }

        private sealed class TestData
        {
            public TestData(
                DefinitionDatabase database,
                ExchangeCatalogDefinition catalog,
                ExchangeSpriteResolver resolver)
            {
                Database = database;
                Catalog = catalog;
                Resolver = resolver;
            }

            public DefinitionDatabase Database { get; }
            public ExchangeCatalogDefinition Catalog { get; }
            public ExchangeSpriteResolver Resolver { get; }
        }

        private sealed class GridFixture
        {
            public GridFixture(
                RectTransform viewport,
                VirtualizedExchangeGridView grid)
            {
                Viewport = viewport;
                Grid = grid;
            }

            public RectTransform Viewport { get; }
            public VirtualizedExchangeGridView Grid { get; }
        }

        private sealed class ConfirmFixture
        {
            public ConfirmFixture(
                ExchangeConfirmView view,
                GameObject root,
                Image contentImage,
                GameObject awakeningIcon,
                TMP_Text awakeningText,
                Image priceIcon,
                TMP_Text priceText,
                Button confirmButton,
                Button cancelButton)
            {
                View = view;
                Root = root;
                ContentImage = contentImage;
                AwakeningIcon = awakeningIcon;
                AwakeningText = awakeningText;
                PriceIcon = priceIcon;
                PriceText = priceText;
                ConfirmButton = confirmButton;
                CancelButton = cancelButton;
            }

            public ExchangeConfirmView View { get; }
            public GameObject Root { get; }
            public Image ContentImage { get; }
            public GameObject AwakeningIcon { get; }
            public TMP_Text AwakeningText { get; }
            public Image PriceIcon { get; }
            public TMP_Text PriceText { get; }
            public Button ConfirmButton { get; }
            public Button CancelButton { get; }
        }
    }
}
