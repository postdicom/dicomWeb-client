
using Newtonsoft.Json;

namespace HelloWorld
{
    class Program
    {
        static Program? program;
        string userName = "your PostDICOM user name";
        string password = "your PostDICOM password";
        string webAddress = "PostDICOM DicomWeb server Address";

        static void Main(string[] args)
        {
            program = new Program();
            program.ShowMainMenu();
        }

        private bool ShowMainMenu()
        {
            Console.Clear();
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1) Upload DICOM Images in a folder");
            Console.WriteLine("2) QIDO Search");
            Console.WriteLine("3) WadoRS Retrieve Images");
            Console.WriteLine("4) Create share link");
            Console.WriteLine("5) Share patient order with URL");
            Console.WriteLine("6) Create folder");
            Console.WriteLine("7) Search folder");
            Console.WriteLine("8) Share folder with URL");
            Console.WriteLine("9) Add order to folder");
            Console.WriteLine("10) Assign order to user");
            Console.WriteLine("11) Assign order to user group");
            Console.WriteLine("12) Create patient order");
            Console.WriteLine("13) Get patient order properties");
            Console.WriteLine("14) Get DICOM Tag Content by PatientOrderUuid (This method can only be used by authorized accounts. For support, contact support@postdicom.com)");
            Console.WriteLine("15) Search for patient orders");
            Console.WriteLine("16) Delete patient order");
            Console.WriteLine("0) Exit");
            Console.Write("\r\nSelect an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    UploadDicomImagesInAFolder();
                    return true;
                case "2":
                    QidoSearch();
                    Console.ReadLine();
                    return true;
                case "3":
                    RetrieveImagesUsingWadoRs();
                    Console.ReadLine();
                    return true;
                case "4":
                    CreateShareLink();
                    Console.ReadLine();
                    return true;
                case "5":
                    SharePatientOrderWithUrl();
                    Console.ReadLine();
                    return true;
                case "6":
                    CreateFolder();
                    Console.ReadLine();
                    return true;
                case "7":
                    SearchFolder();
                    Console.ReadLine();
                    return true;
                case "8":
                    ShareFolderWithUrl();
                    Console.ReadLine();
                    return true;
                case "9":
                    AddOrderToFolder();
                    Console.ReadLine();
                    return true;
                case "10":
                    AssignOrderToUser();
                    Console.ReadLine();
                    return true;
                case "11":
                    AssignOrderToUserGroup();
                    Console.ReadLine();
                    return true;
                case "12":
                    CreatePatientOrder();
                    Console.ReadLine();
                    return true;
                case "13":
                    GetPatientOrderProperties();
                    Console.ReadLine();
                    return true;
                case "14":
                    GetDicomTagContent();
                    Console.ReadLine();
                    return true;
                case "15":
                    GetPatientOrderList();
                    Console.ReadLine();
                    return true;
                case "16":
                    DeletePatientOrder();
                    Console.ReadLine();
                    return true;
                default:
                    return true;
            }
        }

        #region Upload Images to DicomWeb Server

        private void UploadDicomImagesInAFolder()
        {
            Console.Clear();
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Uploading DICOM images in a folder.");

            string directory = "./test-images";
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Error cannot find directory \"" + Path.GetFullPath(directory) + "\"");
                Console.WriteLine("Press enter to return to main menu.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Searching *.dcm files in the folder.");

            List<string> fileNamesList = new List<string>();
            foreach (var path in Directory.EnumerateFiles(directory, "*.dcm", SearchOption.AllDirectories))
            {
                fileNamesList.Add(path);
            }

            Console.WriteLine(fileNamesList.Count + " images found in the folder.");

            if (fileNamesList.Count > 0)
            {
                UploadImagesToDicomWebServer(fileNamesList);
            }

