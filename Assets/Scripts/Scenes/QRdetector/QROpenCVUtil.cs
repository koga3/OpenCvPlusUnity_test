using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using System.Linq;
using UniRx;
using TMPro;
using OpenCvSharp;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
// using OpenCvSharp.ML;

namespace Kew
{
    public class QROpenCVUtil
    {
        // test        
        private List<Image> imageObjList;
        private Mat[] tempMat = new Mat[2] { new Mat(), new Mat() };

        //-----------------------------------------------------------------------------------------------------------------------------------

        // 画像処理
        // 足、マルを判定する範囲
        List<OpenCvSharp.Rect> judgeAreas = new List<OpenCvSharp.Rect>
            // {
            //     new OpenCvSharp.Rect(100, 60, 50, 50),
            //     new OpenCvSharp.Rect(145, 60, 50, 50),
            //     new OpenCvSharp.Rect(125, 245, 50, 50)
            // };            
            {
                new OpenCvSharp.Rect(135, 65, 50, 50),
                new OpenCvSharp.Rect(135, 252, 50, 50)
            };

        // 出力する数字画像の大きさ
        private readonly int numberWidth = 32;
        private readonly int numberHeight = 32;
        // 画面の縦横size
        private readonly OpenCvSharp.Size displaySize = new OpenCvSharp.Size(300, 312);
        // 画面を検出する際の最小サイズ
#if UNITY_EDITOR
        private readonly double minRectSize = 5000.0;
#else
        private readonly double minRectSize = 5000.0;
#endif
        public double MinRectSize => minRectSize;

        private Mat[] objectMats = new Mat[2];
        private List<GameObject> numberObjList;

        public QROpenCVUtil(List<Image> imageObjs)
        {
            // objectMats.CopyTo(this.objectMats);
            imageObjList = imageObjs;
            // ロード
        }

