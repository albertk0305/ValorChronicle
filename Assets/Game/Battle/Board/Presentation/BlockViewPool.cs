using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BlockViewPool : MonoBehaviour
    {
        public const int DefaultPrewarmCount = BoardConstants.CellCount;

        [SerializeField]
        private BlockView blockViewPrefab;

        [SerializeField]
        private RectTransform container;

        [SerializeField]
        private int prewarmCount = DefaultPrewarmCount;

        private readonly Queue<BlockView> available =
            new Queue<BlockView>();
        private readonly HashSet<BlockView> allViews =
            new HashSet<BlockView>();
        private readonly HashSet<BlockView> leasedViews =
            new HashSet<BlockView>();
        private bool initialized;

        public int TotalCreatedCount => allViews.Count;
        public int AvailableCount => available.Count;
        public int ActiveCount => leasedViews.Count;

        public void Configure(
            BlockView prefab,
            RectTransform parent,
            int initialSize = DefaultPrewarmCount)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "An initialized BlockViewPool cannot be reconfigured.");
            }

            blockViewPrefab = prefab
                ?? throw new ArgumentNullException(nameof(prefab));
            container = parent
                ?? throw new ArgumentNullException(nameof(parent));

            if (initialSize < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialSize),
                    initialSize,
                    "Prewarm count cannot be negative.");
            }

            prewarmCount = initialSize;
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (blockViewPrefab == null)
            {
                throw new InvalidOperationException(
                    "A BlockView prefab must be assigned.");
            }

            if (container == null)
            {
                container = transform as RectTransform;
            }

            if (container == null)
            {
                throw new InvalidOperationException(
                    "A RectTransform pool container must be assigned.");
            }

            if (prewarmCount < 0)
            {
                throw new InvalidOperationException(
                    "Prewarm count cannot be negative.");
            }

            initialized = true;
            for (int index = 0; index < prewarmCount; index++)
            {
                available.Enqueue(CreateView());
            }
        }

        public BlockView Acquire()
        {
            Initialize();

            BlockView view = available.Count > 0
                ? available.Dequeue()
                : CreateView();

            if (!leasedViews.Add(view))
            {
                throw new InvalidOperationException(
                    "The pool attempted to lease the same BlockView twice.");
            }

            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(BlockView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            Initialize();

            if (!allViews.Contains(view))
            {
                throw new ArgumentException(
                    "The BlockView does not belong to this pool.",
                    nameof(view));
            }

            if (!leasedViews.Remove(view))
            {
                throw new InvalidOperationException(
                    "The BlockView is not currently leased.");
            }

            view.ResetForPool();
            available.Enqueue(view);
        }

        private BlockView CreateView()
        {
            BlockView view = Instantiate(
                blockViewPrefab,
                container,
                false);

            if (view == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate the BlockView prefab.");
            }

            view.name = "BlockView";
            view.ResetForPool();
            allViews.Add(view);
            return view;
        }
    }
}
