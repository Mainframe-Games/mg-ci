using System;

namespace BuildSystem.PostProcessors.PList
{
	[Serializable]
	public abstract class PListElement
	{
		public string Key;
	}

	[Serializable]
	public class PListElementBool : PListElement
	{
		public bool Value;
	}

	[Serializable]
	public class PListElementString : PListElement
	{
		public string Value;
	}
	
	[Serializable]
	public class PListElementInt : PListElement
	{
		public int Value;
	}
	
	[Serializable]
	public class PListElementFloat : PListElement
	{
		public float Value;
	}
}