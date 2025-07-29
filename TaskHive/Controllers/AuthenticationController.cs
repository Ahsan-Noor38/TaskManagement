using System;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web.Mvc;
using TaskHive.Helper;
using TaskHive.Models;

namespace TaskHive.Controllers
{
    public class AuthenticationController : Controller
    {
        private TaskHiveDB db = new TaskHiveDB();

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            // 1. Validate input.
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Message"] = "Email and password are required.";
                return View();
            }

            // 2. Hash the input password.
            string passwordHash = HashPassword(password);

            // 3. Find user by email and password hash.
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == passwordHash);

            // 4. If not found, return error.
            if (user == null)
            {
                TempData["Message"] = "Invalid email or password.";
                return View();
            }

            BaseHelper.User = user;
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(User user)
        {
            // 1. Validate model state.
            // 2. Check if email already exists in DB.
            // 3. If exists, add error and return view.
            // 4. Hash password.
            // 5. Set default role to Admin.
            // 6. Set CreatedAt.
            // 7. Save user to DB.
            // 8. Redirect to login or show success.

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var existingUser = db.Users.FirstOrDefault(u => u.Email == user.Email);
            if (existingUser != null)
            {
                TempData["Error"] = "Email already exists.";
                return View(user);
            }

            // Hash password
            user.PasswordHash = HashPassword(user.PasswordHash);

            // Set default role to Admin
            var adminRole = db.Roles.FirstOrDefault(r => r.Name == StaticDetails.Roles.Admin);
            if (adminRole == null)
            {
                adminRole = new Role { Name = StaticDetails.Roles.Admin };
                db.Roles.Add(adminRole);
                db.SaveChanges();
            }
            user.RoleId = adminRole.Id;

            user.CreatedAt = DateTime.Now;

            db.Users.Add(user);
            db.SaveChanges();

            TempData["Message"] = "Successfully Register. Please Login!!";
            return RedirectToAction("Login");
        }
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            var user = db.Users.Where(x => x.Email == email).FirstOrDefault();
            if (user == null)
            {
                TempData["Message"] = "invalid email";
                return RedirectToAction("Login");
            }
            Random random = new Random();
            int code = random.Next(1001, 9999);
            Session["code"] = code;
            Session["userforgotpassword"] = user;
            MailProvider.Sentforgotpassmail(user.Email, code);
            return RedirectToAction("CodeVerify");
        }
        public ActionResult CodeVerify()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CodeVerify(int code)
        {
            int sentcode = (int)Session["code"];
            if (code == sentcode)
            {
                TempData["Message"] = "Code validate!!Please Set your new password";
                return RedirectToAction("NewPassword");
            }
            TempData["Error"] = "invalid code";
            return View();
        }
        public ActionResult NewPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult NewPassword(string password)
        {
            var user = (User)Session["userforgotpassword"];
            var v = db.Users.Where(x => x.Email == user.Email).FirstOrDefault();
            v.PasswordHash = HashPassword(password);
            db.Entry(v).State = EntityState.Modified;
            db.SaveChanges();
            TempData["Message"] = "Successfully changed password!!";
            return RedirectToAction("login");
        }

        // Simple password hash using SHA256
        private string HashPassword(string password)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}