using BLL;
using DAL;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace FreightSolution.Models.Statistics
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class StatisticsFilterModel
    {
        public int[] Carriers { get; set; }
        public int[] Services { get; set; }
        public int[] Products { get; set; }
        public int[] SenderCountries { get; set; }
        public int[] ReceiverCountries { get; set; }
        public string[] ShipmentIndicators { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public bool IncludeZero { get; set; }
        public bool IncludeRemaining { get; set; }
        private double? _min;
        public double? Min
        {
            get => Math.Round(_min ?? 0, 1);
            set => _min = (value == null || value < 0) ? 0 : value;
        }

        private double? _max;
        public double? Max
        {
            get => _max == null ? _max : Math.Round(_max ?? 0, 1);
            set => _max = (value <= 0 || value <= _min) ? null : value;
        }

        private double? _intervalSize;

        public double? IntervalSize
        {
            get => _intervalSize;
            set => _intervalSize = (value == null || value <= 0) ? 1 : value;
        }
        
        public string Type { get; set; }

        public int? Year { get; set; }
        public int Layout { get; set; }
    }

    public class StatisticsModel
    {
        public StatisticsFilterModel Filter { get; set; }

        public IEnumerable<ComboItem<int>> Carriers { get; set; }
        public IEnumerable<ComboItem<int>> Services { get; set; }
        public IEnumerable<ComboItem<int>> Products { get; set; }
        public IEnumerable<ComboItem<int>> SenderCountries { get; set; }
        public IEnumerable<ComboItem<int>> ReceiverCountries { get; set; }
        public IEnumerable<ComboItem<string>> ShipmentIndicators { get; set; }
        public IEnumerable<ComboItem<int>> Years { get; set; }
        public bool HasStartEnd
        {
            get { return Filter.Layout == Defs.StatisticsLayout_Overall; }
        }

        public bool HasYearsDropdown
        {
            //get { return Filter.Layout == Defs.StatisticsLayout_ByMonth; }
            get => true;
        }
    }
}

