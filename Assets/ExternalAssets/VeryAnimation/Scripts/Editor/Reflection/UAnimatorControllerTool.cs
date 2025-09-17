using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UAnimatorControllerTool
    {
        private readonly Func<object, object> dg_get_tool;
        private Action<UnityEditor.Animations.AnimatorController> dg_set_animatorController;
        private Func<bool> dg_get_isLocked;
        private Action dg_OnInvalidateAnimatorController;

        public Type animtorControllerToolLayerSettingsWindowType;

        public UAnimatorControllerTool()
        {
            var path = InternalEditorUtility.GetEditorAssemblyPath().Replace("UnityEditor.dll", "UnityEditor.Graphs.dll");
            var asmUnityEditor = Assembly.LoadFrom(path);
            var animatorControllerToolType = asmUnityEditor.GetType("UnityEditor.Graphs.AnimatorControllerTool");
            {
                var fi_tool = animatorControllerToolType.GetField("tool", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(dg_get_tool = EditorCommon.CreateGetFieldDelegate<object>(fi_tool));
            }
            animtorControllerToolLayerSettingsWindowType = asmUnityEditor.GetType("UnityEditor.Graphs.LayerSettingsWindow");
        }

        public void SetAnimatorController(UnityEditor.Animations.AnimatorController ac)
        {
            var w = dg_get_tool(null);
            if (w == null) return;

            if (dg_get_isLocked == null || dg_get_isLocked.Target != w)
                dg_get_isLocked = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), w, w.GetType().GetProperty("isLocked").GetGetMethod());
            if (dg_get_isLocked()) return;

            if (dg_OnInvalidateAnimatorController == null || dg_OnInvalidateAnimatorController.Target != w)
                dg_OnInvalidateAnimatorController = (Action)Delegate.CreateDelegate(typeof(Action), w, w.GetType().GetMethod("OnInvalidateAnimatorController"));
            dg_OnInvalidateAnimatorController();

            if (dg_set_animatorController == null || dg_set_animatorController.Target != w)
                dg_set_animatorController = (Action<UnityEditor.Animations.AnimatorController>)Delegate.CreateDelegate(typeof(Action<UnityEditor.Animations.AnimatorController>), w, w.GetType().GetProperty("animatorController").GetSetMethod());
            dg_set_animatorController(ac);
        }

        public EditorWindow Instance => (EditorWindow)dg_get_tool(null);
    }
}
