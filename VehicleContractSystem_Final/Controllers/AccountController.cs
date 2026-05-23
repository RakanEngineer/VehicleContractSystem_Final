using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Models.ViewModel;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            IWhatsAppService whatsAppService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
        }

        // GET: Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email, // يمكن استخدام الإيميل كـ username
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PersonalNumber = model.PersonalNumber,
                    IsApproved = false, // ينتظر الموافقة
                    RegisteredAt = DateTime.Now,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // إضافة دور User افتراضياً
                    await _userManager.AddToRoleAsync(user, "User");

                    // إرسال إشعار للـ Admins
                    await _notificationService.NotifyAdminsAboutNewUserAsync(user);

                    // إرسال بريد إلكتروني للمستخدم
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);

                    // إرسال واتساب للـ Admins (اختياري)
                    // var adminPhone = _context.Users
                    //     .Where(u => u.UserName == "admin")
                    //     .Select(u => u.PhoneNumber)
                    //     .FirstOrDefault();
                    // if (!string.IsNullOrEmpty(adminPhone))
                    // {
                    //     await _whatsAppService.SendNewUserNotificationAsync(
                    //         adminPhone, user.FullName, user.Email);
                    // }

                    TempData["SuccessMessage"] = "تم التسجيل بنجاح! حسابك الآن في انتظار موافقة المدير.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Account/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // البحث عن المستخدم بالإيميل أو رقم الهاتف
                var user = await _userManager.FindByEmailAsync(model.EmailOrPhone);
                if (user == null && model.EmailOrPhone.StartsWith("07"))
                {
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == model.EmailOrPhone);
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو رقم الهاتف غير صحيح");
                    return View(model);
                }

                // التحقق من الموافقة
                if (!user.IsApproved)
                {
                    ModelState.AddModelError(string.Empty, "حسابك في انتظار موافقة المدير");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true
                );

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "حسابك محظور مؤقتاً بسبب محاولات تسجيل دخول فاشلة");
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, "كلمة المرور غير صحيحة");
            }

            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ExternalLogin
        [AllowAnonymous]
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // GET: Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"خطأ في تسجيل الدخول: {remoteError}";
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in with external provider
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true
            );

            if (result.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null && !user.IsApproved)
                {
                    await _signInManager.SignOutAsync();
                    TempData["ErrorMessage"] = "حسابك في انتظار موافقة المدير";
                    return RedirectToAction("Login");
                }

                return RedirectToLocal(returnUrl);
            }

            // Create new user if doesn't exist
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Provider"] = info.LoginProvider;

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

            return View("ExternalLoginConfirmation", new ExternalLoginViewModel
            {
                Email = email ?? "",
                FirstName = firstName,
                LastName = lastName
            });
        }

        // POST: Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return RedirectToAction("Login");
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PersonalNumber = model.PersonalNumber,
                    PhoneNumber = model.PhoneNumber,
                    IsApproved = false,
                    RegisteredAt = DateTime.Now,
                    EmailConfirmed = true // من Google يكون مؤكد
                };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");

                        // إشعار الـ Admins
                        await _notificationService.NotifyAdminsAboutNewUserAsync(user);

                        TempData["SuccessMessage"] = "تم التسجيل بنجاح! حسابك في انتظار موافقة المدير.";
                        return RedirectToAction("Login");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: Account/PendingUsers (SuperAdmin only)
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> PendingUsers()
        {
            var pendingUsers = await _userManager.Users
                .Where(u => !u.IsApproved && u.OfficeId == null)
                .OrderByDescending(u => u.RegisteredAt)
                .ToListAsync();

            var offices = await _context.Offices
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();

            return View(new PendingUsersViewModel
            {
                Users = pendingUsers,
                Offices = offices
            });
        }

        // POST: Account/ApproveUser
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string userId, int? officeId = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (!officeId.HasValue || !await _context.Offices.AnyAsync(o => o.Id == officeId.Value))
            {
                TempData["ErrorMessage"] = "يرجى اختيار مكتب صالح قبل الموافقة على المستخدم";
                return RedirectToAction(nameof(PendingUsers));
            }

            user.IsApproved = true;
            user.ApprovedAt = DateTime.Now;
            user.ApprovedBy = User.Identity?.Name;
            user.OfficeId = officeId.Value;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // إشعار المستخدم
                await _notificationService.NotifyUserAboutApprovalAsync(user, true);

                // إرسال بريد إلكتروني
                await _emailService.SendApprovalNotificationAsync(user.Email!, user.FullName, true);

                // إرسال واتساب (اختياري)
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await _whatsAppService.SendApprovalNotificationAsync(
                        user.PhoneNumber, user.FullName, true);
                }

                TempData["SuccessMessage"] = $"تم تفعيل حساب {user.FullName} بنجاح";
            }

            return RedirectToAction(nameof(PendingUsers));
        }

        // POST: Account/RejectUser
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // إشعار المستخدم بالرفض
            await _notificationService.NotifyUserAboutApprovalAsync(user, false);
            await _emailService.SendApprovalNotificationAsync(user.Email!, user.FullName, false);

            // حذف الحساب
            await _userManager.DeleteAsync(user);

            TempData["SuccessMessage"] = "تم رفض الطلب وحذف الحساب";
            return RedirectToAction(nameof(PendingUsers));
        }

        // GET: Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }
    }

}
