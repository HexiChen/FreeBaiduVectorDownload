using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Collections.Generic;
using System.Data.OleDb;
using Npgsql;
using System.Web;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using OSGeo.OGR;

namespace DataProcess
{
    public partial class 小牛POI抓取工具 : Form
    {
        public string origson = "";

        public 小牛POI抓取工具()
        {
            InitializeComponent();
            //设置默认下载百度地图的POI
            cbox_maptype.SelectedIndex = 0;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //net2.0以后拒绝多线程访问空间，避免空间造成死锁。以前Control.CheckForIllegalCrossThreadCalls =false;默认就是这样，现在默认为true。
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void 地图POI抓取工具_Load(object sender, EventArgs e)
        {
            this.tbox_keyword.Text = System.Environment.CurrentDirectory + "\\conf\\KeyWord.txt";
            this.tbox_txt.Text = System.Environment.CurrentDirectory + "\\download\\PoiDownload.txt";
            this.tbox_poi.Text = System.Environment.CurrentDirectory + "\\download\\PoiDownload.txt";
            this.tbox_shpPath.Text = System.Environment.CurrentDirectory + "\\shp";
            this.tbox_folder.Text = System.Environment.CurrentDirectory + "\\shp";
            this.tbox_townFolder.Text = System.Environment.CurrentDirectory + "\\shp";
            this.tbox_buildPath.Text = System.Environment.CurrentDirectory + "\\shp";
            this.cbox_province.SelectedIndex = 0;
            this.tbox_roadPath.Text = System.Environment.CurrentDirectory + "\\shp";
            this.tbox_schoolPath.Text = System.Environment.CurrentDirectory + "\\shp";
            this.cbox_buildcoordinate.SelectedIndex = 0;
            this.cbox_roadcoordinate.SelectedIndex = 0;
            this.cbox_schoolcoordinate.SelectedIndex = 0;

            //StreamReader sr = new StreamReader(tbox_keyword.Text, Encoding.Default);
            //string line = sr.ReadLine();
            //sr.Close();
            //char[] separator = { '、' };
            //string[] keyword = line.Split(separator);
            //for (int i = 0; i < keyword.Count(); i++)
            //{
            //    cbox_keyword.Items.Add(keyword[i]);
            //}
            //cbox_keyword.SelectedIndex = 0;
        }

        private void btn_txt_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Title = "选择文件";
            ofd.Filter = "txt文件(*.txt)|*.txt";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbox_txt.Text = ofd.FileName;
            }
            else
                return;
        }

        private void btn_choosekeyword_Click(object sender, EventArgs e)
        {
            //System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            //ofd.Title = "选择文件";
            //ofd.Filter = "txt文件(*.txt)|*.txt";
            //string orifile = tbox_keyword.Text;
            //if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    tbox_keyword.Text = ofd.FileName;
            //    if (tbox_keyword.Text.Contains(orifile))
            //    { }
            //    else
            //    {
            //        cbox_keyword.Items.Clear();
            //        StreamReader sr = new StreamReader(tbox_keyword.Text, Encoding.Default);
            //        string line = sr.ReadLine();
            //        sr.Close();
            //        char[] separator = { '、' };
            //        string[] keyword = line.Split(separator);
            //        for (int i = 0; i < keyword.Count(); i++)
            //        {
            //            cbox_keyword.Items.Add(keyword[i]);
            //        }
            //        cbox_keyword.SelectedIndex = 0;
            //    }
            //}
            //else
            //    return;

        }

