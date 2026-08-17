using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class DamageNumberPool : MonoBehaviour
    {
        public const int DefaultPrewarmCount = 6;

        [SerializeField]
        private DamageNumberView damageNumberPrefab = null;

        [SerializeField]
        private RectTransform container = null;

        [SerializeField]
        private int prewarmCount = DefaultPrewarmCount;

        private readonly Queue<DamageNumberView> available =
            new Queue<DamageNumberView>();
        private readonly HashSet<DamageNumberView> allViews =
            new HashSet<DamageNumberView>();
        private readonly HashSet<DamageNumberView> leasedViews =
            new HashSet<DamageNumberView>();
        private bool initialized;

        public int TotalCreatedCount => allViews.Count;
        public int AvailableCount => available.Count;
        public int ActiveCount => leasedViews.Count;
        public bool IsConfigured => damageNumberPrefab != null;

        public void Configure(
            DamageNumberView prefab,
            RectTransform parent,
            int initialSize = DefaultPrewarmCount)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "An initialized damage number pool cannot be reconfigured.");
            }

            damageNumberPrefab = prefab
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

            if (damageNumberPrefab == null)
            {
                throw new InvalidOperationException(
                    "A DamageNumberView prefab must be assigned.");
            }

            if (container == null)
            {
                container = transform as RectTransform;
            }

            if (container == null || prewarmCount < 0)
            {
                throw new InvalidOperationException(
                    "The damage number pool configuration is invalid.");
            }

            initialized = true;
            for (int index = 0; index < prewarmCount; index++)
            {
                available.Enqueue(CreateView());
            }
        }

        public DamageNumberView Acquire()
        {
            Initialize();
            DamageNumberView view = available.Count > 0
                ? available.Dequeue()
                : CreateView();
            if (!leasedViews.Add(view))
            {
                throw new InvalidOperationException(
                    "The damage number view is already leased.");
            }

            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(DamageNumberView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            Initialize();
            if (!allViews.Contains(view) || !leasedViews.Remove(view))
            {
                throw new InvalidOperationException(
                    "The damage number view is not leased by this pool.");
            }

            view.ResetForPool();
            available.Enqueue(view);
        }

        public bool TryRelease(DamageNumberView view)
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

            var active = new DamageNumberView[leasedViews.Count];
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

        private DamageNumberView CreateView()
        {
            DamageNumberView view = Instantiate(
                damageNumberPrefab,
                container,
                false);
            if (view == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate a DamageNumberView.");
            }

            view.name = "DamageNumberView";
            view.ResetForPool();
            allViews.Add(view);
            return view;
        }
    }
}
