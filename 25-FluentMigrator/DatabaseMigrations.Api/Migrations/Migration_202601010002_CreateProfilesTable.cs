using FluentMigrator;

namespace DatabaseMigrations.Api.Migrations;

[Migration(202601010002, "Create Profiles table with foreign key")]
public class Migration_202601010002_CreateProfilesTable : Migration
{
    public override void Up()
    {
        Create.Table("Profiles")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable().ForeignKey("FK_Profiles_Users", "Users", "Id")
            .WithColumn("FullName").AsString(100).NotNullable()
            .WithColumn("Bio").AsString(500).Nullable();
    }

    public override void Down()
    {
        Delete.Table("Profiles");
    }
}
