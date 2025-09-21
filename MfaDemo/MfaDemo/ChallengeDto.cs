using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MfaDemo
{
    public class ChallengeDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Method { get; set; } = string.Empty;
    }
}
