//#define DEBUG_CLEAR_CACHE

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Sisus.Init.Internal;
using Sisus.Init.ValueProviders;
using Sisus.Shared.EditorOnly;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static Sisus.Init.Internal.ServiceTagUtility;

#if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
using UnityEngine.AddressableAssets;
#endif

#if DEV_MODE && DEBUG && !INIT_ARGS_DISABLE_PROFILING
using Unity.Profiling;
#endif

namespace Sisus.Init.EditorOnly.Internal
{
	[InitializeOnLoad]
	internal static class EditorServiceTagUtility
	{
		internal static Component openSelectTagsMenuFor;
		private static readonly GUIContent serviceLabel = new("Service", "A service of this type is available.\n\nIt can be acquired automatically during initialization.");
		private static readonly GUIContent blankLabel = new(" ");
		private static readonly HashSet<Type> definingTypesBuilder = new();
		private static Dictionary<object, Type[]> objectDefiningTypesCache = new();
		private static Dictionary<object, Type[]> objectDefiningTypesCache2 = new();

		static EditorServiceTagUtility()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;
			ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
			ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
			Service.AnyChangedEditorOnly -= OnAnyServiceChanged;
			Service.AnyChangedEditorOnly += OnAnyServiceChanged;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;
			EditorSceneManager.sceneClosed -= OnSceneClosed;
			EditorSceneManager.sceneClosed += OnSceneClosed;

			static void OnObjectChangesPublished(ref ObjectChangeEventStream stream) => RebuildDefiningTypesCache();
			static void OnUndoRedoPerformed() => RebuildDefiningTypesCache();

			static void OnSelectionChanged()
			{
				// Need to repaint editor header to update service tag position.
				RepaintAllServiceEditors();
			}

			static void OnAnyServiceChanged() => RebuildDefiningTypesCache();
			static void OnPlayModeStateChanged(PlayModeStateChange mode) => RebuildDefiningTypesCache();
			static void OnSceneUnloaded(Scene scene) => RebuildDefiningTypesCache();
			static void OnSceneClosed(Scene scene) => RebuildDefiningTypesCache();

			static void RebuildDefiningTypesCache()
			{
				#if DEV_MODE && DEBUG_CLEAR_CACHE
				Debug.Log(nameof(RebuildDefiningTypesCache));
				#endif
				EditorApplication.delayCall -= RebuildDefiningTypesCacheImmediate;
				EditorApplication.delayCall += RebuildDefiningTypesCacheImmediate;
			}

			#if DEV_MODE
			[MenuItem("DevMode/Rebuild Service Defining Types Cache")]
			#endif
			static void RebuildDefiningTypesCacheImmediate()
			{
				int count = objectDefiningTypesCache.Count;
				if(count is 0)
				{
					return;
				}

				var oldCache = objectDefiningTypesCache;
				(objectDefiningTypesCache, objectDefiningTypesCache2) = (objectDefiningTypesCache2, objectDefiningTypesCache);

				var definingTypesChanged = false;
				foreach(var oldEntry in oldCache)
				{
					var target = oldEntry.Key;
					if(target is Object unityObject && !unityObject)
					{
						definingTypesChanged = true;
						continue;
					}

					var oldDefiningTypes = oldEntry.Value;
					var newDefiningTypes = GetServiceDefiningTypes(target);
					if(!AreEqual(oldDefiningTypes, newDefiningTypes))
					{
						definingTypesChanged = true;
					}
				}

				foreach(var oldDefiningTypes in oldCache.Values)
				{
					if(oldDefiningTypes.Length > 0)
					{
						ArrayPool<Type>.Shared.Return(oldDefiningTypes, true);
					}
				}

				oldCache.Clear();

				if(!definingTypesChanged)
				{
					return;
				}

				InspectorContents.Repaint();
			}

			static void RepaintAllServiceEditors()
			{
				var gameObject = Selection.activeGameObject;
				if(!gameObject)
				{
					return;
				}

				foreach(var component in Selection.activeGameObject.GetComponentsNonAlloc<Component>())
				{
					// Skip missing components
					if(!component)
					{
						continue;
					}
					
					if(GetServiceDefiningTypes(component).Length > 0)
					{
						// Need to repaint editor header to update service tag position.
						InspectorContents.RepaintEditorsWithTarget(component);
					}
				}
			}
		}

