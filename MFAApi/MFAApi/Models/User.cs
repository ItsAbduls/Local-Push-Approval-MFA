namespace MFAApi.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
    }

    public record ApproveRequest(Guid ChallengeId);
}
