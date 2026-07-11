using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackHub.AuthorityServer.Infrastructure.Migrations.SecurityDb
{
    /// <inheritdoc />
    public partial class SyncLockedUntilProjection : Migration
    {
        // Snapshot-sync only (no DDL): the physical security.users.lockeduntil column already
        // exists — it was added by TrackHubSecurity migration 20260707034921_AddSecurityLockoutAndUniqueIndexes.
        // Per SVD-04, this context is a read-only projection of TrackHubSecurity's tables and
        // Security owns the DDL for them; emitting AddColumn here could fail or double-apply
        // against the shared physical database.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
