﻿using System;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.Common.Events;

public class PaymentFailed
{
    public string status { get; set; }

    public CustomerCheckout customer { get; set; }

    public int orderId { get; set; }
    public List<OrderItem> items { get; set; }
    public float totalAmount { get; set; }
    public string instanceId { get; set; }

    public PaymentFailed(){ }

    public PaymentFailed(string status, CustomerCheckout customer, int orderId, List<OrderItem> items, float totalAmount, string instanceId)
    {
        this.status = status;
        this.customer = customer;
        this.orderId = orderId;
        this.items = items;
        this.totalAmount = totalAmount;
        this.instanceId = instanceId;
    }
}