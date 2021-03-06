using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using HydroDesktop.Common;
using HydroDesktop.Interfaces;
using HydroDesktop.Interfaces.ObjectModel;

namespace HydroDesktop.Database
{
    /// <summary>
    /// Repository for DataSeries
    /// </summary>
    class DataSeriesRepository : BaseRepository<Series>, IDataSeriesRepository
    {
        #region Constructors
        
        /// <summary>
        /// Create new instance of <see cref="DataSeriesRepository"/>
        /// </summary>
        /// <param name="dbType">The type of the database (SQLite, SQLServer, ...)</param>
        /// <param name="connectionString">The connection string</param>
        public DataSeriesRepository(DatabaseTypes dbType, string connectionString) : base(dbType, connectionString)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="DataSeriesRepository"/>
        /// </summary>
        /// <param name="db">The DbOperations object for handling the database</param>
        public DataSeriesRepository(IHydroDbOperations db)
            : base(db)
        {
        }

        #endregion

        #region Public methods

        public double GetNoDataValueForSeriesVariable(long seriesID)
        {
            var query =
                "SELECT NoDataValue FROM DataSeries LEFT JOIN Variables ON DataSeries.VariableID = Variables.VariableID WHERE SeriesID = " +
                seriesID;
            var result = DbOperations.ExecuteSingleOutput(query);
            return Convert.ToDouble(result);
        }

        public int GetVariableID(int seriesID)
        {
            var result =
                DbOperations.ExecuteSingleOutput("SELECT VariableID FROM DataSeries WHERE SeriesID = " + seriesID);
            return Convert.ToInt32(result);
        }

        public DataTable GetDetailedSeriesTable()
        {
            string sql = DetailedSeriesSQLQuery();
            sql += " ORDER BY VariableName, SiteName";
            var table = DbOperations.LoadTable("SeriesListTable", sql);
            return table;
        }

        public DataTable GetSeriesTable(string seriesDataFilter)
        {
            string sql = DetailedSeriesSQLQuery() +
                " WHERE " + seriesDataFilter;

            var table = DbOperations.LoadTable("SeriesListTable", sql);
            return table;
        }

        public DataTable GetSeriesTable(IEnumerable<int> listOfSeriesID)
        {
            var seriesIDs = listOfSeriesID.ToArray();
            var sqlIn = new StringBuilder();
            for (int i = 0; i < seriesIDs.Length; i++)
            {
                sqlIn.Append(seriesIDs[i].ToString(CultureInfo.InvariantCulture));
                if (i < seriesIDs.Length - 1)
                {
                    sqlIn.Append(",");
                }
            }
            string filter = "DataThemes.SeriesID in (" + sqlIn + ")";
            return GetSeriesTable(filter);
        }

        public DataTable GetSeriesTable(int seriesID)
        {
            return GetSeriesTable(string.Format("DataSeries.SeriesID={0}", seriesID));
        }
        
        public DataTable GetSeriesIDsWithNoDataValueTable(IEnumerable<int?> themeIDs)
        {
            var themeList = themeIDs.ToList();

            var hasNulls = themeList.Any(value => !value.HasValue);
            var notNullsFilter = new StringBuilder();
            const string separator = ", ";
            foreach (var themeID in themeList.Where(themeID => themeID.HasValue))
            {
                notNullsFilter.Append(themeID + separator);
            }
            if (notNullsFilter.Length > 0)
            {
                notNullsFilter.Remove(notNullsFilter.Length - separator.Length, separator.Length);
            }

            var query = "SELECT v.NoDataValue, ds.SeriesID " +
                        "FROM DataSeries ds INNER JOIN variables v ON ds.VariableID = v.VariableID " +
                        "LEFT JOIN DataThemes t ON ds.SeriesID = t.SeriesID " +
                        "WHERE t.ThemeID in (" + notNullsFilter + ")";
            if (hasNulls)
            {
                query += " or t.ThemeID is null";
            }

            var dtSeries = DbOperations.LoadTable("series", query);
            return dtSeries;
        }

