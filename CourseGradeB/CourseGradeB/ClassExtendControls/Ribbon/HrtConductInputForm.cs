﻿using CourseGradeB.StuAdminExtendControls;
using DevComponents.DotNetBar;
using FISCA.Presentation.Controls;
using FISCA.UDT;
using K12.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace CourseGradeB.ClassExtendControls.Ribbon
{
    public partial class HrtConductInputForm : BaseForm
    {
        private ClassRecord _class;
        private List<StudentRecord> _studentList;
        List<string> _conductTemplate;
        AccessHelper _A;
        List<DataGridViewCell> _dirtyCellList;
        int _gradeYear, _schoolYear, _semester;
        string _term;
        ComboBoxItem _middle, _final, _RunningComboBoxItem;
        ButtonItem _RunningItem;
        Dictionary<string, ConductRecord> _conductRecordDic;

        public HrtConductInputForm(int schoolYear, int semester, string id)
        {
            InitializeComponent();
            _schoolYear = schoolYear;
            _semester = semester;
            _gradeYear = -1;
            //取得班級
            _class = K12.Data.Class.SelectByID(id);

            _conductTemplate = new List<string>();
            _A = new AccessHelper();
            _dirtyCellList = new List<DataGridViewCell>();
            _middle = new ComboBoxItem("Middle", 1);
            _final = new ComboBoxItem("Final", 2);
            _RunningComboBoxItem = new ComboBoxItem("", -1);
            _RunningItem = new ButtonItem();
            _conductRecordDic = new Dictionary<string, ConductRecord>();

            colGrade.Items.Add("");
            colGrade.Items.Add("O");
            colGrade.Items.Add("M");
            colGrade.Items.Add("S");
            colGrade.Items.Add("X");
            cboTerm.Items.Add(_middle);
            cboTerm.Items.Add(_final);

            //取得班級學生and排序
            _studentList = _class.Students;
            _studentList.Sort(delegate(StudentRecord x, StudentRecord y)
            {
                string xx = x.SeatNo.ToString().PadLeft(3, '0');
                xx += x.Name.PadLeft(10, '0');
                string yy = y.SeatNo.ToString().PadLeft(3, '0');
                yy += y.Name.PadLeft(10, '0');

                return xx.CompareTo(yy);
            });

            //取得年級
            GetGradeYear();
        }

        private void HrtConductInputForm_Load(object sender, EventArgs e)
        {
            //未取得正確年級就提示並離開
            if (_gradeYear == -1)
            {
                MessageBox.Show("班級未設定年級...");
                this.Close();
            }
            else
            {
                this.Text += "(學年度:" + _schoolYear + " 學期:" + _semester + ")";
                //取得須呈現的conduct
                GetConductItem();

                if (_gradeYear == 2)
                {
                    cboTerm.SelectedItem = _middle;
                    cboTerm.Enabled = true;
                }
                else
                {
                    cboTerm.SelectedItem = _final;
                    cboTerm.Enabled = false;
                }
            }
        }

        private void ReLoad()
        {
            dgv.Rows.Clear();
            groupPanel1.Text = "目前無選擇任何學生";
            lblSave.Visible = false;
            txtComment.Text = "";

            GetConductRecord();
            FillItemPanel();
        }

        private void GetGradeYear()
        {
            if (_class.GradeYear.HasValue)
            {
                _gradeYear = _class.GradeYear.Value;

                if (_gradeYear <= 2)
                    _gradeYear = 2;
                else if (_gradeYear <= 6)
                    _gradeYear = 6;
                else
                    _gradeYear = 12;
            }
        }

        private void GetConductItem()
        {
            _conductTemplate.Clear();

            //取得指標項目
            List<ConductSetting> ConductSetting = _A.Select<ConductSetting>("grade=" + _gradeYear);
            if (ConductSetting.Count == 1)
            {
                string conductXml = ConductSetting[0].Conduct;
                if (!string.IsNullOrWhiteSpace(conductXml))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(conductXml);

                    //取得所有common屬性的elem
                    foreach (XmlElement elem in doc.SelectNodes("//Conduct[@Common]"))
                    {
                        string group = elem.GetAttribute("Group");

                        foreach (XmlElement item in elem.SelectNodes("Item"))
                        {
                            string title = item.GetAttribute("Title");

                            string key = group + "_" + title;

                            if (!_conductTemplate.Contains(key))
                                _conductTemplate.Add(key);
                        }
                    }
                }
            }
        }

        private void GetConductRecord()
        {
            _conductRecordDic.Clear();

            List<string> idList = new List<string>();
            foreach (K12.Data.StudentRecord r in _studentList)
            {
                idList.Add(r.ID);
            }

            if (idList.Count > 0)
            {
                string ids = string.Join(",", idList);

                //組sql
                string sqlcmd = "ref_student_id in (" + ids + ") and subject is null and school_year=" + _schoolYear + " and semester=" + _semester;
                if (_term == "")
                    sqlcmd += " and term is null";
                else
                    sqlcmd += " and term='" + _term + "'";

                List<ConductRecord> conductRecords = _A.Select<ConductRecord>(sqlcmd);
                foreach (ConductRecord record in conductRecords)
                {
                    if (!_conductRecordDic.ContainsKey(record.RefStudentId + ""))
                        _conductRecordDic.Add(record.RefStudentId + "", record);
                }
            }
        }

        private void FillItemPanel()
        {
            itemPanel1.Items.Clear();
            //加入ButtonItem
            foreach (K12.Data.StudentRecord record in _studentList)
            {
                if (record.Status == K12.Data.StudentRecord.StudentStatus.一般)
                {
                    ButtonItem item = new ButtonItem();
                    item.OptionGroup = "student";
                    item.Text = record.Name;
                    item.Name = "";

                    if (record.Class != null)
                        item.Name += "班級:" + record.Class.Name;

                    item.Name += " 座號:" + record.SeatNo;
                    item.Name += " 學號:" + record.StudentNumber;
                    item.Name += " 姓名:" + record.Name;

                    ConductRecord conductRecord;
                    if (_conductRecordDic.ContainsKey(record.ID))
                    {
                        conductRecord = _conductRecordDic[record.ID];
                    }
                    else
                    {
                        conductRecord = new ConductRecord();
                        conductRecord.RefStudentId = int.Parse(record.ID);
                        conductRecord.SchoolYear = _schoolYear;
                        conductRecord.Semester = _semester;
                        ComboBoxItem cboItem = cboTerm.SelectedItem as ComboBoxItem;
                        conductRecord.Term = _gradeYear == 2 ? cboItem.Value + "" : "";
                        conductRecord.Subject = "";
                    }

                    item.Tag = conductRecord;

                    item.Click += new EventHandler(Item_click);
                    itemPanel1.Items.Add(item);
                }
            }

            itemPanel1.RecalcLayout();
        }

        private void Item_click(object sender, EventArgs e)
        {
            ButtonItem item = sender as ButtonItem;

            //切換不同item
            if (!_RunningItem.Equals(item))
            {
                if (lblSave.Visible)
                {
                    if (MessageBox.Show("資料未儲存,確定切換?", "ischool", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                    {
                        _RunningItem.RaiseClick();
                        return;
                    }
                }

                _dirtyCellList.Clear();
                lblSave.Visible = false;
                _RunningItem = item;
                groupPanel1.Text = item.Text;

                ConductRecord record = item.Tag as ConductRecord;

                txtComment.Text = record.Comment;

                dgv.Rows.Clear();

                XmlDocument doc = new XmlDocument();
                if (!string.IsNullOrWhiteSpace(record.Conduct))
                    doc.LoadXml(record.Conduct);

                //巡迴所有需要呈現的conductItem
                foreach (string str in _conductTemplate)
                {
                    string group = str.Split('_')[0];
                    string title = str.Split('_')[1];
                    string grade = "";
                    XmlElement elem = doc.SelectSingleNode("//Conduct[@Group='" + group + "']/Item[@Title='" + title + "']") as XmlElement;
                    if (elem != null)
                        grade = elem.GetAttribute("Grade");

                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(dgv, group, title, grade);
                    dgv.Rows.Add(row);
                }

                //備份編輯前資料
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        cell.Tag = cell.Value;
                    }
                }

                txtComment.Tag = txtComment.Text;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (itemPanel1.SelectedItems.Count == 1)
            {
                List<string> logStr = new List<string>();
                ButtonItem buttonItem = itemPanel1.SelectedItems[0] as ButtonItem;
                ConductRecord record = buttonItem.Tag as ConductRecord;

                XmlElement root = new XmlDocument().CreateElement("Conducts");
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    string group = row.Cells[colGroup.Index].Value + "";
                    string title = row.Cells[colTitle.Index].Value + "";
                    string grade = row.Cells[colGrade.Index].Value + "";
                    if (row.Cells[colGrade.Index].Tag + "" != row.Cells[colGrade.Index].Value + "")
                    {
                        logStr.Add("項目(" + group + ")" + title + " Grade從『" + row.Cells[colGrade.Index].Tag + "』改為『" + grade + "』");
                    }

                    XmlElement elem = root.SelectSingleNode("//Conduct[@Group='" + group + "']") as XmlElement;
                    if (elem == null)
                    {
                        elem = root.OwnerDocument.CreateElement("Conduct");
                        elem.SetAttribute("Group", group);
                        root.AppendChild(elem);
                    }

                    XmlElement item = elem.OwnerDocument.CreateElement("Item");
                    item.SetAttribute("Title", title);
                    item.SetAttribute("Grade", grade);

                    elem.AppendChild(item);
                }

                record.Conduct = root.OuterXml;
                if (txtComment.Tag + "" != txtComment.Text)
                    logStr.Add("班導師評語由『" + txtComment.Tag + "』" + "改為『" + txtComment.Text + "』");
                record.Comment = txtComment.Text;
                record.Save();
                //寫log
                FiscaLogWriter(buttonItem, logStr);
                _dirtyCellList.Clear();
                lblSave.Visible = false;

                MessageBox.Show(buttonItem.Text + " 資料已儲存");
            }
        }

        private void FiscaLogWriter(ButtonItem item, List<string> logStr)
        {
            string description = item.Name + "\r\n";
            description += "學年度:" + _schoolYear + " 學期:" + _semester + "\r\n";
            foreach (string str in logStr)
            {
                description += str + "\r\n";
            }

            FISCA.LogAgent.ApplicationLog.Log("HRT指標輸入", "資料編輯", description);
        }

        private void cboTerm_SelectedIndexChanged(object sender, EventArgs e)
        {
            _term = "";
            ComboBoxItem item = cboTerm.SelectedItem as ComboBoxItem;
            if (_gradeYear == 2)
            {
                _term = item.Value + "";
            }

            if (!_RunningComboBoxItem.Equals(item))
            {
                if (lblSave.Visible)
                {
                    if (MessageBox.Show("資料未儲存,確定切換?", "ischool", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                    {
                        cboTerm.SelectedItem = _RunningComboBoxItem;
                        return;
                    }
                }

                _RunningComboBoxItem = item;
                ReLoad();
            }
        }

        private void dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != colGrade.Index) return;

            DataGridViewCell cell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];

            //Cell變更檢查
            if ("" + cell.Tag != "" + cell.Value)
            {
                if (!_dirtyCellList.Contains(cell)) _dirtyCellList.Add(cell);
            }
            else
            {
                if (_dirtyCellList.Contains(cell)) _dirtyCellList.Remove(cell);
            }

            lblSave.Visible = (_dirtyCellList.Count > 0) || (txtComment.Tag + "" != txtComment.Text);
        }

        private class ComboBoxItem
        {
            private int _value;
            private string _key;
            public string DisplayText
            {
                get
                {
                    return _key;
                }
            }

            public int Value
            {
                get
                {
                    return _value;
                }
            }

            public ComboBoxItem(string key, int value)
            {
                _key = key;
                _value = value;
            }
        }

        private void txtComment_Leave(object sender, EventArgs e)
        {
            lblSave.Visible = (_dirtyCellList.Count > 0) || (txtComment.Tag + "" != txtComment.Text);
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == colGrade.Index)
                dgv.BeginEdit(true);
        }
    }
}
