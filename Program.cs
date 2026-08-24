using SaaS.Interfaces;
using SaaS.Services;
using SaaS.Validations;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SaaS.Database.Contexts.Master;
using static SaaS.Validations.AuthValidator;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using SaaS.DTOs;
using SaaS.Emails;
using SaaS.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
    option.UseNpgsql(builder.Configuration.GetConnectionString("MasterConnection") 
                     ?? throw new NpgsqlException("No connection string found in project!")
    )
);
// VALIDATORS
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterToCompanyValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterWithCompanyValidator>();

// CONFIGURES
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// DI (Repositories)

// DI (Services)
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
builder.Services.AddHostedService<EmailBackgroundService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings yapılandırması eksik.");

if (Encoding.UTF8.GetByteCount(jwtSettings.SigningKey) < 32)
    throw new InvalidOperationException("JwtSettings:SigningKey en az 32 byte olmalıdır.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),

            // Varsayılan 5 dakika: 15 dk'lık token fiilen 20 dk yaşar
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
