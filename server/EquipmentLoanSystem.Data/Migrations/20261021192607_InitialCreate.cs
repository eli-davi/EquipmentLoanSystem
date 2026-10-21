using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EquipmentLoanSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryItem",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "integer", nullable: false),
                    ItemsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryItem", x => new { x.CategoriesId, x.ItemsId });
                    table.ForeignKey(
                        name: "FK_CategoryItem_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryItem_Items_ItemsId",
                        column: x => x.ItemsId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemCopies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCopies_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemCopyId = table.Column<int>(type: "integer", nullable: false),
                    MemberId = table.Column<int>(type: "integer", nullable: false),
                    LoanDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_ItemCopies_ItemCopyId",
                        column: x => x.ItemCopyId,
                        principalTable: "ItemCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "צילום" },
                    { 2, "הקרנה" },
                    { 3, "שמע" },
                    { 4, "תאורה" },
                    { 5, "מחשוב" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "מצלמה מקצועית ללא מראה, כולל עדשת 24-105", "מצלמת Canon EOS R6" },
                    { 2, "מקרן 3,500 לומן עם כבל HDMI", "מקרן Epson Full HD" },
                    { 3, "רמקול נייד עם סוללה ל-12 שעות", "ערכת סאונד ניידת JBL" },
                    { 4, "זוג פנסי LED עם חצובות", "תאורת סטודיו Godox" },
                    { 5, "מחשב נייד 14 אינץ' להצגות ולעבודה", "מחשב נייד Dell Latitude" },
                    { 6, "חצובת אלומיניום עם ראש נע", "חצובה מקצועית Manfrotto" },
                    { 7, "מיקרופון יד אלחוטי עם מקלט", "מיקרופון אלחוטי Shure" },
                    { 8, "מסך על חצובה, מתקפל לתיק נשיאה", "מסך הקרנה נייד 100 אינץ'" }
                });

            migrationBuilder.InsertData(
                table: "Members",
                columns: new[] { "Id", "Email", "FirstName", "LastName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, "admin@example.com", "מנהל", "מערכת", "PBKDF2$100000$qQlWURpgU+3PvgI+keKZ8g==$8XCeZ8DkM3XHvtGFCwzb/Z42BSTY6Wre9WQqR4DcuQQ=", "Admin" },
                    { 2, "user@example.com", "משתמש", "ראשון", "PBKDF2$100000$UtQAw7uQ8bdDoHRvP4yx5w==$XMK0VkjswHNCUoUrXxNuTjSJnhq1l2Z3ZLHUug4yZjI=", "User" },
                    { 3, "user2@example.com", "משתמש", "שני", "PBKDF2$100000$/cLNS0FO2Isf3FtSBPTV+Q==$Mxe7oZ73f4DSQFd1Qc3PoEjXxHLOad+wjLObLZs7TpE=", "User" }
                });

            migrationBuilder.InsertData(
                table: "CategoryItem",
                columns: new[] { "CategoriesId", "ItemsId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 4 },
                    { 1, 6 },
                    { 2, 2 },
                    { 2, 5 },
                    { 2, 8 },
                    { 3, 3 },
                    { 3, 7 },
                    { 4, 4 },
                    { 5, 5 }
                });

            migrationBuilder.InsertData(
                table: "ItemCopies",
                columns: new[] { "Id", "ItemId", "SerialNumber", "Status", "Version" },
                values: new object[,]
                {
                    { 1, 1, "SN-001", 0, new Guid("6bb3b812-1bc7-47ed-94f6-b357e06a9159") },
                    { 2, 2, "SN-002", 1, new Guid("889dd2b4-a2cc-4f5f-9f8f-cc2266487f13") },
                    { 3, 3, "SN-003", 0, new Guid("fc68de6b-f02a-4eea-ae84-89e7d0b5491c") },
                    { 4, 4, "SN-004", 0, new Guid("46e85045-553c-4e51-b3ed-17be9b7d064d") },
                    { 5, 5, "SN-005", 0, new Guid("438e13fe-949f-45d7-8d66-c8d6172c3771") },
                    { 6, 6, "SN-006", 0, new Guid("fcd0ba9c-b3e4-49ca-8e3b-b6e7a4c0084b") },
                    { 7, 7, "SN-007", 0, new Guid("86745196-a6f3-42b6-b22e-a9fa1e20d04f") },
                    { 8, 8, "SN-008", 0, new Guid("12a006e2-472b-4228-8504-6344b04cf1c2") }
                });

            migrationBuilder.InsertData(
                table: "Loans",
                columns: new[] { "Id", "DueDate", "ItemCopyId", "LoanDate", "MemberId", "ReturnDate", "Status" },
                values: new object[] { 1, new DateTime(2026, 12, 31, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 3, null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryItem_ItemsId",
                table: "CategoryItem",
                column: "ItemsId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCopies_ItemId_Status",
                table: "ItemCopies",
                columns: new[] { "ItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCopies_SerialNumber",
                table: "ItemCopies",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                table: "Items",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans",
                column: "ItemCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_MemberId_Status",
                table: "Loans",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Members_Email",
                table: "Members",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryItem");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "ItemCopies");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}
