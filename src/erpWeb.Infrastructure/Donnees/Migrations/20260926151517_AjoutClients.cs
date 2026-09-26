using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erpWeb.Infrastructure.Donnees.Migrations
{
    /// <inheritdoc />
    public partial class AjoutClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RaisonSociale = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Adresse1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Adresse2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CodePostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Ville = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Pays = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Telephone1 = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Telephone2 = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_RaisonSociale",
                table: "Clients",
                column: "RaisonSociale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
