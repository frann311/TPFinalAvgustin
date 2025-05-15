// Controllers/PerfilController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPFinalAvgustin.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using System.IO;

[Authorize]
public class PerfilController : Controller
{
    private readonly IRepositorioUsuario _repo;
    private readonly IWebHostEnvironment _env;

    public PerfilController(IRepositorioUsuario repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }



    // GET: /Perfil
    [Authorize]
    public IActionResult Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var u = _repo.ObtenerPorId(userId);
        if (u == null) return NotFound();

        return View(u); // Pasamos directamente el modelo Usuario
    }


    // GET: /Perfil/Edit
    public IActionResult EditProfile()
    {
        // 1) Obtener el UserId del claim
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = _repo.ObtenerPorId(userId);
        if (u == null) return NotFound();

        // 2) Poblamos el VM
        var vm = new PerfilViewModel
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Apellido = u.Apellido,
            Email = u.Email,
            AvatarUrl = u.AvatarUrl,  // asume que el modelo Usuario tiene AvatarUrl
            Perfil = u.Perfil,
        };
        return View(vm);
    }

    // POST: /Perfil/Edit
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EditProfile(PerfilViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        // 1) Cargar usuario
        var u = _repo.ObtenerPorId(vm.Id);
        if (u == null) return NotFound();

        // 2) Actualizar datos básicos
        u.Nombre = vm.Nombre;
        u.Apellido = vm.Apellido;
        u.Email = vm.Email;
        u.Perfil = vm.Perfil;

        // 3) Avatar: eliminar si se pidió
        if (vm.RemoveAvatar)
        {
            // borrar fichero físico (opcional)
            if (!string.IsNullOrEmpty(u.AvatarUrl))
            {
                var oldPath = Path.Combine(_env.WebRootPath, u.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }
            u.AvatarUrl = null;
        }

        // 4) Avatar: si sube uno nuevo
        if (vm.AvatarFile != null && vm.AvatarFile.Length > 0)
        {
            // rutas
            var uploads = Path.Combine(_env.WebRootPath, "Avatars", u.Id.ToString());
            Directory.CreateDirectory(uploads);

            // generar nombre único
            var ext = Path.GetExtension(vm.AvatarFile.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploads, fileName);

            // guardar
            using var stream = new FileStream(filePath, FileMode.Create);
            vm.AvatarFile.CopyTo(stream);

            // asignar URL para mostrar
            u.AvatarUrl = $"/Avatars/{u.Id}/{fileName}";
        }

        // 5) Guardar cambios básicos y avatar
        _repo.Modificacion(u);

        TempData["Success"] = "Perfil actualizado correctamente.";
        return RedirectToAction(nameof(EditProfile));
    }

    // POST: /Perfil/ChangePassword
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EditPassword(PerfilViewModel vm)
    {
        // 1) Obtener usuario
        var u = _repo.ObtenerPorId(vm.Id);
        if (u == null) return NotFound();



        // 2) Validar que current password coincida
        if (string.IsNullOrEmpty(vm.CurrentPassword)
         || !BCrypt.Net.BCrypt.Verify(vm.CurrentPassword, u.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "Contraseña actual incorrecta");
        }
        // 3) Validar new vs confirm
        if (vm.NewPassword != vm.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "No coincide con la nueva");
        }

        Console.WriteLine("Validando...");
        Console.WriteLine("CurrentPassword: " + vm.CurrentPassword);
        Console.WriteLine("NewPassword: " + vm.NewPassword);
        Console.WriteLine("ConfirmPassword: " + vm.ConfirmPassword);

        // ignorar otros campos
        ModelState.Remove("Nombre");
        ModelState.Remove("Apellido");
        ModelState.Remove("Email");
        ModelState.Remove("AvatarUrl");
        ModelState.Remove("AvatarFile");
        ModelState.Remove("RemoveAvatar");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("ENTRO EN ERROR CAMBIO CLAVE");
            // reinyectar datos para que no queden vacíos
            vm.AvatarUrl = u.AvatarUrl;
            vm.Nombre = u.Nombre;
            vm.Apellido = u.Apellido;
            vm.Email = u.Email;
            vm.Rol = u.Rol;
            return View("EditProfile", vm);
        }

        // 4) Hashear y guardar
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
        _repo.Modificacion(u);

        TempData["Success"] = "Contraseña cambiada correctamente.";
        return RedirectToAction(nameof(EditProfile));
    }
}
