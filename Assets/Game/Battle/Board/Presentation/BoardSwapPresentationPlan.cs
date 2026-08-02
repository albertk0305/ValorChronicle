using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardSwapPresentationPlan
    {
        private readonly ReadOnlyCollection<BoardSwapViewMotion> motions;

        internal BoardSwapPresentationPlan(
            BoardSwapActionStatus status,
            IReadOnlyList<BoardSwapViewMotion> motions,
            bool requiresSwapBack)
        {
            if (motions == null)
            {
                throw new ArgumentNullException(nameof(motions));
            }

            var copiedMotions = new BoardSwapViewMotion[motions.Count];
            for (int index = 0; index < motions.Count; index++)
            {
                copiedMotions[index] = motions[index]
                    ?? throw new ArgumentException(
                        "Motions cannot contain null.",
                        nameof(motions));
            }

            Status = status;
            this.motions = Array.AsReadOnly(copiedMotions);
            RequiresSwapBack = requiresSwapBack;
        }

        public BoardSwapActionStatus Status { get; }
        public IReadOnlyList<BoardSwapViewMotion> Motions => motions;
        public bool RequiresSwapBack { get; }
    }
}
