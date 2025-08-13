using InternTracker.Data;
using InternTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net; 
using Microsoft.AspNetCore.Authentication; // Added for SignOutAsync

namespace InternTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly InternTrackerContext _context;

        public AccountController(InternTrackerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetStarted()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetStarted(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "User with this email already exists.");
                    return View(model);
                }

                var user = new AppUser
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = UserRole.Intern, 
                    ProfilePicturePath = "",
                    RegistrationDate = DateTime.Now
                };

                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
                    };

                    var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
                        claims, "CookieAuth");

                    var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                    {
                        IsPersistent = true 
                    };

                    await HttpContext.SignInAsync(
                        "CookieAuth",
                        new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role.ToString());

                    switch (user.Role)
                    {
                        case UserRole.Admin:
                            return RedirectToAction("Index", "Admin");
                        case UserRole.Mentor:
                            return RedirectToAction("Index", "Mentor");
                        case UserRole.Intern:
                            return RedirectToAction("Index", "Intern");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            if (Request.Query.ContainsKey("ReturnUrl"))
            {
                ViewData["ReturnUrl"] = Request.Query["ReturnUrl"];
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult RegisterAdmin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                { 
                    ModelState.AddModelError("Email", "User with this email already exists.");
                    return View(model);
                }

                var user = new AppUser
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = UserRole.Admin,
                    ProfilePicturePath = "",
                    RegistrationDate = DateTime.Now
                };

                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login");
            }
            return View(model);
        }
    }
}
