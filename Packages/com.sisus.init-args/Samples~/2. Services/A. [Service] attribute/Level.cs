using UnityEngine;

namespace Sisus.Init.Demos.Services
{
	/// <summary>
	/// Represents a level in the game, defining the bounds inside which
	/// game objects can be positioned.
	/// </summary>
	[Service(typeof(Level), FindFromScene = true, LazyInit = true)]
	public class Level : MonoBehaviour<RectInt>
	{
		[SerializeField] RectInt bounds = new(0, 0, 19, 19);

		public RectInt Bounds => bounds;

		// Allow passing in custom bounds during initialization to facilitate testing.
		protected override void Init(RectInt bounds) => this.bounds = bounds;
	}
}