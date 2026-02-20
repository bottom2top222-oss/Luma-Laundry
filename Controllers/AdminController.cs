using Microsoft.AspNetCore.Mvc;
using LaundryApp.Data;
using LaundryApp.Models;
using LaundryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LaundryApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly OrderStore _orderStore;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LaundryAppDbContext _dbContext;
    private readonly PaymentService _paymentService;
    private readonly LayeredApiJobClient _layeredApiJobClient;
    private readonly LayeredApiOrderClient _layeredApiOrderClient;
    private readonly bool _apiOnlyMode;

    public AdminController(OrderStore orderStore, UserManager<ApplicationUser> userManager, LaundryAppDbContext dbContext, PaymentService paymentService, LayeredApiJobClient layeredApiJobClient, LayeredApiOrderClient layeredApiOrderClient, IConfiguration configuration)
    {
        _orderStore = orderStore;
        _userManager = userManager;
        _dbContext = dbContext;
        _paymentService = paymentService;
        _layeredApiJobClient = layeredApiJobClient;
        _layeredApiOrderClient = layeredApiOrderClient;
        _apiOnlyMode = bool.TryParse(configuration["LayeredServices:ApiOnlyMode"], out var apiOnly) && apiOnly;
    }

    [HttpGet("/Admin")]
