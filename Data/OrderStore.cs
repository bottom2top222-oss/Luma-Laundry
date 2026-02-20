using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using LaundryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryApp.Data;

public class OrderStore
{
    private readonly LaundryAppDbContext _dbContext;

    public OrderStore(LaundryAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<LaundryOrder> All()
    {
        return _dbContext.Orders.OrderByDescending(o => o.CreatedAt).ToList();
    }

    public LaundryOrder Add(LaundryOrder order)
    {
        order.UserEmail = (order.UserEmail ?? string.Empty).Trim();
        order.CreatedAt = DateTime.Now;
        order.LastUpdatedAt = DateTime.Now;
        _dbContext.Orders.Add(order);
        _dbContext.SaveChanges();
        return order;
    }

    public LaundryOrder? Get(int id)
    {
        return _dbContext.Orders.FirstOrDefault(o => o.Id == id);
    }

    public IReadOnlyList<LaundryOrder> ByUser(string userEmail)
    {
        return _dbContext.Orders
            .AsEnumerable()
            .Where(o => EmailsMatch(o.UserEmail, userEmail))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
    }

    private static bool EmailsMatch(string? left, string? right)
    {
        var normalizedLeft = NormalizeEmail(left);
        var normalizedRight = NormalizeEmail(right);
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        return new string(value
            .Trim()
            .Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format)
            .ToArray());
    }

    public bool Delete(int id)
    {
        var order = Get(id);
        if (order == null) return false;

        _dbContext.Orders.Remove(order);
        _dbContext.SaveChanges();
        return true;
    }

    public void Save()
    {
        _dbContext.SaveChanges();
    }
}

