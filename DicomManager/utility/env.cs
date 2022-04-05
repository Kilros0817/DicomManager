using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.IO;
using Newtonsoft.Json;


namespace DicomManager.utility
{
    static public class ENV
    {
        public static config CONFIG = new config();
        public static string PATH_CONFIG = string.Empty;

        public static MySqlConnection con;
        public static MySqlCommand cmd;
        public static MySqlDataAdapter da;
        public static DataTable dt;

        public static string TestServerHost = "127.0.0.1";
        public static int TestServerPort = 4006;
        public static string TestServerAET = "RADIUSPACS160";
        public static string TestAET = "TESTAPP";

        #region set environment
        public static void SetEnv()
        {
            PATH_CONFIG = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

            string envJson = string.Empty;

            envJson = File.ReadAllText(PATH_CONFIG);
            CONFIG = JsonConvert.DeserializeObject<config>(envJson);
            
            con = new MySqlConnection($"server = {CONFIG.DB_IP}; user id = {CONFIG.USER_ID}; pwd = {CONFIG.DB_PWD}; database={CONFIG.DB_NAME}");


            if (!Directory.Exists(ENV.CONFIG.PATH_DCM))
                Directory.CreateDirectory(ENV.CONFIG.PATH_DCM);
            if (!Directory.Exists(ENV.CONFIG.PATH_DCM_Processed))
                Directory.CreateDirectory(ENV.CONFIG.PATH_DCM_Processed);
            if (!Directory.Exists(ENV.CONFIG.PATH_JPG))
                Directory.CreateDirectory(ENV.CONFIG.PATH_JPG);
            if (!Directory.Exists(ENV.CONFIG.PATH_JPG_Processed))
                Directory.CreateDirectory(ENV.CONFIG.PATH_JPG_Processed);
        }

        #endregion
    }
}
