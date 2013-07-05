// -----------------------------------------------------------------------
// <copyright file="InputControlType.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// -----------------------------------------------------------------------

namespace SeleniumFramework.Utilities
{
    /// <summary>
    /// This enum contains all input control types used on web forms
    /// </summary>
    public enum InputControlType
    {
        /// <summary>
        /// No Form Control
        /// </summary>
        None,

        /// <summary>
        /// Html Form
        /// </summary>
        Form,

        /// <summary>
        /// Html Textbox
        /// </summary>
        TextBox,

        /// <summary>
        /// Dropdown List
        /// </summary>
        DropDownList,

        /// <summary>
        /// Html Button
        /// </summary>
        Button,

        /// <summary>
        /// Html Checkbox
        /// </summary>
        CheckBox,

        /// <summary>
        /// Html Image Control
        /// </summary>
        Image,

        /// <summary>
        /// Div control
        /// </summary>
        Div
    }
}
