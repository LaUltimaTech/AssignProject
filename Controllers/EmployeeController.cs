using Microsoft.AspNetCore.Mvc;
using AssignProject.Data;
using AssignProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace AssignProject.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Employee/Index
        public async Task<IActionResult> Index(int? editId, [FromQuery] string searchString)
        {
            var employees = _context.Employees.Where(e => e.IsActive);

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    (e.Employee_First_Name + " " + (e.Employee_Middle_Name ?? "") + " " + e.Employee_Last_Name)
                    .Contains(searchString));
            }

            var formEmployee = editId != null
                ? await _context.Employees.FindAsync(editId) ?? new Employee()
                : new Employee();

            ViewData["EmployeeList"] = await employees.ToListAsync();
            return View(formEmployee); // ✅ Pass single Employee model
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                // ✅ Ensure WhatsApp number starts with "91"
                if (employee.Employee_WhatsApp_Number != 0)
                {
                    string number = employee.Employee_WhatsApp_Number.ToString();

                    // Add "91" prefix only if it’s not already there
                    if (!number.StartsWith("91"))
                        number = "91" + number;

                    // Convert back to long before saving
                    employee.Employee_WhatsApp_Number = long.Parse(number);
                }

                // ✅ Check if WhatsApp number already exists for an active employee
                bool exists = await _context.Employees
                    .AnyAsync(e => e.IsActive && e.Employee_WhatsApp_Number == employee.Employee_WhatsApp_Number);

                if (exists)
                {
                    TempData["Message"] = "⚠️ WhatsApp number is already registered with an employee!";
                    ViewData["EmployeeList"] = await _context.Employees.Where(e => e.IsActive).ToListAsync();
                    return View("Index", employee);
                }

                // ✅ Set employee as active and save
                employee.IsActive = true;
                _context.Add(employee);
                await _context.SaveChangesAsync();

                TempData["Message"] = "✅ Employee registered successfully!";
                return RedirectToAction(nameof(Index));
            }

            // ✅ If ModelState is invalid, reload the active employee list
            ViewData["EmployeeList"] = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            return View("Index", employee);
        }


        // POST: Employee/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Employees.FindAsync(employee.Employee_ID);
                if (existing == null)
                    return NotFound();

                existing.Employee_First_Name = employee.Employee_First_Name;
                existing.Employee_Middle_Name = employee.Employee_Middle_Name;
                existing.Employee_Last_Name = employee.Employee_Last_Name;
                existing.Employee_WhatsApp_Number = employee.Employee_WhatsApp_Number;

                await _context.SaveChangesAsync();
                TempData["Message"] = "Employee updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["EmployeeList"] = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            return View("Index", employee); // ✅ Pass single Employee model
        }

        // GET: Employee/Delete
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Employee_ID == id);
            if (employee != null)
            {
                employee.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Employee deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}
