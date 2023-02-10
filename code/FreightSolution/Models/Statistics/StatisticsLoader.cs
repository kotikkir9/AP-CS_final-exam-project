using System;
using BLL;
using DAL;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using FreightSolution.Models.Statistics.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using MoreLinq.Extensions;

namespace FreightSolution.Models.Statistics
{
    public class StatisticsLoader
    {
        private readonly StatisticsFilterModel _filter;
        private readonly IUserDataModel _userData;
        private readonly int _userId;
        private readonly IFreightSolutionDBEntities _database;
        private readonly IDatabaseCache _databaseCache;

        public int LanguageId { get; set; }
        public int FiscalYearStart { get; set; }

        public StatisticsLoader(IFreightSolutionDBEntities database, IDatabaseCache databaseCache,
            StatisticsFilterModel filter, IUserDataModel userData, int userId)
        {
            _database = database;
            _databaseCache = databaseCache;
            _filter = filter;
            _userData = userData;
            _userId = userId;

            LanguageId = databaseCache.LanguageIdEnglish;
            FiscalYearStart = _userData.SelectedOrUpperCompany?.FiscalYearStart ?? 0;
        }

        #region Statistics Interval

        public async Task<IntervalOverallDto> CreateIntervalOverallModelAsync(string target, CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);
            var model = new IntervalOverallDto();

            var predicate = $"{target} > @0" + GetIntervalFilterString(target);
            model.Intervals = await filteredQuery
                .AsNoTracking()
                .Where(predicate, _filter.Min)
                .Select($"new(" +
                        $"({target} == null || {target} == 0) ? 0" +
                        $" : ({target} > @0) ? null : (Math.Ceiling({target} / @1 ?? 0) * @1) as EndInterval," +
                        "InvoiceLineId," +
                        "ShipmentQuantity," +
                        "ShipmentActualWeight," +
                        "ShipmentLDM," +
                        "ShipmentCBM," +
                        "FreightPrice," +
                        "TotalPrice)", _filter.Max, _filter.IntervalSize)
                .Distinct()
                .GroupBy("EndInterval")
                .Select<IntervalGroup>(
                    "new(" +
                    "((Key - @0) < @1 ? @1 : Key == null ? @2 : Key - @0) as StartInterval," +
                    "(Key == null || Key < @2 ? Key : @2) as EndInterval," +
                    "Count() as Shipments," +
                    "Sum(ShipmentQuantity) as Qty," +
                    "Sum(ShipmentActualWeight) as Kg," +
                    "Sum(ShipmentLDM) as Ldm," +
                    "Sum(ShipmentCBM) as Cbm," +
                    "Sum(TotalPrice) as TotalPrice," +
                    "sum(FreightPrice) as FreightPrice)", _filter.IntervalSize, _filter.Min, _filter.Max)
                .OrderBy(e => e.StartInterval).ThenBy(e => e.EndInterval)
                .ToListAsync(cancellationToken);

            model.Total = new IntervalData
            {
                Shipments = model.Intervals.Sum(e => e.Shipments),
                Qty = model.Intervals.Sum(e => e.Qty),
                Kg = model.Intervals.Sum(e => e.Kg),
                Ldm = model.Intervals.Sum(e => e.Ldm),
                Cbm = model.Intervals.Sum(e => e.Cbm),
                FreightPrice = model.Intervals.Sum(e => e.FreightPrice),
                TotalPrice = model.Intervals.Sum(e => e.TotalPrice)
            };

            return model;
        }

        public async Task<IntervalByCarrierDto> CreateIntervalByCarrierModelAsync(string target, CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);
            var model = new IntervalByCarrierDto();

