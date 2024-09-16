using System;
using System.Collections.Generic;

namespace Grocery_store.Models;

public partial class UnitOfMeasure
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
