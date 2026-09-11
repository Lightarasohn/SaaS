using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using SaaS.Microservices.CMS.DTOs.ExpenseDTOs;

namespace SaaS.Microservices.CMS.Validations
{
    public class ExpenseValidations
    {
        public class CreateExpenseDTOValidator : AbstractValidator<CreateExpenseDTO>
        {
            public CreateExpenseDTOValidator()
            {
                RuleFor(x => x.Amount).GreaterThan(0);
            }
        }
    }
}