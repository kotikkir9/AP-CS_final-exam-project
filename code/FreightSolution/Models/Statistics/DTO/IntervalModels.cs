using System.Collections.Generic;
using FreightSolution.Models.Statistics.DTO;
using Newtonsoft.Json;

namespace FreightSolution.Models.Statistics.DTO
{
    // ==================== Models that client receive ====================
    public class IntervalOverallDto
    {
        public IntervalData Total { get; set; }
        public IList<IntervalGroup> Intervals { get; set; }
    }

    public class IntervalByCarrierDto
    {
        public IntervalData Total { get; set; }
        public IList<IntervalByCarrierGroup> Carriers { get; set; }
    }

    public class IntervalMatrixDto
    {
        public IList<Lane> Lanes { get; set; }
        public IList<IntervalWithLanes> Intervals { get; set; }
        public IntervalData Total { get; set; }
        public IntervalData Min { get; set; }
        public IntervalData Max { get; set; }
    }

    // ==================== General ====================
    public class IntervalData
    {
        public int Shipments { get; set; }
        public double? Qty { get; set; }
        public double? Kg { get; set; }
        public double? Cbm  { get; set; }
        public double? Ldm { get; set; }
        public decimal? FreightPrice { get; set; }
        public decimal? TotalPrice { get; set; }
    }
    
    // ==================== Matrix by interval and lane ====================
    public class IntervalWithLanes
    {
        public double? StartInterval { get; set; }
        public double? EndInterval { get; set; }
        public IDictionary<string, IntervalData> Data { get; set; }
    }
    
    // ==================== Intervals by carrier ====================
    public class IntervalByCarrierGroup
    {
        public int? CarrierId { get; set; }
        public string Carrier { get; set; }
        public IntervalData Total { get; set; }
        public IList<IntervalGroup> Intervals { get; set; }
    }
    
    // ==================== Dynamic Linq Mapping Objects ==================== 
    // Flat models for working with Dynamic Linq, because composition doesn't work well with Dynamic Linq
    public class IntervalGroup : IntervalData
    {
        public double? StartInterval { get; set; }
        public double? EndInterval { get; set; }
        
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public int? CarrierId { get; set; }
        
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public Lane Lane { get; set; }
    }

    public class IntervalGroupByLane : IntervalGroup
    {
        public int? SenderCountryId { get; set; }
        public int? ReceiverCountryId { get; set; }
    }
}