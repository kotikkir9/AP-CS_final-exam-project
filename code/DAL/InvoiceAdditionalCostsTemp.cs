using System;

namespace DAL
{
    public class InvoiceAdditionalCostsTemp
    {
        // From Invoice table
        public int? InvoiceId { get; set; }
        public long BranchId { get; set; }
        public int? CarrierId { get; set; }
        
        // From InvoiceAdditionalCosts table
        public int? AdditionalCostId { get; set; }
        public decimal? AdditionalCostPrice { get; set; }
        
        // From InvoiceLines table
        public int InvoiceLineId { get; set; }
        public decimal? FreightPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public int? ServiceId { get; set; }
        public int ProductId { get; set; }
        public int? SenderCountryId { get; set; }
        public int? ReceiverCountryId { get; set; }
        public DateTime? ShipmentDate { get; set; }
        public string ShipmentIndicator { get; set; }
        public double? ShipmentActualWeight { get; set; }
        public double? ShipmentCBM { get; set; }
        public double? ShipmentLDM { get; set; }
        public double? ShipmentQuantity { get; set; }
    }
}