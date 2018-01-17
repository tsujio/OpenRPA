using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenRPA.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace OpenRPA.Interpreter.Wml
{
    internal class ImageMatchingFailedException : Exception
    {
        internal ImageMatchingFailedException(string message) : base(message)
        {
        }
    }

    internal class ImageMatchingNode : WmlNode
    {
        internal const string TYPE = "ImageMatching";

        internal string ImageUrlPath { get; private set; }

        internal IList<int> StartPos { get; private set; }

        internal IList<int> EndPos { get; private set; }

        internal string WindowTitle { get; private set; }

        internal string Action { get; private set; }

        internal IList<int> ActionPos { get; private set; }

        internal int Timeout { get; private set; }

        internal ImageMatchingNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            ImageUrlPath = prop.imageUrlPath;
            StartPos = (prop.startPos as JArray).ToObject<IList<int>>();
            EndPos = (prop.endPos as JArray).ToObject<IList<int>>();
            WindowTitle = prop.windowTitle;
            Action = prop.action;
            ActionPos = (prop.actionPos as JArray).ToObject<IList<int>>();
            Timeout = prop.timeout;
        }

        internal override void Evaluate(Context context)
        {
            var url = context.Helper.GetFullUrl(ImageUrlPath);
            Bitmap imageToMatch = GetImageToMatch(url);

            FindWindowAndTryMatch(imageToMatch, out WindowModel window, out Rectangle matchingRect);

            DoAction(window, matchingRect);
        }

        private Stream FetchCapturedImage(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to fetch capture image");
                }

                var stream = response.Content.ReadAsStreamAsync().Result;

                return stream;
            }
        }

        private Bitmap GetImageToMatch(string url)
        {
            // Fetch original capture image from server
            var stream = FetchCapturedImage(url);
            Bitmap originalCapture = Image.FromStream(stream) as Bitmap;

            // Create canvas to draw image
            Bitmap imageToMatch = new Bitmap(
                Math.Abs(EndPos[0] - StartPos[0]),
                Math.Abs(EndPos[1] - StartPos[1])
            );

            // Extract area to match from fetched image
            using (Graphics g = Graphics.FromImage(imageToMatch))
            {
                g.DrawImage(originalCapture,
                    new Rectangle(0, 0, imageToMatch.Width, imageToMatch.Height),
                    new Rectangle(Math.Min(StartPos[0], EndPos[0]), Math.Min(StartPos[1], EndPos[1]),
                        imageToMatch.Width, imageToMatch.Height),
                    GraphicsUnit.Pixel);
            }

            return imageToMatch;
        }

        private void FindWindowAndTryMatch(Bitmap imageToMatch, out WindowModel window, out Rectangle matchingRect)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                try
                {
                    // Capture target window
                    window = WindowModel.FindByTitle(WindowTitle);

                    Bitmap capture = window.CaptureWindow();

                    // Find matching area in target window
                    matchingRect = FindMatchingRect(capture, imageToMatch);
                }
                catch (Exception)
                {
                    stopwatch.Stop();
                    if (stopwatch.Elapsed.TotalSeconds > Timeout)
                    {
                        throw;
                    }

                    Thread.Sleep(500);

                    // Retry
                    stopwatch.Start();
                    continue;
                }

                break;
            }
        }

        private Rectangle FindMatchingRect(Bitmap refBmp, Bitmap tmplBmp, double threshold = 1.0)
        {
            // Reference: https://stackoverflow.com/questions/32737420/multiple-results-in-opencvsharp3-matchtemplate

            using (Mat refMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(refBmp))
            using (Mat tmplMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(tmplBmp))
            using (Mat result = new Mat(refMat.Rows - tmplMat.Rows + 1, refMat.Cols - tmplMat.Cols + 1, MatType.CV_32FC1))
            {
                Cv2.MatchTemplate(refMat, tmplMat, result, TemplateMatchModes.CCoeffNormed);

                Rectangle rect = Rectangle.Empty;
                while (true)
                {
                    double minVal, maxVal;
                    OpenCvSharp.Point minLoc, maxLoc;
                    Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);

                    if (maxVal - threshold >= -1e-2)
                    {
                        if (!rect.IsEmpty)
                        {
                            throw new Exception("Matched multiple locations");
                        }

                        rect = new Rectangle(maxLoc.X, maxLoc.Y, tmplMat.Width, tmplMat.Height);

                        // Fill in the res Mat so you don't find the same area again in the MinMaxLoc
                        // TODO: Prefer inversed tmplMat to Scalar(0)
                        result.FloodFill(maxLoc, new Scalar(0));
                    }
                    else
                    {
                        break;
                    }
                }

                if (rect.IsEmpty)
                {
                    throw new ImageMatchingFailedException("Image matching failed");
                }

                return rect;
            }
        }

        private void DoAction(WindowModel window, Rectangle matchingRect)
        {
            var mouse = new MouseModel();

            // Move cursor to matching area
            var windowRect = window.GetRectangle();
            mouse.Move(
                windowRect.X + matchingRect.X + ActionPos[0],
                windowRect.Y + matchingRect.Y + ActionPos[1]
            );

            // Do action
            switch (Action)
            {
                case "Nothing":
                    break;

                case "LeftClick":
                    mouse.LeftClick();
                    break;

                case "RightClick":
                    mouse.RightClick();
                    break;

                case "DoubleLeftClick":
                    mouse.DoubleLeftClick();
                    break;

                default:
                    throw new Exception($"Unknown action {Action}");
            }
        }
    }
}
