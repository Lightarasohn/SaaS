using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs.PaymentDTOs
{
    public static class PaymentDTOs
    {
        public sealed record ChargeResult(bool Succeeded, string? FailureReason = null)
        {
            public static ChargeResult Success() => new(true);
            public static ChargeResult Fail(string reason) => new(false, reason);
        }
    }
}