		internal static Span<Type> GetServiceDefiningTypes([DisallowNull] object serviceOrServiceProvider)
		{
			#if DEV_MODE && DEBUG && !INIT_ARGS_DISABLE_PROFILING
			using var x = getServiceDefiningTypesMarker.Auto();
			#endif

			if(objectDefiningTypesCache.TryGetValue(serviceOrServiceProvider, out var cachedResults))
			{
				if(cachedResults.Length <= 1)
				{
					return cachedResults;
				}

				int nullIndex = Array.IndexOf(cachedResults, null, 1);
				return nullIndex is -1 ? cachedResults.AsSpan() : cachedResults.AsSpan(0, nullIndex);
			}

			AddServiceDefiningTypes(serviceOrServiceProvider, definingTypesBuilder);

			int count = definingTypesBuilder.Count;
			if(count == 0)
			{
				objectDefiningTypesCache.Add(serviceOrServiceProvider, Array.Empty<Type>());
				return Span<Type>.Empty;
			}

			var definingTypes = ArrayPool<Type>.Shared.Rent(definingTypesBuilder.Count);
			definingTypesBuilder.CopyTo(definingTypes);
			definingTypesBuilder.Clear();
			objectDefiningTypesCache.Add(serviceOrServiceProvider, definingTypes);
			return definingTypes.AsSpan(0, count);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void AddServiceDefiningTypes([DisallowNull] object serviceOrServiceProvider, [DisallowNull] HashSet<Type> definingTypes)
			{
				var type = serviceOrServiceProvider.GetType();
				foreach(var definingType in ServiceAttributeUtility.definingTypes)
				{
					var serviceInfo = definingType.Value;
					if(serviceInfo.classWithAttribute != type && serviceInfo.concreteType != type)
					{
						continue;
					}

					if(serviceInfo.loadMethod is LoadMethod.FindFromScene)
					{
						if(Find.GameObjectOf(serviceOrServiceProvider, out var gameObject) && IsSceneObjectOrPrefabEditedInSceneContext(gameObject))
						{
							definingTypes.Add(definingType.Key);
						}
						
						static bool IsSceneObjectOrPrefabEditedInSceneContext(GameObject gameObject)
						{
							if(!gameObject.scene.IsValid())
							{
								return false;
							}

							var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
							if(!prefabStage)
							{
								return true;
							}

							if(prefabStage.mode is PrefabStage.Mode.InIsolation)
							{
								return false;
							}
							
							var root = prefabStage.openedFromInstanceRoot;
							return root && root.scene.IsValid();
						}
					}
					else if(serviceInfo.ResourcePath is { Length: > 0 } resourcePath)
					{
						if(Find.In(serviceOrServiceProvider, out Object unityObject) && string.Equals(unityObject.name, Path.GetFileNameWithoutExtension(resourcePath)) && AssetDatabase.Contains(unityObject))
						{
							definingTypes.Add(definingType.Key);
						}
					}
					#if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
					else if(serviceInfo.referenceType is ReferenceType.AddressableKey)
					{
						if(Find.In(serviceOrServiceProvider, out Object unityObject) && AssetDatabase.Contains(unityObject))
						{
							definingTypes.Add(definingType.Key);
						}
					}
					#endif
					else
					{
						definingTypes.Add(definingType.Key);
					}
				}

				foreach(var activeInstance in Service.ActiveInstancesEditorOnly)
				{
					if(ReferenceEquals(activeInstance.ServiceOrProvider, serviceOrServiceProvider))
					{
						definingTypes.Add(activeInstance.definingType);
					}
				}

				foreach(var service in ServiceInjector.services)
				{
					if(ReferenceEquals(service.Value, serviceOrServiceProvider))
					{
						definingTypes.Add(service.Key);
					}
				}

				// In Edit Mode it's possible that the object is registered as a value provider in a Service Tag
				// or Services component, but the provided service will only become available at runtime.
				if(!Application.isPlaying && serviceOrServiceProvider is Component serviceProviderComponent && serviceProviderComponent && ValueProviderUtility.IsValueProvider(serviceOrServiceProvider))
				{
					foreach(var serviceTag in GetServiceTagsTargeting(serviceProviderComponent))
					{
						if(serviceTag?.DefiningType is { } serviceTagDefiningType)
						{
							definingTypes.Add(serviceTagDefiningType);
						}
					}

					foreach(var servicesComponent in Services.AllEditorOnly)
					{
						foreach(var definition in servicesComponent.providesServices)
						{
							if(ReferenceEquals(definition.service, serviceProviderComponent)
							   && definition.definingType.Value is { } definingType)
							{
								definingTypes.Add(definingType);
							}
						}
					}
				}
			}
		}

