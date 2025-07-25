using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ldap_tels.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LdapSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Server = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    BaseDn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BindDn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BindPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchFilter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UseSSL = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LdapSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Division = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistinguishedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LdapSourceId = table.Column<int>(type: "int", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContactType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_LdapSources_LdapSourceId",
                        column: x => x.LdapSourceId,
                        principalTable: "LdapSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_DisplayName",
                table: "Contacts",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Email",
                table: "Contacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_LastName",
                table: "Contacts",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_LdapSourceId",
                table: "Contacts",
                column: "LdapSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_PhoneNumber",
                table: "Contacts",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "LdapSources");
        }
    }
}
