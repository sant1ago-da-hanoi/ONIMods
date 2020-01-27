﻿/*
 * Copyright 2020 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using Newtonsoft.Json;
using PeterHan.PLib;

namespace PeterHan.ThermalTooltips {
	/// <summary>
	/// The options class used for Thermal Tooltips.
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	public sealed class ThermalTooltipsOptions {
		/// <summary>
		/// Whether to display all temperature units.
		/// </summary>
		[Option("Display All Units", "Displays thermal information in Fahrenheit, Celsius, and Kelvin.")]
		[JsonProperty]
		public bool AllUnits { get; set; }

		/// <summary>
		/// Whether to only show tooltips on the thermal overlay.
		/// </summary>
		[Option("Only on Thermal Overlay", "Shows thermal information only when the Temperature Overlay is selected.")]
		[JsonProperty]
		public bool OnlyOnThermalOverlay { get; set; }

		public ThermalTooltipsOptions() {
			AllUnits = false;
			OnlyOnThermalOverlay = true;
		}

		public override string ToString() {
			return "ThermalTooltipsOptions[onlyOverlay={0},allUnits={1}]".F(
				OnlyOnThermalOverlay, AllUnits);
		}
	}
}
