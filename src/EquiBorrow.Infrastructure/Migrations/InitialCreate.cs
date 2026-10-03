using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiBorrow.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Borrowings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    BorrowDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedReturnDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Borrowings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Borrowings_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Borrowings_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Seed data for students
            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "Name", "IsActive" },
                values: new object[,]
                {
                    { 1, "Ash", true },
                    { 2, "Billy Jean", false },
                    { 3, "Charlie Puth", true },
                    { 4, "Ashley", true },
                    { 5, "Steven", true },
                    { 6, "Joanna", false },
                    { 7, "Fluffy", false },
                    { 8, "Quifrey", true },
                    { 9, "Alice", true },
                    { 10, "Bob", false },
                    { 11, "Queenie", true }
                });

            // Seed data for equipment
            migrationBuilder.InsertData(
                table: "Equipment",
                columns: new[] { "Id", "Name", "IsAvailable" },
                values: new object[,]
                {
                    { 101, "Laptop Dell XPS", true },
                    { 102, "Projector Epson", true },
                    { 103, "Arduino Kit", false },
                    { 104, "3D Printer", true },
                    { 105, "Digital Camera Canon", false },
                    { 106, "VR Headset Oculus", true },
                    { 107, "Microphone Blue Yeti", true },
                    { 108, "Tablet iPad Pro", false },
                    { 109, "Smartwatch Apple Watch", true },
                    { 110, "External Hard Drive Seagate", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Borrowings_EquipmentId",
                table: "Borrowings",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Borrowings_StudentId",
                table: "Borrowings",
                column: "StudentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Borrowings");
            migrationBuilder.DropTable(name: "Equipment");
            migrationBuilder.DropTable(name: "Students");
        }
    }
}
