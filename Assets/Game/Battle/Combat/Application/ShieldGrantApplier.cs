using System;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Application
{
    public static class ShieldGrantApplier
    {
        public static ShieldGrantApplicationResult Apply(
            PartyBattleState party,
            ShieldGenerationResult generationResult,
            ShieldGrantRequest request)
        {
            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (generationResult == null)
            {
                throw new ArgumentNullException(nameof(generationResult));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            long requestedAmount = generationResult.FinalShieldAmount;
            long totalBefore = party.Shields.TotalShield;
            long availableAmount = Math.Max(
                party.MaxHp - totalBefore,
                0L);
            long grantedAmount = Math.Min(
                requestedAmount,
                availableAmount);
            if (grantedAmount == 0)
            {
                return new ShieldGrantApplicationResult(
                    requestedAmount,
                    0,
                    totalBefore,
                    totalBefore,
                    null);
            }

            var shield = new ShieldInstance(
                request.RuntimeId,
                request.SourceId,
                grantedAmount,
                request.CreatedTurn,
                request.RemainingTurns,
                request.CreationOrder);
            party.Shields.Add(shield);
            return new ShieldGrantApplicationResult(
                requestedAmount,
                grantedAmount,
                totalBefore,
                party.Shields.TotalShield,
                shield.RuntimeId);
        }
    }
}
