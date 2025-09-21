namespace MFAApi.Models
{
    public class LoginChallenge
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Method { get; set; } = "Push"; // Push, TOTP, SMS, Email
        public string Status { get; set; } = "Pending"; // Pending, Approved, Consumed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);
    }
}
