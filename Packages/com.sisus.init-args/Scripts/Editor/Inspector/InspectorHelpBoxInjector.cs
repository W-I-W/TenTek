using System;
using Sisus.Init.Internal;
using Sisus.Init.ValueProviders;
using Sisus.Shared.EditorOnly;
using UnityEditor;
using UnityEngine;

namespace Sisus.Init.EditorOnly.Internal
{
	/// <summary>
	/// Class that can be used to insert boxes containing texts into the Inspector
	/// below the headers of components of particular types.
	/// </summary>
	[InitializeOnLoad]
	internal static class InspectorHelpBoxInjector
	{
		const float topPadding = 3f;
		const float minHeight = 30f;
		const float insidePadding = 8f;
		const float bottomPadding = 5f;
		const float leftPadding = 15f;
		static readonly Vector2 iconSize = new(15f, 15f);

		static readonly GUIContent content = new("");

		static InspectorHelpBoxInjector()
		{
			ComponentHeader.AfterHeaderGUI -= OnAfterHeaderGUI;
			ComponentHeader.AfterHeaderGUI += OnAfterHeaderGUI;
		}

		static void OnAfterHeaderGUI(Component[] targets, Rect headerRect, bool HeaderIsSelected, bool supportsRichText)
		{
			var component = targets[0];
			if(component is Services servicesComponent)
			{
				foreach(var serviceDefinition in servicesComponent.providesServices)
				{
					var service = serviceDefinition.service;
					if(!service)
					{
						continue;
					}

					var definingType = (Type)serviceDefinition.definingType;
					if(definingType is not null
						&& !definingType.IsInstanceOfType(service)
						&& !ValueProviderUtility.IsValueProvider(service))
					{
						var concreteType = service.GetType();
						content.text = definingType.IsInterface
							? $"Invalid Service Definition: {TypeUtility.ToString(concreteType)} has been configured as a service with the defining type {TypeUtility.ToString(definingType)}, but {TypeUtility.ToString(concreteType)} does not implement {TypeUtility.ToString(definingType)}."
							: $"Invalid Service Definition: {TypeUtility.ToString(concreteType)} has been configured as a service with the defining type {TypeUtility.ToString(definingType)}, but {TypeUtility.ToString(concreteType)} does not derive from {TypeUtility.ToString(definingType)}.";

						DrawHelpBox(MessageType.Warning, content);
					}

					definingType ??= service.GetType();
					if(ServiceAttributeUtility.ContainsDefiningType(definingType))
					{
						content.text = GetReplacesDefaultServiceText(definingType, servicesComponent.toClients);
						DrawHelpBox(MessageType.Info, content);
					}
					else if(service is Component serviceComponent && TryGetServiceTag(serviceComponent, serviceDefinition.definingType, out _))
					{
						content.text = GetReplacesDefaultServiceText(definingType, servicesComponent.toClients);
						DrawHelpBox(MessageType.Info, content);
					}
				}
			}
			else
			{
				foreach(var serviceTag in ServiceTagUtility.GetServiceTagsTargeting(component))
				{
					if(serviceTag.DefiningType is { } definingType && ServiceAttributeUtility.ContainsDefiningType(definingType))
					{
						content.text = GetReplacesDefaultServiceText(definingType, serviceTag.ToClients);
						DrawHelpBox(MessageType.Info, content);
					}
				}
			}
		}

		static bool TryGetServiceTag(Component component, Type matchingDefiningType, out ServiceTag result)
		{
			foreach(var serviceTag in ServiceTagUtility.GetServiceTagsTargeting(component))
			{
				if(serviceTag.DefiningType == matchingDefiningType)
				{
					result = serviceTag;
					return true;
				}
			}

			result = null;
			return false;
		}

		static string GetReplacesDefaultServiceText(Type serviceType, Clients toClients)
		{
			return toClients switch
			{
				Clients.Everywhere => $"Replaces the default {serviceType.Name} service for all clients.",
				Clients.InGameObject => $"This Replaces the default {serviceType.Name} service for clients in this game object.",
				Clients.InChildren => $"Replaces the default {serviceType.Name} service for clients in this game object and all of its children.",
				Clients.InParents => $"Replaces the default {serviceType.Name} service for clients in this game object and all of its parents.",
				Clients.InHierarchyRootChildren => $"Replaces the default {serviceType.Name} service for clients in the root game object and all of its children.",
				Clients.InScene => $"Replaces the default {serviceType.Name} service for clients in this scene.",
				#pragma warning disable CS0618 // Type or member is obsolete
				Clients.InAllScenes => $"Replaces the default {serviceType.Name} service for clients in all scenes.",
				#pragma warning restore CS0618 // Type or member is obsolete
				_ => $"Replaces the default {serviceType.Name} service for clients {toClients}."
			};
		}

		static void DrawHelpBox(MessageType messageType, GUIContent label)
		{
			GUILayout.Space(topPadding);

			EditorGUIUtility.SetIconSize(iconSize);
			Vector2 size = EditorStyles.helpBox.CalcSize(label);
			size.y = Mathf.Max(size.y + insidePadding, minHeight);
			Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(size.y));
			rect.x += leftPadding;
			rect.width -= leftPadding;
			
			EditorGUI.HelpBox(rect, label.text, messageType);

			GUILayout.Space(bottomPadding);
		}
	}
}