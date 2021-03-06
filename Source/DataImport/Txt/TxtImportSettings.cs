using System.Collections.Generic;
using System.Data;
using DotSpatial.Controls;
using HydroDesktop.Interfaces;

namespace DataImport.Txt
{
    /// <summary>
    /// Settings for txt
    /// </summary>
    public class TxtImportSettings : IWizardImporterSettings
    {
        /// <summary>
        /// File type
        /// </summary>
        public TxtFileType FileType { get; set; }

        /// <summary>
        /// Delimiter
        /// </summary>
        public string Delimiter { get; set; }

        #region  Implementation of IWizardImporterSettings

        public string PathToFile{get; set;}
        public DataTable Preview { get; set; }
        public IList<ColumnInfo> ColumnDatas { get; set; }
        public string DateTimeColumn { get; set; }
        public DataTable Data { get; set; }
        public string ValuesNumberDecimalSeparator { get; set; }
        public int MaxProgressPercentWhenImport { get; set; }
        public ISeriesSelector SeriesSelector { get; set; }
        public IMap Map { get; set; }

        #endregion
    }
}