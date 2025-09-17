using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class URotationCurveInterpolation
    {
        private readonly MethodInfo mi_GetModeFromCurveData;
        private readonly MethodInfo mi_SetInterpolation;

        public enum Mode
        {
            Baked,
            NonBaked,
            RawQuaternions,
            RawEuler,
            Undefined,
            Total,
        }
        public static readonly string[] PrefixForInterpolation =
        {
            "localEulerAnglesBaked.",
            "localEulerAngles.",
            "m_LocalRotation.",
            "localEulerAnglesRaw.",
            null,
        };

        public URotationCurveInterpolation()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var rotationCurveInterpolationType = asmUnityEditor.GetType("UnityEditor.RotationCurveInterpolation");
            Assert.IsNotNull(mi_GetModeFromCurveData = rotationCurveInterpolationType.GetMethod("GetModeFromCurveData", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(mi_SetInterpolation = rotationCurveInterpolationType.GetMethod("SetInterpolation", BindingFlags.NonPublic | BindingFlags.Static));
        }

        public Mode GetModeFromCurveData(EditorCurveBinding data)
        {
            return (Mode)mi_GetModeFromCurveData.Invoke(null, new object[] { data });
        }

        public void SetInterpolation(AnimationClip clip, EditorCurveBinding[] curveBindings, Mode newInterpolationMode)
        {
            mi_SetInterpolation.Invoke(null, new object[] { clip, curveBindings, (int)newInterpolationMode });
        }
    }
}