        public void downloadPoiThread(object threadnum)
        {
            //计算出第threadNum个线程需要爬取得关键字列表downloadKeyWorkArr
            //线程总数
            int threadCount = int.Parse(tbox_poiThreadCount.Text);
            //当前线程序号
            int threadNum = (int)threadnum;
            StreamReader sr = new StreamReader(tbox_keyword.Text, Encoding.Default);
            string line = sr.ReadLine();
            sr.Close();
            char[] separator = { '、' };
            string[] keywordArr = line.Split(separator);
            int keywordArrCount = keywordArr.Count();
            int step = keywordArrCount / threadCount + 1;
            ArrayList downloadKeyWordList = new ArrayList();
            //if (threadNum == threadCount - 1)
            //{
            //    int startindex = threadNum * step;
            //    for (int i = 0; i < (keywordArrCount - (threadCount-1)*step); i++)
            //    {
            //        downloadKeyWordList.Add(keywordArr[startindex + i]);
            //    }
            //}
            //else
            //{
                int startindex = threadNum * step;
                for (int i = 0; (i < step) && (startindex + i < keywordArrCount); i++)
                {
                    downloadKeyWordList.Add(keywordArr[startindex + i]);
                }

            //}

            //

            //创建临时txt文件用于存储爬取下来的POI
            string txtpath = System.Environment.CurrentDirectory + "\\conf\\tempfiles\\poi" + threadNum + ".txt";
            FileStream newfile = File.Create(txtpath);
            newfile.Close();
            StreamWriter writer = new StreamWriter(txtpath, true);



            if (cbox_maptype.SelectedIndex == 0)
            {
                //选择的是百度地图
                if (cbox_coordinates.SelectedIndex == 0)
                {
                    //选择的是百度经纬度坐标系
                    //读取城市代码txt文件，确定对应的代码
                    string citycode = File.ReadAllText(System.Environment.CurrentDirectory + "\\conf\\BaiDuCityCode.txt", Encoding.UTF8);
                    List<string> codeList = citycode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    List<string> citynameList = new List<string>();
                    List<string> citycodeList = new List<string>();
                    foreach (string item in codeList)
                    {
                        StringBuilder sb = new StringBuilder();
                        string[] iteminfo = item.Split('|');
                        citynameList.Add(iteminfo[0]);
                        citycodeList.Add(iteminfo[1]);
                    }
                    string cityname = tbox_cityname.Text;
                    char[] citynameseparator = { '、', '，', ',', '；', ';' };
                    string[] citynamearray = cityname.Split(citynameseparator);
                    if (tbox_txt.Text == "")
                        return;
                    else
                    {
                        for (int m = 0; m < citynamearray.Count(); m++)
                        {
                            if (citynameList.Contains(citynamearray[m]))
                            {
                                int index = citynameList.IndexOf(citynamearray[m]);
                                string code = citycodeList[index].ToString();        //获得城市编号

                                for (int i = 0; i < downloadKeyWordList.Count; i++)
                                {
                                    string curtag = (string)downloadKeyWordList[i];
                                    for (int pagenum = 0; pagenum < 500; pagenum++)
                                    {
                                        string url = "http://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=" + HttpUtility.UrlDecode(curtag).ToUpper() + "&c=" + code + "&pn=" + pagenum;

                                        string jsonString = "";
                                        try
                                        {
                                            jsonString = httpTools.GetBDPage(url).Trim();
                                        }

                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("在关键字 " + downloadKeyWordList[i].ToString() + " 中断！请确保城市名称正确！");
                                            return;
                                        }

                                        if (jsonString.Contains("content") && jsonString.Contains("addr") && (jsonString.Contains("more_city") == false))
                                        {
                                            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                            if (jo["content"][0]["uid"].ToString().Contains("null"))
                                            {
                                                //跳到下一个关键字
                                                break;
                                            }
                                            else
                                            {
                                                if (jo["content"][0].ToString().Contains("name") && jo["content"][0].ToString().Contains("addr") && jo["content"][0].ToString().Contains("x") && jo["content"][0].ToString().Contains("y") && jo["content"][0].ToString().Contains("uid"))
                                                {
                                                    for (int k = 0; k < jo["content"].Count(); k++)
                                                    {
                                                        //提取需要的信息
                                                        try
                                                        {
                                                            string name = jo["content"][k]["name"].ToString();
                                                            string address = jo["content"][k]["addr"].ToString();
                                                            string lon = jo["content"][k]["x"].ToString();
                                                            string lat = jo["content"][k]["y"].ToString();
                                                            string uid = jo["content"][k]["uid"].ToString();
                                                            double[] lonlat = CoordinatesConver.Mercator2BD09(double.Parse(lon) / 100, double.Parse(lat) / 100);
                                                            string all = name + "|" + address + "|" + lonlat[1] + "|" + lonlat[0] + "|" + uid + "|" + curtag;
                                                            if (all.Contains("张湾村二区36号楼")) //百度地图总是返回这个值
                                                            { }
                                                            else
                                                            {
                                                                Console.WriteLine("城市 " + citynamearray[m] + " 关键字 " + downloadKeyWordList[i].ToString() + " : " + all);
                                                                Console.WriteLine();
                                                                writer.WriteLine(all);
                                                                writer.Flush();
                                                            }
                                                        }
                                                        catch { }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //表明这个关键字已经抓完，跳到下一个关键字
                                            break;
                                        }

                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show(citynamearray[m] + " 名称设置错误!请输入正确的城市名称。");
                            }
                        }
                    }
                }


                if (cbox_coordinates.SelectedIndex == 1)
                {
                    //选择的是百度米制经纬度坐标系
                    //读取城市代码txt文件，确定对应的代码
                    string citycode = File.ReadAllText(System.Environment.CurrentDirectory + "\\conf\\BaiDuCityCode.txt", Encoding.UTF8);
                    List<string> codeList = citycode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    List<string> citynameList = new List<string>();
                    List<string> citycodeList = new List<string>();
                    foreach (string item in codeList)
                    {
                        StringBuilder sb = new StringBuilder();
                        string[] iteminfo = item.Split('|');
                        citynameList.Add(iteminfo[0]);
                        citycodeList.Add(iteminfo[1]);
                    }

                    string cityname = tbox_cityname.Text;
                    char[] citynameseparator = { '、', '，', ',', '；', ';' };
                    string[] citynamearray = cityname.Split(citynameseparator);
                    if (tbox_txt.Text == "")
                        return;
                    else
                    {
                        for (int m = 0; m < citynamearray.Count(); m++)
                        {
                            if (citynameList.Contains(citynamearray[m]))
                            {
                                int index = citynameList.IndexOf(citynamearray[m]);
                                string code = citycodeList[index].ToString();        //获得城市编号

                                for (int i = 0; i < downloadKeyWordList.Count; i++)
                                {
                                    string curtag = (string)downloadKeyWordList[i];
                                    for (int pagenum = 0; pagenum < 500; pagenum++)
                                    {
                                        string url = "http://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=" + HttpUtility.UrlDecode(curtag).ToUpper() + "&c=" + code + "&pn=" + pagenum;

                                        string jsonString = "";
                                        try
                                        {
                                            jsonString = httpTools.GetBDPage(url).Trim();
                                        }

                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("在关键字 " + downloadKeyWordList[i].ToString() + " 中断！请确保城市名称正确！");
                                            return;
                                        }

                                        if (jsonString.Contains("content") && jsonString.Contains("addr") && (jsonString.Contains("more_city") == false))
                                        {
                                            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                            if (jo["content"][0]["uid"].ToString().Contains("null"))
                                            {
                                                //跳到下一个关键字
                                                break;
                                            }
                                            else
                                            {
                                                if (jo["content"][0].ToString().Contains("name") && jo["content"][0].ToString().Contains("addr") && jo["content"][0].ToString().Contains("x") && jo["content"][0].ToString().Contains("y") && jo["content"][0].ToString().Contains("uid"))
                                                {
                                                    for (int k = 0; k < jo["content"].Count(); k++)
                                                    {
                                                        //提取需要的信息
                                                        try
                                                        {
                                                            string name = jo["content"][k]["name"].ToString();
                                                            string address = jo["content"][k]["addr"].ToString();
                                                            string lon = jo["content"][k]["x"].ToString();
                                                            string lat = jo["content"][k]["y"].ToString();
                                                            string uid = jo["content"][k]["uid"].ToString();
                                                            string all = name + "|" + address + "|" + double.Parse(lat) / 100 + "|" + double.Parse(lon) / 100 + "|" + uid + "|" + curtag;
                                                            if (all.Contains("张湾村二区36号楼")) //百度地图总是返回这个值
                                                            { }
                                                            else
                                                            {
                                                                Console.WriteLine("城市 " + citynamearray[m] + " 关键字 " + downloadKeyWordList[i].ToString() + " : " + all);
                                                                Console.WriteLine();
                                                                writer.WriteLine(all);
                                                                writer.Flush();
                                                            }
                                                        }
                                                        catch { }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //表明这个关键字已经抓完，跳到下一个关键字
                                            break;
                                        }

                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show(citynamearray[m] + " 名称设置错误!请输入正确的城市名称。");

                            }
                        }
                    }
                }
                if (cbox_coordinates.SelectedIndex == 2)
                {
                    //选择的是国测局经纬度坐标系
                    //读取城市代码txt文件，确定对应的代码
                    string citycode = File.ReadAllText(System.Environment.CurrentDirectory + "\\conf\\BaiDuCityCode.txt", Encoding.UTF8);
                    List<string> codeList = citycode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    List<string> citynameList = new List<string>();
                    List<string> citycodeList = new List<string>();
                    foreach (string item in codeList)
                    {
                        StringBuilder sb = new StringBuilder();
                        string[] iteminfo = item.Split('|');
                        citynameList.Add(iteminfo[0]);
                        citycodeList.Add(iteminfo[1]);
                    }

                    string cityname = tbox_cityname.Text;
                    char[] citynameseparator = { '、', '，', ',', '；', ';' };
                    string[] citynamearray = cityname.Split(citynameseparator);
                    if (tbox_txt.Text == "")
                        return;
                    else
                    {
                        for (int m = 0; m < citynamearray.Count(); m++)
                        {
                            if (citynameList.Contains(citynamearray[m]))
                            {
                                int index = citynameList.IndexOf(citynamearray[m]);
                                string code = citycodeList[index].ToString();        //获得城市编号

                                for (int i = 0; i < downloadKeyWordList.Count; i++)
                                {
                                    string curtag = (string)downloadKeyWordList[i];
                                    for (int pagenum = 0; pagenum < 500; pagenum++)
                                    {
                                        string url = "http://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=" + HttpUtility.UrlDecode(curtag).ToUpper() + "&c=" + code + "&pn=" + pagenum;

                                        string jsonString = "";
                                        try
                                        {
                                            jsonString = httpTools.GetBDPage(url).Trim();
                                        }

                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("在关键字 " + downloadKeyWordList[i].ToString() + " 中断！请确保城市名称正确！");
                                            return;
                                        }

                                        if (jsonString.Contains("content") && jsonString.Contains("addr") && (jsonString.Contains("more_city") == false))
                                        {
                                            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                            if (jo["content"][0]["uid"].ToString().Contains("null"))
                                            {
                                                //跳到下一个关键字
                                                break;
                                            }
                                            else
                                            {
                                                if (jo["content"][0].ToString().Contains("name") && jo["content"][0].ToString().Contains("addr") && jo["content"][0].ToString().Contains("x") && jo["content"][0].ToString().Contains("y") && jo["content"][0].ToString().Contains("uid"))
                                                {
                                                    for (int k = 0; k < jo["content"].Count(); k++)
                                                    {
                                                        //提取需要的信息
                                                        try
                                                        {
                                                            string name = jo["content"][k]["name"].ToString();
                                                            string address = jo["content"][k]["addr"].ToString();
                                                            string lon = jo["content"][k]["x"].ToString();
                                                            string lat = jo["content"][k]["y"].ToString();
                                                            string uid = jo["content"][k]["uid"].ToString();
                                                            double[] bdlonlat = CoordinatesConver.Mercator2BD09(double.Parse(lon) / 100, double.Parse(lat) / 100);
                                                            double[] gcjlatlon = CoordinatesConver.Bd09_To_Gcj02(bdlonlat[1], bdlonlat[0]);
                                                            string all = name + "|" + address + "|" + gcjlatlon[0] + "|" + gcjlatlon[1] + "|" + uid + "|" + curtag;
                                                            if (all.Contains("张湾村二区36号楼")) //百度地图总是返回这个值
                                                            { }
                                                            else
                                                            {
                                                                Console.WriteLine("城市 " + citynamearray[m] + " 关键字 " + downloadKeyWordList[i].ToString() + " : " + all);
                                                                Console.WriteLine();
                                                                writer.WriteLine(all);
                                                                writer.Flush();
                                                            }
                                                        }
                                                        catch { }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //表明这个关键字已经抓完，跳到下一个关键字
                                            break;
                                        }

                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show(citynamearray[m] + " 名称设置错误!请输入正确的城市名称。");
                            }
                        }
                    }

                }
                if (cbox_coordinates.SelectedIndex == 3)
                {
                    //选择的是地球经纬度坐标系
                    //读取城市代码txt文件，确定对应的代码
                    string citycode = File.ReadAllText(System.Environment.CurrentDirectory + "\\conf\\BaiDuCityCode.txt", Encoding.UTF8);
                    List<string> codeList = citycode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    List<string> citynameList = new List<string>();
                    List<string> citycodeList = new List<string>();
                    foreach (string item in codeList)
                    {
                        StringBuilder sb = new StringBuilder();
                        string[] iteminfo = item.Split('|');
                        citynameList.Add(iteminfo[0]);
                        citycodeList.Add(iteminfo[1]);
                    }

                    string cityname = tbox_cityname.Text;
                    char[] citynameseparator = { '、', '，', ',', '；', ';' };
                    string[] citynamearray = cityname.Split(citynameseparator);
                    if (tbox_txt.Text == "")
                        return;
                    else
                    {
                        for (int m = 0; m < citynamearray.Count(); m++)
                        {
                            if (citynameList.Contains(citynamearray[m]))
                            {
                                int index = citynameList.IndexOf(citynamearray[m]);
                                string code = citycodeList[index].ToString();        //获得城市编号

                                for (int i = 0; i < downloadKeyWordList.Count; i++)
                                {
                                    string curtag = (string)downloadKeyWordList[i];
                                    for (int pagenum = 0; pagenum < 500; pagenum++)
                                    {
                                        string url = "http://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=" + HttpUtility.UrlDecode(curtag).ToUpper() + "&c=" + code + "&pn=" + pagenum;

                                        string jsonString = "";
                                        try
                                        {
                                            jsonString = httpTools.GetBDPage(url).Trim();
                                        }

                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("在关键字 " + downloadKeyWordList[i].ToString() + " 中断！请确保城市名称正确！");
                                            return;
                                        }

                                        if (jsonString.Contains("content") && jsonString.Contains("addr") && (jsonString.Contains("more_city") == false))
                                        {
                                            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                            if (jo["content"][0]["uid"].ToString().Contains("null"))
                                            {
                                                //跳到下一个关键字
                                                break;
                                            }
                                            else
                                            {
                                                if (jo["content"][0].ToString().Contains("name") && jo["content"][0].ToString().Contains("addr") && jo["content"][0].ToString().Contains("x") && jo["content"][0].ToString().Contains("y") && jo["content"][0].ToString().Contains("uid"))
                                                {
                                                    for (int k = 0; k < jo["content"].Count(); k++)
                                                    {
                                                        //提取需要的信息
                                                        try
                                                        {
                                                            string name = jo["content"][k]["name"].ToString();
                                                            string address = jo["content"][k]["addr"].ToString();
                                                            string lon = jo["content"][k]["x"].ToString();
                                                            string lat = jo["content"][k]["y"].ToString();
                                                            string uid = jo["content"][k]["uid"].ToString();
                                                            double[] bdlonlat = CoordinatesConver.Mercator2BD09(double.Parse(lon) / 100, double.Parse(lat) / 100);
                                                            double[] wgs84latlon = CoordinatesConver.Bd09_To_Wgs84(bdlonlat[1], bdlonlat[0]);
                                                            string all = name + "|" + address + "|" + wgs84latlon[0] + "|" + wgs84latlon[1] + "|" + uid + "|" + curtag;
                                                            if (all.Contains("张湾村二区36号楼")) //百度地图总是返回这个值
                                                            { }
                                                            else
                                                            {
                                                                Console.WriteLine("城市 " + citynamearray[m] + " 关键字 " + downloadKeyWordList[i].ToString() + " : " + all);
                                                                Console.WriteLine();
                                                                writer.WriteLine(all);
                                                                writer.Flush();
                                                            }
                                                        }
                                                        catch { }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //表明这个关键字已经抓完，跳到下一个关键字
                                            break;
                                        }

                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show(citynamearray[m] + " 名称设置错误!请输入正确的城市名称。");
                            }
                        }
                    }
                }
            }
            if (cbox_maptype.SelectedIndex == 1)
            {
                //选择的是搜狗地图
                if (cbox_coordinates.SelectedIndex == 0)
                {
                    //选择的是搜狗米制墨卡托投影坐标
                    //搜狗地图
                    int maxpoicount = 50;        //每个关键字每页返回50条数据，搜狗地图最大只支持50条。为了降低请求次数，让每次返回数据达到阈值。
                    int maxpagenum = 10;         //每个关键字最多返回10页 X 50条 = 500 条数据
                    int maxrequestcount = 2000;  //没申请clientid之前每天最多请求2000次，这个还没测试。不一定正确。

                    string cityname = tbox_cityname.Text;
                    char[] separatorcity = { '、', '，', ',', '；', ';' };
                    string[] citynamearray = cityname.Split(separatorcity);
                    if (tbox_txt.Text == "")
                        return;
                    else
                    {
                        for (int m = 0; m < citynamearray.Count(); m++)
                        {
                            for (int j = 0; j < downloadKeyWordList.Count; j++)
                            {
                                for (int i = 1; i <= maxpagenum; i++)
                                {
                                    string url = "http://api.go2map.com/engine/api/search/json?what=keyword:" + HttpUtility.UrlEncode(downloadKeyWordList[j].ToString()).ToUpper() + "&range=city:" + HttpUtility.UrlDecode(citynamearray[m]).ToUpper() + "&pageinfo=" + i.ToString() + ",50";

                                    string jsonString = "";
                                    try
                                    {
                                        jsonString = httpTools.GetSGPage(url).Trim();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("在关键字 " + downloadKeyWordList[j].ToString() + " 中断！请确保城市名称正确！");
                                        return;
                                    }

                                    if (jsonString.Contains("每分钟访问次数"))
                                    {
                                        Thread.Sleep(2000);
                                        jsonString = httpTools.GetSGPage(url).Trim();
                                    }

                                    if (jsonString.Contains("error"))
                                    {
                                        break;
                                    }

                                    JObject jo = new JObject();
                                    try
                                    {
                                        jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                        for (int k = 0; k < jo["response"]["data"]["feature"].Count(); k++)
                                        {
                                            string name = jo["response"]["data"]["feature"][k]["caption"].ToString();
                                            string address = jo["response"]["data"]["feature"][k]["detail"]["address"].ToString();
                                            string lon = jo["response"]["data"]["feature"][k]["bounds"]["maxx"].ToString();
                                            string lat = jo["response"]["data"]["feature"][k]["bounds"]["maxy"].ToString();
                                            string id = jo["response"]["data"]["feature"][k]["id"].ToString();
                                            string all = name + "|" + address + "|" + lon + "|" + lat + "|" + id + "|" + downloadKeyWordList[j].ToString();
                                            Console.WriteLine("城市 " + citynamearray[m] + " 关键字 " + downloadKeyWordList[j].ToString() + " : " + all);
                                            Console.WriteLine();
                                            writer.WriteLine(all);
                                            writer.Flush();
                                        }
                                        Thread.Sleep(500);
                                    }
                                    catch (Exception ex)
                                    { }
                                }
                            }
                        }
                    }

                }
                if (cbox_coordinates.SelectedIndex == 1)
                {
                    //选择的是地球经纬度坐标系
                    int maxpoicount = 50;        //每个关键字每页返回50条数据，搜狗地图最大只支持50条。为了降低请求次数，让每次返回数据达到阈值。
                    int maxpagenum = 10;         //每个关键字最多返回10页 X 50条 = 500 条数据
                    int maxrequestcount = 2000;  //没申请clientid之前每天最多请求2000次，这个还没测试。不一定正确。

                    string cityname = tbox_cityname.Text;
                    char[] separatorcity = { '、', '，', ',', '；', ';' };
                    string[] citynamearray = cityname.Split(separatorcity);
                    if (tbox_txt.Text == "")
                        return;
                    else
                    {
                        for (int m = 0; m < citynamearray.Count(); m++)
                        {
                            for (int j = 0; j < downloadKeyWordList.Count; j++)
                            {
                                for (int i = 1; i <= maxpagenum; i++)
                                {
                                    string url = "http://api.go2map.com/engine/api/search/json?what=keyword:" + HttpUtility.UrlEncode(downloadKeyWordList[j].ToString()).ToUpper() + "&range=city:" + HttpUtility.UrlDecode(citynamearray[m]).ToUpper() + "&pageinfo=" + i.ToString() + ",50";

                                    string jsonString = "";
                                    try
                                    {
                                        jsonString = httpTools.GetSGPage(url).Trim();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("在关键字 " + downloadKeyWordList[j].ToString() + " 中断！请确保城市名称正确！");
                                        return;
                                    }

                                    //if (jsonString.Contains("每分钟访问次数"))
                                    //{
                                    //    Thread.Sleep(2000);
                                    //    jsonString = httpTools.GetSGPage(url).Trim();
                                    //}

                                    if (jsonString.Contains("error"))
                                    {
                                        break;
                                    }

                                    JObject jo = new JObject();
                                    try
                                    {
                                        jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                        for (int k = 0; k < jo["response"]["data"]["feature"].Count(); k++)
                                        {
                                            string name = jo["response"]["data"]["feature"][k]["caption"].ToString();
                                            string address = jo["response"]["data"]["feature"][k]["detail"]["address"].ToString();
                                            string lon = jo["response"]["data"]["feature"][k]["bounds"]["maxx"].ToString();
                                            string lat = jo["response"]["data"]["feature"][k]["bounds"]["maxy"].ToString();
                                            string id = jo["response"]["data"]["feature"][k]["id"].ToString();
                                            double[] lonlat = CoordinatesConver.SGMercator_To_Wgs84(double.Parse(lon), double.Parse(lat));
                                            string all = name + "|" + address + "|" + lonlat[0] + "|" + lonlat[1] + "|" + id + "|" + downloadKeyWordList[j].ToString();
                                            Console.WriteLine("城市 " + citynamearray[m] + " 关键字 " + downloadKeyWordList[j].ToString() + " : " + all);
                                            Console.WriteLine();
                                            writer.WriteLine(all);
                                            writer.Flush();
                                        }
                                        //Thread.Sleep(500);
                                    }
                                    catch (Exception ex)
                                    { }
                                }
                            }
                        }
                    }

                }
            }
            writer.Close();
        }

        public void updatePoiDownload()
        {
            Console.WriteLine("正在合并文件");
            Console.WriteLine();
            int threadCount = int.Parse(tbox_poiThreadCount.Text);
            //获得poidownload.txt的存储权限
            StreamWriter writer = new StreamWriter(tbox_txt.Text, true);
            for (int i = 0; i < threadCount; i++)
            {
                string tempTxtPath = System.Environment.CurrentDirectory + "\\conf\\tempfiles\\poi" + i + ".txt";
                StreamReader reader = new StreamReader(tempTxtPath, true);
                string line = reader.ReadLine();
                while (line != null)
                {
                    writer.WriteLine(line);
                    writer.Flush();
                    line = reader.ReadLine();
                }
                reader.Close();
            }
            writer.Close();
            Console.WriteLine("合并完成");
            Console.WriteLine();
            Console.WriteLine("爬取结束");
            //删除文件夹和txt文件
            DeleteFiles(System.Environment.CurrentDirectory + "\\conf\\tempfiles");

            
        }

        public bool DeleteFiles(string path)
        {
            if (Directory.Exists(path) == false)
            {
                MessageBox.Show("Tempfiles Path is not Existed!");
                return false;
            }
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles();
            try
            {
                foreach (var item in files)
                {
                    File.Delete(item.FullName);
                }
                if (dir.GetDirectories().Length != 0)
                {
                    foreach (var item in dir.GetDirectories())
                    {
                        if (!item.ToString().Contains("$") && (!item.ToString().Contains("Boot")))
                        {
                            DeleteFiles(dir.ToString() + "\\" + item.ToString());
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Delete Failed!");
                return false;

            }
        }

        private void btn_startDownloading_Click(object sender, EventArgs e)
        {
            //删除poidownload.txt的所有内容
            FileStream fs = new FileStream(tbox_txt.Text, FileMode.Create, FileAccess.Write);
            fs.Close();

            //删除tempfiles下的txt文件
            DeleteFiles(System.Environment.CurrentDirectory + "\\conf\\tempfiles");

            //POI下载线程
            int threadCount = int.Parse(tbox_poiThreadCount.Text);
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                Thread t = new Thread(new ParameterizedThreadStart(downloadPoiThread));
                t.Start(i);
                threads.Add(t);
            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            //POI更新线程
            Thread updateThread = new Thread(new ThreadStart(updatePoiDownload));
            updateThread.Start();
        }


        private void cbox_maptype_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbox_coordinates.Items.Clear();
            if (cbox_maptype.SelectedIndex == 0)
            {
                //如果选择的是百度地图
                cbox_coordinates.Items.Add("bd09ll(百度经纬度坐标)");
                cbox_coordinates.Items.Add("bd09mc(百度米制墨卡托投影坐标)");
                cbox_coordinates.Items.Add("gcj02(国测局经纬度坐标系)");
                cbox_coordinates.Items.Add("wgs84(地球经纬度坐标系）");
                //设置默认输出坐标系为百度经纬度坐标系
                cbox_coordinates.SelectedIndex = 0;
            }
            if (cbox_maptype.SelectedIndex == 1)
            {
                cbox_coordinates.Items.Add("sgmc(搜狗米制墨卡托投影坐标)");
                cbox_coordinates.Items.Add("wgs84(地球经纬度坐标系）");
                cbox_coordinates.SelectedIndex = 0;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //注册Ogr库
            OSGeo.OGR.Ogr.RegisterAll();
            string pszDriverName = "ESRI Shapefile";

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "CP936");

            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                MessageBox.Show("Driver Error");

            //用此Driver创建Shape文件
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(tbox_shpPath.Text+"\\"+tbox_shpName.Text, null);
            if (poDS == null)
                MessageBox.Show("DataSource Creation Error");

            //创建层Layer
            OSGeo.OGR.Layer poLayer;
            poLayer = poDS.CreateLayer(tbox_shpName.Text.Replace(".shp",""), null, OSGeo.OGR.wkbGeometryType.wkbPoint, null);
            if (poLayer == null)
                MessageBox.Show("Layer Creation Failed");

            StreamReader sr = new StreamReader(tbox_poi.Text, Encoding.Default);
            string line = sr.ReadLine();
            char[] separator = { '|' };
            //创建属性列两列
            OSGeo.OGR.FieldDefn Field1 = new OSGeo.OGR.FieldDefn("Name", OSGeo.OGR.FieldType.OFTString);
            Field1.SetWidth(16);
            OSGeo.OGR.FieldDefn Field2 = new OSGeo.OGR.FieldDefn("Address", OSGeo.OGR.FieldType.OFTString);
            Field2.SetWidth(80);
            OSGeo.OGR.FieldDefn Field3 = new OSGeo.OGR.FieldDefn("Lon", OSGeo.OGR.FieldType.OFTString);
            Field3.SetWidth(20);
            OSGeo.OGR.FieldDefn Field4 = new OSGeo.OGR.FieldDefn("Lat", OSGeo.OGR.FieldType.OFTString);
            Field4.SetWidth(20);
            OSGeo.OGR.FieldDefn Field5 = new OSGeo.OGR.FieldDefn("UID", OSGeo.OGR.FieldType.OFTString);
            Field5.SetWidth(50);
            OSGeo.OGR.FieldDefn Field6 = new OSGeo.OGR.FieldDefn("Tag", OSGeo.OGR.FieldType.OFTString);
            Field5.SetWidth(50);
            poLayer.CreateField(Field1, 0);
            poLayer.CreateField(Field2, 1);
            poLayer.CreateField(Field3, 2);
            poLayer.CreateField(Field4, 3);
            poLayer.CreateField(Field5, 4);
            poLayer.CreateField(Field6, 5);

            //创建一个Feature,一个Point
            OSGeo.OGR.Feature poFeature = new Feature(poLayer.GetLayerDefn());
            OSGeo.OGR.Geometry pt = new Geometry(OSGeo.OGR.wkbGeometryType.wkbPoint);

            while (line != null)
            {
                try
                {
                    string[] value = line.Split(separator);
                    poFeature.SetField(0, value[0].ToString());
                    poFeature.SetField(1, value[1].ToString());
                    poFeature.SetField(2, value[2].ToString());
                    poFeature.SetField(3, value[3].ToString());
                    poFeature.SetField(4, value[4].ToString());
                    poFeature.SetField(5, value[5].ToString());
                    //添加坐标点
                    pt.AddPoint(double.Parse(value[3]), double.Parse(value[2]), 0);
                    poFeature.SetGeometry(pt);
                    //将带有坐标及属性的Feature要素点写入Layer中
                    poLayer.CreateFeature(poFeature);
                }
                catch { }
                line = sr.ReadLine();
            }
            sr.Close();
            //关闭文件读写
            poFeature.Dispose();
            poDS.Dispose();
            MessageBox.Show("TXT转SHP成功");
            
        }

        private void btn_selectPoiTxt_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Title = "选择文件";
            ofd.Filter = "txt文件(*.txt)|*.txt";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbox_poi.Text = ofd.FileName;
            }
            else
                return;
        }

        public void downloadProvinceThread()
        {
            //注册Ogr库
            OSGeo.OGR.Ogr.RegisterAll();
            string pszDriverName = "ESRI Shapefile";

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "CP936");

            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                MessageBox.Show("Driver Error");

            //用此Driver创建Shape文件
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(tbox_folder.Text + "\\" + tbox_provinceName.Text, null);
            if (poDS == null)
                MessageBox.Show("DataSource Creation Error");

            //创建层Layer
            OSGeo.OGR.Layer poLayer;
            poLayer = poDS.CreateLayer(tbox_provinceName.Text.Replace(".shp", ""), null, OSGeo.OGR.wkbGeometryType.wkbPolygon, null);
            if (poLayer == null)
                MessageBox.Show("Layer Creation Failed");

            //创建属性列两列
            OSGeo.OGR.FieldDefn Field1 = new OSGeo.OGR.FieldDefn("Name", OSGeo.OGR.FieldType.OFTString);
            Field1.SetWidth(16);
            poLayer.CreateField(Field1, 0);

            ArrayList uidList = new ArrayList();
            ArrayList nameList = new ArrayList();
            uidList.Add("6f1759abad2e8a57f21937e6");
            nameList.Add("河北省");
            uidList.Add("2b5caae7a59333554251c0e6");
            nameList.Add("山西省");
            uidList.Add("72f75ed8b4becfaea2a934e6");
            nameList.Add("内蒙古自治区");
            uidList.Add("ef5350f29501fab8593ac8e6");
            nameList.Add("黑龙江省");
            uidList.Add("70005baeefe223dd0b1cc7e6");
            nameList.Add("吉林省");
            uidList.Add("59d1bad4f7fadd4ea5da39e6");
            nameList.Add("辽宁省");
            uidList.Add("89d9491c4c1e9a9b3ce231e6");
            nameList.Add("陕西省");
            uidList.Add("deca6b1d50d8c0b9a0acc4e6");
            nameList.Add("甘肃省");
            uidList.Add("db15e096b51b7a184107c1e6");
            nameList.Add("青海省");
            uidList.Add("9664f01efc56794e0482c2e6");
            nameList.Add("新疆维吾尔自治区");
            uidList.Add("d426b2ff5b009d930ab93ae6");
            nameList.Add("宁夏回族自治区");
            uidList.Add("672f03f21eab69ac1b94c6e6");
            nameList.Add("山东省");
            uidList.Add("3d22c7e0f91d160e6cc02ce6");
            nameList.Add("河南省");
            uidList.Add("59133023ffd171b4e50738e6");
            nameList.Add("江苏省");
            uidList.Add("6ee959d082e57f532e4733e6");
            nameList.Add("浙江省");
            uidList.Add("332af1bb49e09ae0132935e6");
            nameList.Add("安徽省");
            uidList.Add("9572a61574e4f36841d63ee6");
            nameList.Add("福建省");
            uidList.Add("a212bc189040548903ff2de6");
            nameList.Add("江西省");
            uidList.Add("cff7ff80e310f2aacb213de6");
            nameList.Add("湖北省");
            uidList.Add("3959e82b0c19ca50a2d230e6");
            nameList.Add("湖南省");
            uidList.Add("16ef15dd46f798e551e5c5e6");
            nameList.Add("广东省");
            uidList.Add("dd0d1e051bdd32f0f7e73be6");
            nameList.Add("海南省");
            uidList.Add("e6ead545d2c73bb682a12ee6");
            nameList.Add("四川省");
            uidList.Add("93490ce51cae2b60b21e36e6");
            nameList.Add("贵州省");
            uidList.Add("2fee091b1cd504ab471a32e6");
            nameList.Add("云南省");
            uidList.Add("0cc3c62b7f7aa865f7c59cd6");
            nameList.Add("台湾省");
            uidList.Add("cde731e17526799f49fd3fe6");
            nameList.Add("广西壮族自治区");
            uidList.Add("87ecb953ff003ccb5d17c3e6");
            nameList.Add("西藏自治区");

            //构建下载链接
            for (int i = 0; i < uidList.Count; i++)
            {
                Thread.Sleep(500);
                string uid = uidList[i].ToString();
                string name = nameList[i].ToString();
                Console.WriteLine("正在爬取 " + name + " 的边界数据");
                Console.WriteLine();
                string url = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=ext&num=1000&l=10&uid=" + uid + "&tn=B_NORMAL_MAP&nn=0&u_loc=13045574,3710968&ie=utf-8&b=(5028570,594930;15831700,7160199)";
                string jsonString = "";

                jsonString = httpTools.GetBDPage(url);
                JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                string geoStr = jo["content"]["geo"].ToString();
                int index = geoStr.LastIndexOf("|");
                string geoStr2 = geoStr.Remove(0, index + 1);
                //可能有多个geom
                char[] separator1 = { ';' };
                string[] multiGeom = geoStr2.Split(separator1);
                for (int k = 0; k < multiGeom.Count(); k++)
                {
                    string currentGeom = multiGeom[k];
                    char[] separator = { ',' };
                    string[] xy = currentGeom.Split(separator);
                    string wkt = "";
                    for (int j = 0; j < xy.Count() - 1; j++)
                    {
                        //先不进行坐标转换
                        double x = Convert.ToDouble(xy[j].ToString());
                        double y = Convert.ToDouble(xy[j + 1].ToString());
                        double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                        if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                        {
                            //如果转换后的点位信息超过中国区域，则不生成
                        }
                        else
                        {
                            if (j == xy.Count() - 2)
                            {
                                wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString();
                            }
                            else
                            {
                                wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString() + " , ";
                            }
                        }
                    }
                    if (wkt != "")
                    {

                        wkt = "POLYGON ((" + wkt + "))";
                        OSGeo.OGR.Feature poFeature = new Feature(poLayer.GetLayerDefn());
                        OSGeo.OGR.Geometry pt = Geometry.CreateFromWkt(wkt);

                        poFeature.SetGeometry(pt);
                        poFeature.SetField(0, name);
                        //将带有坐标及属性的Feature要素点写入Layer中
                        poLayer.CreateFeature(poFeature);

                        //关闭文件读写
                        poFeature.Dispose();
                    }
                }
            }

            poDS.Dispose();
            Console.WriteLine("爬取完成!");
        }

        private void btn_downloadProvince_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(downloadProvinceThread));
            t.Start();
        }

        public void downloadTownThread()
        {
            //注册Ogr库
            OSGeo.OGR.Ogr.RegisterAll();
            string pszDriverName = "ESRI Shapefile";

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "CP936");

            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                MessageBox.Show("Driver Error");

            //用此Driver创建Shape文件
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(tbox_townFolder.Text + "\\" + tbox_townShpName.Text, null);
            if (poDS == null)
                MessageBox.Show("DataSource Creation Error");

            //创建层Layer
            OSGeo.OGR.Layer poLayer;
            poLayer = poDS.CreateLayer(tbox_provinceName.Text.Replace(".shp", ""), null, OSGeo.OGR.wkbGeometryType.wkbPolygon, null);
            if (poLayer == null)
                MessageBox.Show("Layer Creation Failed");

            //创建属性列三列
            OSGeo.OGR.FieldDefn Field0 = new OSGeo.OGR.FieldDefn("Name", OSGeo.OGR.FieldType.OFTString);
            Field0.SetWidth(16);
            poLayer.CreateField(Field0, 0);
            OSGeo.OGR.FieldDefn Field1 = new OSGeo.OGR.FieldDefn("CityName", OSGeo.OGR.FieldType.OFTString);
            Field1.SetWidth(16);
            poLayer.CreateField(Field1, 1);
            OSGeo.OGR.FieldDefn Field2 = new OSGeo.OGR.FieldDefn("ProvinceName", OSGeo.OGR.FieldType.OFTString);
            Field2.SetWidth(16);
            poLayer.CreateField(Field2, 2);
            OSGeo.OGR.FieldDefn Field3 = new OSGeo.OGR.FieldDefn("Uid", OSGeo.OGR.FieldType.OFTString);
            Field3.SetWidth(16);
            poLayer.CreateField(Field3, 3);

            //首先读取 BDTownList.txt 文件
            StreamReader sr = new StreamReader(System.Environment.CurrentDirectory + "\\conf\\BDTownList.txt", Encoding.Default);
            string line = sr.ReadLine();
            string provincename = cbox_province.Text;
            while (line != null)
            {
                char[] separator = { ',' };
                string[] property = line.Split(separator);
                string provinceName = property[0];
                string cityName = property[1];
                string townName = property[2];
                if (provinceName == provincename)
                {
                    //接下来根据townName获得uid
                    string url = "http://map.baidu.com/su?wd=" + HttpUtility.UrlEncode(townName) + "&cid=1&type=0&newmap=1&pc_ver=2";
                    string uidString = httpTools.GetBDPage(url);
                    JObject uidJObject = (JObject)JsonConvert.DeserializeObject(uidString);

                    string uidStr = "";
                    for (int m = 0; m < uidJObject["s"].Count(); m++)
                    {
                        try
                        {
                            uidStr = uidJObject["s"][m].ToString();
                        }
                        catch { }
                        char[] separator2 = { '$' };
                        string[] uidStrlist = uidStr.Split(separator2);
                        string uid = "";
                        uid = uidStrlist[5];
                        try
                        {
                            //根据UID进行下载
                            Console.WriteLine("正在爬取 " + provinceName + " " + cityName + "市 " + townName + " 的边界数据");
                            Console.WriteLine();
                            string downloadurl = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=ext&num=1000&l=10&uid=" + uid + "&tn=B_NORMAL_MAP&nn=0&u_loc=13045574,3710968&ie=utf-8&b=(5028570,594930;15831700,7160199)";
                            string jsonString = httpTools.GetBDPage(downloadurl);

                            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                            string geoStr = jo["content"]["geo"].ToString();
                            int index = geoStr.LastIndexOf("|");
                            string geoStr2 = geoStr.Remove(0, index + 1);
                            //可能有多个geom
                            char[] separator1 = { ';' };
                            string[] multiGeom = geoStr2.Split(separator1);
                            for (int k = 0; k < multiGeom.Count(); k++)
                            {
                                string currentGeom = multiGeom[k];
                                char[] separator3 = { ',' };
                                string[] xy = currentGeom.Split(separator3);
                                string wkt = "";
                                for (int j = 0; j < xy.Count() - 1; j++)
                                {
                                    //先不进行坐标转换
                                    double x = Convert.ToDouble(xy[j].ToString());
                                    double y = Convert.ToDouble(xy[j + 1].ToString());
                                    double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                                    if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                                    {
                                        //如果转换后的点位信息超过中国区域，则不生成
                                    }
                                    else
                                    {
                                        if (j == xy.Count() - 2)
                                        {
                                            wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString();
                                        }
                                        else
                                        {
                                            wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString() + " , ";
                                        }
                                    }
                                }
                                if (wkt != "")
                                {

                                    wkt = "POLYGON ((" + wkt + "))";
                                    OSGeo.OGR.Feature poFeature = new Feature(poLayer.GetLayerDefn());
                                    OSGeo.OGR.Geometry pt = Geometry.CreateFromWkt(wkt);

                                    poFeature.SetGeometry(pt);
                                    poFeature.SetField(0, townName);
                                    poFeature.SetField(1, cityName);
                                    poFeature.SetField(2, provinceName);
                                    poFeature.SetField(3, uid);
                                    //将带有坐标及属性的Feature要素点写入Layer中
                                    poLayer.CreateFeature(poFeature);

                                    //关闭文件读写
                                    poFeature.Dispose();
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                line = sr.ReadLine();
            }
            sr.Close();
            poDS.Dispose();
            Console.WriteLine("爬取完成!");
        }

        private void btn_downloadTown_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(downloadTownThread));
            t.Start();
        }

        private void btn_openFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                this.tbox_folder.Text = dialog.SelectedPath;
            }
        }

        private void btn_openTownFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                this.tbox_townFolder.Text = dialog.SelectedPath;
            }
        }

        private void button2_Click_3(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                this.tbox_shpPath.Text = dialog.SelectedPath;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "http://www.cnblogs.com/niudieyi/");
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            
        }

        private void btn_openBuildFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                this.tbox_buildPath.Text = dialog.SelectedPath;
            }
        }

        private void btn_openRoadPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                this.tbox_roadPath.Text = dialog.SelectedPath;
            }
        }

        public void downloadRoadThread()
        {
            //注册Ogr库
            OSGeo.OGR.Ogr.RegisterAll();
            string pszDriverName = "ESRI Shapefile";

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "CP936");

            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                MessageBox.Show("Driver Error");

            //用此Driver创建Shape文件
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(tbox_roadPath.Text + "\\" + tbox_roadshpName.Text, null);
            if (poDS == null)
                MessageBox.Show("DataSource Creation Error");

            //创建层Layer
            OSGeo.OGR.Layer poLayer;
            poLayer = poDS.CreateLayer(tbox_roadshpName.Text.Replace(".shp", ""), null, OSGeo.OGR.wkbGeometryType.wkbLineString, null);
            if (poLayer == null)
                MessageBox.Show("Layer Creation Failed");

            //创建属性列三列
            OSGeo.OGR.FieldDefn Field0 = new OSGeo.OGR.FieldDefn("Name", OSGeo.OGR.FieldType.OFTString);
            Field0.SetWidth(16);
            poLayer.CreateField(Field0, 0);
            OSGeo.OGR.FieldDefn Field1 = new OSGeo.OGR.FieldDefn("Uid", OSGeo.OGR.FieldType.OFTString);
            Field1.SetWidth(16);
            poLayer.CreateField(Field1, 1);
            OSGeo.OGR.FieldDefn Field2 = new OSGeo.OGR.FieldDefn("CityName", OSGeo.OGR.FieldType.OFTString);
            Field2.SetWidth(16);
            poLayer.CreateField(Field2, 1);

            string cityBound = "";
            try
            {
                string boundUrl = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=" + HttpUtility.UrlEncode(tbox_roadCityNameChs.Text) + "&c=332&src=0&wd2=&pn=0&sug=0&l=12&from=webmap&biz_forward={%22scaler%22:1,%22styles%22:%22pl%22}&sug_forward=&tn=B_NORMAL_MAP&nn=0&u_loc=13045553,3710622&ie=utf-8&t=1526608082782";

                string boundjsonString = httpTools.GetBDPage(boundUrl);
                JObject boundJObject = (JObject)JsonConvert.DeserializeObject(boundjsonString);
                string res_boundStr = boundJObject["content"]["ext"]["detail_info"]["vs_content"]["invisible"]["bigdata"]["res_bound"].ToString();
                string pattern = @"\(.*?\)";//匹配模式
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = regex.Matches(res_boundStr);
                cityBound = matches[0].ToString();
            }
            catch { }

            for (int i = 1; i < 5; i++)
            {
                string url = "http://poi.mapbar.com/" + tbox_roadCityName.Text + "/G70_" + i.ToString() + "/";
                if (isUrlExists(url))
                {
                    string responseStr = "";
                    responseStr = httpTools.GetMapBarPage(url);
                    string[] strArr = responseStr.Split(new string[] { "<div class=\"sortC\">", "<div class=\"sortPage cl\" id=\"pageDiv\"" }, StringSplitOptions.None);
                    Regex regex = new Regex("<a.*</a>");
                    MatchCollection collection = regex.Matches(strArr[1]);
                    foreach (var item in collection)
                    {
                        string[] dataArr = item.ToString().Split(new string[] { ">", "</a>" }, StringSplitOptions.RemoveEmptyEntries);
                        string roadName = dataArr[1];
                        Console.WriteLine("正在爬取" + roadName);
                        //接下来根据townName获得uid
                        string roadUrl = "";
                        if (cityBound == null)
                        {
                            roadUrl = "http://map.baidu.com/su?wd=" + HttpUtility.UrlEncode(roadName) + "&cid=1&type=0&newmap=1&pc_ver=2";
                        }
                        else
                        {
                            roadUrl = "http://map.baidu.com/su?wd=" + HttpUtility.UrlEncode(roadName) + "&cid=1&type=0&newmap=1&b=" + cityBound + "&pc_ver=2";
                        }
                        string uidString = httpTools.GetBDPage(roadUrl);
                        JObject uidJObject = (JObject)JsonConvert.DeserializeObject(uidString);

                        string uidStr = "";
                        for (int m = 0; m < uidJObject["s"].Count(); m++)
                        {
                            try
                            {
                                uidStr = uidJObject["s"][m].ToString();
                            }
                            catch { }
                            char[] separator2 = { '$' };
                            string[] uidStrlist = uidStr.Split(separator2);
                            string uid = "";
                            uid = uidStrlist[5];
                            string geoName = uidStrlist[3];
                            string cityName = uidStrlist[0];
                            if (cityName.Contains(tbox_roadCityNameChs.Text))
                            {
                                try
                                {
                                    string downloadurl = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=ext&uid=" + uid + "&l=18&c=127&tn=B_NORMAL_MAP&nn=0&ie=utf-8&b=(5028570,594930;15831700,7160199)";
                                    string jsonString = httpTools.GetBDPage(downloadurl);
                                    JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                    string geoStr = jo["content"]["geo"].ToString();
                                    string geoStr2 = geoStr.Replace("2|", "");
                                    int index = geoStr2.IndexOf("|");
                                    string realgeoStr = geoStr2.Remove(0, index + 1);
                                    string realgeoStr2 = realgeoStr.Remove(realgeoStr.Count() - 1);
                                    //可能有多个geom
                                    char[] separator1 = { ';' };
                                    string[] multiGeom = realgeoStr2.Split(separator1);
                                    for (int k = 0; k < multiGeom.Count(); k++)
                                    {
                                        string currentGeom = multiGeom[k];
                                        char[] separator3 = { ',' };
                                        string[] xy = currentGeom.Split(separator3);
                                        string wkt = "";
                                        for (int j = 0; j < xy.Count() - 1; j++)
                                        {
                                            //先不进行坐标转换
                                            double x = Convert.ToDouble(xy[j].ToString());
                                            double y = Convert.ToDouble(xy[j + 1].ToString());
                                            if (cbox_roadcoordinate.SelectedIndex == 0)
                                            {
                                                double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                                                if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                                                {
                                                    //如果转换后的点位信息超过中国区域，则不生成
                                                }
                                                else
                                                {
                                                    if (j == xy.Count() - 2)
                                                    {
                                                        wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString();
                                                    }
                                                    else
                                                    {
                                                        wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString() + " , ";
                                                    }
                                                }
                                            }
                                            if (cbox_roadcoordinate.SelectedIndex == 1)
                                            {
                                                double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                                                if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                                                {
                                                    //如果转换后的点位信息超过中国区域，则不生成
                                                }
                                                else
                                                {
                                                    if (j == xy.Count() - 2)
                                                    {
                                                        wkt = wkt + x.ToString() + " " + y.ToString();
                                                    }
                                                    else
                                                    {
                                                        wkt = wkt + x.ToString() + " " + y.ToString() + " , ";
                                                    }
                                                }
                                            }
                                        }
                                        if (wkt != "")
                                        {

                                            wkt = "LINESTRING (" + wkt + ")";
                                            OSGeo.OGR.Feature poFeature = new Feature(poLayer.GetLayerDefn());
                                            OSGeo.OGR.Geometry pt = Geometry.CreateFromWkt(wkt);

                                            poFeature.SetGeometry(pt);
                                            poFeature.SetField(0, geoName);
                                            poFeature.SetField(1, uid);
                                            poFeature.SetField(2, cityName);
                                            //将带有坐标及属性的Feature要素点写入Layer中
                                            poLayer.CreateFeature(poFeature);
                                            //关闭文件读写
                                            poFeature.Dispose();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                { }
                            }
                        }
                    }
                    if (i == 4)
                    {
                        MessageBox.Show("爬取完毕!");
                    }
                }
                else
                {
                    if (i == 1)
                    {
                        MessageBox.Show("链接 " + url + " 无法访问，请重新输入城市中文拼音名称");
                    }
                }
            }
        }

        private void btn_downloadRoad_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(downloadRoadThread));
            t.Start();            
        }

        public bool isUrlExists(string sURL)
        {
            bool bExists = true;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sURL);
                request.Method = "HEAD";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                request.Method = "GET";
                request.AllowAutoRedirect = false;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //关闭对象
                response.Close();
                request.Abort();
            }
            catch (WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;
                if (response != null)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)//判断是否是404错误
                    {
                        bExists = false;
                    }
                }
            }
            return bExists;
        }

        private void btn_openSchoolFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                this.tbox_schoolPath.Text = dialog.SelectedPath;
            }
        }

