using Microsoft.VisualStudio.TestTools.UnitTesting;
using MRPApp.Logic;
using MRPApp.View.Setting;
using System;
using System.Linq;

namespace MRPApp.Test
{
    [TestClass]
    public class SettingsTest
    {
        [TestMethod]
        public void IsDuplicateDataTest()
        {
            var expectVal = true;
            var inputCode = "PC010001";

            SettingList settingList = new SettingList();

            var code = DataAccess.GetSettings().Where(d => d.BasicCode.Contains(inputCode)).FirstOrDefault();
            var realVal = code != null ? true : false;

            Assert.AreEqual(expectVal, realVal);
        }
    }
}
