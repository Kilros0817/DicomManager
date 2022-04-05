using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace DicomManager.utility
{
    public class DBQuery
    {
        public static bool Is_Exist(string studyUid)
        {
            string query = $"SELECT COUNT(*) FROM queryresults WHERE studyuuid = '{studyUid}'";
            ENV.cmd.CommandText = query;
            MySqlDataReader rdr = ENV.cmd.ExecuteReader();
            rdr.Read();

            if (rdr.GetInt32(0) > 0)
            {
                rdr.Close();
                return true;
            }
            rdr.Close();

            return false;
        }

        public static string getBeforeStudyDate()
        {
            string query = $"SELECT StudyDate FROM queryresults ORDER BY StudyDate DESC LIMIT 1";
            ENV.cmd.CommandText = query;
            MySqlDataReader rdr = ENV.cmd.ExecuteReader();
            rdr.Read();
            string result = "";
            try
            {
                result = $"{rdr.GetString(0)}";
            }
            catch
            {
            }
            rdr.Close();
            return result;
        }

        public static void initSQLEnv()
        {
            ENV.cmd = new MySqlCommand();
            ENV.cmd.Connection = ENV.con;
        }
        public static void Execute_Query(string sql)
        {
            try
            {
                initSQLEnv();
                ENV.cmd.CommandText = sql;
                int result = ENV.cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
            }
        }

        public static DataTable CreateDataTable()
        {
            DataTable w_dt = new DataTable();
            w_dt.Columns.Add("id");
            w_dt.Columns.Add("studyuuid");
            w_dt.Columns.Add("PatientName");
            w_dt.Columns.Add("PatientID");
            w_dt.Columns.Add("PatientBD");
            w_dt.Columns.Add("StudyID");
            w_dt.Columns.Add("StudyDate");
            w_dt.Columns.Add("StudyTime");
            w_dt.Columns.Add("StudyDescription");
            w_dt.Columns.Add("Modalities");
            w_dt.Columns.Add("InstitutionName");
            w_dt.Columns.Add("AccessionNumber");
            w_dt.Columns.Add("PhysiciansName");

            return w_dt;
        }

        public static DataTable GetRecordFromDB(string p_studyuuid)
        {
            DataTable dt = CreateDataTable();

            try
            {
                initSQLEnv();
                string sql = $"SELECT * from queryresults where studyuuid = '{p_studyuuid}'";
                ENV.cmd.CommandText = sql;
                ENV.da = new MySqlDataAdapter();
                ENV.da.SelectCommand = ENV.cmd;
                ENV.da.Fill(dt);
                ENV.da.Dispose();
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
            }
            return dt;
        }
    }
}
