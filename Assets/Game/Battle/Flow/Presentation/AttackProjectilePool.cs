using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class AttackProjectilePool : MonoBehaviour
    {
        public const int DefaultPrewarmCount = 1;

        [SerializeField]
        private AttackProjectileView projectilePrefab = null;

        [SerializeField]
        private RectTransform container = null;

        [SerializeField]
        private int prewarmCount = DefaultPrewarmCount;

        private readonly Queue<AttackProjectileView> available =
            new Queue<AttackProjectileView>();
        private readonly HashSet<AttackProjectileView> allViews =
            new HashSet<AttackProjectileView>();
        private readonly HashSet<AttackProjectileView> leasedViews =
            new HashSet<AttackProjectileView>();
        private bool initialized;

        public int TotalCreatedCount => allViews.Count;
        public int AvailableCount => available.Count;
        public int ActiveCount => leasedViews.Count;
        public bool IsConfigured => projectilePrefab != null;

        public void Configure(
            AttackProjectileView prefab,
            RectTransform parent,
            int initialSize = DefaultPrewarmCount)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "An initialized projectile pool cannot be reconfigured.");
            }

            projectilePrefab = prefab
                ?? throw new ArgumentNullException(nameof(prefab));
            container = parent
                ?? throw new ArgumentNullException(nameof(parent));
            if (initialSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialSize));
            }

            prewarmCount = initialSize;
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (projectilePrefab == null)
            {
                throw new InvalidOperationException(
                    "An AttackProjectileView prefab must be assigned.");
            }

            if (container == null)
            {
                container = transform as RectTransform;
            }

            if (container == null || prewarmCount < 0)
            {
                throw new InvalidOperationException(
                    "The projectile pool configuration is invalid.");
            }

            initialized = true;
            for (int index = 0; index < prewarmCount; index++)
            {
                available.Enqueue(CreateView());
            }
        }

        public AttackProjectileView Acquire()
        {
            Initialize();
            AttackProjectileView view = available.Count > 0
                ? available.Dequeue()
                : CreateView();
            if (!leasedViews.Add(view))
            {
                throw new InvalidOperationException(
                    "The projectile view is already leased.");
            }

            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(AttackProjectileView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            Initialize();
            if (!allViews.Contains(view) || !leasedViews.Remove(view))
            {
                throw new InvalidOperationException(
                    "The projectile view is not leased by this pool.");
            }

            view.ResetForPool();
            available.Enqueue(view);
        }

        public bool TryRelease(AttackProjectileView view)
        {
            if (view == null || !initialized || !leasedViews.Contains(view))
            {
                return false;
            }

            Release(view);
            return true;
        }

        public void ReleaseAll()
        {
            if (!initialized || leasedViews.Count == 0)
            {
                return;
            }

            var active = new AttackProjectileView[leasedViews.Count];
            leasedViews.CopyTo(active);
            for (int index = 0; index < active.Length; index++)
            {
                Release(active[index]);
            }
        }

        private void OnDisable()
        {
            ReleaseAll();
        }

        private AttackProjectileView CreateView()
        {
            AttackProjectileView view = Instantiate(
                projectilePrefab,
                container,
                false);
            if (view == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate an AttackProjectileView.");
            }

            view.name = "AttackProjectileView";
            view.ResetForPool();
            allViews.Add(view);
            return view;
        }
    }
}
