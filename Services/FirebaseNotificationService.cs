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
                        #pragma warning disable CS0618
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromJson(jsonCredentials)
                        });
                        #pragma warning restore CS0618
                        _logger.LogInformation("✅ Firebase initialized successfully from FIREBASE_CREDENTIALS_JSON.");
                    }
                    else
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.GetApplicationDefault()
                        });
                        _logger.LogInformation("✅ Firebase initialized from GOOGLE_APPLICATION_CREDENTIALS.");
                    }
                }
                _isFirebaseInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 CRITICAL: Firebase initialization FAILED. Push notifications will NOT be delivered. Verify FIREBASE_CREDENTIALS_JSON environment variable on Render.");
            }
        }

        public async Task<bool> SendPushNotificationAsync(string token, string title, string body, Dictionary<string, string> data = null)
        {
            if (!_isFirebaseInitialized)
            {
                _logger.LogWarning("[Mock] Firebase not initialized. Skipping push to single token.");
                return false;
            }

            // DATA-ONLY message: Flutter handles display in ALL app states
            var mergedData = new Dictionary<string, string>(data ?? new Dictionary<string, string>())
            {
                ["title"] = title,
                ["body"] = body
            };

            var message = new Message()
            {
                Token = token,
                // NO Notification object — data-only so Flutter controls display in all states
                Android = new AndroidConfig()
                {
                    Priority = Priority.High,
                    // No AndroidNotification here — Flutter handles it via flutter_local_notifications
                },
                Data = mergedData
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("[FCM] Single push delivered. MessageId={MessageId}", response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM] Error sending single push to token {Token}", token);
                return false;
            }
        }

        public async Task<int> SendMulticastPushNotificationAsync(List<string> tokens, string title, string body, Dictionary<string, string> data = null)
        {
            if (tokens == null || tokens.Count == 0) return 0;

            if (!_isFirebaseInitialized)
            {
                _logger.LogWarning("[Mock] Firebase not initialized. Skipping multicast push to {Count} devices.", tokens.Count);
                return 0;
            }

            // DATA-ONLY message: Flutter handles display in ALL app states
            // - App FOREGROUND  → FirebaseMessaging.onMessage fires → flutter_local_notifications shows it
            // - App BACKGROUND  → _firebaseMessagingBackgroundHandler fires → flutter_local_notifications shows it
            // - App TERMINATED  → _firebaseMessagingBackgroundHandler fires → flutter_local_notifications shows it
            var mergedData = new Dictionary<string, string>(data ?? new Dictionary<string, string>())
            {
                ["title"] = title,
                ["body"] = body
            };

            var message = new MulticastMessage()
            {
                Tokens = tokens,
                // NO Notification object — data-only so Flutter controls display in all states
                Android = new AndroidConfig()
                {
                    Priority = Priority.High,
                    // No AndroidNotification — Flutter handles via flutter_local_notifications
                },
                Data = mergedData
            };

            try
            {
                _logger.LogInformation("[FCM] Sending data-only multicast to {Count} devices. Title={Title}", tokens.Count, title);
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation("[FCM] Multicast done: {Success} success, {Failure} failure out of {Total}",
                    response.SuccessCount, response.FailureCount, tokens.Count);

                // Log individual failures for debugging
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        _logger.LogWarning("[FCM] Token #{Index} failed: {Error}", i, response.Responses[i].Exception?.Message);
                    }
                }

                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM] Multicast send FAILED for {Count} devices", tokens.Count);
                return 0;
            }
        }
    }
}
