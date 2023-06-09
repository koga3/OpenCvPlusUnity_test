﻿using UnityEngine;
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


namespace Kew
{
    public class OCRSceneController : MonoBehaviour
    {
        //test
        [SerializeField]
        private List<Image> testImageObjList;

        [SerializeField]
        private Button shutterBtn;
        [SerializeField]
        private GameObject saveObjectPopup;
        private Button saveBtn => saveObjectPopup.transform.Find("ButtonSave").GetComponent<Button>();
        private Button cancelBtn => saveObjectPopup.transform.Find("ButtonReTry").GetComponent<Button>();

        [SerializeField]
        private GameObject objRawImage;
        [SerializeField]
        private GameObject testImagesRootObj;
        [SerializeField]
        private List<Image> imageObjList;
        [SerializeField]
        private List<GameObject> numberObjList;
        [SerializeField]
        private List<Texture2D> objectTextures;

        [SerializeField]
        private GameObject walkCntPopup;
        [SerializeField]
        private TMP_Text walkCnt;
        [SerializeField]
        private Button walkCntCloseBtn;

        [SerializeField]
        private Toggle isShowRecognizedNumber;

        [SerializeField]
        private GameObject recognizing;

        private OCROpenCVUtil util;


        // 出力する数字画像の大きさ
        private readonly int numberWidth = 32;
        private readonly int numberHeight = 32;


        WebCamTexture webCamTex;

        private ReactiveProperty<bool> isShuttered = new ReactiveProperty<bool>(false);

        private ReactiveProperty<Mat> walkCountDisplay = new ReactiveProperty<Mat>(new Mat());
        private ReactiveProperty<List<Tuple<Mat, string>>> numbers = new ReactiveProperty<List<Tuple<Mat, string>>>();
        private ReactiveProperty<List<Tuple<Texture2D, string>>> numbersTexture = new ReactiveProperty<List<Tuple<Texture2D, string>>>();

        // private ReactiveProperty<List<Tuple<int, float, float>>> recognized = new ReactiveProperty<List<Tuple<int, float, float>>>();
        private ReactiveProperty<List<int>> recognized = new ReactiveProperty<List<int>>();

        [SerializeField]
        private Image testImg;