            Console.ReadLine();
        }

        public async void UploadImagesToDicomWebServer(List<string> fileNamesList)
        {
            if (fileNamesList == null)
                return;

            var mimeType = "application/dicom";

            for (int i = 0; i < fileNamesList.Count; i++)
            {
                string fileName = fileNamesList[i];
                try
                {
                    Console.WriteLine("Uploading (" + (i + 1) + "/" + fileNamesList.Count + ") FilePath = " + Path.GetFullPath(fileName) + " to DicomWebServer");

                    StreamContent sContent = new StreamContent(File.OpenRead(fileName));
                    sContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                    MultipartContent multiContent = GetMultipartContent(mimeType);
                    multiContent.Add(sContent);

                    await StoreToServerAsync(multiContent);

                    Console.WriteLine("");
                }
                catch (Exception ex)
                {
                    string message = "Error while uploading file = " + fileName + "\nReason = " + ex.Message;
                    if (ex.InnerException != null)
                        message += "\nInnerException = " + ex.InnerException.Message;
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Uploading files finished.");
            Console.WriteLine("Press Enter to exit the program.");
        }

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related", "DICOM DATA BOUNDARY");

            multiContent.Headers.ContentType?.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", "\"" + mimeType + "\""));
            return multiContent;
        }

        private async Task StoreToServerAsync(MultipartContent multiContent)
        {
            try
            {
                string url = webAddress + "/";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + val);
                client.MaxResponseContentBufferSize = 2147483647;
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = multiContent;
                request.Headers.Add("Authorization", "Basic " + val);

                HttpResponseMessage response = await client.SendAsync(request);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                string responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response text =\n" + responseText);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }
        }

        #endregion - Upload Images to DicomWeb Server

        #region Query DICOM objects

        /// <summary>
        /// Search for DICOM objects (QIDO-RS) example 
        /// </summary>
        private async void QidoSearch()
        {
            string? qidoSearchParameter = "";
            string level = "";

            Console.Clear();
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1) SearchStudies-using-PatientID");
            Console.WriteLine("2) SearchStudies-using-StudyInstanceUID");
            Console.WriteLine("3) SearchSeries-using-StudyInstanceUID");
            Console.WriteLine("4) SearchImages-using-SeriesInstanceUID");
            Console.WriteLine("5) Exit");
            Console.Write("\r\nSelect an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    level = "SearchStudies-using-PatientID";
                    Console.WriteLine("1) SearchStudies-using-PatientID is selected.");
                    Console.Write("Please enter PatientID: ");
                    qidoSearchParameter = Console.ReadLine();
                    if (string.IsNullOrEmpty(qidoSearchParameter))
                        qidoSearchParameter = "22222222222";
                    break;
                case "2":
                    level = "SearchStudies-using-StudyInstanceUID";
                    Console.WriteLine("2) SearchStudies-using-StudyInstanceUID is selected.");
                    Console.Write("Please enter StudyInstanceUID: ");
                    qidoSearchParameter = Console.ReadLine();
                    return;
                case "3":
                    level = "SearchSeries-using-StudyInstanceUID";
                    Console.WriteLine("3) SearchSeries-using-StudyInstanceUID is selected.");
                    Console.Write("Please enter StudyInstanceUID: ");
                    qidoSearchParameter = Console.ReadLine();
                    return;
                case "4":
                    level = "SearchImages-using-SeriesInstanceUID";
                    Console.WriteLine("4) SearchImages-using-SeriesInstanceUID is selected.");
                    Console.Write("Please enter SeriesInstanceUID: ");
                    qidoSearchParameter = Console.ReadLine();
                    return;
                default:
                    return;
            }

            if (string.IsNullOrEmpty(qidoSearchParameter))
            {
                Console.WriteLine("Qido Seach Parameter is empty.");
                return;
            }


            if (level == "SearchStudies-using-PatientID")
            {
                string url = webAddress + "/studies?00100020=" + qidoSearchParameter;
                Console.WriteLine("SearchStudies-using-PatientID. URL = " + url);

                await SearchDicomWebServer(url);
            }
            else if (level == "SearchStudies-using-StudyInstanceUID")
            {
                string url = webAddress + "/studies?0020000D=" + qidoSearchParameter;
                Console.WriteLine("SearchStudies-using-StudyInstanceUID. URL = " + url);

                await SearchDicomWebServer(url);
            }
            else if (level == "SearchSeries-using-StudyInstanceUID")
            {
                string url = webAddress + "/studies/series?0020000D=" + qidoSearchParameter;
                Console.WriteLine("SearchSeries-using-StudyInstanceUID. URL = " + url);

                await SearchDicomWebServer(url);
            }
            else if (level == "SearchImages-using-SeriesInstanceUID")
            {
                string url = webAddress + "/studies/series/instances?0020000E=" + qidoSearchParameter;
                Console.WriteLine("SearchImages-using-SeriesInstanceUID. URL = " + url);

                await SearchDicomWebServer(url);
            }
        }

        private async Task SearchDicomWebServer(string url)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion - Query DICOM objects

        #region Retrieve Images using WadoRS

        private async void RetrieveImagesUsingWadoRs()
        {
            string? wadoSearchParameter = "";
            string level = "";

            Console.Clear();
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1) RetrieveImages-using-StudyInstanceUID");
            Console.WriteLine("2) RetrieveImages-using-SeriesInstanceUID");
            Console.WriteLine("3) RetrieveImages-using-SOPInstanceUID");
            Console.WriteLine("4) Exit");
            Console.Write("\r\nSelect an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    level = "RetrieveImages-using-StudyInstanceUID";
                    Console.WriteLine("1) RetrieveImages-using-StudyInstanceUID is selected.");
                    Console.Write("Please enter StudyInstanceUID: ");
                    wadoSearchParameter = Console.ReadLine();
                    break;
                case "2":
                    level = "RetrieveImages-using-SeriesInstanceUID";
                    Console.WriteLine("2) RetrieveImages-using-SeriesInstanceUID is selected.");
                    Console.Write("Please enter SeriesInstanceUID: ");
                    wadoSearchParameter = Console.ReadLine();
                    return;
                case "3":
                    level = "RetrieveImages-using-SOPInstanceUID";
                    Console.WriteLine("3) RetrieveImages-using-SOPInstanceUID is selected.");
                    Console.Write("Please enter SOPInstanceUID: ");
                    wadoSearchParameter = Console.ReadLine();
                    return;
                default:
                    return;
            }

            if (string.IsNullOrEmpty(wadoSearchParameter))
            {
                Console.WriteLine("Wado-RS retrieve parameter is empty.");
                return;
            }

            if (level == "RetrieveImages-using-StudyInstanceUID")
            {
                string url = webAddress + "/studies/" + wadoSearchParameter;
                Console.WriteLine("SearchStudies-using-PatientID. URL = " + url);

                await RetrieveAndSaveImages(url);
            }
            else if (level == "RetrieveImages-using-SeriesInstanceUID")
            {
                string url = webAddress + "/studies/series/" + wadoSearchParameter;
                Console.WriteLine("SearchStudies-using-StudyInstanceUID. URL = " + url);

                await RetrieveAndSaveImages(url);
            }
            else if (level == "RetrieveImages-using-SOPInstanceUID")
            {
                string url = webAddress + "/studies/series/instances/" + wadoSearchParameter;
                Console.WriteLine("SearchSeries-using-StudyInstanceUID. URL = " + url);

                await RetrieveAndSaveImages(url);
            }
        }

        private async Task RetrieveAndSaveImages(string url)
        {
            string result = string.Empty;
            HttpMessageHandler handler = new HttpClientHandler()
            {
            };

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(url),
                Timeout = new TimeSpan(0, 2, 0)
            };

            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
            string val = System.Convert.ToBase64String(plainTextBytes);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);


            HttpResponseMessage response = httpClient.GetAsync(url).Result;

            if (response.Content.Headers.ContentType == null)
            {
                Console.WriteLine("Error - response.Content.Headers.ContentType is null");
            }
            else if (response.Content.Headers.ContentType.MediaType == null)
            {
                Console.WriteLine("Error - response.Content.Headers.ContentType.MediaType is null");
            }
            else if (response.Content.Headers.ContentType.MediaType.Contains("multipart"))
            {
                try
                {
                    var content = await response.Content.ReadAsMultipartAsync();

                    DateTime now = DateTime.Now;
                    string fileRoot = Directory.GetCurrentDirectory() + "/" + now.Year.ToString("0000") + now.Month.ToString("00") + now.Day.ToString("00") + "-" +
                        now.Hour.ToString("00") + now.Minute.ToString("00") + now.Second.ToString("00") + now.Millisecond.ToString("000");

                    int i = 1;
                    foreach (var item in content.Contents)
                    {
                        Stream sc = await item.ReadAsStreamAsync();

                        string fileName = fileRoot + "-" + i.ToString("000") + ".dcm";

                        Console.WriteLine("Writing multipart content (" + i + ") to " + fileName);
                        using (FileStream outputFileStream = new FileStream(fileName, FileMode.Create))
                        {
                            sc.CopyTo(outputFileStream);
                        }

                        i++;
                    }
                }
                catch (Exception ex)
                {
                    string message = "Error in RetrieveImages.\nReason = " + ex.Message;
                    if (ex.InnerException != null)
                        message += "\nInnerException = " + ex.InnerException.Message;

                    Console.WriteLine(message);
                }
            }
            else
            {
                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("RetrieveImages - Response is not multipart. Response text =\n" + result);
            }

            Console.WriteLine("RetrieveAndSaveImages method finished. Press Enter to continue.");
        }

        #endregion - Retrieve Images using WadoRS


        #region Create Share link

        private async void CreateShareLink()
        {
            Console.Clear();
            Console.Write("Please enter PatientOrderUuid: ");
            string patientOrderUuid = Console.ReadLine();

            Console.Write("Please enter ExpireDate(YYYY-MM-DD): ");
            string expireDate = Console.ReadLine();

            Console.Write("Please enter SharePassword: ");
            string password = Console.ReadLine();
            bool isDownloadable = false;

            await CreateShareLinkInternal(new List<string>() { patientOrderUuid }, expireDate, password, isDownloadable);
        }

        private async Task CreateShareLinkInternal(List<string> patientOrderUuidList, string expireDate, string sharePassword, bool isDownloadable)
        {
            List<string> patientOrderInfoList = new List<string>();

            foreach (var patientOrderUuid in patientOrderUuidList)
            {
                Dictionary<string, string> patientOrderInfoDictionary = new Dictionary<string, string>();
                patientOrderInfoDictionary.Add("PatientOrderUuid", patientOrderUuid);
                patientOrderInfoList.Add(JsonConvert.SerializeObject(patientOrderInfoDictionary));
            }

            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("PatientOrderInfoList", JsonConvert.SerializeObject(patientOrderInfoList));
            parameterDictionary.Add("ExpireDate", expireDate);
            parameterDictionary.Add("SharePassword", sharePassword);
            parameterDictionary.Add("Downloadable", isDownloadable.ToString());

            string url = webAddress + "/createsharelink";
            await CreateShareLinkDicomWebServer(url, parameterDictionary);
        }

        private async Task CreateShareLinkDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("ShareParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Create Share link

        #region Share patient order with url

        private async void SharePatientOrderWithUrl()
        {
            Console.Clear();
            Console.Write("Please enter PatientOrderUuid: ");
            string patientOrderUuid = Console.ReadLine();

            Console.Write("Please enter ExpireDate(YYYY-MM-DD): ");
            string expireDate = Console.ReadLine();

            Console.Write("Please enter SharePassword: ");
            string password = Console.ReadLine();

            Console.Write("Please enter GetOrdersInFolder(true or false): ");
            bool userCanDownloadStudies = Console.ReadLine() == "true";

            await SharePatientOrderWithUrlInternal(new List<string>() { patientOrderUuid }, expireDate, password, userCanDownloadStudies);
        }

        private async Task SharePatientOrderWithUrlInternal(List<string> patientOrderUuidList, string expireDate, string sharePassword, bool userCanDownloadStudies)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("OrderUuidList", JsonConvert.SerializeObject(patientOrderUuidList));
            parameterDictionary.Add("ExpireDate", expireDate);
            parameterDictionary.Add("SharePassword", sharePassword);
            parameterDictionary.Add("UserCanDownloadStudies", userCanDownloadStudies.ToString());

            string url = webAddress + "/sharepatientorderwithurl";
            await SharePatientOrderWithUrlDicomWebServer(url, parameterDictionary);
        }

        private async Task SharePatientOrderWithUrlDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("SharePatientOrderParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Share patient order with url

        #region Create Folder

        private async void CreateFolder()
        {
            Console.Clear();
            Console.Write("Please enter FolderName: ");
            string folderName = Console.ReadLine();

            Console.Write("Please enter FolderDescription: ");
            string folderDescription = Console.ReadLine();

            Console.Write("Please enter ParentFolderUuid: ");
            string parentFolderUuid = Console.ReadLine();

            await CreateFolderInternal(folderName, folderDescription, parentFolderUuid);
        }

        private async Task CreateFolderInternal(string folderName, string folderDescription, string parentFolderUuid)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("FolderName", folderName);
            parameterDictionary.Add("FolderDescription", folderDescription);
            parameterDictionary.Add("ParentFolderUuid", parentFolderUuid);

            string url = webAddress + "/createfolder";
            await CreateFolderDicomWebServer(url, parameterDictionary);
        }

        private async Task CreateFolderDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("CreateFolderParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Create Folder

        #region Search Folder

        private async void SearchFolder()
        {
            Console.Clear();
            Console.Write("Please enter ParentFolderUuid: ");
            string parentFolderUuid = Console.ReadLine();

            Console.Write("Please enter FolderName: ");
            string folderName = Console.ReadLine();

            Console.Write("Please enter GetOrdersInFolder(true or false): ");
            bool getOrdersInFolder = Console.ReadLine() == "true";



            await SearchFolderInternal(parentFolderUuid, folderName, getOrdersInFolder);
        }

        private async Task SearchFolderInternal(string parentFolderUuid, string folderName, bool getOrdersInFolder)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("ParentFolderUuid", parentFolderUuid);
            parameterDictionary.Add("FolderName", folderName);
            parameterDictionary.Add("GetOrdersInFolder", getOrdersInFolder.ToString());

            string url = webAddress + "/searchfolder";
            await SearchFolderDicomWebServer(url, parameterDictionary);
        }

        private async Task SearchFolderDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("SearchFolderParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Search Folder

        #region  Share Folder with url

        private async void ShareFolderWithUrl()
        {
            Console.Clear();
            Console.Write("Please enter FolderUuid: ");
            string folderUuid = Console.ReadLine();

            Console.Write("Please enter SharePassword: ");
            string sharePassword = Console.ReadLine();

            Console.Write("Please enter ShareTitle: ");
            string shareTitle = Console.ReadLine();

            Console.Write("Please enter ShareDescription: ");
            string shareDescription = Console.ReadLine();

            Console.Write("Please enter ExpireDate(YYYY-MM-DD): ");
            string expireDate = Console.ReadLine();

            Console.Write("Please enter UserCanDownloadStudies(true or false): ");
            bool userCanDownloadStudies = Console.ReadLine() == "true";



            await ShareFolderWithUrlInternal(folderUuid, sharePassword, shareTitle, shareDescription, expireDate, userCanDownloadStudies);
        }

        private async Task ShareFolderWithUrlInternal(string folderUuid, string sharePassword, string shareTitle, string shareDescription, string expireDate, bool userCanDownloadStudies)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("FolderUuid", folderUuid);
            parameterDictionary.Add("SharePassword", sharePassword);
            parameterDictionary.Add("ShareTitle", shareTitle);
            parameterDictionary.Add("ShareDescription", shareDescription);
            parameterDictionary.Add("ExpireDate", expireDate);
            parameterDictionary.Add("UserCanDownloadStudies", userCanDownloadStudies.ToString());

            string url = webAddress + "/sharefolderwithurl";
            await ShareFolderWithUrlDicomWebServer(url, parameterDictionary);
        }

        private async Task ShareFolderWithUrlDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("ShareFolderParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Share Folder with url

        #region  Add Order to Folder

        private async void AddOrderToFolder()
        {
            Console.Clear();
            Console.Write("Please enter PatientOrderUuid: ");
            string patientOrderUuid = Console.ReadLine();

            Console.Write("Please enter FolderUuid: ");
            string folderUuid = Console.ReadLine();



            await AddOrderToFolderInternal(patientOrderUuid, new List<string>() { folderUuid });
        }

        private async Task AddOrderToFolderInternal(string patientOrderUuid, List<string> folderUuidList)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("PatientOrderUuid", patientOrderUuid);
            parameterDictionary.Add("FolderUuidList", JsonConvert.SerializeObject(folderUuidList));

            string url = webAddress + "/addordertofolder";
            await AddOrderToFolderDicomWebServer(url, parameterDictionary);
        }

        private async Task AddOrderToFolderDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("AddOrderToFolderParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Add Order to Folder

        #region  Assign order to user

        private async void AssignOrderToUser()
        {
            Console.Clear();
            Console.Write("Please enter PatientOrderUuid: ");
            string patientOrderUuid = Console.ReadLine();

            Console.Write("Please enter AssignedUserUuid: ");
            string assignedUserUuid = Console.ReadLine();



            await AssignOrderToUserInternal(patientOrderUuid, assignedUserUuid);
        }

        private async Task AssignOrderToUserInternal(string patientOrderUuid, string assignedUserUuid)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("PatientOrderUuid", patientOrderUuid);
            parameterDictionary.Add("AssignedUserUuid", assignedUserUuid);

            string url = webAddress + "/assignordertouser";
            await AssignOrderToUserDicomWebServer(url, parameterDictionary);
        }

        private async Task AssignOrderToUserDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("AssignOrderToUserParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Assign order to user

        #region  Assign order to user group

        private async void AssignOrderToUserGroup()
        {
            Console.Clear();
            Console.Write("Please enter PatientOrderUuid: ");
            string patientOrderUuid = Console.ReadLine();

            Console.Write("Please enter AssignedUserGroupUuid: ");
            string assignedUserGroupUuid = Console.ReadLine();



            await AssignOrderToUserGroupInternal(patientOrderUuid, assignedUserGroupUuid);
        }

        private async Task AssignOrderToUserGroupInternal(string patientOrderUuid, string assignedUserGroupUuid)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("PatientOrderUuid", patientOrderUuid);
            parameterDictionary.Add("AssignedUserGroupUuid", assignedUserGroupUuid);

            string url = webAddress + "/assignordertousergroup";
            await AssignOrderToUserGroupDicomWebServer(url, parameterDictionary);
        }

        private async Task AssignOrderToUserGroupDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("AssignOrderToUserGroupParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("QidoSearch method finished. Press Enter to continue.");
        }

        #endregion Assign order to user group

        #region Create PatientOrder
        private async void CreatePatientOrder()
        {
            Console.Clear();
            Console.Write("Please enter InstitutionUuid(*required): ");
            string institutionUuid = Console.ReadLine(); //required parameter

            Console.Write("Please enter PatientName(*required): ");
            string patientName = Console.ReadLine(); //required parameter

            Console.Write("Please enter PatientId(*required): ");
            string patientId = Console.ReadLine(); //required parameter

            Console.Write("Please enter OtherPatientId: ");
            string otherPatientId = Console.ReadLine();

            Console.Write("Please enter PatientSex: ");
            string patientSex = Console.ReadLine();

            Console.Write("Please enter PatientBirthdate(*required)(format: YYYY-MM-DD): ");
            string patientBirthdate = Console.ReadLine(); //required parameter

            Console.Write("Please enter Modality(*required): ");
            string modality = Console.ReadLine(); //required parameter

            Console.Write("Please enter StudyDescription: ");
            string studyDescription = Console.ReadLine();

            Console.Write("Please enter AccessionNumber: ");
            string accessionNumber = Console.ReadLine();

            Console.Write("Please enter PatientComplaints: ");
            string patientComplaints = Console.ReadLine();

            Console.Write("Please enter OrderScheduledDatetime(YYYY-MM-DD HH:MM): ");
            string scheduleStartDate = Console.ReadLine();

            Console.Write("Please enter RequestedProcedureId: ");
            string requestedProcedureId = Console.ReadLine();

            Console.Write("Please enter RequestedProcedureDescription: ");
            string requestedProcedureDescription = Console.ReadLine();

            Console.Write("Please enter RequestingPhysician: ");
            string requestingPhysician = Console.ReadLine();

            Console.Write("Please enter ReferringPhysiciansName: ");
            string referringPhysiciansName = Console.ReadLine();

            Console.Write("Please enter ScheduledEquipmentUuid: ");
            string scheduledEquipmentUuid = Console.ReadLine();



            await CreatePatientOrderInternal(institutionUuid, patientName, patientId, otherPatientId, patientSex, patientBirthdate, modality, studyDescription, accessionNumber, patientComplaints, scheduleStartDate, requestedProcedureId, requestedProcedureDescription, requestingPhysician, referringPhysiciansName, scheduledEquipmentUuid);
        }

        private async Task CreatePatientOrderInternal(string institutionUuid, string patientName, string patientId, string otherPatientId, string patientSex, string patientBirthdate, string modality, string studyDescription, string accessionNumber, string patientComplaints, string scheduleStartDate, string requestedProcedureId, string requestedProcedureDescription, string requestingPhysician, string referringPhysiciansName, string scheduledEquipmentUuid)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("InstitutionUuid", institutionUuid); //required parameter
            parameterDictionary.Add("PatientName", patientName); //required parameter
            parameterDictionary.Add("PatientId", patientId); //required parameter
            parameterDictionary.Add("OtherPatientId", otherPatientId);
            parameterDictionary.Add("PatientSex", patientSex);
            parameterDictionary.Add("PatientBirthdate", patientBirthdate); //required parameter
            parameterDictionary.Add("Modality", modality); //required parameter
            parameterDictionary.Add("StudyDescription", studyDescription);
            parameterDictionary.Add("AccessionNumber", accessionNumber);
            parameterDictionary.Add("PatientComplaints", patientComplaints);
            parameterDictionary.Add("OrderScheduledDatetime", scheduleStartDate);
            parameterDictionary.Add("RequestedProcedureId", requestedProcedureId);
            parameterDictionary.Add("RequestedProcedureDescription", requestedProcedureDescription);
            parameterDictionary.Add("RequestingPhysician", requestingPhysician);
            parameterDictionary.Add("ReferringPhysiciansName", referringPhysiciansName);
            parameterDictionary.Add("ScheduledEquipmentUuid", scheduledEquipmentUuid);

            string url = webAddress + "/createpatientorder";
            await CreatePatientOrderDicomWebServer(url, parameterDictionary);
        }

        private async Task CreatePatientOrderDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("CreatePatientOrderParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("Method finished. Press Enter to continue.");
        }

        #endregion Create PatientOrder

        #region Get PatientOrder Properties

        private async void GetPatientOrderProperties()
        {
            Console.Clear();
            Console.Write("Please enter InstitutionUuid(*required): ");
            string institutionUuid = Console.ReadLine(); //required parameter

            Console.Write("Please enter PatientOrderUuid(*required): ");
            string patientOrderUuid = Console.ReadLine(); //required parameter

            await GetPatientOrderPropertiesInternal(institutionUuid, patientOrderUuid);
        }

        private async Task GetPatientOrderPropertiesInternal(string institutionUuid, string patientOrderUuid)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("PatientOrderInstitutionUuid", institutionUuid);
            parameterDictionary.Add("PatientOrderUuid", patientOrderUuid);

            string url = webAddress + "/getpatientorderproperties";
            await GetPatientOrderPropertiesDicomWebServer(url, parameterDictionary);
        }

        private async Task GetPatientOrderPropertiesDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("GetPatientOrderPropertiesParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("Method finished. Press Enter to continue.");
        }
        #endregion

        #region Get DICOM Tag Content by PatientOrderUuid

        private async void GetDicomTagContent()
        {
            Console.Clear();
            Console.Write("Please enter InstitutionUuid(*required): ");
            string institutionUuid = Console.ReadLine(); //required parameter

            Console.Write("Please enter PatientOrderUuid(*required): ");
            string patientOrderUuid = Console.ReadLine(); //required parameter

            Console.Write("Please enter PatientSeriesUuid: ");
            string patientSeriesUuid = Console.ReadLine();

            Console.Write("Please enter TagIdList(optional, comma-separated): ");
            string tagIdListString = Console.ReadLine();
            List<string> tagIdList = new List<string>(tagIdListString.Split(","));
            await GetDicomTagContentInternal(institutionUuid, patientOrderUuid, patientSeriesUuid, tagIdList);
        }

        private async Task GetDicomTagContentInternal(string institutionUuid, string patientOrderUuid, string patientSeriesUuid, List<string> tagIdList)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("PatientOrderInstitutionUuid", institutionUuid);
            parameterDictionary.Add("PatientOrderUuid", patientOrderUuid);
            parameterDictionary.Add("PatientSeriesUuid", patientSeriesUuid);
            parameterDictionary.Add("DicomTagIdList", JsonConvert.SerializeObject(tagIdList));

            string url = webAddress + "/getdicomtagcontent";
            await GetDicomTagContentDicomWebServer(url, parameterDictionary);
        }

        private async Task GetDicomTagContentDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("GetDicomTagContentParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("Method finished. Press Enter to continue.");
        }
        #endregion

        #region Get PatientOrder List

        private async void GetPatientOrderList()
        {
            Console.Clear();
            Console.Write("Please enter InstitutionUuidList: ");
            string institutionUuidListString = Console.ReadLine();
            List<string> institutionUuidList = new List<string>(institutionUuidListString.Split(","));

            Console.Write("Please enter PatientName: ");
            string patientName = Console.ReadLine();

            Console.Write("Please enter PatientId: ");
            string patientId = Console.ReadLine();

            Console.Write("Please enter OtherPatientId: ");
            string otherPatientId = Console.ReadLine();

            Console.Write("Please enter AccessionNumber: ");
            string accessionNumber = Console.ReadLine();

            Console.Write("Please enter ModalityList: ");
            string modalityListString = Console.ReadLine();
            List<string> modalityList = new List<string>(modalityListString.Split(","));

            Console.Write("Please enter StudyDateFrom(format: YYYY-MM-DD): ");
            string studyDateFrom = Console.ReadLine();

            Console.Write("Please enter StudyDateTo(format: YYYY-MM-DD): ");
            string studyDateTo = Console.ReadLine();

            Console.Write("Please enter PatientBirthdateFrom(format: YYYY-MM-DD): ");
            string patientBirthdateFrom = Console.ReadLine();

            Console.Write("Please enter PatientBirthdateTo(format: YYYY-MM-DD): ");
            string patientBirthdateTo = Console.ReadLine();

            await GetPatientOrderListInternal(institutionUuidList, patientName, patientId, otherPatientId, accessionNumber, modalityList, studyDateFrom, studyDateTo, patientBirthdateFrom, patientBirthdateTo);
        }

        private async Task GetPatientOrderListInternal(List<string> institutionUuidList, string patientName, string patientId, string otherPatientId, string accessionNumber, List<string> modalityList, string studyDateFrom, string studyDateTo, string patientbirthdateFrom, string patientBirthdateTo)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("InstitutionUuidList", JsonConvert.SerializeObject(institutionUuidList));
            parameterDictionary.Add("PatientName", patientName);
            parameterDictionary.Add("PatientId", patientId);
            parameterDictionary.Add("OtherPatientId", otherPatientId);
            parameterDictionary.Add("AccessionNumber", accessionNumber);
            parameterDictionary.Add("ModalityList", JsonConvert.SerializeObject(modalityList));

            parameterDictionary.Add("StudyDateFrom", studyDateFrom);
            parameterDictionary.Add("StudyDateTo", studyDateTo);
            parameterDictionary.Add("PatientBirthdateFrom", patientbirthdateFrom);
            parameterDictionary.Add("PatientBirthdateTo", patientBirthdateTo);

            string url = webAddress + "/getpatientorderlist";
            await GetPatientOrderListDicomWebServer(url, parameterDictionary);
        }

        private async Task GetPatientOrderListDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("GetPatientOrderListParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("Method finished. Press Enter to continue.");
        }
        #endregion

        #region Delete PatientOrder

        private async void DeletePatientOrder()
        {
            Console.Clear();
            Console.Write("Please enter InstitutionUuid(*required): ");
            string institutionUuid = Console.ReadLine(); //required parameter

            Console.Write("Please enter PatientOrderUuid(*required): ");
            string patientOrderUuid = Console.ReadLine(); //required parameter

            await DeletePatientOrderInternal(institutionUuid, patientOrderUuid);
        }

        private async Task DeletePatientOrderInternal(string institutionUuid, string patientOrderUuid)
        {
            Dictionary<string, string> parameterDictionary = new Dictionary<string, string>();
            parameterDictionary.Add("PatientOrderInstitutionUuid", institutionUuid);
            parameterDictionary.Add("PatientOrderUuid", patientOrderUuid);

            string url = webAddress + "/deleteorder";
            await DeletePatientOrderDicomWebServer(url, parameterDictionary);
        }

        private async Task DeletePatientOrderDicomWebServer(string url, Dictionary<string, string> parameterDictionary)
        {
            try
            {
                string result = string.Empty;
                HttpMessageHandler handler = new HttpClientHandler()
                {
                };

                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 2, 0)
                };

                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
                string val = System.Convert.ToBase64String(plainTextBytes);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

                httpClient.DefaultRequestHeaders.Add("DeleteOrderParameters", JsonConvert.SerializeObject(parameterDictionary));

                HttpResponseMessage response = await httpClient.GetAsync(url);
                Console.WriteLine("HttpResponseMessage.StatusCode = " + response.StatusCode);

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8))
                {
                    result = stream.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(response.ReasonPhrase))
                {
                    result += $"\nReason: {response.ReasonPhrase}";
                }

                Console.WriteLine("Response text =\n" + result);
            }
            catch (Exception ex)
            {
                string message = "Error while multicontent.\nReason = " + ex.Message;
                if (ex.InnerException != null)
                    message += "\nInnerException = " + ex.InnerException.Message;

                Console.WriteLine(message);
            }

            Console.WriteLine("Method finished. Press Enter to continue.");
        }
        #endregion
    }
}