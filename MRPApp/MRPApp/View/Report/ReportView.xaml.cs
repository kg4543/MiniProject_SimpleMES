using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MRPApp.View.Report
{
    /// <summary>
    /// MyAccount.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReportView : Page
    {
        public ReportView()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitControls();

                //DisplayChart();
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 ReportView Loaded : {ex}");
                throw ex;
            }
        }

        private void InitControls()
        {
            DtpStartDate.SelectedDate = DateTime.Now.AddDays(-7);
            DtpEndDate.SelectedDate = DateTime.Now;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidInputs())
            {
                var startDate = ((DateTime)DtpStartDate.SelectedDate).ToString("yyyy-MM-dd");
                var endDate = ((DateTime)DtpEndDate.SelectedDate).ToString("yyyy-MM-dd");
                var searchResult = Logic.DataAccess.GetReportDatas(startDate, endDate, Commons.PLANTCODE);

                DisplayChart(searchResult);
            }
        }

        private void DisplayChart(List<Model.Report> list)
        {
            int[] SchAmount = list.Select(a => (int)a.SchAmount).ToArray();
            int[] OKAmount = list.Select(a => (int)a.OKAmount).ToArray();
            int[] FailAmount = list.Select(a => (int)a.FailAmount).ToArray();

            var series1 = new LiveCharts.Wpf.ColumnSeries
            {
                Title = "계획 수량",
                Fill = new SolidColorBrush(Colors.BlueViolet),
                Values = new LiveCharts.ChartValues<int>(SchAmount)
            };
            var series2 = new LiveCharts.Wpf.ColumnSeries
            {
                Title = "성공 수량",
                Fill = new SolidColorBrush(Colors.Blue),
                Values = new LiveCharts.ChartValues<int>(OKAmount)
            };
            var series3 = new LiveCharts.Wpf.ColumnSeries
            {
                Title = "실패 수량",
                Fill = new SolidColorBrush(Colors.Red),
                Values = new LiveCharts.ChartValues<int>(FailAmount)
            };

            //chart 할당
            ChtReport.Series.Clear();
            ChtReport.Series.Add(series1);
            ChtReport.Series.Add(series2);
            ChtReport.Series.Add(series3);
            ChtReport.AxisX.First().Labels = list.Select(a => a.PrcDate.ToString("yyyy-MM-dd")).ToList();
        }

        private bool IsValidInputs()
        {
            var result = true;

            if (DtpStartDate.SelectedDate == null | DtpEndDate.SelectedDate == null)
            {
                Commons.ShowMessageAsync("검색", "검색할 일자를 선택하세요");
                result = false;
            }
            if (DtpEndDate.SelectedDate < DtpStartDate.SelectedDate)
            {
                Commons.ShowMessageAsync("검색", "시작일자가 종료일자보다 최신일 수 없습니다");
                result = false;
            }

            return result;
        }
    }
}
