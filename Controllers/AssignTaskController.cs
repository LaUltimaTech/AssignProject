using AssignProject.Data;
using AssignProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AssignProject.Controllers
{
    [Authorize]
    public class AssignTaskController : Controller
    {
        private readonly AppDbContext _context;

        public AssignTaskController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ✅ Auto-stop WhatsApp for overdue tasks
            var today = DateTime.Today;
            var overdueTasks = await _context.AssignTasks
                .Where(t => t.WhatsAppStatus == "Active" && t.Approx_Completion_Date < today)
                .ToListAsync();

            foreach (var task in overdueTasks)
            {
                task.WhatsAppStatus = "Stopped";
            }

            if (overdueTasks.Any())
            {
                await _context.SaveChangesAsync();
            }

            // ✅ Load page model
            var model = new AssignTaskPageViewModel
            {
                FormModel = new AssignTaskViewModel
                {
                    AvailableEmployees = await _context.Employees
                        .Where(e => e.IsActive)
                        .ToListAsync(),
                    Task_Number = 0
                },
                AllTasks = await _context.AssignTaskViewModels
                    .FromSqlRaw("EXEC usp_GetAllTasksWithEmployees")
                    .ToListAsync()
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Create(List<int> SelectedEmployeeIds, AssignTaskViewModel formModel)
        {
            if (SelectedEmployeeIds != null && SelectedEmployeeIds.Any())
            {
                var employeeIds = string.Join(",", SelectedEmployeeIds);

                // Prepare parameters for stored procedure, including output for Task_Unique_ID
                var taskIdParam = new SqlParameter("@Task_Unique_ID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new[]
                {
     new SqlParameter("@Task_Description", formModel.Task_Description ?? (object)DBNull.Value),
     new SqlParameter("@Task_Assigned_Date", formModel.Task_Assigned_Date),
     new SqlParameter("@Approx_Completion_Date", formModel.Approx_Completion_Date),
     new SqlParameter("@Employee_IDs", employeeIds),
     taskIdParam
 };

                // Execute stored procedure to insert task and retrieve Task_Unique_ID
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC usp_InsertTaskMulti @Task_Description, @Task_Assigned_Date, @Approx_Completion_Date, @Employee_IDs, @Task_Unique_ID OUTPUT",
                    parameters);

                int taskUniqueId = (int)taskIdParam.Value;

                // Send WhatsApp messages to selected employees
                var employees = await _context.Employees
                    .Where(e => SelectedEmployeeIds.Contains(e.Employee_ID))
                    .ToListAsync();

                foreach (var emp in employees)
                {
                    var message = $"Hello {emp.Employee_First_Name},\nYou have been assigned a new task:\n" +
                                  $"Description: {formModel.Task_Description}\n" +
                                  $"Assigned Date: {formModel.Task_Assigned_Date:dd-MM-yyyy}\n" +
                                  $"Completion Date: {formModel.Approx_Completion_Date:dd-MM-yyyy}";

                    await SendWhatsAppMessageAsync(emp.Employee_WhatsApp_Number.ToString(), message);
                }

                // Generate reminder timestamps every 4 hours, respecting office hours
                var reminderTimestamps = GeneratePreciseOfficeReminders(
                    DateTime.Now, // Task creation time
                    formModel.Approx_Completion_Date,
                    intervalHours: 4
                );

                // Insert each reminder into tbl_task_reminder
                foreach (var triggerTime in reminderTimestamps)
                {
                    var reminderInsert = new[]
                    {
         new SqlParameter("@Task_Unique_ID", taskUniqueId),
         new SqlParameter("@Message_Description", formModel.Task_Description ?? "Task Reminder"),
         new SqlParameter("@Trigger_Timestamp", triggerTime),
         new SqlParameter("@Reminder_Flag", true)
     };

                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO tbl_task_reminder (Task_Unique_ID, Message_Description, Trigger_Timestamp, Reminder_Flag) " +
                        "VALUES (@Task_Unique_ID, @Message_Description, @Trigger_Timestamp, @Reminder_Flag)",
                        reminderInsert);
                }

                TempData["Message"] = "Task has been successfully created, WhatsApp messages sent, and reminders scheduled.";
                return RedirectToAction(nameof(Index));
            }

            // If no employees selected, reload form with existing data
            var model = new AssignTaskPageViewModel
            {
                FormModel = formModel,
                AllTasks = await _context.AssignTaskViewModels
                    .FromSqlRaw("EXEC usp_GetAllTasksWithEmployees")
                    .ToListAsync()
            };

            model.FormModel.AvailableEmployees = await _context.Employees
                .Where(e => e.IsActive)
                .ToListAsync();

            return View("Index", model);
        }




        private List<DateTime> GeneratePreciseOfficeReminders(DateTime start, DateTime end, int intervalHours = 4)
        {
            var reminders = new List<DateTime>();
            var current = start;

            var officeStart = new TimeSpan(10, 0, 0); // 10:00 AM
            var officeEnd = new TimeSpan(18, 0, 0);   // 6:00 PM

            while (current <= end)
            {
                var next = current.AddHours(intervalHours);

                // ✅ If next is within office hours, add it directly
                if (next.TimeOfDay >= officeStart && next.TimeOfDay < officeEnd)
                {
                    reminders.Add(next);
                    current = next;
                }
                else
                {
                    // ✅ Preserve minute and second
                    var minute = next.Minute;
                    var second = next.Second;

                    // ✅ Shift to next day at 10 AM + preserved minute/second
                    var nextDay = next.Date.AddDays(1).Add(officeStart).AddMinutes(minute).AddSeconds(second);

                    // ✅ If still outside office hours, reset to 10:00 AM sharp
                    if (nextDay.TimeOfDay < officeStart || nextDay.TimeOfDay >= officeEnd)
                    {
                        nextDay = next.Date.AddDays(1).Add(officeStart);
                    }

                    if (nextDay <= end)
                    {
                        reminders.Add(nextDay);
                        current = nextDay;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return reminders;
        }










        private async Task SendWhatsAppMessageAsync(string phoneNumber, string message)
        {
            var authKey = "cmhzyz6ea0lqwjx4kp47h2zmw"; // Replace with your actual token
            var encodedMessage = Uri.EscapeDataString(message);
            var url = $"https://wts.vision360solutions.co.in/api/sendText?token={authKey}&phone={phoneNumber}&message={encodedMessage}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            // Optional: handle response status
        }



        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.AssignTasks
                .Include(t => t.TaskMappings)
                .FirstOrDefaultAsync(t => t.Task_Unique_ID == id);

            if (task == null) return NotFound();

            var model = new AssignTaskPageViewModel
            {
                FormModel = new AssignTaskViewModel
                {
                    Task_Unique_ID = task.Task_Unique_ID,
                    Task_Number = task.Task_Number,
                    Task_Description = task.Task_Description,
                    Task_Assigned_Date = task.Task_Assigned_Date,
                    Approx_Completion_Date = task.Approx_Completion_Date,
                    SelectedEmployeeIds = task.TaskMappings.Select(m => m.Employee_ID).ToList(),
                    AvailableEmployees = await _context.Employees
                        .Where(e => e.IsActive)
                        .ToListAsync()
                },
                AllTasks = await _context.AssignTaskViewModels
                    .FromSqlRaw("EXEC usp_GetAllTasksWithEmployees")
                    .ToListAsync()
            };

            return View("Index", model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(List<int> SelectedEmployeeIds, AssignTaskViewModel formModel)
        {
            if (SelectedEmployeeIds != null && SelectedEmployeeIds.Any())
            {
                var employeeIds = string.Join(",", SelectedEmployeeIds);

                var parameters = new[]
                {
            new SqlParameter("@Task_Unique_ID", formModel.Task_Unique_ID),
            new SqlParameter("@Task_Description", formModel.Task_Description ?? (object)DBNull.Value),
            new SqlParameter("@Task_Assigned_Date", formModel.Task_Assigned_Date),
            new SqlParameter("@Approx_Completion_Date", formModel.Approx_Completion_Date),
            new SqlParameter("@Employee_IDs", employeeIds)
        };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC usp_UpdateTask @Task_Unique_ID, @Task_Description, @Task_Assigned_Date, @Approx_Completion_Date, @Employee_IDs",
                    parameters);

                // ✅ Send WhatsApp messages only if task is still active
                if (formModel.Approx_Completion_Date >= DateTime.Today)
                {
                    var employees = await _context.Employees
                        .Where(e => SelectedEmployeeIds.Contains(e.Employee_ID))
                        .ToListAsync();

                    foreach (var emp in employees)
                    {
                        var message = $"Hello {emp.Employee_First_Name},\nYour task has been updated:\n" +
                                      $"Description: {formModel.Task_Description}\n" +
                                      $"Assigned Date: {formModel.Task_Assigned_Date:dd-MM-yyyy}\n" +
                                      $"Completion Date: {formModel.Approx_Completion_Date:dd-MM-yyyy}";

                        await SendWhatsAppMessageAsync(emp.Employee_WhatsApp_Number.ToString(), message);
                    }
                }

                TempData["Message"] = "Task has been successfully updated.";
                return RedirectToAction(nameof(Index));
            }

            // Reload view if no employees selected
            var model = new AssignTaskPageViewModel
            {
                FormModel = formModel,
                AllTasks = await _context.AssignTaskViewModels
                    .FromSqlRaw("EXEC usp_GetAllTasksWithEmployees")
                    .ToListAsync()
            };

            model.FormModel.AvailableEmployees = await _context.Employees
                .Where(e => e.IsActive)
                .ToListAsync();

            return View("Index", model);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Step 1: Delete task via stored procedure
            var parameter = new SqlParameter("@Task_Unique_ID", id);
            await _context.Database.ExecuteSqlRawAsync("EXEC usp_DeleteTask @Task_Unique_ID", parameter);

            // Step 2: Deactivate related reminders
            var reminders = await _context.TaskReminders
                .Where(r => r.Task_Unique_ID == id && r.Reminder_Flag == true)
                .ToListAsync();

            foreach (var reminder in reminders)
            {
                reminder.Reminder_Flag = false;
            }

            if (reminders.Any())
            {
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Task has been deleted and related reminders deactivated.";
            return RedirectToAction(nameof(Index));
        }


        public async Task<JsonResult> GetNewTaskNumber()
        {
            var today = DateTime.Today;
            var fyStart = new DateTime(today.Month < 4 ? today.Year - 1 : today.Year, 4, 1);

            var maxTaskNo = await _context.AssignTasks
                .Where(t => t.Task_Assigned_Date >= fyStart)
                .MaxAsync(t => (int?)t.Task_Number) ?? 0;

            return Json(maxTaskNo + 1);
        }

        [HttpPost]

        public async Task<IActionResult> StopWhatsApp(int id)
        {
            var task = await _context.AssignTasks.FirstOrDefaultAsync(t => t.Task_Unique_ID == id);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToAction(nameof(Index));
            }

            // Step 1: Stop WhatsApp reminders
            task.WhatsAppStatus = "Stopped";
            await _context.SaveChangesAsync();

            // Step 2: Stop reminder flag in tbl_task_reminder
            var reminders = await _context.TaskReminders
                .Where(r => r.Task_Unique_ID == id && r.Reminder_Flag == true)
                .ToListAsync();

            foreach (var reminder in reminders)
            {
                reminder.Reminder_Flag = false;
            }

            if (reminders.Any())
            {
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "WhatsApp reminders have been stopped and reminder flags updated.";
            return RedirectToAction(nameof(Index));
        }

    }
}
