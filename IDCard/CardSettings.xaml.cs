using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using log4net;

namespace SwipeK12
{
    /// <summary>
    /// Interaction logic for CardSettings.xaml
    /// </summary>
    public partial class CardSettings : Window
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool schoolChanged = false;
        private bool studentChanged = false;
        private bool teacherChanged = false;
        private bool imageFolderChanged = false;

        private string initialFolder = "";

        public CardSettings()
        {
            InitializeComponent();

            loadSchools();
            loadStudents();
            loadTeachers();
            loadFolders();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            schoolChanged = App.getCurrentSchoolID() != getSelectedSchoolID();
            studentChanged = App.getCurrentStudentID() != getSelectedStudentID();
            teacherChanged = App.getCurrentTeacherID() != getSelectedTeacherID();
            imageFolderChanged = !initialFolder.Equals(txtImageFolder.Text);

            App.setCurrentSchoolID(getSelectedSchoolID());
            App.setCurrentStudentID(getSelectedStudentID());
            App.setCurrentTeacherID(getSelectedTeacherID());

            if (imageFolderChanged)
            {
                updateImageFolder();
            }

            this.DialogResult = true;
        }

        private void cboSchool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            loadStudents();
            loadTeachers();
        }

        private void loadSchools()
        {
            int selectedIndex = 0;

            Dictionary<int, string> schoolList = SwipeUtils.getSchoolsList();
            cboSchool.ItemsSource = schoolList;
            cboSchool.SelectedIndex = 0;

            foreach (var item in cboSchool.Items)
            {
                KeyValuePair<int, string> kv = (KeyValuePair<int, string>)item;
                if (kv.Key == App.getCurrentSchoolID())
                {
                    cboSchool.SelectedIndex = selectedIndex;
                    break;
                }
                selectedIndex++;
            }
        }
                   
        private void loadStudents()
        {
            int selectedIndex = 0;

            Dictionary<int, string> studentList = SwipeUtils.getStudentsList(getSelectedSchoolID());
            cboStudent.ItemsSource = studentList;
            cboStudent.SelectedIndex = 0;

            foreach (var item in cboStudent.Items)
            {
                KeyValuePair<int, string> kv = (KeyValuePair<int, string>)item;
                if (kv.Key == App.getCurrentStudentID())
                {
                    cboStudent.SelectedIndex = selectedIndex;
                    break;
                }
                selectedIndex++;
            }
        }

        private void loadTeachers()
        {
            int selectedIndex = 0;

            Dictionary<int, string> teacherList = SwipeUtils.getTeachersList(getSelectedSchoolID());
            cboTeacher.ItemsSource = teacherList;
            cboTeacher.SelectedIndex = 0;

            foreach (var item in cboTeacher.Items)
            {
                KeyValuePair<int, string> kv = (KeyValuePair<int, string>)item;
                if (kv.Key == App.getCurrentTeacherID())
                {
                    cboTeacher.SelectedIndex = selectedIndex;
                    break;
                }
                selectedIndex++;
            }
        }

        private void loadFolders()
        {
            txtImageFolder.Text = SwipeUtils.getPhotoImageFolder();
            initialFolder = txtImageFolder.Text;
        }

        private void updateImageFolder()
        {
            SwipeUtils.updatePhotoImageFolder(txtImageFolder.Text);
        }

        private int getSelectedSchoolID()
        {
            KeyValuePair<int, string> kv = (KeyValuePair<int, string>)cboSchool.SelectedItem;
            return kv.Key;
        }

        private int getSelectedStudentID()
        {
            KeyValuePair<int, string> kv = (KeyValuePair<int, string>)cboStudent.SelectedItem;
            return kv.Key;
        }

        private int getSelectedTeacherID()
        {
            KeyValuePair<int, string> kv = (KeyValuePair<int, string>)cboTeacher.SelectedItem;
            return kv.Key;
        }

        public bool isSchoolChanged()
        {
            return schoolChanged;
        }

        public bool isStudentChanged()
        {
            return studentChanged;
        }

        public bool isTeacherChanged()
        {
            return teacherChanged;
        }

        public bool isImageFolderChanged()
        {
            return imageFolderChanged;
        }

        private void btnImageFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.SelectedPath = txtImageFolder.Text;

            DialogResult result = folderDialog.ShowDialog();

            if (result.ToString() == "OK")
            {
                txtImageFolder.Text = folderDialog.SelectedPath;
            }
        }
    }
}
