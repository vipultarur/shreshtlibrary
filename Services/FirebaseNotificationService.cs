using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Services
{
    public interface INotificationService
    {
        Task<bool> SendPushNotificationAsync(string token, string title, string body, Dictionary<string, string> data = null);
        Task<int> SendMulticastPushNotificationAsync(List<string> tokens, string title, string body, Dictionary<string, string> data = null);
    }

    public class FirebaseNotificationService : INotificationService
    {
        private bool _isFirebaseInitialized = false;
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    var jsonCredentials = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");
                    if (!string.IsNullOrEmpty(jsonCredentials))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromJson(jsonCredentials)
                        });
                    }
                    else
                    {
                        // Fallback to GOOGLE_APPLICATION_CREDENTIALS (file path)
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.GetApplicationDefault()
                        });
                    }
                }
                _isFirebaseInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase initialization failed (Check FIREBASE_CREDENTIALS_JSON or GOOGLE_APPLICATION_CREDENTIALS). Using Mock Push Service.");
            }
        }

        public async Task<bool> SendPushNotificationAsync(string token, string title, string body, Dictionary<string, string> data = null)
        {
            if (!_isFirebaseInitialized)
            {
                _logger.LogDebug("[Mock] Sending Push Notification to {Token}: {Title}", token, title);
                return true;
            }

            var message = new Message()
            {
                Token = token,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to {Token}", token);
                return false;
            }
        }

        public async Task<int> SendMulticastPushNotificationAsync(List<string> tokens, string title, string body, Dictionary<string, string> data = null)
        {
            if (tokens == null || tokens.Count == 0) return 0;

            if (!_isFirebaseInitialized)
            {
                _logger.LogDebug("[Mock] Sending Multicast Push Notification to {Count} devices: {Title}", tokens.Count, title);
                return tokens.Count;
            }

            var notificationObj = new Notification()
            {
                Title = title,
                Body = body
            };

            if (data != null && data.ContainsKey("image_url") && !string.IsNullOrEmpty(data["image_url"]))
            {
                notificationObj.ImageUrl = data["image_url"];
            }
            
            var message = new MulticastMessage()
            {
                Tokens = tokens,
                Notification = notificationObj,
                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending multicast push notification to {Count} devices", tokens.Count);
                return 0;
            }
        }
    }
}
