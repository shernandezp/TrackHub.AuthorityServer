using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Security.Infrastructure.Migrations.SecurityDb
{
    /// <inheritdoc />
    [DbContext(typeof(SecurityDbContext))]
    [Migration("20260518023000_AddUserAccountId")]
    public partial class AddUserAccountId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE security.users ADD COLUMN IF NOT EXISTS accountid uuid;
                UPDATE security.users
                SET accountid = '00000000-0000-0000-0000-000000000000'
                WHERE accountid IS NULL;
                ALTER TABLE security.users ALTER COLUMN accountid SET NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE security.users DROP COLUMN IF EXISTS accountid;");
        }
    }
}
