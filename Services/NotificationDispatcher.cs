using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface INotificationDispatcher
    {
        Task SendToStudentAsync(long studentId, string title, string body, string type,
            Dictionary<string, string>? data = null,
            string? whatsappMessage = null,
            byte[]? pdfBytes = null, string? pdfFileName = null);
    }

    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(IServiceScopeFactory scopeFactory, ILogger<NotificationDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task SendToStudentAsync(long studentId, string title, string body, string type,
            Dictionary<string, string>? data = null,
            string? whatsappMessage = null,
            byte[]? pdfBytes = null, string? pdfFileName = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var firebaseService = scope.ServiceProvider.GetService<INotificationService>();
                    var whatsappService = scope.ServiceProvider.GetService<WhatsAppNotificationService>();

                    // 1. Save Notification to DB
                    var now = DateTime.UtcNow;
                    var notification = new NotificationsNotification
                    {
                        Title = title,
                        Body = body,
                        Type = type,
                        TargetGroup = "student",
                        Target = studentId.ToString(),
                        Audience = "selected",
                        DisplayMode = "persistent",
                        Layout = "text_only",
                        Subtitle = "",
                        Description = "",
                        LinkUrl = data != null && data.ContainsKey("link_url") ? data["link_url"] : "",
                        LinkButtonText = data != null && data.ContainsKey("link_button_text") ? data["link_button_text"] : "",
                        CreatedAt = now,
                        ScheduledAt = now,
                        SendPush = true,
                        FailureCount = 0,
                        SuccessCount = 0,
                        TotalRecipients = 1
                    };

                    context.NotificationsNotifications.Add(notification);
                    var studentNotification = new NotificationsStudentnotification
                    {
                        StudentId = studentId,
                        Notification = notification,
                        IsRead = false
                    };
                    context.NotificationsStudentnotifications.Add(studentNotification);

                    await context.SaveChangesAsync();

                    // Handle PDF Attachment and LinkUrl
                    string? finalAttachmentUrl = null;
                    if (pdfBytes != null && pdfBytes.Length > 0 && !string.IsNullOrWhiteSpace(pdfFileName))
                    {
                        var env = scope.ServiceProvider.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
                        if (env != null)
                        {
                            string folderPath = System.IO.Path.Combine(env.WebRootPath, "media", "receipts");
                            if (!System.IO.Directory.Exists(folderPath))
                            {
                                System.IO.Directory.CreateDirectory(folderPath);
                            }
                            
                            string safeFileName = $"{Guid.NewGuid()}_{pdfFileName}";
                            string filePath = System.IO.Path.Combine(folderPath, safeFileName);
                            
                            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
                            
                            finalAttachmentUrl = $"/media/receipts/{safeFileName}";
                            
                            // Update DB notification LinkUrl
                            notification.LinkUrl = finalAttachmentUrl;
                            notification.LinkButtonText = "Download PDF";
                            await context.SaveChangesAsync();
                        }
                    }

                    // 2. Send FCM Push
                    if (firebaseService != null)
                    {
                        var tokens = await context.NotificationsDevicetokens
                            .Where(t => t.StudentId == studentId)
                            .Select(t => t.Token)
                            .ToListAsync();

                        if (tokens.Any())
                        {
                            var mergedData = new Dictionary<string, string>(data ?? new Dictionary<string, string>())
                            {
                                ["type"] = type,
                                ["notification_id"] = notification.Id.ToString()
                            };

                            if (!string.IsNullOrEmpty(finalAttachmentUrl))
                            {
                                mergedData["link_url"] = finalAttachmentUrl;
                                mergedData["link_button_text"] = "Download PDF";
                            }

                            if (tokens.Count == 1)
                            {
                                await firebaseService.SendPushNotificationAsync(tokens[0], title, body, mergedData);
                            }
                            else
                            {
                                var (successCount, failedTokens) = await firebaseService.SendMulticastPushNotificationAsync(tokens, title, body, mergedData);
                                if (failedTokens.Any())
                                {
                                    var tokensToDelete = await context.NotificationsDevicetokens
                                        .Where(t => failedTokens.Contains(t.Token))
                                        .ToListAsync();
                                    if (tokensToDelete.Any())
                                    {
                                        context.NotificationsDevicetokens.RemoveRange(tokensToDelete);
                                        await context.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                    }

                    // 3. Send WhatsApp
                    if (whatsappService != null)
                    {
                        var student = await context.AccountsCustomusers
                            .Where(u => u.Id == studentId)
                            .OrderBy(u => u.Id)
                            .Select(u => new { u.Mobile })
                            .FirstOrDefaultAsync();

                        if (student != null && !string.IsNullOrWhiteSpace(student.Mobile))
                        {
                            if (!string.IsNullOrWhiteSpace(whatsappMessage))
                            {
                                await whatsappService.SendTextMessageAsync(student.Mobile, whatsappMessage);
                            }

                            if (pdfBytes != null && pdfBytes.Length > 0 && !string.IsNullOrWhiteSpace(pdfFileName))
                            {
                                await whatsappService.SendDocumentAsync(student.Mobile, pdfBytes, pdfFileName, title);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NotificationDispatcher.SendToStudentAsync for student {StudentId}", studentId);
                }
            });

            return Task.CompletedTask;
        }
    }
}
