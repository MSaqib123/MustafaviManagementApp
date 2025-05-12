using MedicineStore.Models;

namespace MustafaviManagementApp.Models
{
    public class CategoryParentCategory
    {
        public int Id { get;set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int ParentCategoryId { get; set; }
        public ParentCategory ParentCategory { get; set; } = null!;
    }
}
