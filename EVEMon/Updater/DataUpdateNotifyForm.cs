using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Serialization.BattleClinic;

namespace EVEMon.Updater
{
    public partial class DataUpdateNotifyForm : EVEMonForm
    {
        private readonly DataUpdateAvailableEventArgs m_args;

        /// <summary>
        /// Default constructor.
        /// </summary>
        private DataUpdateNotifyForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DataUpdateNotifyForm(DataUpdateAvailableEventArgs args)
            : this()
        {
            m_args = args;
        }

        /// <summary>
        /// On load we update the informations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataUpdateNotifyForm_Load(object sender, EventArgs e)
        {
            StringBuilder changedFiles = new StringBuilder();
            StringBuilder notes = new StringBuilder("UPDATE NOTES:\n");
            foreach (SerializableDatafile versionDatafile in m_args.ChangedFiles)
            {
                changedFiles.AppendFormat(CultureConstants.DefaultCulture,
                                          "Filename: {0}\t\tDated: {1}{3}Url: {2}/{0}{3}{3}",
                                          versionDatafile.Name, versionDatafile.Date, versionDatafile.Address, Environment.NewLine);
                notes.AppendLine(versionDatafile.Message).AppendLine();
            }
            tbFiles.Lines = changedFiles.ToString().Split('\n');
            tbNotes.Lines = notes.ToString().Split('\n');
        }

        /// <summary>
        /// Occurs on "update" button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            DialogResult result = DialogResult.Yes;
            int changedFilesCount = m_args.ChangedFiles.Count;

            while (m_args.ChangedFiles.Count != 0 && result == DialogResult.Yes)
            {
                DownloadUpdates();

                if (m_args.ChangedFiles.Count == 0)
                    break;

                // One or more files failed
                string message = String.Format(CultureConstants.DefaultCulture,
                                               "{0} file{1} failed to download, do you wish to try again?",
                                               m_args.ChangedFiles.Count, m_args.ChangedFiles.Count == 1 ? String.Empty : "s");

                result = MessageBox.Show(message, "Failed Download", MessageBoxButtons.YesNo);
            }

            // If no files were updated, abort the update process
            DialogResult = m_args.ChangedFiles.Count == changedFilesCount ? DialogResult.Abort : DialogResult.OK;

            Close();
        }

        /// <summary>
        /// Downloads the updates.
        /// </summary>
        private void DownloadUpdates()
        {
            List<SerializableDatafile> datafiles = new List<SerializableDatafile>();

            // Copy the new datafiles to a new list
            datafiles.AddRange(m_args.ChangedFiles);

            foreach (SerializableDatafile versionDatafile in datafiles)
            {
                // Work out the new names of the files
                string url = String.Format(CultureConstants.DefaultCulture, "{0}/{1}", versionDatafile.Address, versionDatafile.Name);
                string oldFilename = Path.Combine(EveMonClient.EVEMonDataDir, versionDatafile.Name);
                string newFilename = String.Format(CultureConstants.DefaultCulture, "{0}.tmp", oldFilename);

                // If the file already exists delete it
                if (File.Exists(newFilename))
                {
                    try
                    {
                        File.Delete(newFilename);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionHandler.LogException(ex, false);
                    }
                    catch (IOException ex)
                    {
                        ExceptionHandler.LogException(ex, false);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        ExceptionHandler.LogException(ex, false);
                    }
                }

                // Show the download dialog, which will download the file
                using (UpdateDownloadForm form = new UpdateDownloadForm(new Uri(url), newFilename))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                        continue;

                    Datafile downloadedDatafile = new Datafile(Path.GetFileName(newFilename));

                    if (versionDatafile.MD5Sum != null && versionDatafile.MD5Sum != downloadedDatafile.MD5Sum)
                    {
                        try
                        {
                            File.Delete(newFilename);
                        }
                        catch (ArgumentException e)
                        {
                            ExceptionHandler.LogException(e, false);
                        }
                        catch (IOException ex)
                        {
                            ExceptionHandler.LogException(ex, false);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            ExceptionHandler.LogException(ex, false);
                        }
                        continue;
                    }

                    ReplaceDatafile(oldFilename, newFilename);
                    m_args.ChangedFiles.Remove(versionDatafile);
                }
            }
        }

        /// <summary>
        /// Replaces the datafile.
        /// </summary>
        /// <param name="oldFilename">The old filename.</param>
        /// <param name="newFilename">The new filename.</param>
        private static void ReplaceDatafile(string oldFilename, string newFilename)
        {
            try
            {
                File.Delete(String.Format(CultureConstants.DefaultCulture, "{0}.bak", oldFilename));
                File.Copy(oldFilename, String.Format(CultureConstants.DefaultCulture, "{0}.bak", oldFilename));
                File.Delete(oldFilename);
                File.Move(newFilename, oldFilename);
            }
            catch (ArgumentException ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
            catch (IOException ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
            catch (UnauthorizedAccessException ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
        }

        /// <summary>
        /// Occurs on "remind me later" button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLater_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}