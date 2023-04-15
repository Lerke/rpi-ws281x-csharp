using System.Drawing;
using System.Linq;

namespace rpi_ws281x
{
	/// <summary>
	/// Represents a LED which can be controlled by the WS281x controller
	/// </summary>
	public class LED
	{
		/// <summary>
		/// LED which can be controlled by the WS281x controller
		/// </summary>
		internal LED()
		{
			Color = Color.Empty;
		}

		/// <summary>
		/// Gets or sets the color for the LED
		/// </summary>
		public Color Color { get; set; }

		/// <summary>
		/// Returns the RGB value of the color
		/// </summary>
		internal int RGBValue
		{
			get
			{
				var minimalColourValue = (new[] { Color.R, Color.G, Color.B }).Aggregate(Color.R, (acc, curr) => curr < acc ? curr : acc);
				var c = Color.FromArgb(minimalColourValue, Color.R, Color.G, Color.B);
				return c.ToArgb();
			}
		}
	}
}
