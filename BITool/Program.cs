using BITool.BackgroundJobs;
using BITool.DBContext;
using BITool.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// config request handle large file
builder.WebHost.UseKestrel(options => {
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.MaxRequestBodySize = null;
    options.Limits.MaxRequestBufferSize = null;
    options.Limits.MaxResponseBufferSize = null;
}).UseIIS(); 

//add db context
var sqlConnectionStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(
                options => options.UseMySql(
                    connectionString: sqlConnectionStr,
                    serverVersion: new MySqlServerVersion(builder.Configuration["MySqlVersion"])));

//add identity
builder.Services.AddIdentity<AdminUser, AdminUserRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

//add CORS 
var allowSpecificOriginsPolicy = "AllowSpecificOriginsPolicy";
var corsOrigins = (builder.Configuration["CorsOrigins"]?? "http://localhost:8080").Split(',');
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowSpecificOriginsPolicy, builder => 
    { 
        builder.WithOrigins(corsOrigins)
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Add JWT configuration
builder.Services
    .AddAuthentication(option =>
    {
        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(option =>
    {
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey
                (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

// add Swagger & JWT authen to Swagger
var securityScheme = new OpenApiSecurityScheme()
{
    Name = "Authorization",
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JSON Web Token based security",
};
var securityReq = new OpenApiSecurityRequirement()
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }
};
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    //options.SwaggerDoc("v1", info);
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(securityReq);
});
builder.Services.AddMemoryCache();

//add Queue job in background
builder.Services.AddSingleton<IImportDataToQueueService, ImportDataToQueueService>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx =>
{
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
        queueCapacity = 100;
    return new BackgroundTaskQueue(queueCapacity);
});

var app = builder.Build();

// response ExceptionHandler
app.UseExceptionHandler(c => c.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is not null)
    {
        var response = new { error = exception.Message };
        context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
        await context.Response.WriteAsJsonAsync(response);
    }
}));

// Configure UseSwaggerUI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.DisplayRequestDuration();
    });
}

// CORS & Authen & Authorize
app.UseHttpsRedirection();
app.UseCors(allowSpecificOriginsPolicy);
app.UseAuthentication();
app.UseAuthorization();

//add Services
app.AddAdminUserService();
app.AddImportDataService(sqlConnectionStr);
app.AddExportDataService(sqlConnectionStr);


app.Run();