        public IList<Series> GetAllSeriesForSite(Site mySite)
        {
            if (mySite.Id <= 0) throw new ArgumentException("The site must have a valid ID");
            Contract.EndContractBlock();

            string sqlQuery = DetailedSeriesSQLQuery();
            sqlQuery += string.Format(" WHERE DataSeries.SiteID = {0}", mySite.Id);
            DataTable tbl = DbOperations.LoadTable(sqlQuery);
            return SeriesListFromTable(tbl);
        }

        public IList<Series> GetAllSeries()
        {
            var seriesTable = GetDetailedSeriesTable();
            return SeriesListFromTable(seriesTable);
        }
        
        public IList<Series> GetSeriesBySite(Site site)
        {
            if (site.Id <= 0) throw new ArgumentException("site must have a valid ID");

            string filter = "DataSeries.SiteID = " + site.Id;
            DataTable seriesTable = GetSeriesTable(filter);
            return SeriesListFromTable(seriesTable);
        }

        public Series GetSeriesByID(long seriesID)
        {
            var seriesTable = DbOperations.LoadTable("seriesTable", "select * from DataSeries where seriesID=" + seriesID);
            if (seriesTable.Rows.Count == 0) return null;

            var series = new Series();
            DataRow seriesRow = seriesTable.Rows[0];
            series.BeginDateTime = Convert.ToDateTime(seriesRow["BeginDateTime"]);
            series.EndDateTime = Convert.ToDateTime(seriesRow["EndDateTime"]);
            series.BeginDateTimeUTC = Convert.ToDateTime(seriesRow["BeginDateTimeUTC"]);
            series.EndDateTimeUTC = Convert.ToDateTime(seriesRow["EndDatetimeUTC"]);
            series.Id = seriesID;
            series.IsCategorical = Convert.ToBoolean(seriesRow["IsCategorical"]);
            series.LastCheckedDateTime = Convert.ToDateTime(seriesRow["LastCheckedDateTime"]);
            series.UpdateDateTime = Convert.ToDateTime(seriesRow["UpdateDateTime"]);
            series.Subscribed = Convert.ToBoolean(seriesRow["Subscribed"]);
            series.ValueCount = Convert.ToInt32(seriesRow["ValueCount"]);

            int siteID = Convert.ToInt32(seriesRow["SiteID"]);

            string sqlSites = "SELECT SiteID, SiteCode, SiteName, Latitude, Longitude, Elevation_m, " +
                "VerticalDatum, LocalX, LocalY, State, County, Comments FROM Sites where SiteID = " + siteID;

            DataTable siteTable = DbOperations.LoadTable("siteTable", sqlSites);
            if (siteTable.Rows.Count == 0) return null;

            DataRow siteRow = siteTable.Rows[0];
            var newSite = new Site();
            newSite.Id = Convert.ToInt32(siteRow[0]);
            newSite.Code = Convert.ToString(siteRow[1]);
            newSite.Name = Convert.ToString(siteRow[2]);
            newSite.Latitude = Convert.ToDouble(siteRow[3]);
            newSite.Longitude = Convert.ToDouble(siteRow[4]);
            newSite.Elevation_m = Convert.ToDouble(siteRow[5]);
            newSite.VerticalDatum = Convert.ToString(siteRow[6]);
            newSite.LocalX = Convert.ToDouble(siteRow["LocalX"]);
            newSite.LocalY = Convert.ToDouble(siteRow["LocalY"]);
            series.Site = newSite;

            int variableID = Convert.ToInt32(seriesRow["VariableID"]);

            series.Variable = RepositoryFactory.Instance.Get<IVariablesRepository>().GetByKey(variableID);

            var newMethod = new Method {Id = Convert.ToInt32(seriesRow["MethodID"])};
            series.Method = newMethod;

            var newSource = new Source {Id = Convert.ToInt32(seriesRow["SourceID"])};
            series.Source = newSource;

            var newQC = new QualityControlLevel {Id = Convert.ToInt32(seriesRow["QualityControlLevelID"])};
            series.QualityControlLevel = newQC;


            return series;
        }

        public bool ExistsSeries(Site site, Variable variable)
        {
            var query = string.Format("select count(*) from DataSeries where SiteID={0} and VariableID={1}", site.Id,
                                      variable.Id);
            var res = DbOperations.ExecuteSingleOutput(query);
            return Convert.ToInt64(res) > 0;
        }