        public void downloadSchoolThread()
        {
            //注册Ogr库
            OSGeo.OGR.Ogr.RegisterAll();
            string pszDriverName = "ESRI Shapefile";

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "CP936");

            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                MessageBox.Show("Driver Error");

            //用此Driver创建Shape文件
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(tbox_schoolPath.Text + "\\" + tbox_schoolshpName.Text, null);
            if (poDS == null)
                MessageBox.Show("DataSource Creation Error");

            //创建层Layer
            OSGeo.OGR.Layer poLayer;
            poLayer = poDS.CreateLayer(tbox_schoolshpName.Text.Replace(".shp", ""), null, OSGeo.OGR.wkbGeometryType.wkbPolygon, null);
            if (poLayer == null)
                MessageBox.Show("Layer Creation Failed");

            //创建属性列三列
            OSGeo.OGR.FieldDefn Field0 = new OSGeo.OGR.FieldDefn("Name", OSGeo.OGR.FieldType.OFTString);
            Field0.SetWidth(16);
            poLayer.CreateField(Field0, 0);
            OSGeo.OGR.FieldDefn Field1 = new OSGeo.OGR.FieldDefn("Uid", OSGeo.OGR.FieldType.OFTString);
            Field1.SetWidth(16);
            poLayer.CreateField(Field1, 1);
            OSGeo.OGR.FieldDefn Field2 = new OSGeo.OGR.FieldDefn("CityName", OSGeo.OGR.FieldType.OFTString);
            Field2.SetWidth(16);
            poLayer.CreateField(Field2, 2);
            OSGeo.OGR.FieldDefn Field3 = new OSGeo.OGR.FieldDefn("Tag", OSGeo.OGR.FieldType.OFTString);
            Field2.SetWidth(16);
            poLayer.CreateField(Field3, 3);

