using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erpWeb.Infrastructure.Donnees.Migrations
{
    /// <inheritdoc />
    public partial class AjoutDevis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    DateDevis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateValidite = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Objet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NotesInternes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConditionsPaiement = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DelaiLivraison = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Devise = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    MontantTotalHorsTaxe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantTotalTaxe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantTotalToutesTaxesComprises = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TauxRemiseGlobale = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    DateEnvoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateReponse = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotifRefus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devis", x => x.Id);
                    table.CheckConstraint("CK_Devis_Montants", "[MontantTotalHorsTaxe] >= 0 AND [MontantTotalTaxe] >= 0 AND [MontantTotalToutesTaxesComprises] >= 0");
                    table.CheckConstraint("CK_Devis_TauxRemiseGlobale", "[TauxRemiseGlobale] IS NULL OR [TauxRemiseGlobale] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_Devis_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LignesDevis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevisId = table.Column<int>(type: "int", nullable: false),
                    NumeroLigne = table.Column<int>(type: "int", nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Quantite = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Unite = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PrixUnitaireHorsTaxe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TauxRemise = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TauxTaxe = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MontantHorsTaxe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesDevis", x => x.Id);
                    table.CheckConstraint("CK_LignesDevis_TauxRemise", "[TauxRemise] IS NULL OR [TauxRemise] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_LignesDevis_Devis_DevisId",
                        column: x => x.DevisId,
                        principalTable: "Devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devis_ClientId",
                table: "Devis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_Numero",
                table: "Devis",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LignesDevis_DevisId_NumeroLigne",
                table: "LignesDevis",
                columns: new[] { "DevisId", "NumeroLigne" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LignesDevis");

            migrationBuilder.DropTable(
                name: "Devis");
        }
    }
}
