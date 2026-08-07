using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ResourceCollection
    {
        private readonly Dictionary<string, ResourceState> resources =
            new Dictionary<string, ResourceState>(StringComparer.Ordinal);

        public int Count => resources.Count;

        public ResourceState Register(string resourceId, int maxAmount)
        {
            if (resources.ContainsKey(resourceId ?? string.Empty))
            {
                throw new ArgumentException(
                    $"Duplicate resource ID: {resourceId}.",
                    nameof(resourceId));
            }

            var resource = new ResourceState(resourceId, maxAmount);
            resources.Add(resource.ResourceId, resource);
            return resource;
        }

        public ResourceState Get(string resourceId)
        {
            ValidateId(resourceId);
            if (!resources.TryGetValue(resourceId, out ResourceState resource))
            {
                throw new KeyNotFoundException(
                    $"Resource is not registered: {resourceId}.");
            }

            return resource;
        }

        public bool TryGet(
            string resourceId,
            out ResourceState resource)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                resource = null;
                return false;
            }

            return resources.TryGetValue(resourceId, out resource);
        }

        public int GetAmount(string resourceId)
        {
            return Get(resourceId).CurrentAmount;
        }

        public ResourceAddResult Add(string resourceId, int amount)
        {
            return Get(resourceId).Add(amount);
        }

        public ResourceConsumeResult Consume(
            string resourceId,
            int amount)
        {
            return Get(resourceId).Consume(amount);
        }

        public ResourceConsumeResult ConsumeAll(string resourceId)
        {
            return Get(resourceId).ConsumeAll();
        }

        public IReadOnlyList<ResourceState> GetAll()
        {
            var snapshot = new List<ResourceState>(resources.Values);
            snapshot.Sort((left, right) => string.CompareOrdinal(
                left.ResourceId,
                right.ResourceId));
            return snapshot.AsReadOnly();
        }

        private static void ValidateId(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentException(
                    "Resource ID cannot be null or whitespace.",
                    nameof(resourceId));
            }
        }
    }
}
