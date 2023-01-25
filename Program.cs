
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
            Console.WriteLine("4) Exit");
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
                    Console.WriteLine("Uploading (" + (i+1) + "/" + fileNamesList.Count + ") FilePath = " + Path.GetFullPath(fileName) + " to DicomWebServer");
                    
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
                Console.WriteLine("RetrieveImages - Response is not multipart. Response text =\n" + result);
            }

            Console.WriteLine("RetrieveAndSaveImages method finished. Press Enter to continue.");
        }

#endregion - Retrieve Images using WadoRS
 

    }
}