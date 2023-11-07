using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System.Threading;
using OpenCvSharp.ML;
using System.Security.Cryptography.X509Certificates;

namespace Kew
{
    public class OCRWithSVM : OCROpenCVUtil
    {
        private readonly Size numImageSize = new Size(10, 10);
        public OCRWithSVM(List<Mat> objectMats) : base(objectMats)
        {

        }

        public void Training()
        {
            var svm = SVM.Create();

            // 訓練用画像の準備
            var trainData = new List<Mat>[10];
            foreach (var (data, number) in numberData.Select((x, i) => (x, i)))
            {
                for (int i = 0; i < data.Count; i++)
                {
                    int xBias = ((i % maxColumn)) * numberWidth;
                    int yBias = ((i / maxColumn)) * numberHeight;
                    int xCenter = xBias + numberWidth / 2;
                    int yCenter = yBias + numberHeight / 2;
                    // var data = GetPixels(number.Pixels, xBias, yBias, numberWidth, numberHeight);
                    trainData[number].Add(data.Mat.GetRectSubPix(new Size(numberWidth, numberHeight), new Point2f(xCenter, yCenter)));
                }
                trainData[number].ForEach(x => Cv2.Resize(x, x, numImageSize, interpolation: InterpolationFlags.Cubic));
            }
        }
    }
}