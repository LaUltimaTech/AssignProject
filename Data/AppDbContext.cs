using AssignProject.Models;
using Microsoft.EntityFrameworkCore;

namespace AssignProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Admin> adminLogins { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<AssignTask> AssignTasks { get; set; }
        public DbSet<Task_To_Employee> TaskToEmployees { get; set; }
        public DbSet<TaskReminder> TaskReminders { get; set; }

        public DbSet<AssignTaskViewModel> AssignTaskViewModels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>().HasKey(e => e.Employee_ID);

            modelBuilder.Entity<AssignTask>().HasKey(t => t.Task_Unique_ID);
            modelBuilder.Entity<AssignTask>()
                .HasIndex(t => t.Task_Number).IsUnique();

            modelBuilder.Entity<Task_To_Employee>().HasKey(te => te.Task_to_emp_Uniq_ID);

            modelBuilder.Entity<Task_To_Employee>()
                .HasOne(te => te.Task)
                .WithMany(t => t.TaskMappings)
                .HasForeignKey(te => te.Task_Unique_ID);

            modelBuilder.Entity<Task_To_Employee>()
                .HasOne(te => te.Employee)
                .WithMany(e => e.TaskMappings)
                .HasForeignKey(te => te.Employee_ID);

            modelBuilder.Entity<AssignTaskViewModel>().HasNoKey();
            modelBuilder.Entity<AssignTaskViewModel>().Ignore(x => x.SelectedEmployeeIds);
            modelBuilder.Entity<AssignTaskViewModel>().Ignore(x => x.AvailableEmployees);
        }
    }
}
