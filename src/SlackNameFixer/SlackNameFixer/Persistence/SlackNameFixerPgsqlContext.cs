using Microsoft.EntityFrameworkCore;

namespace SlackNameFixer.Persistence
{
    public class SlackNameFixerPgsqlContext : SlackNameFixerContext
    {
        public SlackNameFixerPgsqlContext(DbContextOptions<SlackNameFixerPgsqlContext> options) : base(options)
        {
        }
    }
}
