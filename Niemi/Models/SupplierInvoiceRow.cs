namespace Niemi.Models
{
    public class SupplierInvoiceRow
    {
        public int Id { get; set; }
        public int Account { get; set; }
        public string? AccountDescription { get; set; }
        public string? ArticleNumber { get; set; }
        public string? Code { get; set; }
        public string? CostCenter { get; set; }
        public decimal Credit { get; set; }
        public decimal CreditCurrency { get; set; }
        public decimal Debit { get; set; }
        public decimal DebitCurrency { get; set; }
        public string? ItemDescription { get; set; }
        public decimal Price { get; set; }
        public string? Project { get; set; }
        public decimal Quantity { get; set; }
        public string? StockLocationCode { get; set; }
        public string? StockPointCode { get; set; }
        public decimal Total { get; set; }
        public string? TransactionInformation { get; set; }
        public string? Unit { get; set; }

        public int SupplierInvoiceId { get; set; }
        public SupplierInvoice SupplierInvoice { get; set; } = null!;
    }
} 