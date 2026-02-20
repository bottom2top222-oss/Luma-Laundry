using System;
using System.Linq;
using System.Collections.Generic;
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
        var normalizedEmail = (userEmail ?? string.Empty).Trim().ToLower();

        return _dbContext.Orders
            .Where(o => ((o.UserEmail ?? string.Empty).Trim().ToLower()) == normalizedEmail)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
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

