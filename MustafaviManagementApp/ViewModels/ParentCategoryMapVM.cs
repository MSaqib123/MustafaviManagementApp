using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MustafaviManagementApp.ViewModels
{
    public class ParentCategoryMapVM
    {
        [Required(ErrorMessage = "Brand (Parent Category) is required")]
        public int ParentCategoryId { get; set; }

        public string? ParentCategoryName { get; set; }

        [Required(ErrorMessage = "Select at least one child category")]
        public List<int> SelectedCategoryIds { get; set; } = new();

        public List<SelectListItem> CategoryOptions { get; set; } = new();
    }
}
