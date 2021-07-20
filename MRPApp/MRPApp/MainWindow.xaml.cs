using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows;
using System.Configuration;
using System.Linq;
using MRPApp.View.Setting;
using MRPApp.View.Schedule;
using MRPApp.View.Process;
using MRPApp.View.Report;

namespace MRPApp
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }       

        private void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            Commons.PLANTCODE = ConfigurationManager.AppSettings.Get("PlantCode");
            Commons.FACILITYID = ConfigurationManager.AppSettings.Get("FacilityID");

            try
            {
                var plantName = Logic.DataAccess.GetSettings().Where(c => c.BasicCode.Equals(Commons.PLANTCODE)).FirstOrDefault().CodeName;
                BtnPlantName.Content = plantName;
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 : {ex}");
            }
        }

        private async void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var result = await this.ShowMessageAsync("종료", "프로그램을 종료하기겠습니까?", 
                MessageDialogStyle.AffirmativeAndNegative, null);

            if (result == MessageDialogResult.Affirmative)
            {
                Application.Current.Shutdown();
            }
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ActiveControl.Content = new SettingList();
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 BtnSetting_Click : {ex}");
                this.ShowMessageAsync("예외", $"예외발생 : {ex}");
            }
        }

        private void BtnSchedule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ActiveControl.Content = new ScheduleList();
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 BtnSetting_Click : {ex}");
                this.ShowMessageAsync("예외", $"예외발생 : {ex}");
            }
        }

        private void BtnMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ActiveControl.Content = new ProcessView();
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 BtnSetting_Click : {ex}");
                this.ShowMessageAsync("예외", $"예외발생 : {ex}");
            }
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ActiveControl.Content = new ReportView();
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 BtnSetting_Click : {ex}");
                this.ShowMessageAsync("예외", $"예외발생 : {ex}");
            }
        }
    }
}
