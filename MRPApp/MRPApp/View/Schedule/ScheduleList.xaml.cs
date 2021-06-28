using MRPApp.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Input;

namespace MRPApp.View.Schedule
{
    /// <summary>
    /// MyAccount.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ScheduleList : Page
    {
        public ScheduleList()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadControlData(); //콤보박스 리스트 데이터 로드
                LoadGridData(); //테이블 그리드 표시

                InitErrorMessage();
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 StoreList Loaded : {ex}");
                throw ex;
            }
        }

        private void LoadControlData()
        {
            var plantCodes = Logic.DataAccess.GetSettings().Where(c => c.BasicCode.Contains("PC")).ToList();
            CboPlantCode.ItemsSource = plantCodes;
            CboGrdPlantCode.ItemsSource = plantCodes;

            var facilityCodes = Logic.DataAccess.GetSettings().Where(c => c.BasicCode.Contains("FC")).ToList();
            CboSchFailityID.ItemsSource = facilityCodes;
        }

        private void LoadGridData()
        {
            List<Model.Schedules> schedules = Logic.DataAccess.GetSchedules();
            this.DataContext = schedules;
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
        }

        private async void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidInputs() != true)
                return;

            var item = new Model.Schedules();
            item.PlantCode = CboPlantCode.SelectedValue.ToString();
            item.SchDate = DateTime.Parse(DtpSchDate.Text);
            item.SchLoadTime = int.Parse(TxtSchLoadTime.Text);
            item.SchStartTime = TmpSchStartTime.SelectedDateTime.Value.TimeOfDay;
            item.SchEndTime = TmpSchEndTime.SelectedDateTime.Value.TimeOfDay;
            item.SchFacilityID = CboSchFailityID.SelectedValue.ToString();
            item.SchAmount = (int)NudSchAmount.Value;

            item.RegDate = DateTime.Now;
            item.RegID = "MRP";

            try
            {
                var result = DataAccess.SetSchedule(item);

                if (result == 0)
                {
                    Commons.LOGGER.Error($"데이터 수정시 오류 발생");
                    await Commons.ShowMessageAsync("오류", "데이터 수정 실패");
                }
                else
                {
                    Commons.LOGGER.Info($"데이터 수정 성공 : {item.SchIdx}");
                    ClearInputs();
                    LoadGridData();
                }
            }
            catch (Exception ex)
            {

                Commons.LOGGER.Error($"예외발생 {ex}");
            }
        }

        private void InitErrorMessage()
        {
            LblPlantCode.Visibility = LblSchDate.Visibility = LblSchLoadTime.Visibility = 
            LblSchStartTime.Visibility = LblSchEndTime.Visibility = LblSchFailityID.Visibility =
            LblSchAmount.Visibility = Visibility.Hidden;
        }

        private bool IsValidInputs()
        {
            var isValid = true;
            InitErrorMessage();

            if (CboPlantCode.SelectedValue == null)
            {
                LblPlantCode.Visibility = Visibility.Visible;
                LblPlantCode.Text = "공장을 선택하세요";
                isValid = false;
            }

            if (string.IsNullOrEmpty(DtpSchDate.Text))
            {
                LblSchDate.Visibility = Visibility.Visible;
                LblSchDate.Text = "공정일을 입력하세요";
                isValid = false;
            }

            if (CboPlantCode.SelectedValue != null && !string.IsNullOrEmpty(DtpSchDate.Text))
            {
                var result = Logic.DataAccess.GetSchedules()
                                .Where(s => s.PlantCode.Equals(CboPlantCode.SelectedValue.ToString()))
                                .Where(d => d.SchDate.Equals(DateTime.Parse(DtpSchDate.Text))).Count();

                if (result > 0)
                {
                    LblSchDate.Visibility = Visibility.Visible;
                    LblSchDate.Text = "해당공장에 계획이 이미있습니다.";
                    isValid = false;
                }
            }
            
            if (string.IsNullOrEmpty(TxtSchLoadTime.Text))
            {
                LblSchLoadTime.Visibility = Visibility.Visible;
                LblSchLoadTime.Text = "로드타임을 입력하세요";
                isValid = false;
            }

            if (CboSchFailityID.SelectedValue == null)
            {
                LblSchFailityID.Visibility = Visibility.Visible;
                LblSchFailityID.Text = "공정설비를 선택하세요";
                isValid = false;
            }

            if (NudSchAmount.Value <= 0 | NudSchAmount.Value == null)
            {
                LblSchAmount.Visibility = Visibility.Visible;
                LblSchAmount.Text = "수량은 0 이상이어야 합니다.";
                isValid = false;
            }

            return isValid;
        }

        private bool IsValidUpdate()
        {
            var isValid = true;
            InitErrorMessage();

            if (CboPlantCode.SelectedValue == null)
            {
                LblPlantCode.Visibility = Visibility.Visible;
                LblPlantCode.Text = "공장을 선택하세요";
                isValid = false;
            }

            if (string.IsNullOrEmpty(DtpSchDate.Text))
            {
                LblSchDate.Visibility = Visibility.Visible;
                LblSchDate.Text = "공정일을 입력하세요";
                isValid = false;
            }

            if (string.IsNullOrEmpty(TxtSchLoadTime.Text))
            {
                LblSchLoadTime.Visibility = Visibility.Visible;
                LblSchLoadTime.Text = "로드타임을 입력하세요";
                isValid = false;
            }

            if (CboSchFailityID.SelectedValue == null)
            {
                LblSchFailityID.Visibility = Visibility.Visible;
                LblSchFailityID.Text = "공정설비를 선택하세요";
                isValid = false;
            }

            if (NudSchAmount.Value <= 0 | NudSchAmount.Value == null)
            {
                LblSchAmount.Visibility = Visibility.Visible;
                LblSchAmount.Text = "수량은 0 이상이어야 합니다.";
                isValid = false;
            }

            return isValid;
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (GrdData.SelectedItem != null)
            {
                if (IsValidUpdate() != true)
                    return;

                var item = GrdData.SelectedItem as Model.Schedules;
                item.PlantCode = CboPlantCode.SelectedValue.ToString();
                item.SchDate = DateTime.Parse(DtpSchDate.Text);
                item.SchLoadTime = int.Parse(TxtSchLoadTime.Text);
                item.SchStartTime = TmpSchStartTime.SelectedDateTime.Value.TimeOfDay;
                item.SchEndTime = TmpSchEndTime.SelectedDateTime.Value.TimeOfDay;
                item.SchFacilityID = CboSchFailityID.SelectedValue.ToString();
                item.SchAmount = (int)NudSchAmount.Value;

                item.ModDate = DateTime.Now;
                item.ModID = "MRP";

                try
                {
                    var result = DataAccess.SetSchedule(item);

                    if (result == 0)
                    {
                        Commons.LOGGER.Error($"데이터 수정시 오류 발생");
                        await Commons.ShowMessageAsync("오류", "데이터 수정 실패");
                    }
                    else
                    {
                        Commons.LOGGER.Info($"데이터 수정 성공 : {item.SchIdx}");
                        ClearInputs();
                        LoadGridData();
                    }
                }
                catch (Exception ex)
                {

                    Commons.LOGGER.Error($"예외발생 {ex}");
                }
            }
            else
            {
                await Commons.ShowMessageAsync("수정", "수정할 코드를 선택하세요");
            }
        }

        private void ClearInputs()
        {
            TxtSchIdx.Text = "";
            CboPlantCode.SelectedItem = null;
            DtpSchDate.Text = "";
            TxtSchLoadTime.Text = "";
            TmpSchStartTime.SelectedDateTime = null;
            TmpSchEndTime.SelectedDateTime = null;
            CboSchFailityID.SelectedItem = null;
            NudSchAmount.Value = 0;

            CboPlantCode.Focus();
        }
        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSearch_Click(sender, e);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var search = DtpSearch.Text;
            var list = DataAccess.GetSchedules().Where(s => s.SchDate.Equals(DateTime.Parse(search))).ToList();

            DataContext = list;
        }

        private void GrdData_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            ClearInputs();
            try
            {
                var item = GrdData.SelectedItem as Model.Schedules;
                TxtSchIdx.Text = item.SchIdx.ToString();
                CboPlantCode.SelectedValue = item.PlantCode;
                DtpSchDate.Text = item.SchDate.ToString();
                TxtSchLoadTime.Text = item.SchLoadTime.ToString();
                if (item.SchStartTime != null)
                    TmpSchStartTime.SelectedDateTime = new DateTime(item.SchStartTime.Value.Ticks);
                if (item.SchEndTime != null)
                    TmpSchEndTime.SelectedDateTime = new DateTime(item.SchEndTime.Value.Ticks);;
                CboSchFailityID.SelectedValue = item.SchFacilityID;
                NudSchAmount.Value = item.SchAmount;
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 {ex}");
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var setting = GrdData.SelectedItem as Model.Settings;

            if (setting == null)
            {
                await Commons.ShowMessageAsync("삭제", "삭제할 코드를 선택하세요");
                return;
            }
            else
            {
                try
                {
                    var result = Logic.DataAccess.DelSettings(setting);
                    if (result == 0)
                    {
                        Commons.LOGGER.Error($"데이터 삭제 오류 발생");
                        await Commons.ShowMessageAsync("오류", "데이터 수정 실패");
                    }
                    else
                    {
                        Commons.LOGGER.Info($"데이터 삭제 성공 : {setting.BasicCode}");
                        ClearInputs();
                        LoadGridData();
                    }
                }
                catch (Exception ex)
                {

                    Commons.LOGGER.Error($"예외발생 {ex}");
                }
            }
        }

        
    }
}
