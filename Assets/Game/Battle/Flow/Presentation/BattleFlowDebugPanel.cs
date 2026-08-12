using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleFlowDebugPanel : MonoBehaviour
    {
        [SerializeField]
        private BattleFlowController battleFlowController = null;

        private GameObject panelRoot;
        private Text statusLabel;
        private Button bossDefeatedButton;
        private Button partyDefeatedButton;
        private Button abortBattleButton;
        private BattleFlowCoordinator subscribedCoordinator;
        private BattleSceneCombatBootstrap combatBootstrap;
        public string StatusText { get; private set; } = string.Empty;
        public string BossIntentText { get; private set; } = string.Empty;
        public bool EndBattleButtonsInteractable { get; private set; }

        private void Awake()
        {
            combatBootstrap = GetComponent<BattleSceneCombatBootstrap>();
            BuildPanelIfNeeded();
            if (battleFlowController == null)
            {
                GameLogger.Error(
                    "[BattleFlowDebugPanel] BattleFlowController is not "
                        + "assigned.",
                    this);
            }
        }

        private void Start()
        {
            SubscribeToFlow();
            RefreshState();
        }

        private void OnEnable()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            SubscribeToFlow();
            RefreshState();
        }

        private void OnDisable()
        {
            UnsubscribeFromFlow();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromFlow();
        }

        public void NotifyBossDefeated()
        {
            battleFlowController?.NotifyBossDefeated();
            RefreshState();
        }

        public void NotifyPartyDefeated()
        {
            battleFlowController?.NotifyPartyDefeated();
            RefreshState();
        }

        public void AbortBattle()
        {
            battleFlowController?.AbortBattle();
            RefreshState();
        }

        private void SubscribeToFlow()
        {
            BattleFlowCoordinator coordinator =
                battleFlowController?.Coordinator;
            if (coordinator == null || coordinator == subscribedCoordinator)
            {
                return;
            }

            UnsubscribeFromFlow();
            coordinator.PhaseChanged += HandlePhaseChanged;
            coordinator.ResultReached += HandleResultReached;
            subscribedCoordinator = coordinator;
        }

        private void UnsubscribeFromFlow()
        {
            if (subscribedCoordinator == null)
            {
                return;
            }

            subscribedCoordinator.PhaseChanged -= HandlePhaseChanged;
            subscribedCoordinator.ResultReached -= HandleResultReached;
            subscribedCoordinator = null;
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            RefreshState();
        }

        private void HandleResultReached(BattleResultKind result)
        {
            RefreshState();
        }

        private void RefreshState()
        {
            BattleContext context = battleFlowController?.Context;
            int currentTurn = context?.CurrentTurn ?? 0;
            int turnLimit = context?.TurnLimit
                ?? battleFlowController?.Setup?.TurnLimit
                ?? 0;
            BattlePhase phase = context?.Phase ?? BattlePhase.NotStarted;
            BattleResultKind result =
                context?.Result ?? BattleResultKind.None;
            BossIntentText = FormatBossIntent(ResolveBossIntent());

            StatusText =
                $"Turn: {currentTurn} / {turnLimit}\n" +
                $"Phase: {phase}\n" +
                $"Result: {result}\n" +
                BossIntentText;
            EndBattleButtonsInteractable =
                context != null && result == BattleResultKind.None;

            if (statusLabel != null)
            {
                statusLabel.text = StatusText;
            }

            SetButtonInteractable(
                bossDefeatedButton,
                EndBattleButtonsInteractable);
            SetButtonInteractable(
                partyDefeatedButton,
                EndBattleButtonsInteractable);
            SetButtonInteractable(
                abortBattleButton,
                EndBattleButtonsInteractable);
        }

        private KragmorBossIntent ResolveBossIntent()
        {
            if (combatBootstrap == null)
            {
                combatBootstrap = GetComponent<BattleSceneCombatBootstrap>();
            }

            return combatBootstrap?.BossIntentSource?.NextIntent;
        }

        private static string FormatBossIntent(KragmorBossIntent intent)
        {
            if (intent == null)
            {
                return "Boss Intent: Unavailable";
            }

            return $"Boss Intent: {intent.ActionKind} | "
                + $"Direct: {intent.HasDirectDamage.ToString().ToLowerInvariant()} | "
                + $"Heavy: {intent.IsHeavy.ToString().ToLowerInvariant()} | "
                + "Coefficient: "
                + intent.DamageCoefficient.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);
        }

        private void BuildPanelIfNeeded()
        {
            if (panelRoot != null)
            {
                return;
            }

            panelRoot = CreateUiObject(
                "Battle Flow Debug Panel",
                transform,
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform panelRect =
                panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(470f, 510f);

            Image panelImage = panelRoot.GetComponent<Image>();
            panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            panelImage.raycastTarget = false;

            statusLabel = CreateLabel(
                panelRoot.transform,
                "Status",
                new Vector2(20f, -18f),
                new Vector2(430f, 135f),
                30f,
                TextAnchor.UpperLeft);

            bossDefeatedButton = CreateButton(
                "Boss Defeated",
                -165f,
                NotifyBossDefeated);
            partyDefeatedButton = CreateButton(
                "Party Defeated",
                -250f,
                NotifyPartyDefeated);
            abortBattleButton = CreateButton(
                "Abort Battle",
                -335f,
                AbortBattle);
        }

        private Button CreateButton(
            string label,
            float anchoredY,
            UnityEngine.Events.UnityAction handler)
        {
            GameObject buttonObject = CreateUiObject(
                label,
                panelRoot.transform,
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(20f, anchoredY);
            buttonRect.sizeDelta = new Vector2(430f, 68f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.18f, 0.25f, 0.38f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(handler);

            Text buttonLabel = CreateLabel(
                buttonObject.transform,
                "Label",
                Vector2.zero,
                Vector2.zero,
                26f,
                TextAnchor.MiddleCenter);
            RectTransform labelRect = buttonLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            buttonLabel.text = label;

            return button;
        }

        private static Text CreateLabel(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            TextAnchor alignment)
        {
            GameObject labelObject = CreateUiObject(
                name,
                parent,
                typeof(CanvasRenderer),
                typeof(Text));
            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = anchoredPosition;
            labelRect.sizeDelta = size;

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            label.fontSize = Mathf.RoundToInt(fontSize);
            label.color = Color.white;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateUiObject(
            string name,
            Transform parent,
            params Type[] components)
        {
            var componentTypes = new Type[components.Length + 1];
            componentTypes[0] = typeof(RectTransform);
            Array.Copy(components, 0, componentTypes, 1, components.Length);
            var created = new GameObject(name, componentTypes);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static void SetButtonInteractable(
            Button button,
            bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
