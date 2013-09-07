﻿using System.Linq;
using Synthesis.FieldTypes.Interfaces;

namespace Synthesis.Testing.Fields
{
	/// <summary>
	/// Encapsulates a boolean (checkbox) field from Sitecore
	/// </summary>
	public class TestBooleanField : TestFieldType, IBooleanField
	{
		public TestBooleanField(bool value)
		{
			Value = value;
		}

		public bool Value { get; private set; }

		public override bool HasValue
		{
			get { return true; }
		}
	}
}
