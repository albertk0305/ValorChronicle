using System;

namespace ValorChronicle.Battle.Entry
{
    /// <summary>Owns the single pending request for the next battle.</summary>
    public sealed class BattleEntryService
    {
        private BattleEntryRequest pendingRequest;

        public bool HasPendingRequest => pendingRequest != null;

        public bool TryRegister(BattleEntryRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (pendingRequest != null)
            {
                return false;
            }

            pendingRequest = request;
            return true;
        }

        public bool TryGetCurrent(out BattleEntryRequest request)
        {
            request = pendingRequest;
            return request != null;
        }

        /// <summary>
        /// Removes the pending request only when it is the request previously
        /// observed by the caller.
        /// </summary>
        public bool TryConsume(
            BattleEntryRequest expectedRequest,
            out BattleEntryRequest consumedRequest)
        {
            consumedRequest = null;
            if (expectedRequest == null
                || !ReferenceEquals(pendingRequest, expectedRequest))
            {
                return false;
            }

            consumedRequest = pendingRequest;
            pendingRequest = null;
            return true;
        }

        /// <summary>
        /// Cancels only the request owned by the caller, leaving any different
        /// pending request untouched.
        /// </summary>
        public bool TryCancel(BattleEntryRequest expectedRequest)
        {
            if (expectedRequest == null
                || !ReferenceEquals(pendingRequest, expectedRequest))
            {
                return false;
            }

            pendingRequest = null;
            return true;
        }
    }
}
