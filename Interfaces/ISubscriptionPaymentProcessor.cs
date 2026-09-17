using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.Models;
using static SaaS.DTOs.PaymentDTOs.PaymentDTOs;

namespace SaaS.Interfaces
{
    public interface ISubscriptionPaymentProcessor
    {
        Task<ChargeResult> ChargeRenewalAsync(CompanySubscription subscription, CancellationToken ct);
    }
}