		public static bool IsService([MaybeNull] Object client, Type dependencyType)
		{
			if(ServiceAttributeUtility.ContainsDefiningType(dependencyType)
				|| ServiceInjector.services.ContainsKey(dependencyType)
				|| ServiceInjector.TryGetUninitializedServiceInfo(dependencyType, out _))
			{
				return true;
			}

			var clientComponent = Find.In<Transform>(client);
			var clientIsComponent = clientComponent is not null;

			foreach(var activeInstance in Service.ActiveInstancesEditorOnly)
			{
				if(activeInstance.definingType != dependencyType)
				{
					continue;
				}

				var clients = activeInstance.ToClients;
				if(clients is Clients.Everywhere || (clientIsComponent && activeInstance.ServiceOrProvider is Component serviceOrProvider && Service.IsAccessibleTo(clientComponent, serviceOrProvider, clients)))
				{
					return true;
				}
			}

			// In Edit Mode it's possible that the dependency is registered via a value provider in a Service Tag
			// or Services component, but it will only become available at runtime.
			if(!Application.isPlaying)
			{
				foreach(var serviceTag in ServiceTag.AllEditorOnly)
				{
					if(serviceTag.DefiningType != dependencyType)
					{
						continue;
					}

					var clients = serviceTag.ToClients;
					if(clients is Clients.Everywhere || (clientIsComponent && Service.IsAccessibleTo(clientComponent, serviceTag, clients)))
					{
						return true;
					}
				}

				foreach(var servicesComponent in Services.AllEditorOnly)
				{
					foreach(var definition in servicesComponent.providesServices)
					{
						if(definition.definingType.Value != dependencyType)
						{
							continue;
						}

						var clients = servicesComponent.toClients;
						if(clients is Clients.Everywhere || (clientIsComponent && Service.IsAccessibleTo(clientComponent, servicesComponent, clients)))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		internal static Rect GetTagRect(Component component, Rect headerRect, GUIContent label, GUIStyle style)
		{
			var componentTitle = new GUIContent(ObjectNames.GetInspectorTitle(component));
			float componentTitleEndX = 54f + EditorStyles.largeLabel.CalcSize(componentTitle).x + 10f;
			float availableSpace = Screen.width - componentTitleEndX - 69f;
			float labelWidth = style.CalcSize(label).x;
			if(labelWidth > availableSpace)
			{
				labelWidth = availableSpace;
			}
			const float MinWidth = 18f;
			if(labelWidth < MinWidth)
			{
				labelWidth = MinWidth;
			}

			var labelRect = headerRect;
			labelRect.x = Screen.width - 69f - labelWidth;
			labelRect.y += 3f;

			// Fixes Transform header label rect position.
			// For some reason the Transform header rect starts
			// lower and is shorter than all other headers.
			if(labelRect.height < 22f)
			{
				labelRect.y -= 22f - 15f;
			}

			labelRect.height = 20f;
			labelRect.width = labelWidth;
			return labelRect;
		}

		/// <param name="anyProperty"> SerializedProperty of <see cref="Any{T}"/> or some other type field. </param>
		internal static bool Draw(Rect position, GUIContent prefixLabel, SerializedProperty anyProperty, GUIContent serviceLabel = null, bool serviceExists = true)
		{
			var controlRect = EditorGUI.PrefixLabel(position, blankLabel);
			bool clicked = Draw(controlRect, anyProperty, serviceLabel, serviceExists);
			position.width -= controlRect.x - position.x;
			int indentLevelWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			GUI.Label(position, prefixLabel);
			EditorGUI.indentLevel = indentLevelWas;
			return clicked;
		}

		/// <param name="anyProperty"> SerializedProperty of <see cref="Any{T}"/> or some other type field. </param>
		internal static bool Draw(Rect controlRect, SerializedProperty anyProperty = null, GUIContent label = null, bool serviceExists = true)
		{
			label ??= serviceLabel;
			float maxWidth = Styles.ServiceTag.CalcSize(label).x;
			if(controlRect.width > maxWidth)
			{
				controlRect.width = maxWidth;
			}

			controlRect.y += 2f;
			controlRect.height -= 2f;
			int indentLevelWas = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			var backgroundColorWas = GUI.backgroundColor;
			if(serviceExists)
			{
				GUI.backgroundColor = new Color(1f, 1f, 0f);
			}

			bool clicked = GUI.Button(controlRect, label, Styles.ServiceTag);

			GUI.backgroundColor = backgroundColorWas;

			GUILayout.Space(2f);

			EditorGUI.indentLevel = indentLevelWas;

			if(!clicked)
			{
				return false;
			}

			GUI.changed = true;

			if(anyProperty is not null)
			{
				OnServiceTagClicked(controlRect, anyProperty);
			}

			return true;
		}

		internal static void PingServiceOfClient(Object client, Type serviceDefiningType)
		{
			try
			{
				if(ServiceUtility.TryGetFor(client, serviceDefiningType, out var service) && service is Object unityObjectService && unityObjectService)
				{
					EditorGUIUtility.PingObject(unityObjectService);
					
					if(Event.current == null)
					{
						return;
					}

					if(Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseUp && Event.current.type != EventType.ContextClick && Event.current.type != EventType.KeyDown && Event.current.type != EventType.KeyUp)
					{
						return;
					}

					LayoutUtility.ExitGUI();
				}
			}
			catch(TargetInvocationException)
			{

			}

			var typeToPing = ServiceInjector.GetClassWithServiceAttribute(serviceDefiningType);
			if(ServiceAttributeUtility.definingTypes.TryGetValue(serviceDefiningType, out var serviceInfo))
			{
				typeToPing = serviceInfo.classWithAttribute ?? serviceDefiningType;
				if(serviceInfo.FindFromScene)
				{
					if(Find.Any(serviceDefiningType, out var service) && Find.GameObjectOf(service, out GameObject serviceGameObject))
					{
						EditorGUIUtility.PingObject(serviceGameObject);
						LayoutUtility.ExitGUI();
						return;
					}

					if(serviceInfo.SceneBuildIndex >= 0)
					{
						var sceneAsset = Find.SceneAssetByBuildIndex(serviceInfo.SceneBuildIndex);
						if(sceneAsset)
						{
							EditorGUIUtility.PingObject(sceneAsset);
							LayoutUtility.ExitGUI();
							return;
						}
					}
					else if(serviceInfo.SceneName is { Length: > 0 } sceneName)
					{
						var sceneAsset = Find.SceneAssetByName(sceneName);
						EditorGUIUtility.PingObject(sceneAsset);
						LayoutUtility.ExitGUI();
					}
				}
				else if(serviceInfo.ResourcePath is { Length: > 0 } resourcePath)
				{
					var service = Resources.Load<Object>(resourcePath);
					if(service)
					{
						EditorGUIUtility.PingObject(service);
						LayoutUtility.ExitGUI();
						return;
					}
				}
				#if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
				else if(serviceInfo.AddressableKey is { Length: > 0 } addressableKey)
				{
					var service = Addressables.LoadAssetAsync<Object>(addressableKey).WaitForCompletion();
					if(service)
					{
						EditorGUIUtility.PingObject(service);
						LayoutUtility.ExitGUI();
						return;
					}
				}
				#endif
			}
			else
			{
				typeToPing = serviceDefiningType;
			}

			var script = Find.Script(typeToPing);
			if(!script && typeToPing != serviceDefiningType)
			{
				script = Find.Script(serviceDefiningType);
			}

			if(script)
			{
				EditorGUIUtility.PingObject(script);
				LayoutUtility.ExitGUI();
			}
			else
			{
				EditorApplication.ExecuteMenuItem("Window/General/Console");
				Debug.Log($"Could not locate the script that contains the type '{TypeUtility.ToString(typeToPing)}'.\nThis can happen when the name of the script does not match the type name.", client);
			}
		}

		/// <param name="anyProperty"> SerializedProperty of <see cref="Any{T}"/> or some other type field. </param>
		internal static void OnServiceTagClicked(Rect controlRect, SerializedProperty anyProperty)
		{
			if(anyProperty == null)
			{
				#if DEV_MODE
				Debug.LogWarning($"OnServiceTagClicked called but {nameof(anyProperty)} was null.");
				#endif
				return;
			}

			var propertyValue = anyProperty.GetValue();
			if(propertyValue == null)
			{
				#if DEV_MODE
				Debug.LogWarning($"OnServiceTagClicked called but GetValue returned null for {nameof(anyProperty)} '{anyProperty.name}' ('{anyProperty.propertyPath}').");
				#endif
				return;
			}

			Type propertyType = propertyValue.GetType();

			Type serviceType;
			if(typeof(IAny).IsAssignableFrom(propertyType) && propertyType.IsGenericType)
			{
				serviceType = propertyType.GetGenericArguments()[0];
			}
			else
			{
				serviceType = propertyType;
			}

			switch(Event.current.button)
			{
				case 0:
				case 2:
					var targetObject = anyProperty.serializedObject.targetObject;
					PingServiceOfClient(targetObject, serviceType);
					return;
				case 1:
					AnyPropertyDrawer.OpenDropdown(controlRect, anyProperty);
					return;
			}
		}

		internal static void PingService(Object service)
		{
			EditorGUIUtility.PingObject(service);
			LayoutUtility.ExitGUI();
		}

		private struct PingableServiceInfo
		{
			public Transform Transform;
			public Scene Scene;
			public Object PingTarget;

			public PingableServiceInfo(Transform transform, Object pingTarget)
			{
				Transform = transform;
				Scene = transform.gameObject.scene;
				PingTarget = pingTarget;
			}

			public PingableServiceInfo(Services services, Object service) : this(services.transform, service) { }

			public static implicit operator PingableServiceInfo(Service.ActiveServiceInfo activeServiceInfo) => new(activeServiceInfo.Transform, activeServiceInfo.ServiceOrProvider as Object);
			public static implicit operator PingableServiceInfo(ServiceTag serviceTag) => new(serviceTag.transform, serviceTag.Service);
		}

		internal static bool PingServiceFor([DisallowNull] Object client, [DisallowNull] Type serviceType)
		{
			if(TryGetPingableServiceFor(client, serviceType, out var pingTarget))
			{
				if(pingTarget is Component pingableComponent)
				{
					EditorGUIUtility.PingObject(pingableComponent.gameObject);
				}
				else
				{
					EditorGUIUtility.PingObject(pingTarget);
				}

				return true;
			}

			return false;
		}

		internal static bool TryGetPingableServiceFor([DisallowNull] Object client, [DisallowNull] Type serviceType, [MaybeNullWhen(false), NotNullWhen(true)] out Object result)
		{
			var clientTransform = Find.In<Transform>(client);
			var clientIsComponent = clientTransform is not null;
			var clientScene = clientIsComponent ? clientTransform.gameObject.scene : default;

			PingableServiceInfo? nearestInstance = null;

			// In Edit Mode it's possible that the dependency is registered via a value provider in a Service Tag
			// or Services component, but it will only become available at runtime.
			if(!Application.isPlaying)
			{
				foreach(var serviceTag in ServiceTag.AllEditorOnly)
				{
					if(serviceTag.DefiningType != serviceType || !serviceTag.Service)
					{
						continue;
					}
					
					if(!Service.IsAccessibleTo(clientTransform, serviceTag, serviceTag.ToClients))
					{
						continue;
					}

					if(nearestInstance is not { } nearest)
					{
						nearestInstance = serviceTag;
						continue;
					}

					if(serviceTag.gameObject.scene != clientScene)
					{
						continue;
					}

					if(nearest.Scene != clientScene)
					{
						nearestInstance = serviceTag;
						continue;
					}

					var instanceTransform = serviceTag.transform;
					var nearestTransform = nearest.Transform;

					for(var clientParent = clientTransform; clientParent; clientParent = clientParent.parent)
					{
						if(clientParent == instanceTransform)
						{
							#if DEBUG || INIT_ARGS_SAFE_MODE
							if(clientParent == nearestTransform)
							{
								break;
							}
							#endif

							nearestInstance = serviceTag;
							break;
						}

						if(clientParent == nearestTransform)
						{
							break;
						}
					}
				}

				foreach(var servicesComponent in Services.AllEditorOnly)
				{
					if(!Service.IsAccessibleTo(clientTransform, servicesComponent, servicesComponent.toClients))
					{
						continue;
					}
					
					foreach(var definition in servicesComponent.providesServices)
					{
						if(definition.definingType.Value != serviceType || !definition.service)
						{
							continue;
						}

						if(nearestInstance is not { } nearest)
						{
							nearestInstance = new(servicesComponent, definition.service);
							continue;
						}

						if(servicesComponent.gameObject.scene != clientScene)
						{
							continue;
						}

						if(nearest.Scene != clientScene)
						{
							nearestInstance = new(servicesComponent, definition.service);
							continue;
						}

						var instanceTransform = servicesComponent.transform;
						var nearestTransform = nearest.Transform;

						for(var clientParent = clientTransform; clientParent; clientParent = clientParent.parent)
						{
							if(clientParent == instanceTransform)
							{
								#if DEBUG || INIT_ARGS_SAFE_MODE
								if(clientParent == nearestTransform)
								{
									break;
								}
								#endif

								nearestInstance = new Service.ActiveServiceInfo(serviceType, servicesComponent.toClients, definition.service);
								break;
							}

							if(clientParent == nearestTransform)
							{
								break;
							}
						}
					}
				}
			}

			foreach(var instance in Service.ActiveInstancesEditorOnly)
			{
				if(instance.definingType != serviceType)
				{
					continue;
				}

				var pingable = instance.ServiceOrProvider as Object;
				if(!pingable)
				{
					continue;
				}

				var pingableComponent = pingable as Component;
				if(pingableComponent)
				{
					if(!Service.IsAccessibleTo(clientTransform, pingableComponent, instance.ToClients))
					{
						continue;
					}
				}
				else if (instance.ToClients is not Clients.Everywhere)
				{
					continue;
				}

				if(nearestInstance is not { } nearest)
				{
					nearestInstance = instance;
					continue;
				}

				if(instance.Scene != clientScene)
				{
					continue;
				}

				if(nearest.Scene != clientScene)
				{
					nearestInstance = instance;
					continue;
				}

				var instanceTransform = instance.Transform;
				var nearestTransform = nearest.Transform;

				for(var clientParent = clientTransform; clientParent; clientParent = clientParent.parent)
				{
					if(clientParent == instanceTransform)
					{
						#if DEBUG || INIT_ARGS_SAFE_MODE
						if(clientParent == nearestTransform)
						{
							break;
						}
						#endif

						nearestInstance = instance;
						break;
					}

					if(clientParent == nearestTransform)
					{
						break;
					}
				}
			}

			if(nearestInstance is { PingTarget: { } pingTarget })
			{
				result = pingTarget;
				return true;
			}

			if(ServiceAttributeUtility.TryGetInfoForDefiningType(serviceType, out var globalServiceInfo))
			{
				if(globalServiceInfo.FindFromScene)
				{
					if(Find.Any(serviceType, out var service) && Find.UnityObjectOf(service, out result))
					{
						return true;
					}
				}

				if(globalServiceInfo.SceneBuildIndex >= 0)
				{
					result = Find.SceneAssetByBuildIndex(globalServiceInfo.SceneBuildIndex);
					if(result)
					{
						return true;
					}
				}
				else if(globalServiceInfo.ScenePath is { Length: > 0 } scenePath)
				{
					result = Find.SceneAssetByName(scenePath);
					if(result)
					{
						return true;
					}
				}
				else if(globalServiceInfo.ScenePath is { Length: > 0 } sceneName)
				{
					result = Find.SceneAssetByName(sceneName);
					if(result)
					{
						return true;
					}
				}
				else if(globalServiceInfo.ResourcePath is { Length: > 0 } resourcePath)
				{
					result = Resources.Load<Object>(resourcePath);
					if(result)
					{
						return true;
					}
				}
				#if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
				else if(globalServiceInfo.AddressableKey is { Length: > 0 } addressableKey)
				{
					result = Addressables.LoadAssetAsync<Object>(addressableKey).WaitForCompletion();
					if(result)
					{
						return true;
					}
				}
				#endif
				else if(globalServiceInfo.classWithAttribute is { } classWithAttribute)
				{
					result = Find.Script(classWithAttribute);
					if(result)
					{
						return true;
					}
				}
			}

			result = null;
			return false;
		}

		internal static void SelectAllGlobalServiceClientsInScene(object serviceOrServiceProvider)
			=> Selection.objects = GetServiceDefiningTypes(serviceOrServiceProvider).ToArray().SelectMany(FindAllReferences).Distinct().ToArray();

		internal static void SelectAllReferencesInScene(object serviceOrServiceProvider, Type[] definingTypes, Clients clients, Component registerer)
			=> Selection.objects = definingTypes.SelectMany(FindAllReferences).Distinct().Where(go => Service.IsAccessibleTo(go.transform, registerer, clients)).ToArray();

		/// <summary>
		/// Ping MonoScript or GameObject containing the configuration that causes the object, or the value provided by the object
		/// (in the case of a wrapper component etc.), to be a service.
		/// </summary>
		/// <param name="serviceOrServiceProvider">
		/// An object which is a service, or an object which provides the services, such as an <see cref="IWrapper"/>.
		/// </param>
		internal static void PingServiceDefiningObject(Component serviceOrServiceProvider)
		{
			// Ping services component that defines the service, if any...
			var services = Find.All<Services>().FirstOrDefault(s => s.providesServices.Any(i => AreEqual(i.service, serviceOrServiceProvider)));
			if(services)
			{
				EditorGUIUtility.PingObject(services);
				return;
			}

			// Ping MonoScript that contains the ServiceAttribute, if found...
			var serviceOrServiceProviderType = serviceOrServiceProvider.GetType();
			if(HasServiceAttribute(serviceOrServiceProviderType))
			{
				var scriptWithServiceAttribute = Find.Script(serviceOrServiceProviderType);
				if(scriptWithServiceAttribute)
				{
					EditorGUIUtility.PingObject(scriptWithServiceAttribute);
					return;
				}
			}

			// Ping MonoScript of ServiceInitializer
			foreach(Type serviceInitializerType in TypeUtility.GetImplementingTypes<IServiceInitializer>())
			{
				if(serviceInitializerType.IsGenericType && !serviceInitializerType.IsGenericTypeDefinition && !serviceInitializerType.IsAbstract
				&& serviceInitializerType.GetGenericArguments()[0] == serviceOrServiceProviderType && HasServiceAttribute(serviceInitializerType)
				&& Find.Script(serviceInitializerType, out var serviceInitializerScript))
				{
					EditorGUIUtility.PingObject(serviceInitializerScript);
					return;
				}
			}

			if(serviceOrServiceProvider is IWrapper)
			{
				foreach(Type interfaceType in serviceOrServiceProviderType.GetInterfaces())
				{
					if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IWrapper<>)
					&& Find.Script(interfaceType.GetGenericArguments()[0], out var serviceInitializerScript))
					{
						EditorGUIUtility.PingObject(serviceInitializerScript);
						return;
					}
				}
			}
		}

		internal static bool PingDefiningObject([MaybeNull] Component client, Type definingType)
		{
			// Find nearest service that is not transient or lazily initialized 
			if(ServiceUtility.TryGetFor(client, definingType, out var service, Application.isPlaying ? Context.MainThread : Context.EditMode) &&
				service is Object unityObjectService && unityObjectService)
			{
				EditorGUIUtility.PingObject(unityObjectService);
				return true;
			}

			// Ping services component that defines the service, if any...
			var servicesComponent = Find.All<Services>().FirstOrDefault(s => s.providesServices.Any(i => i.definingType.Value == definingType && Service.IsAccessibleTo(client, s, s.toClients)));
			if(servicesComponent)
			{
				EditorGUIUtility.PingObject(servicesComponent.gameObject);
				return true;
			}

			// Ping ServiceTag component that defines the service, if any...
			var serviceTag = Find.All<ServiceTag>().FirstOrDefault(s => s.DefiningType == definingType && Service.IsAccessibleTo(client, s, s.ToClients));
			if(serviceTag)
			{
				EditorGUIUtility.PingObject(serviceTag.gameObject);
				return true;
			}

			if(ServiceAttributeUtility.definingTypes.TryGetValue(definingType, out var serviceInfo))
			{
				if(serviceInfo.SceneName is { Length: > 0 } sceneName)
				{
					var sceneAsset = Find.SceneAssetByName(sceneName);
					if(sceneAsset)
					{
						EditorGUIUtility.PingObject(sceneAsset);
						return true;
					}
				}
				else if(serviceInfo.SceneBuildIndex >= 0)
				{
					var sceneAsset = Find.SceneAssetByBuildIndex(serviceInfo.SceneBuildIndex);
					if(sceneAsset)
					{
						EditorGUIUtility.PingObject(sceneAsset);
						return true;
					}
				}
				
				// Ping MonoScript that contains the ServiceAttribute, if found...
				if(Find.Script(serviceInfo.classWithAttribute ?? serviceInfo.concreteType ?? definingType, out MonoScript scriptWithServiceAttribute))
				{
					EditorGUIUtility.PingObject(scriptWithServiceAttribute);
					return true;
				}
			}

			return false;
		}

		private static bool HasServiceAttribute(Type type) => type.GetCustomAttributes<ServiceAttribute>().Any();

		internal static bool CanAddServiceTag(Component service)
		{
			if(HasServiceAttribute(service.GetType()))
			{
				return false;
			}

			return !Find.All<Services>().Any(s => s.enabled && s.providesServices.Any(i => AreEqual(i.service, service)));
		}

		internal static void OpenToClientsMenu(Component serviceOrServiceProvider, Rect tagRect)
		{
			var tags = GetServiceTagsTargeting(serviceOrServiceProvider).ToArray();
			
			// If has no ServiceTag component then ping defining object.
			if(tags.Length == 0)
			{
				PingServiceDefiningObject(serviceOrServiceProvider);
				return;
			}

			GUI.changed = true;

			var selectedValue = tags[0].ToClients;
#pragma warning disable CS0618 // Type or member is obsolete
			if(selectedValue is Clients.InAllScenes or < 0 or > Clients.Everywhere)
			{
				#if DEV_MODE
				Debug.LogWarning(selectedValue);
				#endif
				selectedValue = Clients.Everywhere;
			}
#pragma warning restore CS0618 // Type or member is obsolete

			var values = new object[]
			{
				Clients.InGameObject,
				Clients.InChildren,
				Clients.InParents,
				Clients.InHierarchyRootChildren,
				Clients.InScene,
				Clients.Everywhere
			};
			var selectedIndex = Array.IndexOf(values, selectedValue);

			var names = new[]
			{
				"In GameObject",
				"In Children",
				"In Parents",
				"In Hierarchy Root Children",
				"In Scene",
				"Everywhere"
			};
			var selectedValueName = names[selectedIndex];

			DropdownWindow.Show(tagRect, names, values, Enumerable.Repeat(selectedValueName, 1), OnItemSelected, "Availability");

			void OnItemSelected(object value)
			{
				Undo.RecordObjects(tags, "Set Service Clients");
				var toClients = (Clients)value;
				foreach(var serviceTag in tags)
				{
					if(toClients == serviceTag.ToClients)
					{
						Undo.DestroyObjectImmediate(serviceTag);
					}
					else
					{
						serviceTag.ToClients = toClients;
					}
				}
			}
		}

		internal static void OpenContextMenuForService(Component serviceOrServiceProvider, Type[] definingTypes, Clients clients, Component registerer, Rect tagRect)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("Find Clients In Scenes"), false, () => SelectAllReferencesInScene(serviceOrServiceProvider, definingTypes, clients, registerer));

			if(HasServiceTag(serviceOrServiceProvider))
			{
				menu.AddItem(new GUIContent("Set Service Types..."), false, () => openSelectTagsMenuFor = serviceOrServiceProvider);

				var tagRectScreenSpace = tagRect;
				tagRectScreenSpace.y += GUIUtility.GUIToScreenPoint(Vector2.zero).y;
				if(EditorWindow.mouseOverWindow)
				{
					tagRectScreenSpace.y -= EditorWindow.mouseOverWindow.position.y;
				}

				menu.AddItem(new GUIContent("Set Availability..."), false, () => OpenToClientsMenu(serviceOrServiceProvider, tagRectScreenSpace));
			}
			else if(!(serviceOrServiceProvider is Services))
			{
				menu.AddItem(new GUIContent("Find Defining Object"), false, () => PingServiceDefiningObject(serviceOrServiceProvider));
			}

			menu.DropDown(tagRect);
		}
		
