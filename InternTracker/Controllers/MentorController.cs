using InternTracker.Data;
using InternTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Added for Authorize attribute

namespace InternTracker.Controllers
{
    [Authorize(Roles = "Mentor")]
    public class MentorController : Controller
    {
        private readonly InternTrackerContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MentorController(InternTrackerContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var interns = await _context.AppUsers
                .Where(u => u.Role == UserRole.Intern)
                .ToListAsync();
            return View(interns);
        }

        public async Task<IActionResult> ViewInternDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var intern = await _context.AppUsers.FindAsync(id);
            if (intern == null || intern.Role != UserRole.Intern)
            {
                return NotFound();
            }

            var viewModel = new InternDetailsViewModel
            {
                Intern = intern,
                Tasks = await _context.TaskItems.Where(t => t.AssignedToUserId == id).ToListAsync(),
                JournalEntries = await _context.JournalEntries.Where(j => j.UserId == id).ToListAsync(),
                Goals = await _context.Goals.Where(g => g.UserId == id).ToListAsync(),
                Reports = await _context.Reports.Where(r => r.UserId == id).ToListAsync(),
                WorkSessions = await _context.WorkSessions.Where(ws => ws.UserId == id).ToListAsync(),
                Notifications = await _context.Notifications.Where(n => n.UserId == id).ToListAsync()
            };

            return View(viewModel);
        }


        public async Task<IActionResult> AssignTask(int? id)
        {
            if (id == null)
            {
                return BadRequest("An Intern ID must be provided to assign a task.");
            }

            var intern = await _context.AppUsers.FindAsync(id);
            if (intern == null || intern.Role != UserRole.Intern)
            {
                return NotFound("The specified intern could not be found.");
            }


            var taskItem = new TaskItem { AssignedToUserId = id.Value };

            ViewBag.InternUsername = intern.Username;

            return View(taskItem);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTask([Bind("Title,Description,AssignedToUserId")] TaskItem taskDataFromForm)
        {
            if (ModelState.IsValid)
            {
                // Create a brand new TaskItem instance.
                var newTask = new TaskItem
                {
                    // Map the safe properties from the form data to our new object.
                    AssignedToUserId = taskDataFromForm.AssignedToUserId,
                    Title = taskDataFromForm.Title,
                    Description = taskDataFromForm.Description,

                    // Set the server-side properties that the user can't control.
                    AssignedDate = DateTime.Now,
                    Status = Models.TaskStatus.NotStarted
                };

                // Add the *new* object to the context.
                _context.Add(newTask);
                await _context.SaveChangesAsync();

                // Redirect to the intern's details page.
                return RedirectToAction(nameof(ViewInternDetails), new { id = newTask.AssignedToUserId });
            }

            // If validation fails, reload the page with the user's data and error messages.
            var intern = await _context.AppUsers.FindAsync(taskDataFromForm.AssignedToUserId);
            if (intern != null)
            {
                ViewBag.InternUsername = intern.Username;
            }

            return View(taskDataFromForm);
        }

        public async Task<IActionResult> EditTask(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null)
            {
                return NotFound();
            }
            return View(taskItem);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTask(int id, [Bind("Id,Title,Description,Status,AssignedDate,AssignedToUserId")] TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TaskItems.Any(e => e.Id == taskItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // After editing, go back to the intern's details page
                return RedirectToAction(nameof(ViewInternDetails), new { id = taskItem.AssignedToUserId });
            }
            return View(taskItem);
        }


        public async Task<IActionResult> DeleteTask(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems
                .Include(t => t.AssignedToUser) // Include user details for the confirmation page
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
        }


        [HttpPost, ActionName("DeleteTask")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTaskConfirmed(int id)
        {
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem != null)
            {
                var internId = taskItem.AssignedToUserId; // Save the intern's ID before deleting
                _context.TaskItems.Remove(taskItem);
                await _context.SaveChangesAsync();

                // After deleting, go back to the intern's details page
                return RedirectToAction(nameof(ViewInternDetails), new { id = internId });
            }

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
                return RedirectToAction(nameof(ResourceFiles));
            }


            return View();
        }