        private bool isInProgress = false;
        private void Start()
        {
            var token = this.GetCancellationTokenOnDestroy();

            if (!UniAndroidPermission.IsPermitted(AndroidPermission.WRITE_EXTERNAL_STORAGE))
            {
                UniAndroidPermission.RequestPermission(AndroidPermission.WRITE_EXTERNAL_STORAGE);
            }

#if UNITY_EDITOR
            objRawImage.transform.Rotate(new Vector3(0, 0, 90));
#elif !DEBUG
            objRawImage.transform.Rotate(new Vector3(0, 0, 90));
#endif
            try
            {
#if UNITY_EDITOR
                this.webCamTex = new WebCamTexture(WebCamTexture.devices[0].name);
#else
                this.webCamTex = new WebCamTexture(WebCamTexture.devices[0].name);
#endif
                this.webCamTex.Play();
                this.objRawImage.GetComponent<RawImage>().texture = this.webCamTex;
            }
            catch (Exception e)
            {
                Debug.LogError("Camera didn't initialize. : " + e.Message);
            }

            isShuttered.TakeUntilDestroy(this).Skip(1).Subscribe(isShuttered =>
            {
                if (isShuttered)
                {
                    webCamTex.Stop();
                }
                else
                {
                    webCamTex.Play();
                }
            });

            // util = new OCROpenCVUtil(objectTextures.Select(tex => OpenCvSharp.Unity.TextureToMat(tex)).ToList());
            // test
            util = new OCROpenCVUtil(objectTextures.Select(tex => OpenCvSharp.Unity.TextureToMat(tex)).ToList(), testImageObjList, numberObjList);

            shutterBtn.AddCallbackWithTarget(() => Shatter(), this);
            saveBtn.AddCallbackWithTarget(() => SaveNumbers(this.GetCancellationTokenOnDestroy()).Forget(), this);
            cancelBtn.AddCallbackWithTarget(() =>
            {
                isShuttered.Value = false;
                shutterBtn.gameObject.SetActive(true);
                saveObjectPopup.SetActive(false);
            }, this);

            var lockObject = new object();
            walkCountDisplay.TakeUntilDestroy(this).Skip(1).Subscribe(async display =>
            {
                // if (display != null) util.DisplayMat(display, 0);
                recognizing.SetActive(display != null);
                // matList.ForEach(x => x.Dispose());

                lock (lockObject)
                {
                    if (isInProgress)
                    {
                        Debug.Log("this is in progress");
                        return;
                    }

                    // 歩数確認画面から数字部分を抜き出す
                    isInProgress = true;
                }
                if (display != null)
                {
                    ditectedCnt++;
                    if (ditectedCnt > 2)
                    {
                        recognized.Value = await util.RecognizeNumbers(display, isShowRecognizedNumber.isOn, token);
                    }
                }
                else
                {
                    ditectedCnt = 0;
                }
                isInProgress = false;
            });

            // numbers.TakeUntilDestroy(this).ObserveOnMainThread().Skip(1).Subscribe(numbers =>
            // {
            //     ShowMats(numbers);
            //     // numbers.ToList().ForEach(x => x.Item1.Dispose());

            //     if (isShowRecognizedNumber.isOn)
            //     {
            //         numbersTexture.Value = numbers.Select(x => new Tuple<Texture2D, string>(OpenCvSharp.Unity.MatToTexture(x.Item1), x.Item2)).ToList();
            //     }
            // });

            // numbersTexture.TakeUntilDestroy(this).Skip(1).Subscribe(async numbers =>
            // {
            //     if (isInProgress) return;
            //     isInProgress = true;
            //     var token = this.GetCancellationTokenOnDestroy();
            //     var val = await util.RecognizeNumbers(numbers.Select(x => x.Item1), token);
            //     if (val != null)
            //     {
            //         recognized.Value = val.ToList();
            //     }
            //     isInProgress = false;
            // });

            // recognized.TakeUntilDestroy(this).Skip(1).Subscribe(recognized =>
            // {
            //     if (recognized.Count() > 0)
            //     {
            //         Debug.Log("Result : " + recognized.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y));
            //         isShuttered.Value = true;
            //         walkCntPopup.SetActive(true);
            //         walkCnt.text = recognized.Select(x => x.Item1.ToString()).Aggregate((x, y) => x + y);
            //     }
            // });

            recognized.TakeUntilDestroy(this).Skip(1).Subscribe(recognized =>
            {
                if (recognized == null) return;
                if (recognized.Count() > 0)
                {
                    isShuttered.Value = true;
                    walkCntPopup.SetActive(true);
                    walkCnt.text = recognized.Select(x => x.ToString()).Aggregate((x, y) => x + y);
                }
            });
            walkCntCloseBtn.AddCallbackWithTarget(() =>
            {
                isShuttered.Value = false;
                walkCntPopup.SetActive(false);
            }, this);


            // await WaitCameraInitializedCoroutine(this.GetCancellationTokenOnDestroy());


            SetMatsRutine(token).Forget();

            Debug.Log("test");
        }

        private void Update()
        {
            // if (Input.GetMouseButtonDown(1))
            // {
            //     int i = 0;
            //     foreach (var mat in binarizeList)
            //     {
            //         var tex = OpenCvSharp.Unity.MatToTexture(mat);
            //         File.WriteAllBytes($"TestImg/{i}.png", tex.EncodeToPNG());
            //         i++;
            //     }
            // }
        }

