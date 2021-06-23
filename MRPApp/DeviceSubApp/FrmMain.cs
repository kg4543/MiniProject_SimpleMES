using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace DeviceSubApp
{
    public partial class FrmMain : Form
    {
        MqttClient client;
        string connectinString;
        ulong lineCount;
        delegate void UpdateTextCallback(string message);

        Stopwatch sw = new Stopwatch();

        public FrmMain()
        {
            InitializeComponent();
            InitializeAllData();
        }

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

        private void Timer_Tick(object sender, EventArgs e)
        {
            LblResult.Text = sw.Elapsed.Seconds.ToString();
            if (sw.Elapsed.Seconds >= 3)
            {
                sw.Stop();
                sw.Reset();
                //TODO 실제처리 프로세스 실행
                //UpdateText("처리!!");
                PrcCorrectDataToDB();
            }
        }

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
                    string strUpQry = $@"UPDATE Process_dev
                                           SET PrcEndTime = '{ DateTime.Now.ToString("HH:mm:ss") }'
                                              , PrcResult = '{ prcResult }'
                                              , ModDate = '{ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }'
                                              , ModID = '{ "SYS" }'
                                         WHERE PrcIdx = (SELECT Top 1 Prcidx FROM Process_dev
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
                        throw;
                    }
                }
            }

            iotData.Clear(); //데이터 모두 삭제
        }

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

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            client.Disconnect();
            UpdateText(">>>> Client disconnected!!");
            //RtbSubscr.AppendText(">>>> Client disconnected!!\n");

            BtnConnect.Enabled = true;
            BtnDisconnect.Enabled = false;
        }

        private void UpdateText(string message)
        {
            if (RtbSubscr.InvokeRequired)
            {
                UpdateTextCallback callback = new UpdateTextCallback(UpdateText);
                this.Invoke(callback, new object[] { message });
            }
            else
            {
                lineCount++;
                RtbSubscr.AppendText($"{lineCount} : {message} \n");
                RtbSubscr.ScrollToCaret();
            }
        }
    }
}
