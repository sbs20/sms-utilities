using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Sbs20.SmsUtilities
{
    public partial class frmMain : Form
    {
        MessageSet _messageSet;
        MessageSet _filteredMessageSet;

        public frmMain()
        {
            this.Font = SystemFonts.DialogFont;
            InitializeComponent();

            _messageSet = new MessageSet();

            this.dgMessages.Columns.Add("address", "To");
            this.dgMessages.Columns.Add("contact_name", "Who");
            this.dgMessages.Columns.Add("date", "Date");
            this.dgMessages.Columns.Add("type", "Type");
            this.dgMessages.Columns.Add("body", "Body");

            this.dtTo.Value = DateTime.Today;
            this.dtFrom.Value = DateTime.Today.AddMonths(-6);
        }

        private void DisplayMessagesInGrid()
        {
            this.dgMessages.Rows.Clear();

            foreach (DataRow row in _messageSet.DataSet.Tables[0].Rows)
            {
                string type = ((int)row["type"] == 1) ? "Rec" : "Sent";
                this.dgMessages.Rows.Add(
                    row["address"],
                    row["contact_name"],
                    row["date"],
                    type,
                    row["body"]);
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            fileDialogOpen.ShowDialog();
        }

        private void loadFile(FileInfo fileInfo)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(fileInfo.FullName);
            _messageSet = new MessageSet();
            _messageSet.LoadArchive(xdoc);

            DisplayMessagesInGrid();
            this.statusBar.Text = _messageSet.Count + " messages";
            this.btnSaveAs.Enabled = false;
            _filteredMessageSet = null;
        }

        private void fileDialog_FileOk(object sender, CancelEventArgs e)
        {
            this.loadFile(new FileInfo(fileDialogOpen.FileName));
        }

        private void dgMessages_DragDrop(object sender, DragEventArgs e)
        {
            string[] filepaths = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (filepaths.Length > 0)
            {
                this.loadFile(new FileInfo(filepaths[0]));
            }
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            fileDialogSave.ShowDialog();
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            _filteredMessageSet = _messageSet.Filter(this.dtFrom.Value, this.dtTo.Value);
            this.statusBar.Text = _filteredMessageSet.Count + " filtered messages";
            if (_filteredMessageSet.Count > 0)
            {
                this.btnSaveAs.Enabled = true;
            }
        }

        private void fileDialogSave_FileOk(object sender, CancelEventArgs e)
        {
            this._filteredMessageSet.ToXmlFile(this.fileDialogSave.FileName);
        }
    }
}
