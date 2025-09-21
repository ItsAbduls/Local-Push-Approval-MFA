using System.Text;
using OtpNet;

namespace MFAApi.Services
{
    public class TotpService
    {
        private readonly byte[] _secret;

        public TotpService()
        {
            // Static secret for demo; per-user in real app
            _secret = Encoding.ASCII.GetBytes("super-secret-totp-key");
        }

        public string GenerateCode()
        {
            var totp = new Totp(_secret);
            return totp.ComputeTotp(DateTime.UtcNow);
        }

        public bool VerifyCode(string code)
        {
            var totp = new Totp(_secret);
            return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
    }
}
