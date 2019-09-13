﻿/*
 * Copyright 2019 Peter Han
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

using PeterHan.PLib;
using System.Collections.Generic;
using UnityEngine;

namespace PeterHan.SweepByType {
	/// <summary>
	/// The hover text shown when filtered sweep is invoked.
	/// </summary>
	sealed class FilteredSweepHover : HoverTextConfiguration {
		public override void UpdateHoverElements(List<KSelectable> selected) {
			var hoverInstance = HoverTextScreen.Instance;
			// Find the active mode
			var drawer = hoverInstance.BeginDrawing();
			var mode = ToolMenu.Instance.toolParameterMenu.GetLastEnabledFilter();
			int cell = Grid.PosToCell(Camera.main.ScreenToWorldPoint(KInputManager.
				GetMousePos()));
			// Draw the tool title
			drawer.BeginShadowBar(false);
			drawer.DrawText(STRINGS.UI.TOOLS.MARKFORSTORAGE.TOOLNAME.text.ToUpper(),
				ToolTitleTextStyle);
			// Draw the instructions
			if (mode == SweepByTypeStrings.TOOL_KEY_DEFAULT)
				ActionName = STRINGS.UI.TOOLS.MARKFORSTORAGE.TOOLACTION;
			else
				ActionName = string.Format(SweepByTypeStrings.TOOLTIP_FILTERED,
					FilteredClearTool.Instance.SelectedItemTag.ProperName());
			DrawInstructions(hoverInstance, drawer);
			drawer.EndShadowBar();
			if (selected != null && Grid.IsValidCell(cell) && Grid.IsVisible(cell))
				DrawPickupText(selected, drawer);
			drawer.EndDrawing();
		}

		/// <summary>
		/// Draws tool tips for selectable items in the sweep area.
		/// </summary>
		/// <param name="selected">The items which were found.</param>
		/// <param name="drawer">The renderer for hover card text.</param>
		private void DrawPickupText(IEnumerable<KSelectable> selected, HoverTextDrawer drawer)
		{
			var hoverInstance = HoverTextScreen.Instance;
			// For each pickupable object, show the type
			foreach (var obj in selected) {
				var cc = obj.GetComponent<Clearable>();
				var ec = obj.GetComponent<PrimaryElement>();
				// Ignore duplicants
				if (cc != null && cc.isClearable && ec != null && obj.GetComponent<
						MinionIdentity>() == null) {
					var element = ec.Element;
					drawer.BeginShadowBar(false);
					// Element name (uppercase)
					drawer.DrawText(obj.GetProperName().ToUpper(), Styles_Title.Standard);
					drawer.NewLine(26);
					drawer.DrawIcon(hoverInstance.GetSprite("dash"), 18);
					// Mass (kg, g, mg...)
					drawer.DrawText(GameUtil.GetFormattedMass(ec.Mass), Styles_BodyText.
						Standard);
					drawer.NewLine(26);
					drawer.DrawIcon(hoverInstance.GetSprite("dash"), 18);
					// Temperature
					drawer.DrawText(GameUtil.GetFormattedTemperature(ec.Temperature),
						Styles_BodyText.Standard);
					drawer.EndShadowBar();
				}
			}
		}
	}
}