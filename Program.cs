using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Runtime.Serialization.Json;

namespace Merged
{
    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
        Unknown = -1
    };

    
    class Program
    {
        //static Sender sender;
        static string userId = "userId";

        static Socket mainSocket;                          //The socket which captures all incoming packets
        static byte[] byteData = new byte[4096];
        static bool bContinueCapturing = false;            //A flag to check if packets are to be captured or not
        static List<string> IPs = new List<string>();

        static void Main(string[] args)
        {
            userId = args[0];

            SnifferForm_Load();

            //if (!bContinueCapturing)
            //{
            //Start capturing the packets...

            //btnStart.Text = "&Stop";

            bContinueCapturing = true;

            //For sniffing the socket to capture the packets has to be a raw socket, with the
            //address family being of type internetwork, and protocol being IP
            mainSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Raw, ProtocolType.IP);

            //Bind the socket to the selected IP address
            mainSocket.Bind(new IPEndPoint(IPAddress.Parse("10.192.1.0"), 0));//IPs.Last())

            //Set the socket  options
            mainSocket.SetSocketOption(SocketOptionLevel.IP,            //Applies only to IP packets
                                       SocketOptionName.HeaderIncluded, //Set the include the header
                                       true);                           //option to true

            byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
            byte[] byOut = new byte[4] { 1, 0, 0, 0 }; //Capture outgoing packets

            //Socket.IOControl is analogous to the WSAIoctl method of Winsock 2
            mainSocket.IOControl(IOControlCode.ReceiveAll,              //Equivalent to SIO_RCVALL constant
                                                                        //of Winsock 2
                                 byTrue,
                                 byOut);

            //Start receiving the packets asynchronously
            mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(OnReceive), null);

            WebTrackerInitizalize();
            ///------KEY WORDS------
            BadWordsListInitialization();
            ///------EMOTIONS-------
            webcam = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            cam = new VideoCaptureDevice(webcam[0].MonikerString);

            cam.NewFrame += new NewFrameEventHandler(Cam_NewFrame);
            //cam. 
            cam.Start();
            ///------------------------
            while (true)
            {
                Thread.Sleep(5000);
                string windowTitle = GetActiveWindowTitle()?.ToLower().Trim();
                if (windowTitle != null)
                {
                    ++keyWordCounter;
                    foreach (string badWord in badWordsList)
                    {    
                        if (windowTitle.Contains(badWord))
                        {
                            //Debug.WriteLine("You asshole!");
                            badSearchList.Add(badWord);
                            ++badWordCounter;
                            break;
                        }
                    }
                }
                if(keyWordCounter >= 24)
                {
                    double negPerc = (double)badWordCounter / (double)keyWordCounter;
                    double pozPerc = 1 - negPerc;

                    Sender sender = new Sender();
                    sender.Uri = @"http://soerenq.com:9479/events";
                    sender.CreateObjectToSend(userId, negPerc, badSearchList, "Window");
                    sender.SerializeObjectToSend();
                    sender.Send();
                    //SEND IT
                    badWordsList = new List<string>();
                    keyWordCounter = 1;
                    badWordCounter = 1;
                }
            }

        
        }
    


        private static void OnReceive(IAsyncResult ar)
        {
            try
            {
                int nReceived = mainSocket.EndReceive(ar);

                //Analyze the bytes received...

                ParseData(byteData, nReceived);

                if (bContinueCapturing)
                {
                    byteData = new byte[4096];

                    //Another call to BeginReceive so that we continue to receive the incoming
                    //packets
                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "MJsniffer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static DateTime startTime = new DateTime(1000, 1, 1);//fake impossible date
        static System.Timers.Timer timer = new System.Timers.Timer();

        private static System.Timers.Timer timerInit()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Dispose();
            timer = new System.Timers.Timer();
            timer.AutoReset = false;
            timer.Interval = 60000;
            timer.Enabled = true;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(onTimedEvent);
            return timer;
        }

        private static void trackTime(string webPageName)
        {
            if (startingTimes[webPageName] == new DateTime(1000, 1, 1))
            {
                startingTimes[webPageName] = DateTime.Now;

                Sender sender = new Sender();
                sender.Uri = @"http://soerenq.com:9479/events";
                sender.CreateObjectToSend(userId, 1, new List<string>() { webPageName }, "WebTracking");
                sender.SerializeObjectToSend();
                sender.Send();


                //SEND START
                //webPageName
                //DataContractJsonSerializer dsa;
            }
            timers[webPageName]?.Dispose();
            timers[webPageName] = timerInit();
            timers[webPageName].Start();
            //}
            //else
            //{
            //    timers[webPagesName]?.Dispose();
            //    timers[webPagesName] = timerInit();
            //    timers[webPagesName].Start();
            //}


        }

        private static void onTimedEvent(object sender, ElapsedEventArgs e)
        {
            //now - startTime = time spent on fb to be written down

            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            string webPageName = (from t in timers
                                  where t.Value == timer
                                  select t.Key).First();// timers.Select(x => timers[x.Key] == timer).AsEnumerable();

            double timeDiff = ((DateTime.Now - startingTimes[webPageName]).TotalMinutes);
            if (timeDiff - 1 > 0)
            {
                timeDiff -= 1;
            }
            //Debug.WriteLine(timeDiff + $" {webPageName}");

            Sender senderClient = new Sender();
            senderClient.Uri = @"http://soerenq.com:9479/events";
            senderClient.CreateObjectToSend(userId, 0, new List<string>() {webPageName}, "WebTracking");
            senderClient.SerializeObjectToSend();
            senderClient.Send();

            //SEND END
            //webPageName


            startingTimes[webPageName] = new DateTime(1000, 1, 1);
            //anders.stormer@gmail.com

        }

        static Dictionary<string, List<string>> blockedWebPages;

        private static void InitializeBockedIpList()
        {
            List<string> FacebookIps = new List<string>() { "66.220", "69.63.", "204.15", "31.13." };
            List<string> NetflixIps = new List<string>() { "198.38", "52.210", "52.19.", "52.208", "52.18.", "52.209", "176.34", "52.17.", "52.18." };

            blockedWebPages = new Dictionary<string, List<string>>();
            blockedWebPages.Add("Facebook", FacebookIps);
            blockedWebPages.Add("Netflix", NetflixIps);
        }

        private static void WebTrackerInitizalize()
        {
            InitializeBockedIpList();
            InitializeStartingTimes();
            InitializeTimers();
        }

        private static bool doesIpBelongsTo(string ip, List<string> webPortal)
        {
            if (webPortal.Contains(ip.Substring(0, 6)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static Dictionary<string, System.Timers.Timer> timers;

        private static void InitializeTimers()
        {
            timers = new Dictionary<string, System.Timers.Timer>();
            foreach (string webPageName in blockedWebPages.Keys)
            {
                timers.Add(webPageName, new System.Timers.Timer());
            }
        }

        static Dictionary<string, DateTime> startingTimes;

        private static void InitializeStartingTimes()
        {
            startingTimes = new Dictionary<string, DateTime>();
            foreach (string webPageName in blockedWebPages.Keys)
            {
                startingTimes.Add(webPageName, new DateTime(1000, 1, 1));
            }
        }

        private static void TrackBadWebPages(IPHeader ipHeader)
        {
            foreach (string webPageName in blockedWebPages.Keys)
            {
                if (doesIpBelongsTo(ipHeader.DestinationAddress.ToString(), blockedWebPages[webPageName]))
                {
                    Debug.WriteLine($"I am at {webPageName}!!");
                    trackTime(webPageName);
                }
            }
        }

        private static void ParseData(byte[] byteData, int nReceived)
        {

            //Since all protocol packets are encapsulated in the IP datagram
            //so we start by parsing the IP header and see what protocol data
            //is being carried by it
            IPHeader ipHeader = new IPHeader(byteData, nReceived);


            TrackBadWebPages(ipHeader);


        }

        private static void SnifferForm_Load()
        {
            string strIP = null;

            IPHostEntry HosyEntry = Dns.GetHostEntry((Dns.GetHostName()));
            if (HosyEntry.AddressList.Length > 0)
            {
                foreach (IPAddress ip in HosyEntry.AddressList)
                {
                    strIP = ip.ToString();
                    IPs.Add(strIP);
                }
            }
        }



        ///-------------------------KEY WORDS WATCHER------------------------------
        protected delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        protected static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        protected static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        private static int keyWordCounter = 1;
        private static int badWordCounter = 1;
        private static List<string> badSearchList = new List<string>();

        protected static bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
        {
            int size = GetWindowTextLength(hWnd);
            if (size++ > 0 && IsWindowVisible(hWnd))
            {
                StringBuilder sb = new StringBuilder(size);
                GetWindowText(hWnd, sb, size);
                Console.WriteLine(sb.ToString());
            }
            return true;
        }

        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        static List<string> badWordsList;
        private static void BadWordsListInitialization()
        {
            badWordsList = new List<string>() { "facebook", "twitter", "netflix", "porn", "beer",
                "notepad", "saper", "starcraft", "game", "cat", "solitaire","tv","movie","trailer",
            "youtube","tinder","news","food"};

        }

        //------------------------EMOTION RECOGNITION-----------------------------------
        private static FilterInfoCollection webcam;
        private static VideoCaptureDevice cam;
        private static string picturePath = @"C:\Users\Me\documents\visual studio 2015\Projects\PYTHON\NetMonitor\EmotionRecognition\Photos\photo";
        private static int photoId = 1;

        private static int happyFaceCounter = 1;
        private static int faceCounter = 1;

        private static void Cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Thread.Sleep(10000);
            Bitmap bit = (Bitmap)eventArgs.Frame.Clone();
            try
            {
                bit.Save(picturePath + photoId + ".Jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
                photoId = (photoId + 1) % 20;
                if (photoId == 0)
                {
                    ++photoId;
                }
            }
            catch (Exception ex)
            {
                return;
            }
            MakeRequest(picturePath);
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        static async void MakeRequest(string imageFilePath)
        {
            //CameraCaptureTask cameraCaptureTask = new CameraCaptureTask();
            //cameraCaptureTask.Completed += cameraCaptureTask_Completed;
            //cameraCaptureTask.Show();


            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "bc0829f112974b1caa932f2f0440f6b0");

            string uri = "https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize?";
            HttpResponseMessage response;
            string responseContent;

            // Request body. Try this sample with a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath + (photoId - 1) + ".Jpeg");

            using (var content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
                responseContent = response.Content.ReadAsStringAsync().Result;

                double happinessVal = 0;
                for (int i = 0; i + 8 < responseContent.Length; ++i)
                {
                    if (responseContent.Substring(i, 9).Equals("happiness"))
                    {
                        //Debug.WriteLine(responseContent.Substring(i + 11, 9));
                        string number = "";
                        i += 11;
                        while ((responseContent[i] >= '0' && responseContent[i] <= '9') || responseContent[i] == '.')
                        {
                            number += responseContent[i];
                            ++i;
                        }
                        happinessVal = System.Convert.ToDouble(number.Replace('.', ','));
                        break;
                    }
                    //Debug.WriteLine(happiness);
                }
                if (happinessVal > 0.5)
                {
                    //Debug.WriteLine("You are too happy man!");
                    ++happyFaceCounter;

                }
                ++faceCounter;
                if(faceCounter >= 12)
                {
                    double happyTime = (double)happyFaceCounter / (double)faceCounter;
                    double productiveTime = 1 - happyTime;

                    Sender senderClient = new Sender();
                    senderClient.Uri = @"http://soerenq.com:9479/events";
                    senderClient.CreateObjectToSend(userId, happyTime, new List<string>(), "Face");
                    senderClient.SerializeObjectToSend();
                    senderClient.Send();



                    //SEND IT
                    happyFaceCounter = 1;
                    faceCounter = 1;
                }
                //else
                //{
                //    //Debug.WriteLine("Cool!");
                //}
            }

            //A peak at the JSON response.
            //Console.WriteLine(responseContent);



        }
    }
}
