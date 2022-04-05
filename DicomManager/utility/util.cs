using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Dicom;
using Dicom.IO.Buffer;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.Network;
using MySql.Data.MySqlClient;

namespace DicomManager.utility
{
    public static class util
    {
        #region function 1
        public static int g_dataNum = 0;
        private static string log = string.Empty;

        private static string g_path = Path.Combine(Directory.GetCurrentDirectory(), "log.txt");

        public static void DownloadDb()
        {
            g_path = Path.Combine(ENV.CONFIG.PATH_LOG, $"log1.txt");
            WriteFunctionStartLogs($"--- Start downloading meta info from PACS");

            ENV.cmd = new MySqlCommand();
            ENV.cmd.Connection = ENV.con;

            try
            {
                string studyDT = DBQuery.getBeforeStudyDate();
                string studyDate = string.Empty;
                if (string.IsNullOrEmpty(studyDT))
                    studyDate = "20211001";
                else
                    studyDate = (DateTime.Parse(studyDT)).ToString("yyyyMMdd");

                studyDate = DateTime.Today.ToString("yyyyMMdd");

                string curDate = DateTime.Today.ToString("yyyyMMdd");

                studyDate = $"{studyDate}-{curDate}";

                g_dataNum = 0;//reset dataNum before new query

                var client = CreateCFindScuDicomClient("*", studyDate);
                client.Send(ENV.CONFIG.QRServerHost, ENV.CONFIG.QRServerPort, false, ENV.CONFIG.AET, ENV.CONFIG.QRServerAET);
                WriteFunctionEndLogs($"{g_dataNum} studies added!");
            }
            catch (Exception ex)
            {
                WriteFunctionEndLogs(ex.Message);
            }
            
        }

        public static DicomClient CreateCFindScuDicomClient(string patientName, string studyDate)
        {
            var cFindScuDicomClient = new DicomClient();
            cFindScuDicomClient.NegotiateAsyncOps();

            var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Study);

            // To retrieve the attributes of data you are interested in
            // that must be returned in the result
            // you must specify them in advance with empty parameters like shown below

            #region request data
            request.Dataset.AddOrUpdate(DicomTag.PatientName, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientID, "");
            request.Dataset.AddOrUpdate(DicomTag.PatientBirthDate, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyID, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyDate, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyTime, "");
            request.Dataset.AddOrUpdate(DicomTag.StudyDescription, "");
            request.Dataset.AddOrUpdate(DicomTag.EquipmentModality, "");
            request.Dataset.AddOrUpdate(DicomTag.InstitutionName, "");
            request.Dataset.AddOrUpdate(DicomTag.AccessionNumber, "");
            request.Dataset.AddOrUpdate(DicomTag.NameOfPhysiciansReadingStudy, "");
            #endregion

            // Specify the patient name filter 
            request.Dataset.AddOrUpdate(DicomTag.PatientName, patientName);

            request.Dataset.AddOrUpdate(DicomTag.StudyDate, studyDate);
            //request.Dataset.AddOrUpdate(DicomTag.StudyTime, studyTime);
            // Specify the encoding of the retrieved results
            // here the character set is 'Latin alphabet No. 1'
            request.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 100");


