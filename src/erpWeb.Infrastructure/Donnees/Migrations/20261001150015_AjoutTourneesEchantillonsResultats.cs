using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erpWeb.Infrastructure.Donnees.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTourneesEchantillonsResultats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourneesRamassage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateTournee = table.Column<DateOnly>(type: "date", nullable: false),
                    NomChauffeur = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImmatriculationCamion = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    StatutTemperature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourneesRamassage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Echantillons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeBarresAnonyme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatePrelevement = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Filiere = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatutAnalyse = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TemperatureReception = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IdTournee = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Echantillons", x => x.Id);
                    table.CheckConstraint("CK_Echantillons_TemperatureReception", "[TemperatureReception] IS NULL OR [TemperatureReception] BETWEEN -10 AND 100");
                    table.ForeignKey(
                        name: "FK_Echantillons_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Echantillons_TourneesRamassage_IdTournee",
                        column: x => x.IdTournee,
                        principalTable: "TourneesRamassage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultatsAgronomie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEchantillon = table.Column<int>(type: "int", nullable: false),
                    TypeSupport = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhSol = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    MatiereOrganique = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    PhosphoreP2O5 = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: true),
                    PotassiumK2O = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: true),
                    ReliquatAzoteN = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: true),
                    ValeurUclFourrage = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    DateValidation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultatsAgronomie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultatsAgronomie_Echantillons_IdEchantillon",
                        column: x => x.IdEchantillon,
                        principalTable: "Echantillons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Echantillons_CodeBarresAnonyme",
                table: "Echantillons",
                column: "CodeBarresAnonyme",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Echantillons_IdClient",
                table: "Echantillons",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_Echantillons_IdTournee",
                table: "Echantillons",
                column: "IdTournee");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsAgronomie_IdEchantillon",
                table: "ResultatsAgronomie",
                column: "IdEchantillon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourneesRamassage_DateTournee",
                table: "TourneesRamassage",
                column: "DateTournee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultatsAgronomie");

            migrationBuilder.DropTable(
                name: "Echantillons");

            migrationBuilder.DropTable(
                name: "TourneesRamassage");
        }
    }
}
