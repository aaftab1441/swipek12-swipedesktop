using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwipeDesktop.Modal
{
/*
    public static class DialogWindowConstants
    {
        public static string SettingsDialog { get { return "SettingsDialog"; } }
        public static string StudentCardDialog { get { return "StudentCardDialog"; } }
        public static string StaffCardDialog { get { return "StaffCardDialog"; } }
        public static string StudentAlternateId { get { return "StudentAlternateId"; } }
        public static string AddPersonViewModel { get { retuStudentsSelectedForPrintrn "AddPersonViewModel"; } }

        public static string BatchPrint { get { return "BatchPrintViewModel"; } }
    }*/

    public enum DialogConstants
    {
        BatchPrint,
        SettingsDialog,
        VisitorSettingsDialog,
        StudentCardDialog,
        StaffCardDialog,
        StudentAlternateId,
        AddPersonViewModel,
        StudentsSelectedForPrint

    }
}
