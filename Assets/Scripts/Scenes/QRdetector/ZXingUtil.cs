using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
/*
* Copyright 2012 ZXing.Net authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Threading;
using UnityEngine.UI;

using TMPro;
using UnityEngine;

using ZXing;
using ZXing.QrCode;

using UniRx;
using Cysharp.Threading.Tasks;


namespace Kew
{
    public class ZXingUtil : MonoBehaviour
    {
        [SerializeField]
        private GameObject objRawImage;
        [SerializeField]
        private TMP_Text result;
        [SerializeField]
        private Button clearButton;
        // Texture for encoding test
        public Texture2D encoded;

        private WebCamTexture camTexture;
        private Thread qrThread;

        private Color32[] c;
        private int W, H;

        private Rect screenRect;

        private bool isQuit;

        private StringReactiveProperty LastResult = new StringReactiveProperty();
        private bool shouldEncodeNow;

        void OnGUI()
        {
            // GUI.DrawTexture(screenRect, camTexture, ScaleMode.ScaleToFit);
        }

        void OnEnable()
        {
            if (camTexture != null)
            {
                camTexture.Play();
                W = camTexture.width;
                H = camTexture.height;
            }
        }

        void OnDisable()
        {
            // if (camTexture != null)
            // {
            //     camTexture.Pause();
            // }
        }

        void OnDestroy()
        {
            qrThread.Abort();
            camTexture.Stop();
        }

        // It's better to stop the thread by itself rather than abort it.
        void OnApplicationQuit()
        {
            isQuit = true;
        }

        void Start()
        {
#if UNITY_EDITOR
            objRawImage.transform.Rotate(new Vector3(0, 0, 90));
#elif !DEBUG
            objRawImage.transform.Rotate(new Vector3(0, 0, 90));
#endif            
            try
            {
#if UNITY_EDITOR
                this.camTexture = new WebCamTexture(WebCamTexture.devices[0].name);
#else
                this.camTexture = new WebCamTexture(WebCamTexture.devices[0].name);
#endif
                this.camTexture.Play();
                this.objRawImage.GetComponent<RawImage>().texture = this.camTexture;
            }
            catch (Exception e)
            {
                Debug.LogError("Camera didn't initialize. : " + e.Message);
            }

            encoded = new Texture2D(256, 256);
            // LastResult = "http://www.google.com";
            shouldEncodeNow = true;

            screenRect = new Rect(0, 0, Screen.width, Screen.height);

            OnEnable();

            var token = this.GetCancellationTokenOnDestroy();
            DecodeQR(token).Forget();

            clearButton.AddCallbackWithTarget(() => { result.text = ""; Debug.Log("click"); }, this);
            LastResult.TakeUntilDestroy(this).Subscribe(async text =>
            {
                await UniTask.SwitchToMainThread();
                Debug.Log(text);
                result.text = text;
            });
        }

        void Update()
        {
            if (c == null)
            {
                c = camTexture.GetPixels32();
            }

            // encode the last found
            // var textForEncoding = LastResult;
            // if (shouldEncodeNow &&
            //     textForEncoding != null)
            // {
            //     var color32 = Encode(textForEncoding, encoded.width, encoded.height);
            //     encoded.SetPixels32(color32);
            //     encoded.Apply();
            //     shouldEncodeNow = false;
            // }
        }

        async UniTaskVoid DecodeQR(CancellationToken token)
        {
            // create a reader with a custom luminance source
            var barcodeReader = new BarcodeReader { AutoRotate = false, Options = new ZXing.Common.DecodingOptions { TryHarder = false } };

            while (true)
            {
                await using (UniTask.ReturnToMainThread(token))
                {
                    await UniTask.SwitchToThreadPool();
                    if (isQuit)
                        break;

                    try
                    {
                        // decode the current frame
                        var result = barcodeReader.Decode(c, W, H);
                        if (result != null)
                        {
                            LastResult.SetValueAndForceNotify(result.Text);
                            shouldEncodeNow = true;
                            Debug.Log("ZXing" + result.Text);
                        }

                        // Sleep a little bit and set the signal to get the next frame
                        Thread.Sleep(200);
                        c = null;
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static Color32[] Encode(string textForEncoding, int width, int height)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width
                }
            };
            return writer.Write(textForEncoding);
        }
    }
}