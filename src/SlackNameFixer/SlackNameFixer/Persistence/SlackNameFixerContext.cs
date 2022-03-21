using Microsoft.EntityFrameworkCore;

namespace SlackNameFixer.Persistence
{
    public abstract class SlackNameFixerContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected SlackNameFixerContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>()
                .HasIndex(t => new
                {
                    t.TeamId,
                    t.UserId,
                }).IsUnique();
        }
    }
}
