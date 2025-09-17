#pragma warning disable CS8524

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using static Sisus.Init.ValueProviders.ValueProviderUtility;

namespace Sisus.Init
{
	/// <summary>
	/// Encapsulates all the different value provider types that can be used to provide a service of a particular type.
	/// </summary>
	/// <typeparam name="TService"> Type of the service provided. </typeparam>
	internal class ServiceProvider<TService> : IEquatable<ServiceProvider<TService>>
	{
		private readonly ServiceProviderType providerType;
		private readonly IValueProvider<TService> valueProviderT;
		private readonly IValueProviderAsync<TService> valueProviderAsyncT;
		private readonly IValueByTypeProvider valueByTypeProvider;
		private readonly IValueByTypeProviderAsync valueByTypeProviderAsync;
		private readonly IValueProviderAsync valueProviderAsync;
		private readonly IValueProvider valueProvider;

		internal ServiceProvider(IValueProvider<TService> provider)
		{
			providerType = ServiceProviderType.IValueProviderT;
			valueProviderT = provider;
		}

		internal ServiceProvider(IValueProviderAsync<TService> provider)
		{
			providerType = ServiceProviderType.IValueProviderAsyncT;
			valueProviderAsyncT = provider;
		}

		internal ServiceProvider(IValueByTypeProvider provider)
		{
			providerType = ServiceProviderType.IValueByTypeProvider;
			valueByTypeProvider = provider;
		}

		internal ServiceProvider(IValueByTypeProviderAsync provider)
		{
			providerType = ServiceProviderType.IValueByTypeProviderAsync;
			valueByTypeProviderAsync = provider;
		}

		internal ServiceProvider(IValueProvider provider)
		{
			providerType = ServiceProviderType.IValueProvider;
			valueProvider = provider;
		}

		internal ServiceProvider(IValueProviderAsync provider)
		{
			providerType = ServiceProviderType.IValueProviderAsync;
			valueProviderAsync = provider;
		}

		public bool TryGetFor([AllowNull] Component client, out TService result) => providerType switch
		{
			ServiceProviderType.IValueProviderT => valueProviderT.TryGetFor(client, out result),
			ServiceProviderType.IValueProviderAsyncT => TryGetFromAwaitableIfCompleted(valueProviderAsyncT.GetForAsync(client), out result),
			ServiceProviderType.IValueByTypeProvider => valueByTypeProvider.TryGetFor(client, out result),
			ServiceProviderType.IValueByTypeProviderAsync => TryGetFromAwaitableIfCompleted(valueByTypeProviderAsync.GetForAsync<TService>(client), out result),
			ServiceProviderType.IValueProvider => valueProvider.TryGetFor(client, out object objectValue) ? Find.In(objectValue, out result) : None(out result),
			ServiceProviderType.IValueProviderAsync => TryGetFromAwaitableIfCompleted(valueProviderAsync.GetForAsync(client), out result),
			_ => None(out result)
		};

		private static bool None(out TService result)
		{
			result = default;
			return false;
		}

		#if (ENABLE_BURST_AOT || ENABLE_IL2CPP) && !INIT_ARGS_DISABLE_AUTOMATIC_AOT_SUPPORT
		private static void EnsureAOTPlatformSupport() => ServiceUtility.EnsureAOTPlatformSupportForService<TService>();
		#endif

		public bool Equals(ServiceProvider<TService> other) => other is not null && providerType == other.providerType && ReferenceEquals(GetValueProvider(), other.GetValueProvider());

		public override bool Equals(object obj)
		{
			if(obj is not ServiceProvider<TService> serviceProvider)
			{
				return false;
			}

			return Equals(serviceProvider);
		}

		public override int GetHashCode() => HashCode.Combine(providerType, GetValueProvider()?.GetHashCode() ?? 0);

		private object GetValueProvider() => providerType switch
		{
			ServiceProviderType.IValueProviderT => valueProviderT,
			ServiceProviderType.IValueProviderAsyncT => valueProviderAsyncT,
			ServiceProviderType.IValueByTypeProvider => valueByTypeProvider,
			ServiceProviderType.IValueByTypeProviderAsync => valueByTypeProviderAsync,
			ServiceProviderType.IValueProvider => valueProvider,
			ServiceProviderType.IValueProviderAsync => valueProviderAsync,
			_ => 0
		};
	}
}