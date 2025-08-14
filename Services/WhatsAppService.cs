using Microsoft.Extensions.Options;

namespace ReverseMarket.Services
{
    public class WhatsAppSettings
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly WhatsAppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(IOptions<WhatsAppSettings> settings, HttpClient httpClient, ILogger<WhatsAppService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                // For development, just log the message
                _logger.LogInformation($"WhatsApp message to {phoneNumber}: {message}");

                // In production, integrate with actual WhatsApp Business API
                // Example using WhatsApp Business API:
                /*
                var requestBody = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "text",
                    text = new { body = message }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

                var response = await _httpClient.PostAsync(_settings.ApiUrl, content);
                return response.IsSuccessStatusCode;
                */

                return true; // Simulate success for development
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendOTPAsync(string phoneNumber, string otp)
        {
            var message = $"رمز التحقق الخاص بك في السوق العكسي: {otp}\nلا تشارك هذا الرمز مع أي شخص.";
            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> NotifyStoreAsync(string phoneNumber, string storeName, string requestTitle, string requestUrl)
        {
            var message = $"مرحباً {storeName}!\n\n" +
                         $"لديك طلب جديد في تخصصك:\n" +
                         $"📋 {requestTitle}\n\n" +
                         $"للمشاهدة والتواصل مع العميل:\n" +
                         $"{requestUrl}\n\n" +
                         $"السوق العكسي";

            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> NotifyUserApprovalAsync(string phoneNumber, string userName, string requestTitle)
        {
            var message = $"مرحباً {userName}!\n\n" +
                         $"تم الموافقة على طلبك: {requestTitle}\n\n" +
                         $"سيتم إشعار المتاجر المتخصصة وستبدأ بتلقي العروض قريباً.\n\n" +
                         $"شكراً لاستخدامك السوق العكسي";

            return await SendMessageAsync(phoneNumber, message);
        }
    }
}