		internal static void OpenContextMenuForServiceOfClient(Object client, Type serviceType, Rect tagRect)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("Find Service"), false, () => PingServiceOfClient(client, serviceType));

			if(ServiceUtility.TryGetFor(client, serviceType, out object service, Context.MainThread))
			{
				menu.AddItem(new GUIContent("Find Defining Object"), false, () => PingDefiningObject(client as Component, serviceType));
			}

			menu.DropDown(tagRect);
		}

		internal static void OpenSelectTagsMenu(Component service, Rect tagRect)
		{
			if(!CanAddServiceTag(service) && !HasServiceTag(service))
			{
				return;
			}

			GUI.changed = true;

			var typeOptions = GetAllDefiningTypeOptions(service);
			var selectedTypes = GetServiceTagsTargeting(service).Select(tag => tag.DefiningType).ToList();
			TypeDropdownWindow.Show(tagRect, typeOptions, selectedTypes, OnTypeSelected, "Service Types", GetItemContent);

			void OnTypeSelected(Type selectedType)
			{
				Undo.RecordObject(service, "Set Service Type");

				if(ServiceTag.Remove(service, selectedType))
				{
					return;
				}

				ServiceTag.Add(service, selectedType);
				InspectorContents.RepaintEditorsWithTarget(service);
			}

			(string fullPath, Texture icon) GetItemContent(Type type)
			{
				if(type is null)
				{
					return ("Service", null);
				}

				if(type == typeof(Object))
				{
					return ("Reference", EditorGUIUtility.FindTexture("DotSelection"));
				}

				if(!type.IsInstanceOfType(service) || (typeof(IValueProvider).IsAssignableFrom(type) && type.IsInterface))
				{
					return ("Value Provider/" + TypeUtility.ToString(type), EditorGUIUtility.ObjectContent(null, type).image);
				}
				
				if(type.GetCustomAttribute<ValueProviderMenuAttribute>() is ValueProviderMenuAttribute attribute && !string.IsNullOrEmpty(attribute.ItemName))
				{ 
					return (attribute.ItemName, EditorGUIUtility.FindTexture("eyeDropper.Large"));
				}

				return (TypeUtility.ToString(type), EditorGUIUtility.ObjectContent(null, type).image);
			}
		}

		private static bool AreEqual(Type[] pooledArray, in Span<Type> types)
		{
			int pooledArrayLength = types.Length;
			int typeCount = types.Length;
			if(pooledArrayLength < typeCount)
			{
				return false;
			}

			if(pooledArrayLength > typeCount && pooledArray[typeCount] is not null)
			{
				return false;
			}

			for(var i = 0; i < typeCount; i++)
			{
				if(Array.IndexOf(pooledArray, types[i]) == -1)
				{
					return false;
				}
			}

			return true;
		}

		private static bool AreEqual(object x, object y)
		{
			if(ReferenceEquals(x, y))
			{
				return true;
			}

			if(x is IValueProvider xValueProvider)
			{
				object xValue = xValueProvider.Value;
				if(ReferenceEquals(xValue, y))
				{
					return true;
				}
				
				if(y is IValueProvider yValueProvider)
				{
					if(ReferenceEquals(xValue, yValueProvider.Value))
					{
						return true;
					}
				}
			}
			else if(y is IValueProvider yValueProvider)
			{
				if(ReferenceEquals(x, yValueProvider.Value))
				{
					return true;
				}
			}

			return false;
		}

		private static IEnumerable<GameObject> FindAllReferences(Type serviceType)
		{
			for(int s = SceneManager.sceneCount - 1; s >= 0; s--)
			{
				var scene = SceneManager.GetSceneAt(s);
				var rootGameObjects = scene.GetRootGameObjects();
				for(int r = rootGameObjects.Length - 1; r >= 0; r--)
				{
					foreach(var reference in FindAllReferences(rootGameObjects[r].transform, serviceType))
					{
						yield return reference;
					}
				}
			}
		}

		private static IEnumerable<GameObject> FindAllReferences(Transform transform, Type serviceType)
		{
			var components = transform.gameObject.GetComponentsNonAlloc<Component>();

			// Skip component at index 0 which is most likely a Transform.
			for(int c = components.Count - 1; c >= 1; c--)
			{
				var component = components[c];
				if(!component)
				{
					continue;
				}

				var componentType = component.GetType();

				if(component is IOneArgument)
				{
					if(componentType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IArgs<>)) is Type initializableType)
					{
						var argTypes = initializableType.GetGenericArguments();
						if(argTypes[0] == serviceType)
						{
							yield return component.gameObject;
							continue;
						}
					}
				}
				else if(component is ITwoArguments)
				{
					if(componentType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IArgs<,>)) is Type initializableType)
					{
						var argTypes = initializableType.GetGenericArguments();
						if(argTypes[0] == serviceType || argTypes[1] == serviceType)
						{
							yield return component.gameObject;
							continue;
						}
					}
				}
				else if(component is IThreeArguments)
				{
					if(componentType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IArgs<,,>)) is Type initializableType)
					{
						var argTypes = initializableType.GetGenericArguments();
						if(argTypes[0] == serviceType || argTypes[1] == serviceType || argTypes[2] == serviceType)
						{
							yield return component.gameObject;
							continue;
						}
					}
				}
				else if(component is IFourArguments)
				{
					if(componentType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IArgs<,,,>)) is Type initializableType)
					{
						var argTypes = initializableType.GetGenericArguments();
						if(argTypes[0] == serviceType || argTypes[1] == serviceType || argTypes[2] == serviceType || argTypes[3] == serviceType)
						{
							yield return component.gameObject;
							continue;
						}
					}
				}
				else if(component is IFiveArguments)
				{
					if(componentType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IArgs<,,,,>)) is Type initializableType)
					{
						var argTypes = initializableType.GetGenericArguments();
						if(argTypes[0] == serviceType || argTypes[1] == serviceType || argTypes[2] == serviceType || argTypes[3] == serviceType || argTypes[4] == serviceType)
						{
							yield return component.gameObject;
							continue;
						}
					}
				}

				var serializedObject = new SerializedObject(component);
				var property = serializedObject.GetIterator();
				string serviceTypeName = serviceType.Name;
				string serviceTypeNameAlt = string.Concat("PPtr<", serviceTypeName, ">");
				
				if(property.NextVisible(true))
				{
					do
					{
						if(string.Equals(property.type, "Any`1") && property.GetValue() is object any)
						{
							Type anyValueType = any.GetType().GetGenericArguments()[0];
							if(anyValueType == serviceType)
							{
								yield return component.gameObject;
							}
						}
						else if((string.Equals(property.type, serviceTypeName) || string.Equals(property.type, serviceTypeNameAlt)) && property.GetType() == serviceType)
						{
							yield return component.gameObject;
						}
					}
					// Checking only surface level fields, not nested fields, for performance reasons.
					while(property.NextVisible(false));
				}
			}

			for(int i = transform.childCount - 1; i >= 0; i--)
			{
				foreach(var reference in FindAllReferences(transform.GetChild(i), serviceType))
				{
					yield return reference;
				}
			}
		}

		#if DEV_MODE && DEBUG && !INIT_ARGS_DISABLE_PROFILING
		private static readonly ProfilerMarker getServiceDefiningTypesMarker = new(ProfilerCategory.Gui, "EditorServiceTagUtility.GetServiceDefiningTypes");
		#endif
	}
}