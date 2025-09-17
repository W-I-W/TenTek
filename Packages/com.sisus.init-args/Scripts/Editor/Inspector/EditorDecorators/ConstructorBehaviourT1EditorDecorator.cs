#pragma warning disable CS0618

using System;
using UnityEditor;

namespace Sisus.Init.EditorOnly
{
	internal sealed class ConstructorBehaviourT1EditorDecorator : BaseConstructorBehaviourEditorDecorator
	{
		protected override Type BaseTypeDefinition => typeof(ConstructorBehaviour<>);
		public ConstructorBehaviourT1EditorDecorator(Editor decoratedEditor) : base(decoratedEditor) { }
	}
}