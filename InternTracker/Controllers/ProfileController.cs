using InternTracker.Data;
using InternTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authentication;

namespace InternTracker.Controllers
{
    public class ProfileController : Controller
    {
        private readonly InternTrackerContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProfileController(InternTrackerContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.AppUsers.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.AppUsers.FindAsync(userId.Value);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError(string.Empty, "Invalid current password.");
                    return View(model);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();

                ViewBag.Message = "Password changed successfully!";
                return View();
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult UpdateEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(UpdateEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.AppUsers.FindAsync(userId.Value);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError(string.Empty, "Invalid password.");
                    return View(model);
                }

                var existingUserWithEmail = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == model.NewEmail);
                if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
                {
                    ModelState.AddModelError("NewEmail", "This email is already in use.");
                    return View(model);
                }

                user.Email = model.NewEmail;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();

                ViewBag.Message = "Email updated successfully!";
                return View();
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult UploadProfilePicture()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(UploadProfilePictureViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.AppUsers.FindAsync(userId.Value);

                if (user == null)
                {
                    return NotFound();
                }

                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfilePicture.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Delete old profile picture if it exists and is not the default
                    if (!string.IsNullOrEmpty(user.ProfilePicturePath) && user.ProfilePicturePath != "/images/default-profile.png")
                    {
                        var oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(fileStream);
                    }

                    user.ProfilePicturePath = "/images/" + uniqueFileName;
                    _context.AppUsers.Update(user);
                    await _context.SaveChangesAsync();

                    ViewBag.Message = "Profile picture uploaded successfully!";
                    return View();
                }
                ModelState.AddModelError(string.Empty, "Please select a file to upload.");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeUsername()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.AppUsers.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangeUsernameViewModel { CurrentUsername = user.Username, NewUsername = string.Empty };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.AppUsers.FindAsync(userId.Value);

                if (user == null)
                {
                    return NotFound();
                }

                // Check if the new username is already taken by another user
                var existingUserWithUsername = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == model.NewUsername);
                if (existingUserWithUsername != null && existingUserWithUsername.Id != user.Id)
                {
                    ModelState.AddModelError("NewUsername", "This username is already taken.");
                    return View(model);
                }

                user.Username = model.NewUsername;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();

                // Update session username
                HttpContext.Session.SetString("Username", user.Username);

                ViewBag.Message = "Username changed successfully!";
                return View(model);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.AppUsers.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int id, string confirmText)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId != id)
            {
                return Unauthorized();
            }

            if (confirmText != "DELETE")
            {
                ViewBag.ErrorMessage = "You must type 'DELETE' to confirm account deletion.";
                var user = await _context.AppUsers.FindAsync(id);
                return View(user);
            }

            var userToDelete = await _context.AppUsers.FindAsync(id);
            if (userToDelete == null)
            {
                return NotFound();
            }

            // Delete associated data (tasks, journal entries, goals, reports, work sessions, notifications)
            _context.TaskItems.RemoveRange(_context.TaskItems.Where(t => t.AssignedToUserId == id));
            _context.JournalEntries.RemoveRange(_context.JournalEntries.Where(j => j.UserId == id));
            _context.Goals.RemoveRange(_context.Goals.Where(g => g.UserId == id));
            _context.Reports.RemoveRange(_context.Reports.Where(r => r.UserId == id));
            _context.WorkSessions.RemoveRange(_context.WorkSessions.Where(ws => ws.UserId == id));
            _context.Notifications.RemoveRange(_context.Notifications.Where(n => n.UserId == id));

            // Delete profile picture if it exists and is not the default
            if (!string.IsNullOrEmpty(userToDelete.ProfilePicturePath) && userToDelete.ProfilePicturePath != "/images/default-profile.png")
            {
                var oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, userToDelete.ProfilePicturePath.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            _context.AppUsers.Remove(userToDelete);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Clear();

            return RedirectToAction("GetStarted", "Account"); // Redirect to a public page after deletion
        }
    }
}
