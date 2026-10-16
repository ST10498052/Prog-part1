using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mirrors the Part 1 SQL script: same tables, column types, constraints and
        // NO ACTION (Restrict) foreign keys.

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles", t => t.HasCheckConstraint("CK_Roles_Name", "[RoleName] IN ('Organiser', 'Participant')"));
            e.HasKey(r => r.RoleId);
            e.Property(r => r.RoleName).HasColumnType("varchar(20)").IsRequired();
            e.HasIndex(r => r.RoleName).IsUnique();
            e.HasData(
                new Role { RoleId = 1, RoleName = RoleNames.Organiser },
                new Role { RoleId = 2, RoleName = RoleNames.Participant });
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.UserId);
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(u => u.PhoneNumber).HasColumnType("varchar(20)");
            e.Property(u => u.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
                t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Draft', 'Published', 'Cancelled', 'Completed')");
            });
            e.HasKey(ev => ev.EventId);
            e.Property(ev => ev.Name).HasMaxLength(150).IsRequired();
            e.Property(ev => ev.Description).HasColumnType("nvarchar(max)");
            e.Property(ev => ev.EventDate).HasColumnType("datetime");
            e.Property(ev => ev.Location).HasMaxLength(150).IsRequired();
            e.Property(ev => ev.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(ev => ev.EventType).HasColumnType("varchar(10)").IsRequired();
            e.Property(ev => ev.Status).HasColumnType("varchar(20)").IsRequired();
            e.Property(ev => ev.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(ev => ev.Organiser).WithMany(u => u.OrganisedEvents).HasForeignKey(ev => ev.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(255);
            e.Property(c => c.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(c => c.EntryFee).HasColumnType("decimal(8,2)");
            e.HasOne(c => c.Event).WithMany(ev => ev.Categories).HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled', 'Withdrawn')"));
            e.HasKey(en => en.EnrolmentId);
            e.Property(en => en.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(en => en.BibNumber).HasColumnType("varchar(10)").IsRequired();
            e.HasIndex(en => en.BibNumber).IsUnique();
            e.Property(en => en.Status).HasColumnType("varchar(20)").IsRequired();
            e.HasIndex(en => new { en.ParticipantId, en.EventId }).IsUnique();

            e.HasOne(en => en.Participant).WithMany(u => u.Enrolments).HasForeignKey(en => en.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Event).WithMany(ev => ev.Enrolments).HasForeignKey(en => en.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Category).WithMany(c => c.Enrolments).HasForeignKey(en => en.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DSQ')"));
            e.HasKey(r => r.ResultId);
            e.Property(r => r.FinishTime).HasColumnType("time");
            e.Property(r => r.Status).HasColumnType("varchar(20)").IsRequired();
            e.HasOne(r => r.Enrolment).WithOne(en => en.Result).HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => r.EnrolmentId).IsUnique();
        });
    }
}
