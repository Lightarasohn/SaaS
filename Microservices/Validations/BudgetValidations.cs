using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using SaaS.Microservices.CMS.DTOs.BudgetDTOs;

namespace SaaS.Microservices.Validations
{
    public static class BudgetValidations
    {
        public class CreateBudgetDTOValidator : AbstractValidator<CreateBudgetDTO>
        {
            public CreateBudgetDTOValidator()
            {
                RuleFor(x => x.Month).InclusiveBetween(1, 12);
                RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
                RuleFor(x => x.TotalAmount).GreaterThan(0);
            }
        }
    }
}