using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class MarkCollection
    {
        private readonly Dictionary<string, MarkState> marks =
            new Dictionary<string, MarkState>(StringComparer.Ordinal);

        public int Count => marks.Count;

        public MarkState Register(string markId, int maxStacks)
        {
            if (marks.ContainsKey(markId ?? string.Empty))
            {
                throw new ArgumentException(
                    $"Duplicate mark ID: {markId}.",
                    nameof(markId));
            }

            var mark = new MarkState(markId, maxStacks);
            marks.Add(mark.MarkId, mark);
            return mark;
        }

        public MarkState Get(string markId)
        {
            ValidateId(markId);
            if (!marks.TryGetValue(markId, out MarkState mark))
            {
                throw new KeyNotFoundException(
                    $"Mark is not registered: {markId}.");
            }

            return mark;
        }

        public bool TryGet(string markId, out MarkState mark)
        {
            if (string.IsNullOrWhiteSpace(markId))
            {
                mark = null;
                return false;
            }

            return marks.TryGetValue(markId, out mark);
        }

        public int GetStacks(string markId)
        {
            return Get(markId).CurrentStacks;
        }

        public int Add(string markId, int amount)
        {
            return Get(markId).Add(amount);
        }

        public int Consume(string markId, int amount)
        {
            return Get(markId).Consume(amount);
        }

        public int ConsumeAll(string markId)
        {
            return Get(markId).ConsumeAll();
        }

        public IReadOnlyList<MarkState> GetAll()
        {
            var snapshot = new List<MarkState>(marks.Values);
            snapshot.Sort((left, right) => string.CompareOrdinal(
                left.MarkId,
                right.MarkId));
            return snapshot.AsReadOnly();
        }

        private static void ValidateId(string markId)
        {
            if (string.IsNullOrWhiteSpace(markId))
            {
                throw new ArgumentException(
                    "Mark ID cannot be null or whitespace.",
                    nameof(markId));
            }
        }
    }
}
