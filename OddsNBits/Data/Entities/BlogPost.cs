using System.ComponentModel.DataAnnotations;

namespace OddsNBits.Models
{
}

namespace OddsNBits.Data.Entities
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; }

        [MaxLength(150)]
        public string Slug { get; set; }

        [MaxLength(120)]
        public string Image { get; set; }

        [Required, MaxLength(500)]
        public string Introduction { get; set; }

        public string Content { get; set; }

        // [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
        public short CategoryId { get; set; }
        public string UserId { get; set; }

        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }


        public DateTime CreatedOn { get; set; }
        public DateTime? PublishedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        // Might do some sort of "hot topic" special featured list with this?
        public int ViewCount { get; set; }

        public virtual Category Category { get; set; }
        public virtual ApplicationUser User { get; set; }

        // public ICollection<Comment> Comments { get; set; }
    }
}
