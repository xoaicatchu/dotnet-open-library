using FluentMigrator;

namespace DatabaseMigrations.Api.Migrations;

[Migration(202601010003, "Add Status column to Users table")]
public class Migration_202601010003_AddStatusToUsers : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("Status").AsString(20).NotNullable().WithDefaultValue("Active");
    }

    public override void Down()
    {
        Delete.Column("Status").FromTable("Users");
    }
}