            // Find a list of Studies
            var studyUids = new List<string>();
            request.OnResponseReceived += (req, response) =>
            {
                WriteStudyResultsFoundToENV(response);
                studyUids.Add(response.Dataset?.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            };

            //add the request payload to the C FIND SCU Client
            cFindScuDicomClient.AddRequest(request);

            //Add a handler to be notified of any association rejections
            cFindScuDicomClient.AssociationRejected += OnAssociationRejected;

            //Add a handler to be notified of any association information on successful connections
            cFindScuDicomClient.AssociationAccepted += OnAssociationAccepted;

            //Add a handler to be notified when association is successfully released - this can be triggered by the remote peer as well
            cFindScuDicomClient.AssociationReleased += OnAssociationReleased;

            return cFindScuDicomClient;
        }
        public static void WriteStudyResultsFoundToENV(DicomCFindResponse response)
        {

            //data will continue to come as long as the response is 'pending' 
            if (response.Status == DicomStatus.Pending)
            {
                var patientName = response.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                var PatientID = response.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                var PatientSex = response.Dataset.GetSingleValueOrDefault(DicomTag.PatientSex, string.Empty);
                var PatientBD = response.Dataset.GetSingleValueOrDefault(DicomTag.PatientBirthDate, new DateTime());
                var StudyID = response.Dataset.GetSingleValueOrDefault(DicomTag.StudyID, string.Empty);
                var StudyInstanceUID = response.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
                var StudyDate = response.Dataset.GetSingleValueOrDefault(DicomTag.StudyDate, new DateTime());
                var StudyTime = response.Dataset.GetSingleValueOrDefault(DicomTag.StudyTime, new DateTime());
                var StudyDescription = response.Dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty);
                var Modalities = response.Dataset.GetSingleValueOrDefault(DicomTag.EquipmentModality, string.Empty);
                var InstitutionName = response.Dataset.GetSingleValueOrDefault(DicomTag.InstitutionName, string.Empty);
                var AccessionNumber = response.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                var PhysiciansName = response.Dataset.GetSingleValueOrDefault(DicomTag.NameOfPhysiciansReadingStudy, string.Empty);


                if (!DBQuery.Is_Exist(StudyInstanceUID))
                {
                    g_dataNum++;
                    string sql = string.Empty;
                    sql = "INSERT INTO queryresults (patientName, PatientID, PatientBD,"
                                                  + "StudyID, studyuuid, StudyDate,"
                                                  + "StudyTime, StudyDescription, Modalities,"
                                                  + "InstitutionName, AccessionNumber, PhysiciansName) Values (";

                    patientName = string.Concat(patientName.Split(new char[] { ',', '\''}));

                    sql += $"'{patientName}', '{PatientID}', '{PatientBD.ToString("yyyy-MM-dd")}', '{StudyID}', '{StudyInstanceUID}', '{StudyDate.ToString("yyyy-MM-dd")}', '{ StudyTime.ToString("HH:mm:ss") }', '{ StudyDescription }', '{ Modalities }', '{ InstitutionName }', '{ AccessionNumber }', '{ PhysiciansName }' )";

                    DBQuery.Execute_Query(sql);

                }
                if (response.Status == DicomStatus.Success)
                {
                    WriteLogs(response.Status.ToString());
                }
            }
        }
        private static void OnAssociationAccepted(object sender, AssociationAcceptedEventArgs e)
        {
            WriteLogs($"Association was accepted by:{e.Association.RemoteHost}");
        }

        private static void OnAssociationRejected(object sender, AssociationRejectedEventArgs e)
        {
            WriteLogs($"Association was rejected. Rejected Reason:{e.Reason}");
        }

        private static void OnAssociationReleased(object sender, EventArgs e)
        {
            WriteLogs("Association was released. BYE BYE");
        }
        public static void WriteLogs(string informationToLog)
        {
            File.AppendAllText(g_path, $"{informationToLog}\n");
        }
        public static void WriteFunctionStartLogs(string informationToLog)
        {
            File.AppendAllText(g_path, $"\n{DateTime.Now} {informationToLog}\n");
        }
        public static void WriteFunctionEndLogs(string informationToLog)
        {
            File.AppendAllText(g_path, $"\n{informationToLog}\n--- End ---");
        }
        #endregion
        #region function 2
        public static void ConvertJPG2DCM()
        {
            g_path = Path.Combine(ENV.CONFIG.PATH_LOG, $"log2.txt");

            WriteFunctionStartLogs("--- Start converting jpg 2 dcm");
            string[] w_jpgFileList = Directory.GetFiles(ENV.CONFIG.PATH_JPG);
            try
            {
                foreach (string w_jpgFile in w_jpgFileList)
                {
                    string w_jpgName = Path.GetFileNameWithoutExtension(w_jpgFile);
                    string w_dcmFile = Path.Combine(ENV.CONFIG.PATH_DCM, $"{w_jpgName}.dcm");

                    ImportImage(w_jpgFile, w_dcmFile);

                    string w_description = w_jpgName.Split('_')[1];

                    DicomFile dicomFile = DicomFile.Open(w_dcmFile, FileReadOption.ReadAll);

                    DataTable w_dt = DBQuery.GetRecordFromDB(w_jpgName.Split('_')[0]); //get result for jpgfile from localdb
                    dicomFile = FillDataSet(dicomFile, w_dt, w_description);

                    dicomFile.Save(w_dcmFile);

                    //move processed jpg file to processed folder
                    string w_jpgFileProcessed = Path.Combine(ENV.CONFIG.PATH_JPG_Processed, Path.GetFileName(w_jpgFile));
                    File.Move(w_jpgFile, w_jpgFileProcessed);
                }
                WriteFunctionEndLogs("Convert all files!");

            }
            catch (Exception ex)
            {
                WriteFunctionEndLogs(ex.Message);
            }
        }

