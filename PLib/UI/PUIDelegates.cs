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

using UnityEngine;

namespace PeterHan.PLib.UI {
	/// <summary>
	/// Delegate types used in the UI event system.
	/// </summary>
	public sealed class PUIDelegates {
		/// <summary>
		/// The delegate type invoked when a dialog is closed.
		/// </summary>
		/// <param name="option">The key of the chosen option, or PDialog.DIALOG_CLOSE_KEY if
		/// the dialog was closed with ESC or the X button.</param>
		public delegate void OnDialogClosed(string option);

		/// <summary>
		/// The delegate type invoked when a button is pressed.
		/// </summary>
		/// <param name="source">The source button.</param>
		public delegate void OnButtonPressed(GameObject source);

		/// <summary>
		/// The delegate type invoked when components are converted into Unity game objects.
		/// </summary>
		/// <param name="realized">The realized object.</param>
		public delegate void OnRealize(GameObject realized);
	}
}
