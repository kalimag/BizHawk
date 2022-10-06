using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk.ArcadePit
{
	internal class StateMenuManager
	{
		private const int MenuIndex = 2;

		private const int SaveSlotCount = 10;
		private const string SaveSlotFilePrefix = "QuickSave";
		private static readonly Regex SaveSlotFileRegex = new Regex($@"\.{SaveSlotFilePrefix}[0-9]\.State$", RegexOptions.IgnoreCase);
		private static readonly List<string> SaveSlotHotkeyIds = Enumerable.Range(0, SaveSlotCount).Select(i => "Load State " + i).ToList();



		private readonly MainForm _mainForm;
		private readonly SaveSlotManager _saveSlots;
		private readonly Config _config;

		private readonly ToolStripMenuItemEx _menu;
		private readonly ToolStripMenuItemEx _defaultItem;
		private readonly ToolStripSeparatorEx _separator;
		private readonly ToolStripMenuItemEx[] _slotStateItems = new ToolStripMenuItemEx[SaveSlotCount];
		private readonly List<ToolStripMenuItemEx> _namedStateItems = new();



		public StateMenuManager(MainForm mainForm, SaveSlotManager saveSlots, Config config)
		{
			_mainForm = mainForm;
			_saveSlots = saveSlots;
			_config = config;

			_menu = new ToolStripMenuItemEx
			{
				Text = "St&ates",
			};
			_menu.DropDownOpening += OnMenuOpening;
			_mainForm.MainMenuStrip.Items.Insert(Math.Min(MenuIndex, _mainForm.MainMenuStrip.Items.Count), _menu);

			_defaultItem = new ToolStripMenuItemEx
			{
				Text = "",
				Enabled = false,
			};
			_menu.DropDownItems.Add(_defaultItem);

			for (int i = 1; i <= SaveSlotCount; i++)
			{
				int slot = i % SaveSlotCount; // count 1..9,0 to match the vanilla menu
				var slotItem = new ToolStripMenuItemEx
				{
					Text = $"State {slot}",
				};
				slotItem.Click += (s, e) => LoadSlotState(slot);
				_menu.DropDownItems.Add(slotItem);
				_slotStateItems[slot] = slotItem;
			}

			_separator = new ToolStripSeparatorEx();
			_menu.DropDownItems.Add(_separator);
		}

		private void OnMenuOpening(object sender, EventArgs e)
		{
			// second condition is related to TASing, shouldn't be relevant for our purposes
			if (!_mainForm.Emulator.HasSavestates() || _mainForm.Master?.WantsToControlSavestates == true)
			{
				ShowDefaultItem("Savestates unavailable");
				return;
			}

			try
			{
				UpdateSlotItems();
				UpdateNamedItems();
			}
			catch (Exception ex)
			{
				ShowDefaultItem("Error");
				using (var dlg = new ExceptionBox(ex))
					dlg.ShowDialog(_mainForm);
				return;
			}

			var anySlotStates = _slotStateItems.Any(item => item.Available);
			var anyNamedStates = _namedStateItems.Any(item => item.Available);
			if (!anySlotStates && !anyNamedStates)
			{
				ShowDefaultItem("No savestates found");
				return;
			}
			else
			{
				_separator.Available = anySlotStates && anyNamedStates;
				_defaultItem.Available = false;
			}
		}

		private void ShowDefaultItem(string text)
		{
			_defaultItem.Text = text;
			_defaultItem.Available = true;

			foreach (var item in _slotStateItems)
				item.Available = false;

			foreach (var item in _namedStateItems)
				item.Available = false;

			_separator.Available = false;
		}

		private void UpdateSlotItems()
		{
			string prefix = _mainForm.SaveStatePrefix();
			for (int i = 0; i < SaveSlotCount; i++)
			{
				_slotStateItems[i].Available = _saveSlots.HasSlot(_mainForm.Emulator, _mainForm.MovieSession.Movie, i, prefix);
				_slotStateItems[i].ShortcutKeyDisplayString = _config.HotkeyBindings[SaveSlotHotkeyIds[i]];
			}
		}

		private void UpdateNamedItems()
		{
			var states = FindStates().OrderBy(s => s.Label).ToList();

			while (_namedStateItems.Count < states.Count)
			{
				var item = new ToolStripMenuItemEx();
				item.Click += (s, e) => LoadNamedState(item.Tag as string, item.Text);
				_namedStateItems.Add(item);
				_menu.DropDownItems.Add(item);
			}

			for (int i = 0; i < states.Count; i++)
			{
				var state = states[i];
				var item = _namedStateItems[i];
				item.Text = state.Label;
				item.Tag = state.Path;
				item.Available = true;
			}

			// hide excess items
			for (int i = states.Count; i < _namedStateItems.Count; i++)
				_namedStateItems[i].Available = false;
		}

		private IEnumerable<(string Path, string Label)> FindStates()
		{
			// the prefix includes the absolute path to the save state folder, e.g. "[...]\GBA\State\Racing Gears Advance (USA).mGBA"
			string statePrefixFull = _mainForm.SaveStatePrefix();
			string statePrefix = Path.GetFileName(statePrefixFull);
			string stateDirectory = Path.GetDirectoryName(statePrefixFull);

			if (!Directory.Exists(stateDirectory))
				yield break;

			string searchPattern = $"{statePrefix}.*.State";
			Regex removePrefixRegex = new($@"^{Regex.Escape(statePrefix)}\.", RegexOptions.IgnoreCase);

			foreach (var statePath in Directory.EnumerateFiles(stateDirectory, searchPattern, SearchOption.TopDirectoryOnly))
			{
				if (!SaveSlotFileRegex.IsMatch(statePath)) // filter out the numbered states
				{
					var label = removePrefixRegex.Replace(Path.GetFileNameWithoutExtension(statePath), "");
					yield return (statePath, label);
				}
			}
		}

		private void LoadSlotState(int slot)
		{
			_mainForm.LoadQuickSave($"{SaveSlotFilePrefix}{slot}");
		}

		private void LoadNamedState(string path, string label)
		{
			if (string.IsNullOrEmpty(path))
				return;

			_mainForm.LoadState(path, label);
		}

	}
}
