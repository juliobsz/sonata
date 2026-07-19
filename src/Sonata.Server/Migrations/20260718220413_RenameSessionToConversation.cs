using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sonata.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameSessionToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_sessions_session_id",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sessions",
                table: "sessions");

            migrationBuilder.RenameTable(
                name: "sessions",
                newName: "conversations");

            migrationBuilder.RenameColumn(
                name: "started_at",
                table: "conversations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "session_id",
                table: "messages",
                newName: "conversation_id");

            migrationBuilder.RenameIndex(
                name: "IX_messages_session_id_sequence",
                table: "messages",
                newName: "IX_messages_conversation_id_sequence");

            migrationBuilder.AddPrimaryKey(
                name: "PK_conversations",
                table: "conversations",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_conversations_conversation_id",
                table: "messages",
                column: "conversation_id",
                principalTable: "conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_conversations_conversation_id",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_conversations",
                table: "conversations");

            migrationBuilder.RenameTable(
                name: "conversations",
                newName: "sessions");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "sessions",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "conversation_id",
                table: "messages",
                newName: "session_id");

            migrationBuilder.RenameIndex(
                name: "IX_messages_conversation_id_sequence",
                table: "messages",
                newName: "IX_messages_session_id_sequence");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sessions",
                table: "sessions",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_sessions_session_id",
                table: "messages",
                column: "session_id",
                principalTable: "sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
