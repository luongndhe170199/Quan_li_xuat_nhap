using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grocery_store.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public int? ImportPrice { get; set; }

    public int? RetailPrice { get; set; }

    public int? WholesalePrice { get; set; }

    public int? UnitOfMeasureId { get; set; }

    public int Quantity { get; set; }

    // Đánh dấu thuộc tính ImageFile là không ánh xạ vào cơ sở dữ liệu
    [NotMapped]
    public IFormFile? ImageFile { get; set; }


    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual UnitOfMeasure? UnitOfMeasure { get; set; }
}