            //获得城市的bound
            //https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=%E6%99%AF%E5%BE%B7%E9%95%87&c=332&src=0&wd2=&pn=0&sug=0&l=12&from=webmap&biz_forward={%22scaler%22:1,%22styles%22:%22pl%22}&sug_forward=&tn=B_NORMAL_MAP&nn=0&u_loc=13045553,3710622&ie=utf-8&t=1526608082782
            string cityBound = "";
            try
            {
                string boundUrl = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=" + HttpUtility.UrlEncode(tbox_schoolCityNameChs.Text) + "&c=332&src=0&wd2=&pn=0&sug=0&l=12&from=webmap&biz_forward={%22scaler%22:1,%22styles%22:%22pl%22}&sug_forward=&tn=B_NORMAL_MAP&nn=0&u_loc=13045553,3710622&ie=utf-8&t=1526608082782";

                string boundjsonString = httpTools.GetBDPage(boundUrl);
                JObject boundJObject = (JObject)JsonConvert.DeserializeObject(boundjsonString);
                string res_boundStr = boundJObject["content"]["ext"]["detail_info"]["vs_content"]["invisible"]["bigdata"]["res_bound"].ToString();
                string pattern = @"\(.*?\)";//匹配模式
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = regex.Matches(res_boundStr);
                cityBound = matches[0].ToString();
            }
            catch { }

