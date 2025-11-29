using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class GoldenFleetTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    ContactEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    PrimaryContactName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeZone = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    FleetCount = table.Column<int>(type: "int", nullable: false),
                    OktaGroupId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Owners_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fleets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fleets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fleets_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Vin = table.Column<string>(type: "char(17)", fixedLength: true, maxLength: 17, nullable: false),
                    PlateNumber = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Make = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    Model = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ModelYear = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FleetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Fleets_FleetId",
                        column: x => x.FleetId,
                        principalTable: "Fleets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vehicles_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owners",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FleetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    LastName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FleetUsers_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FleetUsers_Vehicles_AssignedVehicleId",
                        column: x => x.AssignedVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelematicsDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Iccid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    Imei = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelematicsDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelematicsDevices_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleTelemetrySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    SpeedKph = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    FuelLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTelemetrySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleTelemetrySnapshots_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_OwnerId_Name",
                table: "Fleets",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetUsers_AssignedVehicleId",
                table: "FleetUsers",
                column: "AssignedVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetUsers_Email",
                table: "FleetUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetUsers_OwnerId",
                table: "FleetUsers",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_VehicleId",
                table: "MaintenanceTickets",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Owners_CityId",
                table: "Owners",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Owners_ContactEmail",
                table: "Owners",
                column: "ContactEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelematicsDevices_SerialNumber",
                table: "TelematicsDevices",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelematicsDevices_VehicleId",
                table: "TelematicsDevices",
                column: "VehicleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_FleetId",
                table: "Vehicles",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OwnerId",
                table: "Vehicles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Vin",
                table: "Vehicles",
                column: "Vin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTelemetrySnapshots_VehicleId_CapturedAtUtc",
                table: "VehicleTelemetrySnapshots",
                columns: new[] { "VehicleId", "CapturedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FleetUsers");

            migrationBuilder.DropTable(
                name: "MaintenanceTickets");

            migrationBuilder.DropTable(
                name: "TelematicsDevices");

            migrationBuilder.DropTable(
                name: "VehicleTelemetrySnapshots");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Fleets");

            migrationBuilder.DropTable(
                name: "Owners");
        }
    }
}
