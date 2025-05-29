using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ldap_tels.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMobileField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "Contacts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "Contacts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
