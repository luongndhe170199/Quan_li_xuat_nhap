using System;
using System.Collections.Generic;

namespace Grocery_store.Models;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public int Quantity { get; set; }

    public int? UnitPrice { get; set; }

    public bool? IsWholesale { get; set; }

    public int? InvoiceId { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Product? Product { get; set; }
}