        public bool DeleteSeries(int seriesID)
        {
            var _db = DbOperations;

            string sqlTheme =
                "SELECT ThemeID from DataThemes where SeriesID = " + seriesID;
            DataTable tblTheme = _db.LoadTable("tblTheme", sqlTheme);

            //if the series belongs to multiple themes, do not delete it.
            if (tblTheme.Rows.Count != 1) return false;

            //otherwise, delete the series
            var seriesToDel = GetSeriesByID(seriesID);

            //SQL Queries
            var sqlSite = "SELECT count(SiteID) from DataSeries where SiteID = " + seriesToDel.Site.Id;
            string sqlVariable = "SELECT count(VariableID) from DataSeries where VariableID = " + seriesToDel.Variable.Id;
            string sqlSource = "SELECT count(SourceID) from DataSeries where SourceID = " + seriesToDel.Source.Id;
            string sqlMethod = "SELECT count(MethodID) from DataSeries where MethodID = " + seriesToDel.Method.Id;
            string sqlQuality = "SELECT count(QualityControlLevelID) from DataSeries where QualityControlLevelID = " + seriesToDel.QualityControlLevel.Id;

            //SQL Delete Commands
            string sqlDeleteValues = "DELETE FROM DataValues WHERE SeriesID = " + seriesID;
            string sqlDeleteSeries = "DELETE FROM DataSeries WHERE SeriesID = " + seriesID;
            string sqlDeleteSeriesTheme = "DELETE FROM DataThemes WHERE SeriesID = " + seriesID;

            string sqlDeleteSite = "DELETE FROM Sites WHERE SiteID = " + seriesToDel.Site.Id;
            string sqlDeleteVariable = "DELETE FROM Variables WHERE VariableID = " + seriesToDel.Variable.Id;
            string sqlDeleteMethod = "DELETE FROM Methods WHERE MethodID = " + seriesToDel.Method.Id;
            string sqlDeleteSource = "DELETE FROM Sources WHERE SourceID = " + seriesToDel.Source.Id;
            string sqlDeleteQuality = "DELETE FROM QualityControlLevels WHERE QualityControlLevelID = " + seriesToDel.QualityControlLevel.Id;

            //Begin Transaction
            using (var conn = _db.CreateConnection())
            {
                conn.Open();

                using (var tran = conn.BeginTransaction())
                {
                    //delete the site
                    var sitesCount = Convert.ToInt32(_db.ExecuteSingleOutput(sqlSite));
                    if (sitesCount == 1)
                    {
                        using (var cmdDeleteSite = conn.CreateCommand())
                        {
                            cmdDeleteSite.CommandText = sqlDeleteSite;
                            cmdDeleteSite.ExecuteNonQuery();
                        }
                    }

                    //delete the variable
                    var variablesCount = Convert.ToInt32(_db.ExecuteSingleOutput(sqlVariable));
                    if (variablesCount == 1)
                    {
                        using (DbCommand cmdDeleteVariable = conn.CreateCommand())
                        {
                            cmdDeleteVariable.CommandText = sqlDeleteVariable;
                            cmdDeleteVariable.ExecuteNonQuery();
                        }
                    }

                    //delete the method
                    var methodsCount = Convert.ToInt32(_db.ExecuteSingleOutput(sqlMethod));
                    if (methodsCount == 1)
                    {
                        using (DbCommand cmdDeleteMethod = conn.CreateCommand())
                        {
                            cmdDeleteMethod.CommandText = sqlDeleteMethod;
                            cmdDeleteMethod.ExecuteNonQuery();
                        }
                    }

                    //delete the source
                    var sourcesCount = Convert.ToInt32(_db.ExecuteSingleOutput(sqlSource));
                    if (sourcesCount == 1)
                    {
                        using (DbCommand cmdDeleteSource = conn.CreateCommand())
                        {
                            cmdDeleteSource.CommandText = sqlDeleteSource;
                            cmdDeleteSource.ExecuteNonQuery();
                        }
                    }

                    //delete the quality control level
                    var qualitiesCount = Convert.ToInt32(_db.ExecuteSingleOutput(sqlQuality));
                    if (qualitiesCount == 1)
                    {
                        using (DbCommand cmdDeleteQuality = conn.CreateCommand())
                        {
                            cmdDeleteQuality.CommandText = sqlDeleteQuality;
                            cmdDeleteQuality.ExecuteNonQuery();
                        }
                    }

                    //delete the data values
                    using (DbCommand cmdDeleteValues = conn.CreateCommand())
                    {
                        cmdDeleteValues.CommandText = sqlDeleteValues;
                        cmdDeleteValues.ExecuteNonQuery();
                    }

                    //finally delete the series
                    using (DbCommand cmdDeleteSeries = conn.CreateCommand())
                    {
                        cmdDeleteSeries.CommandText = sqlDeleteSeries;
                        cmdDeleteSeries.ExecuteNonQuery();
                    }
                    using (DbCommand cmdDeleteSeriesTheme = conn.CreateCommand())
                    {
                        cmdDeleteSeriesTheme.CommandText = sqlDeleteSeriesTheme;
                        cmdDeleteSeriesTheme.ExecuteNonQuery();
                    }

                    //commit transaction
                    tran.Commit();
                }
            }

            //remove seriesID from 'Selection'
            string dele2 = "DELETE from Selection WHERE SeriesID=" + seriesID;
            try
            {
                _db.ExecuteNonQuery(dele2);
            }
            catch { }
            return true;
        }

