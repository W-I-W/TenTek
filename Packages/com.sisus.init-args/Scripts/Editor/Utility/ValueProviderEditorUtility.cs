using System;
using System.Collections.Generic;
using System.Linq;
using Sisus.Init.ValueProviders;
using UnityEditor;
using UnityEngine;

namespace Sisus.Init.EditorOnly
{
	internal static class ValueProviderEditorUtility
	{
		/// <summary>
		/// NOTE: Slow method; should not be called during every OnGUI.
		/// </summary>
		public static bool IsSingleSharedInstanceSlow(ScriptableObject valueProvider)
			=> !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(valueProvider))
			&& ValueProviderUtility.TryGetSingleSharedInstanceSlow(valueProvider.GetType(), out var singleSharedInstance)
			&& ReferenceEquals(singleSharedInstance, valueProvider);

		public static IEnumerable<Type> GetAllValueProviderMenuItemTargetTypes()
			=> TypeCache.GetTypesWithAttribute<ValueProviderMenuAttribute>()
			.Where(t => !t.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(t) && ValueProviderUtility.IsValueProvider(t));
	}
}
