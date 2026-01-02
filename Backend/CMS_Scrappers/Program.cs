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
using CMS_Scrappers.Coordinators.Implementations;
using CMS_Scrappers.Coordinators.Interfaces;


var builder = WebApplication.CreateBuilder(args);

//settings
builder.Configuration.AddEnvironmentVariables();
var googleSettings = builder.Configuration.GetSection("_thirdParties").Get<GoogleAPISettings>();
builder.Services.AddSingleton(googleSettings);

var Jwtsettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
builder.Services.AddSingleton(Jwtsettings);

var AWSsettings = builder.Configuration.GetSection("AWS").Get<S3Settings>();
builder.Services.AddSingleton(AWSsettings);

var AiSettings = builder.Configuration.GetSection("GroqApiKey").Get<AISettings>();
builder.Services.AddSingleton(AiSettings);

//var ShopifySettings=builder.Configuration.GetSection("ShopifyApi").Get<ShopifySettings>();
//builder.Services.AddSingleton(ShopifySettings);

builder.Services.AddHttpClient();

//configs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                 
                maxRetryDelay: TimeSpan.FromSeconds(10), 
                errorCodesToAdd: null             
            );
        }
    ),
    ServiceLifetime.Scoped
);

builder.Services.AddTransient<BackgroundRemover>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var apiToken = config["Removebg:ApiToken"];
    return new BackgroundRemover(httpClient, apiToken);
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
               "http://localhost:3000",
               "http://localhost:3001",
               "http://localhost:5173",
               "http://127.0.0.1:5173",
               "https://localhost:5173",
               "https://cms-scraper-rvny.vercel.app",
               "https://cms-scraper.vercel.app",
               "https://cms.morelytrends.com" 
           )
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
               Console.WriteLine("Token from cookie: " + token); 
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

//Scrapers
builder.Services.AddSingleton<ShoipfyScrapper>();
//queues
builder.Services.AddSingleton<IHighPriorityTaskQueue,HighPriorityTaskQueue>();
builder.Services.AddSingleton<ILowPriorityTaskQueue,LowPriorityTaskQueue>();
builder.Services.AddSingleton<IUpdateShopifyTaskQueue,UpdateShopifyTaskQueue>();
builder.Services.AddHostedService<QueuedProcessorBackgroundService>();
builder.Services.AddHostedService<LowPriorityWorkerService>();
builder.Services.AddHostedService<UpdateShopifyWorkerService>();
//Services
builder.Services.AddScoped<S3Interface, S3Service>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IShopifyRepository, ShopifyRepository>();
builder.Services.AddScoped<IProductStoreMappingRepository, ProductStoreMappingRepository>();
builder.Services.AddScoped<ISdataRepository, SdataRepository>();
builder.Services.AddScoped<IScrapperRepository, ScrapperRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGoogleImageService,GoogleImageService>();
builder.Services.AddScoped<IFileReadWrite,ReadWriteFiles>();
builder.Services.AddScoped<SavonchesStrategy>();
builder.Services.AddScoped<SavonchesCategoryMapper>();
builder.Services.AddScoped<IShopifyScrapperFact,Shopify_Scrapper_factory>();
builder.Services.AddScoped<ICategoryMapperFact,CategoryMapperFactory>();
builder.Services.AddScoped<IProducts,ProductsService>();
builder.Services.AddScoped<Scrap_shopify, ShoipfyScrapper>();
builder.Services.AddScoped<IAi, AI>();
//builder.Services.AddScoped<IShopifyService, ShopifyService>();
//Coordinators
builder.Services.AddScoped<IProductSyncCoordinator, ProductSyncCoordinator>();

builder.Services.AddControllers();
var app = builder.Build();
app.UseRouting();
// Ensure ASP.NET Core knows it's behind a proxy/HTTPS on Railway so cookie Secure and scheme are respected
app.Use((context, next) => {
    if (context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
    {
        context.Request.Scheme = context.Request.Headers["X-Forwarded-Proto"];    
    }
    return next();
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();