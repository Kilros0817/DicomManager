using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicomManager
{
    [Serializable]
    public class config
    {
        public config()
        {
            this.PATH_JPG = @"C:\MOBILEAPP\JPGS";
            this.PATH_JPG_Processed = @"C:\MOBILEAPP\JPGS\Processed";
            this.PATH_DCM = @"C:\MOBILEAPP\DCM";
            this.PATH_DCM_Processed = @"C:\MOBILEAPP\DCM\Processed";
            this.PATH_LOG = string.Empty;

            this.QRServerHost = "10.200.6.116";
            this.QRServerPort = 104;
            this.QRServerAET = "RADIUSPACS160";
            this.AET = "MOBILEAPP";

            this.TestServerHost = "127.0.0.1";
            this.TestServerPort = 4006;
            this.TestServerAET = "RADIUSPACS160";
            this.TestAET = "TESTAPP";

            this.DB_IP = "127.0.0.1";
            this.USER_ID = "root";
            this.DB_NAME = "premier_db";
            this.DB_PWD = "Fast2020!";
        }

        public string PATH_JPG { get; set; }
        public string PATH_JPG_Processed { get; set; }
        public string PATH_DCM { get; set; }
        public string PATH_DCM_Processed { get; set; }
        public string PATH_LOG { get; set; }
        public string QRServerHost { get; set; }
        public int QRServerPort { get; set; }
        public string QRServerAET { get; set; }
        public string AET { get; set; }
        public string TestServerHost { get; set; }
        public int TestServerPort { get; set; }
        public string TestServerAET { get; set; }
        public string TestAET { get; set; }
        public string DB_IP { get; set; }
        public string USER_ID { get; set; }
        public string DB_NAME { get; set; }
        public string DB_PWD { get; set; }
    }
}
