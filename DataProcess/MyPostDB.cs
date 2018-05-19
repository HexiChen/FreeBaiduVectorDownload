using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Data;
using Npgsql; 

namespace DataProcess
{
    /*
     * ExecuteNonQuery方法 ：执行非查询SQL操作，包括增insert、删delete、改update；ExecuteNonQuery()方法执行SQL语句并且不返回数据。
     * ExecuteDataset会运行你的基本SELECT（选择）查询并生成一个DaclosetaSet，然后就能够被绑定到服务器对象上，或者被用来创建DataView(数据视图）。
     * ExecuteReader主要是用于查询语句（SELECT），它是为了提高运行性能而设置的。SqlDataReaders很类似于经典 ADO里的只能向前的只读记录集(即类似ASP中的movenext)，它们对于填充ListBoxe控件和  CheckBoxList控件很有用处。对ExecuteReader的调用看起来就像是一个ExecuteDataset。要记住，它需要命名空间为System.Data.SqlClient.
     * 对于使用ExecuteScalar()，ExecuteScalar()方法执行SQl查询，并返回查询结果集中的第一行的第一列，忽略额外的列或行！虽然返回的值的数据类型可以是string,int。
     */
    public class MyPostDB
    {
        DataSet DS;
        bool ECode;
        public bool isConnection;
        string ErrString;
        public NpgsqlConnection Conn;

        /// <summary>
        /// 构造方法
        /// </summary>
        public MyPostDB()
        {
            //初始化变量
            ECode = false;
            ErrString = "";
            Conn = new NpgsqlConnection();
        }

        /// <summary>
        /// 连接到Postgresql数据库
        /// </summary>
        /// <param name="ServerName">服务名</param>
        /// <param name="ServerPort">端口号2</param>
        /// <param name="DBName">数据库名</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Pwd">密码</param>
        /// <returns>布尔型</returns>
        public bool ConnectToDB(string ServerName, string ServerPort, string DBName, string UserName, string Pwd)
        {
            isConnection = true;
            ECode = false;
            ErrString = "";
            Conn.ConnectionString = "Server=" + ServerName + ";Port=" + ServerPort + ";User Id=" + UserName + ";Password=" + Pwd + ";Database=" + DBName + ";CommandTimeout=60000";
            try
            {
                Conn.Open();
                return true;
            }
            catch (Exception e)
            {
                ECode = true;
                ErrString = e.Message;
            }
            return false;
        }

        /// <summary>
        /// 执行查询，返回Dataset
        /// </summary>
        /// <param name="sql">查询SQL语句：类似select name from table</param>
        /// <returns>结果集Dataset</returns>
        public DataSet GetRecordSet(string sql)
        {
            ECode = false;
            ErrString = "";
            NpgsqlCommand sqlCmd = new NpgsqlCommand();
            sqlCmd.Connection = Conn;
            sqlCmd.CommandText = sql;
            try
            {
                NpgsqlDataAdapter adp = new NpgsqlDataAdapter(sqlCmd);
                DS = new DataSet();
                adp.Fill(DS);
            }
            catch (Exception e)
            {
                ErrString = e.Message;
                ECode = true;
                return null;
            }
            return DS;
        }

        /// <summary>
        /// 执行查询,返回int型记录数量
        /// </summary>
        /// <param name="Sqls">查询SQL语句：类似select count(*) from table</param>
        /// <returns>整型</returns>
        public int ExecuteSQLScalar(string Sqls)
        {
            ECode = false;
            ErrString = "";
            string s;
            NpgsqlCommand sqlCmd = new NpgsqlCommand();
            sqlCmd.Connection = Conn;
            sqlCmd.CommandText = Sqls;
            sqlCmd.CommandType = CommandType.Text;
            try
            {
                s = sqlCmd.ExecuteScalar().ToString();
            }
            catch (Exception e)
            {
                ErrString = e.Message;
                ECode = true;
                return -1;
            }
            return (int.Parse(s));
        }

        /// <summary>
        /// 执行查询,返回string型记录数量
        /// </summary>
        /// <param name="Sqls">查询SQL语句：类似select count(*) from table</param>
        /// <returns>字符串型</returns>
        public string ExecuteSQLScalarTOstring(string Sqls)
        {
            ECode = false;
            ErrString = "";
            string s;
            NpgsqlCommand sqlCmd = new NpgsqlCommand();
            sqlCmd.Connection = Conn;
            sqlCmd.CommandText = Sqls;
            sqlCmd.CommandType = CommandType.Text;
            try
            {
                s = sqlCmd.ExecuteScalar().ToString();
            }
            catch (Exception e)
            {
                ErrString = e.Message;
                ECode = true;
                return "-1";
            }
            return s;
        }

        /// <summary>
        /// 事务中执行SQL语句
        /// </summary>
        /// <param name="Sqls">SQL语句</param>
        /// <returns></returns>
        public string ExecuteSQLWithTrans(string Sqls)
        {
            ECode = false;
            ErrString = "";
            string s;
            NpgsqlTransaction myTrans;
            myTrans = Conn.BeginTransaction();
            NpgsqlCommand sqlCmd = new NpgsqlCommand();
            sqlCmd.Connection = Conn;
            sqlCmd.CommandText = Sqls;
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.Transaction = myTrans;
            sqlCmd.ExecuteNonQuery();
            //Sqls="SELECT @@IDENTITY AS ID";  
            sqlCmd.CommandText = Sqls;
            try
            {
                s = sqlCmd.ExecuteScalar().ToString();
            }
            catch (Exception e)
            {
                ErrString = e.Message;
                ECode = true;
                myTrans.Commit();
                return "";
            }
            myTrans.Commit();
            return (s);
        }

        /// <summary>
        /// 执行增insert、删delete、改update语句
        /// </summary>
        /// <param name="Sqls">SQL语句</param>
        public void ExecuteSQL(string Sqls)
        {
            ECode = false;
            ErrString = "";
            NpgsqlCommand sqlCmd = new NpgsqlCommand();
            sqlCmd.Connection = Conn;
            sqlCmd.CommandText = Sqls;
            sqlCmd.CommandType = CommandType.Text;
            try
            {
                sqlCmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ErrString = e.Message;
                ECode = true;
            }
        }

        /// <summary>
        /// 执行查询select语句
        /// </summary>
        /// <param name="Sqls">查询select语句</param>
        /// <returns></returns>
        public NpgsqlDataReader DBDataReader(string Sqls)
        {
            ECode = false;
            ErrString = "";
            NpgsqlCommand sqlCmd = new NpgsqlCommand();
            sqlCmd.Connection = Conn;
            sqlCmd.CommandText = Sqls;
            sqlCmd.CommandType = CommandType.Text;
            try
            {
                return sqlCmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception e)
            {
                ErrString = e.Message;
                ECode = true;
                return null;
            }
        }

        /// <summary>
        /// 关闭Postgresql数据库连接
        /// </summary>
        public void DBClose()
        {
            ECode = false;
            ErrString = "";
            try
            {
                Conn.Close();
            }
            catch (Exception e)
            {
                ErrString = e.Message;
                ECode = true;
            }
        }

        /// <summary>
        /// 是否错误
        /// </summary>
        /// <returns>布尔型</returns>
        public bool ErrorCode()
        {
            return ECode;
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        /// <returns>字符串型</returns>
        public string ErrMessage()
        {
            return ErrString;
        }
    }
}
