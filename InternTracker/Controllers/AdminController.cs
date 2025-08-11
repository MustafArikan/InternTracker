using InternTracker.Data;
using InternTracker.Models;
using Microsoft.AspNetCore.Authorization; // Added for Authorize attribute
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InternTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly InternTrackerContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(InternTrackerContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.AppUsers.ToListAsync();
            return View(users);
        }

        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(AdminCreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for existing user first 
                var existingUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View(model);
                }

                var appUser = new AppUser
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = model.Role,
                    ProfilePicturePath = "" // <-- THE FIX
                };

                _context.Add(appUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> EditUser(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUser = await _context.AppUsers.FindAsync(id);
            if (appUser == null)
            {
                return NotFound();
            }
            return View(appUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id) 
        {
            var userToUpdate = await _context.AppUsers.FindAsync(id);

            if (userToUpdate == null)
            {
                return NotFound("Unable to find user to update.");
            }

            
            if (await TryUpdateModelAsync<AppUser>(
                userToUpdate,
                "", 
                u => u.Role)) 
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "The user was updated by another administrator. Please go back and try again.");
                }
            }

            return View(userToUpdate);
        }

        private bool AppUserExists(int id)
        {
            return _context.AppUsers.Any(e => e.Id == id);
        }

        public async Task<IActionResult> DeleteUser(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appUser == null)
            {
                return NotFound();
            }

            return View(appUser);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appUser = await _context.AppUsers.FindAsync(id);
            if (appUser != null)
            {
                _context.AppUsers.Remove(appUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ResourceFiles()
        {
            var resourceFiles = await _context.ResourceFiles.Include(r => r.UploadedByUser).ToListAsync();
            return View(resourceFiles);
        }

        public IActionResult UploadResourceFile()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResourceFile(string Title, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError("Title", "The Title field is required.");
            }
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file to upload.");
            }

            if (ModelState.IsValid)
            {
                var resourceFile = new ResourceFile
                {
                    Title = Title, 
                    UploadDate = DateTime.Now 
                };

                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdString, out int userId))
                {
                    resourceFile.UploadedByUserId = userId;
                }
                else
                {
                    
                    ModelState.AddModelError("", "Could not identify the current user. Please log in again.");
                    return View(); 
                }


                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "resources");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                resourceFile.FilePath = "/resources/" + uniqueFileName;


                _context.Add(resourceFile);
                await _context.SaveChangesAsync();

                var interns = await _context.AppUsers.Where(u => u.Role == UserRole.Intern).ToListAsync();
                foreach (var intern in interns)
                {
                    var notification = new Notification
                    {
                        UserId = intern.Id,
                        Message = $"New resource uploaded: {resourceFile.Title}",
                        NotificationType = "ResourceUploaded",
                        RelatedEntityId = resourceFile.Id
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ResourceFiles));
            }

            
            return View();
        }

        public async Task<IActionResult> DeleteResourceFile(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resourceFile = await _context.ResourceFiles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resourceFile == null)
            {
                return NotFound();
            }

            return View(resourceFile);
        }

        [HttpPost, ActionName("DeleteResourceFile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResourceFileConfirmed(int id)
        {
            var resourceFile = await _context.ResourceFiles.FindAsync(id);
            if (resourceFile != null)
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, resourceFile.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _context.ResourceFiles.Remove(resourceFile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ResourceFiles));
        }

        public async Task<IActionResult> EditResourceFile(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resourceFile = await _context.ResourceFiles.FindAsync(id);
            if (resourceFile == null)
            {
                return NotFound();
            }
            return View(resourceFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResourceFile(int id, string Title, IFormFile file)
        {
            var resourceFile = await _context.ResourceFiles.FindAsync(id);
            if (resourceFile == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                resourceFile.Title = Title;

                if (file != null && file.Length > 0)
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, resourceFile.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "resources");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                    var newFilePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    resourceFile.FilePath = "/resources/" + uniqueFileName;
                }

                _context.Update(resourceFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ResourceFiles));
            }
            return View(resourceFile);
        }

        public async Task<IActionResult> SystemReports(DateTime? startDate)
        {
            if (!startDate.HasValue)
            {
                startDate = new DateTime(2025, 7, 28); 
            }

            IQueryable<AppUser> usersQuery = _context.AppUsers.Where(u => u.RegistrationDate >= startDate.Value);

            var registrationData = await usersQuery
                .GroupBy(u => new { u.RegistrationDate.Year, u.RegistrationDate.Month, u.RegistrationDate.Day })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Day = g.Key.Day, Count = g.Count() })
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
                .ToListAsync();

            var formattedRegistrationDates = registrationData.Select(x => new DateTime(x.Year, x.Month, x.Day).ToString("dd MMM yyyy")).ToList();
            var registrationCounts = registrationData.Select(x => x.Count).ToList();

            var viewModel = new SystemReportsViewModel
            {
                TotalUsers = await _context.AppUsers.CountAsync(),
                TotalInterns = await _context.AppUsers.CountAsync(u => u.Role == UserRole.Intern),
                TotalMentors = await _context.AppUsers.CountAsync(u => u.Role == UserRole.Mentor),
                TotalAdmins = await _context.AppUsers.CountAsync(u => u.Role == UserRole.Admin),
                TotalMentorUploads = await _context.ResourceFiles.Include(r => r.UploadedByUser).CountAsync(r => r.UploadedByUser.Role == UserRole.Mentor),
                TotalAdminUploads = await _context.ResourceFiles.Include(r => r.UploadedByUser).CountAsync(r => r.UploadedByUser.Role == UserRole.Admin),
                RegistrationDates = formattedRegistrationDates,
                RegistrationCounts = registrationCounts
            };
            return View(viewModel);
        }
    }
}