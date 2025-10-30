using AssignProject.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AssignProject.Models;
using Microsoft.EntityFrameworkCore;

namespace AssignProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Login page GET
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Admin()
        {
            return View();
        }

        
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Admin(Admin model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.adminLogins
                .FirstOrDefaultAsync(u => u.username == model.username && u.password == model.password);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password!";
                return View(model);
            }

            // ✅ Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.username)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // ✅ Sign in
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // ✅ Redirect to Dashboard
            return RedirectToAction("Index", "DashBoard");
        }

        // ✅ Logout
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Admin", "Admin");
        }
    }
}
