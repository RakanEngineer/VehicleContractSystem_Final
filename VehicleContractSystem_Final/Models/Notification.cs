using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = "Info"; // Info, Success, Warning, Danger

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? RelatedEntityType { get; set; } // "User", "Contract", "Office"
        public string? RelatedEntityId { get; set; }

        public string? ActionUrl { get; set; }
    }
}
