using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;

namespace VehicleContractSystem_Final.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendApprovalNotificationAsync(string toEmail, string userName, bool approved);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = _configuration["EmailSettings:From"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var userName = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                if (string.IsNullOrWhiteSpace(fromEmail) ||
                    string.IsNullOrWhiteSpace(smtpServer) ||
                    string.IsNullOrWhiteSpace(userName) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    Console.WriteLine("Email settings not configured");
                    return;
                }

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(fromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = body
                };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    smtpServer,
                    int.Parse(_configuration["EmailSettings:Port"] ?? "587"),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    userName,
                    password
                );

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "مرحباً بك في نظام إدارة عقود السيارات";
            var body = $@"
                <div dir='rtl' style='font-family: Arial, sans-serif;'>
                    <h2>مرحباً {userName}،</h2>
                    <p>شكراً لتسجيلك في نظام إدارة عقود السيارات.</p>
                    <p>حسابك الآن في انتظار موافقة المدير. سنرسل لك إشعاراً عند تفعيل حسابك.</p>
                    <br>
                    <p>مع تحياتنا،<br>فريق نظام إدارة عقود السيارات</p>
                </div>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendApprovalNotificationAsync(string toEmail, string userName, bool approved)
        {
            var subject = approved ? "تم تفعيل حسابك" : "تحديث بشأن طلب التسجيل";
            var body = approved
                ? $@"
                    <div dir='rtl' style='font-family: Arial, sans-serif;'>
                        <h2>مرحباً {userName}،</h2>
                        <p>نسعد بإخبارك أنه تم تفعيل حسابك!</p>
                        <p>يمكنك الآن تسجيل الدخول واستخدام النظام بالكامل.</p>
                        <br>
                        <p>مع تحياتنا،<br>فريق نظام إدارة عقود السيارات</p>
                    </div>
                "
                : $@"
                    <div dir='rtl' style='font-family: Arial, sans-serif;'>
                        <h2>مرحباً {userName}،</h2>
                        <p>نأسف لإخبارك أنه لم تتم الموافقة على طلب التسجيل الخاص بك في الوقت الحالي.</p>
                        <p>للمزيد من المعلومات، يرجى التواصل مع الإدارة.</p>
                        <br>
                        <p>مع تحياتنا،<br>فريق نظام إدارة عقود السيارات</p>
                    </div>
                ";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
