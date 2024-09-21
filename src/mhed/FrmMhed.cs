﻿/**
 * SPDX-FileCopyrightText: 2011-2024 EasyCoding Team
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
*/

using mhed.lib;
using NLog;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mhed.gui
{
    /// <summary>
    /// Main form's class.
    /// </summary>
    public partial class FrmMhed : Form
    {
        /// <summary>
        /// Logger instance for HUDManager class.
        /// </summary>
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Gets or sets instance of CurrentApp class.
        /// </summary>
        private CurrentApp App;

        /// <summary>
        /// Create an instance of the CurrentApp class.
        /// </summary>
        private void InitializeApp()
        {
            // Create a new instance of CurrentApp class...
            App = new CurrentApp(Properties.Settings.Default.IsPortable, Properties.Resources.AppName);
        }

        /// <summary>
        /// Initializes model view on form.
        /// </summary>
        private void InitializeModelView()
        {
            // Disabling auto columns generating...
            HE_ModelView.AutoGenerateColumns = false;

            // Binding to an object...
            HE_ModelView.DataSource = App.HostsFile.Contents;
        }

        /// <summary>
        /// Save program settings.
        /// </summary>
        private void SaveSettings()
        {
            switch (WindowState)
            {
                case FormWindowState.Normal:
                    Properties.Settings.Default.FormLocation = Location;
                    Properties.Settings.Default.FormSize = Size;
                    Properties.Settings.Default.FormMaximized = false;
                    break;
                case FormWindowState.Maximized:
                    Properties.Settings.Default.FormLocation = RestoreBounds.Location;
                    Properties.Settings.Default.FormSize = RestoreBounds.Size;
                    Properties.Settings.Default.FormMaximized = true;
                    break;
                default:
                    Properties.Settings.Default.FormLocation = RestoreBounds.Location;
                    Properties.Settings.Default.FormSize = RestoreBounds.Size;
                    Properties.Settings.Default.FormMaximized = false;
                    break;
            }
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Change the state of the table editor.
        /// </summary>
        /// <param name="NewState">New state boolean.</param>
        private void ChangeTableEditorState(bool NewState)
        {
            HE_ModelView.ReadOnly = !NewState;
            HE_ModelView.AllowUserToAddRows = NewState;
            HE_ModelView.AllowUserToDeleteRows = NewState;
        }

        /// <summary>
        /// Change the state of the main menu items.
        /// </summary>
        /// <param name="NewState">New state boolean.</param>
        private void ChangeMenuItemsState(bool NewState)
        {
            HE_MenuSaveItem.Enabled = NewState;
            HE_MenuImportItem.Enabled = NewState;
            HE_MenuCutItem.Enabled = NewState;
            HE_MenuPasteItem.Enabled = NewState;
            HE_MenuDeleteItem.Enabled = NewState;
            HE_MenuRestoreDefaultsItem.Enabled = NewState;
        }

        /// <summary>
        /// Change the state of the main toolbar items.
        /// </summary>
        /// <param name="NewState">New state boolean.</param>
        private void ChangeToolbarItemsState(bool NewState)
        {
            HE_ToolbarSaveButton.Enabled = NewState;
            HE_ToolbarCutButton.Enabled = NewState;
            HE_ToolbarPasteButton.Enabled = NewState;
            HE_ToolbarDeleteButton.Enabled = NewState;
        }

        /// <summary>
        /// Change the state of the context menu menu items.
        /// </summary>
        /// <param name="NewState">New state boolean.</param>
        private void ChangeContextMenuItemsState(bool NewState)
        {
            HE_ConextMenuCutItem.Enabled = NewState;
            HE_ConextMenuPasteItem.Enabled = NewState;
            HE_ConextMenuDeleteItem.Enabled = NewState;
        }

        /// <summary>
        /// Change status bar application mode indicator.
        /// </summary>
        /// <param name="NewState">New state boolean.</param>
        private void ChangeStatusBarAppMode()
        {
            HE_StatusBarAppMode.Image = Properties.Resources.IconGreenCircle;
            HE_StatusBarAppMode.Text = AppStrings.AHE_AppStatusRO;
        }
        /// <summary>
        /// Change state of some controls, depending on current running
        /// platform or access level.
        /// </summary>
        private void ChangePrvControlState()
        {
            if (!App.IsAdmin)
            {
                ChangeTableEditorState(false);
                ChangeMenuItemsState(false);
                ChangeToolbarItemsState(false);
                ChangeContextMenuItemsState(false);
                ChangeStatusBarAppMode();
            }
        }

        /// <summary>
        /// Check saved form position.
        /// </summary>
        /// <returns>Return True if saved position is above zero.</returns>
        private bool CheckFormPosition()
        {
            return (Properties.Settings.Default.FormLocation.X > 0) && (Properties.Settings.Default.FormLocation.Y > 0);
        }

        /// <summary>
        /// Check if the saved form position matches the current screen's resolution.
        /// </summary>
        /// <returns>Return True if the saved position can be placed on screen.</returns>
        private bool CheckScreenBounds()
        {
            return Screen.FromControl(this).Bounds.Contains(Properties.Settings.Default.FormLocation);
        }

        /// <summary>
        /// Check if the application update check is required.
        /// </summary>
        private bool IsAutoUpdateCheckNeeded()
        {
            return Properties.Settings.Default.AutoUpdateCheck && (DateTime.Now - Properties.Settings.Default.LastUpdateTime).Days >= Properties.Settings.Default.UpdateCheckInterval;
        }

        /// <summary>
        /// Check if the old updates cleanup is required.
        /// </summary>
        private bool IsCleanupNeeded()
        {
            if (!App.Platform.AutoUpdateSupported) { return false; }
            return (DateTime.Now - Properties.Settings.Default.LastCleanupTime).Days >= Properties.Settings.Default.CleanupInterval;
        }

        /// <summary>
        /// Set strings data on the main form.
        /// </summary>
        private void SetAppStrings()
        {
            // Add Hosts file path to the status bar...
            HE_StatusBarHostsLocation.Text = App.HostsFile.FilePath;
        }

        /// <summary>
        /// Set "Current encoding" form controls to "Default" state.
        /// </summary>
        private void SetEncodingControlStateDefault()
        {
            HE_MenuEncodingDefaultItem.Checked = true;
            HE_MenuEncodingUnicodeItem.Checked = false;
        }

        /// <summary>
        /// Set "Current encoding" form controls to "Unicode" state.
        /// </summary>
        private void SetEncodingControlStateUnicode()
        {
            HE_MenuEncodingDefaultItem.Checked = false;
            HE_MenuEncodingUnicodeItem.Checked = true;
        }

        /// <summary>
        /// Set Hosts file encoding based on specified encoding ID.
        /// </summary>
        /// <param name="Unicode">File encoding ID.</param>
        private void SetFileEncoding(bool Unicode)
        {
            App.HostsFile.MultiByteEncoding = Unicode;
            Properties.Settings.Default.MultiByteEncoding = Unicode;
            if (Unicode) { SetEncodingControlStateUnicode(); } else { SetEncodingControlStateDefault(); }
        }

        /// <summary>
        /// Set Hosts file encoding based on saved settings.
        /// </summary>
        private void SetFileEncoding()
        {
            SetFileEncoding(Properties.Settings.Default.MultiByteEncoding);
        }

        /// <summary>
        /// Set form state based on saved settings.
        /// </summary>
        private void SetFormState()
        {
            try
            {
                if (Properties.Settings.Default.FormMaximized)
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExSetFormState);
            }
        }

        /// <summary>
        /// Set form size based on saved settings.
        /// </summary>
        private void SetFormSize()
        {
            try
            {
                if ((Properties.Settings.Default.FormSize.Width > 0) && (Properties.Settings.Default.FormSize.Height > 0))
                {
                    Size = Properties.Settings.Default.FormSize;
                }
                else
                {
                    Logger.Debug(DebugStrings.AppDbgIncorrectFormSize, Properties.Settings.Default.FormSize.Width, Properties.Settings.Default.FormSize.Height);
                }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExSetFormSize);
            }
        }

        /// <summary>
        /// Set form location based on saved settings.
        /// </summary>
        private void SetFormLocation()
        {
            try
            {
                if (CheckFormPosition() && CheckScreenBounds())
                {
                    StartPosition = FormStartPosition.Manual;
                    Location = Properties.Settings.Default.FormLocation;
                }
                else
                {
                    Logger.Debug(DebugStrings.AppDbgIncorrectFormLocation, Properties.Settings.Default.FormLocation.X, Properties.Settings.Default.FormLocation.Y);
                }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExSetFormLocation);
            }
        }

        /// <summary>
        /// Restore form state based on saved settings.
        /// </summary>
        private void RestoreFormState()
        {
            if (Properties.Settings.Default.PreserveFormState)
            {
                SetFormLocation();
                SetFormState();
                SetFormSize();
            }
        }

        /// <summary>
        /// Show specified file in default file manager.
        /// </summary>
        /// <param name="FileName">Fully qualified file name.</param>
        private void HelperShowFile(string FileName)
        {
            try
            {
                App.Platform.OpenExplorer(FileName);
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExOpenShell, FileName);
                MessageBox.Show(AppStrings.AHE_OpenShellError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Load specified file into the selected text editor.
        /// </summary>
        /// <param name="FileName">Fully qualified file name.</param>
        private void HelperTextEditor(string FileName)
        {
            try
            {
                App.Platform.OpenTextEditor(FileName, Properties.Settings.Default.EditorBin);
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExOpenNotepad, FileName);
                MessageBox.Show(AppStrings.AHE_OpenInNotepadError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Load specified URL in default Web browser.
        /// </summary>
        /// <param name="SourceUrl">Source URL.</param>
        private void HelperOpenUrl(string SourceUrl)
        {
            try
            {
                App.Platform.OpenWebPage(SourceUrl);
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExOpenUrl);
                MessageBox.Show(AppStrings.AHE_UrlOpenError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Restart the application with admin user rights.
        /// </summary>
        private void HelperRunAs()
        {
            try
            {
                App.Platform.RestartApplicationAsAdmin();
            }
            catch (PlatformNotSupportedException Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgRestartAsAdminNotImplemented);
                MessageBox.Show(AppStrings.AHE_RestartAsAdminNotImplemented, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception Ex)
            {
                Logger.Error(Ex, DebugStrings.AppDbgRestartAsAdminError);
                MessageBox.Show(AppStrings.AHE_RestartAsAdminError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Copy the contents of the selected cells to the clipboard
        /// using built-in method.
        /// </summary>
        private void HelperCopySelectedCells()
        {
            if (HE_ModelView.SelectedCells.Count > 0)
            {
                Clipboard.SetDataObject(HE_ModelView.GetClipboardContent());
            }
        }

        /// <summary>
        /// Clear the contents of the selected cells.
        /// </summary>
        private void HelperClearSelectedCells()
        {
            foreach (DataGridViewCell Cell in HE_ModelView.SelectedCells)
            {
                if (!Cell.OwningRow.IsNewRow)
                {
                    Cell.Value = null;
                }
            }
        }

        /// <summary>
        /// Cut the contents of the selected cells to the clipboard.
        /// </summary>
        private void HelperCut()
        {
            try
            {
                HelperCopySelectedCells();
                HelperClearSelectedCells();
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExClipboardCut);
                MessageBox.Show(AppStrings.AHE_ClipboardCutError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Copy the contents of the selected cells to the clipboard.
        /// </summary>
        private void HelperCopy()
        {
            try
            {
                HelperCopySelectedCells();
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExClipboardCopy);
                MessageBox.Show(AppStrings.AHE_ClipboardCopyError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Paste the IP-address from the clipboard into the selected cell.
        /// </summary>
        private void HelperPasteIPAddress()
        {
            if (IPAddress.TryParse(Clipboard.GetText(), out IPAddress IP))
            {
                HE_ModelView.Rows[HE_ModelView.CurrentRow.Index].Cells[HE_ModelView.CurrentCell.ColumnIndex].Value = IP;
            }
            else
            {
                MessageBox.Show(AppStrings.AHE_ClipboardNonIPAddress, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Paste the hostname from the clipboard into the selected cell.
        /// </summary>
        private void HelperPasteHostname()
        {
            if (Hostname.TryParse(Clipboard.GetText(), out Hostname Host))
            {
                HE_ModelView.Rows[HE_ModelView.CurrentRow.Index].Cells[HE_ModelView.CurrentCell.ColumnIndex].Value = Host;
            }
            else
            {
                MessageBox.Show(AppStrings.AHE_ClipboardNonHostname, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Paste the comment from the clipboard into the selected cell.
        /// </summary>
        private void HelperPasteComment()
        {
            string Comment = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(Comment))
            {
                HE_ModelView.Rows[HE_ModelView.CurrentRow.Index].Cells[HE_ModelView.CurrentCell.ColumnIndex].Value = Comment;
            }
            else
            {
                MessageBox.Show(AppStrings.AHE_ClipboardNonComment, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Paste the contents of the clipboard into the selected cell.
        /// </summary>
        private void HelperPasteSingle()
        {
            if (!HE_ModelView.Rows[HE_ModelView.CurrentRow.Index].IsNewRow && Clipboard.ContainsText())
            {
                switch (HE_ModelView.CurrentCell.ColumnIndex)
                {
                    case 0:
                        HelperPasteIPAddress();
                        break;
                    case 1:
                        HelperPasteHostname();
                        break;
                    case 2:
                        HelperPasteComment();
                        break;
                    default:
                        Logger.Warn(DebugStrings.AppDbgModelViewColumnIndexOutOfRange);
                        break;
                }
            }
        }

        /// <summary>
        /// Paste the contents of the clipboard into the selected cells.
        /// Internal implementation.
        /// </summary>
        /// <returns>Returns True if all content were successfully pasted.</returns>
        private bool HelperPasteMultipleInternal()
        {
            bool Result = true;
            string[] ClipboardEntries = Clipboard.GetText().Split('\n');
            for (int i = 0; i < ClipboardEntries.Length; i++)
            {
                string[] Entry = ClipboardEntries[i].Split('\t');
                if (Entry.Length > 2 && IPAddress.TryParse(Entry[0], out IPAddress IP) && Hostname.TryParse(Entry[1], out Hostname Host))
                {
                    if ((i < HE_ModelView.SelectedRows.Count) && !HE_ModelView.SelectedRows[i].IsNewRow)
                    {
                        HE_ModelView.SelectedRows[i].Cells[0].Value = IP;
                        HE_ModelView.SelectedRows[i].Cells[1].Value = Host;
                        HE_ModelView.SelectedRows[i].Cells[2].Value = Entry[2];
                    }
                    else
                    {
                        HE_ModelView.CancelEdit();
                        App.HostsFile.AddEntry(IP, Host, Entry[2]);
                    }
                }
                else
                {
                    Result &= false;
                }
            }
            return Result;
        }

        /// <summary>
        /// Paste the contents of the clipboard into the selected cells.
        /// </summary>
        private void HelperPasteMultiple()
        {
            if (!HelperPasteMultipleInternal())
            {
                MessageBox.Show(AppStrings.AHE_ClipboardFormatError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Paste the contents from the clipboard.
        /// </summary>
        private void HelperPaste()
        {
            try
            {
                if (HE_ModelView.SelectedCells.Count == 1) { HelperPasteSingle(); } else { HelperPasteMultiple(); }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExClipboardPaste);
                MessageBox.Show(AppStrings.AHE_ClipboardPasteError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Delete selected rows.
        /// </summary>
        private void HelperDelete()
        {
            try
            {
                foreach (DataGridViewCell Cell in HE_ModelView.SelectedCells)
                {
                    if (Cell.RowIndex != -1 && !Cell.OwningRow.IsNewRow)
                    {
                        HE_ModelView.Rows.RemoveAt(Cell.RowIndex);
                    }
                }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExDeleteRow);
                MessageBox.Show(AppStrings.AHE_DeleteRowError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Show offline help with specified page name to display.
        /// </summary>
        /// <param name="PageName">Page name to display.</param>
        private void HelperShowHelp(string PageName)
        {
            try
            {
                string CHMFile = Path.Combine(App.FullAppPath, Properties.Resources.AppHelpDirectory, string.Format(Properties.Resources.AppHelpFileName, AppStrings.AHE_LangPrefix));
                if (File.Exists(CHMFile))
                {
                    if (string.IsNullOrEmpty(PageName)) { Help.ShowHelp(this, CHMFile); } else { Help.ShowHelp(this, CHMFile, HelpNavigator.Topic, PageName); }
                }
                else
                {
                    MessageBox.Show(AppStrings.AHE_ChmFileNotFound, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExHelpShow);
                MessageBox.Show(AppStrings.AHE_ShowHelpError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Show offline help.
        /// </summary>
        private void HelperShowHelp()
        {
            HelperShowHelp(string.Empty);
        }

        /// <summary>
        /// Show debug log.
        /// </summary>
        private void HelperShowLog()
        {
            try
            {
                if (File.Exists(App.AppLogFile))
                {
                    if (ModifierKeys == Keys.Shift) { HelperShowFile(App.AppLogFile); } else { HelperTextEditor(App.AppLogFile); }
                }
                else
                {
                    MessageBox.Show(AppStrings.AHE_LogFileNotFound, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExShowLogFile);
                MessageBox.Show(AppStrings.AHE_ShowLogFileError, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// FrmMhed class constructor.
        /// </summary>
        public FrmMhed()
        {
            InitializeComponent();
        }

        /// <summary>
        /// "Form create" event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void FrmMhed_Load(object sender, EventArgs e)
        {
            RestoreFormState();
            InitializeApp();
            InitializeModelView();
            ChangePrvControlState();
            SetAppStrings();
            SetFileEncoding();
            await LoadHostsFile();
            await CheckForUpdates();
            await CleanOldUpdates();
        }

        /// <summary>
        /// "Form close" event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void FrmMhed_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = Properties.Settings.Default.ConfirmExit && MessageBox.Show(string.Format(AppStrings.AHE_ExitConfirmation, Properties.Resources.AppName), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes;
            }
        }

        /// <summary>
        /// "Form closed" event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void FrmMhed_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();
        }

        /// <summary>
        /// Save Hosts file changes to disk.
        /// </summary>
        private async Task SaveToFile()
        {
            if (App.IsAdmin)
            {
                try
                {
                    await App.HostsFile.Save();
                    MessageBox.Show(AppStrings.AHE_Saved, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception Ex)
                {
                    Logger.Error(Ex, DebugStrings.AppDbgExSaveTask);
                    MessageBox.Show(string.Format(AppStrings.AHE_SaveException, App.HostsFile.FilePath), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(AppStrings.AHE_NoAdminRights, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Export Hosts file entries to a separate file.
        /// </summary>
        private async Task ExportToFile()
        {
            if (HE_ExportDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    await App.HostsFile.Save(HE_ExportDialog.FileName);
                    MessageBox.Show(AppStrings.AHE_Exported, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception Ex)
                {
                    Logger.Error(Ex, DebugStrings.AppDbgExExportTask);
                    MessageBox.Show(AppStrings.AHE_ExportException, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Try to read Hosts file.
        /// </summary>
        private async Task LoadHostsFile()
        {
            try
            {
                await App.HostsFile.Load();
            }
            catch (FileNotFoundException Ex)
            {
                Logger.Error(Ex, DebugStrings.AppDbgHostsFileDoesNotExists);
                MessageBox.Show(string.Format(AppStrings.AHE_NoFileDetected, App.HostsFile.FilePath), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                App.Platform.Exit(ReturnCodes.HostsFileDoesNotExists);
            }
            catch (Exception Ex)
            {
                Logger.Error(Ex, DebugStrings.AppDbgExHostsLoadParse);
                MessageBox.Show(string.Format(AppStrings.AHE_ExceptionDetected, App.HostsFile.FilePath, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
        }

        /// <summary>
        /// Try to import Hosts file entries from file.
        /// </summary>
        private async Task ImportFromFile()
        {
            if (App.IsAdmin)
            {
                if (HE_ImportDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        await App.HostsFile.Load(HE_ImportDialog.FileName);
                        MessageBox.Show(AppStrings.AHE_Imported, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (FileNotFoundException Ex)
                    {
                        Logger.Error(Ex, DebugStrings.AppDbgImportFileDoesNotExists);
                        MessageBox.Show(AppStrings.AHE_ImportFileNotFound, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception Ex)
                    {
                        Logger.Error(Ex, DebugStrings.AppDbgExImportTask);
                        MessageBox.Show(AppStrings.AHE_ImportFileException, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(AppStrings.AHE_NoAdminRights, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Reload Hosts file contents from disk.
        /// </summary>
        private async Task ReloadHostsFile()
        {
            try
            {
                await App.HostsFile.Refresh();
            }
            catch (Exception Ex)
            {
                Logger.Error(Ex, DebugStrings.AppDbgExHostsLoadParse);
                MessageBox.Show(string.Format(AppStrings.AHE_ExceptionDetected, App.HostsFile.FilePath), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Check for the application updates in a separate thread.
        /// </summary>
        /// <param name="UA">User-Agent header for outgoing HTTP queries.</param>
        /// <returns>Returns True if the updates were found.</returns>
        private async Task<bool> IsUpdatesAvailable(string UA)
        {
            UpdateManager Updater = await UpdateManager.Create(UA);
            return Updater.CheckAppUpdate();
        }

        /// <summary>
        /// Launch an update checker in a separate thread, waits for the
        /// result and returns a message if found.
        /// </summary>
        private async Task CheckForUpdates()
        {
            if (IsAutoUpdateCheckNeeded())
            {
                try
                {
                    if (await IsUpdatesAvailable(App.UserAgent))
                    {
                        MessageBox.Show(string.Format(AppStrings.AHE_NewVersionAvailable, Properties.Resources.AppName), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    Properties.Settings.Default.LastUpdateTime = DateTime.Now;
                }
                catch (Exception Ex)
                {
                    Logger.Warn(Ex, DebugStrings.AppDbgExBgaChk);
                }
            }
        }

        /// <summary>
        /// Find and delete old application update files in a separate
        /// thread.
        /// </summary>
        private async Task CleanOldUpdates()
        {
            try
            {
                if (IsCleanupNeeded())
                {
                    await Task.Run(() => { if (Directory.Exists(App.AppUpdateDir)) { Directory.Delete(App.AppUpdateDir, true); } });
                    Properties.Settings.Default.LastCleanupTime = DateTime.Now;
                }
            }
            catch (Exception Ex)
            {
                Logger.Warn(Ex, DebugStrings.AppDbgExClnOldUpdates);
            }
        }

        /// <summary>
        /// "Data error" event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ModelView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Context.HasFlag(DataGridViewDataErrorContexts.Commit))
            {
                switch (e.ColumnIndex)
                {
                    case 0:
                        MessageBox.Show(AppStrings.AHE_IncorrectIPAddress, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    case 1:
                        MessageBox.Show(AppStrings.AHE_IncorrectHostname, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    case 2:
                        MessageBox.Show(AppStrings.AHE_IncorrectComment, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    default:
                        Logger.Warn(DebugStrings.AppDbgModelViewColumnIndexOutOfRange);
                        break;
                }
            }
            else
            {
                Logger.Warn(e.Exception, DebugStrings.AppDbgExModelView);
            }
        }

        /// <summary>
        /// "Refresh" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void HE_MenuRefreshItem_Click(object sender, EventArgs e)
        {
            await ReloadHostsFile();
        }

        /// <summary>
        /// "Save" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void HE_MenuSaveItem_Click(object sender, EventArgs e)
        {
            await SaveToFile();
        }

        /// <summary>
        /// "Import" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void HE_MenuImportItem_Click(object sender, EventArgs e)
        {
            await ImportFromFile();
        }

        /// <summary>
        /// "Export" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void HE_MenuExportItem_Click(object sender, EventArgs e)
        {
            await ExportToFile();
        }

        /// <summary>
        /// "Options" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuOptionsItem_Click(object sender, EventArgs e)
        {
            GuiHelpers.FormShowOptions();
        }

        /// <summary>
        /// "Quit" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuQuitItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// "Cut" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuCutItem_Click(object sender, EventArgs e)
        {
            HelperCut();
        }

        /// <summary>
        /// "Copy" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuCopyItem_Click(object sender, EventArgs e)
        {
            HelperCopy();
        }

        /// <summary>
        /// "Paste" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuPasteItem_Click(object sender, EventArgs e)
        {
            HelperPaste();
        }

        /// <summary>
        /// "Delete" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuDeleteItem_Click(object sender, EventArgs e)
        {
            HelperDelete();
        }

        /// <summary>
        /// "Restore defaults" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void HE_MenuRestoreDefaultsItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(AppStrings.AHE_RestDef, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                App.HostsFile.Restore();
                await SaveToFile();
            }
        }

        /// <summary>
        /// "Open in Notepad" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuOpenNotepadItem_Click(object sender, EventArgs e)
        {
            HelperTextEditor(App.HostsFile.FilePath);
        }

        /// <summary>
        /// "Default encoding" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuEncodingDefaultItem_Click(object sender, EventArgs e)
        {
            SetFileEncoding(false);
        }

        /// <summary>
        /// "Unicode encoding" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuEncodingUnicodeItem_Click(object sender, EventArgs e)
        {
            SetFileEncoding(true);
        }

        /// <summary>
        /// "Show help" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuShowHelpItem_Click(object sender, EventArgs e)
        {
            HelperShowHelp();
        }

        /// <summary>
        /// "Privacy policy" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuShowPrivacyPolicyItem_Click(object sender, EventArgs e)
        {
            HelperShowHelp(Properties.Resources.AppPrivacyPolicyPageName);
        }

        /// <summary>
        /// "Check for updates" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuCheckForUpdatesItem_Click(object sender, EventArgs e)
        {
            GuiHelpers.FormShowUpdater(App.UserAgent, App.FullAppPath, App.AppUpdateDir);
        }

        /// <summary>
        /// "Report bug" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuReportItem_Click(object sender, EventArgs e)
        {
            HelperOpenUrl(Properties.Resources.AppBtURL);
        }

        /// <summary>
        /// "Show debug logs" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuDebugLogItem_Click(object sender, EventArgs e)
        {
            HelperShowLog();
        }

        /// <summary>
        /// "About" menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_MenuAboutItem_Click(object sender, EventArgs e)
        {
            GuiHelpers.FormShowAboutApp();
        }

        /// <summary>
        /// "Refresh" toolbar button event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void HE_ToolbarRefreshButton_Click(object sender, EventArgs e)
        {
            await ReloadHostsFile();
        }

        /// <summary>
        /// "Save" toolbar button event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void HE_ToolbarSaveButton_Click(object sender, EventArgs e)
        {
            await SaveToFile();
        }

        /// <summary>
        /// "Cut" toolbar button event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ToolbarCutButton_Click(object sender, EventArgs e)
        {
            HelperCut();
        }

        /// <summary>
        /// "Copy" toolbar button event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ToolbarCopyButton_Click(object sender, EventArgs e)
        {
            HelperCopy();
        }

        /// <summary>
        /// "Paste" toolbar button event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ToolbarPasteButton_Click(object sender, EventArgs e)
        {
            HelperPaste();
        }

        /// <summary>
        /// "Delete row" toolbar button event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ToolbarDeleteButton_Click(object sender, EventArgs e)
        {
            HelperDelete();
        }

        /// <summary>
        /// "Cut" context menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ConextMenuCutItem_Click(object sender, EventArgs e)
        {
            HelperCut();
        }

        /// <summary>
        /// "Copy" context menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ConextMenuCopyItem_Click(object sender, EventArgs e)
        {
            HelperCopy();
        }

        /// <summary>
        /// "Paste" context menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ConextMenuPasteItem_Click(object sender, EventArgs e)
        {
            HelperPaste();
        }

        /// <summary>
        /// "Delete" context menu item event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_ConextMenuDeleteItem_Click(object sender, EventArgs e)
        {
            HelperDelete();
        }

        /// <summary>
        /// "Mouse enter location" status bar event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_StatusBarHostsLocation_MouseEnter(object sender, EventArgs e)
        {
            HE_StatusBarHostsLocation.ForeColor = Color.Red;
        }

        /// <summary>
        /// "Mouse leave location" status bar event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_StatusBarHostsLocation_MouseLeave(object sender, EventArgs e)
        {
            HE_StatusBarHostsLocation.ForeColor = Color.Black;
        }

        /// <summary>
        /// "Status bar location click" event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_StatusBarHostsLocation_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(string.Format(AppStrings.AHE_HMessg, App.HostsFile.FilePath), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                HelperShowFile(App.HostsFile.FilePath);
            }
        }

        /// <summary>
        /// "Status bar application mode click" event handler.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void HE_StatusBarAppMode_Click(object sender, EventArgs e)
        {
            if (!App.IsAdmin)
            {
                if (MessageBox.Show(AppStrings.AHE_RestartAsAdminQuestion, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    HelperRunAs();
                }
            }
            else
            {
                MessageBox.Show(AppStrings.AHE_RestartAsAdminRunning, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