        public async Task<IActionResult> EditResourceFile(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var resourceFile = await _context.ResourceFiles.FindAsync(id);

            if (resourceFile == null || resourceFile.UploadedByUserId != userId)
            {
                return NotFound();
            }

            return View(resourceFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResourceFile(int id, string Title)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError("Title", "The Title field is required.");
            }

            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int currentUserId))
                {
                    return Unauthorized(); 
                }

                var resourceToUpdate = await _context.ResourceFiles.FindAsync(id);

                if (resourceToUpdate == null || resourceToUpdate.UploadedByUserId != currentUserId)
                {
                    return NotFound();
                }

                try
                {
                    resourceToUpdate.Title = Title;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(ResourceFiles));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Unable to save changes. The resource was modified by another user.");
                }
            }

            var modelToReturn = await _context.ResourceFiles.FindAsync(id);
            if (modelToReturn != null)
            {
                modelToReturn.Title = Title; 
                return View(modelToReturn);
            }

            return NotFound();
        }

        public async Task<IActionResult> GiveFeedback(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var report = await _context.Reports
                .Include(r => r.User) // Include the User to display intern's name
                .FirstOrDefaultAsync(m => m.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiveFeedback(int id, string mentorFeedback)
        {
            var reportToUpdate = await _context.Reports.FindAsync(id);

            if (reportToUpdate == null)
            {
                return NotFound();
            }

            reportToUpdate.MentorFeedback = mentorFeedback;
            _context.Update(reportToUpdate);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewInternDetails), new { id = reportToUpdate.UserId });
        }

        public async Task<IActionResult> InternProgressDashboard(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var intern = await _context.AppUsers.FindAsync(id);
            if (intern == null || intern.Role != UserRole.Intern)
            {
                return NotFound();
            }

            var tasks = await _context.TaskItems.Where(t => t.AssignedToUserId == id).ToListAsync();
            var journalEntries = await _context.JournalEntries.Where(j => j.UserId == id).ToListAsync();
            var goals = await _context.Goals.Where(g => g.UserId == id).ToListAsync();
            var reports = await _context.Reports.Where(r => r.UserId == id).ToListAsync();
            var workSessions = await _context.WorkSessions.Include(ws => ws.TaskItem).Where(ws => ws.UserId == id).ToListAsync();

            // Prepare data for individual work session chart
            var individualWorkSessions = workSessions
                .OrderBy(ws => ws.StartTime)
                .Select(ws => new { Label = ws.StartTime.ToString("MM/dd HH:mm"), ws.TotalMinutes })
                .ToList();
            var notifications = await _context.Notifications.Where(n => n.UserId == id).ToListAsync();

            // Prepare data for charts
            var taskStatusCounts = tasks.GroupBy(t => t.Status)
                                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                        .ToList();

            var goalStatusCounts = goals.GroupBy(g => g.Status)
                                      .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                      .ToList();

            var workSessionDailyMinutes = workSessions.GroupBy(ws => ws.StartTime.Date)
                                                    .Select(g => new { Date = g.Key, TotalMinutes = g.Sum(ws => ws.TotalMinutes) })
                                                    .OrderBy(x => x.Date)
                                                    .ToList();

            var journalEntryDailyCounts = journalEntries.GroupBy(je => je.Date.Date)
                                                        .Select(g => new { Date = g.Key, Count = g.Count() })
                                                        .OrderBy(x => x.Date)
                                                        .ToList();

            var reportSubmissionDailyCounts = reports.GroupBy(r => r.SubmissionDate.Date)
                                                    .Select(g => new { Date = g.Key, Count = g.Count() })
                                                    .OrderBy(x => x.Date)
                                                    .ToList();

            // Prepare activity logs
            var activityLogs = new List<string>();
            foreach (var task in tasks.OrderByDescending(t => t.AssignedDate))
            {
                activityLogs.Add($"Task '{task.Title}' assigned on {task.AssignedDate.ToShortDateString()} with status {task.Status}.");
            }
            foreach (var journal in journalEntries.OrderByDescending(j => j.Date))
            {
                activityLogs.Add($"Journal entry on {journal.Date.ToShortDateString()}: {journal.EntryText.Substring(0, Math.Min(journal.EntryText.Length, 50))}...");
            }
            foreach (var report in reports.OrderByDescending(r => r.SubmissionDate))
            {
                activityLogs.Add($"Report submitted on {report.SubmissionDate.ToShortDateString()}.");
            }
            foreach (var ws in workSessions.OrderByDescending(ws => ws.StartTime))
            {
                activityLogs.Add($"Work session logged from {ws.StartTime.ToShortTimeString()} to {ws.EndTime.ToShortTimeString()} on {ws.StartTime.ToShortDateString()} ({ws.TotalMinutes} minutes).");
            }
            // Add more activity types as needed
            activityLogs = activityLogs.OrderByDescending(a => a).ToList(); // Simple chronological sort

            var viewModel = new InternProgressDashboardViewModel
            {
                Intern = intern,
                Tasks = tasks,
                JournalEntries = journalEntries,
                Goals = goals,
                Reports = reports,
                WorkSessions = workSessions,
                Notifications = notifications,

                TaskStatusLabels = taskStatusCounts.Select(x => x.Status).ToList(),
                TaskStatusCounts = taskStatusCounts.Select(x => x.Count).ToList(),

                GoalStatusLabels = goalStatusCounts.Select(x => x.Status).ToList(),
                GoalStatusCounts = goalStatusCounts.Select(x => x.Count).ToList(),

                WorkSessionDates = workSessionDailyMinutes.Select(x => x.Date.ToShortDateString()).ToList(),
                WorkSessionMinutes = workSessionDailyMinutes.Select(x => x.TotalMinutes).ToList(),

                JournalEntryDates = journalEntryDailyCounts.Select(x => x.Date.ToShortDateString()).ToList(),
                JournalEntryCounts = journalEntryDailyCounts.Select(x => x.Count).ToList(),

                ReportSubmissionDates = reportSubmissionDailyCounts.Select(x => x.Date.ToShortDateString()).ToList(),
                ReportSubmissionCounts = reportSubmissionDailyCounts.Select(x => x.Count).ToList(),

                IndividualWorkSessionLabels = individualWorkSessions.Select(x => x.Label).ToList(),
                IndividualWorkSessionMinutes = individualWorkSessions.Select(x => x.TotalMinutes).ToList(),

                ActivityLogs = activityLogs
            };

            return View(viewModel);
        }
    }
}