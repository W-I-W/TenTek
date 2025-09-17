using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UAnimationWindowUtility
    {
        protected MethodInfo mi_CreateDefaultCurves;
        protected MethodInfo mi_IsNodeLeftOverCurve;
        protected MethodInfo mi_CreateNewClipAtPath;
        protected MethodInfo mi_GetNextKeyframeTime;
        protected MethodInfo mi_GetPreviousKeyframeTime;

        public UAnimationWindowUtility()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var animationWindowUtilityType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowUtility");
            var animationWindowStateType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowState");
            Assert.IsNotNull(mi_CreateDefaultCurves = animationWindowUtilityType.GetMethod("CreateDefaultCurves", new Type[] { animationWindowStateType, typeof(AnimationClip), typeof(EditorCurveBinding[]) }));
            Assert.IsNotNull(mi_IsNodeLeftOverCurve = animationWindowUtilityType.GetMethod("IsNodeLeftOverCurve", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(mi_CreateNewClipAtPath = animationWindowUtilityType.GetMethod("CreateNewClipAtPath", BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(mi_GetNextKeyframeTime = animationWindowUtilityType.GetMethod("GetNextKeyframeTime", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(mi_GetPreviousKeyframeTime = animationWindowUtilityType.GetMethod("GetPreviousKeyframeTime", BindingFlags.Public | BindingFlags.Static));
        }

        public void CreateDefaultCurves(object state, AnimationClip animationClip, EditorCurveBinding[] properties)
        {
            mi_CreateDefaultCurves.Invoke(null, new object[] { state, animationClip, properties });
        }

        public virtual bool IsNodeLeftOverCurve(object state, object node)
        {
            return (bool)mi_IsNodeLeftOverCurve.Invoke(null, new object[] { node });
        }

        public AnimationClip CreateNewClipAtPath(string clipPath)
        {
            return mi_CreateNewClipAtPath.Invoke(null, new object[] { clipPath }) as AnimationClip;
        }

        public float GetNextKeyframeTime(Array curves, float currentTime, float frameRate)
        {
            return (float)mi_GetNextKeyframeTime.Invoke(null, new object[] { curves, currentTime, frameRate });
        }
        public float GetPreviousKeyframeTime(Array curves, float currentTime, float frameRate)
        {
            return (float)mi_GetPreviousKeyframeTime.Invoke(null, new object[] { curves, currentTime, frameRate });
        }
    }
}
