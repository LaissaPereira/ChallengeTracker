using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChallengeTrackerApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeDomainModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.Ordinal);
            var guidType = sqlServer ? "uniqueidentifier" : "TEXT";
            var stringType = sqlServer ? "nvarchar(max)" : "TEXT";
            var intType = sqlServer ? "int" : "INTEGER";
            var dateType = sqlServer ? "date" : "TEXT";
            var dateTimeOffsetType = sqlServer ? "datetimeoffset" : "TEXT";
            var decimalType = sqlServer ? "decimal(18,2)" : "TEXT";

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    OwnerId = table.Column<Guid>(type: guidType, nullable: false),
                    Title = table.Column<string>(type: stringType, nullable: false),
                    Description = table.Column<string>(type: stringType, nullable: true),
                    Visibility = table.Column<int>(type: intType, nullable: false),
                    Status = table.Column<int>(type: intType, nullable: false),
                    StartDate = table.Column<DateOnly>(type: dateType, nullable: false),
                    EndDate = table.Column<DateOnly>(type: dateType, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: dateTimeOffsetType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    UserId = table.Column<Guid>(type: guidType, nullable: false),
                    ChallengeId = table.Column<Guid>(type: guidType, nullable: false),
                    Status = table.Column<int>(type: intType, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: dateTimeOffsetType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Memberships_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    UserId = table.Column<Guid>(type: guidType, nullable: false),
                    ChallengeId = table.Column<Guid>(type: guidType, nullable: false),
                    Amount = table.Column<decimal>(type: decimalType, nullable: false),
                    Note = table.Column<string>(type: stringType, nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(type: dateTimeOffsetType, nullable: false),
                    LoggedDate = table.Column<DateOnly>(type: dateType, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: dateTimeOffsetType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgressEntries_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_OwnerId",
                table: "Challenges",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ChallengeId",
                table: "Memberships",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserId_ChallengeId",
                table: "Memberships",
                columns: new[] { "UserId", "ChallengeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressEntries_ChallengeId",
                table: "ProgressEntries",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressEntries_UserId_ChallengeId_LoggedDate",
                table: "ProgressEntries",
                columns: new[] { "UserId", "ChallengeId", "LoggedDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropTable(
                name: "ProgressEntries");

            migrationBuilder.DropTable(
                name: "Challenges");
        }
    }
}
