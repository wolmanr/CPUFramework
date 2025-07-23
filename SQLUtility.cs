using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

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
                    CheckReturnValue(cmd);
                    if (loadtable == true)
                    {
                        dt.Load(dr);
                    }
                }
                catch (SqlException ex)
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

        private static void CheckReturnValue(SqlCommand cmd)
        {
            int returnValue = 0;
            string msg = "";

            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.ReturnValue)
                    {
                        if (p.Value != null && p.Value != DBNull.Value)
                        {
                            returnValue = Convert.ToInt32(p.Value);
                        }
                    }
                    else if (p.ParameterName.Equals("@message", StringComparison.OrdinalIgnoreCase) &&
                             (p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.InputOutput))
                    {
                        if (p.Value != null && p.Value != DBNull.Value)
                        {
                            msg = p.Value.ToString();
                        }
                    }
                }

                if (returnValue == 1)
                {
                    if (string.IsNullOrWhiteSpace(msg))
                    {
                        msg = $"{cmd.CommandText} did not do action that was requested.";
                    }
                    throw new Exception(msg);
                }
            }
        }

        public static DataTable GetDataTable(string sqlstatement)
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



        private static void SetAllColumnAllowNull(DataTable dt)
        {
            foreach (DataColumn c in dt.Columns)
            {
                c.AllowDBNull = true;
            }
        }

        public static void DebugPrintDataTable(DataTable dt)
        {
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
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
            if (cmd.Connection != null)
            {
                sb.AppendLine($"--{cmd.Connection.DataSource}");
                sb.AppendLine($"use {cmd.Connection.Database}");
                sb.AppendLine("go");
            }
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendLine($"exec {cmd.CommandText}");
                int paramcount = cmd.Parameters.Count - 1;
                int paramnum = 0;
                string comma = ",";
                foreach (SqlParameter p in cmd.Parameters)
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
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
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
            catch (Exception ex)
            {
                throw new Exception(cmd.CommandText + ":" + ex.Message, ex);
            }
        }

        public static string ParseConstraintMsg(string msg)
        {
            string orgmsg = msg;
            string prefix = "";
            string msgend = "";

            if (msg.Contains("ck_"))
            {
                prefix = "ck_";
            }
            else if (msg.Contains("u_") || msg.Contains("U_"))
            {
                prefix = "u_";
                msgend = " must be unique";
            }
            else if (msg.Contains("f_"))
            {
                prefix = "f_";
            }
            else
            {
                return orgmsg;
            }

            msg = msg.Replace("\"", "'");
            int pos = msg.IndexOf(prefix);
            if (pos == -1)
            {
                return orgmsg;
            }

            msg = msg.Substring(pos + prefix.Length);
            int endpos = msg.IndexOf("'");
            if (endpos == -1)
            {
                return orgmsg;
            }

            msg = msg.Substring(0, endpos);
            msg = msg.Replace("_", " ") + msgend;

            if (prefix == "f_")
            {
                var words = msg.Split(" ");
                if (words.Length > 1)
                {
                    msg = $"Cannot delete {words[0]} because it has related {words[1]} record";
                }
            }
            return msg;
        }
        
    }
}
