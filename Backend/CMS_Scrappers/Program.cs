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
using CMS_Scrappers.Ai;
using CMS_Scrappers.Ai.Implementation;
using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.BackgroundJobs.Implementations;


var builder = WebApplication.CreateBuilder(args);

//settings
var googleSettings = builder.Configuration.GetSection("_thirdParties").Get<GoogleAPISettings>();
builder.Services.AddSingleton(googleSettings);

var Jwtsettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
builder.Services.AddSingleton(Jwtsettings);

var AWSsettings = builder.Configuration.GetSection("AWS").Get<S3Settings>();
builder.Services.AddSingleton(AWSsettings);

var AiSettings = builder.Configuration.GetSection("GroqApiKey").Get<AISettings>();
builder.Services.AddSingleton(AiSettings);

var ShopifySettings=builder.Configuration.GetSection("ShopifyApi").Get<ShopifySettings>();
builder.Services.AddSingleton(ShopifySettings);

builder.Services.AddHttpClient();

//configs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped  
);
builder.Services.AddTransient<BackgroundRemover>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var apiToken = config["RemovalAi:ApiToken"];
    return new BackgroundRemover(httpClient, apiToken);
});
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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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


//queues
builder.Services.AddSingleton<IHighPriorityTaskQueue,HighPriorityTaskQueue>();
builder.Services.AddSingleton<ILowPriorityTaskQueue,LowPriorityTaskQueue>();
builder.Services.AddSingleton<ShoipfyScrapper>();
builder.Services.AddHostedService<QueuedProcessorBackgroundService>();
builder.Services.AddHostedService<LowPriorityWorkerService>();

//Services
builder.Services.AddScoped<S3Interface, S3Service>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISdataRepository, SdataRepository>();
builder.Services.AddScoped<IScrapperRepository, ScrapperRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGoogleImageService,GoogleImageService>();
builder.Services.AddScoped<SavonchesStrategy>();
builder.Services.AddScoped<SavonchesCategoryMapper>();
builder.Services.AddScoped<IShopifyScrapperFact,Shopify_Scrapper_factory>();
builder.Services.AddScoped<ICategoryMapperFact,CategoryMapperFactory>();
builder.Services.AddScoped<IProducts,ProductsService>();
builder.Services.AddScoped<Scrap_shopify, ShoipfyScrapper>();
builder.Services.AddScoped<IAi, AI>();
builder.Services.AddScoped<IShopifyService, ShopifyService>();


builder.Services.AddControllers();
var app = builder.Build();
app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();