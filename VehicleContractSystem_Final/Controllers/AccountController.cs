using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Models.ViewModel;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            IWhatsAppService whatsAppService,
            IAuditService auditService,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
            _auditService = auditService;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Account/Register
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            await SetGoogleLoginAvailabilityAsync();
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            await SetGoogleLoginAvailabilityAsync();

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
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            await SetGoogleLoginAvailabilityAsync();
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            await SetGoogleLoginAvailabilityAsync();

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
                    ModelState.AddModelError(string.Empty, GetGenericLoginFailureMessage());
                    return View(model);
                }

                // التحقق من الموافقة
                if (!user.IsApproved)
                {
                    ModelState.AddModelError(string.Empty, GetGenericLoginFailureMessage());
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
                    ModelState.AddModelError(string.Empty, GetGenericLoginFailureMessage());
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, GetGenericLoginFailureMessage());
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("PasswordRecovery")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && user.IsApproved && !string.IsNullOrWhiteSpace(user.Email))
            {
                if (TryGetPublicBaseUrl(out var publicBaseUrl))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var resetUrl = BuildResetPasswordUrl(publicBaseUrl, user.Email, encodedToken);
                    var encodedResetUrl = HtmlEncoder.Default.Encode(resetUrl);
                    var body = $"""
                        <div dir="rtl" style="font-family: Tahoma, Arial, sans-serif; text-align: right;">
                            <h2>إعادة تعيين كلمة المرور</h2>
                            <p>تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك.</p>
                            <p><a href="{encodedResetUrl}">اضغط هنا لاختيار كلمة مرور جديدة</a></p>
                            <p>إذا لم تطلب إعادة تعيين كلمة المرور، يمكنك تجاهل هذه الرسالة.</p>
                        </div>
                        """;

                    await _emailService.SendEmailAsync(
                        user.Email,
                        "إعادة تعيين كلمة المرور",
                        body);
                }
                else
                {
                    _logger.LogError(
                        "Password reset email could not be generated because Application:PublicBaseUrl is missing or invalid.");
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "رابط إعادة تعيين كلمة المرور غير صالح أو منتهي الصلاحية";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("PasswordRecovery")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "رابط إعادة تعيين كلمة المرور غير صالح أو منتهي الصلاحية");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null || !user.IsApproved)
            {
                ModelState.AddModelError(string.Empty, "رابط إعادة تعيين كلمة المرور غير صالح أو منتهي الصلاحية");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, GetSafeIdentityError(error));
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ChangePassword
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح";

            return RedirectToAction(nameof(ChangePassword));
        }

        // GET: Account/ExternalLogin
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            if (!string.Equals(provider, GoogleDefaults.AuthenticationScheme, StringComparison.Ordinal) ||
                !await IsGoogleLoginAvailableAsync())
            {
                TempData["ErrorMessage"] = "تسجيل الدخول بواسطة Google غير متاح حالياً";
                return RedirectToAction(nameof(Login));
            }

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
                _logger.LogWarning("Google authentication returned an error: {RemoteError}", remoteError);
                TempData["ErrorMessage"] = "تعذر إكمال تسجيل الدخول بواسطة Google";
                return RedirectToAction(nameof(Login));
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
                bypassTwoFactor: false
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

            if (result.RequiresTwoFactor)
            {
                TempData["ErrorMessage"] = "يتطلب هذا الحساب تحققاً إضافياً لإكمال تسجيل الدخول";
                return RedirectToAction(nameof(Login));
            }

            if (result.IsLockedOut)
            {
                TempData["ErrorMessage"] = "تعذر تسجيل الدخول إلى الحساب حالياً";
                return RedirectToAction(nameof(Login));
            }

            // Create new user if doesn't exist
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Provider"] = info.LoginProvider;

            if (!TryGetVerifiedExternalEmail(info, out var email))
            {
                _logger.LogWarning("Google authentication did not provide a verified email claim.");
                TempData["ErrorMessage"] = "تعذر التحقق من البريد الإلكتروني لحساب Google";
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                _logger.LogWarning("Google login was not linked because its email belongs to an existing local account.");
                TempData["ErrorMessage"] = "يتعذر ربط حساب Google بهذا الحساب تلقائياً. استخدم طريقة تسجيل الدخول الحالية";
                return RedirectToAction(nameof(Login));
            }

            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

            return View("ExternalLoginConfirmation", new ExternalLoginViewModel
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName
            });
        }

        // POST: Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null ||
                !string.Equals(info.LoginProvider, GoogleDefaults.AuthenticationScheme, StringComparison.Ordinal) ||
                !TryGetVerifiedExternalEmail(info, out var providerEmail))
            {
                TempData["ErrorMessage"] = "تعذر التحقق من بيانات حساب Google";
                return RedirectToAction(nameof(Login));
            }

            ModelState.Remove(nameof(ExternalLoginViewModel.Email));
            model.Email = providerEmail;

            if (ModelState.IsValid)
            {
                if (await _userManager.FindByEmailAsync(providerEmail) != null)
                {
                    _logger.LogWarning("Google account creation was rejected because its email belongs to an existing account.");
                    TempData["ErrorMessage"] = "يتعذر ربط حساب Google بهذا الحساب تلقائياً. استخدم طريقة تسجيل الدخول الحالية";
                    return RedirectToAction(nameof(Login));
                }

                var user = new ApplicationUser
                {
                    UserName = providerEmail,
                    Email = providerEmail,
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
                        var roleResult = await _userManager.AddToRoleAsync(user, "User");
                        if (!roleResult.Succeeded)
                        {
                            await _userManager.DeleteAsync(user);
                            AddIdentityErrors(roleResult);
                            ViewData["ReturnUrl"] = returnUrl;
                            return View(model);
                        }

                        // إشعار الـ Admins
                        await _notificationService.NotifyAdminsAboutNewUserAsync(user);

                        TempData["SuccessMessage"] = "تم التسجيل بنجاح! حسابك في انتظار موافقة المدير.";
                        return RedirectToAction("Login");
                    }

                    await _userManager.DeleteAsync(user);
                }

                AddIdentityErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [Authorize(Roles = "SuperAdmin,OfficeAdmin")]
        public async Task<IActionResult> Users(string? search, int? officeId, string? role)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var isOfficeAdmin = User.IsInRole("OfficeAdmin");

            if (!isSuperAdmin && !isOfficeAdmin)
                return Forbid();

            var usersQuery = _userManager.Users
                .Include(u => u.Office)
                .AsQueryable();

            if (isOfficeAdmin)
            {
                usersQuery = usersQuery.Where(u => u.OfficeId == currentUser.OfficeId);
            }
            else if (officeId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.OfficeId == officeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)) ||
                    u.PersonalNumber.Contains(search));
            }

            var users = await usersQuery
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var items = new List<UserManagementItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isUserSuperAdmin = roles.Contains("SuperAdmin");
                if (isOfficeAdmin && isUserSuperAdmin)
                    continue;

                if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role))
                    continue;

                items.Add(new UserManagementItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    PersonalNumber = user.PersonalNumber,
                    OfficeId = user.OfficeId,
                    OfficeName = user.Office?.Name ?? "بدون مكتب",
                    IsApproved = user.IsApproved,
                    RegisteredAt = user.RegisteredAt,
                    RolesText = roles.Any() ? string.Join("، ", roles) : "بدون دور",
                    IsSuperAdmin = isUserSuperAdmin
                });
            }

            return View(new UserManagementViewModel
            {
                Users = items,
                Offices = await BuildOfficeSelectListAsync(isSuperAdmin ? null : currentUser.OfficeId),
                Roles = await BuildRoleSelectListAsync(includeSuperAdmin: isSuperAdmin),
                Search = search,
                OfficeId = officeId,
                Role = role,
                CanManageAllUsers = isSuperAdmin,
                CanChangeRoles = isSuperAdmin,
                CanAssignOffices = isSuperAdmin
            });
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string role)
        {
            if (string.IsNullOrWhiteSpace(role) || !await _roleManager.RoleExistsAsync(role))
            {
                TempData["ErrorMessage"] = "يرجى اختيار دور صالح";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (role != "SuperAdmin" && user.OfficeId == null)
            {
                TempData["ErrorMessage"] = "يجب تعيين المستخدم إلى مكتب قبل اختيار هذا الدور";
                return RedirectToAction(nameof(Users));
            }

            var oldRoles = await _userManager.GetRolesAsync(user);
            if (oldRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, oldRoles);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "تعذر تحديث أدوار المستخدم";
                    return RedirectToAction(nameof(Users));
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
            {
                TempData["ErrorMessage"] = "تعذر تعيين الدور الجديد";
                return RedirectToAction(nameof(Users));
            }

            if (role == "SuperAdmin" && user.OfficeId != null)
            {
                var previousOfficeId = user.OfficeId.Value;
                user.OfficeId = null;
                await _userManager.UpdateAsync(user);

                await _auditService.LogAsync(
                    "تعيين مكتب",
                    "ApplicationUser",
                    user.Id,
                    $"تم فصل المستخدم {user.FullName} من المكتب رقم {previousOfficeId} بعد تعيينه مديراً أعلى للنظام");
            }

            await _auditService.LogAsync(
                "تغيير دور مستخدم",
                "ApplicationUser",
                user.Id,
                $"تم تغيير دور المستخدم {user.FullName} من {string.Join("، ", oldRoles)} إلى {role}");

            TempData["SuccessMessage"] = "تم تحديث دور المستخدم بنجاح";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUserOffice(string userId, int? officeId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                TempData["ErrorMessage"] = "لا يمكن ربط مدير النظام الأعلى بمكتب";
                return RedirectToAction(nameof(Users));
            }

            if (!officeId.HasValue || !await _context.Offices.AnyAsync(o => o.Id == officeId.Value))
            {
                TempData["ErrorMessage"] = "يرجى اختيار مكتب صالح";
                return RedirectToAction(nameof(Users));
            }

            user.OfficeId = officeId.Value;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                "تعيين مكتب",
                "ApplicationUser",
                user.Id,
                $"تم ربط المستخدم {user.FullName} بالمكتب رقم {officeId.Value}");

            TempData["SuccessMessage"] = "تم تحديث مكتب المستخدم بنجاح";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,OfficeAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActivation(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (currentUser == null || user == null)
                return NotFound();

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var isOfficeAdmin = User.IsInRole("OfficeAdmin");

            if (await _userManager.IsInRoleAsync(user, "SuperAdmin") && !isSuperAdmin)
                return Forbid();

            if (isOfficeAdmin && user.OfficeId != currentUser.OfficeId)
                return Forbid();

            user.IsApproved = !user.IsApproved;
            if (user.IsApproved)
            {
                user.ApprovedAt = DateTime.Now;
                user.ApprovedBy = User.Identity?.Name;
            }

            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                user.IsApproved ? "تفعيل مستخدم" : "تعطيل مستخدم",
                "ApplicationUser",
                user.Id,
                $"{(user.IsApproved ? "تم تفعيل" : "تم تعطيل")} المستخدم {user.FullName}");

            TempData["SuccessMessage"] = user.IsApproved ? "تم تفعيل المستخدم" : "تم تعطيل المستخدم";
            return RedirectToAction(nameof(Users));
        }

        // GET: Account/PendingUsers
        [Authorize(Roles = "SuperAdmin,OfficeAdmin")]
        public async Task<IActionResult> PendingUsers()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var pendingUsers = await _userManager.Users
                .Where(u => !u.IsApproved && u.OfficeId == null)
                .OrderByDescending(u => u.RegisteredAt)
                .ToListAsync();

            return View(new PendingUsersViewModel
            {
                Users = pendingUsers,
                Offices = await BuildOfficeSelectListAsync(isSuperAdmin ? null : currentUser.OfficeId),
                Roles = await BuildRoleSelectListAsync(includeSuperAdmin: false),
                CanSelectOffice = isSuperAdmin,
                CanSelectRole = isSuperAdmin
            });
        }

        // POST: Account/ApproveUser
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,OfficeAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string userId, int? officeId = null, string role = "User")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var isOfficeAdmin = User.IsInRole("OfficeAdmin");
            if (!isSuperAdmin && !isOfficeAdmin)
                return Forbid();

            if (isOfficeAdmin)
            {
                if (currentUser.OfficeId == null)
                    return Forbid();

                officeId = currentUser.OfficeId;
                role = "User";
            }

            if (role == "SuperAdmin" && !isSuperAdmin)
                return Forbid();

            if (string.IsNullOrWhiteSpace(role) || !await _roleManager.RoleExistsAsync(role))
            {
                TempData["ErrorMessage"] = "يرجى اختيار دور صالح";
                return RedirectToAction(nameof(PendingUsers));
            }

            if (role != "SuperAdmin" && (!officeId.HasValue || !await _context.Offices.AnyAsync(o => o.Id == officeId.Value)))
            {
                TempData["ErrorMessage"] = "يرجى اختيار مكتب صالح قبل الموافقة على المستخدم";
                return RedirectToAction(nameof(PendingUsers));
            }

            user.IsApproved = true;
            user.ApprovedAt = DateTime.Now;
            user.ApprovedBy = User.Identity?.Name;
            user.OfficeId = role == "SuperAdmin" ? null : officeId;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                await _userManager.AddToRoleAsync(user, role);

                await _auditService.LogAsync(
                    "موافقة مستخدم",
                    "ApplicationUser",
                    user.Id,
                    $"تمت الموافقة على المستخدم {user.FullName} وتعيينه بدور {role} ومكتب {(user.OfficeId?.ToString() ?? "بدون مكتب")}");

                await _auditService.LogAsync(
                    "تغيير دور مستخدم",
                    "ApplicationUser",
                    user.Id,
                    $"تم تعيين دور {role} للمستخدم {user.FullName} أثناء الموافقة");

                if (user.OfficeId.HasValue)
                {
                    await _auditService.LogAsync(
                        "تعيين مكتب",
                        "ApplicationUser",
                        user.Id,
                        $"تم ربط المستخدم {user.FullName} بالمكتب رقم {user.OfficeId.Value} أثناء الموافقة");
                }

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

            await _auditService.LogAsync(
                "رفض مستخدم",
                "ApplicationUser",
                user.Id,
                $"تم رفض طلب المستخدم {user.FullName} وحذف الحساب");

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

        private async Task SetGoogleLoginAvailabilityAsync()
        {
            ViewData["GoogleLoginEnabled"] = await IsGoogleLoginAvailableAsync();
        }

        private async Task<bool> IsGoogleLoginAvailableAsync()
        {
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            return schemes.Any(scheme =>
                string.Equals(scheme.Name, GoogleDefaults.AuthenticationScheme, StringComparison.Ordinal));
        }

        private bool TryGetPublicBaseUrl(out string publicBaseUrl)
        {
            var configuredUrl = _configuration["Application:PublicBaseUrl"]?.Trim();
            if (Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(uri.Query) &&
                string.IsNullOrEmpty(uri.Fragment))
            {
                publicBaseUrl = configuredUrl!.TrimEnd('/');
                return true;
            }

            publicBaseUrl = string.Empty;
            return false;
        }

        private static string BuildResetPasswordUrl(string publicBaseUrl, string email, string token)
        {
            return $"{publicBaseUrl}/Account/ResetPassword" +
                $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }

        private static bool TryGetVerifiedExternalEmail(
            ExternalLoginInfo info,
            out string email)
        {
            email = info.Principal.FindFirstValue(ClaimTypes.Email)?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailVerifiedClaim = info.Principal.Claims.FirstOrDefault(claim =>
                string.Equals(claim.Type, "email_verified", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, "urn:google:email_verified", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    claim.Type,
                    "http://schemas.google.com/identity/claims/emailverified",
                    StringComparison.OrdinalIgnoreCase));

            return emailVerifiedClaim != null &&
                bool.TryParse(emailVerifiedClaim.Value, out var verified) &&
                verified;
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, GetSafeIdentityError(error));
            }
        }

        private static string GetGenericLoginFailureMessage()
        {
            return "تعذر تسجيل الدخول. تحقق من بيانات الدخول أو تواصل مع الإدارة.";
        }

        private static string GetSafeIdentityError(IdentityError error)
        {
            return error.Code switch
            {
                "PasswordTooShort" => "كلمة المرور أقصر من الحد الأدنى المطلوب",
                "PasswordRequiresDigit" => "يجب أن تحتوي كلمة المرور على رقم واحد على الأقل",
                "PasswordRequiresLower" => "يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير واحد على الأقل",
                "PasswordRequiresUpper" => "يجب أن تحتوي كلمة المرور على حرف إنجليزي كبير واحد على الأقل",
                "PasswordRequiresNonAlphanumeric" => "يجب أن تحتوي كلمة المرور على رمز واحد على الأقل",
                "PasswordRequiresUniqueChars" => "يجب أن تحتوي كلمة المرور على عدد أكبر من الأحرف المختلفة",
                "InvalidToken" => "رابط إعادة تعيين كلمة المرور غير صالح أو منتهي الصلاحية",
                "DuplicateEmail" => "تعذر إنشاء الحساب باستخدام بيانات Google المقدمة",
                "DuplicateUserName" => "تعذر إنشاء الحساب باستخدام بيانات Google المقدمة",
                _ => error.Description
            };
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }

        private async Task<List<SelectListItem>> BuildOfficeSelectListAsync(int? onlyOfficeId)
        {
            var query = _context.Offices
                .Where(o => o.IsActive)
                .OrderBy(o => o.Name)
                .AsQueryable();

            if (onlyOfficeId.HasValue)
                query = query.Where(o => o.Id == onlyOfficeId.Value);

            return await query
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> BuildRoleSelectListAsync(bool includeSuperAdmin)
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            if (!includeSuperAdmin)
                roles = roles.Where(r => r != "SuperAdmin").ToList();

            return roles
                .Select(r => new SelectListItem
                {
                    Value = r,
                    Text = GetRoleDisplayName(r)
                })
                .ToList();
        }

        private static string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "SuperAdmin" => "مدير النظام الأعلى",
                "OfficeAdmin" => "مدير مكتب",
                "User" => "مستخدم",
                _ => role
            };
        }
    }

}
