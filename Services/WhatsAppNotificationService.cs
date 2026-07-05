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
        private readonly string _baseUrl;
        private readonly string _sessionId;
        private readonly string _apiKey;
        private readonly ILogger<WhatsAppNotificationService> _logger;

        public WhatsAppNotificationService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppNotificationService> logger)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["WhatsAppApi:BaseUrl"] ?? "http://localhost:2785";
            _sessionId = configuration["WhatsAppApi:SessionId"] ?? "default";
            _apiKey = configuration["WhatsAppApi:ApiKey"] ?? "";
            _logger = logger;
        }

        public async Task<bool> SendTextMessageAsync(string phoneNumber, string message)
        {
            try
            {
                // Format the phone number (assuming India +91 as default if length is 10)
                if (phoneNumber.Length == 10 && !phoneNumber.StartsWith("91"))
                {
                    phoneNumber = "91" + phoneNumber;
                }
                
                string chatId = $"{phoneNumber}@c.us"; 
                var endpoint = $"{_baseUrl}/api/sessions/{_sessionId}/messages/send-text";

                var payload = new
                {
                    chatId = chatId,
                    text = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                }

                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully sent WhatsApp message to {phoneNumber}");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to send WhatsApp message to {phoneNumber}. Status: {response.StatusCode}, Error: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending WhatsApp message to {phoneNumber}");
                return false;
            }
        }
        public async Task<bool> SendDocumentAsync(string phoneNumber, byte[] fileBytes, string fileName, string caption = "")
        {
            try
            {
                if (phoneNumber.Length == 10 && !phoneNumber.StartsWith("91"))
                {
                    phoneNumber = "91" + phoneNumber;
                }
                
                string chatId = $"{phoneNumber}@c.us"; 
                var endpoint = $"{_baseUrl}/api/sessions/{_sessionId}/messages/send-document";

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
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                }

                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully sent WhatsApp document to {phoneNumber}");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to send WhatsApp document to {phoneNumber}. Status: {response.StatusCode}, Error: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending WhatsApp document to {phoneNumber}");
                return false;
            }
        }
    }
}
