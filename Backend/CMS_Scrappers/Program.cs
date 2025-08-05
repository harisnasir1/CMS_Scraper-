using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Http;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
using CMS_Scrappers.Repositories.Repos;
using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.AspNetCore.WebSockets;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Services.Implementations;
using CMS_Scrappers.Utils;


var builder = WebApplication.CreateBuilder(args);

var Jwtsettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
builder.Services.AddSingleton(Jwtsettings);

var AWSsettings = builder.Configuration.GetSection("AWS").Get<S3Settings>();
builder.Services.AddSingleton(AWSsettings);


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped  
);
builder.Services.AddHttpClient(); 

builder.Services.AddTransient<BackgroundRemover>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var apiToken = config["RemovalAi:ApiToken"];
    return new BackgroundRemover(httpClient, apiToken);
});
builder.Services.AddScoped<S3Interface, S3Service>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISdataRepository, SdataRepository>();
builder.Services.AddScoped<IScrapperRepository, ScrapperRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGoogleImageService,GoogleImageService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundQueue>();
builder.Services.AddHostedService<QueuedProcessorBackgroundService>();
builder.Services.AddSingleton<ShoipfyScrapper>();
builder.Services.AddScoped<SavonchesStrategy>();
builder.Services.AddScoped<SavonchesCategoryMapper>();
builder.Services.AddScoped<IShopifyScrapperFact,Shopify_Scrapper_factory>();
builder.Services.AddScoped<ICategoryMapperFact,CategoryMapperFactory>();
builder.Services.AddScoped<IProducts,ProductsService>();
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

var googleSettings = builder.Configuration
    .GetSection("_thirdParties")
    .Get<GoogleAPISettings>();

builder.Services.AddSingleton(googleSettings);

var app = builder.Build();

app.UseRouting();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();