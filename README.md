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
                                              , ModDate = '{ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }'
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

<kbd>[![Set](/Capture/Set.PNG "Set")](https://github.com/kg4543/MiniProject_SimpleMES/tree/main/MRPApp/MRPApp/View/Setting)</kbd> </br>
(Click the Image)

- Entity Framwork를 활용하여 DB Model을 불러옴
- 기본적인 공장의 정보를 입력

## Schedule

<kbd>[![Plan](/Capture/Plan.PNG "Plan")](https://github.com/kg4543/MiniProject_SimpleMES/tree/main/MRPApp/MRPApp/View/Schedule)</kbd> </br>
(Click the Image)

## Process

<kbd>[![Monitor](/Capture/Monitor.PNG "Monitor")](https://github.com/kg4543/MiniProject_SimpleMES/tree/main/MRPApp/MRPApp/View/Process)</kbd> </br>
(Click the Image)

## Report

<kbd>[![Report](/Capture/Report.PNG "Report")](https://github.com/kg4543/MiniProject_SimpleMES/tree/main/MRPApp/MRPApp/View/Report)</kbd> </br>
(Click the Image)