            string[] grps = new string[2];
            grps[0] = "中小学校";
            grps[1] = "大学院校";
            for (int p = 0; p < 2; p++)
            {
                string leibie = grps[p];
                string code = "";
                if (p == 0)
                {
                    code = "A10";
                }
                if (p == 1)
                {
                    code = "A20";
                }
                string url = "http://poi.mapbar.com/" + tbox_schoolCityName.Text + "/" + code + "_1/";

                if (isUrlExists(url))
                {
                    string responseStr = "";
                    responseStr = httpTools.GetMapBarPage(url);
                    string[] strArr = responseStr.Split(new string[] { "<div class=\"sortC\">", "<div class=\"sortPage cl\" id=\"pageDiv\"" }, StringSplitOptions.None);
                    Regex regex = new Regex("<a.*</a>");
                    MatchCollection collection = regex.Matches(strArr[1]);
                    foreach (var item in collection)
                    {
                        string[] dataArr = item.ToString().Split(new string[] { ">", "</a>" }, StringSplitOptions.RemoveEmptyEntries);
                        string schoolName = dataArr[1];
                        Console.WriteLine("正在爬取" + schoolName);
                        //接下来根据schoolName获得uid
                        string schoolUrl = "";
                        if (cityBound == null)
                        {
                            schoolUrl = "http://map.baidu.com/su?wd=" + HttpUtility.UrlEncode(schoolName) + "&cid=1&type=0&newmap=1&pc_ver=2";
                        }
                        else
                        {
                            schoolUrl = "http://map.baidu.com/su?wd=" + HttpUtility.UrlEncode(schoolName) + "&cid=1&type=0&newmap=1&b=" + cityBound + "&pc_ver=2";
                        }

                        string uidString = httpTools.GetBDPage(schoolUrl);
                        JObject uidJObject = (JObject)JsonConvert.DeserializeObject(uidString);

                        string uidStr = "";
                        for (int m = 0; m < uidJObject["s"].Count(); m++)
                        {
                            try
                            {
                                uidStr = uidJObject["s"][m].ToString();
                            }
                            catch { }
                            char[] separator2 = { '$' };
                            string[] uidStrlist = uidStr.Split(separator2);
                            string uid = "";
                            uid = uidStrlist[5];
                            string geoName = uidStrlist[3];
                            string cityName = uidStrlist[0];
                            if (cityName.Contains(tbox_schoolCityNameChs.Text))
                            {
                                try
                                {
                                    string downloadurl = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=ext&uid=" + uid + "&ext_ver=new&tn=B_NORMAL_MAP&nn=0&ie=utf-8&l=18&b=(5028570,594930;15831700,7160199)";
                                    string jsonString = httpTools.GetBDPage(downloadurl);
                                    JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                    string geoStr = jo["content"]["geo"].ToString();
                                    int index = geoStr.IndexOf("1-");
                                    string realgeoStr = geoStr.Remove(0, index + 2);
                                    string realgeoStr2 = realgeoStr.Remove(realgeoStr.Count() - 1);
                                    //可能有多个geom
                                    char[] separator1 = { ';' };
                                    string[] multiGeom = realgeoStr2.Split(separator1);
                                    for (int k = 0; k < multiGeom.Count(); k++)
                                    {
                                        string currentGeom = multiGeom[k];
                                        char[] separator3 = { ',' };
                                        string[] xy = currentGeom.Split(separator3);
                                        string wkt = "";
                                        for (int j = 0; j < xy.Count() - 1; j++)
                                        {
                                            //先不进行坐标转换
                                            double x = Convert.ToDouble(xy[j].ToString());
                                            double y = Convert.ToDouble(xy[j + 1].ToString());
                                            if (cbox_schoolcoordinate.SelectedIndex == 0)
                                            {
                                                //如果选中的是百度经纬度坐标系
                                                double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                                                if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                                                {
                                                    //如果转换后的点位信息超过中国区域，则不生成
                                                }
                                                else
                                                {
                                                    if (j == xy.Count() - 2)
                                                    {
                                                        wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString();
                                                    }
                                                    else
                                                    {
                                                        wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString() + " , ";
                                                    }
                                                }
                                            }
                                            if (cbox_schoolcoordinate.SelectedIndex == 1)
                                            {
                                                //如果选中的是百度米制墨卡托坐标系
                                                double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                                                if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                                                {
                                                    //如果转换后的点位信息超过中国区域，则不生成
                                                }
                                                else
                                                {
                                                    if (j == xy.Count() - 2)
                                                    {
                                                        wkt = wkt + x.ToString() + " " + y.ToString();
                                                    }
                                                    else
                                                    {
                                                        wkt = wkt + x.ToString() + " " + y.ToString() + " , ";
                                                    }
                                                }
                                            }
                                        }
                                        if (wkt != "")
                                        {

                                            wkt = "POLYGON ((" + wkt + "))";

                                            OSGeo.OGR.Feature poFeature = new Feature(poLayer.GetLayerDefn());
                                            OSGeo.OGR.Geometry pt = Geometry.CreateFromWkt(wkt);

                                            poFeature.SetGeometry(pt);
                                            poFeature.SetField(0, geoName);
                                            poFeature.SetField(1, uid);
                                            poFeature.SetField(2, cityName);
                                            poFeature.SetField(3, leibie);
                                            //将带有坐标及属性的Feature要素点写入Layer中
                                            poLayer.CreateFeature(poFeature);
                                            //关闭文件读写
                                            poFeature.Dispose();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                { }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("链接 " + url + " 无法访问，请重新输入城市中文拼音名称");
                }
            }
        }

        private void btn_downloadSchool_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(downloadSchoolThread));
            t.Start();
        }

        public void downloadBuildThread()
        {
            //注册Ogr库
            OSGeo.OGR.Ogr.RegisterAll();
            string pszDriverName = "ESRI Shapefile";

            // 为了支持中文路径，请添加下面这句代码
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            // 为了使属性表字段支持中文，请添加下面这句
            OSGeo.GDAL.Gdal.SetConfigOption("SHAPE_ENCODING", "CP936");

            //调用对Shape文件读写的Driver接口
            OSGeo.OGR.Driver poDriver = OSGeo.OGR.Ogr.GetDriverByName(pszDriverName);
            if (poDriver == null)
                MessageBox.Show("Driver Error");

            //用此Driver创建Shape文件
            OSGeo.OGR.DataSource poDS;
            poDS = poDriver.CreateDataSource(tbox_buildPath.Text + "\\" + tbox_buildshpName.Text, null);
            if (poDS == null)
                MessageBox.Show("DataSource Creation Error");

            //创建层Layer
            OSGeo.OGR.Layer poLayer;
            poLayer = poDS.CreateLayer(tbox_buildshpName.Text.Replace(".shp", ""), null, OSGeo.OGR.wkbGeometryType.wkbPolygon, null);
            if (poLayer == null)
                MessageBox.Show("Layer Creation Failed");

            //创建属性列三列
            OSGeo.OGR.FieldDefn Field0 = new OSGeo.OGR.FieldDefn("Name", OSGeo.OGR.FieldType.OFTString);
            Field0.SetWidth(16);
            poLayer.CreateField(Field0, 0);
            OSGeo.OGR.FieldDefn Field1 = new OSGeo.OGR.FieldDefn("Uid", OSGeo.OGR.FieldType.OFTString);
            Field1.SetWidth(16);
            poLayer.CreateField(Field1, 1);
            OSGeo.OGR.FieldDefn Field2 = new OSGeo.OGR.FieldDefn("CityName", OSGeo.OGR.FieldType.OFTString);
            Field2.SetWidth(16);
            poLayer.CreateField(Field2, 1);

            string cityBound = "";
            try
            {
                string boundUrl = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=s&da_src=searchBox.button&wd=" + HttpUtility.UrlEncode(tbox_buildCityNameChs.Text) + "&c=332&src=0&wd2=&pn=0&sug=0&l=12&from=webmap&biz_forward={%22scaler%22:1,%22styles%22:%22pl%22}&sug_forward=&tn=B_NORMAL_MAP&nn=0&u_loc=13045553,3710622&ie=utf-8&t=1526608082782";

                string boundjsonString = httpTools.GetBDPage(boundUrl);
                JObject boundJObject = (JObject)JsonConvert.DeserializeObject(boundjsonString);
                string res_boundStr = boundJObject["content"]["ext"]["detail_info"]["vs_content"]["invisible"]["bigdata"]["res_bound"].ToString();
                string pattern = @"\(.*?\)";//匹配模式
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = regex.Matches(res_boundStr);
                cityBound = matches[0].ToString();
            }
            catch { }

            for (int i = 1; i < 5; i++)
            {
                string url = "http://poi.mapbar.com/" + tbox_buildCityName.Text + "/F10_" + i.ToString() + "/";
                if (isUrlExists(url))
                {
                    string responseStr = "";
                    responseStr = httpTools.GetMapBarPage(url);
                    string[] strArr = responseStr.Split(new string[] { "<div class=\"sortC\">", "<div class=\"sortPage cl\" id=\"pageDiv\"" }, StringSplitOptions.None);
                    Regex regex = new Regex("<a.*</a>");
                    MatchCollection collection = regex.Matches(strArr[1]);
                    foreach (var item in collection)
                    {
                        string[] dataArr = item.ToString().Split(new string[] { ">", "</a>" }, StringSplitOptions.RemoveEmptyEntries);
                        string buildName = dataArr[1];
                        Console.WriteLine("正在爬取" + buildName);
                        //接下来根据townName获得uid
                        string buildUrl = "";
                        if (cityBound == null)
                        {
                            buildUrl = "http://map.baidu.com/su?wd=" + HttpUtility.UrlEncode(buildName) + "&cid=1&type=0&newmap=1&pc_ver=2";
                        }
                        else
                        {
                            buildUrl = "http://map.baidu.com/su?wd=" + HttpUtility.UrlEncode(buildName) + "&cid=1&type=0&newmap=1&b=" + cityBound + "&pc_ver=2";
                        }

                        string uidString = httpTools.GetBDPage(buildUrl);
                        JObject uidJObject = (JObject)JsonConvert.DeserializeObject(uidString);

                        string uidStr = "";
                        for (int m = 0; m < uidJObject["s"].Count(); m++)
                        {
                            try
                            {
                                uidStr = uidJObject["s"][m].ToString();
                            }
                            catch { }
                            char[] separator2 = { '$' };
                            string[] uidStrlist = uidStr.Split(separator2);
                            string uid = "";
                            uid = uidStrlist[5];
                            string geoName = uidStrlist[3];
                            string cityName = uidStrlist[0];
                            if (cityName.Contains(tbox_buildCityNameChs.Text))
                            {

                                try
                                {
                                    string downloadurl = "https://map.baidu.com/?newmap=1&reqflag=pcmap&biz=1&from=webmap&da_par=direct&pcevaname=pc4.1&qt=ext&uid=" + uid + "&ext_ver=new&tn=B_NORMAL_MAP&nn=0&ie=utf-8&l=18&b=(5028570,594930;15831700,7160199)";
                                    string jsonString = httpTools.GetBDPage(downloadurl);
                                    JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
                                    string geoStr = jo["content"]["geo"].ToString();
                                    int index = geoStr.IndexOf("1-");
                                    string realgeoStr = geoStr.Remove(0, index + 2);
                                    string realgeoStr2 = realgeoStr.Remove(realgeoStr.Count() - 1);
                                    //可能有多个geom
                                    char[] separator1 = { ';' };
                                    string[] multiGeom = realgeoStr2.Split(separator1);
                                    for (int k = 0; k < multiGeom.Count(); k++)
                                    {
                                        string currentGeom = multiGeom[k];
                                        char[] separator3 = { ',' };
                                        string[] xy = currentGeom.Split(separator3);
                                        string wkt = "";
                                        for (int j = 0; j < xy.Count() - 1; j++)
                                        {
                                            //先不进行坐标转换
                                            double x = Convert.ToDouble(xy[j].ToString());
                                            double y = Convert.ToDouble(xy[j + 1].ToString());
                                            if (cbox_buildcoordinate.SelectedIndex == 0)
                                            {
                                                //如果选中的是百度经纬度坐标系
                                                double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                                                if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                                                {
                                                    //如果转换后的点位信息超过中国区域，则不生成
                                                }
                                                else
                                                {
                                                    if (j == xy.Count() - 2)
                                                    {
                                                        wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString();
                                                    }
                                                    else
                                                    {
                                                        wkt = wkt + latlon[0].ToString() + " " + latlon[1].ToString() + " , ";
                                                    }
                                                }
                                            }
                                            if (cbox_buildcoordinate.SelectedIndex == 1)
                                            {
                                                //如果选中的是百度米制墨卡托坐标系
                                                double[] latlon = CoordinatesConver.Mercator2BD09(x, y);
                                                if (latlon[0] > 136 || latlon[0] < 73 || latlon[1] > 54 || latlon[1] < 3)
                                                {
                                                    //如果转换后的点位信息超过中国区域，则不生成
                                                }
                                                else
                                                {
                                                    if (j == xy.Count() - 2)
                                                    {
                                                        wkt = wkt + x.ToString() + " " + y.ToString();
                                                    }
                                                    else
                                                    {
                                                        wkt = wkt + x.ToString() + " " + y.ToString() + " , ";
                                                    }
                                                }
                                            }
                                        }
                                        if (wkt != "")
                                        {

                                            wkt = "POLYGON ((" + wkt + "))";

                                            OSGeo.OGR.Feature poFeature = new Feature(poLayer.GetLayerDefn());
                                            OSGeo.OGR.Geometry pt = Geometry.CreateFromWkt(wkt);

                                            poFeature.SetGeometry(pt);
                                            poFeature.SetField(0, geoName);
                                            poFeature.SetField(1, uid);
                                            poFeature.SetField(2, cityName);
                                            //将带有坐标及属性的Feature要素点写入Layer中
                                            poLayer.CreateFeature(poFeature);
                                            //关闭文件读写
                                            poFeature.Dispose();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                { }
                            }
                        }
                    }
                    if (i == 4)
                    {
                        MessageBox.Show("爬取完毕!");
                    }
                }
                else
                {
                    if (i == 1)
                    {
                        MessageBox.Show("链接 " + url + " 无法访问，请重新输入城市中文拼音名称");
                    }
                }
            }
        }

        private void btn_downloadBuild_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(downloadBuildThread));
            t.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string[] test = new string[4];
            test[0] = "0";
            test[1] = "1";
            Console.WriteLine("Count:" + test.Count());
            Console.WriteLine("Length:" + test.Length);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "https://github.com/HexiChen/FreeBaiduVectorDownload");
        }
    }
}
