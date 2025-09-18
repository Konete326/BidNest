using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentId { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public string? ParentName { get; set; }
        public int ItemCount { get; set; }
        public int SubCategoryCount { get; set; }
        public List<CategoryViewModel> SubCategories { get; set; } = new();
    }

    public class CategoryListViewModel
    {
        public List<CategoryViewModel> Categories { get; set; } = new();
        public string? SearchTerm { get; set; }
        public bool ShowInactive { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
    }

    public class CategoryMergeViewModel
    {
        [Required(ErrorMessage = "Source category is required")]
        [Display(Name = "Source Category (to be merged)")]
        public int SourceCategoryId { get; set; }

        [Required(ErrorMessage = "Target category is required")]
        [Display(Name = "Target Category (merge into)")]
        public int TargetCategoryId { get; set; }

        public string? SourceCategoryName { get; set; }
        public string? TargetCategoryName { get; set; }

        [Display(Name = "Delete source category after merge")]
        public bool DeleteSourceCategory { get; set; } = true;
    }
}
