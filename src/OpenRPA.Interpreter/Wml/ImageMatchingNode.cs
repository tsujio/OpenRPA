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

namespace OpenRPA.Interpreter.Wml
{
    internal class ImageMatchingNode : WmlNode
    {
        internal string ImageUrlPath { get; private set; }

        internal IList<int> StartPos { get; private set; }

        internal IList<int> EndPos { get; private set; }

        internal string WindowTitle { get; private set; }

        internal const string TYPE = "ImageMatching";

        internal ImageMatchingNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            ImageUrlPath = prop.imageUrlPath;
            StartPos = (prop.startPos as JArray).ToObject<IList<int>>();
            EndPos = (prop.endPos as JArray).ToObject<IList<int>>();
            WindowTitle = prop.windowTitle;
        }

        internal override void Evaluate(Context context)
        {
            var url = context.Helper.GetFullUrl(ImageUrlPath);
            var stream = FetchCapturedImage(url);

            Bitmap originalCapture = Image.FromStream(stream) as Bitmap;
            Bitmap matchingImage = new Bitmap(EndPos[0] - StartPos[0], EndPos[1] - StartPos[1]);
            using (Graphics g = Graphics.FromImage(matchingImage))
            {
                g.DrawImage(originalCapture,
                    new Rectangle(0, 0, matchingImage.Width, matchingImage.Height),
                    new Rectangle(StartPos[0], StartPos[1], matchingImage.Width, matchingImage.Height),
                    GraphicsUnit.Pixel);
            }

            var window = WindowModel.FindByTitle(WindowTitle);
            Bitmap bmp = window.CaptureWindow();

            Rectangle matchingRect = FindMatchingRect(bmp, matchingImage);

            var windowRect = window.GetRectangle();

            var mouse = new MouseModel();
            mouse.Move(
                windowRect.X + matchingRect.X + matchingRect.Width / 2,
                windowRect.Y + matchingRect.Y + matchingRect.Height / 2,
                MouseModel.MouseActionType.LeftClick
            );
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

        private Rectangle FindMatchingRect(Bitmap refBmp, Bitmap tmplBmp, double threshold = 1.0)
        {
            // Reference: https://stackoverflow.com/questions/32737420/multiple-results-in-opencvsharp3-matchtemplate

            using (Mat refMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(refBmp))
            using (Mat tmplMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(tmplBmp))
            using (Mat result = new Mat(refMat.Rows - tmplMat.Rows + 1, refMat.Cols - tmplMat.Cols + 1, MatType.CV_32FC1))
            {
                Cv2.MatchTemplate(refMat, tmplMat, result, TemplateMatchModes.CCoeffNormed);

                Rectangle retval = Rectangle.Empty;
                while (true)
                {
                    double minVal, maxVal;
                    OpenCvSharp.Point minLoc, maxLoc;
                    Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);

                    if (maxVal - threshold >= -1e-3)
                    {
                        retval = new Rectangle(maxLoc.X, maxLoc.Y, tmplMat.Width, tmplMat.Height);

                        // Fill in the res Mat so you don't find the same area again in the MinMaxLoc
                        result.FloodFill(maxLoc, new Scalar(0));
                    }
                    else
                    {
                        break;
                    }
                }

                if (retval.IsEmpty)
                {
                    throw new Exception("Image matching failed");
                }

                return retval;
            }
        }
    }
}
