using Microsoft.EntityFrameworkCore;

namespace SlackNameFixer.Persistence
{
    public class SlackNameFixerSqlServerContext : SlackNameFixerContext
    {
        public SlackNameFixerSqlServerContext(DbContextOptions<SlackNameFixerSqlServerContext> options) : base(options)
        {
        }
    }
}
