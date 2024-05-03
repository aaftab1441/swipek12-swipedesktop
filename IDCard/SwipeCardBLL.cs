using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeK12.SwipeCardTableAdapters;

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

        // ======================
        // StudentAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeCard.StudentCardDataTable GetStudentCard(int personID)
        {
            return StudentAdapter.GetStudentCard(personID);
        }

        // ==========================
        // TempStudentAdapter Methods
        // ==========================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeCard.TempStudentCardDataTable GetTempStudentCard(int personID)
        {
            return TempStudentAdapter.GetTempStudentCard(personID);
        }

        // ======================
        // TeacherAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeCard.TeacherCardDataTable GetTeacherCard(int personID)
        {
            return TeacherAdapter.GetTeacherCard(personID);
        }

        // ======================
        // PersonAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeCard.PersonDataTable GetAllActiveStudentsBySchool(int schoolID)
        {
            return PersonAdapter.GetActivePersonsBySchoolAndType(1, schoolID);
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public SwipeCard.PersonDataTable GetAllActiveTeachersBySchool(int schoolID)
        {
            return PersonAdapter.GetActivePersonsBySchoolAndType(4, schoolID);
        }

        // ======================
        // IDCardsAdapter Methods
        // ======================

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, true)]
        public SwipeCard.IDCardsDataTable GetIDCards()
        {
            return IDCardsAdapter.GetAllCards();
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public SwipeCard.IDCardsDataTable GetCardById(int CardID)
        {
            return IDCardsAdapter.GetCardByID(CardID);
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public SwipeCard.IDCardsDataTable GetActiveIDCards()
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
        public SwipeCard.IDCardsConfigDataTable GetIDCardsConfig()
        {
            return IDCardsConfigAdapter.GetIDCardsConfig();
        }

        [System.ComponentModel.DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType.Select, false)]
        public string GetIDCardsByConfigName(string ConfigName)
        {
            SwipeCard.IDCardsConfigDataTable table = IDCardsConfigAdapter.GetIDCardsConfigByConfigName(ConfigName);
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
        public SwipeCard.SchoolDataTable GetAllSchools()
        {
            return SchoolAdapter.GetAllSchools();
        }
    }
}
