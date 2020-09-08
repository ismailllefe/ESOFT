﻿using DevExpress.Utils;
using Esso.Data;
using Esso.Model.Models;
using Esso.Models;
using Esso.Web.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Esso.Web.Controllers
{
    [Authorize]
    public class StringController : BaseController
    {

        EssoEntities DB = new EssoEntities();
        // GET: String
        public ActionResult Index(int stationId)
        {
            return View(stationId);
        }


        public class TempDTO
        {
            public int ID { get; set; }
            public string NAME { get; set; }
            public int INV_NO { get; set; }
            public int INPUT_NO { get; set; }
            public float? VALUE { get; set; }
            public long TARIH_NUMBER { get; set; }
            public string DISPLAY_NAME { get; set; }
        }
        public class STR_DTO
        {
            public int ID { get; set; }
            public string NAME { get; set; }
            public DateTime date { get; set; }
            public long? dateNumber { get; set; }
            public float? VALUE { get; set; }

        }

        public ActionResult StringGridPartial(int stationId, string date, string hour)
        {
            List<TempDTO> strTagNames = DB.stationString
               .Join(DB.Tags, r => r.STRING_ID, ro => ro.ID, (r, ro) => new { r, ro })
               .Where(x => x.r.STATION_ID == stationId && x.r.DISPLAY_NAME.Contains("temp") == false && x.r.IS_DELETED == false)
              .GroupBy(x => x.r.DISPLAY_NAME)
              .Select(g => new TempDTO { NAME = g.Key, ID = g.FirstOrDefault().ro.ID })
              .ToList();

            strTagNames = strTagNames.OrderBy(x => x.NAME).ToList();

            DataTable dt = new DataTable();


            if (strTagNames == null || strTagNames.Count == 0)
            {
                return PartialView(dt);
            }


            List<string> sameTag = new List<string>();
            if (strTagNames != null)
            {
                string sck = string.Empty;


                foreach (TempDTO tag in strTagNames)
                {
                    sck = tag.NAME.Split('_')[0];


                    if (!dt.Columns.Contains(sck + " (A)"))
                    {
                        sck = sck.Replace("SCK", "DCB");

                        dt.Columns.Add("Name:" + sck, typeof(string))/*.SetOrdinal(0)*/;
                        dt.Columns.Add(sck + " (A)");

                        sameTag = strTagNames.Where(x => x.NAME.Split('_')[0] == sck).Select(x => x.NAME).ToList();

                        if (sameTag != null)
                        {
                            for (int i = 0; i <= sameTag.Count - 1; i++)
                            {
                                if (dt.Rows.Count == 0 || dt.Rows.Count < (i + 1))
                                {
                                    DataRow rw = dt.NewRow();
                                    rw["Name:" + sck] = sameTag[i];
                                    rw[sck + " (A)"] = sameTag[i];
                                    dt.Rows.Add(rw);

                                }
                                else
                                {
                                    dt.Rows[i]["Name:" + sck] = sameTag[i];
                                    dt.Rows[i][sck + " (A)"] = sameTag[i];
                                }
                            }

                        }
                    }

                }
            }

            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            DateTime curDate = DateTime.Now;

            List<STR_DTO> values = new List<STR_DTO>();
            if (hour == "")
            {
                values = (from so in DB.StringOzetLive
                          join t in DB.Tags on so.STRING_ID equals t.ID
                          join tct in DB.stationString on t.ID equals tct.STRING_ID
                          where so.STATION_ID == stationId
                          && tct.STATION_ID == stationId
                          && tct.IS_DELETED == false
                          && t.IS_DELETED == false
                          && tct.IS_DELETED == false
                          select new STR_DTO
                          {
                              NAME = tct.DISPLAY_NAME,
                              ID = t.ID,
                              date = so.INSERT_DATE.Value,
                              VALUE = so.VALUE
                          }).ToList();
                ViewBag.Date = values[0].date;
            }
            else
            {
                DateTime slcDate = DateTime.Parse(date);
                DateTime nowDate = DateTime.Now;

                if (nowDate.Year == slcDate.Year && nowDate.Month == slcDate.Month) //Quarter String
                {
                    string _date = date + " " + hour;
                    ViewBag.Date = _date;
                    string[] hourSplit = hour.Split(':');
                    var _convertDate = ConvertNumberFormatDateAndHourQuarter(date, hourSplit[0], hourSplit[1])._begin;
                    values = (from so in DB.StringOzetQuarterHourAVG
                              join t in DB.Tags on so.STRING_ID equals t.ID
                              join tct in DB.stationString on t.ID equals tct.STRING_ID
                              where so.STATION_ID == stationId && tct.STATION_ID == stationId
                              && tct.IS_DELETED == false
                              && t.IS_DELETED == false
                              && so.TARIH_NUMBER == _convertDate
                              select new STR_DTO
                              {
                                  NAME = tct.DISPLAY_NAME,
                                  ID = t.ID,
                                  VALUE = (float)Math.Round(so.VALUE, 2)
                              }
                                ).ToList();
                }
                else //Hourly String
                {
                    string _date = date + " " + hour;
                    ViewBag.Date = _date;
                    string[] hourSplit = hour.Split(':');
                    var _convertDate = ConvertNumberFormatDateAndHour(date, hourSplit[0])._begin;
                    values = (from so in DB.StringOzetAVG
                              join t in DB.Tags on so.STRING_ID equals t.ID
                              join tct in DB.stationString on t.ID equals tct.STRING_ID
                              where so.STATION_ID == stationId && tct.STATION_ID == stationId
                              && tct.IS_DELETED == false
                              && t.IS_DELETED == false
                              && so.TARIH_NUMBER == _convertDate
                              select new STR_DTO
                              {
                                  NAME = tct.DISPLAY_NAME,
                                  ID = t.ID,
                                  VALUE = (float)Math.Round(so.VALUE, 2)
                              }
                                ).ToList();
                }
            }
            if (values.Count != 0)
            {

                float? _totalDCcurrent = values.Where(w => w.VALUE != null).Sum(sm => sm.VALUE);
                ViewBag.StringDCSum = _totalDCcurrent;
                if (dt.Columns.Count > 0 && dt.Rows.Count > 0 && values != null)
                {

                    string cellTagName = string.Empty;
                    foreach (DataRow rw in dt.Rows)
                    {
                        foreach (DataColumn col in dt.Columns)
                        {
                            cellTagName = rw[col].ToString();

                            if (values.Any(x => x.NAME == cellTagName))
                            {

                                rw[col] = values.Where(x => x.NAME == cellTagName).FirstOrDefault().VALUE;

                            }
                            //else
                            //{
                            //	rw[col] = "-";
                            //}

                        }
                    }
                }
            }
            else
            {
                string cellTagName = string.Empty;
                foreach (DataRow rw in dt.Rows)
                {
                    foreach (DataColumn col in dt.Columns)
                    {

                        cellTagName = rw[col].ToString();
                        rw[col] = "-";

                    }
                }
            }
            if (values.Count != 0)
            {
                decimal _min;

                List<StringMin> _ListStringMin = new List<StringMin>();

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    _min = Convert.ToDecimal(dt.Rows[0][j]);

                    StringMin _StringMin = new StringMin();

                    _StringMin.ColumnIndex = j;
                    _StringMin.FieldName = dt.Columns[j].ColumnName;

                    try
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (i == 11)
                            {
                                var dddd = dt.Rows[i][j];
                            }
                            if (dt.Rows[i][j] != DBNull.Value)
                            {
                                if (dt.Rows[i][j].ToString() != "-")
                                {
                                    if (Convert.ToDecimal(dt.Rows[i][j]) < _min)
                                    {
                                        _min = Convert.ToDecimal(dt.Rows[i][j].ToString());
                                        _StringMin.Min = _min;
                                        _StringMin.RowIndex = i;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                    _ListStringMin.Add(_StringMin);
                }

                ViewBag.StringMin = _ListStringMin;

                // Max değeri bulma -----------------------------
                decimal _max = 0;

                List<StringDeneme> _ListStringMax = new List<StringDeneme>();

                for (int j = 1; j < dt.Columns.Count; j++)
                {
                    _max = 0;

                    StringDeneme _cStringDeneme = new StringDeneme();

                    _cStringDeneme.ColumnIndex = j;
                    _cStringDeneme.FieldName = dt.Columns[j].ColumnName;

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (dt.Rows[i][j] != DBNull.Value)
                        {
                            if (Convert.ToDecimal(dt.Rows[i][j]) > _max)
                            {
                                _max = Convert.ToDecimal(dt.Rows[i][j]);
                                _cStringDeneme.Value = _max;
                                _cStringDeneme.RowIndex = i;
                            }
                        }



                    }

                    j++;

                    _ListStringMax.Add(_cStringDeneme);
                }

                ViewBag.StringMax = _ListStringMax;

                // max bitti--------

                // Avg değeri bulma ----------------------


                List<StringAvg> ListStringAvg = new List<StringAvg>();

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    decimal count = 0, sum = 0;

                    if (dt.Columns[i].ColumnName.IndexOf("Name") == -1)
                    {
                        for (int j = 0; j < dt.Rows.Count; j++)
                        {
                            decimal _ActiveValue = 0;

                            if (dt.Rows[j][i] != DBNull.Value)
                                _ActiveValue = Convert.ToDecimal(dt.Rows[j][i]);

                            if (_ActiveValue > 0)
                            {
                                sum += _ActiveValue;

                                count++;
                            }
                        }

                        StringAvg _StrAvg = new StringAvg();

                        if (count != 0)
                            _StrAvg.Avg = decimal.Round(sum / count, 2);
                        else
                            _StrAvg.Avg = 0;



                        _StrAvg.FieldName = dt.Columns[i].ColumnName;
                        _StrAvg.ColumnIndex = i;

                        ListStringAvg.Add(_StrAvg);
                    }

                }
                ViewBag.StringAvg = ListStringAvg;

                //Inverter Toplam
                var invGroup = (from u in ListStringAvg
                                    //group u by u.FieldName.Substring(0, 4) into g
                                select new StringInvNameAvg { invName = u.FieldName, Avg = Math.Round(u.Avg, 2) })
                                .ToList();

                ViewBag.ListInvAVG = invGroup;
                // avg bitti-----------------------------------------


                foreach (TempDTO tag in strTagNames)
                {
                    string sck = string.Empty;
                    sck = tag.NAME.Split('_')[0];

                    sameTag = strTagNames.Where(x => x.NAME.Split('_')[0] == sck).Select(x => x.NAME).ToList();

                    if (sameTag != null)
                    {
                        for (int i = 0; i <= sameTag.Count - 1; i++)
                        {
                            string[] tttt = sameTag[i].Split('_');

                            dt.Rows[i]["Name:" + sck] = tttt[1] + " " + tttt[2] + "-" + tttt[3];

                        }

                    }
                }

                //TempDTO mt = strTagNames.AsEnumerable()
                //		.GroupBy(r => r.NAME.Split('_')[0])
                //		.Select(g => new TempDTO { NAME = g.Key, ID = g.Count() })//(g => new TempDTO{ NAME = g.OrderByDescending(r => r.NAME.Split('_')[0]  } )
                //		.OrderByDescending(x => x.ID).ToList().First();

                //List<string> tagCOl = strTagNames.Where(x => x.NAME.StartsWith(mt.NAME + "_")).Select(X => X.NAME).ToList();

                var aaa = strTagNames.AsEnumerable()
                        .GroupBy(r => r.NAME.Split('_')[0])
                        .Select(g => new TempDTO { NAME = g.Key, ID = g.Count() })//(g => new TempDTO{ NAME = g.OrderByDescending(r => r.NAME.Split('_')[0]  } )
                        .OrderByDescending(x => x.ID).ToList();



                List<string> listt = new List<string>();

                foreach (var a in aaa)
                {
                    List<string> tagCOl = strTagNames.Where(x => x.NAME.StartsWith(a.NAME + "_")).Select(X => X.NAME).ToList();

                    var fff = strTagNames.Where(x => x.NAME.StartsWith(a.NAME + "_")).Select(X => X.NAME).FirstOrDefault();
                    string ddd = string.Empty;
                    ddd = fff.Split('_')[0];

                    for (int i = 0; i <= 0 - 1; i++)
                    {
                        dt.Columns.Add(ddd, typeof(string)).SetOrdinal(i);


                        for (int t = 0; t <= tagCOl.Count - 1; t++)
                        {

                            if (dt.Rows.Count < t)
                            {
                                break;
                            }
                            string[] ccc = tagCOl[t].Split('_');

                            if (tagCOl[t].Contains("temp"))
                            {
                                dt.Rows[t]["Tag"] = "Temp.";
                            }
                            else
                            {

                                dt.Rows[t][ddd] = ccc[1] + " " + ccc[2] + "-" + ccc[3] /*+ " " + ccc[4]*/;

                            }

                        }
                    }
                }

            }
            //ViewData["columns"] = columns;
            #region ColumnsSums
            DataTable _dtSums = dt.Clone();

            _dtSums.Rows.Clear();

            DataRow _drSums = _dtSums.NewRow();

            for (int i = 1; i < dt.Columns.Count; i++)
            {
                if (i % 2 != 0)
                {
                    decimal _sum = 0;

                    for (int j = 0; j < dt.Rows.Count; j++)
                    {
                        if (dt.Rows[j][i] != DBNull.Value)
                        {
                            if (dt.Rows[j][i].ToString() != "-")
                            {
                                _sum += Convert.ToDecimal(dt.Rows[j][i]);
                            }

                        }

                    }

                    _drSums[i] = _sum;
                }
            }

            _dtSums.Rows.Add(_drSums);
            #endregion

            #region StandardDeviation 

            DataTable _dtSD = dt.Clone();
            _dtSD.Rows.Clear();

            DataRow _drSD = _dtSD.NewRow();

            List<StringAvg> _ListStringAvg = ViewBag.StringAvg as List<StringAvg>;

            for (int i = 1; i < dt.Columns.Count; i++)
            {
                double _value = 0;

                int n = -1;

                double _avg = 0;

                StringAvg _cAvg = _ListStringAvg.Where(x => x.FieldName == dt.Columns[i].ColumnName).FirstOrDefault();
                if (i % 2 != 0)
                {
                    if (_cAvg != null)
                    {
                        _avg = Convert.ToDouble(_cAvg.Avg);
                    }

                    for (int j = 0; j < dt.Rows.Count; j++)
                    {
                        if (dt.Rows[j][i] != DBNull.Value)
                        {
                            if (dt.Rows[j][i].ToString() != "-")
                            {
                                n++;
                                double _value2 = Convert.ToDouble(dt.Rows[j][i]) - _avg;
                                _value2 = _value2 * _value2;
                                _value += _value2;
                            }
                        }

                    }

                    _value = Math.Sqrt(_value / n);
                    _value = Math.Round(_value, 2);
                    _drSD[i] = _value;
                }
            }
            _dtSD.Rows.Add(_drSD);

            ViewBag._dtSD = _dtSD;
            #endregion
            //sveri çekilecek.
            return PartialView(dt);
        }

        public class StringDeneme
        {
            public int ColumnIndex { get; set; }
            public int RowIndex { get; set; }
            public string FieldName { get; set; }
            public decimal Value { get; set; }
        }
        public class StringAvg
        {
            public int ColumnIndex { get; set; }
            public int RowIndex { get; set; }

            public decimal Avg { get; set; }
            public string FieldName { get; set; }

        }

        public class StringMin
        {
            public int ColumnIndex { get; set; }
            public int RowIndex { get; set; }

            public decimal Min { get; set; }
            public string FieldName { get; set; }

        }
        public class StringInvNameAvg
        {
            public string invName { get; set; }
            public decimal Avg { get; set; }
        }
        public class NUMBER_FORMAT_DTO
        {
            public long _begin { get; set; }
            public long _end { get; set; }
        }
        public NUMBER_FORMAT_DTO ConvertNumberFormatDateAndHour(string date, string hour)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            DateTime selectDate = DateTime.Parse(date);
            string _convertDate = "";
            string _year = selectDate.Year.ToString();
            string _month = selectDate.Month.ToString();
            string _day = selectDate.Day.ToString();
            if (_month.Length < 2)
            {
                _month = "0" + selectDate.Month.ToString();
            }
            if (_day.Length < 2)
            {
                _day = "0" + selectDate.Day.ToString();
            }
            _convertDate = _year + _month + _day;
            string _strDateBegin = _convertDate + hour;
            NUMBER_FORMAT_DTO ndto = new NUMBER_FORMAT_DTO();
            ndto._begin = Convert.ToInt64(_strDateBegin);
            return ndto;
        }
        public NUMBER_FORMAT_DTO ConvertNumberFormatDateAndHourQuarter(string date, string hour, string minute)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            DateTime selectDate = DateTime.Parse(date);
            string _convertDate = "";
            string _year = selectDate.Year.ToString();
            string _month = selectDate.Month.ToString();
            string _day = selectDate.Day.ToString();
            if (_month.Length < 2)
            {
                _month = "0" + selectDate.Month.ToString();
            }
            if (_day.Length < 2)
            {
                _day = "0" + selectDate.Day.ToString();
            }
            _convertDate = _year + _month + _day;
            string _strDateBegin = _convertDate + hour + minute;
            NUMBER_FORMAT_DTO ndto = new NUMBER_FORMAT_DTO();
            ndto._begin = Convert.ToInt64(_strDateBegin);
            return ndto;
        }

        public JsonResult HourlyColorReport(int stationId, string slctDate)
        {
            StringPerformanceView _strModel = new StringPerformanceView();
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                DateTime date = DateTime.Parse(@slctDate);
                DateTime curDate = DateTime.Now;

                List<TempDTO> strTagNames = DB.stationString
                                          .Join(DB.Tags, r => r.STRING_ID, ro => ro.ID, (r, ro) => new { r, ro })
                                          .Where(x => x.r.STATION_ID == stationId && x.r.DISPLAY_NAME.Contains("temp") == false && x.r.IS_DELETED == false)
                                          .GroupBy(x => x.r.DISPLAY_NAME)
                                          .Select(g => new TempDTO { NAME = g.Key, ID = g.FirstOrDefault().ro.ID }).OrderBy(o => o.NAME)
                                          .ToList();
             
                _strModel.strModel = new StringModel();
                List<String_Hour_DTO> values = new List<String_Hour_DTO>();
                List<String_Hour_DTO> valuesTemp = new List<String_Hour_DTO>();
                if (curDate.Date.Year == date.Date.Year && curDate.Date.Month == date.Date.Month)
                {
                    NUMBER_FORMAT_DTO convertFormat = ConvertNumberFormatMinute(slctDate);
                    long _numberDateBegin = convertFormat._begin;
                    long _numberDateEnd = convertFormat._end;
                    valuesTemp = (from u in DB.StringOzetQuarterHourAVG
                                  join v in DB.stationString on u.STRING_ID equals v.STRING_ID
                                  join t in DB.Tags on v.STRING_ID equals t.ID
                                  where v.IS_DELETED == false
                                  && v.STATION_ID == stationId
                                  && u.STATION_ID == stationId
                                  && _numberDateBegin <= u.TARIH_NUMBER && _numberDateEnd >= u.TARIH_NUMBER
                                  orderby u.TARIH_NUMBER ascending
                                  select new String_Hour_DTO
                                  {
                                      NAME = t.NAME,
                                      DISPLAY_NAME = v.DISPLAY_NAME,
                                      ID = u.STRING_ID,
                                      TARIH_NUMBER = u.TARIH_NUMBER.Value,
                                      VALUE = u.VALUE
                                  }).ToList();

             

                    foreach (var item in valuesTemp.GroupBy(gg => gg.TARIH_NUMBER).ToList())
                    {
                        foreach (var item2 in strTagNames)
                        {
                            var vv = valuesTemp.Where(w => w.TARIH_NUMBER == item.Key && w.ID == item2.ID).FirstOrDefault();
                            values.Add(vv);
                        }
                    }
                    

                    foreach (var item in strTagNames)
                    {
                        _strModel.strModel.StringList.Add(item.NAME);
                    }

                    var valueList = new List<Values>();

                    string[] groupTime = values.GroupBy(grp => grp.TARIH_NUMBER).OrderBy(a => a.Key).Select(a => a.Key.ToString().Substring(8, 4)).ToArray();
                    int endHour = int.Parse(groupTime[groupTime.Length - 1]);
                    DateTime EndDate = DateTime.Now.Date;
                    TimeSpan tEndDate = new TimeSpan((int.Parse(groupTime[groupTime.Length - 1].Substring(0, 2))), (int.Parse(groupTime[groupTime.Length - 1].Substring(2, 2))), 0);
                    EndDate = EndDate.Date + tEndDate;

                    DateTime LastDate = EndDate;
                    TimeSpan tLastDate = new TimeSpan(21, 0, 0);
                    LastDate = LastDate.Date + tLastDate;

                    for (int i = 0; i < groupTime.Length; i++)
                    {
                        _strModel.strModel.Hours.Add(groupTime[i]);
                    }

                    while (EndDate < LastDate)
                    {
                        EndDate = EndDate.AddMinutes(15);
                        _strModel.strModel.Hours.Add(HourMinuteFormat(EndDate));
                    }


                    //VALUE SIRALAMA

                    var groupList = values.GroupBy(a => a.DISPLAY_NAME).ToList();

                    foreach (var item in groupList)
                    {
                        var listvalue = new Values();
                        foreach (var i in item.GroupBy(grp => grp.TARIH_NUMBER).OrderBy(a => a.Key).ToList())
                        {
                            listvalue.values.Add((float)Math.Round((i.Max(a => a.VALUE)),2));
                        }
                        valueList.Add(listvalue);
                    }
                    _strModel.strModel.series = valueList.ToList();
                    _strModel.ErrorMessage = "";
                }
                else
                {
                    NUMBER_FORMAT_DTO convertFormat = ConvertNumberFormatHour(slctDate);
                    long _numberDateBegin = convertFormat._begin;
                    long _numberDateEnd = convertFormat._end;
                    valuesTemp = (from u in DB.StringOzetAVG
                              join v in DB.stationString on u.STRING_ID equals v.STRING_ID
                              join t in DB.Tags on v.STRING_ID equals t.ID
                              where v.IS_DELETED == false
                              && v.STATION_ID == stationId
                              && u.STATION_ID == stationId
                              && _numberDateBegin < u.TARIH_NUMBER && _numberDateEnd > u.TARIH_NUMBER
                              orderby u.TARIH_NUMBER ascending
                              select new String_Hour_DTO
                              {
                                  NAME = t.NAME,
                                  DISPLAY_NAME = v.DISPLAY_NAME,
                                  ID = u.STRING_ID,
                                  TARIH_NUMBER = u.TARIH_NUMBER.Value,
                                  VALUE = u.VALUE
                              }).ToList();

                    foreach (var item in valuesTemp.GroupBy(gg => gg.TARIH_NUMBER).ToList())
                    {
                        foreach (var item2 in strTagNames)
                        {
                            var vv = valuesTemp.Where(w => w.TARIH_NUMBER == item.Key && w.ID == item2.ID).FirstOrDefault();
                            values.Add(vv);
                        }
                    }


                    foreach (var item in strTagNames)
                    {
                        _strModel.strModel.StringList.Add(item.NAME);
                    }

                    var valueList = new List<Values>();

                    string[] groupTime = values.GroupBy(grp => grp.TARIH_NUMBER).OrderBy(a => a.Key).Select(a => a.Key.ToString().Substring(8, 2)).ToArray();
                    int endHour = int.Parse(groupTime[groupTime.Length - 1]);
                    for (int i = 0; i < groupTime.Length; i++)
                    {
                        _strModel.strModel.Hours.Add(groupTime[i] + "00");
                    }

                    for (int i = endHour + 1; i < 21; i++)
                    {
                        if (!groupTime.Contains(i.ToString()))
                        {
                            _strModel.strModel.Hours.Add(i.ToString() + "00");
                        }
                    }
                    //VALUE SIRALAMA

                    var groupList = values.GroupBy(a => a.DISPLAY_NAME).ToList();

                    foreach (var item in groupList)
                    {
                        var listvalue = new Values();
                        foreach (var i in item.GroupBy(grp => grp.TARIH_NUMBER).OrderBy(a => a.Key).ToList())
                        {
                            listvalue.values.Add((float)Math.Round((i.Max(a => a.VALUE)), 2));
                        }
                        valueList.Add(listvalue);
                    }
                    _strModel.strModel.series = valueList.ToList();
                    _strModel.ErrorMessage = "";
                }

            }
            catch (Exception ex)
            {
                _strModel.ErrorMessage = ex.ToString();
            }

            return Json(_strModel);

        }

        public NUMBER_FORMAT_DTO ConvertNumberFormatHour(string date)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            DateTime selectDate = DateTime.Parse(date);
            string _convertDate = "";
            string _year = selectDate.Year.ToString();
            string _month = selectDate.Month.ToString();
            string _day = selectDate.Day.ToString();
            if (_month.Length < 2)
            {
                _month = "0" + selectDate.Month.ToString();
            }
            if (_day.Length < 2)
            {
                _day = "0" + selectDate.Day.ToString();
            }
            _convertDate = _year + _month + _day;
            string _strDateBegin = _convertDate + "04";
            string _strDateEnd = _convertDate + "21";
            NUMBER_FORMAT_DTO ndto = new NUMBER_FORMAT_DTO();
            ndto._begin = Convert.ToInt64(_strDateBegin);
            ndto._end = Convert.ToInt64(_strDateEnd);
            return ndto;
        }

        public NUMBER_FORMAT_DTO ConvertNumberFormatMinute(string date)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            DateTime selectDate = DateTime.Parse(date);
            string _convertDate = "";
            string _year = selectDate.Year.ToString();
            string _month = selectDate.Month.ToString();
            string _day = selectDate.Day.ToString();
            if (_month.Length < 2)
            {
                _month = "0" + selectDate.Month.ToString();
            }
            if (_day.Length < 2)
            {
                _day = "0" + selectDate.Day.ToString();
            }
            _convertDate = _year + _month + _day;
            string _strDateBegin = _convertDate + "0400";
            string _strDateEnd = _convertDate + "2100";
            NUMBER_FORMAT_DTO ndto = new NUMBER_FORMAT_DTO();
            ndto._begin = Convert.ToInt64(_strDateBegin);
            ndto._end = Convert.ToInt64(_strDateEnd);
            return ndto;
        }
        public string HourMinuteFormat(DateTime dt)
        {
            string _date = "";
            string _hour = dt.Hour.ToString();
            string _minute = dt.Minute.ToString();
            if (_hour.Length < 2)
            {
                _hour = "0" + dt.Month.ToString();
            }
            if (_minute.Length < 2)
            {
                _minute = "0" + dt.Minute.ToString();
            }
            _date = _hour + _minute;
            return _date;
        }
    }
}