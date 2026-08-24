using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.DTOs.AuthDTOs;
using FluentValidation;

namespace SaaS.Validations
{
    public static class AuthValidator
    {
        public class RegisterWithCompanyValidator : AbstractValidator<RegisterWithCompanyDTO>
        {
            public RegisterWithCompanyValidator()
            {
                RuleFor(r => r.CompanyName)
                    .NotEmpty().WithMessage("Şirket ismi boş bırakalamaz");
                RuleFor(r => r.Name)
                    .NotEmpty().WithMessage("İsim boş bırakılamaz");
                RuleFor(r => r.Email)
                    .NotEmpty().WithMessage("E-posta boş bırakılamaz")
                    .EmailAddress().WithMessage("Geçerli bir e-posta giriniz");
                
                RuleFor(r => r.Password)
                    .NotEmpty().WithMessage("Parola boş bırakılamaz")
                    .MinimumLength(8).WithMessage("Parola en az 8 karakter olmalıdır")
                    .MaximumLength(32).WithMessage("Parola en fazla 32 karakter olmalıdır")
                    .Matches("[A-Z]").WithMessage("Parola en az bir büyük harf içermelidir.")
                    .Matches("[a-z]").WithMessage("Parola en az bir küçük harf içermelidir.")
                    .Matches("[0-9]").WithMessage("Parola en az bir rakam içermelidir.");
            }
        }
        public class RegisterToCompanyValidator : AbstractValidator<RegisterToCompanyDTO>
        {
            public RegisterToCompanyValidator()
            {
                RuleFor(r => r.Name)
                    .NotEmpty().WithMessage("İsim boş bırakılamaz");
                RuleFor(r => r.InviteCode)
                    .NotEmpty().WithMessage("Davet kodu boş bırakılamaz");
                RuleFor(r => r.Email)
                    .NotEmpty().WithMessage("E-posta boş bırakılamaz")
                    .EmailAddress().WithMessage("Geçerli bir e-posta giriniz");
                
                RuleFor(r => r.Password)
                    .NotEmpty().WithMessage("Parola boş bırakılamaz")
                    .MinimumLength(8).WithMessage("Parola en az 8 karakter olmalıdır")
                    .MaximumLength(32).WithMessage("Parola en fazla 32 karakter olmalıdır")
                    .Matches("[A-Z]").WithMessage("Parola en az bir büyük harf içermelidir.")
                    .Matches("[a-z]").WithMessage("Parola en az bir küçük harf içermelidir.")
                    .Matches("[0-9]").WithMessage("Parola en az bir rakam içermelidir.");
            }
        }
        public class LoginValidator : AbstractValidator<LoginDTO>
        {
            public LoginValidator()
            {
                RuleFor(l => l.Email)
                    .NotEmpty().WithMessage("E-posta boş bırakılamaz")
                    .EmailAddress().WithMessage("Geçerli bir e-posta giriniz");
            }
        }
    }
}