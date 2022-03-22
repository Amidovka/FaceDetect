using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Diagnostics;

namespace FaceDetect
{
    class Program
    {
        private static string msg = "Please provide the API key as the first command-line parameter, followed by the filename of the image to be submitted for processing.";

        static void Main(string[] args)
        {
            // command line arguments

            var apiKey = !string.IsNullOrWhiteSpace(args[0]) ? args[0] : throw new ArgumentException(msg, args[0]);
            var fileName = File.Exists(args[1]) ? args[1] : throw new FileNotFoundException(msg, args[1]);

            // HTTP Request

            var region = "germanywestcentral";
            var target = new Uri($"https://{region}.api.cognitive.microsoft.com/face/v1.0/detect/?subscription-key={apiKey}");
            var httpPost = CreateHttpRequest(target, "POST", "application/octet-stream");

            // Load image

            using (var fs = File.OpenRead(fileName))
            {
                fs.CopyTo(httpPost.GetRequestStream());
            }

            // Submit the image to HTTP endpoint

            string data = GetResponse(httpPost);

            // Inspect JSON 

            var rectangles = GetRectangles(data);

            // Draw rectangles on the image (copy)

            var img = Image.Load(fileName);

            var count = 0;
            foreach (var rectangle in GetRectangles(data))
            {
                img.Mutate(a => a.DrawPolygon(Rgba32.ParseHex("#ff69b4"), 7, rectangle));
                count++;
            }
            Console.WriteLine($"Number of faces detected: {count}");
            var outputfilename = $"{Environment.CurrentDirectory}\\{Path.GetFileNameWithoutExtension(fileName)}-2{Path.GetExtension(fileName)}";
            SaveImage(img, outputfilename);

            OpenWithDefaultApp(outputfilename);
        }

        private static void OpenWithDefaultApp(string fileName)
        {
            var si = new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = fileName,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(si);
        }

        private static void SaveImage(Image img, string outputfilename)
        {
            using (var fs = File.Create(outputfilename))
            {
                img.SaveAsJpeg(fs);
            }
        }

        private static IEnumerable<PointF[]> GetRectangles(string data)
        {
            var faces = JArray.Parse(data);
            foreach (var face in faces)
            {
                var id = (string)face["faceId"];

                var top = (int)face["faceRectangle"]["top"];
                var left = (int)face["faceRectangle"]["left"];
                var width = (int)face["faceRectangle"]["width"];
                var height = (int)face["faceRectangle"]["height"];

                var rectangle = new PointF[]
                {
                    new PointF(left, top),
                    new PointF(left + width, top),
                    new PointF(left + width, top + height),
                    new PointF(left, top + height)
                };

                yield return rectangle;
            }
        }

        private static string GetResponse(HttpWebRequest httpPost)
        {
            using (var response = httpPost.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }

        private static HttpWebRequest CreateHttpRequest(Uri target, string method, string contentType)
        {
            var request = WebRequest.CreateHttp(target);
            request.Method = method;
            request.ContentType = contentType;
            return request;
        }
    }
}
