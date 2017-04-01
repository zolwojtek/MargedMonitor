using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Merged
{
    public class Sender
    {
        public string Uri { get; set; }
        private HttpClient client = new HttpClient();
        private SenderObject obj;
        private DataContractJsonSerializer serializer;
        MemoryStream stream = new MemoryStream();

        public void CreateObjectToSend(string userId, double procrastination, List<string>tags, string type)
        {
            obj = new SenderObject();
            obj.timeStamp = DateTime.Now;
            obj.userId = userId;
            obj.procrastination = procrastination;
            obj.tags = tags;
            obj.type = "Window";
        }

        public void SerializeObjectToSend()
        {
            
            serializer = new DataContractJsonSerializer(typeof(SenderObject));
            serializer.WriteObject(stream, obj);

            //stream.Position = 0;
            //StreamReader sr = new StreamReader(stream);
            //Debug.Write("JSON form of window object: ");
            //Debug.WriteLine(sr.ReadToEnd());
        }

        public void Send()
        {
            //HttpResponseMessage response = await client.PostAsync(uri, obj);
            //string responseContent = response.Content.ReadAsStringAsync().Result;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(Uri);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                stream.Position = 0;
                StreamReader sr = new StreamReader(stream);
                //Debug.Write("JSON form of window object: ");
                //Debug.WriteLine(sr.ReadToEnd());
                streamWriter.Write(sr.ReadToEnd());
                streamWriter.Flush();
                streamWriter.Close();
            }

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    Debug.WriteLine(result);
                }
            }
            catch (Exception ex)
            { }
        }


    }
}
