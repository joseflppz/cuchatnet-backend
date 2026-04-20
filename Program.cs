using CUChatNet.Api;
using CUChatNet.Api.Data;
using CUChatNet.Api.Services;
using CUChatNet.Api.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. CONTROLADORES Y JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ICorreoRecuperacionAdminServicio, CorreoRecuperacionAdminServicio>();
builder.Services.AddScoped<IContactosServicio, ContactosServicio>();

// 2. SIGNALR
builder.Services.AddSignalR();
builder.Services.AddSingleton<EncryptionService>();

// 3. SWAGGER
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CUChatNet API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 4. BASE DE DATOS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("SQLEXPRESS") || connectionString.Contains("LocalDB"))
{
    Console.WriteLine("⚠️ Redirigiendo conexión a Base de Datos en PLESK (CUC)...");
    connectionString = "Server=tcp:tiusr25pl.cuc-carrera-ti.ac.cr,1433;Initial Catalog=tiusr25pl_CUChatNetDB;User ID=CUChatNetDB;Password=CUChatNetDB;Encrypt=True;TrustServerCertificate=True;Connect Timeout=60;MultipleActiveResultSets=True;";
}

builder.Services.AddDbContext<CUChatNetDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    })
);

// 5. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 6. JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSuperSecretaDePrueba1234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CUChatNet";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CUChatNetUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "admin"));
});

var app = builder.Build();


// ================= PIPELINE =================

// ✅ MANEJO GLOBAL DE ERRORES
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync("{\"error\":\"Error interno del servidor\"}");
    });
});

// 1. HTTPS
app.UseHttpsRedirection();

// 2. ARCHIVOS ESTÁTICOS
app.UseStaticFiles();

var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// 3. SWAGGER
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CUChatNet API v1");
    c.RoutePrefix = "swagger";
});

// 4. ROUTING
app.UseRouting();

// 5. MANEJO DE PREFLIGHT REQUESTS (OPTIONS)
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "*");
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});

// 6. CORS
app.UseCors("AllowFrontend");

// 7. AUTHENTICATION Y AUTHORIZATION
app.UseAuthentication();
app.UseAuthorization();

// 8. ENDPOINTS
app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();
