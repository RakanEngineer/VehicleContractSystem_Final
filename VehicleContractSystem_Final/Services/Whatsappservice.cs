using System.Text;

namespace VehicleContractSystem_Final.Services
{
    /// <summary>
    /// خدمة إرسال رسائل الواتساب
    /// يمكن استخدام WhatsApp Business API أو خدمات مثل Twilio
    /// </summary>
    public interface IWhatsAppService
    {
        Task<bool> SendMessageAsync(string phoneNumber, string message);
        Task<bool> SendNewUserNotificationAsync(string adminPhone, string userName, string userEmail);
        Task<bool> SendApprovalNotificationAsync(string userPhone, string userName, bool approved);
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WhatsAppService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                // طريقة 1: استخدام Twilio WhatsApp API
                // https://www.twilio.com/docs/whatsapp/api

                var accountSid = _configuration["WhatsApp:Twilio:AccountSid"];
                var authToken = _configuration["WhatsApp:Twilio:AuthToken"];
                var fromNumber = _configuration["WhatsApp:Twilio:FromNumber"]; // whatsapp:+14155238886

                if (string.IsNullOrEmpty(accountSid) ||
                    string.IsNullOrEmpty(authToken) ||
                    string.IsNullOrEmpty(fromNumber))
                {
                    Console.WriteLine("WhatsApp credentials not configured");
                    return false;
                }

                var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("From", fromNumber),
                    new KeyValuePair<string, string>("To", $"whatsapp:{phoneNumber}"),
                    new KeyValuePair<string, string>("Body", message)
                });

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;

                // طريقة 2: استخدام WhatsApp Business API المباشر
                // https://developers.facebook.com/docs/whatsapp/cloud-api

                // var accessToken = _configuration["WhatsApp:Business:AccessToken"];
                // var phoneNumberId = _configuration["WhatsApp:Business:PhoneNumberId"];
                // var url = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";

                // var payload = new
                // {
                //     messaging_product = "whatsapp",
                //     to = phoneNumber,
                //     type = "text",
                //     text = new { body = message }
                // };

                // var jsonContent = new StringContent(
                //     JsonSerializer.Serialize(payload),
                //     Encoding.UTF8,
                //     "application/json"
                // );

                // _httpClient.DefaultRequestHeaders.Authorization = 
                //     new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // var response = await _httpClient.PostAsync(url, jsonContent);
                // return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending WhatsApp message: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNewUserNotificationAsync(string adminPhone, string userName, string userEmail)
        {
            var message = $@"
🔔 *مستخدم جديد في انتظار الموافقة*

👤 الاسم: {userName}
📧 البريد: {userEmail}

يرجى مراجعة طلب التسجيل والموافقة عليه.
            ".Trim();

            return await SendMessageAsync(adminPhone, message);
        }

        public async Task<bool> SendApprovalNotificationAsync(string userPhone, string userName, bool approved)
        {
            var message = approved
                ? $@"
✅ *تم تفعيل حسابك*

مرحباً {userName}،

تم تفعيل حسابك في نظام إدارة عقود السيارات.
يمكنك الآن تسجيل الدخول واستخدام النظام.

مع تحياتنا 🚗
                ".Trim()
                : $@"
❌ *تحديث بشأن طلب التسجيل*

مرحباً {userName}،

نأسف لإخبارك أنه لم تتم الموافقة على طلبك في الوقت الحالي.
للمزيد من المعلومات، يرجى التواصل مع الإدارة.
                ".Trim();

            return await SendMessageAsync(userPhone, message);
        }
    }
}
