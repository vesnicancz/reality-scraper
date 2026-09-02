using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealityScraper.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeListingExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BaseScraperService.ExtractExternalId nově ořezává lomítka. Reality Idnes vrací odkazy
            // na detail zakončené lomítkem, takže uložená ID mají tvar "...ec65/" a nový parser by
            // je nespároval - všechny inzeráty by se v prvním běhu označily za vyřazené a hned
            // založily znovu jako nové, bez cenové historie.
            //
            // Unikátní index (ExternalId, ScraperTaskId) přepis zároveň hlídá: kdyby ořez vyrobil
            // kolizi, UPDATE spadne, transakce migrace se odroluje a aplikace nenastartuje. Není
            // tedy jak skončit s daty přepsanými napůl a novým parserem v provozu.
            migrationBuilder.Sql(@"
                UPDATE ""Listing""
                SET ""ExternalId"" = rtrim(""ExternalId"", '/')
                WHERE ""ExternalId"" LIKE '%/';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Zrcadlo, ne prázdno: rollback samotného kódu nad znormalizovanými daty je právě ta
            // kombinace, která párování rozhodí - starý parser by hledal "...ec65/". Z ořezaného ID
            // už nepoznáme, které lomítko mělo, takže se obnovuje podle portálu v Url.
            migrationBuilder.Sql(@"
                UPDATE ""Listing""
                SET ""ExternalId"" = ""ExternalId"" || '/'
                WHERE ""Url"" LIKE '%reality.idnes.cz%'
                  AND ""ExternalId"" NOT LIKE '%/';");
        }
    }
}
