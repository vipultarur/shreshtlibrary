using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace WebApplication1
{
    public class CheckDbScript
    {
        public static async Task Run()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddJsonFile("appsettings.json");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            
            var app = builder.Build();
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try 
            {
                var count = await context.LibraryDatabasefiles.CountAsync();
                Console.WriteLine($"LibraryDatabasefiles count: {count}");
                var files = await context.LibraryDatabasefiles.Take(5).ToListAsync();
                foreach(var f in files) {
                    Console.WriteLine($"File: {f.Name}");
                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
