using System;
using System.Data;
using System.Text;
using System.Xml;

namespace Sbs20.SmsUtilities
{
    class MessageSet
    {
        private DataSet _messages;
        const long TICKS_AT_EPOCH = 621355968000000000L;
        const long TICKS_PER_MILLISECOND = 10000;

        public MessageSet()
        {
            DataTable table = new DataTable("messages");
            table.Columns.Add("protocol", typeof(int));
            table.Columns.Add("address", typeof(string));
            table.Columns.Add("date", typeof(DateTime));
            table.Columns.Add("type", typeof(int));
            table.Columns.Add("subject", typeof(string));
            table.Columns.Add("body", typeof(string));
            table.Columns.Add("toa", typeof(int));
            table.Columns.Add("sc_toa", typeof(int));
            table.Columns.Add("service_center", typeof(string));
            table.Columns.Add("read", typeof(int));
            table.Columns.Add("status", typeof(int));
            table.Columns.Add("locked", typeof(int));
            table.Columns.Add("readable_date", typeof(string));
            table.Columns.Add("contact_name", typeof(string));

            _messages = new DataSet();
            _messages.Tables.Add(table);
        }

        private object GetAttribute(XmlNode xnode, string attributeName, Type type)
        {
            string attValue = xnode.Attributes[attributeName].Value;

            switch (type.FullName)
            {
                case "System.String":
                    return attValue;

                case "System.Int32":
                    return (attValue == "null") ? 0 : Convert.ToInt32(attValue);

                case "System.DateTime":
                    Int64 javaDateTime = Convert.ToInt64(attValue);
                    return ConvertJavaDateNumberToDateTime(javaDateTime);

                default:
                    throw new NotImplementedException();
            }
            
        }

        private void CopyFromRowValues(DataRow sourcerow)
        {
            DataRow row = _messages.Tables[0].NewRow();

            foreach (DataColumn column in row.Table.Columns)
            {
                row[column] = sourcerow[column.ColumnName];
            }

            _messages.Tables[0].Rows.Add(row);
        }

        private void AddRowFromSmsNode(XmlNode xnode)
        {
            DataRow row = _messages.Tables[0].NewRow();

            foreach (DataColumn column in row.Table.Columns)
            {
                row[column] = GetAttribute(xnode, column.ColumnName, column.DataType);
            }

            _messages.Tables[0].Rows.Add(row);
        }

        public void LoadArchive(XmlDocument xdoc)
        {
            XmlNodeList xlist = xdoc.SelectNodes("/smses/sms");
            foreach (XmlNode xnode in xlist)
            {
                AddRowFromSmsNode(xnode);
            }
        }

        public DataSet DataSet
        {
            get { return _messages; }
        }

        public DataTable DataTable
        {
            get { return _messages.Tables[0]; }
        }

        public MessageSet Filter(DateTime from, DateTime to)
        {
            MessageSet filteredMessageSet = new MessageSet();

            foreach (DataRow row in DataTable.Rows)
            {
                DateTime rowDateTime = (DateTime)row["date"];

                if (from < rowDateTime && rowDateTime < to)
                {
                    filteredMessageSet.CopyFromRowValues(row);
                }
            }
            return filteredMessageSet;
        }

        private DateTime ConvertJavaDateNumberToDateTime(long javaDate)
        {
            javaDate *= TICKS_PER_MILLISECOND;
            javaDate += TICKS_AT_EPOCH;

            return DateTime.FromBinary(javaDate);
        }

        private long ConvertDateTimeToJavaDateNumber(DateTime dateTime)
        {
            long ticks = dateTime.ToBinary();
            ticks -= TICKS_AT_EPOCH;
            ticks /= TICKS_PER_MILLISECOND;
            return ticks;
        }

        public void ToXmlFile(string filepath)
        {
            XmlTextWriter xwriter = new XmlTextWriter(filepath, Encoding.UTF8);
            xwriter.Formatting = Formatting.Indented;
            xwriter.Indentation = 2;

            xwriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8' standalone='yes'");
            xwriter.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"sms.xsl\"");
            xwriter.WriteStartElement("smses");
            xwriter.WriteAttributeString("count", this.Count.ToString());

            foreach (DataRow row in this.DataTable.Rows)
            {
                xwriter.WriteStartElement("sms");

                foreach (DataColumn column in this.DataTable.Columns)
                {
                    object rowValue = row[column];
                    switch (column.DataType.FullName)
                    {
                        case "System.Int32":
                        case "System.String":
                            xwriter.WriteAttributeString(column.ColumnName, rowValue.ToString());
                            break;

                        case "System.DateTime":
                            string dateVal = ConvertDateTimeToJavaDateNumber((DateTime)rowValue).ToString();
                            xwriter.WriteAttributeString(column.ColumnName, dateVal);
                            break;
                    }
                }

                xwriter.WriteEndElement();
            }

            xwriter.WriteEndElement();
            xwriter.Close();
        }

        public int Count
        {
            get { return _messages.Tables[0].Rows.Count; }
        }
    }
}
