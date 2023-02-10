using BLL;
using DAL;
using FreightSolution.Models.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using System.Threading;

namespace FreightSolution.Controllers
{
    [Authorize(Policy = Defs.Policy_NoCustomersAndTrials)]
    public class StatisticsController : ControllerBase
    {
        ILogger<StatisticsController> _Logger;
        IEncryptionService _encryptionService;
        private readonly IOptions<AzureSettings> _settings;

        public StatisticsController(IFreightSolutionDBEntities database, IDatabaseCache databaseHelpers, ILogger<StatisticsController> logger, INotificationsManager notificationsManager, IDownloadsManager downloadManager, IEncryptionService encryptionService, IOptions<AzureSettings> settings)
            : base(database, databaseHelpers, notificationsManager, downloadManager)
        {
            _Logger = logger;
            _encryptionService = encryptionService;
            _settings = settings;
        }

        // Statistics 2.0
        [HttpGet]
        public async Task<ViewResult> Index3()
        {
            var filterOptions = new StatisticsModel
            {
                Filter = new StatisticsFilterModel(),
            };

            await FillStatisticsFilter(filterOptions);
            return View(filterOptions);
        }

        [HttpPost]
        public async Task<JsonResult> WeightIntervals([FromBody]StatisticsFilterModel filter, CancellationToken cancellationToken)
        {
            _database.SetTimeout(new TimeSpan(0, 0, 5 * 60));
            filter.Max ??= 100;

            var loader = new StatisticsLoader(_database, _databaseCache, filter, UserData, UserId);
            const string target = nameof(InvoiceAdditionalCostsTemp.ShipmentActualWeight);

            object model = filter.Layout switch
            {
                Defs.StatisticsLayout_Overall => await loader.CreateIntervalOverallModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_ByCarrier => await loader.CreateIntervalByCarrierModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_Matrix => await loader.CreateMatrixModelAsync(target, cancellationToken),
                _ => null
            };

            return new JsonResult(new { result = model, filter.Layout, filter.Min, filter.Max });
        }
        
        [HttpPost]
        public async Task<JsonResult> QuantityIntervals([FromBody]StatisticsFilterModel filter, CancellationToken cancellationToken)
        {
            _database.SetTimeout(new TimeSpan(0, 0, 5 * 60));
            filter.Max ??= 35;

            var loader = new StatisticsLoader(_database, _databaseCache, filter, UserData, UserId);
            const string target = nameof(InvoiceAdditionalCostsTemp.ShipmentQuantity);

            object model = filter.Layout switch
            {
                Defs.StatisticsLayout_Overall => await loader.CreateIntervalOverallModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_ByCarrier => await loader.CreateIntervalByCarrierModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_Matrix => await loader.CreateMatrixModelAsync(target, cancellationToken),
                _ => null
            };

            return new JsonResult(new { result = model, filter.Layout, filter.Min, filter.Max });
        }
        
        [HttpPost]
        public async Task<JsonResult> LoadMeterIntervals([FromBody]StatisticsFilterModel filter, CancellationToken cancellationToken)
        {
            _database.SetTimeout(new TimeSpan(0, 0, 5 * 60));
            filter.Max ??= 13.6;

            var loader = new StatisticsLoader(_database, _databaseCache, filter, UserData, UserId);
            const string target = nameof(InvoiceAdditionalCostsTemp.ShipmentLDM);

            object model = filter.Layout switch
            {
                Defs.StatisticsLayout_Overall => await loader.CreateIntervalOverallModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_ByCarrier => await loader.CreateIntervalByCarrierModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_Matrix => await loader.CreateMatrixModelAsync(target, cancellationToken),
                _ => null
            };

            return new JsonResult(new { result = model, filter.Layout, filter.Min, filter.Max });
        }
        
        [HttpPost]
        public async Task<JsonResult> CubicMeterIntervals([FromBody]StatisticsFilterModel filter, CancellationToken cancellationToken)
        {
            _database.SetTimeout(new TimeSpan(0, 0, 5 * 60));
            filter.Max ??= 50;
            filter.IntervalSize ??= 5;

            var loader = new StatisticsLoader(_database, _databaseCache, filter, UserData, UserId);
            const string target = nameof(InvoiceAdditionalCostsTemp.ShipmentCBM);
            
            object model = filter.Layout switch
            {
                Defs.StatisticsLayout_Overall => await loader.CreateIntervalOverallModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_ByCarrier => await loader.CreateIntervalByCarrierModelAsync(target, cancellationToken),
                Defs.StatisticsLayout_Matrix => await loader.CreateMatrixModelAsync(target, cancellationToken),
                _ => null
            };

            return new JsonResult(new { result = model, filter.Layout, filter.Min, filter.Max });
        }

        [HttpPost]
        public async Task<JsonResult> LaneIntervals([FromBody] StatisticsFilterModel filter, CancellationToken cancellationToken)
        {
            _database.SetTimeout(new TimeSpan(0, 0, 5 * 60));
            var loader = new StatisticsLoader(_database, _databaseCache, filter, UserData, UserId);
            object model = filter.Layout switch
            {
                Defs.StatisticsLayout_Overall => await loader.CreateLaneIntervalOverallModelAsync(cancellationToken),
                Defs.StatisticsLayout_ByCarrier => await loader.CreateLaneIntervalByCarrierModelAsync(cancellationToken),
                _ => null
            };

            return new JsonResult(new { result = model, filter.Layout });
        }
        
