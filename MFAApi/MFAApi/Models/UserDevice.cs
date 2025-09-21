namespace MFAApi.Models
{
    public class UserDevice
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string DeviceName { get; set; } = default!;
        public string? FcmToken { get; set; }
    }
}
