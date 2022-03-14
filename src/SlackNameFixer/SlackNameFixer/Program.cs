using Microsoft.EntityFrameworkCore;
using SlackNameFixer;
using SlackNameFixer.Infrastructure;
using SlackNameFixer.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Host.ConfigureAppConfiguration((context, _) =>
{
    builder.Services.Configure<SlackOptions>(context.Configuration.GetSection("SlackOptions"));
});
builder.Services.AddHttpClient();
builder.Services.AddDbContext<SlackNameFixerContext>(optionsBuilder =>
    optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("slacknamefixerdb")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SlackNameFixerContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseAuthorization();

app.MapControllers();
app.UseMiddleware<EnableRequestBodyBufferingMiddleware>();
app.Run();
