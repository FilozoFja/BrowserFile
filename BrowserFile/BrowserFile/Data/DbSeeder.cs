using BrowserFile.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace BrowserFile.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();


            var adminEmail = "admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, "Admin123!");

            }
        }
        public static void SeedIcons(ApplicationDbContext context)
        {
            if (!context.Icons.Any())
            {
                var icons = new List<Icon>
                {
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "arrow-to-bottom-stroke", Url = "/icons/arrow-to-bottom-stroke.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "chart-gantt", Url = "/icons/chart-gantt.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "cupcake", Url = "/icons/cupcake.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "database-alt", Url = "/icons/database-alt.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "dumbbell", Url = "/icons/dumbbell.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "education", Url = "/icons/education.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "file", Url = "/icons/file.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "film-roll-alt", Url = "/icons/film-roll-alt.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "folder", Url = "/icons/folder.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "folder-code", Url = "/icons/folder-code.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "keyframe-hold-ease-in", Url = "/icons/keyframe-hold-ease-in.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "microphone-alt", Url = "/icons/microphone-alt.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "movie", Url = "/icons/movie.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "music", Url = "/icons/music.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "palette", Url = "/icons/palette.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "paper-plane", Url = "/icons/paper-plane.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "save", Url = "/icons/save.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "trash-x", Url = "/icons/trash-x.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "unlink", Url = "/icons/unlink.svg" },
                    new Icon { Id = Guid.NewGuid().ToString(), Name = "hot", Url = "/icons/hot.svg" },
                };

                context.Icons.AddRange(icons);
                context.SaveChanges();
            }
        }
    }
}
