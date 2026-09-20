namespace CleanVerticalSlice.Domain.Products;

using System;
using System.Collections.Generic;
using CleanVerticalSlice.Domain.Common;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    private Money() { Currency = "VND"; } // EF Core
    
    public Money(decimal amount, string currency = "VND")
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");
        Amount = amount;
        Currency = currency;
    }
    
    protected override IEnumerable<object> GetEqualityComponents() 
    { 
        yield return Amount; 
        yield return Currency; 
    }
    
    public override string ToString() => $"{Amount:N0} {Currency}";
}
