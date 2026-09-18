using SaaS.Interfaces;
using SaaS.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SaaS.Database.Contexts.Master;
using static SaaS.Validations.AuthValidator;
using Microsoft.AspNetCore.Mvc;
using SaaS.DTOs;
using SaaS.Emails;
using SaaS.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SaaS.Handlers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using SaaS.Database.Contexts.CMS;
using SaaS.Microservices.CMS.Interfaces;
using SaaS.Microservices.CMS.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(option =>
        option.InvalidModelStateResponseFactory = context =>
        {
            var errors = new Dictionary<string, string[]>();

            foreach (var entry in context.ModelState)
            {
                if (entry.Value.Errors.Count == 0)
                    continue;

                var key = string.IsNullOrEmpty(entry.Key)
                    ? "genel"
                    : JsonNamingPolicy.CamelCase.ConvertName(entry.Key);

                var messages = entry.Value.Errors
                    .Select(e => e.ErrorMessage)
                    .ToArray();

                errors[key] = messages;
            }

            var result = Result<Dictionary<string, string[]>>.Invalid(errors);

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
builder.Services.AddDbContext<CMSContext>(option =>
    option.UseNpgsql(builder.Configuration.GetConnectionString("CMSConnection")
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
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

// DI (Services)
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddSingleton<IAuthorizationHandler, ModuleAccessHandler>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISubscriptionPaymentProcessor, AlwaysSucceedPaymentProcessor>();
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHostedService<SubscriptionExpiryBackgroundService>();

// DI (Microservices)
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<CMSContext>(sp =>
{
    var options = sp.GetRequiredService<DbContextOptions<CMSContext>>();
    var currentUser = sp.GetRequiredService<ICurrentUser>();

    var context = new CMSContext(options);
    context.CurrentCompanyId = currentUser.CompanyId;
    return context;
});
builder.Services.AddScoped<IOrgUnitService, OrgUnitService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IOrgUnitAuthorizationService, OrgUnitAuthorizationService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings yapılandırması eksik.");

if (Encoding.UTF8.GetByteCount(jwtSettings.SigningKey) < 32)
    throw new InvalidOperationException("JwtSettings:SigningKey en az 32 byte olmalıdır.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("cost-management", policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("cost-management")));

    options.AddPolicy("hr", policy =>
        policy.Requirements.Add(new ModuleAccessRequirement("hr")));
});

// CORS
builder.Services.AddCors(option =>
        option.AddPolicy("Frontend",
                policy =>
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.UseCors("Frontend");

app.UseForwardedHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
