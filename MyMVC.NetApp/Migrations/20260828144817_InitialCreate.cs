using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMVC.NetApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StravaActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    AthleteId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    SportType = table.Column<string>(type: "TEXT", nullable: true),
                    Distance = table.Column<double>(type: "REAL", nullable: false),
                    MovingTime = table.Column<int>(type: "INTEGER", nullable: false),
                    ElapsedTime = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalElevationGain = table.Column<double>(type: "REAL", nullable: false),
                    StartDateUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartDateLocal = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", nullable: true),
                    AverageSpeed = table.Column<double>(type: "REAL", nullable: true),
                    MaxSpeed = table.Column<double>(type: "REAL", nullable: true),
                    AverageHeartrate = table.Column<double>(type: "REAL", nullable: true),
                    MaxHeartrate = table.Column<double>(type: "REAL", nullable: true),
                    Calories = table.Column<double>(type: "REAL", nullable: true),
                    KudosCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AchievementCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Trainer = table.Column<bool>(type: "INTEGER", nullable: false),
                    Commute = table.Column<bool>(type: "INTEGER", nullable: false),
                    Manual = table.Column<bool>(type: "INTEGER", nullable: false),
                    Private = table.Column<bool>(type: "INTEGER", nullable: false),
                    GearId = table.Column<string>(type: "TEXT", nullable: true),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false),
                    FetchedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StravaActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StravaAthletes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Firstname = table.Column<string>(type: "TEXT", nullable: true),
                    Lastname = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Sex = table.Column<string>(type: "TEXT", nullable: true),
                    Premium = table.Column<bool>(type: "INTEGER", nullable: false),
                    Summit = table.Column<bool>(type: "INTEGER", nullable: false),
                    StravaCreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    StravaUpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ProfileMediumUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ProfileUrl = table.Column<string>(type: "TEXT", nullable: true),
                    FollowerCount = table.Column<int>(type: "INTEGER", nullable: true),
                    FriendCount = table.Column<int>(type: "INTEGER", nullable: true),
                    MeasurementPreference = table.Column<string>(type: "TEXT", nullable: true),
                    Ftp = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<double>(type: "REAL", nullable: true),
                    ProfileRawJson = table.Column<string>(type: "TEXT", nullable: false),
                    StatsRawJson = table.Column<string>(type: "TEXT", nullable: true),
                    FetchedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StravaAthletes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StravaTokens",
                columns: table => new
                {
                    AthleteId = table.Column<long>(type: "INTEGER", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TokenType = table.Column<string>(type: "TEXT", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StravaTokens", x => x.AthleteId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StravaActivities_AthleteId",
                table: "StravaActivities",
                column: "AthleteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StravaActivities");

            migrationBuilder.DropTable(
                name: "StravaAthletes");

            migrationBuilder.DropTable(
                name: "StravaTokens");
        }
    }
}
