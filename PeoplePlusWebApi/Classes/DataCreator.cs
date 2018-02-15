using System.Data;
using System.Data.OracleClient;
using System.Data.OleDb;

namespace PeoplePlusWebApi
{
    public class DataCreator
    {
        //execute a procedure and return status from database
        public static int ExecuteProcedure(string procedureName, object[] values)
        {
            //string connStr = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.152.2.27)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=neptune;Password=NEPTUNE;";
            string connStr = "Provider=OraOLEDB.Oracle;" + new DataProvider().GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(connStr))
            {
                using (OleDbCommand cmd = new OleDbCommand(procedureName, con))
                {
                    try
                    {
                        con.Open();
                        cmd.CommandType = CommandType.StoredProcedure;
                        for (int i = 0; i < values.Length; i++)
                        {
                            cmd.Parameters.AddWithValue("param" + (i + 1), values[i]).Direction = ParameterDirection.Input;
                        }
                        cmd.Parameters.AddWithValue("sp_outnmbr", OracleType.Number).Direction = ParameterDirection.Output;
                        cmd.ExecuteNonQuery();
                        int status = (int)cmd.Parameters["sp_outnmbr"].Value;
                        return status;
                    }
                    finally
                    {
                        con.Close();
                        con.Dispose();
                        cmd.Dispose();
                    }
                }
            }
        }

        public static int ExecuteSQL(string query)
        {
            using (OracleConnection con = new OracleConnection(new DataProvider().GetConnectionString()))
            {
                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    con.Open();
                    cmd.CommandType = CommandType.Text;  //default
                    int status = cmd.ExecuteNonQuery();
                    return status;
                }
            }
        }
    }
}