# MES Program

<kbd>[![MES](/Capture/MES.gif "MES")](https://github.com/kg4543/MiniProject_SimpleMES)</kbd> </br>
- 'Manufacturing Execution System'으로 오더착수부터 제품 출하까지 전 생산활동을 관리하는 시스템으로 생산 현장에서 발생하는 데이터를 실시간으로 집계/분석/모니터링하는 시스템을 말한다
- 이프로그램에서는 공정의 품질검사 부분을 집계/모니터링한다.

## SENSOR

<kbd>[![sensor](/Capture/sensor.jpg "sensor")](https://github.com/kg4543/MiniProject_SimpleMES/blob/main/check_publish_app.py)</kbd> </br>
(Click the Image)

- RGB sensor를 사용하여 초록(양품) / 빨강(불량)을 구분한다.
- sensor의 정확도를 위해 10이하 2000이상의 데이터는 제외

```
def read_value(a2, a3):
    GPIO.output(s2, a2)
    GPIO.output(s3, a3)
    # Set Time to set sensor
    time.sleep(0.3)
    # waiting
    #GPIO.wait_for_edge(out, GPIO.FALLING)
    #GPIO.wait_for_edge(out, GPIO.RISING)
    start = time.time() #current time
    for impulse_count in range(NUM_CYCLES):
        GPIO.wait_for_edge(out, GPIO.FALLING)

    end = (time.time() - start)
    return NUM_CYCLES / end
    
def loop():
    result = ''

    while True:
        red = read_value(GPIO.LOW, GPIO.LOW) #s2 low, s3 low
        time.sleep(0.1) # Delay 0.1s
        green = read_value(GPIO.HIGH, GPIO.HIGH) #s2 high, s3 high
        time.sleep(0.1)
        blue = read_value(GPIO.LOW, GPIO.HIGH)

        print('red = {0}, green = {1}, blue={2})'.format(red, green, blue))
        if(red < 10): continue
        if(red > 2000 or green > 2000 or blue > 2000): continue
        
        if((red > green) and (red > blue)) :
            result = 'RED'
            send_data(result, red, green, blue)
        elif((green > red) and (green > blue)):
            result = 'GREEN'
            send_data(result, red, green, blue)
        else:
            result = 'ERROR'
        
        time.sleep(1)
```
- mosquito를 활용하여 mqtt방식으로 Json 형태의 데이터를 전달

```
dev_id = 'MACHINE01'
broker_address = '210.119.12.92'
pub_topic = 'factory1/machine1/data/'

def send_data(param, red, green, blue):
    message = ''
    if(param == 'GREEN'):
        message = 'OK'
    elif(param == 'RED'):
        message = 'FAIL'
    elif(param == 'CONN'):
        message = 'CONNECTED'
    else:
        message = 'ERROR'

    currtime = dt.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')
    #json data gen
    raw_data = OrderedDict()
    raw_data['DEV_ID'] = dev_id
    raw_data['PRC_TIME'] = currtime
    raw_data['PRC_MSG'] = message
    raw_data['COLOR'] = param
    raw_data['RED'] = red
    raw_data['GREEN'] = green
    raw_data['BLUE'] = blue
    
    pub_data = json.dumps(raw_data, ensure_ascii=False, indent='\t')
    print(pub_data)
    #mqtt_publish
    client2.publish(pub_topic, pub_data)
```

## Data Log

<kbd>[![Log](/Capture/DataLog.png "Log")](https://github.com/kg4543/MiniProject_SimpleMES/blob/main/MRPApp/DeviceSubApp/FrmMain.cs)</kbd> </br>
(Click the Image)

- Winform의 '리치텍스트박스'를 활용하여 Json형태의 데이터를 받아 Monitoring
- Json 및 Mqtt library 활용 Client 연결
```
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

private void InitializeAllData()
        {
            connectinString = $@"Data Source = {TxtConnectionString.Text}; Initial Catalog = MRP; 
                                    User ID = sa; password = mssql_p@ssw0rd!";
            lineCount = 0;
            BtnConnect.Enabled = true;
            BtnDisconnect.Enabled = false;
            IPAddress brokerAddress;
            try
            {
                brokerAddress = IPAddress.Parse(TxtConnectionString.Text);
                client = new MqttClient(brokerAddress);
                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            Timer.Enabled = true;
            Timer.Interval = 1000; //1000ms = 1sec
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }
        
 private void BtnConnect_Click(object sender, EventArgs e)
        {
            client.Connect(TxtClientId.Text);
            UpdateText(">>>>> Client Connected");
            client.Subscribe(new string[] { TxtSubscriptionTopic.Text },
                new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            UpdateText(">>>>> Subscribing to : " + TxtSubscriptionTopic.Text);

            BtnConnect.Enabled = false;
            BtnDisconnect.Enabled = true;
        }      
```
- Mqtt를 통해 들어온 메세지를 문자열에 표시
```
private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var message = Encoding.UTF8.GetString(e.Message);
                UpdateText($">>>>> 받은 메시지 : {message}");
                //message(json) > C#
                var currentData = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);
                PrcinputDataToList(currentData);

                sw.Stop();
                sw.Reset();
                sw.Start();
            }
            catch (Exception ex)
            {
                UpdateText($">>>>> ERROR!!! : {ex.Message}");
            }
        }

        List<Dictionary<string, string>> iotData = new List<Dictionary<string, string>>();

        //라즈베리에서 들어온 메시지를 전역리스트에 입력하는 메서드
        private void PrcinputDataToList(Dictionary<string, string> currentData)
        {
            if (currentData["PRC_MSG"] != "OK" || currentData["PRC_MSG"] != "FAIL")
            {
                iotData.Add(currentData);
            }
            iotData.Add(currentData);
        }
```
- mqtt로 받은 데이터를 DB에 저장
```
//실제 여러 데이터 중 최종 데이터만 DB입력
        private void PrcCorrectDataToDB()
        {
            if (iotData.Count > 0)
            {
                var correntData = iotData[iotData.Count - 1];
                //DB에 입력
                //UpdateText("DB 처리");
                using (var conn = new SqlConnection(connectinString))
                {
                    var prcResult = correntData["PRC_MSG"] == "OK" ? 1 : 0;
                    string strUpQry = $@"UPDATE Process
                                           SET  PrcResult = '{ prcResult }'
                                              , ModDate = '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}'
                                              , ModID = '{ "SYS" }'
                                         WHERE PrcIdx = (SELECT Top 1 Prcidx FROM Process
                                                         order by PrcIdx desc)";

                    try
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(strUpQry, conn);
                        if (cmd.ExecuteNonQuery() == 1)
                            UpdateText("[DB] 센싱값 Update 성공");
                        else
                            UpdateText("[DB] 센싱값 Update 실패");
                    }
                    catch (Exception ex)
                    {
                        UpdateText($">>>>> DB ERROR!!! : {ex.Message}");
                    }
                }
            }
            iotData.Clear(); //데이터 모두 삭제
        }
```

## Setting

<kbd>[![Set](/Capture/Set.PNG "Set")](https://github.com/kg4543/MiniProject_SimpleMES/blob/main/MRPApp/MRPApp/View/Setting/SettingList.xaml.cs)</kbd> </br>
(Click the Image)

- 기본적인 공장의 정보를 입력
- Entity Framwork를 활용하여 DB Model을 로드 및 수정
```
public class DataAccess
    {
        //setting table에서 데이터 가져오기
        public static List<Settings> GetSettings()
        {
            List<Settings> list;

            using(var ctx = new MRPEntities())
                list = ctx.Settings.ToList(); //Select

            return list;
        }

        public static int SetSetting(Settings item)
        {
            using (var ctx = new MRPEntities())
            {
                ctx.Settings.AddOrUpdate(item); //insert or update
                return ctx.SaveChanges();
            }
        }
     }
--------------------------------------------------------------------------
private void LoadGridData()
        {
            List<Model.Settings> settings = Logic.DataAccess.GetSettings();
            this.DataContext = settings;
        }
```
- 유효성검사를 통한 데이터 무결성 유지
```
private bool IsValidInputs()
        {
            var isValid = true;
            InitErrorMessage();

            if (string.IsNullOrEmpty(TxtBasicCode.Text))
            {
                LblBasicCode.Visibility = Visibility.Visible;
                LblBasicCode.Text = "코드를 입력하세요";
                isValid = false;
            }
            else if (Logic.DataAccess.GetSettings().Where(s => s.BasicCode.Equals(TxtBasicCode.Text)).Count() > 0)
            {
                LblBasicCode.Visibility = Visibility.Visible;
                LblBasicCode.Text = "중복코드가 존재합니다.";
                isValid = false;
            }

            if (string.IsNullOrEmpty(TxtCodeName.Text))
            {
                LblCodeName.Visibility = Visibility.Visible;
                LblCodeName.Text = "코드명를 입력하세요";
                isValid = false;
            }

            return isValid;
        }
```

## Schedule

<kbd>[![Plan](/Capture/Plan.PNG "Plan")](https://github.com/kg4543/MiniProject_SimpleMES/blob/main/MRPApp/MRPApp/View/Schedule/ScheduleList.xaml.cs)</kbd> </br>
(Click the Image)

- Setting에서 설정한 공장의 생산 스케줄을 입력
- 데이터 유효성 검사 후 데이터 입력 및 수정
- Grid 공장 데이터와 ComboBox 값을 코드가 아닌 공장의 이름을 받아와 표시 (DisplayMemberPath="CodeName")
```
<DataGrid.Columns>
                    <DataGridTextColumn Binding="{Binding SchIdx}" Header="순번" Width="100" />
                    <!--<DataGridTextColumn Binding="{Binding PlantCode}" Header="공장" Width="1*" IsReadOnly="True" />-->
                    <DataGridComboBoxColumn x:Name="CboGrdPlantCode" Header="공장" Width="100"
                                            DisplayMemberPath="CodeName" SelectedValuePath="BasicCode"
                                            SelectedValueBinding="{Binding PlantCode}"/>
                    <DataGridTextColumn Binding="{Binding SchDate, StringFormat=yyyy-MM-dd}" Header="공정일" Width="1*" />
                    <DataGridTextColumn Binding="{Binding SchAmount}" Header="계획수량" Width="1*" />
                    <DataGridTextColumn Header="" Width="10" IsReadOnly="True"/>
                </DataGrid.Columns>
```

## Process

<kbd>[![Monitor](/Capture/Monitor.PNG "Monitor")](https://github.com/kg4543/MiniProject_SimpleMES/blob/main/MRPApp/MRPApp/View/Schedule/ScheduleList.xaml.cs)</kbd> </br>
(Click the Image)

- 오늘 날짜의 공정계획이 있는지 판단하여 없는 경우 실행 불가
```
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
```
- M2Mqtt NuGet Package를 받아 Json 및 mqtt library를 활용, 센싱 데이터를 받아와 결과 표시 
- 결과 데이터를 Entity Framework로 연동시킨 DB에 저장
```
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
```
- Timer library를 활용하여 공정이 흘러가는 시간 대기 후 결과 표시
```
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
            }
        }
```
- Media.Animation library를 활용하여 공정이 흘러가는 Animation 표현
```
private void StartAnimation()
        {
            Product.Fill = new SolidColorBrush(Colors.Gray);

            //Gear Animation (기어회전)
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

            //Product Animation (제품 이동)
            DoubleAnimation ma = new DoubleAnimation();
            ma.From = 140;
            ma.To = 575;
            ma.Duration = TimeSpan.FromSeconds(currSchedule.SchLoadTime / 10);
            //ma.AccelerationRatio = 0.5;
            //ma.AutoReverse = true;

            Product.BeginAnimation(Canvas.LeftProperty, ma);
        }
```

## Report

<kbd>[![Report](/Capture/Report.PNG "Report")](https://github.com/kg4543/MiniProject_SimpleMES/blob/main/MRPApp/MRPApp/View/Report/ReportView.xaml.cs)</kbd> </br>
(Click the Image)

- LiveCharts Nuget Package를 받아 Report를 그래프로 표현
```
<live:CartesianChart
        x:Name="ChtReport"
        BorderThickness="2"
        LegendLocation="Top" Margin="10"/>
        
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

```
- Query문을 통해 Report Data를 받아옴
```
 var sqlQuery = $@"SELECT sch.SchIdx, sch.PlantCode, sch.SchAmount, prc.PrcDate,
		                                    prc.OK_Amount, prc.Fail_Amount
	                                From Schedules as sch
                             inner join(
			                            SELECT smr.SchIdx, smr.PrcDate, sum(PrcOK) as OK_Amount, sum(PrcFail) as Fail_Amount
			                              From (
					                             SELECT p.SchIdx, p.PrcDate, 
							                            CASE p.PrcResult When 1 Then 1 else 0 END AS PrcOK,
							                            CASE p.PrcResult When 0 Then 1 else 0 END AS PrcFail
					                               From Process AS p
					                            ) as smr
			                                        Group by smr.SchIdx, smr.PrcDate
			                                    )AS prc
			                                        ON sch.SchIdx = prc.SchIdx
			                                        where sch.PlantCode = '{plantCode}'
			                                          and prc.PrcDate Between '{startDate}' and '{endDate}' ";
```
- 시작일이 종료일보다 넘지 않도록 유효성 검사 실시
```
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
```