        // test
        public QROpenCVUtil(List<Mat> objectMats, List<Image> imageObjs, List<GameObject> numberObjList)
        {
            objectMats.CopyTo(this.objectMats);
            int i = 0;
            foreach (var mat in this.objectMats)
            {
                this.objectMats[i] = Threshold2(mat);
                i++;
            }
            imageObjList = imageObjs;
            this.numberObjList = numberObjList;
            // ロード
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
        // detect
        public string DetectQrCode(Mat input_image)
        {
            Mat output_image = input_image.Clone();
            Point2f[] points;
            Mat straight_qrcode = new Mat();
            // QRコード検出器
            QRCodeDetector detector = new QRCodeDetector();
            // QRコードの検出と復号化(デコード)
            string data = detector.DetectAndDecode(input_image, out points, straight_qrcode);
            if (data.Count() > 0)
            {
                // 復号化情報(文字列)の出力
                // Debug.Log("decoded data: " + data);
                // 検出結果の矩形描画
                for (int i = 0; i < points.Length; i++)
                {
                    // Cv2.Rectangle(output_image, points[i], points[(i + 1) % points.Length], new Scalar(0, 0, 255));
                    Debug.Log("points: " + points[i]);
                }
                Trimming(output_image, output_image, points);
                // imwrite("output.png", output_image);
                DisplayMat(output_image, 1);
                // おまけでQRコードのバージョンも計算
                // Debug.Log("QR code version: " + ((straight_qrcode.Rows - 21) / 4) + 1);
            }
            else
            {
                Debug.Log("QR code not detected");
            }

            return data;
        }

        public Mat Threshold(Mat image)
        {
            Cv2.CvtColor(image, image, ColorConversionCodes.BGRA2GRAY);
            Cv2.GaussianBlur(image, image, new Size(1, 1), 1000);
            var thresh = Cv2.Threshold(image, image, 0, 255.0, ThresholdTypes.Otsu);
            // Debug.Log("typetype: " + image.Type());
            return image;
        }

        public IEnumerable<Mat> Threshold(List<Mat> images)
        {
            return images.Select(x => Threshold(x));
        }

        public Mat Threshold2(Mat image)
        {
            Cv2.CvtColor(image, image, ColorConversionCodes.BGRA2GRAY);
            Cv2.MedianBlur(image, image, 5);
            Cv2.AdaptiveThreshold(image, image, 255.0, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 11, 2);
            return image;
        }

        public Mat Threshold3(Mat image)
        {
            Cv2.CvtColor(image, image, ColorConversionCodes.BGRA2GRAY);
            Cv2.MedianBlur(image, image, 7);
            // DisplayMat(image, 1);
            Cv2.AdaptiveThreshold(image, image, 255.0, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 9, 0.9);
            return image;
        }
        public Mat Threshold4(Mat image)
        {
            Cv2.CvtColor(image, image, ColorConversionCodes.BGRA2GRAY);
            // DisplayMat(image, 0);
            Cv2.MedianBlur(image, image, 7);
            DisplayMat(image, 2);
            Cv2.AdaptiveThreshold(image, image, 255.0, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 23, 0.7);
            return image;
        }
        public IEnumerable<Mat> Threshold2(List<Mat> images)
        {
            return images.Select(x => Threshold2(x));
        }

        private void Trimming(Mat src, OutputArray dst, IEnumerable<Point2f> points)
        {
            using (Mat rotated = new Mat())
            {
                // triming
                var rect = Cv2.MinAreaRect(points);
                // Debug.Log($"{rect.Size}, {rect.Size.Height / rect.Size.Width}");
                float angle = rect.Angle;
                // Debug.Log("Angle: " + rect.Angle);
                // テスト用タブレットでやったら、角度がずれていた
#if UNITY_EDITOR
                if (rect.Angle < -45)
                {
                    angle += 90;
                }
#else
                if (rect.Angle > 45)
                {
                    angle -= 90;
                }
#endif
                var m = Cv2.GetRotationMatrix2D(rect.Center, angle, 1.0);
                Cv2.WarpAffine(src, rotated, m, src.Size(), InterpolationFlags.Cubic);
                Cv2.GetRectSubPix(rotated, new Size(rect.Size.Width, rect.Size.Height), rect.Center, dst);
            }
        }

        // 画像から矩形の部分を検出、トリミング
        private IEnumerable<Mat> GetRectangles(Mat image)
        {
            using (Mat working = Threshold(image.Clone()))
            {
                //test
                working.CopyTo(tempMat[0]);
                // DisplayMat(working, 0);

                Point[][] contourPoints;
                HierarchyIndex[] i;
                Cv2.FindContours(working, out contourPoints, out i, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
                contourPoints = contourPoints.Where(points =>
                {
                    points = Cv2.ApproxPolyDP(points, 30.0, true);
                    return (points.Count() == 4 && Cv2.ContourArea(points) > minRectSize);
                }).ToArray();


                List<Mat> rects = new List<Mat>();
                foreach (var points in contourPoints)
                {
                    using (Mat adding = new Mat())
                    {
                        using (Mat rotated = new Mat())
                        {
                            // triming
                            var rect = Cv2.MinAreaRect(points);
                            if (!(0.90 < rect.Size.Height / rect.Size.Width && rect.Size.Height / rect.Size.Width < 1.2))
                            {
                                continue;
                            }
                            // Debug.Log($"{rect.Size}, {rect.Size.Height / rect.Size.Width}");
                            float angle = rect.Angle;
                            // Debug.Log("Angle: " + rect.Angle);
                            // テスト用タブレットでやったら、角度がずれていた
#if UNITY_EDITOR
                            if (rect.Angle < -45)
                            {
                                angle += 90;
                            }
#else
                            if (rect.Angle > 45)
                            {
                                angle -= 90;
                            }
#endif
                            var m = Cv2.GetRotationMatrix2D(rect.Center, angle, 1.0);
                            Cv2.WarpAffine(image, rotated, m, image.Size(), InterpolationFlags.Cubic);
                            Cv2.GetRectSubPix(rotated, new Size(rect.Size.Width, rect.Size.Height), rect.Center, adding);
                            //resize
                            // Cv2.Resize(adding, adding, displaySize);
                        }
                        rects.Add(adding.Clone());
                    }
                }
                // Debug.Log(rects.Aggregate(x => x.angle))
                return rects;
            }
        }

        // test
        public void DisplayMat(Mat src, int i, int width = 200)
        {
            Texture2D tex = new Texture2D(0, 0);
            tex = OpenCvSharp.Unity.MatToTexture(src);

            var target = imageObjList[i];
            target.sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            target.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, width * tex.height / tex.width);
            target.gameObject.SetActive(true);
        }

        public void DisplayNums(IEnumerable<Mat> matList)
        {
            int startIndex = 0;
            foreach (var mat in matList)
            {
                DisplayNum(mat, startIndex);
                startIndex++;
            }
            for (int i = startIndex; i < numberObjList.Count(); i++)
            {
                numberObjList[i].SetActive(false);
            }
        }

        private void DisplayNum(Mat src, int i)
        {
            Texture2D tex = new Texture2D(0, 0);
            tex = OpenCvSharp.Unity.MatToTexture(src);

            var target = numberObjList[i];
            var image = target.GetComponentInChildren<Image>();
            image.sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            image.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(200 * tex.width / tex.height, 200);
            target.SetActive(true);
        }
    }

}