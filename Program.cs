using TPFinalAvgustin.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;  // Asegúrate de tener el namespace correcto
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // 1.1.1. Creamos una política global que exige usuario autenticado
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    // 1.1.2. Registramos un filtro global de autorización
    //        de modo que TODAS las acciones requieran login
    options.Filters.Add(new AuthorizeFilter(policy));
});

// 2) Para que además reconozca API Controllers
builder.Services.AddControllers();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        MySqlServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    ));


// 1.2. Configuramos la autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // 1.2.1. Ruta a la que redirige cuando no está autenticado
        options.LoginPath = "/Account/Login";
        // 1.2.2. Ruta para el logout (opcional)
        options.LogoutPath = "/Account/Logout";
        // 1.2.3. Tiempo de vida de la cookie, etc. se pueden ajustar aquí


        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = false;
    });

// 1.3. (Opcional) Configuramos la autorización por roles o policies
builder.Services.AddAuthorization(options =>
{
    // Ejemplo: política que exige rol Administrador
    options.AddPolicy("SoloAdmin", policy =>
        policy.RequireRole("Administrador"));
});
// Registrar los repositorios existentes
builder.Services.AddTransient<IRepositorioUsuario, RepositorioUsuario>();



// ←–––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
// REGISTRO DEL REPOSITORIO DE IMÁGENES (nuevo)
// Añade este servicio para que puedas inyectar IRepositorioImagen
// builder.Services.AddTransient<IRepositorioImagen, RepositorioImagen>();
// ←–––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––

builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization();

var app = builder.Build();  // ← La aplicación se construye tras registrar servicios

// Configuración del pipeline (middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ←–––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
// Este middleware habilita servir archivos estáticos desde wwwroot,
// incluidas las imágenes subidas bajo wwwroot/Uploads/…
app.UseStaticFiles();
// ←–––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
// app.Use(async (context, next) =>
// {
//     Console.WriteLine($"[Request] {context.Request.Method} {context.Request.Path}");
//     await next();
// });
app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication(); // ✅ primero autentica (valida cookie y reconstruye el usuario)
app.UseAuthorization();  // ✅ luego autoriza usando ese usuario


// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 3) Ruteo para API Controllers
app.MapControllers();



app.Run();
