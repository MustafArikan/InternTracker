using InternTracker.Data;
using InternTracker.Models; 
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InternTracker.Controllers
{
    [Authorize(Roles = "Intern")] 
    public class InternController : Controller
    {
        private readonly InternTrackerContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; 

        public InternController(InternTrackerContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment; 
        }

        public async Task<IActionResult> MyTasks()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var tasks = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId)
                .ToListAsync();

            return View(tasks);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null || taskItem.AssignedToUserId != userId)
            {
                return NotFound(); 
            }
            return View(taskItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,AssignedDate,Status,AssignedToUserId")] TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || taskItem.AssignedToUserId != userId)
            {
                return Unauthorized();
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
                    if (!TaskItemExists(taskItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(taskItem);
        }

        public async Task<IActionResult> Start(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id && t.AssignedToUserId == userId);
            if (task == null)
            {
                return NotFound();
            }

            var workSession = new WorkSession
            {
                UserId = userId.Value,
                TaskId = id,
                StartTime = DateTime.Now
            };

            _context.WorkSessions.Add(workSession);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("ActiveWorkSessionId", workSession.Id);
            HttpContext.Session.SetInt32("ActiveTaskId", id);

            task.Status = InternTracker.Models.TaskStatus.InProgress;
            _context.Update(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Stop(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var activeWorkSessionId = HttpContext.Session.GetInt32("ActiveWorkSessionId");
            if (activeWorkSessionId == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var workSession = await _context.WorkSessions.FindAsync(activeWorkSessionId);
            if (workSession == null || workSession.TaskId != id)
            {
                return NotFound();
            }

            workSession.EndTime = DateTime.Now;
            workSession.TotalMinutes = (int)(workSession.EndTime - workSession.StartTime).TotalMinutes;
            _context.Update(workSession);

            var task = await _context.TaskItems.FindAsync(id);
            if (task != null)
            {
                task.Status = InternTracker.Models.TaskStatus.Completed;
                _context.Update(task);
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("ActiveWorkSessionId");
            HttpContext.Session.Remove("ActiveTaskId");

            return RedirectToAction(nameof(Index));
        }

        private bool TaskItemExists(int id)
        {
            return _context.TaskItems.Any(e => e.Id == id);
        }

        public IActionResult LogWorkSession()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int _)) 
            {
                // User is not authenticated.
                return RedirectToAction("Login", "Account");
            }

            //  If the user is valid, just show the form.
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogWorkSession(DateTime StartTime, DateTime EndTime)
        {
            if (EndTime <= StartTime)
            {
                ModelState.AddModelError("EndTime", "The end time must be after the start time.");
            }

            // Check if validation passed.
            if (ModelState.IsValid)
            {
                var workSession = new Models.WorkSession
                {
                    StartTime = StartTime,
                    EndTime = EndTime
                };

                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdString, out int userId))
                {
                    workSession.UserId = userId;
                }
                else
                {
                    ModelState.AddModelError("", "Could not identify the current user. Please log in again.");
                    return View(workSession);
                }

                workSession.TotalMinutes = (int)(workSession.EndTime - workSession.StartTime).TotalMinutes;

                _context.Add(workSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); 
            }

            var invalidModel = new Models.WorkSession { StartTime = StartTime, EndTime = EndTime };
            return View(invalidModel);
        }

        public async Task<IActionResult> JournalEntries()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                // User is not authenticated or the claim is missing.
                // Redirecting to login is the correct action here.
                return RedirectToAction("Login", "Account");
            }

            var journalEntries = await _context.JournalEntries
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.Date)
                .ToListAsync();

            return View(journalEntries);
        }

        public IActionResult CreateJournalEntry()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int _)) 
            {
                // User is not authenticated.
                return RedirectToAction("Login", "Account");
            }

            // If the user is valid, just show the form.
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJournalEntry(string EntryText)
        {
            if (string.IsNullOrWhiteSpace(EntryText))
            {
                ModelState.AddModelError("EntryText", "The journal entry cannot be empty.");
            }

            // Check if validation passed.
            if (ModelState.IsValid)
            {
                // Manually create a new JournalEntry object.
                var journalEntry = new Models.JournalEntry
                {
                    EntryText = EntryText,
                    Date = DateTime.Now // Set the date on the server
                };

                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdString, out int userId))
                {
                    journalEntry.UserId = userId;
                }
                else
                {
                    ModelState.AddModelError("", "Could not identify the current user. Please log in again.");
                    return View(journalEntry);
                }

                // Add the now-complete object to the context and save.
                _context.Add(journalEntry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(JournalEntries));
            }

            // If ModelState is invalid, return the view.
            // We create a temporary object to pass back the user's input.
            var invalidModel = new Models.JournalEntry { EntryText = EntryText };
            return View(invalidModel);
        }

        public async Task<IActionResult> EditJournalEntry(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                // User is not authenticated.
                return RedirectToAction("Login", "Account");
            }

            var journalEntry = await _context.JournalEntries.FindAsync(id);
            if (journalEntry == null || journalEntry.UserId != userId)
            {
                return NotFound();
            }

            // If everything is valid, show the form with the entry's data.
            return View(journalEntry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJournalEntry(int id, string EntryText)
        {
            if (string.IsNullOrWhiteSpace(EntryText))
            {
                ModelState.AddModelError("EntryText", "The journal entry cannot be empty.");
            }

            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId))
                {
                    ModelState.AddModelError("", "Could not identify the current user. Please log in again.");
                    var tempModel = new JournalEntry { Id = id, EntryText = EntryText };
                    return View(tempModel);
                }

                var entryToUpdate = await _context.JournalEntries.FindAsync(id);

                if (entryToUpdate == null || entryToUpdate.UserId != userId)
                {
                    return NotFound();
                }

                try
                {
                    entryToUpdate.EntryText = EntryText;
                    entryToUpdate.Date = DateTime.Now;
                    _context.Update(entryToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.JournalEntries.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(JournalEntries));
            }

            var invalidModel = new JournalEntry { Id = id, EntryText = EntryText };
            return View(invalidModel);
        }

        

        public async Task<IActionResult> DeleteJournalEntry(int? id)
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

            var journalEntry = await _context.JournalEntries
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (journalEntry == null)
            {
                return NotFound();
            }

            return View(journalEntry);
        }

        [HttpPost, ActionName("DeleteJournalEntry")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJournalEntryConfirmed(int id)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var journalEntry = await _context.JournalEntries.FindAsync(id);
            if (journalEntry == null || journalEntry.UserId != userId)
            {
                return NotFound();
            }
            
            _context.JournalEntries.Remove(journalEntry);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(JournalEntries));
        }

        private bool JournalEntryExists(int id)
        {
            return _context.JournalEntries.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Goals()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var goals = await _context.Goals
                .Where(g => g.UserId == userId)
                .ToListAsync();

            return View(goals);
        }

        public IActionResult CreateGoal()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int _))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGoal(string GoalTitle)
        {
            if (string.IsNullOrWhiteSpace(GoalTitle))
            {
                ModelState.AddModelError("GoalTitle", "The goal title cannot be empty.");
            }

            if (ModelState.IsValid)
            {
                var goal = new Models.Goal
                {
                    GoalTitle = GoalTitle,
                    CreatedDate = DateTime.Now,
                    Status = Models.GoalStatus.Active,
                    ReflectionText = string.Empty
                };

                // Get user ID and assign it to the new object
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdString, out int userId))
                {
                    goal.UserId = userId;
                }
                else
                {
                    ModelState.AddModelError("", "Could not identify the current user. Please log in again.");
                    return View(goal);
                }

                _context.Add(goal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Goals));
            }

            // If validation fails, return the view with the user's input
            var invalidModel = new Models.Goal { GoalTitle = GoalTitle };
            return View(invalidModel);
        }

        public async Task<IActionResult> EditGoal(int? id)
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

            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            return View(goal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGoal(int id, string GoalTitle, GoalStatus Status, string ReflectionText)
        {
            if (string.IsNullOrWhiteSpace(GoalTitle))
            {
                ModelState.AddModelError("GoalTitle", "The goal title cannot be empty.");
            }

            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized(); 
                }

                var goalToUpdate = await _context.Goals.FindAsync(id);

                if (goalToUpdate == null || goalToUpdate.UserId != userId)
                {
                    return NotFound();
                }

                try
                {
                    goalToUpdate.GoalTitle = GoalTitle;
                    goalToUpdate.Status = Status;
                    goalToUpdate.ReflectionText = ReflectionText ?? string.Empty;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GoalExists(goalToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Goals));
            }

            var invalidModel = new Goal
            {
                Id = id,
                GoalTitle = GoalTitle,
                Status = Status,
                ReflectionText = ReflectionText
            };
            return View(invalidModel);
        }

        public async Task<IActionResult> DeleteGoals(int? id)
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
            var goal = await _context.Goals
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            if (goal == null)
            {
                return NotFound();
            }
            return View(goal);
        }
        [HttpPost, ActionName("DeleteGoals")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGoalsConfirmed(int id)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var goal = await _context.Goals.FindAsync(id);
            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }
            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Goals));
        }
        private bool GoalExists(int id)
        {
            return _context.Goals.Any(e => e.Id == id);
        }


        public async Task<IActionResult> Reports()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var reports = await _context.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.SubmissionDate)
                .ToListAsync();

            return View(reports);
        }

        public IActionResult SubmitReport()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int _))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(string Feedback, IFormFile reportFile)
        {
            if (string.IsNullOrWhiteSpace(Feedback))
            {
                ModelState.AddModelError("Feedback", "The feedback field is required.");
            }
            if (reportFile == null || reportFile.Length == 0)
            {
                ModelState.AddModelError("reportFile", "Please select a file to upload.");
            }

            if (ModelState.IsValid)
            {
                var report = new Models.Report
                {
                    Feedback = Feedback,
                    SubmissionDate = DateTime.Now,
                    MentorFeedback = string.Empty // Initialize MentorFeedback to an empty string
                };

                // Get user ID and assign it to the new object
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdString, out int userId))
                {
                    report.UserId = userId;
                }
                else
                {
                    ModelState.AddModelError("", "Could not identify the current user. Please log in again.");
                    return View(report);
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "reports");
                Directory.CreateDirectory(uploadsFolder); // Does nothing if it already exists

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(reportFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await reportFile.CopyToAsync(fileStream);
                }
                report.FilePath = "/reports/" + uniqueFileName;

                _context.Add(report);
                await _context.SaveChangesAsync();

                // Notify all mentors about the new report
                var mentors = await _context.AppUsers.Where(u => u.Role == UserRole.Mentor).ToListAsync();
                foreach (var mentor in mentors)
                {
                    var notification = new Notification
                    {
                        UserId = mentor.Id,
                        Message = $"New report submitted by intern {User.Identity.Name}: {report.Feedback.Substring(0, Math.Min(report.Feedback.Length, 50))}...",
                        NotificationType = "ReportSubmitted",
                        RelatedEntityId = report.Id
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Reports));
            }

            // If validation fails, return the view with the user's input
            var invalidModel = new Models.Report { Feedback = Feedback };
            return View(invalidModel);
        }
        
        public async Task<IActionResult> EditReport(int? id)
        {
            // This logic is identical to Details for the GET request
            if (id == null) return NotFound();

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Account");

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (report == null) return NotFound();

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReport(int id, string Feedback, IFormFile reportFile)
        {
            if (string.IsNullOrWhiteSpace(Feedback))
            {
                ModelState.AddModelError("Feedback", "The feedback field is required.");
            }

            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

                var reportToUpdate = await _context.Reports
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (reportToUpdate == null) return NotFound();

                reportToUpdate.Feedback = Feedback;

                // Check if a new file was uploaded to replace the old one
                if (reportFile != null && reportFile.Length > 0)
                {
                    // Delete the old file if it exists
                    if (!string.IsNullOrEmpty(reportToUpdate.FilePath))
                    {
                        var oldPhysicalPath = Path.Combine(_webHostEnvironment.WebRootPath, reportToUpdate.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPhysicalPath))
                        {
                            System.IO.File.Delete(oldPhysicalPath);
                        }
                    }

                    // Upload the new file
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "reports");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(reportFile.FileName);
                    var newFilePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await reportFile.CopyToAsync(fileStream);
                    }
                    reportToUpdate.FilePath = "/reports/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Reports));
            }

            var invalidModel = new Models.Report { Id = id, Feedback = Feedback };
            return View(invalidModel);
        }

        public async Task<IActionResult> DeleteReport(int? id)
        {
            if (id == null) return NotFound();

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Account");

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (report == null) return NotFound();

            return View(report);
        }

        [HttpPost, ActionName("DeleteReport")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var reportToDelete = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reportToDelete != null)
            {
                // First, get the path of the file to delete
                var filePathToDelete = reportToDelete.FilePath;

                // Remove the record from the database
                _context.Reports.Remove(reportToDelete);
                await _context.SaveChangesAsync();

                // After successful DB deletion, delete the physical file from the server
                if (!string.IsNullOrEmpty(filePathToDelete))
                {
                    var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, filePathToDelete.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }
            }

            return RedirectToAction(nameof(Reports));
        }


        

        public async Task<IActionResult> ResourceFiles()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var resourceFiles = await _context.ResourceFiles.Include(r => r.UploadedByUser).ToListAsync();

            return View(resourceFiles);
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var intern = await _context.AppUsers.FindAsync(userId.Value);
            if (intern == null)
            {
                return NotFound();
            }

            var tasks = await _context.TaskItems.Where(t => t.AssignedToUserId == userId).ToListAsync();
            var workSessions = await _context.WorkSessions.Where(ws => ws.UserId == userId).ToListAsync();
            var goals = await _context.Goals.Where(g => g.UserId == userId).ToListAsync();
            var reports = await _context.Reports.Where(r => r.UserId == userId).ToListAsync();

            // Prepare data for charts
            var taskStatusCounts = tasks.GroupBy(t => t.Status)
                                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                        .ToList();

            var timeSpentWeekly = workSessions.GroupBy(ws => ws.StartTime.Date.AddDays(-(int)ws.StartTime.DayOfWeek))
                                                .Select(g => new { Week = g.Key, TotalMinutes = g.Sum(ws => ws.TotalMinutes) })
                                                .OrderBy(x => x.Week)
                                                .ToList();

            var goalStatusCounts = goals.GroupBy(g => g.Status)
                                      .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                      .ToList();

            var reportsSubmittedWeekly = reports.GroupBy(r => r.SubmissionDate.Date.AddDays(-(int)r.SubmissionDate.DayOfWeek))
                                                .Select(g => new { Week = g.Key, Count = g.Count() })
                                                .OrderBy(x => x.Week)
                                                .ToList();

            var viewModel = new InternDashboardViewModel
            {
                Intern = intern,
                Tasks = tasks,
                WorkSessions = workSessions,
                Goals = goals,
                Reports = reports,

                TaskStatusLabels = taskStatusCounts.Select(x => x.Status).ToList(),
                TaskStatusCounts = taskStatusCounts.Select(x => x.Count).ToList(),

                TimeSpentWeeklyLabels = timeSpentWeekly.Select(x => x.Week.ToString("yyyy-MM-dd")).ToList(),
                TimeSpentWeeklyData = timeSpentWeekly.Select(x => (double)x.TotalMinutes).ToList(),

                GoalStatusLabels = goalStatusCounts.Select(x => x.Status).ToList(),
                GoalStatusCounts = goalStatusCounts.Select(x => x.Count).ToList(),

                ReportsSubmittedLabels = reportsSubmittedWeekly.Select(x => x.Week.ToString("yyyy-MM-dd")).ToList(),
                ReportsSubmittedCounts = reportsSubmittedWeekly.Select(x => x.Count).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var intern = await _context.AppUsers.FindAsync(userId.Value);
            if (intern == null)
            {
                return NotFound();
            }

            var tasks = await _context.TaskItems.Where(t => t.AssignedToUserId == userId).ToListAsync();
            var workSessions = await _context.WorkSessions.Where(ws => ws.UserId == userId).ToListAsync();
            var goals = await _context.Goals.Where(g => g.UserId == userId).ToListAsync();
            var reports = await _context.Reports.Where(r => r.UserId == userId).ToListAsync();

            // Prepare data for charts
            var taskStatusCounts = tasks.GroupBy(t => t.Status)
                                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                        .ToList();

            var timeSpentWeekly = workSessions.GroupBy(ws => ws.StartTime.Date.AddDays(-(int)ws.StartTime.DayOfWeek))
                                                .Select(g => new { Week = g.Key, TotalMinutes = g.Sum(ws => ws.TotalMinutes) })
                                                .OrderBy(x => x.Week)
                                                .ToList();

            var goalStatusCounts = goals.GroupBy(g => g.Status)
                                      .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                      .ToList();

            var reportsSubmittedWeekly = reports.GroupBy(r => r.SubmissionDate.Date.AddDays(-(int)r.SubmissionDate.DayOfWeek))
                                                .Select(g => new { Week = g.Key, Count = g.Count() })
                                                .OrderBy(x => x.Week)
                                                .ToList();

            var viewModel = new InternDashboardViewModel
            {
                Intern = intern,
                Tasks = tasks,
                WorkSessions = workSessions,
                Goals = goals,
                Reports = reports,

                TaskStatusLabels = taskStatusCounts.Select(x => x.Status).ToList(),
                TaskStatusCounts = taskStatusCounts.Select(x => x.Count).ToList(),

                TimeSpentWeeklyLabels = timeSpentWeekly.Select(x => x.Week.ToString("yyyy-MM-dd")).ToList(),
                TimeSpentWeeklyData = timeSpentWeekly.Select(x => (double)x.TotalMinutes).ToList(),

                GoalStatusLabels = goalStatusCounts.Select(x => x.Status).ToList(),
                GoalStatusCounts = goalStatusCounts.Select(x => x.Count).ToList(),

                ReportsSubmittedLabels = reportsSubmittedWeekly.Select(x => x.Week.ToString("yyyy-MM-dd")).ToList(),
                ReportsSubmittedCounts = reportsSubmittedWeekly.Select(x => x.Count).ToList()
            };

            return View("Dashboard", viewModel);
        }
    }
}