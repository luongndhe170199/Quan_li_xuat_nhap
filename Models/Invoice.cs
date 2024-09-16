using System;
using System.Collections.Generic;

namespace Grocery_store.Models;

public partial class Invoice
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int? TotalAmount { get; set; }

    public int? Discount { get; set; }

    public int? FinalAmount { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
