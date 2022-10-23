using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	// Keep ArcadePit related MainForm changes consolidated here, because merging WinForms designer code is a nightmare
	public partial class MainForm
	{

		private void InitializeArcadePit()
		{
			InitializeSaveRegularStatesMenu();
		}

		private void InitializeSaveRegularStatesMenu()
		{
			var menuItem = new ToolStripMenuItemEx
			{
				Text = "Save Regular States"
			};
			menuItem.Click += (s, e) => Config.Savestates.RegularStatesForMovies ^= true;
			MovieSubMenu.DropDownItems.Insert(MovieSubMenu.DropDownItems.IndexOf(AutomaticallyBackupMoviesMenuItem) + 1, menuItem);
			MovieSubMenu.DropDownOpened += (s, e) => menuItem.Checked = Config.Savestates.RegularStatesForMovies;
		}

	}
}
