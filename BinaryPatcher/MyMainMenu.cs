using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Leayal.IO;
using Microsoft.VisualBasic;

namespace BinaryPatcher
{
    public partial class MyMainMenu : Form
    {
        private BackgroundWorker worker;
        private static readonly char[] Spacing = { ' ', ControlChars.Tab };
        private SynchronizationContext synccontext;

        public MyMainMenu()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.synccontext = SynchronizationContext.Current;
            this.Icon = Properties.Resources.icon;
            this.worker = new BackgroundWorker();
            this.worker.DoWork += this.Worker_DoWork;
            this.worker.RunWorkerCompleted += this.Worker_RunWorkerCompleted;
            this.worker.WorkerSupportsCancellation = true;
            this.worker.WorkerReportsProgress = false;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.radioButton_hex.Checked = true;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UnauthorizedAccessException ex = e.Error as UnauthorizedAccessException;
                if (ex != null && ((ex.Message.IndexOf("access to the path", StringComparison.OrdinalIgnoreCase) > -1) && (ex.Message.IndexOf("is denied", ex.Message.Length - 10, StringComparison.OrdinalIgnoreCase) > -1)))
                    MessageBox.Show(this, $"{this.Text} cannot access to the file. Try to run the binary as Admin instead.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                    MessageBox.Show(this, e.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Cancelled)
                MessageBox.Show(this, "User cancelled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else
            {
                ReplaceResult result = e.Result as ReplaceResult;
                if (result != null)
                {
                    if (result.Offset.Count > 0)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        if (result.Offset.Count == 1)
                        {
                            sb.AppendLine("The file has been patched. Found 1 result.");
                            sb.AppendFormat("The patched hex is at this offset: {0}", result.Offset[0]);
                        }
                        else
                        {
                            sb.AppendLine($"The file has been patched. Found {result.Offset.Count} results.");
                            sb.AppendLine("The patched hex are at these offset:");
                            for (int i = 0; i < result.Offset.Count; i++)
                                if (i == 0)
                                    sb.Append(result.Offset[i].ToString());
                                else
                                    sb.AppendFormat(", {0}", result.Offset[i].ToString());
                        }
                        MessageBox.Show(this, sb.ToString(), "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                        MessageBox.Show(this, "The file has been failed to be patched.\nFound no hex that match with the given search for hex.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "The operation has been failed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            this.synccontext?.Post(new SendOrPostCallback(delegate
            {
                this.button_Start.Text = "Start";
                this.panel1.Visible = true;
            }), null);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.synccontext?.Post(new SendOrPostCallback(delegate
            {
                this.button_Start.Text = "Stop";
                this.panel1.Visible = false;
            }), null);
            TaskModel model = e.Argument as TaskModel;
            if (model != null)
            {
                using (BinaryScanner scanner = this.Create(model.Filepath))
                {
                    IEnumerable<BinaryScanResult> walker;
                    byte[] replacingbytes;
                    string[] strs;
                    switch (model.Type)
                    {
                        case Type.Byte:
                            bool qualified = false;
                            for (int i = 0; i < Spacing.Length; i++)
                                if (model.ReplaceWith.IndexOf(Spacing[i]) > -1)
                                {
                                    qualified = true;
                                    break;
                                }
                            if (!qualified)
                                throw new ArgumentException("The replace with should has space between each byte.");

                            strs = model.ReplaceWith.Split(Spacing, StringSplitOptions.RemoveEmptyEntries);
                            byte[] bytes = new byte[strs.Length];
                            for (int i = 0; i < bytes.Length; i++)
                                bytes[i] = byte.Parse(strs[i]);

                            qualified = false;
                            for (int i = 0; i < Spacing.Length; i++)
                                if (model.ReplaceWith.IndexOf(Spacing[i]) > -1)
                                {
                                    qualified = true;
                                    break;
                                }
                            if (!qualified)
                                throw new ArgumentException("The replace with should has space between each byte.");

                            strs = model.ReplaceWith.Split(Spacing, StringSplitOptions.RemoveEmptyEntries);
                            replacingbytes = new byte[strs.Length];
                            for (int i = 0; i < replacingbytes.Length; i++)
                                replacingbytes[i] = byte.Parse(strs[i]);
                            walker = scanner.Scan(bytes);
                            break;
                        default:
                            replacingbytes = Leayal.ByteHelper.FromHexString(model.ReplaceWith);
                            walker = scanner.Scan(model.SearchFor);
                            break;
                    }
                    ReplaceResult result = new ReplaceResult();
                    foreach (var match in walker)
                    {
                        if (this.worker.CancellationPending)
                        {
                            e.Cancel = true;
                            break;
                        }
                        match.Replace(replacingbytes);
                        result.Offset.Add(match.Offset);
                    }
                    e.Result = result;
                }
            }
            else
                throw new ArgumentException();
        }

        private BinaryScanner Create(string filepath)
        {
            BinaryScanner scanner = BinaryScanner.FromFile(filepath);
            scanner.ProgressPercentage += this.Scanner_ProgressPercentage;
            return scanner;
        }

        private void Scanner_ProgressPercentage(object sender, ProgressPercentageEventArgs e)
        {
            this.synccontext?.Post(new SendOrPostCallback(delegate
            {
                this.progressBar1.Value = (int)e.Percentage;
            }), null);
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            if (this.worker.IsBusy)
                this.worker.CancelAsync();
            else
            {
                if (string.IsNullOrWhiteSpace(this.textBox_file.Text))
                {
                    MessageBox.Show(this, "File cannot be empty. Please specify a file to patch.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(this.textBox_file.Text))
                {
                    MessageBox.Show(this, "The given file is not existed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(this.textBox_searchfor.Text))
                {
                    MessageBox.Show(this, "Search pattern is not allowed to be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(this.textBox_replacewith.Text))
                {
                    MessageBox.Show(this, "Replacing content is not allowed to be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (this.textBox_replacewith.Text.Length > this.textBox_searchfor.Text.Length)
                {
                    MessageBox.Show(this, "The replacing content should have shorter or same length as the relaced one.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (Leayal.StringHelper.IsEqual(this.textBox_replacewith.Text, this.textBox_searchfor.Text, true))
                {
                    MessageBox.Show(this, "The replacing content should be different from the relaced one.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                TaskModel model = new TaskModel()
                {
                    Filepath = this.textBox_file.Text,
                    SearchFor = this.textBox_searchfor.Text,
                    ReplaceWith = this.textBox_replacewith.Text
                };
                if (this.radioButton_byte.Checked)
                    model.Type = Type.Byte;
                else
                    model.Type = Type.Hex;
                this.worker.RunWorkerAsync(model);
            }
        }

        private enum Type : byte
        {
            Hex,
            Byte
        }

        private class ReplaceResult
        {
            public List<long> Offset { get; }
            public ReplaceResult()
            {
                this.Offset = new List<long>();
            }
        }

        private class TaskModel
        {
            public string Filepath;
            public Type Type;
            public string SearchFor;
            public string ReplaceWith;
        }

        private void btn_browse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog()
            {
                Multiselect = false,
                Filter = "Any files|*",
                CheckPathExists = true,
                RestoreDirectory = true,
                CheckFileExists = true,
                Title = "Select file for binary scanner"
            })
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    this.textBox_file.Text = ofd.FileName;
                }
            }
        }
    }
}
