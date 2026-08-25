using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreventTenantDeletionWithUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_tenants",
                table: "users");

            migrationBuilder.AddForeignKey(
                name: "fk_users_tenants",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_tenants",
                table: "users");

            migrationBuilder.AddForeignKey(
                name: "fk_users_tenants",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
