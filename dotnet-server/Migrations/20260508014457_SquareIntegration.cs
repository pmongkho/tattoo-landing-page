using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_server.Migrations
{
    /// <inheritdoc />
    public partial class SquareIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SquareBookingId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SquareCustomerId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SquareLocationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SquareOrderId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SquareInvoiceId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AppointmentStartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DepositAmountCents = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InvoicePublicUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingDeposits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SquareWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SquareEventId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SquareWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeposits_SquareBookingId",
                table: "BookingDeposits",
                column: "SquareBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeposits_SquareInvoiceId",
                table: "BookingDeposits",
                column: "SquareInvoiceId",
                unique: true,
                filter: "\"SquareInvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SquareWebhookEvents_SquareEventId",
                table: "SquareWebhookEvents",
                column: "SquareEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingDeposits");

            migrationBuilder.DropTable(
                name: "SquareWebhookEvents");
        }
    }
}
