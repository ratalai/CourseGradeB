﻿using System.Collections.Generic;
using System.Windows.Forms;
using FISCA;
using FISCA.Presentation;
using Framework.Security;
using JHSchool.Affair;
using System;
using DataRationality;
using K12.Presentation;
using JHSchool;
using Framework;
using CourseGradeB.CourseExtendControls;


namespace CourseGradeB
{
    public class Program
    {
        [MainMethod("")]
        public static void Main()
        {

            // 課程加入教師檢視
            Course.Instance.AddView(new TeacherCategoryView());

            #region 資料項目
            // 基本資料
            Course.Instance.AddDetailBulider(new JHSchool.Legacy.ContentItemBulider<BasicInfoItem>());

            // 修課學生
            Course.Instance.AddDetailBulider(new JHSchool.Legacy.ContentItemBulider<SCAttendItem>());
            
            #endregion
            
            #region 課程/編輯
            RibbonBarItem rbItem = Student.Instance.RibbonBarItems["教務"];
            RibbonBarButton rbButton;
            rbItem = Course.Instance.RibbonBarItems["編輯"];
            rbButton = rbItem["新增"];
            rbButton.Size = RibbonBarButton.MenuButtonSize.Large;
            rbButton.Image = Properties.Resources.btnAddCourse;
            rbButton.Enable = User.Acl["JHSchool.Course.Ribbon0000"].Executable;
            rbButton.Click += delegate
            {
                new CourseGradeB.CourseExtendControls.Ribbon.AddCourse().ShowDialog();
            };

            rbButton = rbItem["刪除"];
            rbButton.Size = RibbonBarButton.MenuButtonSize.Large;
            rbButton.Image = Properties.Resources.btnDeleteCourse;
            rbButton.Enable = User.Acl["JHSchool.Course.Ribbon0010"].Executable;
            rbButton.Click += delegate
            {
                if (Course.Instance.SelectedKeys.Count == 1)
                {
                    JHSchool.Data.JHCourseRecord record = JHSchool.Data.JHCourse.SelectByID(Course.Instance.SelectedKeys[0]);
                    //int CourseAttendCot = Course.Instance.Items[record.ID].GetAttendStudents().Count;
                    List<JHSchool.Data.JHSCAttendRecord> scattendList = JHSchool.Data.JHSCAttend.SelectByStudentIDAndCourseID(new List<string>() { }, new List<string>() { record.ID });
                    int attendStudentCount = 0;
                    foreach (JHSchool.Data.JHSCAttendRecord scattend in scattendList)
                    {
                        if (scattend.Student.Status == K12.Data.StudentRecord.StudentStatus.一般)
                            attendStudentCount++;
                    }

                    if (attendStudentCount > 0)
                        MsgBox.Show(record.Name + " 有" + attendStudentCount.ToString() + "位修課學生，請先移除修課學生後再刪除課程.");
                    else
                    {
                        string msg = string.Format("確定要刪除「{0}」？", record.Name);
                        if (MsgBox.Show(msg, "刪除課程", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            #region 自動刪除非一般學生的修課記錄
                            List<JHSchool.Data.JHSCAttendRecord> deleteSCAttendList = new List<JHSchool.Data.JHSCAttendRecord>();
                            foreach (JHSchool.Data.JHSCAttendRecord scattend in scattendList)
                            {
                                JHSchool.Data.JHStudentRecord stuRecord = JHSchool.Data.JHStudent.SelectByID(scattend.RefStudentID);
                                if (stuRecord == null) continue;
                                if (stuRecord.Status != K12.Data.StudentRecord.StudentStatus.一般)
                                    deleteSCAttendList.Add(scattend);
                            }
                            List<string> studentIDs = new List<string>();
                            foreach (JHSchool.Data.JHSCAttendRecord scattend in deleteSCAttendList)
                                studentIDs.Add(scattend.RefStudentID);
                            List<JHSchool.Data.JHSCETakeRecord> sceList = JHSchool.Data.JHSCETake.SelectByStudentAndCourse(studentIDs, new List<string>() { record.ID });
                            JHSchool.Data.JHSCETake.Delete(sceList);
                            JHSchool.Data.JHSCAttend.Delete(deleteSCAttendList);
                            #endregion

                            JHSchool.Data.JHCourse.Delete(record);
                            
                            // 加這主要是重新整理
                            Course.Instance.SyncDataBackground(record.ID);
                        }
                        else
                            return;
                    }
                }
            };

            RibbonBarButton CouItem = Course.Instance.RibbonBarItems["編輯"]["刪除"];
            Course.Instance.SelectedListChanged += delegate
            {
                // 課程刪除不能多選
                CouItem.Enable = (Course.Instance.SelectedList.Count < 2) && User.Acl["JHSchool.Course.Ribbon0010"].Executable;
            };
            #endregion

            #region 匯出/匯入

            RibbonBarButton rbItemExport = Student.Instance.RibbonBarItems["資料統計"]["匯出"];
            RibbonBarButton rbItemImport = Student.Instance.RibbonBarItems["資料統計"]["匯入"];

            RibbonBarItem rbItemCourseImportExport = Course.Instance.RibbonBarItems["資料統計"];
            rbItemCourseImportExport["匯出"]["匯出課程修課學生"].Enable = User.Acl["JHSchool.Course.Ribbon0031"].Executable;
            rbItemCourseImportExport["匯出"]["匯出課程修課學生"].Click += delegate
            {
                SmartSchool.API.PlugIn.Export.Exporter exporter = new CourseGradeB.ImportExport.Course.ExportCourseStudents("");
                CourseGradeB.ImportExport.Course.ExportStudentV2 wizard = new CourseGradeB.ImportExport.Course.ExportStudentV2(exporter.Text, exporter.Image);
                exporter.InitializeExport(wizard);
                wizard.ShowDialog();
            };
            rbItemCourseImportExport["匯入"]["匯入課程修課學生"].Enable = User.Acl["JHSchool.Course.Ribbon0021"].Executable;
            rbItemCourseImportExport["匯入"]["匯入課程修課學生"].Click += delegate
            {
                SmartSchool.API.PlugIn.Import.Importer importer = new CourseGradeB.ImportExport.Course.ImportCourseStudents("");
                CourseGradeB.ImportExport.Course.ImportStudentV2 wizard = new CourseGradeB.ImportExport.Course.ImportStudentV2(importer.Text, importer.Image);
                importer.InitializeImport(wizard);
                wizard.ShowDialog();
            };			

            #endregion

            #region 權限註冊
            // 學生學期歷程
            Catalog ribbon = RoleAclSource.Instance["學生"]["功能按鈕"];
            ribbon.Add(new RibbonFeature("JHSchool.Student.Ribbon0169", "匯出學期歷程"));
            ribbon.Add(new RibbonFeature("JHSchool.Student.Ribbon0170", "匯入學期歷程"));

            // 課程
            ribbon = RoleAclSource.Instance["課程"]["功能按鈕"];
            ribbon.Add(new RibbonFeature("JHSchool.Course.Ribbon0031", "匯出課程修課學生"));
            ribbon.Add(new RibbonFeature("JHSchool.Course.Ribbon0021", "匯入課程修課學生"));
            #endregion

        }
    }
}
