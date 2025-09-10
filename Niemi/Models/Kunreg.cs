namespace Niemi.Models;

public class Kunreg
{
    public int KunKunr { get; set; }                    // KUN_KUNR - Customer Number (Primary Key)
    public string? KunSok1 { get; set; }               // KUN_SOK1 - Search Field 1
    public string? KunSok2 { get; set; }               // KUN_SOK2 - Search Field 2
    public string? KunNamn { get; set; }               // KUN_NAMN - Customer Name
    public string? KunAdr1 { get; set; }               // KUN_ADR1 - Address 1
    public string? KunAdr2 { get; set; }               // KUN_ADR2 - Address 2
    public string? KunPadr { get; set; }               // KUN_PADR - Postal Address
    public string? KunOrgn { get; set; }               // KUN_ORGN - Organization Number
    public short? KunGrupp { get; set; }               // KUN_GRUPP - Group
    public string? KunRef1 { get; set; }               // KUN_REF1 - Reference 1
    public string? KunTel1 { get; set; }               // KUN_TEL1 - Phone 1
    public string? KunTfax { get; set; }               // KUN_TFAX - Fax
    public short KunKrti { get; set; }                 // KUN_KRTI - Credit Terms
    public double? KunFavg { get; set; }               // KUN_FAVG - Average
    public short? KunDist { get; set; }                // KUN_DIST - District
    public DateTime? KunRdat { get; set; }             // KUN_RDAT - Registration Date
    public short? KunPris { get; set; }                // KUN_PRIS - Price
    public short? KunTimdeb { get; set; }              // KUN_TIMDEB - Time Debit
    public short? KunRaba { get; set; }                // KUN_RABA - Discount
    public short? KunMomkod { get; set; }              // KUN_MOMKOD - VAT Code
    public string? KunRta { get; set; }                // KUN_RTA - Route
    public short? KunKrav { get; set; }                // KUN_KRAV - Claim
    public double? KunSaldo { get; set; }              // KUN_SALDO - Balance
    public double? KunLimit { get; set; }              // KUN_LIMIT - Credit Limit
    public string? KunJurpers { get; set; }            // KUN_JURPERS - Legal Person
    public string? KunKont { get; set; }               // KUN_KONT - Contact
    public string? KunTel2 { get; set; }               // KUN_TEL2 - Phone 2
    public string? KunTel3 { get; set; }               // KUN_TEL3 - Phone 3
    public string? KunEpostadress { get; set; }        // KUN_EPOSTADRESS - Email Address
    public double? KunRabarb { get; set; }             // KUN_RABARB - Discount Work
    public byte[]? KunInfo { get; set; }               // KUN_INFO - Info (Binary)
    public string? KunStopp { get; set; }              // KUN_STOPP - Stop Flag
    public string? KunValuta { get; set; }             // KUN_VALUTA - Currency
    public string? KunExport { get; set; }             // KUN_EXPORT - Export
    public string? KunEu { get; set; }                 // KUN_EU - EU
    public string? KunAvtal { get; set; }              // KUN_AVTAL - Agreement
    public int? KunBrandQuery { get; set; }            // KUN_BRAND_QUERY
    public int? KunCompanyQuery { get; set; }          // KUN_COMPANY_QUERY
    public DateTime? KunQueryAt { get; set; }          // KUN_QUERY_AT
    public int? KunBrandContacttype { get; set; }      // KUN_BRAND_CONTACTTYPE
    public DateTime? KunChangeAt { get; set; }         // KUN_CHANGE_AT
    public string? KunChangeBy { get; set; }           // KUN_CHANGE_BY
    public int? KunBrandUpload { get; set; }           // KUN_BRAND_UPLOAD
    public int? KunVatSw { get; set; }                 // KUN_VAT_SW
    public int? KunCabasSw { get; set; }               // KUN_CABAS_SW
    public int KunFleetpayer { get; set; }             // KUN_FLEETPAYER
    public int ReminderDisable { get; set; }           // REMINDER_DISABLE
    public string? KunFakmallnamn { get; set; }        // KUN_FAKMALLNAMN - Invoice Template Name
    public string? KunMomsnr { get; set; }             // KUN_MOMSNR - VAT Number
    public byte[]? CustomerInfo { get; set; }          // CUSTOMER_INFO - Customer Info (Binary)
    public int? KunGdprStatus { get; set; }            // KUN_GDPR_STATUS
    public DateTime? KunGdprAt { get; set; }           // KUN_GDPR_AT
    public string? InvoicePrinter { get; set; }        // INVOICE_PRINTER
    public string? KunGlnInvoice { get; set; }         // KUN_GLN_INVOICE
    public int? KunCountryCode { get; set; }           // KUN_COUNTRY_CODE

    // Monthly financial data (F101-M112, F201-M212) - these appear to be monthly financial summaries
    // F = Financial, M = Month, followed by year and month (e.g., F101 = Financial 2021-01, M101 = Month 2021-01)
    public double? KunF101 { get; set; }               // KUN_F101
    public double? KunM101 { get; set; }               // KUN_M101
    public double? KunF102 { get; set; }               // KUN_F102
    public double? KunM102 { get; set; }               // KUN_M102
    public double? KunF103 { get; set; }               // KUN_F103
    public double? KunM103 { get; set; }               // KUN_M103
    public double? KunF104 { get; set; }               // KUN_F104
    public double? KunM104 { get; set; }               // KUN_M104
    public double? KunF105 { get; set; }               // KUN_F105
    public double? KunM105 { get; set; }               // KUN_M105
    public double? KunF106 { get; set; }               // KUN_F106
    public double? KunM106 { get; set; }               // KUN_M106
    public double? KunF107 { get; set; }               // KUN_F107
    public double? KunF108 { get; set; }               // KUN_F108
    public double? KunM108 { get; set; }               // KUN_M108
    public double? KunF109 { get; set; }               // KUN_F109
    public double? KunM110 { get; set; }               // KUN_M110
    public double? KunF111 { get; set; }               // KUN_F111
    public double? KunM111 { get; set; }               // KUN_M111
    public double? KunF112 { get; set; }               // KUN_F112
    public double? KunM112 { get; set; }               // KUN_M112
    public double? KunF201 { get; set; }               // KUN_F201
    public double? KunM201 { get; set; }               // KUN_M201
    public double? KunF202 { get; set; }               // KUN_F202
    public double? KunM202 { get; set; }               // KUN_M202
    public double? KunF203 { get; set; }               // KUN_F203
    public double? KunM203 { get; set; }               // KUN_M203
    public double? KunF204 { get; set; }               // KUN_F204
    public double? KunM204 { get; set; }               // KUN_M204
    public double? KunF205 { get; set; }               // KUN_F205
    public double? KunM205 { get; set; }               // KUN_M205
    public double? KunF206 { get; set; }               // KUN_F206
    public double? KunM206 { get; set; }               // KUN_M206
    public double? KunF207 { get; set; }               // KUN_F207
    public double? KunM207 { get; set; }               // KUN_M207
    public double? KunF208 { get; set; }               // KUN_F208
    public double? KunF209 { get; set; }               // KUN_F209
    public double? KunF210 { get; set; }               // KUN_F210
    public double? KunM210 { get; set; }               // KUN_M210
    public double? KunF211 { get; set; }               // KUN_F211
    public double? KunM211 { get; set; }               // KUN_M211
    public double? KunF212 { get; set; }               // KUN_F212
    public double? KunM212 { get; set; }               // KUN_M212
}
