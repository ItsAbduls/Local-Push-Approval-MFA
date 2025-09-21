using MFAApi.Data;
using MFAApi.Models;

namespace MFAApi.Services
{
    public class ChallengeService
    {
        private readonly AppDbContext _db;

        public ChallengeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<LoginChallenge> CreateChallengeAsync(Guid userId, string method)
        {
            var challenge = new LoginChallenge
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Method = method,
                Status = "Pending"
            };
            _db.LoginChallenges.Add(challenge);
            await _db.SaveChangesAsync();
            return challenge;
        }

        public async Task<bool> MarkApproved(Guid challengeId)
        {
            var ch = await _db.LoginChallenges.FindAsync(challengeId);
            if (ch is null) return false;

            ch.Status = "Approved";
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task MarkConsumed(Guid challengeId)
        {
            var ch = await _db.LoginChallenges.FindAsync(challengeId);
            if (ch is null) return;

            ch.Status = "Consumed";
            await _db.SaveChangesAsync();
        }
    }
}