        public static DicomFile FillDataSet(DicomFile dicom, DataTable dt, string description)
        {
            string w_patientBDStr = DateTime.Parse(dt.Rows[0]["PatientBD"].ToString()).ToString("yyyyMMdd");
            string w_studyDateStr = DateTime.Parse(dt.Rows[0]["StudyDate"].ToString()).ToString("yyyyMMdd");
            string w_studyTimeStr = DateTime.Parse(dt.Rows[0]["StudyTime"].ToString()).ToString("HHmmss");

            dicom.Dataset.AddOrUpdate<string>(DicomTag.StudyInstanceUID, dt.Rows[0]["studyuuid"].ToString());
            dicom.Dataset.AddOrUpdate<string>(DicomTag.PatientName, dt.Rows[0]["PatientName"].ToString());
            dicom.Dataset.AddOrUpdate<string>(DicomTag.PatientID, dt.Rows[0]["PatientID"].ToString());
            dicom.Dataset.AddOrUpdate<string>(DicomTag.PatientBirthDate, w_patientBDStr);
            dicom.Dataset.AddOrUpdate<string>(DicomTag.StudyID, dt.Rows[0]["StudyID"].ToString());
            dicom.Dataset.AddOrUpdate<string>(DicomTag.StudyDate, w_studyDateStr);
            dicom.Dataset.AddOrUpdate<string>(DicomTag.StudyTime, w_studyTimeStr);
            dicom.Dataset.AddOrUpdate<string>(DicomTag.StudyDescription, description);
            dicom.Dataset.AddOrUpdate<string>(DicomTag.EquipmentModality, dt.Rows[0]["Modalities"].ToString());
            dicom.Dataset.AddOrUpdate<string>(DicomTag.InstitutionName, dt.Rows[0]["InstitutionName"].ToString());
            dicom.Dataset.AddOrUpdate<string>(DicomTag.AccessionNumber, dt.Rows[0]["AccessionNumber"].ToString());
            dicom.Dataset.AddOrUpdate<string>(DicomTag.NameOfPhysiciansReadingStudy, dt.Rows[0]["PhysiciansName"].ToString());

            dicom.Dataset.AddOrUpdate<string>(DicomTag.Modality, "SC");
            dicom.Dataset.AddOrUpdate<string>(DicomTag.SeriesDescription, description);
            string w_seriesUid = Guid.NewGuid().ToString();
            dicom.Dataset.AddOrUpdate<string>(DicomTag.SeriesInstanceUID, w_seriesUid);

            return dicom;
        }

        public static void ImportImage(string srcfile, string destfile)
        {
            Bitmap bitmap = new Bitmap(srcfile);
            bitmap = GetValidImage(bitmap);
            int rows, columns;
            byte[] pixels = GetPixels(bitmap, out rows, out columns);
            MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);

            DicomDataset dataset = new DicomDataset();
            //FillDataset(dataset);
            dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
            dataset.Add(DicomTag.Rows, (ushort)rows);
            dataset.Add(DicomTag.Columns, (ushort)columns);
            dataset.Add(DicomTag.BitsAllocated, (ushort)8);
            dataset.Add(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.7");
            dataset.Add(DicomTag.SOPInstanceUID, GenerateUid());
            dataset.Add(DicomTag.ImageLaterality, "U");
            DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
            pixelData.BitsStored = 8;
            //pixelData.BitsAllocated = 8;
            //pixelData.PhotometricInterpretation = PhotometricInterpretation.Rgb;
            pixelData.SamplesPerPixel = 3;
            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = 0;
            pixelData.PlanarConfiguration = 0;
            pixelData.AddFrame(buffer);

            DicomFile dicomfile = new DicomFile(dataset);
            //var comfile = dicomfile.Clone(DicomTransferSyntax.JPEG2000Lossy);
            dicomfile.Save(destfile);
            bitmap.Dispose();
        }

