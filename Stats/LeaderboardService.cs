using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Coflnet.Leaderboard.Client.Api;
using Coflnet.Leaderboard.Client.Model;
using ISession = Cassandra.ISession;

public class LeaderboardService
{
    IScoresApi scoresApi;
    Table<Profile> profileTable;
    string prefix = "ctw_";

    public LeaderboardService(IScoresApi scoresApi, ISession session)
    {
        this.scoresApi = scoresApi;
        var mapping = new MappingConfiguration()
            .Define(new Map<Profile>()
            .PartitionKey(t => t.UserId)
            .Column(t => t.Name)
            .Column(t => t.Avatar)
        );
        profileTable = new Table<Profile>(session, mapping, "leaderboard_profiles");
        profileTable.CreateIfNotExists();
    }

    public class Profile
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string? Avatar { get; set; }
    }

    public class PublicProfile
    {
        public string Name { get; set; }
        public string Avatar { get; set; }
    }

    public class BoardEntry
    {
        public Profile? User { get; set; }
        public long Score { get; set; }
    }

    public async Task<IEnumerable<BoardEntry>> GetLeaderboard(string leaderboardId, int offset = 0, int count = 10)
    {
        var users = await scoresApi.ScoresLeaderboardSlugGetAsync(GetPrfixed(leaderboardId), offset, count);
        return await FormatWithProfile(users);
    }

    private string GetPrfixed(string leaderboardId)
    {
        return prefix + leaderboardId;
    }

    private async Task<IEnumerable<BoardEntry>> FormatWithProfile(List<BoardScore> users)
    {
        var ids = users.Select(u => Guid.Parse(u.UserId)).ToHashSet();
        var profiles = await profileTable.Where(p => ids.Contains(p.UserId)).ExecuteAsync();
        return users.Select(u => new BoardEntry()
        {
            User = profiles.FirstOrDefault(p => p.UserId == Guid.Parse(u.UserId)) ?? new Profile() { Name = "Unknown", Avatar = null, UserId = Guid.Parse(u.UserId) },
            Score = u.Score
        });
    }

    public async Task<IEnumerable<BoardEntry>> GetLeaderboardAroundMe(string leaderboardId, Guid userId, int count = 10)
    {
        var around = await scoresApi.ScoresLeaderboardSlugUserUserIdGetAsync(GetPrfixed(leaderboardId), userId.ToString(), count, count / 2);
        return await FormatWithProfile(around);
    }

    public async Task<PublicProfile> GetProfile(Guid userId)
    {
        var internalProfile = await profileTable.Where(p => p.UserId == userId).FirstOrDefault().ExecuteAsync();
        return new PublicProfile() { Name = internalProfile?.Name ?? "Unknown", Avatar = internalProfile?.Avatar ?? "" };
    }

    public async Task SetProfile(Guid userId, string name, string avatar)
    {
        await profileTable.Insert(new Profile() { UserId = userId, Name = name, Avatar = avatar }).ExecuteAsync();
    }

    public async Task SetScore(string leaderboardId, Guid userId, long score)
    {
        await scoresApi.ScoresLeaderboardSlugPostAsync(GetPrfixed(leaderboardId), new()
        {
            Confidence = 1,
            Score = score,
            HighScore = true,
            UserId = userId.ToString(),
            DaysToKeep = 30
        });
    }

    public async Task<long> GetRankOf(string leaderboardId, Guid userId)
    {
        return await scoresApi.ScoresLeaderboardSlugUserUserIdRankGetAsync(GetPrfixed(leaderboardId), userId.ToString());
    }
}
