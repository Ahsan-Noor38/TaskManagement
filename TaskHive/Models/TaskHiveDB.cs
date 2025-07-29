using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace TaskHive.Models
{
    public partial class TaskHiveDB : DbContext
    {
        public TaskHiveDB()
            : base("name=TaskHive")
        {
        }

        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<TaskAssignment> TaskAssignments { get; set; }
        public virtual DbSet<Task> Tasks { get; set; }
        public virtual DbSet<TaskUpdate> TaskUpdates { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>()
                .HasMany(e => e.Users)
                .WithRequired(e => e.Role)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Task>()
                .HasMany(e => e.TaskAssignments)
                .WithRequired(e => e.Task)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Task>()
                .HasMany(e => e.TaskUpdates)
                .WithRequired(e => e.Task)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Notifications)
                .WithRequired(e => e.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.TaskAssignments)
                .WithRequired(e => e.User)
                .HasForeignKey(e => e.AssignedTo)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.Tasks)
                .WithRequired(e => e.User)
                .HasForeignKey(e => e.CreatedBy)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasMany(e => e.TaskUpdates)
                .WithRequired(e => e.User)
                .HasForeignKey(e => e.UpdatedBy)
                .WillCascadeOnDelete(false);
        }
    }
}