        private static Bitmap GetValidImage(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Bitmap old = bitmap;
                using (old)
                {
                    bitmap = new Bitmap(old.Width, old.Height, PixelFormat.Format24bppRgb);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.DrawImage(old, 0, 0, old.Width, old.Height);
                    }
                }
            }
            return bitmap;
        }

        private static byte[] GetPixels(Bitmap image, out int rows, out int columns)
        {
            rows = image.Height;
            columns = image.Width;

            if (rows % 2 != 0 && columns % 2 != 0)
                --columns;

            BitmapData data = image.LockBits(new Rectangle(0, 0, columns, rows), ImageLockMode.ReadOnly, image.PixelFormat);
            IntPtr bmpData = data.Scan0;
            try
            {
                int stride = columns * 3;
                int size = rows * stride;
                byte[] pixelData = new byte[size];
                for (int i = 0; i < rows; ++i)
                    Marshal.Copy(new IntPtr(bmpData.ToInt64() + i * data.Stride), pixelData, i * stride, stride);

                //swap BGR to RGB
                SwapRedBlue(pixelData);
                return pixelData;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
        private static void SwapRedBlue(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 3)
            {
                byte temp = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = temp;
            }
        }
        private static DicomUID GenerateUid()
        {
            StringBuilder uid = new StringBuilder();
            uid.Append("1.2.840.10008.5.1.4.1.1.7").Append('.').Append(DateTime.UtcNow.Ticks);
            return new DicomUID(uid.ToString(), "SOP Instance UID", DicomUidType.SOPInstance);
        }
        #endregion
        #region function 3
        public static void DCMUpload()
        {
            g_path = Path.Combine(ENV.CONFIG.PATH_LOG, $"log3.txt");
            WriteFunctionStartLogs("--- Start Storing dcm to PACS.");

            string[] w_dcmFileList = Directory.GetFiles(ENV.CONFIG.PATH_DCM);
            try
            {
                foreach (string _dcmFile in w_dcmFileList)
                {
                    StoreDcm2PACS(_dcmFile);
                }
                WriteFunctionEndLogs("Store all files!");
            }
            catch (Exception ex)
            {
                WriteFunctionEndLogs(ex.Message);
            }
        }

        private static void StoreDcm2PACS(string dcmFile)
        {
            //create DICOM store SCU client with handlers
            var client = CreateDicomStoreClient(dcmFile);

            //send the verification request to the remote DICOM server
            //client.Send(ENV.CONFIG.QRServerHost, ENV.CONFIG.QRServerPort, false, ENV.CONFIG.AET, ENV.CONFIG.QRServerAET);
            client.Send(ENV.CONFIG.TestServerHost, ENV.CONFIG.TestServerPort, false, ENV.CONFIG.TestAET, ENV.CONFIG.TestServerAET);

        }
        private static DicomClient CreateDicomStoreClient(string fileToTransmit)
        {
            var client = new DicomClient();

            //request for DICOM store operation
            var dicomCStoreRequest = new DicomCStoreRequest(fileToTransmit);


            //attach an event handler when remote peer responds to store request 
            dicomCStoreRequest.OnResponseReceived += OnStoreResponseReceivedFromRemoteHost;
            dicomCStoreRequest.OnResponseReceived += (req, response) =>
            {
                MoveDCM(response, fileToTransmit);
            };
            client.AddRequest(dicomCStoreRequest);

            //Add a handler to be notified of any association rejections
            client.AssociationRejected += OnAssociationRejected;

            //Add a handler to be notified of any association information on successful connections
            client.AssociationAccepted += OnAssociationAccepted;

            //Add a handler to be notified when association is successfully released - this can be triggered by the remote peer as well
            client.AssociationReleased += OnAssociationReleased;

            return client;
        }
        private static void MoveDCM(DicomCStoreResponse response, string filePath)
        {
            if (response.Status == DicomStatus.Success)
            {
                string w_pDcmPath = Path.Combine(ENV.CONFIG.PATH_DCM_Processed, Path.GetFileName(filePath));
                File.Move(filePath, w_pDcmPath);
            }
        }

        private static void OnStoreResponseReceivedFromRemoteHost(DicomCStoreRequest request, DicomCStoreResponse response)
        {
            WriteLogs("DICOM Store request was received by remote host for storage...");
            WriteLogs($"DICOM Store request was received by remote host for SOP instance transmitted for storage:{request.SOPInstanceUID}");
            WriteLogs($"Store operation response status returned was:{response.Status}");
        }
        #endregion
    }
}
