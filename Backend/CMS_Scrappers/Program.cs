using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Http;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

var builder = WebApplication.CreateBuilder(args);

var Jwtsettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

builder.Services.AddSingleton(Jwtsettings);

// Database context registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped  // This is important!
);

// Repository registrations
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IScrapperRepository, ScrapperRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundQueue>();
builder.Services.AddHostedService<QueuedProcessorBackgroundService>();
builder.Services.AddSingleton<ShoipfyScrapper>();
builder.Services.AddScoped<SavonchesStrategy>();
builder.Services.AddScoped<IShopifyScrapperFact,Shopify_Scrapper_factory>();
// Scraper services
builder.Services.AddHttpClient();
builder.Services.AddScoped<Scrap_shopify, ShoipfyScrapper>();


builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
       policy.WithOrigins("http://localhost:3000") 
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Jwtsettings.Issuer,
            ValidAudience = Jwtsettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwtsettings.Key))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });






var app = builder.Build();

app.UseRouting();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();