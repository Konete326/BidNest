using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

public partial class Category
{
    [Key]
    public int CategoryId { get; set; }

    [StringLength(150)]
    public string Name { get; set; } = null!;

    public int? ParentId { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("Parent")]
    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    [InverseProperty("Category")]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    [ForeignKey("ParentId")]
    [InverseProperty("InverseParent")]
    public virtual Category? Parent { get; set; }
}
