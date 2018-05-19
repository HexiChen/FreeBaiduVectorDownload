using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace DataProcess
{
    class CoordinatesConver
    {
        private static double[] Sp = { 1.289059486E7, 8362377.87, 5591021, 3481989.83, 1678043.12, 0 };
        private static String BAIDU_LBS_TYPE = "bd09ll";
        private static double pi = 3.1415926535897932384626;
        private static double a = 6378245.0;
        private static double ee = 0.00669342162296594323;

        /// <summary>
        /// 搜狗墨卡托投影转WGS84
        /// </summary>
        /// <param name="mercatorX"></param>
        /// <param name="mercatorY"></param>
        /// <returns></returns>
        public static double[] SGMercator_To_Wgs84(double mercatorX, double mercatorY)
        {
            double[] xy = new double[2];
            double x = mercatorX / 20037508.34 * 180;
            double y = mercatorY / 20037508.34 * 180;
            y = 180 / Math.PI * (2 * Math.Atan(Math.Exp(y * Math.PI / 180)) - Math.PI / 2);
            xy[0] = x;
            xy[1] = y;
            return xy;
        }

        /// <summary>
        /// Wgs84 to GCJ-02
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public static double[] Wgs84_To_Gcj02(double lat, double lon)
        {
            if (outOfChina(lat, lon))
            {
                return null;
            }
            double dLat = transformLat(lon - 105.0, lat - 35.0);
            double dLon = transformLon(lon - 105.0, lat - 35.0);
            double radLat = lat / 180.0 * pi;
            double magic = Math.Sin(radLat);
            magic = 1 - ee * magic * magic;
            double sqrtMagic = Math.Sqrt(magic);
            dLat = (dLat * 180.0) / ((a * (1 - ee)) / (magic * sqrtMagic) * pi);
            dLon = (dLon * 180.0) / (a / sqrtMagic * Math.Cos(radLat) * pi);
            double mgLat = lat + dLat;
            double mgLon = lon + dLon;
            double[] latlon = new double[2];
            latlon[0] = mgLat;
            latlon[1] = mgLon;
            return latlon;
        }

        /// <summary>
        /// GCJ-02 To Wgs84
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public static double[] Gcj02_To_Wgs84(double lat, double lon)
        {
            double[] latlon = transform(lat, lon);
            double lontitude = lon * 2 - latlon[1];
            double latitude = lat * 2 - latlon[0];
            double[] latlon2 = new double[2];
            latlon2[0] = latitude;
            latlon2[1] = lontitude;
            return latlon2;
        }

        /// <summary>
        /// GCJ-02 To Bd09
        /// </summary>
        /// <param name="gg_lat"></param>
        /// <param name="gg_lon"></param>
        /// <returns></returns>
        public static double[] Gcj02_To_Bd09(double gg_lat, double gg_lon)
        {
            double x = gg_lon, y = gg_lat;
            double z = Math.Sqrt(x * x + y * y) + 0.00002 * Math.Sin(y * pi);
            double theta = Math.Atan2(y, x) + 0.000003 * Math.Cos(x * pi);
            double bd_lon = z * Math.Cos(theta) + 0.0065;
            double bd_lat = z * Math.Sin(theta) + 0.006;
            double[] latlon2 = new double[2];
            latlon2[0] = bd_lat;
            latlon2[1] = bd_lon;
            return latlon2;
        }

        /// <summary>
        /// Bd09 To Gcj02
        /// </summary>
        /// <param name="bd_lat"></param>
        /// <param name="bd_lon"></param>
        /// <returns></returns>
        public static double[] Bd09_To_Gcj02(double bd_lat, double bd_lon)
        {
            double x = bd_lon - 0.0065, y = bd_lat - 0.006;
            double z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * pi);
            double theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * pi);
            double gg_lon = z * Math.Cos(theta);
            double gg_lat = z * Math.Sin(theta);
            double[] latlon2 = new double[2];
            latlon2[0] = gg_lat;
            latlon2[1] = gg_lon;
            return latlon2;
        }

        /// <summary>
        /// bd09 To wgs84
        /// </summary>
        /// <param name="bd_lat"></param>
        /// <param name="bd_lon"></param>
        /// <returns></returns>
        public static double[] Bd09_To_Wgs84(double bd_lat, double bd_lon)
        {

            double[] gcj02 = CoordinatesConver.Bd09_To_Gcj02(bd_lat, bd_lon);
            double[] map84 = CoordinatesConver.Gcj02_To_Wgs84(gcj02[0],
                    gcj02[1]);
            return map84;
        }

        private static Boolean outOfChina(double lat, double lon)
        {
            if (lon < 72.004 || lon > 137.8347)
                return true;
            if (lat < 0.8293 || lat > 55.8271)
                return true;
            return false;
        }

        private static double[] transform(double lat, double lon)
        {
            if (outOfChina(lat, lon))
            {
                double[] latlon2 = new double[2];
                latlon2[0] = lat;
                latlon2[1] = lon;
                return latlon2;
            }
            double dLat = transformLat(lon - 105.0, lat - 35.0);
            double dLon = transformLon(lon - 105.0, lat - 35.0);
            double radLat = lat / 180.0 * pi;
            double magic = Math.Sin(radLat);
            magic = 1 - ee * magic * magic;
            double sqrtMagic = Math.Sqrt(magic);
            dLat = (dLat * 180.0) / ((a * (1 - ee)) / (magic * sqrtMagic) * pi);
            dLon = (dLon * 180.0) / (a / sqrtMagic * Math.Cos(radLat) * pi);
            double mgLat = lat + dLat;
            double mgLon = lon + dLon;
            double[] latlon3 = new double[2];
            latlon3[0] = mgLat;
            latlon3[1] = mgLon;
            return latlon3;
        }

        private static double transformLat(double x, double y)
        {
            double ret = -100.0 + 2.0 * x + 3.0 * y + 0.2 * y * y + 0.1 * x * y
                    + 0.2 * Math.Sqrt(Math.Abs(x));
            ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
            ret += (20.0 * Math.Sin(y * pi) + 40.0 * Math.Sin(y / 3.0 * pi)) * 2.0 / 3.0;
            ret += (160.0 * Math.Sin(y / 12.0 * pi) + 320 * Math.Sin(y * pi / 30.0)) * 2.0 / 3.0;
            return ret;
        }

        private static double transformLon(double x, double y)
        {
            double ret = 300.0 + x + 2.0 * y + 0.1 * x * x + 0.1 * x * y + 0.1
                    * Math.Sqrt(Math.Abs(x));
            ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
            ret += (20.0 * Math.Sin(x * pi) + 40.0 * Math.Sin(x / 3.0 * pi)) * 2.0 / 3.0;
            ret += (150.0 * Math.Sin(x / 12.0 * pi) + 300.0 * Math.Sin(x / 30.0
                    * pi)) * 2.0 / 3.0;
            return ret;
        }   


        /// <summary>
        /// Bd09mc To Bd09（精度高）
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <returns></returns>
        public static double[] Mercator2BD09(double lng, double lat)
        {
            double[] lnglat = new double[2];
            ArrayList c = null;
            //List<Double> d0 = new ArrayList<Double>();
            ArrayList d0 = new ArrayList();
            double[] d0str = { 1.410526172116255E-8, 8.98305509648872E-6, -1.9939833816331, 200.9824383106796, -187.2403703815547, 91.6087516669843, -23.38765649603339, 2.57121317296198, -0.03801003308653, 1.73379812E7 };
            for (int i = 0; i < d0str.Length; i++)
            {
                d0.Add(d0str[i]);
            }

            ArrayList d1 = new ArrayList();
            double[] d1str = { -7.435856389565537E-9, 8.983055097726239E-6, -0.78625201886289, 96.32687599759846, -1.85204757529826, -59.36935905485877, 47.40033549296737, -16.50741931063887, 2.28786674699375, 1.026014486E7 };
            for (int i = 0; i < d1str.Length; i++)
            {
                d1.Add(d1str[i]);
            }

            ArrayList d2 = new ArrayList();
            double[] d2str = { -3.030883460898826E-8, 8.98305509983578E-6, 0.30071316287616, 59.74293618442277, 7.357984074871, -25.38371002664745, 13.45380521110908, -3.29883767235584, 0.32710905363475, 6856817.37 };
            for (int i = 0; i < d2str.Length; i++)
            {
                d2.Add(d2str[i]);
            }

            ArrayList d3 = new ArrayList();
            double[] d3str = { -1.981981304930552E-8, 8.983055099779535E-6, 0.03278182852591, 40.31678527705744, 0.65659298677277, -4.44255534477492, 0.85341911805263, 0.12923347998204, -0.04625736007561, 4482777.06 };
            for (int i = 0; i < d3str.Length; i++)
            {
                d3.Add(d3str[i]);
            }

            ArrayList d4 = new ArrayList();
            double[] d4str = { 3.09191371068437E-9, 8.983055096812155E-6, 6.995724062E-5, 23.10934304144901, -2.3663490511E-4, -0.6321817810242, -0.00663494467273, 0.03430082397953, -0.00466043876332, 2555164.4 };
            for (int i = 0; i < d4str.Length; i++)
            {
                d4.Add(d4str[i]);
            }

            ArrayList d5 = new ArrayList();
            double[] d5str = { 2.890871144776878E-9, 8.983055095805407E-6, -3.068298E-8, 7.47137025468032, -3.53937994E-6, -0.02145144861037, -1.234426596E-5, 1.0322952773E-4, -3.23890364E-6, 826088.5 };
            for (int i = 0; i < d5str.Length; i++)
            {
                d5.Add(d5str[i]);
            }

            lnglat[0] = Math.Abs(lng);
            lnglat[1] = Math.Abs(lat);

            for (int d = 0; d < 6; d++)
            {
                if (lnglat[1] >= Sp[d])
                {
                    if (d == 0)
                    {
                        c = d0;
                    }

                    if (d == 1)
                    {
                        c = d1;
                    }

                    if (d == 2)
                    {
                        c = d2;
                    }

                    if (d == 3)
                    {
                        c = d3;
                    }

                    if (d == 4)
                    {
                        c = d4;
                    }

                    if (d == 5)
                    {
                        c = d5;
                    }

                    break;
                }
            }
            lnglat = Yr(lnglat, c);
            return lnglat;
        }

        private static double[] Yr(double[] lnglat, ArrayList b)
        {
            if (b != null)
            {
                double c = double.Parse(b[0].ToString()) + double.Parse(b[1].ToString()) * Math.Abs(lnglat[0]);
                double d = Math.Abs(lnglat[1]) / double.Parse(b[9].ToString());
                d = double.Parse(b[2].ToString()) + double.Parse(b[3].ToString()) * d + double.Parse(b[4].ToString()) * d * d + double.Parse(b[5].ToString()) * d * d * d + double.Parse(b[6].ToString()) * d * d * d * d + double.Parse(b[7].ToString()) * d * d * d * d * d + double.Parse(b[8].ToString()) * d * d * d * d * d * d;
                double bd;
                if (0 > lnglat[0])
                {
                    bd = -1 * c;
                }
                else
                {
                    bd = c;
                }
                lnglat[0] = bd;

                double bd2;
                if (0 > lnglat[1])
                {
                    bd2 = -1 * d;
                }
                else
                {
                    bd2 = d;
                }
                lnglat[1] = bd2;
                return lnglat;
            }
            return null;
        }
    }
}
