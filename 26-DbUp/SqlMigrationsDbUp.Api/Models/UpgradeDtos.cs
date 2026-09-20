namespace SqlMigrationsDbUp.Api.Models;

public record UpgradeStatusDto(
    bool IsUpgradeRequired,
    List<string> ExecutedScripts,
    List<string> PendingScripts
);

public record UpgradeResultDto(
    bool Successful,
    int ScriptsExecutedCount,
    List<string> ExecutedScripts,
    string? ErrorMessage
);

public record TableInfoDto(
    string TableName,
    int RowCount,
    List<string> Columns
);
