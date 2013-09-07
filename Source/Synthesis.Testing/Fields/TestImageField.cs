﻿using System.Linq;
using Synthesis.FieldTypes.Interfaces;

namespace Synthesis.Testing.Fields
{
	public class TestImageField : TestFileField, IImageField
	{
		public TestImageField(string url, int? width = null, int? height = null, string alternateText = null)
			: base(url)
		{
			Width = width;
			Height = height;
			AlternateText = alternateText;
		}

		/// <summary>
		/// Gets the width of the image, if one was entered
		/// </summary>
		public int? Width { get; set; }

		/// <summary>
		/// Gets the height of the image, if one was entered
		/// </summary>
		public int? Height { get; set; }

		/// <summary>
		/// Gets the alt text of the image, if any was entered
		/// </summary>
		public string AlternateText { get; set; }

		/// <summary>
		/// Renders the field using a Sitecore FieldRenderer and returns the result
		/// </summary>
		public string RenderedValue
		{
			get
			{
				var tag = string.Format("<img src=\"{0}\" alt=\"{1}\"", Url ?? string.Empty, AlternateText ?? string.Empty);
				if (Width.HasValue)
					tag += " width=\"" + Width.Value + "\"";

				if (Height.HasValue)
					tag += " height=\"" + Height.Value + "\"";

				return tag + " />";
			}
		}
	}
}
