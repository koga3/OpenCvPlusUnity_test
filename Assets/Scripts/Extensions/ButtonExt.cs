using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.Events;

namespace Kew
{
    public static class ButtonExt
    {

        public static IDisposable AddCallbackWithTarget<T>(this Button button, UnityAction<T> action, T value, Component target, double interval = 1)
        {
            return button.OnClickAsObservable().TakeUntilDestroy(target).ThrottleFirst(TimeSpan.FromSeconds(interval)).Subscribe(_ => action?.Invoke(value));
        }

        public static IDisposable AddCallbackWithTarget(this Button button, Action action, Component target, double interval = 1)
        {
            return button.OnClickAsObservable().TakeUntilDestroy(target).ThrottleFirst(TimeSpan.FromSeconds(interval)).Subscribe(_ => action?.Invoke());
        }

        [Obsolete("代わりにAddCallbackWithTargetを使用してください")]
        public static IDisposable AddCallback(this Button button, Action<Unit> action, double interval = 1)
        {
            return button.OnClickAsObservable().ThrottleFirst(TimeSpan.FromSeconds(interval)).Subscribe(action);
        }

        public static void SetEnable(this Button button, bool isEnable)
        {
            button.interactable = isEnable;
        }
    }
}