        public Tuple<DateTime, DateTime> GetDateTimes(long seriesID)
        {
            var begin = Convert.ToDateTime(DbOperations.ExecuteSingleOutput("Select BeginDateTime FROM DataSeries WHERE SeriesID = " + seriesID));
            var end = Convert.ToDateTime(DbOperations.ExecuteSingleOutput("Select EndDateTime FROM DataSeries WHERE SeriesID = " + seriesID));
            return new Tuple<DateTime, DateTime>(begin, end);
        }

        public DataTable GetUnitSiteVarForFirstSeries(long seriesID)
        {
            var sqlQuery = string.Format("SELECT UnitsName, SiteName, VariableName FROM DataSeries " +
                                          "INNER JOIN Variables ON Variables.VariableID = DataSeries.VariableID " +
                                          "INNER JOIN Units ON Variables.VariableUnitsID = Units.UnitsID " +
                                          "INNER JOIN Sites ON Sites.SiteID = DataSeries.SiteID WHERE SeriesID = {0} limit 1",
                                          seriesID);
            var seriesNameTable = DbOperations.LoadTable("table", sqlQuery);
            return seriesNameTable;
        }

        public void UpdateDataSeriesFromDataValues(long seriesID)
        {
            var SQLstring = "SELECT LocalDateTime FROM DataValues WHERE SeriesID = " + seriesID +
                            " ORDER BY LocalDateTime ASC";
            var BeginDateTime = Convert.ToDateTime(DbOperations.ExecuteSingleOutput(SQLstring),
                                                   CultureInfo.InvariantCulture);

            SQLstring = "SELECT LocalDateTime FROM DataValues WHERE SeriesID = " + seriesID +
                        " ORDER BY LocalDateTime DESC";
            var EndDateTime = Convert.ToDateTime(DbOperations.ExecuteSingleOutput(SQLstring),
                                                 CultureInfo.InvariantCulture);
            SQLstring = "SELECT DateTimeUTC FROM DataValues WHERE SeriesID = " + seriesID +
                        " ORDER BY LocalDateTime ASC";
            var BeginDateTimeUTC = Convert.ToDateTime(DbOperations.ExecuteSingleOutput(SQLstring),
                                                  CultureInfo.InvariantCulture);
            SQLstring = "SELECT DateTimeUTC FROM DataValues WHERE SeriesID = " + seriesID +
                        " ORDER BY LocalDateTime DESC";
            var EndDateTimeUTC = Convert.ToDateTime(DbOperations.ExecuteSingleOutput(SQLstring),
                                                CultureInfo.InvariantCulture);
            SQLstring = "SELECT COUNT(*) FROM DataValues WHERE SeriesID = " + seriesID;
            var ValueCount = DbOperations.ExecuteSingleOutput(SQLstring);

            SQLstring = "UPDATE DataSeries SET BeginDateTime = '" + BeginDateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', ";
            SQLstring += "EndDateTime = '" + EndDateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', ";
            SQLstring += "BeginDateTimeUTC = '" + BeginDateTimeUTC.ToString("yyyy-MM-dd HH:mm:ss") + "', ";
            SQLstring += "EndDateTimeUTC = '" + EndDateTimeUTC.ToString("yyyy-MM-dd HH:mm:ss") + "', ";
            SQLstring += "ValueCount = " + ValueCount + " WHERE SeriesID = " + seriesID;
            DbOperations.ExecuteNonQuery(SQLstring);
        }

