using System;
using System.Diagnostics;

namespace ValorChronicle.Core.Logging
{
    public static class GameLogger
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.Log(message, context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(object message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        public static void Error(object message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        public static void Exception(Exception exception, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogException(exception, context);
        }
    }
}
