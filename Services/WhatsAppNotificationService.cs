using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Services
{
    public class WhatsAppNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WhatsAppNotificationService> _logger;

        public WhatsAppNotificationService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            ILogger<WhatsAppNotificationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        private async Task<(string baseUrl, string sessionId, string apiKey)> GetSettingsAsync()
        {
            string baseUrl = _configuration["WhatsAppApi:BaseUrl"] ?? "http://localhost:2785";
            string sessionId = _configuration["WhatsAppApi:SessionId"] ?? "default";
            string apiKey = _configuration["WhatsAppApi:ApiKey"] ?? "";

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WebApplication1.Data.ApplicationDbContext>();
            
            var dbBaseUrl = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                context.CoreGlobalsettings, s => s.Key == "wa_base_url");
            if (dbBaseUrl != null && !string.IsNullOrEmpty(dbBaseUrl.Value)) baseUrl = dbBaseUrl.Value;

            var dbSessionId = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                context.CoreGlobalsettings, s => s.Key == "wa_session_id");
            if (dbSessionId != null && !string.IsNullOrEmpty(dbSessionId.Value)) sessionId = dbSessionId.Value;

            var dbApiKey = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                context.CoreGlobalsettings, s => s.Key == "wa_api_key");
            if (dbApiKey != null && !string.IsNullOrEmpty(dbApiKey.Value)) apiKey = dbApiKey.Value;

            return (baseUrl, sessionId, apiKey);
        }

        public Task<bool> SendTextMessageAsync(string phoneNumber, string message)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Format the phone number (assuming India +91 as default if length is 10)
                    if (phoneNumber.Length == 10 && !phoneNumber.StartsWith("91"))
                    {
                        phoneNumber = "91" + phoneNumber;
                    }
                    
                    var settings = await GetSettingsAsync();
                    string chatId = $"{phoneNumber}@c.us"; 
                    var endpoint = $"{settings.baseUrl}/api/sessions/{settings.sessionId}/messages/send-text";

                    var payload = new
                    {
                        chatId = chatId,
                        text = message
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _httpClient.DefaultRequestHeaders.Clear();
                    if (!string.IsNullOrEmpty(settings.apiKey))
                    {
                        _httpClient.DefaultRequestHeaders.Add("x-api-key", settings.apiKey);
                    }

                    var response = await _httpClient.PostAsync(endpoint, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully sent WhatsApp message to {phoneNumber}");
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Failed to send WhatsApp message to {phoneNumber}. Status: {response.StatusCode}, Error: {error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception occurred while sending WhatsApp message to {phoneNumber}");
                }
            });

            return Task.FromResult(true);
        }

        public Task<bool> SendDocumentAsync(string phoneNumber, byte[] fileBytes, string fileName, string caption = "")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (phoneNumber.Length == 10 && !phoneNumber.StartsWith("91"))
                    {
                        phoneNumber = "91" + phoneNumber;
                    }
                    
                    var settings = await GetSettingsAsync();
                    string chatId = $"{phoneNumber}@c.us"; 
                    var endpoint = $"{settings.baseUrl}/api/sessions/{settings.sessionId}/messages/send-document";

                    string base64File = Convert.ToBase64String(fileBytes);
                    var payload = new
                    {
                        chatId = chatId,
                        base64 = base64File,
                        mimetype = "application/pdf",
                        filename = fileName,
                        caption = caption
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _httpClient.DefaultRequestHeaders.Clear();
                    if (!string.IsNullOrEmpty(settings.apiKey))
                    {
                        _httpClient.DefaultRequestHeaders.Add("x-api-key", settings.apiKey);
                    }

                    var response = await _httpClient.PostAsync(endpoint, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully sent WhatsApp document to {phoneNumber}");
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Failed to send WhatsApp document to {phoneNumber}. Status: {response.StatusCode}, Error: {error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception occurred while sending WhatsApp document to {phoneNumber}");
                }
            });

            return Task.FromResult(true);
        }
        public Task<bool> SendImageAsync(string phoneNumber, byte[] fileBytes, string fileName, string caption = "", string mimetype = "image/jpeg")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (phoneNumber.Length == 10 && !phoneNumber.StartsWith("91"))
                    {
                        phoneNumber = "91" + phoneNumber;
                    }
                    
                    var settings = await GetSettingsAsync();
                    string chatId = $"{phoneNumber}@c.us"; 
                    var endpoint = $"{settings.baseUrl}/api/sessions/{settings.sessionId}/messages/send-image";

                    string base64File = Convert.ToBase64String(fileBytes);
                    var payload = new
                    {
                        chatId = chatId,
                        base64 = base64File,
                        mimetype = mimetype,
                        filename = fileName,
                        caption = caption
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _httpClient.DefaultRequestHeaders.Clear();
                    if (!string.IsNullOrEmpty(settings.apiKey))
                    {
                        _httpClient.DefaultRequestHeaders.Add("x-api-key", settings.apiKey);
                    }

                    var response = await _httpClient.PostAsync(endpoint, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully sent WhatsApp image to {phoneNumber}");
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Failed to send WhatsApp image to {phoneNumber}. Status: {response.StatusCode}, Error: {error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception occurred while sending WhatsApp image to {phoneNumber}");
                }
            });

            return Task.FromResult(true);
        }
    }
}