        public string GetQualityControlLevelCode (long seriesID)
        {
            var query =
                    "SELECT QualityControlLevelCode FROM DataSeries AS d LEFT JOIN QualityControlLevels AS q ON (d.QualityControlLevelID = q.QualityControlLevelID) WHERE SeriesID = " +
                    seriesID;
            var res =  DbOperations.ExecuteSingleOutput(query);
            return Convert.ToString(res);
        }

        public long GetQualityControlLevelID(long seriesID)
        {
            var res =
                DbOperations.ExecuteSingleOutput("SELECT QualityControlLevelID FROM DataSeries WHERE SeriesID = " +
                                                 seriesID);
            return Convert.ToInt64(res);
        }

        public IList<long>  GetDataValuesIDs(long seriesID)
        {
            var query = "SELECT ValueID FROM DataValues WHERE SeriesID = " + seriesID;
            var res = DbOperations.Read(query,
                              r => r.GetInt64(0));
            return res;
        }

        public int InsertNewSeries(long sourceSeriesID, long variableID, long methodID, long qualityControlLevelID)
        {
            var newSeriesID = DbOperations.GetNextID("DataSeries", "SeriesID");
            var dt = DbOperations.LoadTable("DataSeries", "SELECT * FROM DataSeries WHERE SeriesID = " + sourceSeriesID);
            var row = dt.Rows[0];

            //Making the INSERT SQL string for the new data series
            var sqlString = new StringBuilder();
            sqlString.Append("INSERT INTO DataSeries(SeriesID, SiteID, VariableID, IsCategorical, MethodID, SourceID, ");
            sqlString.Append("QualityControlLevelID, BeginDateTime, EndDateTime, BeginDateTimeUTC, EndDateTimeUTC, ");
            sqlString.Append("ValueCount, CreationDateTime, Subscribed, UpdateDateTime, LastcheckedDateTime) Values (");
            //SeriesID value
            sqlString.Append(newSeriesID + ", ");
            //SiteID value
            sqlString.Append(Convert.ToInt64(row[1]) + ", ");
            //VariableID values
            sqlString.Append(variableID + ", ");
            //IsCategorical value
            sqlString.Append(row[3].ToString() == "True" ? "1, " : "0, ");
            //MethodID value
            sqlString.Append(methodID + ", ");
            //SourceID value
            sqlString.Append(Convert.ToInt64(row[5]) + ", ");
            //QualityControlLevelID value
            sqlString.Append(qualityControlLevelID + ", ");
            //BeginDateTime, EndDateTime, BeginDateTimeUTC and EndDateTimeUTC values
            for(int i =7; i<=10; i++)
            {
                var tempstring = Convert.ToDateTime(row[i]).ToString("yyyy-MM-dd HH:mm:ss");
                sqlString.Append("'" + tempstring + "', ");
            }
            var todaystring = DateTime.Today.ToString("yyyy-MM-dd HH:mm:ss");
            //ValueCount, CreationDateTime, Subscribed, UpdateDateTime and LastcheckedDateTime values
            sqlString.Append(row[11] + ", '" + todaystring + "', 0, '" + todaystring + "','" + todaystring +
                             "')");
            //Execute the SQL string
            DbOperations.ExecuteNonQuery(sqlString.ToString());

            return newSeriesID;
        }
    
