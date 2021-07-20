using MRPApp.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace MRPApp.Logic
{
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

        public static int DelSettings(Settings item)
        {
            using(var ctx = new MRPEntities())
            {
                var obj = ctx.Settings.Find(item.BasicCode);
                ctx.Settings.Remove(obj); //delete
                return ctx.SaveChanges();
            }
        }

        internal static List<Schedules> GetSchedules()
        {
            List<Schedules> list;

            using (var ctx = new MRPEntities())
                list = ctx.Schedules.ToList(); //Select

            return list;
        }

        internal static int SetSchedule(Schedules item)
        {
            using (var ctx = new MRPEntities())
            {
                ctx.Schedules.AddOrUpdate(item); //insert or update
                return ctx.SaveChanges();
            }
        }

        internal static List<Process> GetProcess()
        {
            List<Process> list;

            using (var ctx = new MRPEntities())
                list = ctx.Process.ToList();

            return list;
        }

        internal static int SetProcess(Process item)
        {
            using (var ctx = new MRPEntities())
            {
                ctx.Process.AddOrUpdate(item);
                return ctx.SaveChanges();
            }
        }

        internal static List<Report> GetReportDatas(string startDate, string endDate, string plantCode)
        {
            var connString = ConfigurationManager.ConnectionStrings["MRPConnString"].ToString();
            var list = new List<Report>();
            var lastObj = new Model.Report(); // 추가 : 최종 Report값 담는 변수

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
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

                SqlCommand cmd = new SqlCommand(sqlQuery, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var tmp = new Model.Report
                    {
                        SchIdx = (int)reader["SchIdx"],
                        PlantCode = reader["PlantCode"].ToString(),
                        PrcDate = DateTime.Parse(reader["PrcDate"].ToString()),
                        SchAmount = (int)reader["SchAmount"],
                        OKAmount = (int)reader["OK_Amount"],
                        FailAmount = (int)reader["Fail_Amount"]
                    };
                    list.Add(tmp);
                    lastObj = tmp; // 마지막 값을 할당
                }
            }
            // 시작일부터 종료일까지 없는 값 만들어주는 로직
            var DtStart = DateTime.Parse(startDate);
            var DtEnd = DateTime.Parse(endDate);
            var DtCurrent = DtStart;

            while (DtCurrent < DtEnd)
            {
                var count = list.Where(c => c.PrcDate.Equals(DtCurrent)).Count();
                if (count == 0)
                {
                    // 새로운 Report(없는 날짜)
                    var tmp = new Report
                    {
                        SchIdx = lastObj.SchIdx,
                        PlantCode = lastObj.PlantCode,
                        PrcDate = DtCurrent,
                        SchAmount = 0,
                        OKAmount = 0,
                        FailAmount = 0
                    };
                    list.Add(tmp);
                }
                DtCurrent = DtCurrent.AddDays(1); // 날하루 증가
            }
            list.Sort((reportA, reportB) => reportA.PrcDate.CompareTo(reportB.PrcDate)); // 가장오래된 날짜부터 오름차순 정렬
            return list;
        }
    }
}
