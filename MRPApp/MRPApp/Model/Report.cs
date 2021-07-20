using System;

namespace MRPApp.Model
{
    public class Report
    {
        public int SchIdx { get; set; }

        public string PlantCode { get; set; }

        public DateTime PrcDate { get; set; }

        public Nullable<int> SchAmount { get; set; }

        public Nullable<int> OKAmount { get; set; }

        public Nullable<int> FailAmount { get; set; }
    }
}
