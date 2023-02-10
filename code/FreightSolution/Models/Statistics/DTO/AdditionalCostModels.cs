using System.Collections.Generic;

namespace FreightSolution.Models.Statistics.DTO
{
    // ==================== Models that client receive ====================
    public class AdditionalCostsByCarrierDto
    {
        public IList<AdditionalCostsByCarrier> Carriers { get; set; }
        public AdditionalCostData Total { get; set; } = new AdditionalCostData();
    }

    public class AdditionalCostsByLaneDto
    {
        public IList<AdditionalCostsByLane> Lanes { get; set; }
        public AdditionalCostData Total { get; set; }
    }
    
    public class AdditionalCostsByMonthDto
    {
        public IList<AdditionalCostsByMonthCarrierGroup> CarrierGroups { get; set; }
        public int FiscalYearStart { get; set; }
        public int Year { get; set; }
    }
    
    // ==================== Additional Costs By Carrier/Lane ====================
    public class AdditionalCostsCommon
    {
        public AdditionalCostData Total { get; set; }
        public AdditionalCostData Freight { get; set; }
        public List<AdditionalCostInfo> AdditionalCosts { get; set; }
    }
    
    public class AdditionalCostsByLane : AdditionalCostsCommon
    {
        public Lane Lane { get; set; }
    }
    
    public class AdditionalCostsByCarrier : AdditionalCostsCommon
    {
        public int? CarrierId { get; set; }
        public string Carrier { get; set; }
    }
    
    public class AdditionalCostInfo
    {
        public int AdditionalCostId { get; set; }
        public string AdditionalCost { get; set; }
        public string Href { get; set; }
        public AdditionalCostData Data { get; set; }
        public int? CarrierId { get; set; }
        public string Carrier { get; set; }
    }

    public class AdditionalCostData
    {
        public decimal? Price { get; set; }
        public int Count { get; set; }
    }
    
    // ==================== Additional Costs By Fiscal Year ====================
    
    public class AdditionalCostsByMonthCarrierGroup
    {
        public int? CarrierId { get; set; }
        public string Carrier { get; set; }
        
        public IDictionary<int, MonthGroup> FreightByMonth { get; set; } = new Dictionary<int, MonthGroup>();
        public IDictionary<int, MonthGroup> TotalByMonth { get; set; } = new Dictionary<int, MonthGroup>();
        public IList<AdditionalCostGroup> AdditionalCosts { get; set; } = new List<AdditionalCostGroup>();
    }

    public class AdditionalCostGroup
    {
        public AdditionalCostGroup()
        {
            StatsByMonth = new Dictionary<int, MonthGroup>();
        }
        public AdditionalCostGroup(string description) : this()
        {
            Description = description;
        }

        public string Description { get; set; }
        public IDictionary<int, MonthGroup> StatsByMonth { get; set; }
    }

    public class MonthGroup
    {
        public decimal? Price { get; set; }
        public int Count { get; set; }
    }
}