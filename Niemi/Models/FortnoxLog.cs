namespace Niemi.Models;

public class FortnoxLog
{
    public int Id { get; set; }                          // FORTNOX_LOG.ID
    public DateTime? TimeStamp { get; set; }             // FORTNOX_LOG.TIME_STAMP
    public int? TransactionNo { get; set; }              // FORTNOX_LOG.TRANSACTION_NO
    public string? Description { get; set; }             // FORTNOX_LOG.DESCRIPTION
    public string? ErrorCode { get; set; }               // FORTNOX_LOG.ERROR_CODE
    public string? ErrorMessage { get; set; }            // FORTNOX_LOG.ERROR_MESSAGE
    public int? LogType { get; set; }                    // FORTNOX_LOG.LOG_TYPE
    public string? KeyNo { get; set; }                   // FORTNOX_LOG.KEY_NO
}
