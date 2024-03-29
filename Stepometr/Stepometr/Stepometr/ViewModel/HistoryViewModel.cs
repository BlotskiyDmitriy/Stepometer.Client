﻿using Sharpnado.Presentation.Forms;
using SkiaSharp;
using Stepometer.Controls.Charts;
using Stepometer.Enum;
using Stepometer.Models;
using Stepometer.Page;
using Stepometer.Service.HttpApi.ConvertService.Contracts;
using Stepometer.Service.LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Stepometer.ViewModel
{
    public class HistoryViewModel : BaseViewModel
    {
        private readonly ILogService _logService;

        private IList<StepometerModel> _allDataStat;
        private IHistoryService _historyService;

        public AvgPeriodDataModel PeriodAvgData { get; set; }
        public ObservableRangeCollection<ChartEntry> Entries { get; set; }
        public ObservableRangeCollection<ExpanderHistoryModel> ExpanderHistory { get; set; }
        public ICommand SegmentChangedCommand { get; }
        public ICommand OpenAchievePageCommand { get; }
        public TaskLoaderNotifier SegmentLoader { get; set; }
        public byte SelectedSegment { get; set; }

        public HistoryViewModel(IHistoryService historyService, ILogService logService)
        {
            _logService = logService;
            _historyService = historyService;

            PeriodAvgData = new AvgPeriodDataModel();
            Entries = new ObservableRangeCollection<ChartEntry>();
            ExpanderHistory = new ObservableRangeCollection<ExpanderHistoryModel>();
            SegmentChangedCommand = new AsyncCommand(async () => await OnSegmentChanged(), continueOnCapturedContext: false);
            OpenAchievePageCommand = new AsyncCommand(async () => await OpenAchievePage(), continueOnCapturedContext: false);
            SegmentLoader = new TaskLoaderNotifier();

        }

        public Task Init()
        {
            SegmentLoader.Load(async () => await InitFillAllData());

            return Task.CompletedTask;
        }

        public async Task OnSegmentChanged() => await PrintGraphExpander();

        private static (EnumTypePeriod period, byte ValuePoint)
            ConvertSegmentToNecessaryData(byte segment) =>
            segment switch
            {
                0 => (EnumTypePeriod.Week, 7),
                1 => (EnumTypePeriod.Month, 4),
                2 => (EnumTypePeriod.HalfYear, 6),
                3 => (EnumTypePeriod.Year, 12),
                _ => throw new ArgumentException()
            };

        /// <summary>
        /// Draws a graph and the necessary expander depending on the selected period
        /// </summary>
        /// <param name="segment">Selected segment/period</param>
        private async Task PrintGraphExpander()
        {
            try
            {
                var (period, valuePoint) = ConvertSegmentToNecessaryData(SelectedSegment);

                var tempPeriod = period == EnumTypePeriod.Week ? EnumTypePeriod.Week : EnumTypePeriod.Month;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Entries?.Clear();
                    ExpanderHistory?.Clear();
                });

                var historyData = await _historyService.GetHistoryData();

                var avgPeriod = await GetPeriodData(historyData, tempPeriod);
                var data = await PrepareGraphStat(historyData, tempPeriod);
                var entries = await CreateGraphData(data);
                var expanderData = await PrepareExpanderData(data);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    PeriodAvgData = avgPeriod;
                    Entries.AddRange(entries);
                    ExpanderHistory.AddRange(expanderData);
                });
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private Task<AvgPeriodDataModel> GetPeriodData(AvgHistoryWebModel historyData, EnumTypePeriod period)
        {
            try
            {
                var avgStepometerModel = new AvgPeriodDataModel();

                switch (period)
                {
                    case EnumTypePeriod.Week:
                        {
                            avgStepometerModel = historyData.AvgDataStepsWeek;
                        }
                        break;
                    case EnumTypePeriod.Month:
                        {
                            avgStepometerModel = historyData.AvgDataStepsMonth;
                        }
                        break;
                    case EnumTypePeriod.HalfYear:
                        {
                            avgStepometerModel = historyData.AvgDataStepsHalfYear;
                        }
                        break;
                    case EnumTypePeriod.Year:
                        {
                            avgStepometerModel = historyData.AvgDataStepsYear;
                        }
                        break;
                    default:
                        break;
                }
                return Task.FromResult(avgStepometerModel);
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                return Task.FromResult(new AvgPeriodDataModel());
            }
        }

        private async Task OpenAchievePage()
        {
            await Shell.Current.GoToAsync($"{nameof(AchievePage)}");
        }

        public async Task InitFillAllData()
        {
            try
            {
                if (IsBusy)
                {
                    return;
                }

                IsBusy = true;

                await PrintGraphExpander();

                IsBusy = false;
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        /// <summary>
        /// Fill and prepare expander data
        /// </summary>
        /// <param name="data">List with all data</param>
        /// <param name="valueCountRootExpander">Amount root expander</param>
        /// <param name="period">Selected period</param>
        /// <returns>Return data for expander</returns>
        private Task<IList<ExpanderHistoryModel>> PrepareExpanderData(IList<StepometerModel> data)
        {
            try
            {
                IList<ExpanderHistoryModel> expanderData = new List<ExpanderHistoryModel>();

                var stories = data
                    .GroupBy(p => p.Date.ToString("MMMM"))
                    .Select(g => new
                    {
                        Name = g.Key,
                        Employees = g.Select(p => p)
                    });

                foreach (var item in stories)
                {
                    expanderData.Add(new ExpanderHistoryModel
                    {
                        Title = item.Name.ToString(),
                        ExpanderContent = item.Employees.ToList()
                    });
                }

                return Task.FromResult(expanderData);
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                return Task.FromResult<IList<ExpanderHistoryModel>>(new List<ExpanderHistoryModel>());
            }
        }

        /// <summary>
        /// Fill and prepare data for graph
        /// </summary>
        /// <param name="valuePoint">Amount point to graph</param>
        /// <returns>Return prepare data for graph</returns>
        private Task<IList<StepometerModel>> PrepareGraphStat(AvgHistoryWebModel historyData, EnumTypePeriod period)
        {
            try
            {
                var stepometerListData = new List<StepometerModel>();

                var stories = historyData.AvgDataStepsPerDay
                   .GroupBy(p => p.Date)
                   .Select(g => new
                   {
                       Name = g.Key,
                       Employees = g.Select(p => p)
                   });

                DateTime currentDateTime = DateTime.Now;

                var reversStories = stories.Reverse();

                var periodCount = 0;
                foreach (var item in reversStories)
                {
                    var daysInPeriod = period == EnumTypePeriod.Week ? 7 : DateTime.DaysInMonth(item.Name.Date.Year, item.Name.Month);

                    if (periodCount < daysInPeriod)
                    {
                        if (item.Name.Date == currentDateTime.AddDays(-periodCount).Date)
                        {
                            stepometerListData.Add(new StepometerModel
                            {
                                Steps = Convert.ToInt32(item.Employees.Average(s => s.Steps)),
                                Date = item.Name.Date
                            });
                        }
                        else
                        {
                            stepometerListData.Add(new StepometerModel
                            {
                                Steps = 0,
                                Date = currentDateTime.AddDays(-periodCount)
                            });
                        }
                    }

                    periodCount++;
                }

                return Task.FromResult<IList<StepometerModel>>(stepometerListData);
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                return Task.FromResult<IList<StepometerModel>>(new List<StepometerModel>());
            }
        }

        /// <summary>
        /// Fill result data for graph
        /// </summary>
        /// <param name="data">List with all data</param>
        /// <returns>Return final result for graph</returns>
        private async Task<IList<ChartEntry>> CreateGraphData(IList<StepometerModel> data)
        {
            try
            {
                IList<ChartEntry> entries = new List<ChartEntry>();

                foreach (var model in data)
                {
                    entries.Add(new ChartEntry(model.Steps)
                    {
                        Color = SKColor.Parse("#db7900"),
                        Label = model.Date.ToString("d.M"),
                        TextColor = SKColors.Yellow,
                        ValueLabelColor = SKColors.White,
                    });
                }

                return entries;
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
                return new List<ChartEntry>();
            }
        }
    }
}