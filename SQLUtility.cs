using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace CPUFramework
{
    public class SQLUtility
    {
        public static string connectionstring = "";

        public static SqlCommand GetSqlCommand(string sprocname)
        {
            SqlCommand cmd;
            using (SqlConnection conn = new SqlConnection(SQLUtility.connectionstring))
            {
                cmd = new SqlCommand(sprocname, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                SqlCommandBuilder.DeriveParameters(cmd);
            }
            return cmd;
        }

        public static DataTable GetDataTable(SqlCommand cmd)
        {
            Debug.Print("-----" + Environment.NewLine +  cmd.CommandText);
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.connectionstring))
            {
                cmd.Connection = conn;
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
            }
            SetAllColumnAllowNull(dt);
            return dt;

        }
        public  static DataTable GetDataTable(string sqlstatement)
        {
            return GetDataTable(new SqlCommand(sqlstatement));
        }

        public static void ExecuteSQL(string sqlstatement)
        {
            GetDataTable(sqlstatement);
        }

        public static void ExecuteSQL(SqlCommand cmd)
        {
            using (SqlConnection conn = new SqlConnection(SQLUtility.connectionstring))
            {
                cmd.Connection = conn;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private  static void SetAllColumnAllowNull(DataTable dt)
        {
            foreach(DataColumn c in dt.Columns)
            {
                c.AllowDBNull = true;
            }
        }

        public static void DebugPrintDataTable(DataTable dt)
        {
            foreach(DataRow r in dt.Rows)
            {
                foreach(DataColumn c in dt.Columns)
                {
                    Debug.Print(c.ColumnName + " = " + r[c.ColumnName].ToString());
                }
            }
        }

        public static int GetFirstColumnFirstRowValue(string sql)
        {
            int n = 0;
            DataTable dt = GetDataTable(sql);
            if(dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {
                    int.TryParse(dt.Rows[0][0].ToString(), out n);
                }
            }
            return n;


        }
    }
}
//note