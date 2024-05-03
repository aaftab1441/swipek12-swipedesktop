using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop;
using SwipeDesktop.IdCardUtils.SwipeCardTableAdapters;

namespace SwipeK12
{
    [System.ComponentModel.DataObject]
    class SwipeCardBLL
    {
        private StudentCardTableAdapter _StudentAdapter = null;
        private TempStudentCardTableAdapter _TempStudentAdapter = null;
        private TeacherCardTableAdapter _TeacherAdapter = null;
        private PersonTableAdapter _PersonAdapter = null;
        private IDCardsTableAdapter _IDCardsAdapter = null;
        private IDCardsConfigTableAdapter _IDCardsConfigAdapter = null;
        private SchoolTableAdapter _SchoolAdapter = null;

        protected StudentCardTableAdapter StudentAdapter
        {
            get
            {
                if (_StudentAdapter == null)
                    _StudentAdapter = new StudentCardTableAdapter();

                return _StudentAdapter;
            }
        }

        protected TempStudentCardTableAdapter TempStudentAdapter
        {
            get
            {
                if (_TempStudentAdapter == null)
                    _TempStudentAdapter = new TempStudentCardTableAdapter();

                return _TempStudentAdapter;
            }
        }

        protected TeacherCardTableAdapter TeacherAdapter
        {
            get
            {
                if (_TeacherAdapter == null)
                    _TeacherAdapter = new TeacherCardTableAdapter();

                return _TeacherAdapter;
            }
        }

        protected PersonTableAdapter PersonAdapter
        {
            get
            {
                if (_PersonAdapter == null)
                    _PersonAdapter = new PersonTableAdapter();

                return _PersonAdapter;
            }
        }

        protected IDCardsTableAdapter IDCardsAdapter
        {
            get
            {
                if (_IDCardsAdapter == null)
                    _IDCardsAdapter = new IDCardsTableAdapter();

                return _IDCardsAdapter;
            }
        }

        protected IDCardsConfigTableAdapter IDCardsConfigAdapter
        {
            get
            {
                if (_IDCardsConfigAdapter == null)
                    _IDCardsConfigAdapter = new IDCardsConfigTableAdapter();

                return _IDCardsConfigAdapter;
            }
        }

        protected SchoolTableAdapter SchoolAdapter
        {
            get
            {
                if (_SchoolAdapter == null)
                    _SchoolAdapter = new SchoolTableAdapter();

                return _SchoolAdapter;
            }
        }


        SwipeDesktop.IdCardUtils.SwipeCard.StudentCardDataTable getDefault()
        {
           
            var dt1 = new SwipeDesktop.IdCardUtils.SwipeCard.StudentCardDataTable();
            dt1.AddStudentCardRow("Sample", "", "", "Student", "Student, Sample", DateTime.Parse("1992-04-01"), Settings.Default.School, "AA0000", "XX", "bus", "homeroom", "AA0000.jpg","AA0000");
            return dt1;
        }

        SwipeDesktop.IdCardUtils.SwipeCard.TempStudentCardDataTable getDefaultTemp()
        {
           
            var dt1 = new SwipeDesktop.IdCardUtils.SwipeCard.TempStudentCardDataTable();
            dt1.AddTempStudentCardRow("Sample", "", "", "Student", "Student, Sample", DateTime.Parse("1992-04-01"), Settings.Default.School, "AA0000", "XX", "bus", "homeroom",0,0,0, "AA0000.jpg", "AA0000");
            return dt1;
        }
        // ======================
        // StudentAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeDesktop.IdCardUtils.SwipeCard.StudentCardDataTable GetStudentCard(int personID)
        {
            try
            {
                //return getDefault();
                return StudentAdapter.GetStudentCard(personID);
            }
            catch (Exception ex)
            {
                return getDefault();
            }
        }

        // ==========================
        // TempStudentAdapter Methods
        // ==========================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeDesktop.IdCardUtils.SwipeCard.TempStudentCardDataTable GetTempStudentCard(int personID)
        {
            try
            {
                //return getDefault();
                return TempStudentAdapter.GetTempStudentCard(personID);
            }
            catch (Exception ex)
            {
                return getDefaultTemp();
            }
        }

        // ======================
        // TeacherAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeDesktop.IdCardUtils.SwipeCard.TeacherCardDataTable GetTeacherCard(int personID)
        {
            return TeacherAdapter.GetTeacherCard(personID);
        }

