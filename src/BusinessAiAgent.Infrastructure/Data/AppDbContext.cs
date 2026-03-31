using BusinessAiAgent.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<CallLog> CallLogs => Set<CallLog>();
    public DbSet<SmsRecord> SmsRecords => Set<SmsRecord>();
    public DbSet<EmailRecord> EmailRecords => Set<EmailRecord>();
    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasOne(u => u.Contact)
                .WithMany()
                .HasForeignKey(u => u.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Company>(e =>
        {
            e.HasMany(co => co.Contacts)
                .WithOne(c => c.Company)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Contact>(e =>
        {
            e.HasIndex(c => c.Email);
            e.HasIndex(c => c.Phone);
        });

        modelBuilder.Entity<Conversation>(e =>
        {
            e.HasOne(c => c.Contact)
                .WithMany(ct => ct.Conversations)
                .HasForeignKey(c => c.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            e.Property(c => c.Channel).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(m => m.Direction).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<CallLog>(e =>
        {
            e.HasOne(cl => cl.Conversation)
                .WithMany(c => c.CallLogs)
                .HasForeignKey(cl => cl.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(cl => cl.CallSid).IsUnique();
        });

        modelBuilder.Entity<SmsRecord>(e =>
        {
            e.HasOne(s => s.Conversation)
                .WithMany(c => c.SmsRecords)
                .HasForeignKey(s => s.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => s.MessageSid).IsUnique();
        });

        modelBuilder.Entity<EmailRecord>(e =>
        {
            e.HasOne(er => er.Conversation)
                .WithMany(c => c.EmailRecords)
                .HasForeignKey(er => er.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
