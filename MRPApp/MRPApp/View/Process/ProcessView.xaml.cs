using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MRPApp.View.Process
{
    /// <summary>
    /// ProcessView.xaml에 대한 상호 작용 논리
    /// 1.공정계획에서 오늘의 생산계획 일정 불러옴
    /// 2.없으면 에러표시, 시작버튼 클릭하지 못하게 만듬
    /// 3.있으면 오늘의 날짜를 표시, 시작버튼 활성화
    /// 3.1. Mqtt Subscription 연결 factory01/machine/data 확인
    /// 4.시작버튼 클릭시 새공정을 생성, DB에 입력 
    ///   공정코드 : PRC202106180011 (PRC+yyyy+MM+dd+NNN)
    /// 5.공정처리 애니메이션 시작
    /// 6.로드타임 후 애니메이션 중지
    /// 7.센서값 리턴될 때까지 대기
    /// 8.센서 결과값에 따라서 생산품 색상 변경
    /// 9.현재공정의 DB값 업데이트
    /// 10.결과레이블 값 수정/표시
    /// </summary>
    public partial class ProcessView : Page
    {
        //금일 일정
        private Model.Schedules currSchedule;

        public ProcessView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                currSchedule = Logic.DataAccess.GetSchedules().Where(s => s.PlantCode.Equals(Commons.PLANTCODE))
                                    .Where(s => s.SchDate.Equals(DateTime.Parse(today))).FirstOrDefault();

                if (currSchedule == null)
                {
                    await Commons.ShowMessageAsync("공정","공정계획이 없습니다. 계획일정을 먼저 입력하세요");
                    LblProcessDate.Content = string.Empty;
                    LblSchLoadTime.Content = "None";
                    LblSchAmount.Content = "None";
                    BtnStartProcess.IsEnabled = false;
                    return;
                }
                else
                {
                    await Commons.ShowMessageAsync("공정", $"{today} 공정 시작합니다.");
                    LblProcessDate.Content = currSchedule.SchDate.ToString("yyyy년MM월dd일");
                    LblSchLoadTime.Content = $"{currSchedule.SchLoadTime} 초";
                    LblSchAmount.Content = $"{currSchedule.SchAmount} 개";
                    UpdateData();
                    BtnStartProcess.IsEnabled = true;
                    InitConnectMqttBroker(); //공정 시작시 Mqtt연결
                }
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 MyAccount Loaded : {ex}");
                throw ex;
            }
        }

        MqttClient client;
        Timer timer = new Timer();
        Stopwatch sw = new Stopwatch();

        private void InitConnectMqttBroker()
        {
            var brokerAddress = IPAddress.Parse("210.119.12.92");
            client = new MqttClient(brokerAddress);
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            client.Connect("Monitor");
            client.Subscribe(new string[] { "factory1/machine1/data/" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

            timer.Enabled = true;
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (sw.Elapsed.Seconds >= 2) //2초 대기후 일처리
            {
                sw.Stop();
                sw.Reset();
                //MessageBox.Show(currentData["PRC_MSG"]);
                if (currentData["PRC_MSG"] == "OK")
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        Product.Fill = new SolidColorBrush(Colors.Green);
                    }));
                }
                else if (currentData["PRC_MSG"] == "FAIL")
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        Product.Fill = new SolidColorBrush(Colors.Red);
                    }));
                }

                /*Dispatcher.Invoke(DispatcherPriority.Normal,new Action(delegate
                {
                    UpdateData();
                }));*/
            }
        }

        private void UpdateData()
        {
            // 성공 수량
            var prcOkAmount = Logic.DataAccess.GetProcess().Where(p => p.SchIdx.Equals(currSchedule.SchIdx))
                                                           .Where(p => p.PrcResult.Equals(true)).Count();
            // 실패 수량
            var prcFailAmount = Logic.DataAccess.GetProcess().Where(p => p.SchIdx.Equals(currSchedule.SchIdx))
                                                           .Where(p => p.PrcResult.Equals(false)).Count();

            // 성공률
            //var prcOkRate = (double)prcOkAmount / (double)currSchedule.SchAmount * 100;
            var prcOkRate = (double)prcOkAmount / ((double)prcOkAmount+(double)prcFailAmount) * 100;
            // 실패률
            //var prcFailRate = (double)prcFailAmount / (double)currSchedule.SchAmount * 100;
            var prcFailRate = (double)prcFailAmount / ((double)prcOkAmount + (double)prcFailAmount) * 100;

            LblPrcOkAmount.Content = $"{prcOkAmount}개";
            LblPrcFailAmount.Content = $"{prcFailAmount}개";
            LblPrcOkRatio.Content = $"{prcOkRate.ToString("#.##")}%";
            LblPrcFailRatio.Content = $"{prcFailRate.ToString("#.##")}%";
        }

        Dictionary<string, string> currentData = new Dictionary<string, string>();

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message);
            currentData = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);

            if (currentData["PRC_MSG"] == "OK" || currentData["PRC_MSG"] == "FAIL")
            {
                sw.Stop();
                sw.Reset();
                sw.Start();
                StartSensorAnimation();
            }
        }

        private void StartSensorAnimation()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
             {
                 DoubleAnimation ba = new DoubleAnimation();
                 ba.From = 1; //이미지 보임
                 ba.To = 0; //이미지 보이지 않음
                 ba.Duration = TimeSpan.FromSeconds(2);
                 ba.AutoReverse = true;
                //ba.RepeatBehavior = RepeatBehavior.Forever;

                Sensor.BeginAnimation(Canvas.OpacityProperty, ba);
             }));
        }

        private void BtnStartProcess_Click(object sender, RoutedEventArgs e)
        {
            if (InsertProcessData())
            {
                StartAnimation();//HMI animation

                UpdateData();
            }
        }

        private bool InsertProcessData()
        {
            var item = new Model.Process();
            item.SchIdx = currSchedule.SchIdx;
            item.PrcCD = GetProcessCodeFromDB();
            item.PrcDate = DateTime.Now;
            item.PrcLoadTime = currSchedule.SchLoadTime;
            item.PrcStartTime = currSchedule.SchStartTime;
            item.PrcEndTime = currSchedule.SchEndTime;
            item.PrcFacilityID = Commons.FACILITYID;
            item.PrcResult = true; //공정성공으로 우선 fix
            item.RegDate = DateTime.Now;
            item.RegID = "MRP";

            try
            {
                var result = Logic.DataAccess.SetProcess(item);
                if (result == 0)
                {
                    Commons.LOGGER.Error("공정데이터 입력 실패!");
                    Commons.ShowMessageAsync("오류", "공정시작 오류발생, 관리자 문의");
                    return false;
                }
                else
                {
                    Commons.LOGGER.Info("공정데이터 입력!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Commons.LOGGER.Error($"예외발생 : {ex}");
                Commons.ShowMessageAsync("오류", "공정시작 오류발생, 관리자 문의");
                return false;
            }
        }

        private string GetProcessCodeFromDB()
        {
            var prefix = "PRC";
            var prePrcCode = prefix + DateTime.Now.ToString("yyyyMMdd");
            var resultCode = string.Empty;

            //이전까지 공정이 없어 PRC20210629...) null이 넘어오고
            //PRC20210620001, 002, 003, 004
            var maxPrc = Logic.DataAccess.GetProcess().Where(p => p.PrcCD.Contains(prePrcCode))
                                            .OrderByDescending(p => p.PrcCD).FirstOrDefault();
            if (maxPrc == null)
            {
                resultCode = prePrcCode + 1.ToString("000"); //당일 공정코드 최초값
            }
            else
            {
                var maxPrcCd = maxPrc.PrcCD; //PRC20210629004
                var maxVal = int.Parse(maxPrcCd.Substring(11)) + 1; // 004 --> 4 + 1 = 5

                resultCode = prePrcCode + maxVal.ToString("000");
            }
            return resultCode;
        }

        private void StartAnimation()
        {
            Product.Fill = new SolidColorBrush(Colors.Gray);

            //Gear Animation
            DoubleAnimation da = new DoubleAnimation();
            da.From = 0;
            da.To = 360;
            da.Duration = new Duration(TimeSpan.FromSeconds(currSchedule.SchLoadTime / 10)); //일정 계획 로드타임
            //da.RepeatBehavior = RepeatBehavior.Forever;

            RotateTransform rt = new RotateTransform();
            Gear1.RenderTransform = rt;
            Gear1.RenderTransformOrigin = new Point(0.5, 0.5);
            Gear2.RenderTransform = rt;
            Gear2.RenderTransformOrigin = new Point(0.5, 0.5);

            rt.BeginAnimation(RotateTransform.AngleProperty, da);

            //Product Animation
            DoubleAnimation ma = new DoubleAnimation();
            ma.From = 140;
            ma.To = 575;
            ma.Duration = TimeSpan.FromSeconds(currSchedule.SchLoadTime / 10);
            //ma.AccelerationRatio = 0.5;
            //ma.AutoReverse = true;

            Product.BeginAnimation(Canvas.LeftProperty, ma);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //자원 해제
            if (client.IsConnected) client.Disconnect();
            timer.Dispose();
        }
    }
}
