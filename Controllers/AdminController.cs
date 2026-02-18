using Microsoft.AspNetCore.Mvc;
using LaundryApp.Data;
using LaundryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Text;

namespace LaundryApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly OrderStore _orderStore;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LaundryAppDbContext _dbContext;

    public AdminController(OrderStore orderStore, UserManager<ApplicationUser> userManager, LaundryAppDbContext dbContext)
    {
        _orderStore = orderStore;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [HttpGet("/Admin")]
public IActionResult Index(string? status, string? filter, string? search)
{

    // Accept either query param name
    var selected = !string.IsNullOrWhiteSpace(status) ? status : filter;
    selected ??= "All";

    var allOrders = _orderStore.All().ToList();

    // Apply search filter if provided
    if (!string.IsNullOrWhiteSpace(search))
    {
        search = search.ToLower();
        allOrders = allOrders.Where(o => 
            o.UserEmail.ToLower().Contains(search) ||
            o.Id.ToString().Contains(search) ||
            o.Address.ToLower().Contains(search) ||
            GetUserName(o.UserEmail).ToLower().Contains(search)
        ).ToList();
    }

    // Counts for pills (only count from filtered results if searching)
    var countSource = string.IsNullOrWhiteSpace(search) ? _orderStore.All().ToList() : allOrders;
    ViewBag.CountAll = countSource.Count;
    ViewBag.CountScheduled = countSource.Count(o => o.Status == "Scheduled");
    ViewBag.CountInProgress = countSource.Count(o => o.Status == "In Progress");
    ViewBag.CountCompleted = countSource.Count(o => o.Status == "Completed");
    ViewBag.CountCancelled = countSource.Count(o => o.Status == "Cancelled");

    ViewBag.Status = selected;
    ViewBag.Search = search;
    ViewBag.LastUpdated = DateTime.Now;

    var orders = allOrders
        .OrderByDescending(o => o.CreatedAt)
        .ToList();

    if (selected != "All")
        orders = orders.Where(o => o.Status == selected).ToList();

    var ordersWithUsers = orders.Select(o => new OrderWithUserViewModel
    {
        Order = o,
        UserName = GetUserName(o.UserEmail)
    }).ToList();

    return View(ordersWithUsers);
}


    [HttpPost]
    public IActionResult UpdateStatus(int id, string status, string currentStatus = "All", string? search = null)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        var oldStatus = order.Status;
        order.Status = status;
        order.LastUpdatedAt = DateTime.Now;
        _orderStore.Save();

        // Log audit
        var auditLog = new AuditLog
        {
            UserEmail = User.Identity?.Name ?? "Unknown",
            Action = "Status Changed",
            Entity = $"Order #{id}",
            Details = $"{oldStatus} → {status}"
        };
        _dbContext.AuditLogs.Add(auditLog);
        _dbContext.SaveChanges();

        return RedirectToAction("Index", new { status = currentStatus, search = search });
    }

    [HttpPost]
    public IActionResult UpdateAdminNotes(int id, string adminNotes, string currentStatus = "All", string? search = null)
    {
        var order = _orderStore.Get(id);
        if (order == null) return NotFound();

        var oldNotes = order.AdminNotes;
        order.AdminNotes = adminNotes;
        order.LastUpdatedAt = DateTime.Now;
        _orderStore.Save();

        // Log audit
        var auditLog = new AuditLog
        {
            UserEmail = User.Identity?.Name ?? "Unknown",
            Action = "Admin Notes Updated",
            Entity = $"Order #{id}",
            Details = $"Notes changed"
        };
        _dbContext.AuditLogs.Add(auditLog);
        _dbContext.SaveChanges();

        return RedirectToAction("Index", new { status = currentStatus, search = search });
    }

    [HttpPost]
    public IActionResult Delete(int id, string currentStatus = "All", string? search = null)
    {
        var order = _orderStore.Get(id);
        if (order != null)
        {
            // Log audit before deleting
            var auditLog = new AuditLog
            {
                UserEmail = User.Identity?.Name ?? "Unknown",
                Action = "Order Deleted",
                Entity = $"Order #{id}",
                Details = $"Deleted order for {order.UserEmail}"
            };
            _dbContext.AuditLogs.Add(auditLog);
            _dbContext.SaveChanges();
        }

        _orderStore.Delete(id);

        return RedirectToAction("Index", new { status = currentStatus, search = search });
    }

    [HttpGet]
    public IActionResult ExportCsv(string? status)
    {
        var orders = _orderStore.All().ToList();

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            orders = orders.Where(o => o.Status == status).ToList();
        }

        var csv = new StringBuilder();
        csv.AppendLine("Id,UserEmail,ServiceType,ScheduledAt,Address,Notes,AdminNotes,Status,CreatedAt,LastUpdatedAt");

        foreach (var order in orders)
        {
            csv.AppendLine($"{order.Id},{EscapeCsv(order.UserEmail)},{EscapeCsv(order.ServiceType)},{order.ScheduledAt:yyyy-MM-dd HH:mm:ss},{EscapeCsv(order.Address)},{EscapeCsv(order.Notes)},{EscapeCsv(order.AdminNotes)},{EscapeCsv(order.Status)},{order.CreatedAt:yyyy-MM-dd HH:mm:ss},{order.LastUpdatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "orders.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var userRoles = new List<UserRoleViewModel>();

        foreach (var user in users)
        {
            var appUser = user as ApplicationUser;
            var roles = await _userManager.GetRolesAsync(user);
            userRoles.Add(new UserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                FirstName = appUser?.FirstName ?? "",
                LastName = appUser?.LastName ?? "",
                PhoneNumber = appUser?.PhoneNumber ?? "",
                AddressLine1 = appUser?.AddressLine1 ?? "",
                AddressLine2 = appUser?.AddressLine2 ?? "",
                City = appUser?.City ?? "",
                State = appUser?.State ?? "",
                ZipCode = appUser?.ZipCode ?? "",
                IsAdmin = roles.Contains("Admin"),
                Roles = string.Join(", ", roles)
            });
        }

        return View(userRoles);
    }

    [HttpGet]
    public IActionResult CreateUser()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(string email, string password, string confirmPassword, string firstName, string lastName, string phoneNumber, string addressLine1, string addressLine2, string city, string state, string zipCode, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Email is required");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Password is required");
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match");
        }

        if (password?.Length < 6)
        {
            ModelState.AddModelError("", "Password must be at least 6 characters");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            ModelState.AddModelError("", "A user with this email already exists");
            return View();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            State = state,
            ZipCode = zipCode
        };
        var result = await _userManager.CreateAsync(user, password!);

        if (result.Succeeded)
        {
            // Add to User role by default
            await _userManager.AddToRoleAsync(user, "User");

            // Add to Admin role if selected
            if (isAdmin)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            TempData["Message"] = $"User {email} created successfully" + (isAdmin ? " with admin privileges" : "");
            return RedirectToAction("Users");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var appUser = user as ApplicationUser;
        var roles = await _userManager.GetRolesAsync(user);

        var model = new UserRoleViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            FirstName = appUser?.FirstName ?? "",
            LastName = appUser?.LastName ?? "",
            PhoneNumber = appUser?.PhoneNumber ?? "",
            AddressLine1 = appUser?.AddressLine1 ?? "",
            AddressLine2 = appUser?.AddressLine2 ?? "",
            City = appUser?.City ?? "",
            State = appUser?.State ?? "",
            ZipCode = appUser?.ZipCode ?? "",
            IsAdmin = roles.Contains("Admin"),
            Roles = string.Join(", ", roles)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string phoneNumber, string addressLine1, string addressLine2, string city, string state, string zipCode, bool isAdmin)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var appUser = user as ApplicationUser;
        if (appUser == null) return BadRequest();

        // Update user properties
        appUser.FirstName = firstName ?? "";
        appUser.LastName = lastName ?? "";
        appUser.PhoneNumber = phoneNumber ?? "";
        appUser.AddressLine1 = addressLine1 ?? "";
        appUser.AddressLine2 = addressLine2 ?? "";
        appUser.City = city ?? "";
        appUser.State = state ?? "";
        appUser.ZipCode = zipCode ?? "";

        // Update user
        var updateResult = await _userManager.UpdateAsync(appUser);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(new UserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                City = city,
                State = state,
                ZipCode = zipCode,
                IsAdmin = isAdmin
            });
        }

        // Update admin role
        var currentRoles = await _userManager.GetRolesAsync(user);
        var isCurrentlyAdmin = currentRoles.Contains("Admin");

        if (isAdmin && !isCurrentlyAdmin)
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }
        else if (!isAdmin && isCurrentlyAdmin)
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
        }

        // Log audit
        var auditLog = new AuditLog
        {
            UserEmail = User.Identity?.Name ?? "Unknown",
            Action = "User Updated",
            Entity = $"User {appUser.Email}",
            Details = $"Updated user information"
        };
        _dbContext.AuditLogs.Add(auditLog);
        _dbContext.SaveChanges();

        TempData["Message"] = $"User {appUser.Email} updated successfully";
        return RedirectToAction("Users");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var appUser = user as ApplicationUser;

        // Log audit before deleting
        var auditLog = new AuditLog
        {
            UserEmail = User.Identity?.Name ?? "Unknown",
            Action = "User Deleted",
            Entity = $"User {user.Email}",
            Details = $"Deleted user account"
        };
        _dbContext.AuditLogs.Add(auditLog);
        _dbContext.SaveChanges();

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            TempData["Error"] = "Failed to delete user";
            return RedirectToAction("Users");
        }

        TempData["Message"] = $"User {user.Email} deleted successfully";
        return RedirectToAction("Users");
    }

    [HttpGet]
    public IActionResult AuditLogs()
    {
        var logs = _dbContext.AuditLogs.OrderByDescending(l => l.Timestamp).ToList();
        return View(logs);
    }

    private string EscapeCsv(string value)
    {
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    private string GetUserName(string email)
    {
        var user = _userManager.FindByEmailAsync(email).Result;
        if (user != null)
        {
            return $"{user.FirstName} {user.LastName}";
        }
        return email; // fallback
    }
}



