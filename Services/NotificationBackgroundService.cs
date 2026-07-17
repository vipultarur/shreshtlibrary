using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing scheduled notifications.");
                }

                // Wait 1 minute before polling again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessScheduledNotificationsAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var now = DateTime.UtcNow;
                
                // Find notifications that are scheduled for now or in the past, and haven't been sent yet
                var pendingNotifications = await context.NotificationsNotifications
                    .Where(n => n.ScheduledAt != null && n.ScheduledAt <= now && n.SentAt == null)
                    .ToListAsync(stoppingToken);

                foreach (var notification in pendingNotifications)
                {
                    _logger.LogInformation($"Sending scheduled notification ID {notification.Id} - {notification.Title}");
                    
                    // Fetch device tokens for the recipients
                    var studentIds = await context.NotificationsStudentnotifications
                        .Where(sn => sn.NotificationId == notification.Id)
                        .Select(sn => sn.StudentId)
                        .ToListAsync(stoppingToken);
                    
                    var tokens = await context.NotificationsDevicetokens
                        .Where(dt => studentIds.Contains(dt.StudentId))
                        .Select(dt => dt.Token)
                        .ToListAsync(stoppingToken);

                    int successCount = 0;
                    if (notification.SendPush && tokens.Count > 0)
                    {
                        var data = new Dictionary<string, string>
                        {
                            { "notification_id", notification.Id.ToString() },
                            { "type", notification.Type ?? "GENERAL" },
                            { "layout", notification.Layout ?? "text_only" },
                            { "display_mode", notification.DisplayMode ?? "persistent" },
                            { "title", notification.Title ?? "" },
                            { "body", notification.Body ?? "" },
                            { "subtitle", notification.Subtitle ?? "" },
                            { "description", notification.Description ?? "" },
                            { "link_url", notification.LinkUrl ?? "" },
                            { "link_button_text", notification.LinkButtonText ?? "" }
                        };

                        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5247";

                        if (!string.IsNullOrEmpty(notification.BackgroundImage))
                        {
                            data["background_image"] = notification.BackgroundImage.StartsWith("http") ? notification.BackgroundImage : $"{baseUrl.TrimEnd('/')}/media/{notification.BackgroundImage}";
                        }
                        
                        var firstImage = await context.NotificationsNotificationimages
                            .FirstOrDefaultAsync(i => i.NotificationId == notification.Id, stoppingToken);
                        if (firstImage != null)
                        {
                            data["image_url"] = firstImage.Image.StartsWith("http") ? firstImage.Image : $"{baseUrl.TrimEnd('/')}/media/{firstImage.Image}";
                        }
                        
                        try
                        {
                            var (success, failed) = await notificationService.SendMulticastPushNotificationAsync(tokens, notification.Title, notification.Body, data);
                            successCount = success;
                            
                            if (failed.Any())
                            {
                                var tokensToDelete = await context.NotificationsDevicetokens
                                    .Where(t => failed.Contains(t.Token))
                                    .ToListAsync(stoppingToken);
                                if (tokensToDelete.Any())
                                {
                                    context.NotificationsDevicetokens.RemoveRange(tokensToDelete);
                                }
                            }                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send push notification for scheduled notification ID {notification.Id}");
                            successCount = 0;
                        }
                    }

                    notification.SentAt = DateTime.UtcNow;
                    notification.SuccessCount = successCount;
                    notification.FailureCount = tokens.Count - successCount;
                    
                    var studentNotifications = await context.NotificationsStudentnotifications
                        .Where(sn => sn.NotificationId == notification.Id)
                        .ToListAsync(stoppingToken);
                        
                    foreach (var sn in studentNotifications)
                    {
                        sn.DeliveredAt = DateTime.UtcNow;
                        sn.PushDelivered = notification.SendPush;
                    }
                    
                    await context.SaveChangesAsync(stoppingToken);
                }
            }
        }
    }
}