        // ======================
        // PersonAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeDesktop.IdCardUtils.SwipeCard.PersonDataTable GetAllActiveStudentsBySchool(int schoolID)
        {
            return PersonAdapter.GetActivePersonsBySchoolAndType(1, schoolID);
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public SwipeDesktop.IdCardUtils.SwipeCard.PersonDataTable GetAllActiveTeachersBySchool(int schoolID)
        {
            return PersonAdapter.GetActivePersonsBySchoolAndType(4, schoolID);
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public SwipeDesktop.IdCardUtils.SwipeCard.PersonDataTable GetStaff(int schoolID)
        {
            return PersonAdapter.GetStaff(schoolID);
        }

        // ======================
        // IDCardsAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeDesktop.IdCardUtils.SwipeCard.IDCardsDataTable GetIDCards()
        {
            return IDCardsAdapter.GetAllCards();
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public SwipeDesktop.IdCardUtils.SwipeCard.IDCardsDataTable GetCardById(int CardID)
        {
            return IDCardsAdapter.GetCardByID(CardID);
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public SwipeDesktop.IdCardUtils.SwipeCard.IDCardsDataTable GetActiveIDCards()
        {
            return IDCardsAdapter.GetCardsByActive(true);
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public int GetActiveIDCardsCount()
        {
            return Convert.ToInt32(IDCardsAdapter.GetActiveCardsCount());
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Insert, true)]
        public int AddIDCard(int SchoolID, string CardName, double CardWidth, double CardHeight, bool StudentCard, bool TeacherCard, bool OtherCard, bool TempCard, string FrontBackground, double FrontOpacity, string BackBackground, double BackOpacity, bool DualSided, bool FrontPortrait, bool BackPortrait, string Fields, bool Active)
        {
            int newRowID = Convert.ToInt32(IDCardsAdapter.InsertIDCard(SchoolID, CardName, CardWidth, CardHeight, StudentCard, TeacherCard, OtherCard, TempCard, FrontBackground, FrontOpacity, BackBackground, BackOpacity, DualSided, FrontPortrait, BackPortrait, Fields, Active));

            return newRowID;
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Update, true)]
        public int UpdateCardByIdAndSchool(string FrontBackground, double FrontOpacity, string BackBackground, double BackOpacity, bool DualSided, string Fields, bool Active, int CardID, int SchoolID)
        {
            int rowUpdated = IDCardsAdapter.UpdateCardByIdAndSchool(FrontBackground, FrontOpacity, BackBackground, BackOpacity, DualSided, Fields, Active, DateTime.Now, CardID, SchoolID);

            return rowUpdated;
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Update, false)]
        public int ArchiveIdCard(int CardID, int SchoolID)
        {
            int rowUpdated = IDCardsAdapter.UpdateCardActiveByIdAndSchool(false, DateTime.Now, CardID, SchoolID);

            return rowUpdated;
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Insert, false)]
        public int DuplicateIDCard(int CardID, string CardName)
        {
            int newRowID = Convert.ToInt32(IDCardsAdapter.DuplicateIDCard(CardName, CardID));

            return newRowID;
        }

        // =================================
        // IDCardsConfigTableAdapter Methods
        // =================================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeDesktop.IdCardUtils.SwipeCard.IDCardsConfigDataTable GetIDCardsConfig()
        {
            return IDCardsConfigAdapter.GetIDCardsConfig();
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public string GetIDCardsByConfigName(string ConfigName)
        {
            SwipeDesktop.IdCardUtils.SwipeCard.IDCardsConfigDataTable table = IDCardsConfigAdapter.GetIDCardsConfigByConfigName(ConfigName);
            return table.Rows[0]["ConfigValue"].ToString();
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Update, false)]
        public int UpdateIDCardsConfigByName(string ConfigValue, string ConfigName)
        {
            int rowUpdated = IDCardsConfigAdapter.UpdateIDCardsConfigByName(ConfigValue, ConfigName);

            return rowUpdated;
        }
        

        // ==========================
        // SchoolTableAdapter Methods
        // ==========================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeDesktop.IdCardUtils.SwipeCard.SchoolDataTable GetAllSchools()
        {
            return SchoolAdapter.GetAllSchools();
        }
    }
}
