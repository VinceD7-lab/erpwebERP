using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erpWeb.Infrastructure.Donnees.Migrations
{
    /// <inheritdoc />
    public partial class AjoutFactures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Factures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEchantillon = table.Column<int>(type: "int", nullable: false),
                    NumeroFacture = table.Column<int>(type: "int", nullable: false),
                    DateFacture = table.Column<DateOnly>(type: "date", nullable: false),
                    DateEcheance = table.Column<DateOnly>(type: "date", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    NomClient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdresseFacturation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodePostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Ville = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Pays = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModePaiement = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MontantHorsTaxe = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TauxTva = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MontantTva = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MontantToutesTaxesComprises = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Remise = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Devise = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    StatutFacture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DatePaiement = table.Column<DateOnly>(type: "date", nullable: true),
                    Commentaire = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factures", x => x.Id);
                    table.CheckConstraint("CK_Factures_Montants", "[MontantHorsTaxe] >= 0 AND [MontantTva] >= 0 AND [MontantToutesTaxesComprises] >= 0");
                    table.CheckConstraint("CK_Factures_Remise", "[Remise] IS NULL OR [Remise] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_Factures_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Factures_Echantillons_IdEchantillon",
                        column: x => x.IdEchantillon,
                        principalTable: "Echantillons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Factures_DateFacture",
                table: "Factures",
                column: "DateFacture");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_IdClient",
                table: "Factures",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_IdEchantillon",
                table: "Factures",
                column: "IdEchantillon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factures_NumeroFacture",
                table: "Factures",
                column: "NumeroFacture",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Factures");
        }
    }
}