        public void DeriveInsertAggregateDataValues(DataTable dt,
            long newSeriesID,
            DateTime currentdate, DateTime lastdate, DeriveAggregateMode mode,
            DeriveComputeMode computeMode,
            double nodatavalue, IProgressHandler progressHandler)
        {
            const string insertQuery =
                "INSERT INTO DataValues(ValueID, SeriesID, DataValue, ValueAccuracy, LocalDateTime, UtcOffset, DateTimeUtc, OffsetValue, OffsetTypeID, CensorCode, QualifierID, SampleID, FileID) " +
                "VALUES ({0}, {1}, {2}, {3}, '{4}', {5}, '{6}', {7}, {8}, '{9}', {10}, {11}, {12});";

            const int chunkLength = 400;
            var index = 0;

            while (currentdate <= lastdate)
            {
                // Save values by chunks

                var newValueID = DbOperations.GetNextID("DataValues", "ValueID");
                var query = new StringBuilder("BEGIN TRANSACTION; ");


                for (int i = 0; i <= chunkLength - 1; i++)
                {
                    double newvalue = 0.0;
                    string sqlString = string.Empty;
                    double UTC = 0.0;

                    switch (mode)
                    {
                        case DeriveAggregateMode.Daily:
                            sqlString = "LocalDateTime >= '" + currentdate.ToString(CultureInfo.InvariantCulture) + "' AND LocalDateTime <= '" +
                                        currentdate.AddDays(1).AddMilliseconds(-1).ToString(CultureInfo.InvariantCulture) + "' AND DataValue <> " +
                                        nodatavalue.ToString(CultureInfo.InvariantCulture);
                            break;
                        case DeriveAggregateMode.Monthly:
                            sqlString = "LocalDateTime >= '" + currentdate.ToString(CultureInfo.InvariantCulture) + "' AND LocalDateTime <= '" +
                                        currentdate.AddMonths(1).AddMilliseconds(-1).ToString(CultureInfo.InvariantCulture) + "' AND DataValue <> " +
                                        nodatavalue.ToString(CultureInfo.InvariantCulture);
                            break;
                        case DeriveAggregateMode.Quarterly:
                            sqlString = "LocalDateTime >= '" + currentdate.ToString(CultureInfo.InvariantCulture) +
                                        "' AND LocalDateTime <= '" +
                                        currentdate.AddMonths(3).AddMilliseconds(-1).ToString(
                                            CultureInfo.InvariantCulture) + "' AND DataValue <> " +
                                        nodatavalue.ToString(CultureInfo.InvariantCulture);
                            break;
                    }
                    try
                    {
                        switch (computeMode)
                        {
                            case DeriveComputeMode.Maximum:
                                newvalue = Convert.ToDouble(dt.Compute("Max(DataValue)", sqlString));
                                break;
                            case DeriveComputeMode.Minimum:
                                newvalue = Convert.ToDouble(dt.Compute("MIN(DataValue)", sqlString));
                                break;
                            case DeriveComputeMode.Average:
                                newvalue = Convert.ToDouble(dt.Compute("AVG(DataValue)", sqlString));
                                break;
                            case DeriveComputeMode.Sum:
                                newvalue = Convert.ToDouble(dt.Compute("Sum(DataValue)", sqlString));
                                break;
                        }

                        UTC = Convert.ToDouble(dt.Compute("AVG(UTCOffset)", sqlString));
                    }
                    catch (Exception)
                    {
                        newvalue = nodatavalue;
                    }

                    query.AppendFormat(insertQuery,
                                       newValueID + i,
                                       newSeriesID,
                                       newvalue,
                                       0,
                                       Convert.ToDateTime(dt.Rows[index]["LocalDateTime"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                       UTC.ToString(CultureInfo.InvariantCulture),
                                       currentdate.AddHours(UTC).ToString("yyyy-MM-dd HH:mm:ss"),
                                       "NULL",
                                       "NULL",
                                       "nc",
                                       "NULL",
                                       "NULL",
                                       "NULL");
                    query.AppendLine();

                    switch (mode)
                    {
                        case DeriveAggregateMode.Daily:
                            currentdate = currentdate.AddDays(1);
                            break;
                        case DeriveAggregateMode.Monthly:
                            currentdate = currentdate.AddMonths(1);
                            break;
                        case DeriveAggregateMode.Quarterly:
                            currentdate = currentdate.AddMonths(3);
                            break;

                    }

                    if (currentdate > lastdate) break;
                    index = index + 1;

                    //Report progress
                    progressHandler.ReportProgress(index - 1, null);
                }

                query.AppendLine("COMMIT;");
                DbOperations.ExecuteNonQuery(query.ToString());

                progressHandler.ReportProgress(index - 1, null);
            }
        }

        public void DeriveInsertDataValues(double A, double B, double C, double D, double E, double F,
            DataTable dt,
            long newSeriesID, long sourceSeriesID, bool isAlgebraic, IProgressHandler progressHandler)
        {
            const int chunkLength = 400;
            var nodatavalue = GetNoDataValueForSeriesVariable(newSeriesID);

            const string insertQuery =
                "INSERT INTO DataValues(ValueID, SeriesID, DataValue, ValueAccuracy, LocalDateTime, UtcOffset, DateTimeUtc, OffsetValue, OffsetTypeID, CensorCode, QualifierID, SampleID, FileID) " +
                "VALUES ({0}, {1}, {2}, {3}, '{4}', {5}, '{6}', {7}, {8}, '{9}', {10}, {11}, {12});";

            var index = 0;
            while (index != dt.Rows.Count - 1)
            {
                //Save values by chunks       

                var newValueID = DbOperations.GetNextID("DataValues", "ValueID");
                var query = new StringBuilder("BEGIN TRANSACTION; ");


                for (int i = 0; i < chunkLength; i++)
                {
                    // Calculating value
                    double newvalue = 0.0;
                    if (isAlgebraic)
                    {
                        var currentvalue = Convert.ToDouble(dt.Rows[index]["DataValue"]);
                        if (currentvalue != nodatavalue)
                        {

                            //NOTE:Equation = Fx ^ 5 + Ex ^ 4 + Dx ^ 3 + Cx ^ 2 + Bx + A
                            newvalue = (F*(Math.Pow(currentvalue, 5))) + (E*(Math.Pow(currentvalue, 4))) +
                                       (D*(Math.Pow(currentvalue, 3))) + (C*(Math.Pow(currentvalue, 2))) +
                                       (B*currentvalue) +
                                       A;
                            newvalue = Math.Round(newvalue, 5);
                        }
                        else
                        {
                            newvalue = nodatavalue;
                        }

                    }
                    else
                    {
                        newvalue = Convert.ToDouble(dt.Rows[index]["DataValue"]);
                    }

                    var row = dt.Rows[index];
                    query.AppendFormat(insertQuery,
                                       newValueID + i,
                                       newSeriesID,
                                       newvalue,
                                       row["ValueAccuracy"].ToString() == "" ? "NULL" : row["ValueAccuracy"].ToString(),
                                       Convert.ToDateTime(row["LocalDateTime"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                       row["UTCOffset"].ToString(),
                                       Convert.ToDateTime(row["DateTimeUTC"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                       row["OffsetValue"].ToString() == "" ? "NULL" : row["OffsetValue"].ToString(),
                                       row["OffsetTypeID"].ToString() == "" ? "NULL" : row["OffsetTypeID"].ToString(),
                                       row["CensorCode"].ToString(),
                                       row["QualifierID"].ToString() == "" ? "NULL" : row["QualifierID"].ToString(),
                                       row["SampleID"].ToString() == "" ? "NULL" : row["SampleID"].ToString(),
                                       row["FileID"].ToString() == "" ? "NULL" : row["FileID"].ToString());
                    query.AppendLine();

                    if (index == dt.Rows.Count - 1) break;
                    index = index + 1;
                }

                query.AppendLine("COMMIT;");
                DbOperations.ExecuteNonQuery(query.ToString());

                progressHandler.ReportProgress(index, null);
            }
        }


        #endregion

        #region Private methods

        private string DetailedSeriesSQLQuery()
        {
            const string sql = "SELECT DataSeries.SeriesID, " +
                               "DataThemes.ThemeID, DataSeries.SiteID, DataSeries.VariableID, DataSeries.MethodID, DataSeries.SourceID, DataSeries.QualityControlLevelID, " +
                               "SiteName, SiteCode, Latitude, Longitude, " +
                               "VariableName, VariableCode, DataType, ValueType, Speciation, SampleMedium, " +
                               "TimeSupport, GeneralCategory, NoDataValue, " +
                               "units1.UnitsName as 'VariableUnitsName', units2.UnitsName as 'TimeUnitsName', " +
                               "MethodDescription, " +
                               "SourceDescription, Organization, Citation, " +
                               "QualityControlLevelCode, Definition as 'QualityControlLevelDefinition', " +
                               "BeginDateTime, EndDateTime, BeginDateTimeUTC, EndDateTimeUTC, ValueCount, ThemeName " +
                               "FROM DataSeries " +
                               "LEFT JOIN DataThemes ON DataThemes.SeriesID = DataSeries.SeriesID " +
                               "LEFT JOIN DataThemeDescriptions ON DataThemes.ThemeID = DataThemeDescriptions.ThemeID " +
                               "LEFT JOIN Sites ON DataSeries.SiteID = Sites.SiteID " +
                               "LEFT JOIN Variables ON DataSeries.VariableID = Variables.VariableID " +
                               "LEFT JOIN Units units1 ON Variables.VariableUnitsID = units1.UnitsID " +
                               "LEFT JOIN Units units2 ON Variables.TimeUnitsID = units2.UnitsID " +
                               "LEFT JOIN Methods  ON DataSeries.MethodID = Methods.MethodID " +
                               "LEFT JOIN Sources ON DataSeries.SourceID = Sources.SourceID " +
                               "LEFT JOIN QualityControlLevels ON DataSeries.QualityControlLevelID = QualityControlLevels.QualityControlLevelID ";

            return sql;
        }

        private IList<Series> SeriesListFromTable(DataTable seriesTable)
        {
            return (from DataRow row in seriesTable.Rows select CreateSeriesFromSeriesRow(row)).ToList();
        }

        private Series CreateSeriesFromSeriesRow(DataRow row)
        {
            var st = new Site();
            st.Id = (long)row["SiteID"];
            st.Latitude = (double)row["Latitude"];
            st.Longitude = (double)row["Longitude"];
            st.Name = (string)row["SiteName"];
            st.Code = (string)row["SiteCode"];

            var timeUnit = Unit.UnknownTimeUnit;
            timeUnit.Name = (string)row["TimeUnitsName"];

            var variableUnit = Unit.Unknown;
            variableUnit.Abbreviation = (string)row["VariableUnitsName"];

            var v = new Variable();
            v.Id = (long)row["VariableID"];
            v.Name = (string)row["VariableName"];
            v.Code = (string)row["VariableCode"];
            v.DataType = (string)row["DataType"];
            v.ValueType = (string)row["ValueType"];
            v.Speciation = row["Speciation"] == DBNull.Value ? null : (string)row["Speciation"];
            v.SampleMedium = row["SampleMedium"] == DBNull.Value ? null : (string)row["SampleMedium"];
            v.TimeSupport = row["TimeSupport"] == DBNull.Value ? 0 : (double)row["TimeSupport"];
            v.VariableUnit = variableUnit;
            v.TimeUnit = timeUnit;
            v.GeneralCategory = row["GeneralCategory"] == DBNull.Value ? null : (string)row["GeneralCategory"];
            v.NoDataValue = (double)row["NoDataValue"];

            var m = new Method {Description = (string) row["MethodDescription"]};

            var src = Source.Unknown;
            src.Description = (string)row["SourceDescription"];
            src.Citation = (string)row["Citation"];
            src.Organization = (string)row["Organization"];

            var qc = QualityControlLevel.Unknown;
            qc.Code = (string)row["QualityControlLevelCode"];
            qc.Definition = (string)row["QualityControlLevelDefinition"];

            var ser = new Series(st, v, m, qc, src);
            ser.BeginDateTime = Convert.ToDateTime(row["BeginDateTime"]);
            ser.EndDateTime = Convert.ToDateTime(row["EndDateTime"]);
            ser.BeginDateTimeUTC = Convert.ToDateTime(row["BeginDateTimeUTC"]);
            ser.EndDateTimeUTC = Convert.ToDateTime(row["EndDateTimeUTC"]);
            ser.ValueCount = Convert.ToInt32(row["ValueCount"]);
            ser.Id = (long)row["SeriesID"];

            return ser;
        }

        #endregion

        protected override string TableName
        {
            get { return "DataSeries"; }
        }
    }
}