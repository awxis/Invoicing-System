using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Client_Invoice_System.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerBankAccountsAndRestructureOwnerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "AccountTitle",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "BeneficiaryName",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "BranchAddress",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "PaymentInstruction",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "Sortcode",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "Swiftcode",
                table: "Owners");

            migrationBuilder.CreateTable(
                name: "OwnerBankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerProfileId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IBAN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SwiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankBranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReceivingPaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpecificBankPaymentInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerBankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerBankAccounts_CountryCurrencies_CountryId",
                        column: x => x.CountryId,
                        principalTable: "CountryCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerBankAccounts_CountryCurrencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "CountryCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerBankAccounts_Owners_OwnerProfileId",
                        column: x => x.OwnerProfileId,
                        principalTable: "Owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Invoice_Generated_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<double>(type: "float", nullable: false),
                    Invoice_paid_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receipts_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_Receipts_CountryCurrencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "CountryCurrencies",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9a869d71-1c2a-4b99-9f2f-4fbbda7b0d5e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "cd645ff5-3fc7-4fa0-90be-6431918e170c", "AQAAAAIAAYagAAAAECAl0VlB4b5xwmboO2PFBviWIRUtEvdHsD0nS+eHMpTXTQOsNdsxmRKUBO9oLVfEUA==", "3f7ffb7c-a590-484e-8412-e71720305345" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBankAccounts_CountryId",
                table: "OwnerBankAccounts",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBankAccounts_CurrencyId",
                table: "OwnerBankAccounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBankAccounts_OwnerProfileId_CurrencyId_IsDefault",
                table: "OwnerBankAccounts",
                columns: new[] { "OwnerProfileId", "CurrencyId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ClientId",
                table: "Receipts",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_CurrencyId",
                table: "Receipts",
                column: "CurrencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnerBankAccounts");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Owners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccountTitle",
                table: "Owners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Owners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryName",
                table: "Owners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchAddress",
                table: "Owners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentInstruction",
                table: "Owners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sortcode",
                table: "Owners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Swiftcode",
                table: "Owners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9a869d71-1c2a-4b99-9f2f-4fbbda7b0d5e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "6058f417-7a56-46c0-a19f-324eee66f88e", "AQAAAAIAAYagAAAAEN75WosjPol5bJUhC0+txwUF0v7rWXGcGIGjOvEOfYJ/chWXEaoWwjHYLZf6O6KDVg==", "0b7cbb71-d31e-4046-a83f-42609bfdd838" });
        }
    }
}
