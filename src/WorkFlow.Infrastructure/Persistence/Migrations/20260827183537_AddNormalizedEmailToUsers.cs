using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedEmailToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_system_admin_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ux_users_tenant_id_email",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                    UPDATE users
                    SET normalized_email = UPPER(BTRIM(email));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_system_admin_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_users_tenant_id_normalized_email",
                table: "users",
                columns: new[] { "tenant_id", "normalized_email" },
                unique: true,
                filter: "tenant_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_system_admin_normalized_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ux_users_tenant_id_normalized_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ux_users_system_admin_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_users_tenant_id_email",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true,
                filter: "tenant_id IS NOT NULL");
        }
    }
}
