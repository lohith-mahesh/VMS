using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RRVMS.Api.Migrations;

[Migration("20260906160000_VisitorDetailsAndEcId")]
public partial class VisitorDetailsAndEcId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "VisitingCompanyAddress", table: "Visitors", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "VisitingCompanyCountry", table: "Visitors", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<bool>(name: "IsFaculty", table: "Visitors", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "IsGtre", table: "Visitors", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "EcIdType", table: "Visitors", type: "text", nullable: true);

        migrationBuilder.AddColumn<string>(name: "VisitingCompanyAddress", table: "VisitorForms", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "VisitingCompanyCountry", table: "VisitorForms", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<bool>(name: "IsFaculty", table: "VisitorForms", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "IsGtre", table: "VisitorForms", type: "boolean", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "VisitingCompanyAddress", table: "Visitors");
        migrationBuilder.DropColumn(name: "VisitingCompanyCountry", table: "Visitors");
        migrationBuilder.DropColumn(name: "IsFaculty", table: "Visitors");
        migrationBuilder.DropColumn(name: "IsGtre", table: "Visitors");
        migrationBuilder.DropColumn(name: "EcIdType", table: "Visitors");
        migrationBuilder.DropColumn(name: "VisitingCompanyAddress", table: "VisitorForms");
        migrationBuilder.DropColumn(name: "VisitingCompanyCountry", table: "VisitorForms");
        migrationBuilder.DropColumn(name: "IsFaculty", table: "VisitorForms");
        migrationBuilder.DropColumn(name: "IsGtre", table: "VisitorForms");
    }
}