using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shandis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMomentComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MomentComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MomentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MomentComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MomentComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MomentComments_MemoryMoments_MomentId",
                        column: x => x.MomentId,
                        principalTable: "MemoryMoments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MomentComments_MomentComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "MomentComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MomentCommentLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MomentCommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MomentCommentLikes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MomentCommentLikes_MomentComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "MomentComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MomentCommentLikes_CommentId_UserId",
                table: "MomentCommentLikes",
                columns: new[] { "CommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MomentCommentLikes_UserId",
                table: "MomentCommentLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MomentComments_MomentId_ParentCommentId_CreatedAtUtc",
                table: "MomentComments",
                columns: new[] { "MomentId", "ParentCommentId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MomentComments_ParentCommentId",
                table: "MomentComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_MomentComments_UserId",
                table: "MomentComments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MomentCommentLikes");

            migrationBuilder.DropTable(
                name: "MomentComments");
        }
    }
}
