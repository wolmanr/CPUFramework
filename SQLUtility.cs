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
            
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.connectionstring))
            {
                cmd.Connection = conn;
                conn.Open();
                Debug.Print(GetSql(cmd));
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    dt.Load(dr);
                }
                catch(SqlException ex)
                {
                    string msg = ParseConstraintMsg(ex.Message);
                    throw new Exception(msg);
                }
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
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    string msg = ParseConstraintMsg(ex.Message);
                    if (string.IsNullOrEmpty(msg))
                    {
                        msg = ex.Message;
                    }
                    throw new Exception(msg);
                }
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

        private static string ParseConstraintMsg(string msg)
        {
            string origmsg = msg;
            int firstQuote = msg.IndexOf('"');
            int secondQuote = msg.IndexOf('"', firstQuote + 1);
            if (firstQuote == -1 || secondQuote == -1)
            {
                return origmsg;
            }
            string constraintName = msg.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
            string prefix = "";
            string msg_end = "";

            if (constraintName.StartsWith("ck_"))
            {
                prefix = "ck_";
            }
            else if (constraintName.StartsWith("u_"))
            {
                prefix = "u_";
                msg_end = " must be unique";
            }
            else if (constraintName.StartsWith("f_"))
            {
                prefix = "f_";
                msg_end = " is interfering with the delete due to a foreign key constraint";
            }
            else if (constraintName.StartsWith("c_"))
            {
                prefix = "c_";
            }
            else
            {
                  return origmsg;
            }
            string parsed = constraintName.Substring(prefix.Length).Replace("_", " ") + msg_end;

            return parsed;
        }

    }
}