            var predicate = $"{target} > @0" + GetIntervalFilterString(target);
            var intervalsByCarrierAndTarget = await filteredQuery
                .AsNoTracking()
                .Where(predicate, _filter.Min)
                .Select($"new(" +
                        $"({target} == null || {target} == 0) ? 0" +
                        $" : ({target} > @0) ? null : (Math.Ceiling({target} / @1 ?? 0) * @1) as EndInterval," +
                        "CarrierId," +
                        "InvoiceLineId," +
                        "ShipmentQuantity," +
                        "ShipmentActualWeight," +
                        "ShipmentLDM," +
                        "ShipmentCBM," +
                        "FreightPrice," +
                        "TotalPrice)", _filter.Max, _filter.IntervalSize)
                .Distinct()
                .GroupBy("new (EndInterval, CarrierId)")
                .Select<IntervalGroup>(
                    "new(" +
                    "((Key.EndInterval - @0) < @1 ? @1 : Key.EndInterval == null ? @2 : Key.EndInterval - @0) as StartInterval," +
                    "(Key.EndInterval == null || Key.EndInterval < @2 ? Key.EndInterval : @2) as EndInterval," +
                    "Key.CarrierId as CarrierId," +
                    "Count() as Shipments," +
                    "Sum(ShipmentQuantity) as Qty," +
                    "Sum(ShipmentActualWeight) as Kg," +
                    "Sum(ShipmentLDM) as Ldm," +
                    "Sum(ShipmentCBM) as Cbm," +
                    "Sum(TotalPrice) as TotalPrice," +
                    "sum(FreightPrice) as FreightPrice)", _filter.IntervalSize, _filter.Min, _filter.Max)
                .OrderBy(e => e.StartInterval).ThenBy(e => e.EndInterval)
                .ToListAsync(cancellationToken);

            model.Carriers = intervalsByCarrierAndTarget
                .GroupBy(e => e.CarrierId)
                .Select(g => new IntervalByCarrierGroup
                {
                    CarrierId = g.Key,
                    Carrier = _databaseCache.GetTranslatedCarrierById(g.Key ?? 0, LanguageId)?.Name,
                    Total = new IntervalData
                    {
                        Shipments = g.Sum(i => i.Shipments),
                        Qty = g.Sum(i => i.Qty),
                        Kg = g.Sum(i => i.Kg),
                        Cbm = g.Sum(i => i.Cbm),
                        Ldm = g.Sum(i => i.Ldm),
                        FreightPrice = g.Sum(i => i.FreightPrice),
                        TotalPrice = g.Sum(i => i.TotalPrice)
                    },
                    Intervals = g.ToList()
                })
                .OrderBy(e => e.Carrier)
                .ToList();

            model.Total = new IntervalData
            {
                Shipments = model.Carriers.Sum(e => e.Total.Shipments),
                Qty = model.Carriers.Sum(e => e.Total.Qty),
                Kg = model.Carriers.Sum(e => e.Total.Kg),
                Ldm = model.Carriers.Sum(e => e.Total.Ldm),
                Cbm = model.Carriers.Sum(e => e.Total.Cbm),
                FreightPrice = model.Carriers.Sum(e => e.Total.FreightPrice),
                TotalPrice = model.Carriers.Sum(e => e.Total.TotalPrice)
            };

