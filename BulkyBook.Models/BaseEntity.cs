using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    /// <summary>
    /// Base class for entities with soft delete and audit fields
    /// </summary>
    public abstract class BaseEntity
    {
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Created By")]
        [StringLength(450)] // Max length for Identity User ID
        public string? CreatedBy { get; set; }

        [Display(Name = "Modified By")]
        [StringLength(450)] // Max length for Identity User ID
        public string? ModifiedBy { get; set; }
    }
}

