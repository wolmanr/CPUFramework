using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Data.SqlClient;


namespace CPUFramework
{
    public class SQLUtility
    {
        public static string connectionstring = "";
        public static DataTable GetDataTable(string sqlstatement) 
        { 
            Debug.Print(sqlstatement); 
          DataTable dt = new(); 
          SqlConnection conn = new(); 
          conn.ConnectionString = connectionstring; 
          conn.Open(); 
          var cmd = new SqlCommand(); 
          cmd.Connection = conn; 
          cmd.CommandText = sqlstatement; 
          var dr = cmd.ExecuteReader();
          SetAllColumnAllowNull(dt);
          dt.Load(dr); 
         SetAllColumnAllowNull(dt); 
         return dt; 
        }



        public static void ExecuteSQL(string sqlstatement)
        {
            GetDataTable(sqlstatement);
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
    }
}
