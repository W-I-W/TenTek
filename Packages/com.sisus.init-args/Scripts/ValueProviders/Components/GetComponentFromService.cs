using System;
using UnityEngine;

namespace Sisus.Init.ValueProviders
{
	/// <summary>
	/// <para>
	/// Base class for value providers that return an object of the requested type
	/// attached to a service of type <typeparamref name="TService"/>.
	/// </para>
	/// <para>
	/// Can be used to retrieve an Init argument at runtime.
	/// </para>
	/// </summary>
	/// <typeparam name="TService">
	/// <para>
	/// The defining type of the service from which the component will be retrieved.
	/// </para>
	/// <para>
	/// This type must be an interface that the service implements, a base type that the service derives from,
	/// or the exact type of the service.
	/// </para>
	/// </typeparam>
	public abstract class GetComponentFromService<TService> : ScriptableObject, IValueByTypeProvider
	#if UNITY_EDITOR
	, INullGuardByType
	#endif
	{
		/// <summary>
		/// Gets an object of type <typeparamref name="TValue"/> attached to the <paramref name="client"/>.
		/// </summary>
		/// <typeparam name="TValue"> Type of object to find. </typeparam>
		/// <param name="client"> The <see cref="GameObject"/> to search. </param>
		/// <param name="value">
		/// When this method returns, contains an object <typeparamref name="TValue"/> if one was found; otherwise, the <see langword="null"/>. This parameter is passed uninitialized.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if an object was found; otherwise, <see langword="false"/>.
		/// </returns>
		public bool TryGetFor<TValue>(Component client, out TValue value)
		{
			var service = client ? Service.GetFor<TService>(client) : Service.Get<TService>();
			return Find.In(service, out value);
		}

		bool IValueByTypeProvider.CanProvideValue<TValue>(Component client) => client ? Service.ExistsFor<TService>(client) : Service.Exists<TService>() && Find.typesToFindableTypes.ContainsKey(typeof(TValue));
		bool IValueByTypeProvider.CanProvideValue(Component client, Type valueType) => client ? Service.ExistsFor<TService>(client) : Service.Exists<TService>() && Find.typesToFindableTypes.ContainsKey(valueType);
		bool IValueByTypeProvider.HasValueFor<TValue>(Component client) => TryGetFor<TValue>(client, out _);

		#if UNITY_EDITOR
		NullGuardResult INullGuardByType.EvaluateNullGuard<TValue>(Component client)
		{
			if(!Find.typesToFindableTypes.ContainsKey(typeof(TValue)))
			{
				return NullGuardResult.TypeNotSupported;
			}

			var service = client ? Service.ExistsFor<TService>(client) : Service.Exists<TService>();
			return service ? NullGuardResult.Passed : NullGuardResult.ValueProviderValueNullInEditMode;
		}
		#endif
	}
}