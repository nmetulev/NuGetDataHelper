using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetDataHelper
{

    public class NugetData
    {
        public object[] Rows { get; set; }
        public int Total { get; set; }
        public Dimension[] Dimensions { get; set; }
        public Fact[] Facts { get; set; }
        public Table[][] Table { get; set; }
        public string[] Columns { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
        public string Id { get; set; }

        public string PackageName { get; set; }
    }

    public class Dimension
    {
        public string Value { get; set; }
        public string DisplayName { get; set; }
        public bool IsChecked { get; set; }
    }

    public class Fact
    {
        public Dimensions Dimensions { get; set; }
        public int Amount { get; set; }
    }

    public class Dimensions
    {
        public string Version { get; set; }
        public string ClientName { get; set; }
        public string ClientVersion { get; set; }
        public string Operation { get; set; }
    }

    public class Table
    {
        public string Data { get; set; }
        public int Rowspan { get; set; }
        public string Uri { get; set; }
        public bool IsNumeric { get; set; }
    }

}
