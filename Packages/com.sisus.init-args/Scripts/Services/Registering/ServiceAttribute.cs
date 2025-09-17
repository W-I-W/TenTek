﻿using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Sisus.Init
{
	/// <summary>
	/// Classes that have the <see cref="ServiceAttribute"/> can provide services
	/// for one or more client objects.
	/// <para>
	/// A single instance of each class that has the attribute will automatically
	/// get loaded behind the scenes making them ready for clients to retrieve as needed.
	/// </para>
	/// <para>
	/// Objects deriving from <see cref="MonoBehaviour{TDefiningClassOrInterface}"/> or <see cref="ScriptableObject{TDefiningClassOrInterface}"/>
	/// receive the service during initialization automatically.
	/// </para>
	/// <para>
	/// Other clients can retrieve a service by implementing the <see cref="IArgs{TDefiningClassOrInterface}"/>
	/// interface and calling <see cref="InitArgs.TryGet{TClient, TDefiningClassOrInterface}"/> during initialization.
	/// </para>
	/// <para>
	/// It is possible to receive more than one services automatically by implementing an <see cref="IArgs{TFirstService, TSecondService}">IArgs{}</see>
	/// interface with more than one service argument (upto a maximum of five).
	/// </para>
	/// <para>
	/// Services can also receive other services during initialization by implementing an <see cref="IInitializable{TService}"/> interface targeting
	/// the services they depend on.
	/// </para>
	/// <para>
	/// <see cref="Initializer{}"/> classes are also able to retrieve all service instances automatically and inject them to the client's Init method.
	/// </para>
	/// <para>
	/// <see cref="Service{TDefiningClassOrInterface}.Instance"/> can be used to manually retrieve a service object.
	/// </para>
	/// <para>
	/// Service objects that implement <see cref="IAwake"/>, <see cref="IOnEnable"/> or <see cref="IStart"/> receive the relevant event after all
	/// services have been created and all services have received the other services they depend on.
	/// </para>
	/// <para>
	/// A service can optionally receive callbacks during select unity event functions by implementing one or more of the following interfaces:
	/// <list type="bullet">
	/// <item>
	/// <term> <see cref="IAwake"/> </term>
	/// <description> Receive callback during the MonoBehaviour.Awake event. </description>
	/// </item>
	/// <item>
	/// <term> <see cref="IOnEnable"/> </term>
	/// <description> Receive callback during the MonoBehaviour.OnEnable event. </description>
	/// </item>
	/// <item>
	/// <term> <see cref="IStart"/> </term>
	/// <description> Receive callback during the MonoBehaviour.Start event. </description>
	/// </item>
	/// <item>
	/// <term> <see cref="IUpdate"/> </term>
	/// <description> Receive the MonoBehaviour.Update event. </description>
	/// </item>
	/// <item>
	/// <term> <see cref="ILateUpdate"/> </term>
	/// <description> Receive callback during the MonoBehaviour.LateUpdate event. </description>
	/// </item>
	/// <item>
	/// <term> <see cref="IFixedUpdate"/> </term>
	/// <description> Receive callback during the MonoBehaviour.FixedUpdate event. </description>
	/// </item>
	/// </list>
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
	[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	[RequireAttributeUsages]
	public class ServiceAttribute : PreserveAttribute
	{
		/// <summary>
		/// Class or interface type that uniquely defines the service and can be used to retrieve an instance of it.
		/// <para>
		/// This must be an interface that the service implement, a base type that the service derives from, or the exact type of the service.
		/// </para>
		/// </summary>
		[DisallowNull]
		public readonly Type[] definingTypes;

		internal LoadMethod loadMethod { get; set; }
		internal ReferenceType referenceType { get; set; }

		[MaybeNull]
		internal string loadData;

		#if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
		/// <summary>
		/// Addressable key using which an instance of the service can be loaded during initialization.
		/// <para>
		/// <see langword="null"/> if the service is not loaded using the Addressable asset system.
		/// </para>
		/// </summary>
		[DisallowNull]
		public string AddressableKey
		{
			get => referenceType is ReferenceType.AddressableKey ? loadData : null;

			set
			{
				if(!string.IsNullOrEmpty(value))
				{
					referenceType = ReferenceType.AddressableKey;
					loadData = value;
				}
			}
		}
		#endif

		/// <summary>
		/// Resources path from which an instance of the service can be loaded during initialization.
		/// <para>
		/// <see langword="null"/> if the service is not loaded from a Resources folder.
		/// </para>
		/// </summary>
		[DisallowNull]
		public string ResourcePath
		{
			get => referenceType is ReferenceType.ResourcePath ? loadData : null;

			set
			{
				if(!string.IsNullOrEmpty(value))
				{
					referenceType = ReferenceType.ResourcePath;
					loadData = value;
				}
			}
		}

		/// <summary>
		/// If <see langword="true"/> then an instance of the service
		/// will be loaded from the initial scene.
		/// </summary>
		/// <seealso cref="UnityEngine.Object.FindAnyObjectByType(Type)"/>
		public bool FindFromScene
		{
			get => loadMethod is LoadMethod.FindFromScene && referenceType is ReferenceType.None;

			set
			{
				if(value)
				{
					referenceType = ReferenceType.None;
					loadMethod = LoadMethod.FindFromScene;
				}
			}
		}

		/// <summary>
		/// The name (<see langword="string"/>) or build index (<see langword="int"/>) of the scene containing the service, if provided; otherwise, null.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If the scene in question is not loaded when the service is being initialized, the scene will be loaded additively.
		/// </para>
		/// <para>
		/// Note that objects in loaded scenes don't become active immediately, but only at the end of the frame, which means that
		/// any clients in the initial scene that depend on services in scenes that are loaded on-the-fly must be initialized asynchronously.
		/// </para>
		/// </remarks>
		public object LoadScene
		{
			get => referenceType switch
			{
				ReferenceType.SceneName => loadData is { Length: > 0 } ? loadData : null,
				ReferenceType.SceneBuildIndex => int.TryParse(loadData, out int buildIndex) && buildIndex >= 0 ? buildIndex : null,
				_ => null
			};

			set
			{
				if(value is string { Length: > 0 } sceneName)
				{
					referenceType = ReferenceType.SceneName;
					loadMethod = LoadMethod.FindFromScene;
					loadData = sceneName;
				}
				else if(value is int sceneBuildIndex and >= 0)
				{
					referenceType = ReferenceType.SceneBuildIndex;
					loadMethod = LoadMethod.FindFromScene;
					loadData = sceneBuildIndex.ToString();
				}
			}
		}

		/// <summary>
		/// The name of the scene containing the service, if provided; otherwise, null.
		/// </summary>
		/// <remarks>
		/// If the scene in question is not loaded when the service is being initialized, the scene will be loaded additively.
		/// </remarks>
		public string SceneName
		{
			get => referenceType == ReferenceType.SceneName ? loadData : "";

			[Obsolete("Use " + nameof(LoadScene) + " instead.")]
			set
			{
				if(!string.IsNullOrEmpty(value))
				{
					referenceType = ReferenceType.SceneName;
					loadMethod = LoadMethod.FindFromScene;
					loadData = value;
				}
			}
		}

		/// <summary>
		/// The build index of the scene containing the service, if provided; otherwise, null.
		/// </summary>
		/// <remarks>
		/// If the scene in question is not loaded when the service is being initialized, the scene will be loaded additively.
		/// </remarks>
		public int SceneBuildIndex
		{
			get => referenceType == ReferenceType.SceneBuildIndex && int.TryParse(loadData, out int buildIndex) ? buildIndex : -1;

			[Obsolete("Use " + nameof(LoadScene) + " instead.")]
			set
			{
				if(value >= 0)
				{
					referenceType = ReferenceType.SceneBuildIndex;
					loadMethod = LoadMethod.FindFromScene;
					loadData = value.ToString();
				}
			}
		}

		/// <summary>
		/// <para>
		/// If <see langword="true"/>, then service instance will be a direct reference to an asset that is loaded using a resource path or an addressable key.
		/// </para>
		/// <para>
		/// If <see langword="false"/>, then service instance will be a clone instantiated from an asset that is loaded using a resource path or an addressable key.
		/// </para>
		/// </summary>
		public bool Instantiate
		{
			get => loadMethod is LoadMethod.Instantiate;
			set => loadMethod = value ? LoadMethod.Instantiate : LoadMethod.Load;
		}

		/// <summary>
		/// <para>
		/// If set to <see langword="true"/> then an instance of the service will only
		/// be created on demand when the first client requests an instance.
		/// </para>
		/// <para>
		/// Otherwise, the instance will be created immediately when the game loads,
		/// before the Awake event method is executed for any components in the first scene.
		/// </para>
		/// </summary>
		/// <remarks>
		/// Note that the initialization of service instances of some types is not a
		/// thread-safe operation, so if <see cref="LazyInit"/> is set to <see langword="true"/>
		/// and the first client that requests the service does so from a background
		/// thread the initialization of the service could fail.
		/// </remarks>>
		public bool LazyInit
		{
			get => lazyInit ?? false;
			set => lazyInit = value;
		}

		internal bool? lazyInit { get; private set; }

		/// <summary>
		/// If set to <see langword="true"/> then the service will be loaded asynchronously if possible.
		/// <para>
		/// When set to <see langword="true"/> and <see cref="ResourcePath"/> is given a non-empty value,
		/// then <see cref="UnityEngine.Resources.LoadAsync"/> will be used to load the resource.
		/// </para>
		/// <para>
		/// When set to <see langword="true"/> and <see cref="AddressableKey"/> is given a non-empty value,
		/// then <see cref="Addressables.LoadAssetAsync"/> will be used to load the asset.
		/// </para>
		/// </summary>
		public bool LoadAsync { get; set; }

		/// <summary>
		/// Should the service object be moved into the <see cref="UnityEngine.Object.DontDestroyOnLoad"/> scene?
		/// <para>
		/// By default service objects will use DontDestroyOnLoad if they are created by Init(args), but be left in their
		/// original scene if using <see cref="FindFromScene"/>.
		/// </para>
		/// </summary>
		/// <remarks>
		/// Objects in the <see cref="UnityEngine.Object.DontDestroyOnLoad"/> scene are not destroyed
		/// when loading a new Scene, even if <see cref="UnityEngine.SceneManagement.LoadSceneMode.Single"/> is used.
		/// </remarks>
		public bool DontDestroyOnLoad
		{
			get => dontDestroyOnLoad ?? false;
			set => dontDestroyOnLoad = value;
		}

		internal bool? dontDestroyOnLoad;

		#if UNITY_ENTITIES && !INIT_ARGS_DISABLE_ECS_SUPPORT
		/// <summary>
		/// <para>
		/// Name of the <see cref="Unity.Entities.World"/> this service is or belongs to.
		/// </para>
		/// <para>
		/// If the service is a system (derives from <see cref="Unity.Entities.SystemBase"/> or implements <see cref="Unity.Entities.ISystem"/>)
		/// then this property determines the world that should contain the system. If world does not exist during initialization of the service,
		/// it will be created automatically. If no existing instance of the system is found during the initialization of the service,
		/// the system will be created automatically in the specified world. If no existing instance of the system is found and the value of this property is
		/// null or empty, then the system will be created in the <see cref="Unity.Entities.World.DefaultGameObjectInjectionWorld">default injection world</see>.
		/// </para>
		/// <para>
		/// If the service is a <see cref="Unity.Entities.World"/>, then the value of this property determines the name of the world.
		/// </para>
		/// </summary>
		/// <remarks>
		/// <para>
		/// Unity will create instances of all systems automatically by default. You can disable this behavior by adding the
		/// <see cref="Unity.Entities.DisableAutoCreationAttribute"/> to the system's type or assembly, or by defining the
		/// UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP scripting symbol in your Project Settings.
		/// </para>
		/// <para>
		/// If the service is not a <see cref="Unity.Entities.World"/> nor a <see cref="Unity.Entities.SystemBase">system</see>,
		/// then the value of this property is ignored.
		/// </para>
		/// <para>
		/// Systems and worlds that have the <see cref="ServiceAttribute"/> can receive other services that they depend
		/// on as constructor or <see cref="IInitializable{TService}.Init"/> method arguments.
		/// </para>
		/// </remarks>
		[DisallowNull]
		public string World
		{
			get => referenceType is ReferenceType.World ? loadData : null;

			set
			{
				if(!string.IsNullOrEmpty(value))
				{
					referenceType = ReferenceType.World;
					loadData = value;
				}
			}
		}
		#endif

		#if DEV_MODE
		/// <summary>
		/// If set to <see langword="true"/> then the service will be automatically registered
		/// using all the interfaces that it implements as its <see cref="definingType">defining types</see>.
		/// </summary>
		public bool RegisterAsAllInterfaces { get; set; }
		#endif

		/// <summary>
		/// Classes that have the <see cref="ServiceAttribute"/> can provide services
		/// for one or more client objects.
		/// <para>
		/// A single instance of each class that has the attribute will automatically
		/// get cached behind the scenes making them ready for clients to retrieve as needed.
		/// </para>
		/// <para>
		/// A fresh instance of the service will be created unless <see cref="ResourcePath"/> or
		/// <see cref="AddressableKey"/> is used to specify a location from which an existing
		/// instance of the service can be loaded.
		/// </para>
		/// </summary>
		/// <param name="definingTypes">
		/// Class or interface types that can be used to retrieve an instance of it.
		/// <para>
		/// These should be interfaces that the service implement, bases type that the service derives from, or the exact type of the service.
		/// </para>
		/// </param>
		public ServiceAttribute(params Type[] definingTypes)
		{
			this.definingTypes = definingTypes ?? Array.Empty<Type>();
			loadMethod = LoadMethod.Default;
			loadData = null;
		}
	}

	internal enum ReferenceType
	{
		/// <summary>
		/// No reference; create a new instance or find by type.
		/// </summary>
		None,

		/// <summary>
		/// A direct reference to a prefab or a scene object.
		/// </summary>
		DirectReference,

		/// <summary>
		/// A reference to a scene by name.
		/// </summary>
		SceneName,

		/// <summary>
		/// A reference to a scene by build index.
		/// </summary>
		SceneBuildIndex,

		/// <summary>
		/// Loads or instantiates an asset using resources.
		/// </summary>
		ResourcePath,

		#if UNITY_ADDRESSABLES_1_17_4_OR_NEWER
		/// <summary>
		/// Loads or instantiates an asset using addressables.
		/// </summary>
		AddressableKey,
		#endif

		#if UNITY_ENTITIES && !INIT_ARGS_DISABLE_ECS_SUPPORT
		/// <summary>
		/// Gets or creates an ECS world, or a system from the world.
		/// </summary>
		World,
		#endif
	}

	internal enum LoadMethod
	{
		/// <summary>
		/// Uses <see cref="Instantiate"/> for prefabs and <see cref="Load"/> for other assets.
		/// </summary>
		Default,

		/// <summary>
		/// Loads an asset using either resources or addressables, and registers it as a service directly.
		/// </summary>
		Load,

		/// <summary>
		/// Creates a new instance from scratch or by cloning an asset.
		/// </summary>
		Instantiate,

		/// <summary>
		/// Finds an object from the initial scene, and registers it as a service.
		/// </summary>
		FindFromScene
	}
}