namespace Niemi.Services;

using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Niemi.Models;

public class LaginkService : ILaginkService
{
    private readonly string _connectionString;
    private readonly ILogger<LaginkService> _logger;

    public LaginkService(IConfiguration configuration, ILogger<LaginkService> logger)
    {
        _connectionString = configuration.GetConnectionString("FirebirdConnection") 
            ?? throw new ArgumentNullException("FirebirdConnection string is missing");
        _logger = logger;
    }

    public async Task<IEnumerable<LaginkHd>> GetLaginkDataAsync(DateTime fromDate, DateTime toDate, int skip, int take)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<LaginkHd>();

        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand(@"
                SELECT FIRST @take SKIP @skip
                    h.ORDERNR,
                    h.ORDERDATUM,
                    h.LEV,
                    h.INLEVDATUM,
                    h.SUMMA,
                    h.ORDERTYP,
                    h.LEVREF,
                    h.KUNDREF,
                    h.INLEV,
                    h.BEST,
                    h.CORRELATIONID,
                    h.EORDERID,
                    h.DELIVERYCODE,
                    h.LAGINK_CREATED_BY,
                    h.LAGINK_CREATED_AT,
                    h.LAGINK_UPDATED_BY,
                    h.LAGINK_UPDATED_AT,
                    r.RADNR,
                    r.ARTNR,
                    r.BEN,
                    r.ANTAL,
                    r.PRIS,
                    r.LEV as ROW_LEV,
                    r.RAD,
                    r.REST,
                    r.RADREF,
                    r.INLEV as ROW_INLEV,
                    r.BEST as ROW_BEST,
                    r.BESTFIL,
                    r.LEVERERAT,
                    r.LP,
                    r.BESTNR,
                    r.SUMMA as ROW_SUMMA,
                    r.STATUS,
                    r.ORDRADNR,
                    r.TYP,
                    r.ITEM_EXTERNAL_ID,
                    r.ORIGIN,
                    r.LAGINKRD_CREATED_BY,
                    r.LAGINKRD_CREATED_AT,
                    r.LAGINKRD_UPDATED_BY,
                    r.LAGINKRD_UPDATED_AT
                FROM LAGINKHD h
                LEFT JOIN LAGINKRD r ON h.ORDERNR = r.ORDERNR
                WHERE h.INLEVDATUM >= @fromDate AND h.INLEVDATUM <= @toDate
                ORDER BY h.ORDERNR, r.RADNR", connection);

            var skipParam = command.Parameters.Add("@skip", FbDbType.Integer);
            skipParam.Value = skip;
            
            var takeParam = command.Parameters.Add("@take", FbDbType.Integer);
            takeParam.Value = take;
            
            command.Parameters.AddWithValue("@fromDate", fromDate);
            command.Parameters.AddWithValue("@toDate", toDate);

            _logger.LogInformation(
                "Executing query with skip: {Skip}, take: {Take}", 
                skip, take);

            LaginkHd? currentHeader = null;
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var orderNr = reader.GetInt64(0);
                
                if (currentHeader == null || currentHeader.OrderNr != orderNr)
                {
                    currentHeader = new LaginkHd 
                    { 
                        OrderNr = orderNr,
                        OrderDatum = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
                        Lev = reader.IsDBNull(2) ? null : reader.GetString(2),
                        InlevDatum = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                        Summa = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                        OrderTyp = reader.IsDBNull(5) ? null : reader.GetString(5),
                        LevRef = reader.IsDBNull(6) ? null : reader.GetString(6),
                        KundRef = reader.IsDBNull(7) ? null : reader.GetString(7),
                        Inlev = reader.IsDBNull(8) ? null : reader.GetString(8),
                        Best = reader.IsDBNull(9) ? null : reader.GetString(9),
                        CorrelationId = reader.IsDBNull(10) ? null : reader.GetString(10),
                        EOrderId = reader.IsDBNull(11) ? 0 : reader.GetInt64(11),
                        DeliveryCode = reader.IsDBNull(12) ? 0 : reader.GetInt64(12),
                        LaginkCreatedBy = reader.IsDBNull(13) ? null : reader.GetString(13),
                        LaginkCreatedAt = reader.IsDBNull(14) ? DateTime.MinValue : reader.GetDateTime(14),
                        LaginkUpdatedBy = reader.IsDBNull(15) ? null : reader.GetString(15),
                        LaginkUpdatedAt = reader.IsDBNull(16) ? DateTime.MinValue : reader.GetDateTime(16),
                        Rows = new List<LaginkRd>()
                    };
                    results.Add(currentHeader);
                }

                if (!reader.IsDBNull(17)) // If we have row data (RADNR)
                {
                    currentHeader.Rows.Add(new LaginkRd
                    {
                        OrderNr = orderNr,
                        RadNr = reader.GetInt64(17),
                        ArtNr = reader.IsDBNull(18) ? null : reader.GetString(18),
                        Ben = reader.IsDBNull(19) ? null : reader.GetString(19),
                        Antal = reader.IsDBNull(20) ? 0 : reader.GetInt64(20),
                        Pris = reader.IsDBNull(21) ? 0 : reader.GetDecimal(21),
                        Lev = reader.IsDBNull(22) ? null : reader.GetString(22),
                        Rad = reader.IsDBNull(23) ? 0 : reader.GetDecimal(23),
                        Rest = reader.IsDBNull(24) ? 0 : reader.GetInt64(24),
                        RadRef = reader.IsDBNull(25) ? null : reader.GetString(25),
                        Inlev = reader.IsDBNull(26) ? null : reader.GetString(26),
                        Best = reader.IsDBNull(27) ? null : reader.GetString(27),
                        BestFil = reader.IsDBNull(28) ? null : reader.GetString(28),
                        Levererat = reader.IsDBNull(29) ? 0 : reader.GetInt64(29),
                        Lp = reader.IsDBNull(30) ? null : reader.GetString(30),
                        BestNr = reader.IsDBNull(31) ? null : reader.GetString(31),
                        Summa = reader.IsDBNull(32) ? 0 : reader.GetDecimal(32),
                        Status = reader.IsDBNull(33) ? 0 : reader.GetDecimal(33),
                        OrdRadNr = reader.IsDBNull(34) ? 0 : reader.GetInt64(34),
                        Typ = reader.IsDBNull(35) ? null : reader.GetString(35),
                        ItemExternalId = reader.IsDBNull(36) ? 0 : reader.GetInt64(36),
                        Origin = reader.IsDBNull(37) ? 0 : reader.GetDecimal(37),
                        LaginkrdCreatedBy = reader.IsDBNull(38) ? null : reader.GetString(38),
                        LaginkrdCreatedAt = reader.IsDBNull(39) ? DateTime.MinValue : reader.GetDateTime(39),
                        LaginkrdUpdatedBy = reader.IsDBNull(40) ? null : reader.GetString(40),
                        LaginkrdUpdatedAt = reader.IsDBNull(41) ? DateTime.MinValue : reader.GetDateTime(41)
                    });
                }
            }

            sw.Stop();
            _logger.LogInformation(
                "Query completed in {ElapsedMs}ms. Found {HeaderCount} headers with their details", 
                sw.ElapsedMilliseconds, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }
} 