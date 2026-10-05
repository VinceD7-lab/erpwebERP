using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erpWeb.Infrastructure.Donnees.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPlanningAnalyses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanningAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    DateAnalyse = table.Column<DateOnly>(type: "date", nullable: false),
                    TypeAnalyse = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningAnalyses", x => x.Id);
                    table.CheckConstraint("CK_PlanningAnalyses_TypeAnalyse", "[TypeAnalyse] IN ('A', 'B', 'C', 'D')");
                    table.ForeignKey(
                        name: "FK_PlanningAnalyses_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningAnalyses_DateAnalyse",
                table: "PlanningAnalyses",
                column: "DateAnalyse");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningAnalyses_IdClient_DateAnalyse",
                table: "PlanningAnalyses",
                columns: new[] { "IdClient", "DateAnalyse" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanningAnalyses");
        }
    }
}
