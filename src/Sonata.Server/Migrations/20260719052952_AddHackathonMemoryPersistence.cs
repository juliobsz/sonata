using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sonata.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddHackathonMemoryPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "movements",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false),
                    started_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    message_id = table.Column<long>(
                        type: "bigint",
                        nullable: false),
                    excerpt = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_source_notes_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memories",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    movement_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    source_note_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    text = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false),
                    type = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false),
                    lifecycle_state = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memories", x => x.id);
                    table.ForeignKey(
                        name: "FK_memories_movements_movement_id",
                        column: x => x.movement_id,
                        principalTable: "movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_memories_source_notes_source_note_id",
                        column: x => x.source_note_id,
                        principalTable: "source_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memory_uses",
                columns: table => new
                {
                    id = table.Column<long>(
                            type: "bigint",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy
                                .IdentityByDefaultColumn),
                    memory_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    response_message_id = table.Column<long>(
                        type: "bigint",
                        nullable: false),
                    rank = table.Column<int>(
                        type: "integer",
                        nullable: false),
                    reason = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memory_uses", x => x.id);
                    table.CheckConstraint(
                        "CK_memory_uses_rank_positive",
                        "rank > 0");
                    table.ForeignKey(
                        name: "FK_memory_uses_memories_memory_id",
                        column: x => x.memory_id,
                        principalTable: "memories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_memory_uses_messages_response_message_id",
                        column: x => x.response_message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "movements",
                columns: new[] { "id", "name", "started_at" },
                values: new object[]
                {
                    new Guid("10000000-0000-0000-0000-000000000001"),
                    "Qwen AI Hackathon",
                    new DateTimeOffset(
                        new DateTime(
                            2026, 7, 19, 0, 0, 0,
                            DateTimeKind.Unspecified),
                        TimeSpan.Zero)
                });

            migrationBuilder.AddColumn<Guid>(
                name: "movement_id",
                table: "conversations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid(
                    "10000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_conversations_movement_id",
                table: "conversations",
                column: "movement_id");

            migrationBuilder.CreateIndex(
                name: "IX_memories_movement_id_lifecycle_state_created_at",
                table: "memories",
                columns: new[]
                {
                    "movement_id",
                    "lifecycle_state",
                    "created_at"
                });

            migrationBuilder.CreateIndex(
                name: "IX_memories_source_note_id",
                table: "memories",
                column: "source_note_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_memory_uses_memory_id",
                table: "memory_uses",
                column: "memory_id");

            migrationBuilder.CreateIndex(
                name: "IX_memory_uses_response_message_id_memory_id",
                table: "memory_uses",
                columns: new[]
                {
                    "response_message_id",
                    "memory_id"
                },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_notes_message_id",
                table: "source_notes",
                column: "message_id");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_movements_movement_id",
                table: "conversations",
                column: "movement_id",
                principalTable: "movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE conversations
                ALTER COLUMN movement_id DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_movements_movement_id",
                table: "conversations");

            migrationBuilder.DropTable(name: "memory_uses");
            migrationBuilder.DropTable(name: "memories");
            migrationBuilder.DropTable(name: "movements");
            migrationBuilder.DropTable(name: "source_notes");

            migrationBuilder.DropIndex(
                name: "IX_conversations_movement_id",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "movement_id",
                table: "conversations");
        }
    }
}
