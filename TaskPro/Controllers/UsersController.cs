using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using TaskPro.Helper;
using TaskPro.Models;

namespace TaskPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly MailProvider _emailSender;
        private readonly TaskProDbContext _db;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, TaskProDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = new MailProvider(configuration);
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var users = await _userManager.Users.Where(u => u.CreatedBy == userId).ToListAsync();
                var userViewModels = new List<UserViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    bool hasTasks = await _db.TaskAssignments.AnyAsync(t => t.AssignedTo == user.Id);

                    if (!roles.Contains("Admin"))
                    {
                        userViewModels.Add(new UserViewModel
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FullName = user.FullName,
                            Role = roles.FirstOrDefault(),
                            HasTasks = hasTasks
                        });
                    }
                }
                return View(userViewModels);
            }

            return RedirectToPage("/Account/Login", new { returnUrl = Url.Action("Index", "Home") });
        }

        public IActionResult Create()
        {
            var roles = _roleManager.Roles
                                    .Where(r => r.Name != "Admin") // exclude Admin
                                    .Select(r => new SelectListItem
                                    {
                                        Value = r.Name,
                                        Text = r.Name
                                    })
                                    .ToList();

            ViewBag.Roles = roles;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
                await SendSetPasswordEmail(user);

            TempData["Message"] = "Email resent successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var user = new ApplicationUser
                    {
                        FullName = model.FullName,
                        UserName = model.Email,
                        Email = model.Email,
                        CreatedBy = userId,
                    };

                    var result = await _userManager.CreateAsync(user, "DefaultPass@123");
                    if (result.Succeeded)
                    {
                        if (!await _roleManager.RoleExistsAsync(model.Role))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(model.Role));
                        }
                        await _userManager.AddToRoleAsync(user, model.Role);
                        await SendSetPasswordEmail(user);

                        return RedirectToAction(nameof(Index));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            var roles = _roleManager.Roles
                                   .Where(r => r.Name != "Admin") // exclude Admin
                                   .Select(r => new SelectListItem
                                   {
                                       Value = r.Name,
                                       Text = r.Name
                                   })
                                   .ToList();

            ViewBag.Roles = roles;
            return View(model);
        }

        private async System.Threading.Tasks.Task SendSetPasswordEmail(ApplicationUser user)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code },
                protocol: Request.Scheme);

            var emailResult = _emailSender.SendEmail(
                                        user.Email,
                                        "Set New Password",
                                        $"Please set your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Role = roles.FirstOrDefault(),
                FullName = user.FullName
            }; 
            var rolesViewBag = _roleManager.Roles
                                   .Where(r => r.Name != "Admin") // exclude Admin
                                   .Select(r => new SelectListItem
                                   {
                                       Value = r.Name,
                                       Text = r.Name
                                   })
                                   .ToList();

            ViewBag.Roles = rolesViewBag;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }
                await _userManager.AddToRoleAsync(user, model.Role);
                user.Email = model.Email;
                user.UserName = model.Email;
                user.FullName = model.FullName;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    if (user.Email != model.Email)
                        await SendSetPasswordEmail(user);

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var roles = _roleManager.Roles
                        .Where(r => r.Name != "Admin") // exclude Admin
                        .Select(r => new SelectListItem
                        {
                            Value = r.Name,
                            Text = r.Name
                        })
                        .ToList();

            ViewBag.Roles = roles;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}