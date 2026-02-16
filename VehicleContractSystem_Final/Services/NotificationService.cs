using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string title, string message, string userId, string type = "Info", string? actionUrl = null);
        Task NotifyAdminsAboutNewUserAsync(ApplicationUser newUser);
        Task NotifyUserAboutApprovalAsync(ApplicationUser user, bool approved);
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(string title, string message, string userId, string type = "Info", string? actionUrl = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                UserId = userId,
                Type = type,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAdminsAboutNewUserAsync(ApplicationUser newUser)
        {
            // Get all SuperAdmins
            var admins = await _context.Users
                .Where(u => u.Id != newUser.Id)
                .ToListAsync();

            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "SuperAdmin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (adminRoleId != null)
            {
                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var adminId in adminUserIds)
                {
                    await CreateNotificationAsync(
                        "مستخدم جديد في انتظار الموافقة",
                        $"المستخدم {newUser.FullName} ({newUser.Email}) سجل في النظام وينتظر موافقتك",
                        adminId,
                        "Warning",
                        $"/Account/PendingUsers"
                    );
                }
            }
        }

        public async Task NotifyUserAboutApprovalAsync(ApplicationUser user, bool approved)
        {
            var title = approved ? "تم تفعيل حسابك" : "تم رفض طلبك";
            var message = approved
                ? "مرحباً بك! تم تفعيل حسابك ويمكنك الآن استخدام النظام بالكامل."
                : "عذراً، لم تتم الموافقة على طلب التسجيل الخاص بك.";
            var type = approved ? "Success" : "Danger";

            await CreateNotificationAsync(title, message, user.Id, type);
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
