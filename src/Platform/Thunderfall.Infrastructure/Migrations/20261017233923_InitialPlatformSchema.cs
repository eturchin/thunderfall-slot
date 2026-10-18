using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thunderfall.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatformSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<string>(
                        type: "character varying(80)",
                        maxLength: 80,
                        nullable: false
                    ),
                    MathVersion = table.Column<string>(
                        type: "character varying(80)",
                        maxLength: 80,
                        nullable: false
                    ),
                    RulesetHash = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    TokenHash = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "DemoWallets",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoWallets", x => x.SessionId);
                    table.CheckConstraint("CK_Wallet_Nonnegative", "\"Balance\" >= 0");
                    table.ForeignKey(
                        name: "FK_DemoWallets_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "FeatureSessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateJson = table.Column<string>(type: "jsonb", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    MathVersion = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_FeatureSessions_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RoundRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<string>(type: "text", nullable: false),
                    MathVersion = table.Column<string>(type: "text", nullable: false),
                    RulesetHash = table.Column<string>(type: "text", nullable: false),
                    Stake = table.Column<long>(type: "bigint", nullable: false),
                    Award = table.Column<long>(type: "bigint", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    OutcomeJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundRecords_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WalletLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Delta = table.Column<long>(type: "bigint", nullable: false),
                    ClosingBalance = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletLedger_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "text", nullable: false),
                    RoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => new { x.SessionId, x.Key });
                    table.ForeignKey(
                        name: "FK_IdempotencyRecords_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_IdempotencyRecords_RoundRecords_RoundId",
                        column: x => x.RoundId,
                        principalTable: "RoundRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSessions_Active_SessionId",
                table: "FeatureSessions",
                columns: new[] { "Active", "SessionId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_TokenHash",
                table: "GameSessions",
                column: "TokenHash",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_RoundId",
                table: "IdempotencyRecords",
                column: "RoundId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RoundRecords_SessionId_CreatedAt",
                table: "RoundRecords",
                columns: new[] { "SessionId", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_WalletLedger_SessionId_CreatedAt",
                table: "WalletLedger",
                columns: new[] { "SessionId", "CreatedAt" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DemoWallets");

            migrationBuilder.DropTable(name: "FeatureSessions");

            migrationBuilder.DropTable(name: "IdempotencyRecords");

            migrationBuilder.DropTable(name: "WalletLedger");

            migrationBuilder.DropTable(name: "RoundRecords");

            migrationBuilder.DropTable(name: "GameSessions");
        }
    }
}