        [HttpPost]
        public async Task<JsonResult> AdditionalCosts([FromBody]StatisticsFilterModel filter, CancellationToken cancellationToken)
        {
            _database.SetTimeout(new TimeSpan(0, 0, 5 * 60));
            var loader = new StatisticsLoader(_database, _databaseCache, filter, UserData, UserId);

            object model = filter.Layout switch
            {
                Defs.StatisticsLayout_ByCarrier => await loader.LoadAdditionalCostsByCarrierAsync(Url, cancellationToken),
                Defs.StatisticsLayout_ByLane => await loader.LoadAdditionalCostsByLaneAsync(Url, cancellationToken),
                Defs.StatisticsLayout_ByMonth => await loader.LoadCarrierByAdditionalCostByMonthAsync(cancellationToken),
                _ => null
            };

            return new JsonResult(new { result = model, layout = filter.Layout });
        }

        private async Task FillStatisticsFilter(StatisticsModel model)
        {
            var branchIds = new StatisticsLoader(_database, _databaseCache, model.Filter, UserData, UserId)
                .GetBranchIds();

            var filterData = await _database.InvoiceLines
                .AsNoTracking()
                .Where(il => branchIds.Contains(il.Branchid))
                .Select(il => new
                {
                    il.FkServiceid,
                    il.FkProductid,
                    il.FkSenderCountryid,
                    il.FkReceiverCountryid,
                    il.ShipmentIndicator
                })
                .Distinct()
                .ToListAsync();

            model.Carriers = filterData
                .Where(d => d.FkServiceid != null)
                .Select(d => _databaseCache.GetTranslatedCarrierServiceById(d.FkServiceid ?? 0, _databaseCache.LanguageIdEnglish)?.Instance?.FkCarrierid ?? 0)
                .Where(carrierId => carrierId > 0)
                .Distinct()
                .Select(cid => new ComboItem<int>()
                {
                    Key = cid,
                    Value = _databaseCache.GetTranslatedCarrierById(cid, this.LanguageId)?.Name ?? string.Empty
                })
                .OrderBy(i => i.Value);

            model.Services = filterData
                .Where(d => d.FkServiceid != null)
                .Select(d => d.FkServiceid ?? 0)
                .Distinct()
                .Select(sid =>
                {
                    var service = _databaseCache.GetTranslatedCarrierServiceById(sid, this.LanguageId);
                    var carrier = _databaseCache.GetTranslatedCarrierById(service?.Instance?.FkCarrierid ?? 0, this.LanguageId);
                    return new ComboItem<int>()
                    {
                        Key = sid,
                        Value = (carrier?.Name) + " / " + (service?.Name ?? string.Empty) + " / " + service.Instance.ImportExport
                    };
                })
                .OrderBy(i => i.Value);

            model.Products = filterData
                .Select(d => d.FkProductid)
                .Distinct()
                .Select(sid =>
                {
                    return new ComboItem<int>()
                    {
                        Key = sid,
                        Value = _databaseCache.GetTranslatedProductById(sid, this.LanguageId)?.Name ?? string.Empty
                    };
                })
                .OrderBy(i => i.Value);

            model.SenderCountries = filterData
                .Where(d => d.FkSenderCountryid != null)
                .Select(d => d.FkSenderCountryid ?? 0)
                .Distinct()
                .Select(cid => new ComboItem<int>()
                {
                    Key = cid,
                    Value = _databaseCache.GetTranslatedCountryById(cid, this.LanguageId)?.Name ?? string.Empty
                })
                .OrderBy(i => i.Value);

            model.ReceiverCountries = filterData
                .Where(d => d.FkReceiverCountryid != null)
                .Select(il => il.FkReceiverCountryid ?? 0)
                .Distinct()
                .Select(cid => new ComboItem<int>()
                {
                    Key = cid,
                    Value = _databaseCache.GetTranslatedCountryById(cid, this.LanguageId)?.Name ?? string.Empty
                })
                .OrderBy(i => i.Value);

            model.ShipmentIndicators = filterData
                .Where(d => d.ShipmentIndicator != null)
                .Select(d => d.ShipmentIndicator)
                .Distinct()
                .Select(shi => new ComboItem<string>()
                {
                    Key = shi,
                    Value = Defs.ShipmentIndicatorToString(shi)
                })
                .OrderBy(i => i.Value);

            if (model.HasYearsDropdown)
            {
                var yearsList = await _database.InvoiceLines
                    .AsNoTracking()
                    .Where(il => branchIds.Contains(il.Branchid) && il.ShipmentDate != null)
                    .Select(il => FreightSolutionDBEntities.DatePart("year", il.ShipmentDate) ?? 0)
                    .Distinct()
                    .OrderByDescending(year => year)
                    .ToListAsync();

                var fiscalYearStart = UserData.SelectedOrUpperCompany?.FiscalYearStart ?? 0;
                if (fiscalYearStart > 0)
                {
                    var extendedYearsSet = new HashSet<int>(yearsList);
                    extendedYearsSet.UnionWith(yearsList.Select(year => year - 1));
                    yearsList = extendedYearsSet
                        .OrderByDescending(year => year)
                        .ToList();
                }

                model.Years = yearsList
                    .Select(year => new ComboItem<int>(year, year.ToString()));
            }

            if (model.HasYearsDropdown && model.Filter.Year == null)
            {
                model.Filter.Year = model.Years?.Select(i => i.Key).FirstOrDefault();
            }
        }
    }
}