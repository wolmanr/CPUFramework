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
            int firstQuote = msg.IndexOf('"');
            int secondQuote = msg.IndexOf('"', firstQuote + 1);
            if (firstQuote == -1 || secondQuote == -1)
                return msg;

            string constraintName = msg.Substring(firstQuote + 1, secondQuote - firstQuote - 1);

            if (constraintName.StartsWith("f_"))
            {
                int tableIndex = msg.IndexOf("table \"");
                if (tableIndex == -1)
                    return msg;

                int startTableName = tableIndex + 7;
                int endTableName = msg.IndexOf('"', startTableName);
                if (endTableName == -1)
                    return msg;

                string relatedTable = msg.Substring(startTableName, endTableName - startTableName);

                string[] msgParts = msg.Split(' ');
                string childTable = "record";
                for (int i = 0; i < msgParts.Length; i++)
                {
                    if (msgParts[i].Equals("constraint", StringComparison.OrdinalIgnoreCase) && i + 1 < msgParts.Length)
                    {
                        childTable = msgParts[i + 1].Trim('"');
                        break;
                    }
                }

                return $"Cannot delete {childTable} because it has a related {relatedTable} record";
            }

            return msg;
        }

    }
}
