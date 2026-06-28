using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public FirebaseNotificationService()
        {
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    // In production, the GOOGLE_APPLICATION_CREDENTIALS environment variable should be set
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.GetApplicationDefault()
                    });
                }
                _isFirebaseInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase initialization failed: {ex.Message}. Using Mock Push Service.");
            }
        }

        public async Task<bool> SendPushNotificationAsync(string token, string title, string body, Dictionary<string, string> data = null)
        {
            if (!_isFirebaseInitialized)
            {
                Console.WriteLine($"[Mock] Sending Push Notification to {token}: {title} - {body}");
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
                Console.WriteLine($"Error sending push notification: {ex.Message}");
                return false;
            }
        }

        public async Task<int> SendMulticastPushNotificationAsync(List<string> tokens, string title, string body, Dictionary<string, string> data = null)
        {
            if (tokens == null || tokens.Count == 0) return 0;

            if (!_isFirebaseInitialized)
            {
                Console.WriteLine($"[Mock] Sending Multicast Push Notification to {tokens.Count} devices: {title} - {body}");
                return tokens.Count;
            }

            var message = new MulticastMessage()
            {
                Tokens = tokens,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending multicast push notification: {ex.Message}");
                return 0;
            }
        }
    }
}
