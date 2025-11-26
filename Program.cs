using TPFinalAvgustin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 1) MVC + política global: TODAS las rutas requieren login
// =======================================================
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

// Necesario para API Controllers
builder.Services.AddControllers();

// =======================================================
// 2) Base de datos (MySQL)
// =======================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        MySqlServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    ));

// =======================================================
// 3) JWT Authentication (único método de autenticación)
// =======================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Permite sacar el JWT de la cookie "jwt"
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
                context.Token = token;

            return Task.CompletedTask;
        }
    };

    var secretKey = builder.Configuration["Jwt:Key"]!;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)
        )
    };
});

// =======================================================
// 4) Autorización adicional (roles/policies opcionales)
// =======================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloAdmin", policy =>
        policy.RequireRole("Administrador"));
});

// =======================================================
// 5) Repositorios y servicios
// =======================================================
builder.Services.AddTransient<IRepositorioUsuario, RepositorioUsuario>();

builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// =======================================================
// 6) Middleware
// =======================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Archivos estáticos (wwwroot)
app.UseStaticFiles();

// Middleware personalizado para redireccionar por JWT
app.UseMiddleware<JwtRedirectMiddleware>();

// Enrutamiento
app.UseRouting();

// Autenticación → Autorization
app.UseAuthentication();
app.UseAuthorization();

// =======================================================
// 7) Routing MVC y API
// =======================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
