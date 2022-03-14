using Microsoft.EntityFrameworkCore;

namespace SlackNameFixer.Persistence
{
    public class SlackNameFixerContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public SlackNameFixerContext(DbContextOptions<SlackNameFixerContext> options) : base(options)
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
