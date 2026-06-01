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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IAuditService _auditService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            IWhatsAppService whatsAppService,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
            _auditService = auditService;
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