            return model;
        }
        
        public async Task<IntervalOverallDto> CreateLaneIntervalOverallModelAsync(CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);
            var model = new IntervalOverallDto();

            model.Intervals = await filteredQuery
                .AsNoTracking()
                .Where(e => e.SenderCountryId != null && e.ReceiverCountryId != null)
                .Select(e => new
                {
                    e.InvoiceLineId,
                    e.SenderCountryId,
                    e.ReceiverCountryId,
                    e.TotalPrice,
                    e.FreightPrice,
                    e.ShipmentActualWeight,
                    e.ShipmentQuantity,
                    e.ShipmentLDM,
                    e.ShipmentCBM
                })
                .Distinct()
                .GroupBy(e => new { e.SenderCountryId, e.ReceiverCountryId })
                .Select(e => new IntervalGroup
                {
                    Lane = new Lane
                    {
                        SenderCountryId = e.Key.SenderCountryId,
                        ReceiverCountryId = e.Key.ReceiverCountryId,
                        SenderCountryISO = _databaseCache.GetTranslatedCountryById(e.Key.SenderCountryId ?? 0, LanguageId).Instance.ISOcode,
                        ReceiverCountryISO = _databaseCache.GetTranslatedCountryById(e.Key.ReceiverCountryId ?? 0, LanguageId).Instance.ISOcode,
                    },
                    TotalPrice = e.Sum(g => g.TotalPrice),
                    FreightPrice = e.Sum(g => g.FreightPrice),
                    Shipments = e.Count(),
                    Kg = e.Sum(g => g.ShipmentActualWeight),
                    Qty = e.Sum(g => g.ShipmentQuantity),
                    Ldm = e.Sum(g => g.ShipmentLDM),
                    Cbm = e.Sum(g => g.ShipmentCBM)
                })
                .ToListAsync(cancellationToken);

            model.Intervals = model.Intervals
                .OrderBy(e => e.Lane.SenderCountryISO)
                .ThenBy(e => e.Lane.ReceiverCountryISO).ToList();
            
            model.Total = new IntervalData
            {
                TotalPrice = model.Intervals.Sum(e => e.TotalPrice),
                FreightPrice = model.Intervals.Sum(e => e.FreightPrice),
                Shipments = model.Intervals.Sum(e => e.Shipments),
                Kg = model.Intervals.Sum(e => e.Kg),
                Qty = model.Intervals.Sum(e => e.Qty),
                Ldm = model.Intervals.Sum(e => e.Ldm),
                Cbm = model.Intervals.Sum(e => e.Cbm),
            };

            return model;
        }
        
        public async Task<IntervalByCarrierDto> CreateLaneIntervalByCarrierModelAsync(CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);
            var model = new IntervalByCarrierDto();

            var lanesByCarrier = await filteredQuery
                .AsNoTracking()
                .Where(e => e.SenderCountryId != null && e.ReceiverCountryId != null)
                .Select(e => new
                {
                    e.InvoiceLineId,
                    e.CarrierId,
                    e.SenderCountryId,
                    e.ReceiverCountryId,
                    e.TotalPrice,
                    e.FreightPrice,
                    e.ShipmentActualWeight,
                    e.ShipmentQuantity,
                    e.ShipmentLDM,
                    e.ShipmentCBM
                })
                .Distinct()
                .GroupBy(e => new { e.SenderCountryId, e.ReceiverCountryId, e.CarrierId })
                .Select(e => new IntervalGroup
                {
                    CarrierId = e.Key.CarrierId,
                    Lane = new Lane
                    {
                        SenderCountryId = e.Key.SenderCountryId,
                        ReceiverCountryId = e.Key.ReceiverCountryId,
                        SenderCountryISO = _databaseCache.GetTranslatedCountryById(e.Key.SenderCountryId ?? 0, LanguageId).Instance.ISOcode,
                        ReceiverCountryISO = _databaseCache.GetTranslatedCountryById(e.Key.ReceiverCountryId ?? 0, LanguageId).Instance.ISOcode,
                    },
                    TotalPrice = e.Sum(g => g.TotalPrice),
                    FreightPrice = e.Sum(g => g.FreightPrice),
                    Shipments = e.Count(),
                    Kg = e.Sum(g => g.ShipmentActualWeight),
                    Qty = e.Sum(g => g.ShipmentQuantity),
                    Ldm = e.Sum(g => g.ShipmentLDM),
                    Cbm = e.Sum(g => g.ShipmentCBM)
                })
                .ToListAsync(cancellationToken);
            
            model.Carriers = lanesByCarrier
                .GroupBy(e => e.CarrierId)
                .Select(g => new IntervalByCarrierGroup
                {
                    CarrierId = g.Key,
                    Carrier = _databaseCache.GetTranslatedCarrierById(g.Key ?? 0, LanguageId)?.Name,
                    Total = new IntervalData
                    {
                        Shipments = g.Sum(i => i.Shipments),
                        Qty = g.Sum(i => i.Qty),
                        Kg = g.Sum(i => i.Kg),
                        Cbm = g.Sum(i => i.Cbm),
                        Ldm = g.Sum(i => i.Ldm),
                        FreightPrice = g.Sum(i => i.FreightPrice),
                        TotalPrice = g.Sum(i => i.TotalPrice)
                    },
                    Intervals = g
                        .OrderBy(e => e.Lane.SenderCountryISO)
                        .ThenBy(e => e.Lane.ReceiverCountryISO)
                        .ToList(),
                })
                .OrderBy(e => e.Carrier)
                .ToList();

            model.Total = new IntervalData
            {
                Shipments = model.Carriers.Sum(e => e.Total.Shipments),
                Qty = model.Carriers.Sum(e => e.Total.Qty),
                Kg = model.Carriers.Sum(e => e.Total.Kg),
                Ldm = model.Carriers.Sum(e => e.Total.Ldm),
                Cbm = model.Carriers.Sum(e => e.Total.Cbm),
                FreightPrice = model.Carriers.Sum(e => e.Total.FreightPrice),
                TotalPrice = model.Carriers.Sum(e => e.Total.TotalPrice)
            };
            
            return model;
        }
        
        public async Task<IntervalMatrixDto> CreateMatrixModelAsync(string target, CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);
            var model = new IntervalMatrixDto();

            var predicate = $"{target} > @0" + GetIntervalFilterString(target);
            var intervalsByKgAndLane = await filteredQuery
                .AsNoTracking()
                .Where(predicate, _filter.Min)
                .Select($"new(" +
                    $"({target} == null || {target} == 0) ? 0" +
                    $" : ({target} > @0) ? null : (Math.Ceiling({target} / @1 ?? 0) * @1) as EndInterval," +
                    "SenderCountryId," +
                    "ReceiverCountryId," +
                    "InvoiceLineId," +
                    "ShipmentQuantity," +
                    "ShipmentActualWeight," +
                    "ShipmentLDM," +
                    "ShipmentCBM," +
                    "FreightPrice," +
                    "TotalPrice)", _filter.Max, _filter.IntervalSize)
                .Distinct()
                .GroupBy("new (EndInterval, SenderCountryId, ReceiverCountryId)")
                .Select<IntervalGroupByLane>(
                    "new(" +
                    "((Key.EndInterval - @0) < @1 ? @1 : Key.EndInterval == null ? @2 : Key.EndInterval - @0) as StartInterval," +
                    "(Key.EndInterval == null || Key.EndInterval < @2 ? Key.EndInterval : @2) as EndInterval," +
                    "Key.SenderCountryId as SenderCountryId," +
                    "Key.ReceiverCountryId as ReceiverCountryId," +
                    "Count() as Shipments," +
                    "Sum(ShipmentQuantity) as Qty," +
                    "Sum(ShipmentActualWeight) as Kg," +
                    "Sum(ShipmentLDM) as Ldm," +
                    "Sum(ShipmentCBM) as Cbm," +
                    "Sum(TotalPrice) as TotalPrice," +
                    "sum(FreightPrice) as FreightPrice)", _filter.IntervalSize, _filter.Min, _filter.Max)
                .OrderBy(e => e.StartInterval).ThenBy(e => e.EndInterval)
                .ToListAsync(cancellationToken);

             model.Lanes = intervalsByKgAndLane
                .Select(e => new { e.SenderCountryId, e.ReceiverCountryId })
                .Distinct()
                .Select(e => new Lane
                {
                    SenderCountryId = e.SenderCountryId,
                    ReceiverCountryId = e.ReceiverCountryId,
                    SenderCountry = _databaseCache.GetTranslatedCountryById(e.SenderCountryId ?? 0, LanguageId)?.Name,
                    ReceiverCountry = _databaseCache.GetTranslatedCountryById(e.ReceiverCountryId ?? 0, LanguageId)?.Name,
                    SenderCountryISO = _databaseCache.GetTranslatedCountryById(e.SenderCountryId ?? 0, LanguageId)?.Instance?.ISOcode,
                    ReceiverCountryISO = _databaseCache.GetTranslatedCountryById(e.ReceiverCountryId ?? 0, LanguageId)?.Instance?.ISOcode,
                })
                .OrderBy(e => e.SenderCountry)
                .ThenBy(e => e.ReceiverCountry)
                .ToList();

             model.Intervals = intervalsByKgAndLane
                 .GroupBy(e => new { e.StartInterval, e.EndInterval })
                 .Select(e => new IntervalWithLanes
                 {
                     StartInterval = e.Key.StartInterval,
                     EndInterval = e.Key.EndInterval,
                     Data = e
                         .ToDictionary(key => key.SenderCountryId + ":" + key.ReceiverCountryId, val => new IntervalData
                         {
                             Shipments = val.Shipments,
                             Qty = val.Qty,
                             Kg = val.Kg,
                             Cbm = val.Cbm,
                             Ldm = val.Ldm,
                             FreightPrice = val.FreightPrice,
                             TotalPrice = val.TotalPrice
                         })
                 })
                 .ToList();

             model.Min = new IntervalData
             {
                 Shipments = intervalsByKgAndLane.Min(e => e.Shipments),
                 Qty = intervalsByKgAndLane.Min(e => e.Qty),
                 Kg = intervalsByKgAndLane.Min(e => e.Kg),
                 Ldm = intervalsByKgAndLane.Min(e => e.Ldm),
                 Cbm = intervalsByKgAndLane.Min(e => e.Cbm),
                 FreightPrice = intervalsByKgAndLane.Min(e => e.FreightPrice),
                 TotalPrice = intervalsByKgAndLane.Min(e => e.TotalPrice)
             };
             
             model.Max = new IntervalData
             {
                 Shipments = intervalsByKgAndLane.Max(e => e.Shipments),
                 Qty = intervalsByKgAndLane.Max(e => e.Qty),
                 Kg = intervalsByKgAndLane.Max(e => e.Kg),
                 Ldm = intervalsByKgAndLane.Max(e => e.Ldm),
                 Cbm = intervalsByKgAndLane.Max(e => e.Cbm),
                 FreightPrice = intervalsByKgAndLane.Max(e => e.FreightPrice),
                 TotalPrice = intervalsByKgAndLane.Max(e => e.TotalPrice)
             };

             model.Total = new IntervalData
             {
                Shipments = intervalsByKgAndLane.Sum(e => e.Shipments),
                Qty = intervalsByKgAndLane.Sum(e => e.Qty),
                Kg = intervalsByKgAndLane.Sum(e => e.Kg),
                Ldm = intervalsByKgAndLane.Sum(e => e.Ldm),
                Cbm = intervalsByKgAndLane.Sum(e => e.Cbm),
                FreightPrice = intervalsByKgAndLane.Sum(e => e.FreightPrice),
                TotalPrice = intervalsByKgAndLane.Sum(e => e.TotalPrice)
             };
            
            return model;
        }

        #endregion

        #region Statistics Additional Costs

        public async Task<AdditionalCostsByCarrierDto> LoadAdditionalCostsByCarrierAsync(IUrlHelper urlHelper, CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);
            var model = new AdditionalCostsByCarrierDto();
            
            // Get all carrier groups for given Branchid and total freight price for each carrier
            model.Carriers = await filteredQuery
                .Where(e => e.CarrierId != null)
                .Select(e => new
                {
                    e.FreightPrice,
                    e.CarrierId,
                    e.InvoiceLineId
                })
                .Distinct()
                .GroupBy(e => e.CarrierId)
                .Select(e => new AdditionalCostsByCarrier
                {
                    CarrierId = e.Key,
                    Carrier = _databaseCache.GetTranslatedCarrierById(e.Key ?? 0, LanguageId).Name,
                    AdditionalCosts = new List<AdditionalCostInfo>(),
                    Freight = new AdditionalCostData
                    {
                        Price = e.Sum(i => i.FreightPrice),
                        Count = e.Count(),
                    },
                    Total = new AdditionalCostData
                    {
                        Price = e.Sum(i => i.FreightPrice),
                        Count = e.Count(),
                    }
                })
                .ToListAsync(cancellationToken);
            
            model.Carriers = model.Carriers
                .OrderBy(e => e.Carrier, StringComparer.OrdinalIgnoreCase).ToList();

            // Get all additional cost prices from temp table
            var additionalCostPrices = await filteredQuery
                .Where(e => e.AdditionalCostId != null)
                .Select(e => new
                {
                    e.AdditionalCostPrice,
                    e.AdditionalCostId,
                    e.CarrierId
                })
                .GroupBy(e => new
                {
                    e.AdditionalCostId,
                    e.CarrierId
                })
                .Select(e => new AdditionalCostInfo
                {
                    AdditionalCostId = e.Key.AdditionalCostId ?? 0,
                    AdditionalCost = _databaseCache.GetTranslatedAdditionalCostById(e.Key.AdditionalCostId ?? 0, LanguageId).Name,
                    CarrierId = e.Key.CarrierId,
                    Data = new AdditionalCostData
                    {
                        Price = e.Sum(ac => ac.AdditionalCostPrice),
                        Count = e.Count(),
                    }
                })
                .ToListAsync(cancellationToken);

            additionalCostPrices = additionalCostPrices
                .OrderBy(e => e.AdditionalCost, StringComparer.OrdinalIgnoreCase).ToList();
            
            foreach (var e in additionalCostPrices)
            {
                var isFuelAdditionalCost = _databaseCache
                    .GetTranslatedAdditionalCostById(e.AdditionalCostId, _databaseCache.LanguageIdEnglish)
                    ?.Instance.FkAditionalCostGroupid == Defs.AdditionalCostGroup_Fuel;

                if (!isFuelAdditionalCost)
                {
                    e.Href = urlHelper.Action("ShipmentsOverview", "Invoice",
                        new { carrierId = e.CarrierId, additionalCostId = e.AdditionalCostId });
                }

                var carrier = model.Carriers.FirstOrDefault(c => c.CarrierId == e.CarrierId);
                if (carrier != null)
                {
                    carrier.AdditionalCosts.Add(e);
                    carrier.Total.Count += e.Data.Count;
                    carrier.Total.Price += e.Data.Price;
                }
            };
            
            model.Total = new AdditionalCostData
            {
                Price = model.Carriers.Sum(e => e.Total.Price),
                Count = model.Carriers.Sum(e => e.Total.Count)
            };
            
            return model;
        }
        
        public async Task<AdditionalCostsByLaneDto> LoadAdditionalCostsByLaneAsync(IUrlHelper urlHelper, CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);
            var model = new AdditionalCostsByLaneDto();
            
            // Get all unique lanes for given Branchid and total freight price for each lane
            model.Lanes = await filteredQuery
                .Where(e => e.SenderCountryId != null && e.ReceiverCountryId != null)
                .Select(e => new
                {
                    e.InvoiceLineId,
                    e.FreightPrice,
                    e.SenderCountryId,
                    e.ReceiverCountryId,
                })
                .Distinct()
                .GroupBy(e => new { e.SenderCountryId, e.ReceiverCountryId })
                .Select(e => new AdditionalCostsByLane
                {
                    AdditionalCosts = new List<AdditionalCostInfo>(),
                    Lane = new Lane
                    {
                        SenderCountryId = e.Key.SenderCountryId,
                        ReceiverCountryId = e.Key.ReceiverCountryId,
                        SenderCountryISO = _databaseCache.GetTranslatedCountryById(e.Key.SenderCountryId ?? 0, LanguageId).Instance.ISOcode,
                        ReceiverCountryISO = _databaseCache.GetTranslatedCountryById(e.Key.ReceiverCountryId ?? 0, LanguageId).Instance.ISOcode,
                    },
                    Freight = new AdditionalCostData
                    {
                        Price = e.Sum(i => i.FreightPrice),
                        Count = e.Count(),
                    },
                    Total = new AdditionalCostData
                    {
                        Price = e.Sum(i => i.FreightPrice),
                        Count = e.Count(),
                    }
                })
                .ToListAsync(cancellationToken);

            model.Lanes = model.Lanes
                .OrderBy(e => e.Lane.SenderCountryISO)
                .ThenBy(e => e.Lane.ReceiverCountryISO)
                .ToList();
            
            // Get all additional cost prices from temp table
            var additionalCostPrices = await filteredQuery
                .Where(e => e.AdditionalCostId != null)
                .Select(e => new
                {
                    e.SenderCountryId,
                    e.ReceiverCountryId,
                    e.AdditionalCostPrice,
                    e.AdditionalCostId,
                    e.CarrierId
                })
                .GroupBy(e => new
                {
                    e.SenderCountryId,
                    e.ReceiverCountryId,
                    e.AdditionalCostId,
                    e.CarrierId
                })
                .Select(e => new 
                {
                    e.Key.SenderCountryId,
                    e.Key.ReceiverCountryId,
                    AdditionalCostId = e.Key.AdditionalCostId,
                    AdditionalCost = _databaseCache.GetTranslatedAdditionalCostById(e.Key.AdditionalCostId ?? 0, LanguageId).Name,
                    CarrierId = e.Key.CarrierId,
                    Price = e.Sum(ac => ac.AdditionalCostPrice),
                    Count = e.Count()
                })
                .ToListAsync(cancellationToken);

            additionalCostPrices = additionalCostPrices
                .OrderBy(e => e.AdditionalCost, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach(var e in additionalCostPrices)
            {
                var additionalCost = new AdditionalCostInfo
                {
                    AdditionalCostId = e.AdditionalCostId ?? 0,
                    AdditionalCost = e.AdditionalCost,
                    Carrier = _databaseCache.GetTranslatedCarrierById(e.CarrierId ?? 0, LanguageId).Name,
                    Data = new AdditionalCostData
                    {
                        Price = e.Price,
                        Count = e.Count,
                    }
                };
                
                var isFuelAdditionalCost = _databaseCache
                    .GetTranslatedAdditionalCostById(e.AdditionalCostId ?? 0, _databaseCache.LanguageIdEnglish)
                    ?.Instance.FkAditionalCostGroupid == Defs.AdditionalCostGroup_Fuel;

                if (!isFuelAdditionalCost)
                {
                    additionalCost.Href = urlHelper.Action("ShipmentsOverview", "Invoice",
                        new { carrierId = e.CarrierId, additionalCostId = e.AdditionalCostId });
                }
                
                var lane = model.Lanes.FirstOrDefault(c => c.Lane.SenderCountryId == e.SenderCountryId && c.Lane.ReceiverCountryId == e.ReceiverCountryId);
                if (lane != null)
                {
                    lane.AdditionalCosts.Add(additionalCost);
                    lane.Total.Count += e.Count;
                    lane.Total.Price += e.Price;
                }
            };
            
            model.Total = new AdditionalCostData
            {
                Price = model.Lanes.Sum(e => e.Total.Price),
                Count = model.Lanes.Sum(e => e.Total.Count)
            };
            
            return model;
        }

        public async Task<AdditionalCostsByMonthDto> LoadCarrierByAdditionalCostByMonthAsync(CancellationToken cancellationToken)
        {
            var filteredQuery = GetFilteredStatisticsByBranch(_database.InvoiceAdditionalCostsTemp);

            var model = new AdditionalCostsByMonthDto()
            {
                CarrierGroups = new List<AdditionalCostsByMonthCarrierGroup>(),
                FiscalYearStart = FiscalYearStart,
                Year = _filter.Year ?? 0
            };

            var carriers = await filteredQuery
                .Where(e => e.CarrierId != null)
                .Select(e => new
                {
                    Month = e.ShipmentDate.Value.Month - 1,
                    e.FreightPrice,
                    e.CarrierId,
                    e.InvoiceLineId
                })
                .Distinct()
                .GroupBy(e => new { e.CarrierId, e.Month })
                .Select(e => new
                {
                    e.Key.CarrierId,
                    e.Key.Month,
                    Price = e.Sum(i => i.FreightPrice ?? 0),
                    Count = e.Count()
                })
                .ToListAsync(cancellationToken);

            foreach (var e in carriers.GroupBy(e => e.CarrierId))
            {
                model.CarrierGroups.Add(new AdditionalCostsByMonthCarrierGroup
                {
                    CarrierId = e.Key,
                    Carrier = _databaseCache.GetTranslatedCarrierById(e.Key ?? 0, LanguageId).Name,
                    FreightByMonth = e.ToDictionary(k => k.Month, v => new MonthGroup{ Price = v.Price, Count = v.Count }),
                    TotalByMonth = e.ToDictionary(k => k.Month, v => new MonthGroup{ Price = v.Price, Count = v.Count }),
                });
            };

            model.CarrierGroups = model.CarrierGroups.OrderBy(e => e.Carrier).ToList();

            // Getting all additional costs by ID, month, carrier, total price and count
            var additionalCostsByMonth = await filteredQuery
                .Where(e => e.AdditionalCostId != null)
                .Select(e => new
                {
                    Month = e.ShipmentDate.Value.Month - 1,
                    e.AdditionalCostId,
                    e.AdditionalCostPrice,
                    e.CarrierId
                })
                .GroupBy(e => new
                {
                    e.Month,
                    e.AdditionalCostId,
                    e.CarrierId
                })
                .Select(e => new
                {
                    e.Key.AdditionalCostId,
                    e.Key.CarrierId,
                    e.Key.Month,
                    Stats = new MonthGroup
                    {
                        Price = e.Sum(ac => ac.AdditionalCostPrice),
                        Count = e.Count(),
                    }
                })
                .ToListAsync(cancellationToken);

            // Merging additional costs statistics into the model with carriers
            foreach(var e in additionalCostsByMonth.GroupBy(e => e.CarrierId))
            {
                var carrierGroup = model.CarrierGroups.FirstOrDefault(carrier => carrier.CarrierId == e.Key);
                if (carrierGroup == null) continue; // Should not be possible

                carrierGroup.AdditionalCosts = e
                    .GroupBy(g => g.AdditionalCostId)
                    .Select(ac => new AdditionalCostGroup
                    {
                        Description = _databaseCache.GetTranslatedAdditionalCostById(ac.Key ?? 0, LanguageId)?.Name,
                        StatsByMonth = ac.ToDictionary(key => key.Month, val => val.Stats),
                    })
                    .OrderBy(ac => ac.Description, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var acGroup in carrierGroup.AdditionalCosts)
                {
                    foreach (var month in acGroup.StatsByMonth)
                    {
                        if (carrierGroup.TotalByMonth.TryGetValue(month.Key, out var stats))
                        {
                            stats.Count += month.Value.Count;
                            stats.Price += month.Value.Price;
                        }
                    }
                }
            };

            return model;
        }

        #endregion

        #region Utility Methods
        private IQueryable<InvoiceAdditionalCostsTemp> GetFilteredStatisticsByBranch(
            IQueryable<InvoiceAdditionalCostsTemp> query)
        {
            var branchIds = GetBranchIds();
            query = query.Where(ac => branchIds.Contains(ac.BranchId));

            if (_filter != null)
            {
                if (_filter.Carriers != null && _filter.Carriers.Length > 0)
                {
                    query = query.Where(ac => _filter.Carriers.Contains(ac.CarrierId ?? 0));
                }

                if (_filter.Services != null && _filter.Services.Length > 0)
                {
                    query = query.Where(ac => ac.ServiceId != null && _filter.Services.Contains(ac.ServiceId ?? 0));
                }

                if (_filter.Products != null && _filter.Products.Length > 0)
                {
                    query = query.Where(ac => _filter.Products.Contains(ac.ProductId));
                }

                if (_filter.SenderCountries != null && _filter.SenderCountries.Length > 0)
                {
                    query = query.Where(ac =>
                        ac.SenderCountryId != null && _filter.SenderCountries.Contains(ac.SenderCountryId ?? 0));
                }

                if (_filter.ReceiverCountries != null && _filter.ReceiverCountries.Length > 0)
                {
                    query = query.Where(ac =>
                        ac.ReceiverCountryId != null && _filter.ReceiverCountries.Contains(ac.ReceiverCountryId ?? 0));
                }

                if (_filter.ShipmentIndicators != null && _filter.ShipmentIndicators.Length > 0)
                {
                    query = query.Where(ac =>
                        ac.ShipmentIndicator != null && _filter.ShipmentIndicators.Contains(ac.ShipmentIndicator));
                }

                var fromDateTime = _filter.GetFromDateTime(FiscalYearStart);
                if (fromDateTime != null)
                {
                    query = query
                        .Where(ac => ac.ShipmentDate >= fromDateTime);
                }

                var toDateTime = _filter.GetToDateTime(FiscalYearStart);
                if (toDateTime != null)
                {
                    query = query
                        .Where(ac => ac.ShipmentDate < toDateTime);
                }
            }

            return query;
        }

        public string GetIntervalFilterString(string target)
        {
            var predicate = string.Empty;
            if (_filter.IncludeZero) predicate += $" || {target} == 0 || {target} == null";
            if (!_filter.IncludeRemaining) predicate += $" && {target} <= {_filter.Max}";

            return predicate;
        }
        
        
        public long[] GetBranchIds()
        {
            var branchIds = _userData.SelectedNodeIds.GetBranchIdsForNodeAndBelow(_databaseCache).ToArray();

            if (!branchIds.Any())
            {
                branchIds = _userData.SelectedNodeIds.GetBranchIdsForNodeAndBelow(_databaseCache,
                    _userData.SelectedCompanyId, _userData.SelectedOfficeId, _userData.SelectedDepartmentId).ToArray();
            }

            return branchIds;
        }
        #endregion
    }
}