namespace FreightSolution.Models.Statistics.DTO
{
    public class Lane
    {
        public int? SenderCountryId { get; set; }
        public int? ReceiverCountryId { get; set; }
        public string SenderCountry { get; set; }
        public string SenderCountryISO { get; set; }
        public string ReceiverCountry { get; set; }
        public string ReceiverCountryISO { get; set; }
        public string LaneString
        {
            get => SenderCountryId + ":" + ReceiverCountryId;
        }
    }
}