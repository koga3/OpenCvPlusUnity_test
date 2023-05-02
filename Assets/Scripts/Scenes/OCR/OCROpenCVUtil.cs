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
    public class OCROpenCVUtil
    {
        // test        
        private List<Image> imageObjList;
        private Mat tempMat = new Mat();

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
                new OpenCvSharp.Rect(123, 60, 50, 50),
                new OpenCvSharp.Rect(125, 245, 50, 50)
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
        private readonly double minRectSize = 20000.0;
#endif
        public double MinRectSize => minRectSize;

        private Mat[] objectMats = new Mat[2];

        public OCROpenCVUtil(List<Mat> objectMats)
        {
            objectMats.CopyTo(this.objectMats);
        }

        // test
        public OCROpenCVUtil(List<Mat> objectMats, List<Image> imageObjs)
        {
            objectMats.CopyTo(this.objectMats);
            int i = 0;
            foreach (var mat in this.objectMats)
            {
                this.objectMats[i] = Threshold2(mat);
                i++;
            }
            imageObjList = imageObjs;
        }

        // 歩数確認画面かどうか判定して画面のMatを返す(歩数確認画面ではなければnull)
        public async UniTask<Mat> GetWalkCountDisplay(Mat src, CancellationToken token)
        {
            SynchronizationContext context = SynchronizationContext.Current;
            // Debug.Log(context);

            Mat retval = null;
            // 実装用
            await UniTask.RunOnThreadPool(() =>
            {
                // 矩形の画像を抽出(カラー)
                var matList = GetRectangles(src);

                int i = 0, maxCount = 0;
                retval = matList.Count() > 0 ? matList.ToList()[0] : null; // testように矩形があったらとりあえず通す
                foreach (var image in matList)
                {
                    matList.ToList()[i] = Threshold2(image);
                    var points = GetPointByTemplateMatching(image, objectMats).ToList();
                    // Debug.Log($"Left Foot : {points[0]} {judgeAreas[0].Contains(points[0])}, Right Foot : {points[1]} {judgeAreas[1].Contains(points[1])}, Circle : {points[2]} {judgeAreas[2].Contains(points[2])}");

                    var matchCnt = judgeAreas
                        .Select((x, i) => new { area = x, i = i })
                        .Count(x => x.area.Contains(points[x.i]));

                    // if (matchCnt >= 2)
                    {
                        if (matchCnt > maxCount)
                        {
                            maxCount = matchCnt;
                            retval = image;
                        }
                    }
                    i++;
                }
            }, cancellationToken: token);

            return retval;
        }

        // 歩数確認画面から数字部分を抜き出す
        public async UniTask<IEnumerable<Tuple<Mat, string>>> ClipNumber(Mat src, CancellationToken token)
        {
            SynchronizationContext context = SynchronizationContext.Current;

            IEnumerable<Tuple<Mat, string>> retval = new List<Tuple<Mat, string>>();
            // await UniTask.RunOnThreadPool(() =>
            // {
            // オブジェクト検出
            var list = GetObjects(src, 100, 500);
            // var tmp = list
            retval = list
                .Where(x => x.Item2.X < 170 && (175 < x.Item2.Y && x.Item2.Y < 215)) // 位置で数字を抜き出す
                .OrderBy(x => x.Item2.X)                                            // x座標で並び替え
                .Select(x => new Tuple<Mat, string>(x.Item1, x.Item2.ToString()));  // 座標情報追加(debug用)

            // if (tmp.Count() > 0) Debug.Log(tmp.Select(x => x.Item2.ToString()).Aggregate((x, y) => x + y));

            // retval = tmp
            //     .Where((x, i) =>
            //     {
            //         return i < tmp.Select((y, j) => (y, j))
            //                 .Where(y =>
            //                 {
            //                     // Debug.Log(y.y.Item2.X.ToString() + list.ToList()[y.j - 1].Item2.X.ToString());
            //                     return y.j == 0 ? false : y.y.Item2.X - tmp.ToList()[y.j - 1].Item2.X > 30;
            //                 })
            //                 .Select(y => y.j)
            //                 .DefaultIfEmpty(100)
            //                 .First();

            //     })                                                                  // 空白で分割した左側のみを取得
            //     .Select(x => new Tuple<Mat, string>(x.Item1, x.Item2.ToString()));  // 座標情報追加(debug用)
            // }, cancellationToken: token);
            // if (retval.Count() > 0) Debug.Log(retval.Select(x => x.Item2.ToString()).Aggregate((x, y) => x + y));

            var resized = await ResizeNumbers(retval.Select(x => x.Item1), token);
            retval = resized.Count() == retval.Count() ? retval.Select((x, i) => new Tuple<Mat, string>(resized.ToList()[i], x.Item2)) : new List<Tuple<Mat, string>>();

            // if (tmp.Count() > 0) Debug.Log("t e :" + tmp.Select((y, j) => (y, j))
            //                 .Where(y =>
            //                 {
            //                     // Debug.Log(y.y.Item2.X.ToString() + list.ToList()[y.j - 1].Item2.X.ToString());
            //                     return y.j == 0 ? false : y.y.Item2.X - tmp.ToList()[y.j - 1].Item2.X > 30;
            //                 })
            //                 .Select(y => y.y.Item2)
            //                 .DefaultIfEmpty()
            //                 .First());
            return retval;
        }

        // 数字を正方形にresizeする(埋める)
        async UniTask<IEnumerable<Mat>> ResizeNumbers(IEnumerable<Mat> src, CancellationToken token)
        {
            List<Mat> retval = new List<Mat>();

            await using (UniTask.ReturnToMainThread(token))
            {
                await UniTask.SwitchToThreadPool();
                foreach (var srcMat in src)
                {
                    var adding = (srcMat);
                    var scale = adding.Height > adding.Width ? (double)numberHeight / adding.Height : (double)numberWidth / adding.Width;
                    if (adding.Size().Height * scale <= 1 || adding.Size().Width * scale <= 1)
                    {
                        continue;
                    }
                    // Debug.Log($"{Size.Zero}, {scale}, {new Size(adding.Size().Width * scale, adding.Size().Height * scale)}");
                    Cv2.Resize(adding, adding, Size.Zero, scale, scale);
                    adding = Overlay(adding, new Size(numberWidth, numberHeight));
                    retval.Add(adding);
                }
            };

            return retval;
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

        public IEnumerable<Mat> Threshold2(List<Mat> images)
        {
            return images.Select(x => Threshold2(x));
        }

        // 画像から矩形の部分を検出、トリミング
        private IEnumerable<Mat> GetRectangles(Mat image)
        {
            using (Mat working = Threshold(image.Clone()))
            {
                //test
                working.CopyTo(tempMat);

                Point[][] contourPoints;
                HierarchyIndex[] i;
                Cv2.FindContours(working, out contourPoints, out i, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
                contourPoints = contourPoints.Where(points =>
                {
                    points = Cv2.ApproxPolyDP(points, 30.0, true);
                    return (points.Count() == 4 && Cv2.ContourArea(points) > minRectSize);
                }).ToArray();

                // Debug.Log(contourPoints.Length);

                List<Mat> rects = new List<Mat>();
                foreach (var points in contourPoints)
                {
                    using (Mat adding = new Mat())
                    {
                        using (Mat rotated = new Mat())
                        {
                            // triming
                            var rect = Cv2.MinAreaRect(points);
                            float angle = rect.Angle;
                            Debug.Log("Angle: " + rect.Angle);
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
                            Cv2.Resize(adding, adding, displaySize);
                        }
                        rects.Add(adding.Clone());
                    }
                }
                // Debug.Log(rects.Aggregate(x => x.angle))
                return rects;
            }
        }


        private IEnumerable<Tuple<Mat, Point>> GetObjects(Mat image, double minSize, double maxSize)
        {
            using (Mat working = (image.Clone()))
            {

                Point[][] contourPoints;
                HierarchyIndex[] i;
                Cv2.FindContours(working, out contourPoints, out i, RetrievalModes.Tree, ContourApproximationModes.ApproxNone);
                contourPoints = contourPoints.Where(points =>
                {
                    var size = Cv2.ContourArea(points);
                    return (minSize < size && size < maxSize);
                }).ToArray();

                // Debug.Log(contourPoints.Length);

                List<Tuple<Mat, Point>> rects = new List<Tuple<Mat, Point>>();
                foreach (var points in contourPoints)
                {
                    using (Mat adding = new Mat())
                    {
                        var rect = Cv2.BoundingRect(points);
                        Cv2.GetRectSubPix(image, new Size(rect.Width, rect.Height), rect.Center, adding);
                        rects.Add(new Tuple<Mat, Point>(adding.Clone(), rect.Center));
                    }
                }
                return rects;
            }
        }

        // テンプレートが一致したオブジェクトの中心位置のlistを返す(検出しなかったら(-1, -1))
        private IEnumerable<Point> GetPointByTemplateMatching(Mat src, Mat[] templates)
        {
            int i = 0;
            List<Point> points = new List<Point>();

            using (Mat tmp = new Mat())
            {
                // Cv2.CvtColor(src, tmp, ColorConversionCodes.BGRA2BGR);

                foreach (var template in templates)
                {
                    Mat result = new Mat();
                    // Debug.Log($"tmp={tmp.Size()}, template={templ}")
                    Cv2.MatchTemplate(src, template, result, TemplateMatchModes.CCoeff);
                    double minval, maxVal;
                    Point minloc, maxLoc;

                    Cv2.MinMaxLoc(result, out minval, out maxVal, out minloc, out maxLoc);
                    var topLeft = maxLoc;
                    var bottomRight = new Point(topLeft.X + template.Width, topLeft.Y + template.Height);
                    var center = new Point((topLeft.X + bottomRight.X) / 2, (topLeft.Y + bottomRight.Y) / 2);

                    if (maxVal > 7000000)
                    {
                        points.Add(center);
                        Cv2.Rectangle(src, topLeft, bottomRight, new Scalar(0, 0, 0), 2);
                    }
                    else
                    {
                        points.Add(new Point(-1, -1));
                    }

                    // debug
                    // Cv2.DrawContours(src, result, -1, new Scalar(i == 0 ? 255 : 0, i == 1 ? 255 : 0, i == 2 ? 255 : 0), 2);
                    // Debug.Log($"max value: {maxVal}, position: {maxLoc}");
                    i++;
                }
            }
            return points;
        }


        Mat Overlay(Mat targetImage, Size backSize)
        {
            Mat backgroundImage = new Mat(backSize.Width, backSize.Height, MatType.CV_8UC1, new Scalar(255));

            // 埋め込む画像を背景画像の中央に配置する
            int x = (backgroundImage.Width - targetImage.Width) / 2;
            int y = (backgroundImage.Height - targetImage.Height) / 2;

            // 埋め込む画像を背景画像にコピーする
            targetImage.CopyTo(backgroundImage[new OpenCvSharp.Rect(x, y, targetImage.Width, targetImage.Height)]);

            // 埋め込まれた画像を含む背景画像を保存する
            return backgroundImage;
        }
        Mat Overlay(Mat targetImage, Size backSize, Point leftUpper)
        {
            Mat backgroundImage = new Mat(backSize.Width, backSize.Height, MatType.CV_8UC1, new Scalar(255));

            // 埋め込む画像を背景画像にコピーする
            targetImage.CopyTo(backgroundImage[new OpenCvSharp.Rect(leftUpper.X, leftUpper.Y, targetImage.Width, targetImage.Height)]);

            // 埋め込まれた画像を含む背景画像を保存する
            return backgroundImage;
        }

        Mat Overlay(Mat targetImage, Mat backgroundImage, Point leftUpper)
        {
            // 埋め込む画像を背景画像にコピーする
            targetImage.CopyTo(backgroundImage[new OpenCvSharp.Rect(leftUpper.X, leftUpper.Y, targetImage.Width, targetImage.Height)]);

            return backgroundImage;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        // 保存処理

        // 保存する数字の最大文字数
        private readonly int maxColumn = 10;
        private readonly int maxRaw = 10;

        // 保存する数字画像の大きさ
        private Size numberDataSize => new Size(numberWidth * maxColumn, numberHeight * maxRaw);

        // 保存先
#if UNITY_EDITOR
        readonly private string numberImageDataSavingPath = @"TestImg\NumberData";
#else
        readonly private string numberImageDataSavingPath = "/storage/emulated/0/DCIM/Camera/NumberData";
#endif

        public async UniTask SaveNumber(Texture2D imageTexture, int label, CancellationToken token)
        {
            using (Mat image = OpenCvSharp.Unity.TextureToMat(imageTexture))
            {
                Cv2.CvtColor(image, image, ColorConversionCodes.BGRA2GRAY);

                if (PlayerPrefs.GetInt("ocr_data_count_no" + label, 0) >= maxColumn * maxRaw)
                {
                    Debug.LogError("Too Many Data Error");
                    return;
                }

                if (!Directory.Exists(numberImageDataSavingPath))
                {
                    Directory.CreateDirectory(numberImageDataSavingPath);
                }
#if UNITY_EDITOR
                string path = numberImageDataSavingPath + @"\";
#else
                string path = numberImageDataSavingPath + "/";
#endif

                {
                    Mat saving = new Mat();
                    Debug.Log(path + label + ".png");
                    // データ作成
                    if (!File.Exists(path + label + ".png"))
                    {
                        saving = await CreateSaveData(image, label, 0, token);
                        PlayerPrefs.SetInt("ocr_data_count_no" + label, 1);
                    }
                    else
                    {
                        var formerTex = ReadPng(path + label + ".png");
                        /*using (*/
                        var former = OpenCvSharp.Unity.TextureToMat(formerTex);
                        {
                            Cv2.CvtColor(former, former, ColorConversionCodes.BGRA2GRAY);
                            Debug.Log($"dim: {former.Dims()}, channels: {former.Channels()}, size: {former.Size()}");
                            saving = await CreateSaveData(image, label, PlayerPrefs.GetInt("ocr_data_count_no" + label, 0), token, former);
                            PlayerPrefs.SetInt("ocr_data_count_no" + label, PlayerPrefs.GetInt("ocr_data_count_no" + label, 0) + 1);
                        }
                    }
                    Debug.Log(saving.Size());

                    // save
                    var tex = OpenCvSharp.Unity.MatToTexture(saving);
                    tex.Apply();
                    File.WriteAllBytes(path + label + ".png", tex.EncodeToPNG());

                    saving.Dispose();
                }
            }
        }

        private async UniTask<Mat> CreateSaveData(Mat input, int label, int numbersCnt, CancellationToken token, Mat formerSaveData = null)
        {
            var output = new Mat();
            await using (UniTask.ReturnToMainThread(token))
            {
                await UniTask.SwitchToThreadPool();
                int width = maxColumn * numberWidth, height = maxRaw * numberHeight;
                // Color32[] numImgPixels = tex2d.GetPixels32();
                // input.CopyTo(output);

                if (formerSaveData == null)
                {
                    output = Overlay(input, numberDataSize, new Point(0, 0));
                    // output = Overlay(output, numberDataSize);
                    // output = input;
                }
                else
                {
                    int x = (numbersCnt % maxColumn) * numberWidth;
                    int y = (numbersCnt / maxColumn) * numberHeight;
                    output = Overlay(input, formerSaveData, new Point(x, y));
                }

            };

            return output;
        }

        private Texture2D ReadPng(string path)
        {
            byte[] readBinary;
            try
            {
                readBinary = ReadPngFile(path);
            }
            catch
            {
                Debug.LogError("Couldn't read file!: " + path);
                return null;
            }

            int pos = 16; // 16バイトから開始

            int width = 0;
            for (int i = 0; i < 4; i++)
            {
                width = width * 256 + readBinary[pos++];
            }

            int height = 0;
            for (int i = 0; i < 4; i++)
            {
                height = height * 256 + readBinary[pos++];
            }

            Texture2D texture = new Texture2D(width, height);
            texture.LoadImage(readBinary);

            return texture;
        }

        byte[] ReadPngFile(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader bin = new BinaryReader(fileStream);
            byte[] values = bin.ReadBytes((int)bin.BaseStream.Length);

            bin.Close();

            return values;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        // 数字画像の認識
        public async UniTask<IEnumerable<Tuple<Mat, string>>> GetNumbers(Mat srcImage, CancellationToken token)
        {
            IEnumerable<Tuple<Mat, string>> retval = new List<Tuple<Mat, string>>();
            List<Tuple<Mat, string>> numbers;
            await using (UniTask.ReturnToMainThread(token))
            {
                await UniTask.SwitchToThreadPool();
                // 画像から歩数確認画面を抜き出す
                var mat = await GetWalkCountDisplay(srcImage, token);
                // Debug.Log(matList.Count());

                if (mat == null)
                {
                    return retval;
                }

                // 歩数確認画面から数字部分を抜き出す
                numbers = (await ClipNumber(mat, token)).ToList();
            }

            return numbers;
        }

        public async UniTask<IEnumerable<Tuple<int, float, float>>> RecognizeNumbers(IEnumerable<Texture2D> numberTexs, CancellationToken token)
        {
            // await UniTask.SwitchToMainThread();

            // await using (UniTask.ReturnToMainThread())
            // {
            Debug.Log(numberTexs.Count());
            // await UniTask.SwitchToThreadPool();
            List<Tuple<int, float, float>> result = new List<Tuple<int, float, float>>();
            // numberTexs.ToList().ForEach(async x =>
            // {
            //     var temp = await ;
            // });

            foreach (var tex in numberTexs)
            {
                var temp = await OcrWithKnn(tex, token);
                Debug.Log(temp);
                result.Add(temp);
            }

            Debug.Log(result.Count());
            if (result.Count() <= 0) return null;

            if (result.Min(x => x.Item2 > 0.7f) && result.Min(x => x.Item3 > 0.001))
            {
                return result;
            }
            else
            {
                Debug.Log("Discard Result : " + result.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y));
                return null;
            }
            // }
        }

        private readonly int k = 10;
        public async UniTask<Tuple<int, float, float>> OcrWithKnn(Texture2D tex2d, CancellationToken token)
        {
            if (!Directory.Exists(numberImageDataSavingPath))
            {
                Directory.CreateDirectory(numberImageDataSavingPath);
            }
#if UNITY_EDITOR
            string path = numberImageDataSavingPath + @"\";
#else
            string path = numberImageDataSavingPath + "/";
#endif

            List<Tuple<int, float>> nearerList = new List<Tuple<int, float>>();
            var targetPixels = tex2d.GetPixels();
            for (int number = 0; number < 10; number++)
            {
                int count = PlayerPrefs.GetInt("ocr_data_count_no" + number, 0);
                if (count <= 0)
                {
                    continue;
                }

                // データから画像読み込み
                var datas = ReadPng(path + number + ".png");
                datas.Apply();
                // Debug.Log(path + number + ".png");
                if (datas == null)
                {
                    Debug.LogError($"Cannot open file : {path + number + ".png"}");
                }
                var pixels = datas.GetPixels();

                await using (UniTask.ReturnToMainThread(cancellationToken: token))
                {
                    await UniTask.SwitchToThreadPool();
                    for (int i = 0; i < count; i++)
                    {
                        // 画像切り出し
                        int xBias = maxColumn * numberWidth - ((i % maxColumn) + 1) * numberWidth;
                        int yBias = maxColumn * numberHeight - ((i / maxColumn) + 1) * numberHeight;
                        var data = GetPixels(pixels, xBias, yBias, numberWidth, numberHeight);

                        float similarity = (float)CaluculateCosSimilarity(data.Select(x => x.r).ToArray(), targetPixels.Select(x => x.r).ToArray());

                        if (nearerList.Count >= k)
                        {
                            if (nearerList.Last().Item2 < similarity)
                            {
                                nearerList.RemoveAt(nearerList.Count - 1);
                                nearerList.Add(new Tuple<int, float>(number, similarity));
                                nearerList = nearerList.OrderByDescending(val => val.Item2).ToList();
                            }
                        }
                        else
                        {
                            nearerList.Add(new Tuple<int, float>(number, similarity));
                            nearerList = nearerList.OrderByDescending(val => val.Item2).ToList();
                        }
                        // Debug.Log(number + ":  " + distance + " last: " + nearerList.Last());
                        // yield return null;
                    }
                }
            }

            int maxCount = 0, retNum = 0;
            for (int number = 0; number < 10; number++)
            {
                int numCount = nearerList.Count(list => list.Item1 == number);
                // Debug.Log("num " + number + "  " + numCount);
                if (numCount > maxCount)
                {
                    retNum = number;
                    maxCount = numCount;
                }
            }
            // nearerList.ForEach(x =>
            // {
            //     Debug.Log(x);
            // });
            Debug.Log("result: " + retNum + "  " + maxCount);

            // if(maxCount > k * judgeRate){
            // Debug.Log("result!!: " + retNum);
            // 判定した番号, k中の割合, 最小距離
            return new Tuple<int, float, float>(retNum, maxCount / (float)nearerList.Count, nearerList.First().Item2);
        }

        private Color[] GetPixels(Color[] pixels, int xBias, int yBias, int numberWidth, int numberHeight)
        {
            Color[] retval = new Color[numberHeight * numberWidth];

            for (int y = yBias; y < yBias + numberHeight; y++)
            {
                for (int x = xBias; x < xBias + numberWidth; x++)
                {
                    if (x + y * numberWidth * maxColumn >= pixels.Length) Debug.Log($"Error src : {x}, {y}");
                    if ((x - xBias) + (y - yBias) * numberWidth >= retval.Length) Debug.Log($"Error target : {x - xBias}, {y - yBias}");
                    retval[(x - xBias) + (y - yBias) * numberWidth] = pixels[x + y * numberWidth * maxColumn];
                }
            }
            return retval;
        }

        private float? CaluculateDistance(float[] src1, float[] src2)
        {
            float distance = 0;

            if (src1.Length != src2.Length)
            {
                return null;
            }

            for (int j = 0; j < src1.Length; j++)
            {
                distance += Math.Abs(src1[j] - src2[j]);
            }

            return Mathf.Pow(distance, src1.Length);
        }


        private float? CaluculateCosSimilarity(float[] src1, float[] src2)
        {
            if (src1.Length != src2.Length)
            {
                return null;
            }

            var temp = src1.Zip(src2, (x, y) => new { src1 = x, src2 = y })
                        .Select(x => new { xy = x.src1 * x.src2, x = Mathf.Pow(x.src1 * x.src1, 0.5f), y = Mathf.Pow(x.src2 * x.src2, 0.5f) });

            return temp.Sum(arr => arr.xy) / (temp.Sum(arr => arr.x) * temp.Sum(arr => arr.y));
        }
        // public void OcrWithKnn(Texture2D tex2d)
        // {
        //     // if (!Directory.Exists(numberImageDataSavingPath))
        //     // {
        //     //     Directory.CreateDirectory(numberImageDataSavingPath);
        //     // }
        //     // string path = @"TestImg\";

        //     // List<Tuple<int, float>> nearerList = new List<Tuple<int, float>>();
        //     // var targetPixels = tex2d.GetPixels();

        //     // float distance = 0;
        //     // // データから画像読み込み
        //     // var data = ReadPng(path + "7.png");
        //     // data.Apply();
        //     // var datas = data.GetPixels();

        //     // for (int j = 0; j < datas.Length; j++)
        //     // {
        //     //     distance += Math.Abs(datas[j].r - targetPixels[j].r);
        //     //     Debug.Log(datas[j].r);
        //     // }
        //     // Debug.Log(":  " + distance);
        //     // // yield return null;


        //     if (!Directory.Exists(numberImageDataSavingPath))
        //     {
        //         Directory.CreateDirectory(numberImageDataSavingPath);
        //     }
        //     string path = numberImageDataSavingPath + @"\";

        //     List<Tuple<int, float>> nearerList = new List<Tuple<int, float>>();
        //     var targetPixels = tex2d.GetPixels();
        //     for (int number = 0; number < 10; number++)
        //     {
        //         if (PlayerPrefs.GetInt("ocr_data_count_no" + number, 0) <= 0)
        //         {
        //             continue;
        //         }

        //         // データから画像読み込み
        //         var datas = ReadPng(path + number + ".png");
        //         datas.Apply();
        //         Debug.Log(path + number + ".png");
        //         if (datas == null)
        //         {
        //             Debug.LogError($"Cannot open file : {path + number + ".png"}");
        //         }

        //         for (int i = 0; i < PlayerPrefs.GetInt("ocr_data_count_no" + number, 0); i++)
        //         {
        //             float distance = 0;
        //             // 画像切り出し
        //             int xBias = maxColumn * numberWidth - ((i % maxColumn) + 1) * numberWidth;
        //             int yBias = maxColumn * numberHeight - ((i / maxColumn) + 1) * numberHeight;
        //             Debug.Log($"{xBias}, {yBias}");
        //             var data = datas.GetPixels(xBias, yBias, numberWidth, numberHeight);
        //             Debug.Log($"{xBias}, {yBias}, {data.Select(x => x.r).Average()}");

        //             for (int j = 0; j < data.Length; j++)
        //             {
        //                 distance += Math.Abs(data[j].r - targetPixels[j].r);
        //             }

        //             if (nearerList.Count >= k)
        //             {
        //                 if (nearerList.Last().Item2 > distance)
        //                 {
        //                     nearerList.RemoveAt(nearerList.Count - 1);
        //                     nearerList.Add(new Tuple<int, float>(number, distance));
        //                     nearerList = nearerList.OrderBy(val => val.Item2).ToList();
        //                 }
        //             }
        //             else
        //             {
        //                 nearerList.Add(new Tuple<int, float>(number, distance));
        //                 nearerList = nearerList.OrderBy(val => val.Item2).ToList();
        //             }
        //             Debug.Log(number + ":  " + distance + " last: " + nearerList.Last());
        //             // yield return null;
        //         }
        //     }

        //     int maxCount = 0, retNum = 0;
        //     for (int number = 0; number < 10; number++)
        //     {
        //         int numCount = nearerList.Count(list => list.Item1 == number);
        //         // Debug.Log("num " + number + "  " + numCount);
        //         if (numCount > maxCount)
        //         {
        //             retNum = number;
        //             maxCount = numCount;
        //         }
        //     }
        //     nearerList.ForEach(x =>
        //     {
        //         Debug.Log(x);
        //     });
        //     Debug.Log("result: " + retNum + "  " + maxCount);
        // }
        // public void RunTest()
        // {
        //     float[] trainFeaturesData =
        //     {
        //         2,2,2,2,
        //         3,3,3,3,
        //         4,4,4,4,
        //         5,5,5,5,
        //         6,6,6,6,
        //         7,7,7,7
        //     };
        //     // G
        //     int[] counts = new int[10];
        //     for(int i = 0; i < 10 ; i++)
        //     {
        //         counts[i] = PlayerPrefs.GetInt("ocr_data_count_no" + i, 0);
        //     }

        //     int[] sizes = new int[] {10, counts.Min(), numberWidth, numberHeight};
        //     var trainFeatures = new Mat(sizes, MatType.CV_8SC1);
        //     for(int i = 0; i < 10; i++)
        //     {
        //         for(int j = 0; j < counts.Min(); j++)
        //         {
        //             trainFeatures.Set<Mat>(i, j, ) 
        //         }
        //     }

        //     int[] trainLabelsData = { 2, 3, 4, 5, 6, 7 };
        //     var trainLabels = new Mat(1, 6, MatType.CV_32S, trainLabelsData);

        //     using var kNearest = KNearest.Create();
        //     kNearest.Train(trainFeatures, SampleTypes.RowSample, trainLabels);

        //     float[] testFeatureData = { 3, 3, 3, 3 };
        //     var testFeature = new Mat(1, 4, MatType.CV_32F, testFeatureData);

        //     const int k = 1;
        //     var results = new Mat();
        //     var neighborResponses = new Mat();
        //     var dists = new Mat();
        //     var detectedClass = (int)kNearest.FindNearest(testFeature, k, results, neighborResponses, dists);

        //     Assert.Equal(3, detectedClass);
        // }

        // test
        public void CallAtMainThred()
        {
            DisplayMat(tempMat);
        }

        private void DisplayMat(Mat src)
        {
            Texture2D tex = OpenCvSharp.Unity.MatToTexture(src);

            var target = imageObjList.First();
            target.sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            target.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 240 * tex.height / tex.width);
            target.gameObject.SetActive(true);
        }

    }

}

namespace System
{
    public interface IAsyncDisposable
    {
    }
}