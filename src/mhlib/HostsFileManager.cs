﻿/**
 * SPDX-FileCopyrightText: 2011-2025 EasyCoding Team
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
*/

using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace mhed.lib
{
    /// <summary>
    /// Class for working with Hosts file.
    /// </summary>
    public sealed class HostsFileManager
    {
        /// <summary>
        /// Store information about current running platform.
        /// </summary>
        private readonly CurrentPlatform Platform;

        /// <summary>
        /// Store Hosts file encoding.
        /// </summary>
        private Encoding FileEncoding;

        /// <summary>
        /// Get or set full Hosts file path.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Get or set Hosts file contents.
        /// </summary>
        public SortableBindingList<HostsFileEntry> Contents { get; private set; }

        /// <summary>
        /// Get or set Hosts file encoding between the current platform default
        /// and Unicode.
        /// </summary>
        public bool MultiByteEncoding
        {
            get => FileEncoding is UTF8Encoding;
            set => FileEncoding = value ? new UTF8Encoding(Platform.HostsFileBOM) : Encoding.Default;
        }

        /// <summary>
        /// Get user-friendly Hosts file encoding name.
        /// </summary>
        public string EncodingName => FileEncoding.EncodingName;

        /// <summary>
        /// Add a new entry.
        /// </summary>
        /// <param name="IP">IP address.</param>
        /// <param name="Host">Associated hostname.</param>
        /// <param name="Comm">Entry comment.</param>
        public void AddEntry(IPAddress IP, Hostname Host, string Comm)
        {
            Contents.Add(new HostsFileEntry(IP, Host, Comm));
        }

        /// <summary>
        /// Clear Hosts file data object.
        /// </summary>
        private void ClearHostsContents()
        {
            Contents.Clear();
        }

        /// <summary>
        /// Add localhost entry for other than Windows operating systems.
        /// Windows since NT 6.0 (Vista) don't need it.
        /// </summary>
        private void AddLocalHostEntry()
        {
            if (Platform.LocalHostEntry)
            {
                Contents.Add(new HostsFileEntry(IPAddress.Loopback, Hostname.LocalHost, string.Empty));
                Contents.Add(new HostsFileEntry(IPAddress.IPv6Loopback, Hostname.LocalHost, string.Empty));
            }
        }

        /// <summary>
        /// Asynchronically read contents of the specified Hosts file to
        /// the data object.
        /// </summary>
        /// <param name="SourceFile">Hosts source file path.</param>
        private async Task ReadHostsFile(string SourceFile)
        {
            using (StreamReader OpenedHosts = new StreamReader(SourceFile, FileEncoding))
            {
                while (OpenedHosts.Peek() >= 0)
                {
                    string ImpStr = StringsManager.CleanString(await OpenedHosts.ReadLineAsync());
                    if (HostsFileEntryParser.TryParse(ImpStr, out HostsFileEntryParser Parser) && IPAddress.TryParse(Parser.IP, out IPAddress IP) && Hostname.TryParse(Parser.Host, out Hostname Host))
                    {
                        Contents.Add(new HostsFileEntry(IP, Host, Parser.Comment));
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronically write contents of the data object to specified Hosts
        /// file.
        /// </summary>
        /// <param name="SourceFile">Hosts source file path.</param>
        /// <param name="SkipHeader">Skip header if it is required on this platform.</param>
        private async Task WriteHostsFile(string SourceFile, bool SkipHeader)
        {
            using (StreamWriter CFile = new StreamWriter(SourceFile, false, FileEncoding))
            {
                if (Platform.HostsFileHeader && !SkipHeader)
                {
                    await CFile.WriteLineAsync(Properties.Resources.HtTemplate);
                }

                foreach (HostsFileEntry Entry in Contents.Where(e => e.IsValid))
                {
                    if (string.IsNullOrWhiteSpace(Entry.Comment))
                    {
                        await CFile.WriteLineAsync(string.Format(Properties.Resources.NewEntrySingle, Entry.IPAddr, Entry.Hostname));
                    }
                    else
                    {
                        await CFile.WriteLineAsync(string.Format(Properties.Resources.NewEntryWithComment, Entry.IPAddr, Entry.Hostname, Entry.Comment));
                    }
                }
            }
        }

        /// <summary>
        /// Read Hosts file from disk.
        /// </summary>
        public async Task Load()
        {
            await ReadHostsFile(FilePath);
        }

        /// <summary>
        /// Read Hosts entries from the specified source file.
        /// </summary>
        /// <param name="SourceFile">Hosts source file path.</param>
        public async Task Load(string SourceFile)
        {
            await ReadHostsFile(SourceFile);
        }

        /// <summary>
        /// Re-read (refresh) Hosts file from disk.
        /// </summary>
        public async Task Refresh()
        {
            ClearHostsContents();
            await ReadHostsFile(FilePath);
        }

        /// <summary>
        /// Restore default contents of the Hosts file.
        /// </summary>
        public void Restore()
        {
            ClearHostsContents();
            AddLocalHostEntry();
        }

        /// <summary>
        /// Write Hosts file changes to disk.
        /// </summary>
        public async Task Save()
        {
            await WriteHostsFile(FilePath, false);
        }

        /// <summary>
        /// Write Hosts entries to the specified source file.
        /// </summary>
        /// <param name="SourceFile">Hosts source file path.</param>
        public async Task Save(string SourceFile)
        {
            await WriteHostsFile(SourceFile, true);
        }

        /// <summary>
        /// HostsFileManager class constructor.
        /// </summary>
        /// <param name="RunningPlatform">Current running platform.</param>
        public HostsFileManager(CurrentPlatform RunningPlatform)
        {
            Platform = RunningPlatform;
            FilePath = Platform.HostsFileFullPath;
            FileEncoding = Encoding.Default;
            Contents = new SortableBindingList<HostsFileEntry>();
        }
    }
}
