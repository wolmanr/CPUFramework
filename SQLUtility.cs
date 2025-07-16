using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
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
            return DoExecuteSql(cmd, true);
        }

        private static DataTable DoExecuteSql(SqlCommand cmd, bool loadtable)
        {
            
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.connectionstring))
            {
                cmd.Connection = conn;
                conn.Open();
                Debug.Print(GetSql(cmd));
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (loadtable == true)
                    {
                        dt.Load(dr);
                    }
                }
                catch(SqlException ex)
                {
                    string msg = ParseConstraintMsg(ex.Message);
                    throw new Exception(msg);
                }
                catch (InvalidCastException ex)
                {
                    throw new Exception(cmd.CommandText + ":" + ex.Message, ex);
                }
            }
            SetAllColumnAllowNull(dt);
            return dt;

        }
        public  static DataTable GetDataTable(string sqlstatement)
        {
            return DoExecuteSql(new SqlCommand(sqlstatement), true);
        }

        public static void ExecuteSQL(SqlCommand cmd)
        {
            DoExecuteSql(cmd, false);
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

        public static string GetSql(SqlCommand cmd)
        {
            string val = "";
#if DEBUG
            StringBuilder sb = new();
            if(cmd.Connection != null)
            {
                sb.AppendLine($"--{cmd.Connection.DataSource}");
                sb.AppendLine($"use {cmd.Connection.Database}");
                sb.AppendLine("go");
            }
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendLine($"exec {cmd.CommandText}");
                int paramcount = cmd.Parameters.Count -1;
                int paramnum = 0;
                string comma = ",";
                foreach(SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction != ParameterDirection.ReturnValue)
                    {
                        if (paramnum == paramcount)
                        {
                            comma = "";
                        }
                        sb.AppendLine($"{p.ParameterName} = {(p.Value == null ? "null" : p.Value.ToString())}{comma}");

                    }
                    paramnum++;
                }
            }
            else
            {
                sb.AppendLine(cmd.CommandText);
            }
            val = sb.ToString();
#endif
            return val;
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

        public static string GetFirstRowFirstColumnValueAsString(string sql)
        {
            string s = "";
            DataTable dt = SQLUtility.GetDataTable(sql);
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {
                    s = dt.Rows[0][0].ToString();
                }
            }
            return s;
        }

        public static void SetParamValue(SqlCommand cmd, string paramname, object value)
        {
            try
            {
                cmd.Parameters[paramname].Value = value;
            }
            catch(Exception ex)
            {
                throw new Exception(cmd.CommandText + ":" + ex.Message, ex);
            }
        }

        private static string ParseConstraintMsg(string msg)
        {
            if (msg.Contains("The delete statement conflicted with the reference constraint"))
            {
                int tableIndex = msg.IndexOf("table \"") + 7;
                int endTableIndex = msg.IndexOf("\"", tableIndex);
                string childTable = msg.Substring(tableIndex, endTableIndex - tableIndex);

                return $"Cannot delete the record because it has related records in {childTable}.";
            }

            if (msg.Contains("unique key constraint"))
            {
                return "This record must be unique. A duplicate value exists.";
            }

            return "An error occurred while processing your request. Please ensure data integrity.";
        }

    }
}
