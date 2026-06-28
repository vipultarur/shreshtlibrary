using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApplication1.Data;
using WebApplication1.Models;

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=localhost;Database=shresht;Username=postgres;Password=postgres")
    .Options;

using var context = new ApplicationDbContext(options);
try {
    var plan = new MembershipsMembershipplan {
        Name = "Test", DurationMonths = 1, DurationDays = 0, Price = 100,
        IsActive = true, Benefits = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };
    context.MembershipsMembershipplans.Add(plan);
    context.SaveChanges();
    Console.WriteLine("Success");
} catch (Exception ex) {
    Console.WriteLine(ex.ToString());
}
