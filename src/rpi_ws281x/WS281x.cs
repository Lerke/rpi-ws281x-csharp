﻿using Native;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace rpi_ws281x
{
	/// <summary>
	/// Wrapper class to controll WS281x LEDs
	/// </summary>
	public class WS281x : IDisposable
	{
		private ws2811_t _ws2811;
		private GCHandle _ws2811Handle;
		private bool _isDisposingAllowed;

		/// <summary>
		/// Initialize the wrapper
		/// </summary>
		/// <param name="settings">Settings used for initialization</param>
		public WS281x(Settings settings)
		{
			_ws2811 = new ws2811_t();
			//Pin the object in memory. Otherwies GC will probably move the object to another memory location.
			//This would cause errors because the native library has a pointer on the memory location of the object.
			_ws2811Handle = GCHandle.Alloc(_ws2811, GCHandleType.Pinned);

			_ws2811.dmanum	= settings.DMAChannel;
			_ws2811.freq	= settings.Frequency;

			_ws2811.channel_0 = InitChannel(0, settings);
			_ws2811.channel_1 = InitChannel(1, settings);

			if (settings.GammaCorrection != null)
			{
				if (settings.Channels.ContainsKey(0))
					Marshal.Copy(Settings.GammaCorrection.ToArray(), 0, _ws2811.channel_0.gamma, Settings.GammaCorrection.Count);
				if (settings.Channels.ContainsKey(1))
					Marshal.Copy(Settings.GammaCorrection.ToArray(), 0, _ws2811.channel_1.gamma, Settings.GammaCorrection.Count);
			}

			Settings = settings;

			var initResult = PInvoke.ws2811_init(ref _ws2811);
			if (initResult != ws2811_return_t.WS2811_SUCCESS)
			{
				var returnMessage = GetMessageForStatusCode(initResult);
				throw new Exception(String.Format("Error while initializing.{0}Error code: {1}{0}Message: {2}", Environment.NewLine, initResult.ToString(), returnMessage));
			}	

			//Disposing is only allowed if the init was successfull.
			//Otherwise the native cleanup function throws an error.
			_isDisposingAllowed = true;
		}

		/// <summary>
		/// Renders the content of the channels
		/// </summary>
		public void Render()
		{
			if (Settings.Channels.ContainsKey(0))
			{
				var ledColor = Settings.Channels[0].LEDs.Select(x => x.RGBValue).ToArray();
				Marshal.Copy(ledColor, 0, _ws2811.channel_0.leds, ledColor.Length);
			}
			if (Settings.Channels.ContainsKey(1))
			{
				var ledColor = Settings.Channels[1].LEDs.Select(x => x.RGBValue).ToArray();
				Marshal.Copy(ledColor, 0, _ws2811.channel_1.leds, ledColor.Length);
			}
			
			var result = PInvoke.ws2811_render(ref _ws2811);
			if (result != ws2811_return_t.WS2811_SUCCESS)
			{
				var returnMessage = GetMessageForStatusCode(result);
				throw new Exception(String.Format("Error while rendering.{0}Error code: {1}{0}Message: {2}", Environment.NewLine, result.ToString(), returnMessage));
			}
		}

		/// <summary>
		/// Sets the color of a given LED
		/// </summary>
		/// <param name="channelIndex">Channel which controls the LED</param>
		/// <param name="ledID">ID/Index of the LED</param>
		/// <param name="color">New color</param>
		public void SetLEDColor(int channelIndex, int ledID, Color color)
		{
			Settings.Channels[channelIndex].LEDs[ledID].Color = color;
		}

		/// <summary>
		/// Clear all LEDs
		/// </summary>
		public void Reset()
		{
			foreach (var channel in Settings.Channels)
			{
				foreach (var led in channel.Value.LEDs)
				{
					led.Color = Color.Black;	// Black == OFF
				}
			}
			Render();
		}

		/// <summary>
		/// Returns the settings which are used to initialize the component
		/// </summary>
		public Settings Settings { get; private set; }

		/// <summary>
		/// Initialize the channel propierties
		/// </summary>
		/// <param name="channelIndex">Index of the channel tu initialize</param>
		/// <param name="settings">Controller Settings</param>
		private ws2811_channel_t InitChannel(int channelIndex, Settings settings)
		{
			ws2811_channel_t channel = new ws2811_channel_t();

			if (settings.Channels.ContainsKey(channelIndex))
			{
				channel.count		= settings.Channels[channelIndex].LEDs.Count;
				channel.gpionum		= settings.Channels[channelIndex].GPIOPin;
				channel.brightness	= settings.Channels[channelIndex].Brightness;
				channel.invert		= Convert.ToInt32(settings.Channels[channelIndex].Invert);

				if (settings.Channels[channelIndex].StripType != StripType.Unknown)
				{
					//Strip type is set by the native assembly if not explicitly set.
					//This type defines the ordering of the colors e. g. RGB or GRB, ...
					channel.strip_type = (int)settings.Channels[channelIndex].StripType;
				}
			}
			return channel;
		}

		/// <summary>
		/// Returns the error message for the given status code
		/// </summary>
		/// <param name="statusCode">Status code to resolve</param>
		/// <returns></returns>
		private string GetMessageForStatusCode(ws2811_return_t statusCode)
		{
			var strPointer = PInvoke.ws2811_get_return_t_str((int)statusCode);
			return Marshal.PtrToStringAuto(strPointer);
		}

	#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				if(_isDisposingAllowed)
				{
					PInvoke.ws2811_fini(ref _ws2811);
					_ws2811Handle.Free();
										
					_isDisposingAllowed = false;
				}
				
				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~WS281x()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
	#endregion
	}
}
