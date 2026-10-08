using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GRIF.Migrations.ExchangeUpload
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelDocumentWord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NumberOfEditing = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelDocumentWord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TableFootnotes",
                columns: table => new
                {
                    FootNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: true),
                    Column = table.Column<int>(type: "integer", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableFootnotes", x => x.FootNoteId);
                });

            migrationBuilder.CreateTable(
                name: "DocumentParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ModelDocumentWordId = table.Column<int>(type: "integer", nullable: false),
                    NumberOfEditing = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentParts_ModelDocumentWord_ModelDocumentWordId",
                        column: x => x.ModelDocumentWordId,
                        principalTable: "ModelDocumentWord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentBlock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    DocumentPartId = table.Column<int>(type: "integer", nullable: false),
                    NumberOfEditing = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    BookmarkName = table.Column<string>(type: "text", nullable: true),
                    ColumnHeaders = table.Column<string[]>(type: "text[]", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    TextBlock_BookmarkName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentBlock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentBlock_DocumentParts_DocumentPartId",
                        column: x => x.DocumentPartId,
                        principalTable: "DocumentParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TableRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    TableBlockId = table.Column<int>(type: "integer", nullable: false),
                    FootnoteId = table.Column<int>(type: "integer", nullable: true),
                    Columns = table.Column<List<string>>(type: "text[]", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    AdditionalInfo = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HashCode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableRows_DocumentBlock_TableBlockId",
                        column: x => x.TableBlockId,
                        principalTable: "DocumentBlock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RowFootnote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    TableRowId = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Column = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RowFootnote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RowFootnote_TableRows_TableRowId",
                        column: x => x.TableRowId,
                        principalTable: "TableRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlock_DocumentPartId",
                table: "DocumentBlock",
                column: "DocumentPartId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentParts_ModelDocumentWordId",
                table: "DocumentParts",
                column: "ModelDocumentWordId");

            migrationBuilder.CreateIndex(
                name: "IX_RowFootnote_TableRowId",
                table: "RowFootnote",
                column: "TableRowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TableRows_TableBlockId",
                table: "TableRows",
                column: "TableBlockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RowFootnote");

            migrationBuilder.DropTable(
                name: "TableFootnotes");

            migrationBuilder.DropTable(
                name: "TableRows");

            migrationBuilder.DropTable(
                name: "DocumentBlock");

            migrationBuilder.DropTable(
                name: "DocumentParts");

            migrationBuilder.DropTable(
                name: "ModelDocumentWord");
        }
    }
}
