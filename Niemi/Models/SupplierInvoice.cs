namespace Niemi.Models
{
    public class SupplierInvoice
    {
        public int Id { get; set; }
        public string? Url { get; set; }
        public string? AccountingMethod { get; set; }
        public decimal? AdministrationFee { get; set; }
        public decimal Balance { get; set; }
        public bool Booked { get; set; }
        public bool Cancelled { get; set; }
        public string? Comments { get; set; }
        public string? CostCenter { get; set; }
        public bool Credit { get; set; }
        public int? CreditReference { get; set; }
        public string? Currency { get; set; }
        public decimal? CurrencyRate { get; set; }
        public int? CurrencyUnit { get; set; }
        public bool DisablePaymentFile { get; set; }
        public DateTime? DueDate { get; set; }
        public string? ExternalInvoiceNumber { get; set; }
        public string? ExternalInvoiceSeries { get; set; }
        public DateTime? FinalPayDate { get; set; }
        public decimal? Freight { get; set; }
        public string GivenNumber { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? OCR { get; set; }
        public string? OurReference { get; set; }
        public bool PaymentPending { get; set; }
        public string? Project { get; set; }
        public decimal? RoundOffValue { get; set; }
        public string? SalesType { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierNumber { get; set; }
        public decimal Total { get; set; }
        public decimal? VAT { get; set; }
        public string? VATType { get; set; }
        public int? VoucherNumber { get; set; }
        public string? VoucherSeries { get; set; }
        public int? VoucherYear { get; set; }
        public string? YourReference { get; set; }

        // Navigation properties
        public ICollection<SupplierInvoiceRow> SupplierInvoiceRows { get; set; } = new List<SupplierInvoiceRow>();
        public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
    }
} 