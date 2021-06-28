using MRPApp.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
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
    }
}
