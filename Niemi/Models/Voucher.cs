namespace Niemi.Models
{
    public class Voucher
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string? ReferenceType { get; set; }
        public string? Series { get; set; }
        public int Year { get; set; }
        
        public int SupplierInvoiceId { get; set; }
        public SupplierInvoice SupplierInvoice { get; set; } = null!;
    }
} 