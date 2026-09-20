namespace DatabaseMigrations.Api.Models;

public record MigrationHistoryDto(
    long Version,
    DateTime AppliedOn,
    string? Description
);

public record TableInfoDto(
    string TableName,
    int ColumnCount,
    List<string> Columns
);

public record MigrationResultDto(
    string Action,
    long? TargetVersion,
    string Message
);
