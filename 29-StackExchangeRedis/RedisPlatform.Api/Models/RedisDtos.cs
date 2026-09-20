namespace RedisPlatform.Api.Models;

public record CounterResponseDto(
    string Key,
    long Value
);

public record HashFieldRequest(
    string Field,
    string Value
);

public record HashResponseDto(
    string Key,
    Dictionary<string, string> Entries
);

public record SetMemberRequest(
    string Member
);

public record SetResponseDto(
    string Key,
    List<string> Members
);

public record LeaderboardScoreRequest(
    string Member,
    double Score
);

public record LeaderboardEntryDto(
    string Member,
    double Score,
    long Rank
);

public record PublishMessageRequest(
    string Channel,
    string Message
);

public record PublishResponseDto(
    string Channel,
    long ReceiversCount,
    string Message
);
