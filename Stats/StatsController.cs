using Coflnet.Auth;
using Coflnet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly StatsService statsService;
    private readonly ILogger<StatsController> logger;

    public StatsController(StatsService statsService, ILogger<StatsController> logger)
    {
        this.statsService = statsService;
        this.logger = logger;
    }

    [HttpGet("all")]
    [Authorize]
    public async Task<IEnumerable<Stat>> GetAllStats()
    {
        var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new ApiException("missing_user_id", "User id not found in claims"));
        return await statsService.GetStats(userId);
    }


    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IEnumerable<Stat>> GetUserStats(Guid userId)
    {
        return await statsService.GetStats(userId);
    }

    [HttpGet("stat/{statName}")]
    [Authorize]
    public async Task<long> GetStat(string statName)
    {
        var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new ApiException("missing_user_id", "User id not found in claims"));
        return await statsService.GetStat(userId, statName);
    }

    [HttpGet("stat/daily_exp")]
    [Authorize]

    public async Task<long> GetDailyExpStats()
    {
        return await statsService.GetExpireStat(DateTime.Now, User.UserId(), "daily_exp");
    }
    [HttpGet("stat/weekly_exp")]
    [Authorize]

    public async Task<long> GetWeeklyExpStats()
    {
        var lastDayOfWeek = DateTime.Now.RoundDown(TimeSpan.FromDays(7)).AddDays(7);
        return await statsService.GetExpireStat(lastDayOfWeek, User.UserId(), "weekly_exp");
    }
}
