using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class Categry : BaseEntity
    {
        public int Id { get; set; }
        public required String Name { get; set; }
        public required string Description { get; set; } 
    }
}
