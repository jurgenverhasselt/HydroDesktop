using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using HydroDesktop.Common;
using HydroDesktop.Interfaces.ObjectModel;
using ImportFromWaterML;

namespace DataImport.WaterML
{
    class WaterMLImporterImpl : IImporter
    {
        public IList<Series> Import(IImporterSettings settings)
        {
            var wmlSettings = (WaterMLImportSettings) settings;

            var objDownloader = new Downloader();
            string fileName = wmlSettings.PathToFile;
            var themeName = wmlSettings.ThemeName;

            string shortFileName = Path.GetFileNameWithoutExtension(fileName);
            string siteName = shortFileName;
            string variableName = shortFileName;

            int separatorIndex = shortFileName.IndexOf("_", System.StringComparison.Ordinal);
            if (separatorIndex > 0 && separatorIndex < shortFileName.Length - 1)
            {
                siteName = shortFileName.Substring(0, shortFileName.IndexOf("_"));
                variableName = shortFileName.Substring(shortFileName.IndexOf("_"));
            }


            IList<Series> seriesList = objDownloader.DataSeriesFromXml(fileName);
            if (seriesList == null)
            {
                MessageBox.Show("error converting xml file");
            }
            else
            {
                if (objDownloader.ValidateSeriesList(seriesList))
                {
                    foreach (var series in seriesList)
                    {
                        objDownloader.SaveDataSeries(series, themeName, siteName, variableName);
                    }
                }
                else
                {
                    seriesList = null;
                }
            }

            if (seriesList == null)
                return new List<Series>(0);

            return seriesList;
        }

        public IProgressHandler ProgressHandler
        {
            get;set;
        }
    }
}