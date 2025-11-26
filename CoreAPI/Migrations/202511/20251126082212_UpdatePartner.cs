using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAPI.Migrations._202511
{
    /// <inheritdoc />
    public partial class UpdatePartner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Warning",
                table: "Partner",
                newName: "WebSite");

            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "Partner",
                newName: "ToastWarning");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Partner",
                newName: "ResidenceTypeIdText");

            migrationBuilder.RenameColumn(
                name: "AdditionTypeIdsText",
                table: "Partner",
                newName: "ReceivesSwiftCode");

            migrationBuilder.RenameColumn(
                name: "AdditionTypeIds",
                table: "Partner",
                newName: "ReceivesBankId");

            migrationBuilder.RenameColumn(
                name: "AccNumber",
                table: "Partner",
                newName: "ReceivesAccountNumber");

            migrationBuilder.RenameColumn(
                name: "AccName",
                table: "Partner",
                newName: "ReceivesAccountName");

            migrationBuilder.AddColumn<string>(
                name: "AccountBranch",
                table: "Partner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "Partner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Partner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerTypeIdText",
                table: "Partner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                table: "Partner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureName",
                table: "Partner",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenedAt",
                table: "Partner",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountBranch",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "CustomerTypeIdText",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "Fax",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "FeatureName",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "Partner");

            migrationBuilder.RenameColumn(
                name: "WebSite",
                table: "Partner",
                newName: "Warning");

            migrationBuilder.RenameColumn(
                name: "ToastWarning",
                table: "Partner",
                newName: "Icon");

            migrationBuilder.RenameColumn(
                name: "ResidenceTypeIdText",
                table: "Partner",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "ReceivesSwiftCode",
                table: "Partner",
                newName: "AdditionTypeIdsText");

            migrationBuilder.RenameColumn(
                name: "ReceivesBankId",
                table: "Partner",
                newName: "AdditionTypeIds");

            migrationBuilder.RenameColumn(
                name: "ReceivesAccountNumber",
                table: "Partner",
                newName: "AccNumber");

            migrationBuilder.RenameColumn(
                name: "ReceivesAccountName",
                table: "Partner",
                newName: "AccName");
        }
    }
}
