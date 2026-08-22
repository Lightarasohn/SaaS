using CMS.Interfaces;
using CMS.Services;
using CMS.Validations;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CMS.Database.Contexts.Master;
using static CMS.Validations.AuthValidator;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using CMS.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(option 
        => option.InvalidModelStateResponseFactory = context =>
        {
            // 1. ModelState içindeki tüm hata mesajlarını seçip düz bir liste (List<string>) yapıyoruz
            var errorMessages = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            // 2. Bu mesajları tek bir string halinde birleştiriyoruz 
            // (Aralarına virgül, boşluk veya " - " koyabilirsin)
            var combinedMessage = string.Join(" | ", errorMessages);

            // 3. Kendi Result protokolünün fail metoduna mesajı yolluyoruz
            // Not: Fail metodunun parametresine göre burayı uyarlayabilirsin.
            var result = Result<string>.Fail(combinedMessage); 

            return new BadRequestObjectResult(result);
        });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// DEVELOPMENT
builder.Services.AddSwaggerGen();

// DATABASE
builder.Services.AddDbContext<MasterContext>(option => 
    option.UseNpgsql(builder.Configuration.GetConnectionString("CMSConnection") 
                     ?? throw new NpgsqlException("No connection string found in project!")
    )
);
// VALIDATORS
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterToCompanyValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterWithCompanyValidator>();

// DI (Repositories)

// DI (Services)
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();
