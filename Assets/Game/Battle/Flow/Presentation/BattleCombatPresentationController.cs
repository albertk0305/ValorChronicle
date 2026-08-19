using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public interface IPresentationRandomSource
    {
        float NextUnitValue();
    }

    public sealed class SystemPresentationRandomSource
        : IPresentationRandomSource
    {
        private readonly System.Random random;

        public SystemPresentationRandomSource()
            : this(Environment.TickCount)
        {
        }

        public SystemPresentationRandomSource(int seed)
        {
            random = new System.Random(seed);
        }

        public float NextUnitValue()
        {
            return (float)random.NextDouble();
        }
    }

    public readonly struct DamageNumberPresentation
    {
        public DamageNumberPresentation(
            string text,
            float fontSize,
            Color faceColor,
            Color outlineColor,
            bool useBossAnchor)
        {
            Text = text;
            FontSize = fontSize;
            FaceColor = faceColor;
            OutlineColor = outlineColor;
            UseBossAnchor = useBossAnchor;
        }

        public string Text { get; }
        public float FontSize { get; }
        public Color FaceColor { get; }
        public Color OutlineColor { get; }
        public bool UseBossAnchor { get; }
    }

    public sealed class BattleCombatPresentationController : MonoBehaviour,
        IMatchDamageProjectilePresenter,
        IActiveDamageProjectilePresenter,
        IBossDamageProjectilePresenter
    {
        public const float DefaultProjectileDuration = 0.18f;
        public const float DefaultDamageTextLifetime = 0.75f;
        public const float DefaultDamageTextOutlineWidth = 0.2f;

        [SerializeField]
        private BattleHudController hudController = null;

        [SerializeField]
        private RectTransform effectLayer = null;

        [SerializeField]
        private AttackProjectilePool projectilePool = null;

        [SerializeField]
        private DamageNumberPool damageNumberPool = null;

        [SerializeField]
        private Sprite projectileSprite = null;

        [SerializeField]
        [Min(0f)]
        private float projectileDuration = DefaultProjectileDuration;

        [SerializeField]
        [Min(0f)]
        private float damageTextLifetime = DefaultDamageTextLifetime;

        [SerializeField]
        private Vector2 damageTextRandomOffset = new Vector2(32f, 24f);

        [SerializeField]
        private float damageTextVerticalTravel = 80f;

        [SerializeField]
        [Min(1f)]
        private float normalDamageFontSize = 42f;

        [SerializeField]
        [Min(1f)]
        private float weaknessDamageFontSize = 48f;

        [SerializeField]
        [Min(1f)]
        private float criticalDamageFontSize = 56f;

        [SerializeField]
        private Color normalDamageColor = Color.white;

        [SerializeField]
        private Color weaknessDamageColor = Color.yellow;

        [SerializeField]
        private Color healColor = Color.green;

        [SerializeField]
        private Color normalOutlineColor = Color.red;

        [SerializeField]
        private Color weaknessOutlineColor = Color.white;

        [SerializeField]
        [Range(0f, 1f)]
        private float damageTextOutlineWidth = DefaultDamageTextOutlineWidth;

        [SerializeField]
        private Color fireColor = new Color(1f, 0.25f, 0.2f, 1f);

        [SerializeField]
        private Color waterColor = new Color(0.2f, 0.55f, 1f, 1f);

        [SerializeField]
        private Color grassColor = new Color(0.25f, 0.8f, 0.35f, 1f);

        [SerializeField]
        private Color lightColor = new Color(1f, 0.85f, 0.2f, 1f);

        [SerializeField]
        private Color darkColor = new Color(0.65f, 0.3f, 1f, 1f);

        [SerializeField]
        private Color fallbackColor = Color.white;

        private Coroutine activeCoroutine;
        private AttackProjectileView activeView;
        private Action<bool> activeCompletion;
        private int presentationVersion;
        private readonly Dictionary<DamageNumberView, Coroutine>
            activeDamageNumbers =
                new Dictionary<DamageNumberView, Coroutine>();
        private IPresentationRandomSource damageNumberRandomSource;

        public BattleHudController HudController => hudController;
        public RectTransform EffectLayer => effectLayer;
        public AttackProjectilePool ProjectilePool => projectilePool;
        public DamageNumberPool DamageNumberPool => damageNumberPool;
        public Sprite ProjectileSprite => projectileSprite;
        public float ProjectileDuration => projectileDuration;
        public bool IsPresenting => activeCompletion != null;
        public Color BossProjectileColor => Color.white;
        public float DamageTextLifetime => damageTextLifetime;
        public Vector2 DamageTextRandomOffset => damageTextRandomOffset;

        public void SetDamageNumberRandomSource(
            IPresentationRandomSource randomSource)
        {
            damageNumberRandomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
        }

        public bool TryPresent(CombatActionResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return TryCreateDamageNumberPresentation(
                    result,
                    out DamageNumberPresentation presentation)
                && TryPresentDamageNumber(presentation);
        }

        public bool TryCreateDamageNumberPresentation(
            CombatActionResult result,
            out DamageNumberPresentation presentation)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            long value;
            bool isWeakness = false;
            bool isCritical = false;
            bool isHealing = false;
            bool useBossAnchor;
            if (result is DamageActionResult playerDamage)
            {
                value = playerDamage.AppliedDamage;
                isWeakness = playerDamage.WasWeakness;
                isCritical = playerDamage.WasCritical;
                useBossAnchor = true;
            }
            else if (result is BossDamageActionResult bossDamage)
            {
                value = bossDamage.AppliedHpDamage;
                useBossAnchor = false;
            }
            else if (result is HealActionResult healing
                && healing.AppliedHealing > 0)
            {
                value = healing.AppliedHealing;
                isHealing = true;
                useBossAnchor = false;
            }
            else
            {
                presentation = default;
                return false;
            }

            bool criticalStyle = !isHealing && isCritical;
            presentation = new DamageNumberPresentation(
                string.Concat(
                    value.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    criticalStyle ? "!" : string.Empty),
                criticalStyle
                    ? criticalDamageFontSize
                    : isWeakness
                        ? weaknessDamageFontSize
                        : normalDamageFontSize,
                isHealing
                    ? healColor
                    : isWeakness
                        ? weaknessDamageColor
                        : normalDamageColor,
                isWeakness
                    ? weaknessOutlineColor
                    : normalOutlineColor,
                useBossAnchor);
            return true;
        }

        public bool TryPresent(
            MatchDamageProjectileRequest request,
            Action<MatchDamageProjectileCompletion> completion)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (completion == null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (IsPresenting)
            {
                GameLogger.Warning(
                    "[BattleCombatPresentation] A player damage projectile "
                        + "is already active.",
                    this);
                return false;
            }

            if (!TryResolvePlayerPositions(
                request,
                out Vector2 start,
                out Vector2 end))
            {
                GameLogger.Warning(
                    "[BattleCombatPresentation] Player damage projectile "
                        + "references are unavailable; applying immediately.",
                    this);
                return false;
            }

            return TryStartProjectile(
                start,
                end,
                GetElementColor(request.AttackElement),
                arrived => completion(
                    arrived
                        ? MatchDamageProjectileCompletion.Arrived
                        : MatchDamageProjectileCompletion.Cancelled),
                "Player damage");
        }

        public bool TryPresent(
            ActiveDamageProjectileRequest request,
            Action<ActiveDamageProjectileCompletion> completion)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (completion == null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (IsPresenting)
            {
                GameLogger.Warning(
                    "[BattleCombatPresentation] An active damage "
                        + "projectile is already active.",
                    this);
                return false;
            }

            if (!TryResolvePlayerPositions(
                request.PartySlotIndex,
                request.CharacterId,
                out Vector2 start,
                out Vector2 end))
            {
                GameLogger.Warning(
                    "[BattleCombatPresentation] Active damage projectile "
                        + "references are unavailable; applying "
                        + "immediately.",
                    this);
                return false;
            }

            return TryStartProjectile(
                start,
                end,
                GetElementColor(request.AttackElement),
                arrived => completion(
                    arrived
                        ? ActiveDamageProjectileCompletion.Arrived
                        : ActiveDamageProjectileCompletion.Cancelled),
                "Active damage");
        }

        public bool TryPresent(
            BossDamageProjectileRequest request,
            Action<BossDamageProjectileCompletion> completion)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (completion == null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (IsPresenting)
            {
                GameLogger.Warning(
                    "[BattleCombatPresentation] A damage projectile is "
                        + "already active.",
                    this);
                return false;
            }

            if (projectilePool == null
                || projectileSprite == null
                || !projectilePool.isActiveAndEnabled
                || !TryResolveBossPositions(
                    out Vector2 start,
                    out Vector2 end))
            {
                GameLogger.Warning(
                    "[BattleCombatPresentation] Boss damage projectile "
                        + "references are unavailable; applying "
                        + "immediately.",
                    this);
                return false;
            }

            return TryStartProjectile(
                start,
                end,
                BossProjectileColor,
                arrived => completion(
                    arrived
                        ? BossDamageProjectileCompletion.Arrived
                        : BossDamageProjectileCompletion.Cancelled),
                "Boss damage");
        }

        private bool TryStartProjectile(
            Vector2 start,
            Vector2 end,
            Color color,
            Action<bool> completion,
            string presentationName)
        {
            AttackProjectileView view = null;
            try
            {
                view = projectilePool.Acquire();
                view.Prepare(
                    projectileSprite,
                    color,
                    start);
            }
            catch (Exception exception)
            {
                projectilePool?.TryRelease(view);
                GameLogger.Exception(exception, this);
                GameLogger.Warning(
                    $"[BattleCombatPresentation] {presentationName} "
                        + "projectile setup failed; "
                        + "applying immediately.",
                    this);
                return false;
            }

            activeView = view;
            activeCompletion = completion;
            int version = ++presentationVersion;
            if (projectileDuration <= 0f)
            {
                activeView.SetAnchoredPosition(end);
                Complete(version, arrived: true);
                return true;
            }

            try
            {
                activeCoroutine = StartCoroutine(
                    AnimateProjectile(version, start, end));
            }
            catch (Exception exception)
            {
                DiscardCurrent();
                GameLogger.Exception(exception, this);
                GameLogger.Warning(
                    $"[BattleCombatPresentation] {presentationName} "
                        + "projectile animation failed "
                        + "to start; applying immediately.",
                    this);
                return false;
            }

            if (activeCoroutine == null)
            {
                DiscardCurrent();
                GameLogger.Warning(
                    $"[BattleCombatPresentation] {presentationName} "
                        + "projectile animation did "
                        + "not start; applying immediately.",
                    this);
                return false;
            }

            return true;
        }

        public void CancelActive()
        {
            if (!IsPresenting)
            {
                return;
            }

            presentationVersion++;
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            CompleteCurrent(arrived: false);
        }

        public Color GetElementColor(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire:
                    return fireColor;
                case ElementType.Water:
                    return waterColor;
                case ElementType.Grass:
                    return grassColor;
                case ElementType.Light:
                    return lightColor;
                case ElementType.Dark:
                    return darkColor;
                default:
                    return fallbackColor;
            }
        }

        private void OnDisable()
        {
            CancelActive();
            ReleaseAllDamageNumbers();
        }

        private bool TryPresentDamageNumber(
            DamageNumberPresentation presentation)
        {
            if (!TryResolveDamageNumberPosition(
                presentation.UseBossAnchor,
                out Vector2 anchor))
            {
                return false;
            }

            Vector2 start = anchor + CreateDamageNumberRandomOffset();
            DamageNumberView view = null;
            try
            {
                view = damageNumberPool.Acquire();
                view.Prepare(
                    presentation.Text,
                    presentation.FontSize,
                    presentation.FaceColor,
                    presentation.OutlineColor,
                    damageTextOutlineWidth,
                    start);

                if (damageTextLifetime <= 0f)
                {
                    damageNumberPool.Release(view);
                    return true;
                }

                activeDamageNumbers.Add(view, null);
                Coroutine animation = StartCoroutine(
                    AnimateDamageNumber(view, start));
                if (animation == null)
                {
                    activeDamageNumbers.Remove(view);
                    damageNumberPool.Release(view);
                    return false;
                }

                activeDamageNumbers[view] = animation;
                return true;
            }
            catch (Exception exception)
            {
                if (view != null)
                {
                    activeDamageNumbers.Remove(view);
                }

                damageNumberPool?.TryRelease(view);
                GameLogger.Exception(exception, this);
                GameLogger.Warning(
                    "[BattleCombatPresentation] Damage number setup "
                        + "failed; combat progression will continue.",
                    this);
                return false;
            }
        }

        private IEnumerator AnimateDamageNumber(
            DamageNumberView view,
            Vector2 start)
        {
            float elapsed = 0f;
            while (elapsed < damageTextLifetime)
            {
                if (view == null || !activeDamageNumbers.ContainsKey(view))
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / damageTextLifetime);
                view.SetPresentation(
                    start + Vector2.up * (damageTextVerticalTravel * progress),
                    1f - progress);
                yield return null;
            }

            if (view != null && activeDamageNumbers.Remove(view))
            {
                damageNumberPool?.TryRelease(view);
            }
        }

        private Vector2 CreateDamageNumberRandomOffset()
        {
            IPresentationRandomSource randomSource =
                damageNumberRandomSource ??=
                    new SystemPresentationRandomSource();
            return new Vector2(
                Mathf.Lerp(
                    -damageTextRandomOffset.x,
                    damageTextRandomOffset.x,
                    Mathf.Clamp01(randomSource.NextUnitValue())),
                Mathf.Lerp(
                    -damageTextRandomOffset.y,
                    damageTextRandomOffset.y,
                    Mathf.Clamp01(randomSource.NextUnitValue())));
        }

        private bool TryResolveDamageNumberPosition(
            bool useBossAnchor,
            out Vector2 position)
        {
            position = default;
            if (damageNumberPool == null
                || !damageNumberPool.isActiveAndEnabled
                || !isActiveAndEnabled
                || !TryResolveBossPositions(
                    out Vector2 bossPosition,
                    out Vector2 partyPosition))
            {
                return false;
            }

            position = useBossAnchor ? bossPosition : partyPosition;
            return true;
        }

        private void ReleaseAllDamageNumbers()
        {
            if (activeDamageNumbers.Count > 0)
            {
                Coroutine[] coroutines =
                    new Coroutine[activeDamageNumbers.Count];
                activeDamageNumbers.Values.CopyTo(coroutines, 0);
                for (int index = 0; index < coroutines.Length; index++)
                {
                    if (coroutines[index] != null)
                    {
                        StopCoroutine(coroutines[index]);
                    }
                }

                activeDamageNumbers.Clear();
            }

            damageNumberPool?.ReleaseAll();
        }

        private IEnumerator AnimateProjectile(
            int version,
            Vector2 start,
            Vector2 end)
        {
            float elapsed = 0f;
            while (elapsed < projectileDuration)
            {
                if (version != presentationVersion || activeView == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / projectileDuration);
                activeView.SetAnchoredPosition(
                    Vector2.LerpUnclamped(start, end, progress));
                yield return null;
            }

            if (version == presentationVersion && activeView != null)
            {
                activeView.SetAnchoredPosition(end);
                Complete(version, arrived: true);
            }
        }

        private bool TryResolvePlayerPositions(
            MatchDamageProjectileRequest request,
            out Vector2 start,
            out Vector2 end)
        {
            return TryResolvePlayerPositions(
                request.PartySlotIndex,
                request.CharacterId,
                out start,
                out end);
        }

        private bool TryResolvePlayerPositions(
            int partySlotIndex,
            string characterId,
            out Vector2 start,
            out Vector2 end)
        {
            start = default;
            end = default;
            if (hudController == null
                || effectLayer == null
                || projectilePool == null
                || projectileSprite == null
                || !isActiveAndEnabled
                || !gameObject.activeInHierarchy
                || !effectLayer.gameObject.activeInHierarchy
                || !projectilePool.isActiveAndEnabled
                || !hudController.TryResolvePlayerProjectileAnchors(
                    partySlotIndex,
                    characterId,
                    out RectTransform source,
                    out RectTransform target))
            {
                return false;
            }

            Canvas canvas = effectLayer.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            return TryGetLocalCenter(source, camera, out start)
                && TryGetLocalCenter(target, camera, out end);
        }

        private bool TryResolveBossPositions(
            out Vector2 start,
            out Vector2 end)
        {
            start = default;
            end = default;
            if (hudController == null
                || effectLayer == null
                || !isActiveAndEnabled
                || !gameObject.activeInHierarchy
                || !effectLayer.gameObject.activeInHierarchy
                || !hudController.TryResolveBossProjectileAnchors(
                    out RectTransform source,
                    out IReadOnlyList<RectTransform> targets))
            {
                return false;
            }

            Canvas canvas = effectLayer.GetComponentInParent<Canvas>();
            if (canvas == null
                || !TryGetLocalCenter(source, GetCanvasCamera(canvas),
                    out start))
            {
                return false;
            }

            Camera camera = GetCanvasCamera(canvas);
            Vector2 total = Vector2.zero;
            for (int index = 0; index < targets.Count; index++)
            {
                RectTransform target = targets[index];
                if (target == null
                    || !TryGetLocalCenter(target, camera,
                        out Vector2 targetPosition))
                {
                    return false;
                }

                total += targetPosition;
            }

            if (targets.Count == 0)
            {
                return false;
            }

            end = total / targets.Count;
            return true;
        }

        private static Camera GetCanvasCamera(Canvas canvas)
        {
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
        }

        private bool TryGetLocalCenter(
            RectTransform target,
            Camera camera,
            out Vector2 localPosition)
        {
            Vector3 worldCenter = target.TransformPoint(target.rect.center);
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
                camera,
                worldCenter);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectLayer,
                screenPosition,
                camera,
                out localPosition);
        }

        private void Complete(int version, bool arrived)
        {
            if (version != presentationVersion)
            {
                return;
            }

            CompleteCurrent(arrived);
        }

        private void CompleteCurrent(bool arrived)
        {
            Action<bool> callback = activeCompletion;
            AttackProjectileView view = activeView;
            activeCoroutine = null;
            activeView = null;
            activeCompletion = null;
            projectilePool?.TryRelease(view);
            callback?.Invoke(arrived);
        }

        private void DiscardCurrent()
        {
            AttackProjectileView view = activeView;
            activeCoroutine = null;
            activeView = null;
            activeCompletion = null;
            projectilePool?.TryRelease(view);
        }
    }
}