public async Task<IActionResult> Index(string? status, string? filter, string? search)
{

    // Accept either query param name
    var selected = !string.IsNullOrWhiteSpace(status) ? status : filter;
    selected ??= "All";

    var apiOrders = await _layeredApiOrderClient.GetAdminOrdersAsync(selected, search);
    
    // Use local store if: API returned null OR (API returned empty AND not in API-only mode AND we're filtering all statuses)
    var shouldUseLocalStore = apiOrders == null || 
        (apiOrders.Count == 0 && !_apiOnlyMode && selected == "All");
    
    var allOrders = shouldUseLocalStore ? _orderStore.All().ToList() : (apiOrders ?? new List<LaundryOrder>());

    if (apiOrders == null && _apiOnlyMode)
    {
        ViewBag.Error = "Order service is temporarily unavailable.";
    }

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
    var countSource = allOrders;
    ViewBag.CountAll = countSource.Count;
    ViewBag.CountScheduled = countSource.Count(o => o.Status == "PendingPickup");
    ViewBag.CountInProgress = countSource.Count(o => o.Status == "InProgress");
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
    public async Task<IActionResult> UpdateStatus(int id, string status, string currentStatus = "All", string? search = null)
    {
        var order = await _layeredApiOrderClient.GetOrderAsync(id) ?? (_apiOnlyMode ? null : _orderStore.Get(id));
        if (order == null) return NotFound();

        var oldStatus = order.Status;
        var statusUpdatedViaApi = await _layeredApiOrderClient.UpdateOrderStatusAsync(id, status);
        if (!statusUpdatedViaApi)
        {
            if (_apiOnlyMode)
            {
                TempData["Error"] = "Unable to update order status right now.";
                return RedirectToAction("Index", new { status = currentStatus, search = search });
            }

            order.Status = status;
            order.LastUpdatedAt = DateTime.Now;
            _orderStore.Save();
        }
        else
        {
            order.Status = status;
        }

        // Handle quote creation when processing is completed
        if (status == "Quoted")
        {
            try
            {
                var quote = BuildQuote(order);
                var invoiceResult = await _layeredApiOrderClient.GenerateInvoiceAsync(
                    id,
                    subtotal: quote.subtotal,
                    taxAmount: 0,
                    deliveryFee: 0,
                    lineItems: quote.lineItems);

                if (!invoiceResult.success)
                {
                    if (_apiOnlyMode)
                    {
                        throw new Exception("API invoice generation failed in API-only mode.");
                    }

                    var dbOrder = await _dbContext.Orders.FindAsync(id);
                    if (dbOrder != null && dbOrder.InvoiceId == null)
                    {
                        await _paymentService.GenerateInvoiceAsync(
                            id,
                            subtotal: quote.subtotal,
                            taxAmount: 0,
                            deliveryFee: 0,
                            lineItems: quote.lineItems
                        );
                    }
                }

                order.PaymentStatus = "ApprovalRequired";
            }
            catch (Exception ex)
            {
                // Log error but don't block status update
                var auditLog = new AuditLog
                {
                    UserEmail = User.Identity?.Name ?? "Unknown",
                    Action = "Invoice Generation Failed",
                    Entity = $"Order #{id}",
                    Details = $"Error: {ex.Message}"
                };
                _dbContext.AuditLogs.Add(auditLog);
            }
        }
        else if (status == "Approved")
        {
            order.PaymentStatus = "Approved";
        }
        else if (status == "Ready" && (order.PaymentStatus == "PaymentMethodOnFile" || order.PaymentStatus == "Approved"))
        {
            try
            {
                var attemptResult = await _layeredApiOrderClient.AttemptPaymentAsync(id);

                if (attemptResult.success)
                {
                    order.PaymentStatus = "ChargeAttempted";

                    if (attemptResult.status == "success")
                    {
                        var apiOrder = await _layeredApiOrderClient.GetOrderAsync(id);
                        if (apiOrder != null)
                        {
                            var apiInvoice = await _layeredApiOrderClient.GetInvoiceAsync(id);
                            var receiptAttempt = new PaymentAttempt
                            {
                                OrderId = id,
                                Status = attemptResult.status,
                                Amount = attemptResult.amount,
                                TransactionId = attemptResult.transactionId,
                                AttemptNumber = 1,
                                CreatedAt = DateTime.Now
                            };

                            await _layeredApiJobClient.QueueReceiptEmailAsync(apiOrder, apiInvoice, receiptAttempt);
                        }
                    }
                }
                else
                {
                    if (_apiOnlyMode)
                    {
                        throw new Exception("API payment attempt failed in API-only mode.");
                    }

                    var dbOrder = await _dbContext.Orders.FindAsync(id);
                    if (dbOrder != null && dbOrder.InvoiceId != null)
                    {
                        var invoice = await _dbContext.Invoices.FindAsync(dbOrder.InvoiceId);
                        if (invoice != null && dbOrder.PaymentMethodId != null)
                        {
                            var attempt = await _paymentService.AttemptPaymentAsync(id, invoice.Total, dbOrder.PaymentMethodId.Value);

                            if (attempt.Status == "success")
                            {
                                dbOrder.PaymentStatus = "Paid";
                                await _layeredApiJobClient.QueueReceiptEmailAsync(dbOrder, invoice, attempt);
                            }
                            else
                            {
                                dbOrder.Status = "PaymentFailed";
                                dbOrder.PaymentStatus = "PaymentFailed";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't block status update
                var auditLog = new AuditLog
                {
                    UserEmail = User.Identity?.Name ?? "Unknown",
                    Action = "Payment Attempt Failed",
                    Entity = $"Order #{id}",
                    Details = $"Error: {ex.Message}"
                };
                _dbContext.AuditLogs.Add(auditLog);
            }
        }

        // Log audit
        var auditLog2 = new AuditLog
        {
            UserEmail = User.Identity?.Name ?? "Unknown",
            Action = "Status Changed",
            Entity = $"Order #{id}",
            Details = $"{oldStatus} → {status}"
        };
        _dbContext.AuditLogs.Add(auditLog2);
        _dbContext.SaveChanges();

        return RedirectToAction("Index", new { status = currentStatus, search = search });
    }

    private (decimal subtotal, string lineItems) BuildQuote(LaundryOrder order)
    {
        var notes = (order.Notes ?? string.Empty).ToLowerInvariant();
        var serviceType = (order.ServiceType ?? string.Empty).ToLowerInvariant();
        var lineItems = new List<object>();
        decimal subtotal = 0m;

        var includesWashFold = serviceType.Contains("wash") || serviceType.Contains("fold");
        var includesLarge = serviceType.Contains("large") || serviceType.Contains("bedding");
        var includesHousehold = serviceType.Contains("household");

        var isByRequest = notes.Contains("by request") || notes.Contains("request");
        var washRate = isByRequest ? 2.25m : 2.00m;

        if (includesWashFold)
        {
            var weight = ExtractWeight(notes);
            var billableWeight = Math.Max(weight, 20m);
            var washTotal = billableWeight * washRate;
            subtotal += washTotal;
            lineItems.Add(new { description = $"Wash & Fold ({billableWeight:0.##} lbs @ ${washRate:0.00}/lb)", amount = washTotal });
        }

        var largeCatalog = new Dictionary<string, decimal>
        {
            ["comforter (king)"] = 34.99m,
            ["comforter (queen)"] = 34.99m,
            ["comforter (full)"] = 32.99m,
            ["comforter (twin)"] = 32.99m,
            ["duvet cover"] = 19.99m,
            ["blanket"] = 17.99m,
        };

        if (includesLarge || includesHousehold)
        {
            foreach (var item in largeCatalog)
            {
                var qty = ExtractQuantity(notes, item.Key);
                if (qty <= 0) continue;
                var amount = qty * item.Value;
                subtotal += amount;
                lineItems.Add(new { description = $"{item.Key} x{qty}", amount });
            }

            var weightedBlanketWeight = ExtractWeightedBlanketWeight(notes);
            if (weightedBlanketWeight > 0)
            {
                var weightedAmount = weightedBlanketWeight * 2.85m;
                subtotal += weightedAmount;
                lineItems.Add(new { description = $"Weighted blanket ({weightedBlanketWeight:0.##} lbs @ $2.85/lb)", amount = weightedAmount });
            }
        }

        var minimum = includesLarge || notes.Contains("weighted blanket") ? 50m : 0m;
        if (includesWashFold)
        {
            minimum = Math.Max(minimum, 20m * washRate);
        }

        if (subtotal < minimum)
        {
            lineItems.Add(new { description = "Minimum pricing applied", amount = minimum - subtotal });
            subtotal = minimum;
        }

        if (subtotal <= 0)
        {
            subtotal = 40m;
            lineItems.Add(new { description = "Manual pricing placeholder", amount = subtotal });
        }

        return (subtotal, JsonSerializer.Serialize(lineItems));
    }

    private static decimal ExtractWeight(string notes)
    {
        var match = Regex.Match(notes, @"(\d+(?:\.\d+)?)\s*(lb|lbs|pounds)");
        return match.Success && decimal.TryParse(match.Groups[1].Value, out var weight) ? weight : 0m;
    }

    private static int ExtractQuantity(string notes, string itemName)
    {
        var pattern = Regex.Escape(itemName) + @"\s*(x\s*(\d+))?";
        var match = Regex.Match(notes, pattern);
        if (!match.Success) return 0;

        if (match.Groups.Count > 2 && int.TryParse(match.Groups[2].Value, out var qty))
        {
            return Math.Max(qty, 1);
        }

        return 1;
    }

    private static decimal ExtractWeightedBlanketWeight(string notes)
    {
        var match = Regex.Match(notes, @"weighted blanket[^\d]*(\d+(?:\.\d+)?)");
        return match.Success && decimal.TryParse(match.Groups[1].Value, out var weight) ? weight : 0m;
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAdminNotes(int id, string adminNotes, string currentStatus = "All", string? search = null)
    {
        var order = await _layeredApiOrderClient.GetOrderAsync(id) ?? (_apiOnlyMode ? null : _orderStore.Get(id));
        if (order == null) return NotFound();

        var oldNotes = order.AdminNotes;
        var updatedViaApi = await _layeredApiOrderClient.UpdateAdminNotesAsync(id, adminNotes);
        if (!updatedViaApi)
        {
            if (_apiOnlyMode)
            {
                TempData["Error"] = "Unable to update admin notes right now.";
                return RedirectToAction("Index", new { status = currentStatus, search = search });
            }

            order.AdminNotes = adminNotes;
            order.LastUpdatedAt = DateTime.Now;
            _orderStore.Save();
        }

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
    public async Task<IActionResult> Delete(int id, string currentStatus = "All", string? search = null)
    {
        var order = await _layeredApiOrderClient.GetOrderAsync(id) ?? (_apiOnlyMode ? null : _orderStore.Get(id));
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

        var deletedViaApi = await _layeredApiOrderClient.DeleteOrderAsync(id);
        if (!deletedViaApi)
        {
            if (_apiOnlyMode)
            {
                TempData["Error"] = "Unable to delete order right now.";
                return RedirectToAction("Index", new { status = currentStatus, search = search });
            }

            _orderStore.Delete(id);
        }

        return RedirectToAction("Index", new { status = currentStatus, search = search });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? status)
    {
        var apiOrders = await _layeredApiOrderClient.GetAdminOrdersAsync(status, null);
        var orders = apiOrders ?? (_apiOnlyMode ? new List<LaundryOrder>() : _orderStore.All().ToList());

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
                FirstName = firstName ?? "",
                LastName = lastName ?? "",
                PhoneNumber = phoneNumber ?? "",
                AddressLine1 = addressLine1 ?? "",
                AddressLine2 = addressLine2 ?? "",
                City = city ?? "",
                State = state ?? "",
                ZipCode = zipCode ?? "",
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