        private int ditectedCnt = 0;
        private async UniTaskVoid SetMatsRutine(CancellationToken token)
        {
            while (true)
            {
                // recognizing.SetActive(matList.Value.Count() > 0);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                while (isShuttered.Value)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                // 判定動作を3フレームに1回にする
                int i = 0;
                while (i < 3)
                {
                    i++;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                using (var webCamMat = OpenCvSharp.Unity.TextureToMat(this.webCamTex))
                {
                    using (Mat copy = new Mat())
                    {
                        webCamMat.CopyTo(copy);
                        await using (UniTask.ReturnToMainThread(token))
                        {
                            await UniTask.SwitchToThreadPool();
                            // 画像から歩数確認画面を抜き出す
                            List<Mat> list = new List<Mat>();
                            walkCountDisplay.Value = await util.GetWalkCountDisplay(webCamMat, token);
                        }
                    }
                }
            }
        }


        private void ShowMats(IEnumerable<Mat> matList)
        {
            if (matList.Count() > imageObjList.Count())
            {
                Debug.LogError($"Objects Is Too Many! : count = {matList.Count()}");
                return;
            }
            foreach (var (imageObj, i) in imageObjList.Select((x, i) => (x, i)))
            {
                imageObj.gameObject.SetActive(i < matList.Count());
            }
            foreach (var (mat, i) in matList.Select((x, i) => (x, i)))
            {
                // var image = Instantiate(imagePrefab, testImagesRootObj.transform);
                var image = imageObjList[i];

                Texture2D texture2D = OpenCvSharp.Unity.MatToTexture(mat);

                image.sprite = Sprite.Create(texture2D, new UnityEngine.Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
                image.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(texture2D.width, texture2D.height);
            }
        }

        private void ShowMats(IEnumerable<Tuple<Mat, string>> matList)
        {
            if (matList.Count() > numberObjList.Count())
            {
                Debug.LogError($"Numbers Is Too Many! : count = {matList.Count()}");
                return;
            }
            foreach (var (numObj, i) in numberObjList.Select((x, i) => (x, i)))
            {
                numObj.SetActive(i < matList.Count());
            }

            matList.Select((x, i) => new { tuple = x, i }).ToList().ForEach(x =>
            {
                ShowMatAndText(x.tuple.Item1, x.tuple.Item2, x.i);
            });
        }

        private void ShowMatAndText(Mat mat, string text, int i)
        {
            // var number = Instantiate(numberPrefab, testImagesRootObj.transform);
            if (i >= numberObjList.Count())
            {
                Debug.LogError($"Numbers Is Too Many! : count = {i + 1}");
                return;
            }
            var number = numberObjList[i];

            Texture2D texture2D = OpenCvSharp.Unity.MatToTexture(mat);

            number.GetComponentInChildren<Image>().sprite = Sprite.Create(texture2D, new UnityEngine.Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
            // number.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(texture2D.width, texture2D.height);
            number.transform.Find("Bg/Accuracy").GetComponent<TMP_Text>().text = text;
        }

        private void Shatter()
        {
            shutterBtn.gameObject.SetActive(false);
            Debug.Log("shutter!!");
            isShuttered.Value = true;
            saveObjectPopup.SetActive(true);
        }

        private async UniTaskVoid SaveNumbers(CancellationToken token)
        {
            foreach (var (numberObj, i) in numberObjList.Select((x, i) => (x, i)))
            {
                if (numberObj.activeSelf == false
                     || string.IsNullOrEmpty(numberObj.transform.GetComponentInChildren<TMP_InputField>().text))
                {
                    continue;
                }

                Debug.Log("saving");
                var tex2d = numberObj.GetComponentInChildren<Image>().sprite.texture;
                await util.SaveNumber(tex2d, int.Parse(numberObj.transform.GetComponentInChildren<TMP_InputField>().text), token);
            }

            shutterBtn.gameObject.SetActive(true);
            saveObjectPopup.SetActive(false);
            isShuttered.Value = false;
        }
    }
}