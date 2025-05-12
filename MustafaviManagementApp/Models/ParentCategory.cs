using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MustafaviManagementApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MedicineStore.Models
{
    public class ParentCategory
    {
        public int ParentCategoryId { get; set; }

        [Required, StringLength(100)]
        public string ParentCategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // children one-to-many
        public ICollection<Category> Categories { get; set; } = new List<Category>();


        /* NEW — many-to-many bridge navigation */
        public ICollection<CategoryParentCategory> CategoryParentCategories { get; set; }
            = new List<CategoryParentCategory>();
    }

}