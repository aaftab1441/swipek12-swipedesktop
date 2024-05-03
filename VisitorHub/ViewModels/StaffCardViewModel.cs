using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Reactive.Linq;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Interfaces;
using Ookii;
using SwipeDesktop.Common;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;

namespace SwipeDesktop.ViewModels
{
    public class StaffCardViewModel : ReactiveObject, IViewModel
    {
        private readonly LocalStorage Storage;
        private readonly FineStorage FineStorage;
        private readonly IdCardStorage IdCardStorage;

        private ReactiveList<Tuple<string, int>> _stats;
        public ReactiveList<Tuple<string, int>> DatabaseStats
        {
            get { return _stats; }
            set { this.RaiseAndSetIfChanged(ref _stats, value); }
        }

        private ReactiveList<CardTemplate> _cards;
        public ReactiveList<CardTemplate> IdCardTemplates
        {
            get { return _cards; }
            set { this.RaiseAndSetIfChanged(ref _cards, value); }
        }


        private ReactiveList<Tuple<DateTime>> _pvcPrinted;
        public ReactiveList<Tuple<DateTime>> PvcCardsPrinted
        {
            get { return _pvcPrinted; }
            set { this.RaiseAndSetIfChanged(ref _pvcPrinted, value); }
        }

        private ReactiveList<Tuple<DateTime>> _tempPrinted;
        public ReactiveList<Tuple<DateTime>> TempCardsPrinted
        {
            get { return _tempPrinted; }
            set { this.RaiseAndSetIfChanged(ref _tempPrinted, value); }
        }

        private ReactiveList<Fine> _fines;
        public ReactiveList<Fine> Fines
        {
            get { return _fines; }
            set { this.RaiseAndSetIfChanged(ref _fines, value); }
        }

        /*
        private ReactiveList<IdCard> _idCards;
        public ReactiveList<IdCard> IdCards
        {
            get { return _idCards; }
            set { this.RaiseAndSetIfChanged(ref _idCards, value); }
        }*/

        ObservableAsPropertyHelper<string> _pvcPrinter;
        public string PvcPrinter
        {
            get { return _pvcPrinter.Value; }
        }

        
        ObservableAsPropertyHelper<string> _tempIdPrinter;
        public string TempIdPrinter
        {
            get { return _tempIdPrinter.Value; }
        }

        private CardTemplate _selectedTemplate;

        public CardTemplate SelectedTemplate
        {
            get { return _selectedTemplate; }
            set { this.RaiseAndSetIfChanged(ref _selectedTemplate, value); }
        }

        private CardTemplate _pvcTemplate;

        public CardTemplate PvcTemplate
        {
            get { return _pvcTemplate; }
            set { this.RaiseAndSetIfChanged(ref _pvcTemplate, value); }
        }

        private CardTemplate _tempTemplate;

        public CardTemplate TempTemplate
        {
            get { return _tempTemplate; }
            set { this.RaiseAndSetIfChanged(ref _tempTemplate, value); }
        }



        private string _host;

        public string Host
        {
            get { return _host; }
            set { this.RaiseAndSetIfChanged(ref _host, value); }
        }


        private string _sqlIp;

        public string SqlIp
        {
            get { return _sqlIp; }
            set { this.RaiseAndSetIfChanged(ref _sqlIp, value); }
        }

        private string _config;

        public string Config
        {
            get { return _config; }
            set { this.RaiseAndSetIfChanged(ref _config, value); }
        }

        private decimal _finePaid;

        public decimal FinePaid
        {
            get { return _finePaid; }
            set { this.RaiseAndSetIfChanged(ref _finePaid, value); }
        }


        private bool _needsRefresh;

        public bool ModelNeedsRefresh
        {
            get { return _needsRefresh; }
            set { this.RaiseAndSetIfChanged(ref _needsRefresh, value); }
        }

        /*
        public void SaveFine()
        {
            var schoolId = Settings.Default.SchoolId;

            if (ChargeFee)
            {
                
                var fine = new AssessedFine(this.FineAmt) { SchoolId = schoolId };
                fine.StudentNumber = this.CurrentStudent.IdNumber;
                fine.StudentGuid = this.CurrentStudent.UniqueId;
                fine.StudentId = this.CurrentStudent.StudentId;
                fine.AmountPaid = this.FinePaid;
                fine.RecordedBy = this.AcceptedBy;
                fine.FineDate = DateTime.Now;
                fine.Amount = this.FineAmt.Amount;
                fine.Text = "ID Card Fee";

                FineStorage.InsertObject(fine);
            }

            if (PrintPvcId)
            {
                var pvcCard = new NewIdCard() { SchoolId = schoolId };
                pvcCard.PrintDate = DateTime.Now;
                pvcCard.StudentNumber = this.CurrentStudent.IdNumber;
                pvcCard.StudentGuid = this.CurrentStudent.UniqueId;
                pvcCard.StudentId = this.CurrentStudent.StudentId;
                pvcCard.RecordedBy = this.AcceptedBy;
                pvcCard.PrintDate = DateTime.Now;
                pvcCard.Type = "Replacement ID";

                IdCardStorage.InsertObject(pvcCard);
            }

            if (PrintTempId)
            {
                var tempCard = new NewIdCard() { SchoolId = schoolId };
                tempCard.PrintDate = DateTime.Now;
                tempCard.StudentNumber = this.CurrentStudent.IdNumber;
                tempCard.StudentGuid = this.CurrentStudent.UniqueId;
                tempCard.StudentId = this.CurrentStudent.StudentId;
                tempCard.RecordedBy = this.AcceptedBy;
                tempCard.PrintDate = DateTime.Now;
                tempCard.Type = "Temp ID";

                IdCardStorage.InsertObject(tempCard);
            }

            //CurrentStudent = null;
        }
         * */

        public StaffCardViewModel(LocalStorage storage, FineStorage fineStorage, IdCardStorage idCardStorage)
        {
            Storage = storage;
            FineStorage = fineStorage;
            IdCardStorage = idCardStorage;
            PrintTempId = true;

            Printers = Application.Current.Properties["Printers"] as PrintQueueCollection;
            TempIdPrinterQueue = Application.Current.Properties["TempIdPrintQueue"] as PrintQueue;
            PvcPrinterQueue = Application.Current.Properties["PvcPrintQueue"] as PrintQueue;
          
            SchoolName = Settings.Default.School;
            SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture);

            Fines = new ReactiveList<Fine>(storage.Fines(Settings.Default.SchoolId));
            IdCardTemplates = new ReactiveList<CardTemplate>(storage.TeacherCards(Settings.Default.SchoolId));

            FineAmt = Fines.SingleOrDefault(x => x.Name.ToLower() == "temp id");
            SelectedTemplate = IdCardTemplates.SingleOrDefault(x => x.TemplateName == Settings.Default.TempIdTemplateName);
     
            SqlIp = Settings.Default.SqlMasterIp;

            Host = Environment.MachineName;

            this.WhenAnyValue(x => x.TempIdPrinterQueue).Select(x => x.FullName).ToProperty(this, x => x.TempIdPrinter, out _tempIdPrinter);
            this.WhenAnyValue(x => x.PvcPrinterQueue).Select(x => x.FullName).ToProperty(this, x => x.PvcPrinter, out _pvcPrinter);

            this.WhenAnyValue(x => x.CurrentPerson).Where(x => x != null).Subscribe(x =>
            {
                var printed = new List<Tuple<DateTime>>(storage.TempIdsPrinted(CurrentPerson.PersonId));
                var printedToday = IdCardStorage.GetItemsByStudent(DateTime.Today, CurrentPerson.IdNumber).ToList();
                
                if(printedToday.Any())
                   printed.AddRange(printedToday.Where(s=>s.Type == "Temp ID").Select(s=>new Tuple<DateTime>(s.PrintDate)));

                TempCardsPrinted = new ReactiveList<Tuple<DateTime>>(printed.AsEnumerable().DistinctBy(p => p.Item1.ToString("g")).OrderByDescending(d => d.Item1).ToArray());

                printed =
                    new List<Tuple<DateTime>>(
                        storage.PvcIdsPrinted(CurrentPerson.PersonId).OrderByDescending(d => d.Item1));

                if (printedToday.Any())
                    printed.AddRange(printedToday.Where(s=>s.Type == "Replacement ID").Select(s=>new Tuple<DateTime>(s.PrintDate)));

                PvcCardsPrinted = new ReactiveList<Tuple<DateTime>>(printed.AsEnumerable().DistinctBy(p => p.Item1.ToString("g")).OrderByDescending(d => d.Item1).ToArray());
            });

         
            this.WhenAnyValue(x => x.PaidInFull,x=>x.ChargeFee).Where(x => x.Item1 && x.Item2).Select(x => this.FineAmt.Amount).Subscribe(x=>
            {
                FinePaid = x;
            });

            this.WhenAnyValue(x => x.PaidInFull).Where(x => x == false).Subscribe(x =>
            {
                FinePaid = 0;
            });

            this.WhenAnyValue(x => x.PrintTempId).Where(x => x).Subscribe(x =>
            {
                PrintPvcId = false;
                var defaultTemplate = Settings.Default.TempIdTemplateName;

                if (TempTemplate == null)
                {
                    if (string.IsNullOrEmpty(defaultTemplate))
                    {
                        if (IdCardTemplates.Count > 0)
                            defaultTemplate = IdCardTemplates.First().TemplateName;
                    }

                    if (TempTemplate == null)
                    {
                        TempTemplate = IdCardTemplates.SingleOrDefault(i => i.TemplateName == defaultTemplate);
                    }
                }

                SelectedTemplate = TempTemplate;
            });

            this.WhenAnyValue(x => x.SelectedTemplate).Subscribe(x =>
            {
                if (PrintTempId)
                {
                    TempTemplate = SelectedTemplate;
                }

                if (PrintPvcId)
                {
                    PvcTemplate = SelectedTemplate;
                }
            });

            this.WhenAnyValue(x => x.PrintPvcId).Where(x => x).Subscribe(x =>
            {
                PrintTempId = false;
                if (PvcTemplate == null)
                {
                    var defaultTemplate = Settings.Default.StudentIdTemplateName;

                    if (string.IsNullOrEmpty(defaultTemplate))
                    {
                        if (IdCardTemplates.Count > 0)
                            defaultTemplate = IdCardTemplates.First().TemplateName;
                    }

                    if (PvcTemplate == null)
                    {
                        PvcTemplate = IdCardTemplates.SingleOrDefault(i => i.TemplateName == defaultTemplate);
                    }
                }

                SelectedTemplate = PvcTemplate;
               
            });
        }

        /*
        public void RefreshFineStats()
        {
            if (CurrentStudent != null)
            {
                var printed = new List<Tuple<DateTime>>(Storage.TempIdsPrinted(CurrentStudent.PersonId));
                var printedToday = IdCardStorage.GetItemsByStudent(DateTime.Today, CurrentStudent.IdNumber).ToList();

                if (printedToday.Any())
                    printed.AddRange(
                        printedToday.Where(s => s.Type == "Temp ID").Select(s => new Tuple<DateTime>(s.PrintDate)));

                TempCardsPrinted =
                    new ReactiveList<Tuple<DateTime>>(
                        printed.AsEnumerable()
                            .DistinctBy(p => p.Item1.ToString("g"))
                            .OrderByDescending(d => d.Item1)
                            .ToArray());

                printed =
                    new List<Tuple<DateTime>>(
                        Storage.PvcIdsPrinted(CurrentStudent.PersonId).OrderByDescending(d => d.Item1));

                if (printedToday.Any())
                    printed.AddRange(
                        printedToday.Where(s => s.Type == "Replacement ID")
                            .Select(s => new Tuple<DateTime>(s.PrintDate)));

                PvcCardsPrinted =
                    new ReactiveList<Tuple<DateTime>>(
                        printed.AsEnumerable()
                            .DistinctBy(p => p.Item1.ToString("g"))
                            .OrderByDescending(d => d.Item1)
                            .ToArray());
            }

        }
        */

        private PrintQueue _tempIdPrinterQueue;

        public PrintQueue TempIdPrinterQueue
        {
            get { return _tempIdPrinterQueue; }
            set { this.RaiseAndSetIfChanged(ref _tempIdPrinterQueue, value); }
        }



        private PrintQueue _pvcPrinterQueue;

        public PrintQueue PvcPrinterQueue
        {
            get { return _pvcPrinterQueue; }
            set { this.RaiseAndSetIfChanged(ref _pvcPrinterQueue, value); }
        }

        private bool _chargeFee;

        public bool ChargeFee
        {
            get { return _chargeFee; }
            set { this.RaiseAndSetIfChanged(ref _chargeFee, value); }
        }

        private bool _printReceipt;

        public bool PrintReceipt
        {
            get { return _printReceipt; }
            set { this.RaiseAndSetIfChanged(ref _printReceipt, value); }
        }

        private bool _tempIdPrinted;

        public bool PrintTempId
        {
            get { return _tempIdPrinted; }
            set { this.RaiseAndSetIfChanged(ref _tempIdPrinted, value); }
        }

        private bool _pvcIdPrinted;

        public bool PrintPvcId
        {
            get { return _pvcIdPrinted; }
            set { this.RaiseAndSetIfChanged(ref _pvcIdPrinted, value); }
        }

        private bool _paidInFull;

        public bool PaidInFull
        {
            get { return _paidInFull; }
            set { this.RaiseAndSetIfChanged(ref _paidInFull, value); }
        }

        private Fine _fineAmt;

        public Fine FineAmt
        {
            get { return _fineAmt; }
            set { this.RaiseAndSetIfChanged(ref _fineAmt, value); }
        }


        private string _schoolId;

        public string SchoolId
        {
            get { return _schoolId; }
            set { this.RaiseAndSetIfChanged(ref _schoolId, value); }
        }


        private int _tempIdCount;

        public int TempIdCount
        {
            get { return _tempIdCount; }
            set { this.RaiseAndSetIfChanged(ref _tempIdCount, value); }
        }


        private int _pvcIdCount;

        public int PvcIdCount
        {
            get { return _pvcIdCount; }
            set { this.RaiseAndSetIfChanged(ref _pvcIdCount, value); }
        }

        private string _acceptedBy;

        public string AcceptedBy
        {
            get { return _acceptedBy; }
            set { this.RaiseAndSetIfChanged(ref _acceptedBy, value); }
        }

        private string _schoolName;

        public string SchoolName
        {
            get { return _schoolName; }
            set { this.RaiseAndSetIfChanged(ref _schoolName, value); }
        }

        private PersonModel _currentPerson;

        public PersonModel CurrentPerson
        {
            get { return _currentPerson; }
            set { this.RaiseAndSetIfChanged(ref _currentPerson, value); }
        }

        private PrintQueueCollection _printers;

        public PrintQueueCollection Printers
        {
            get { return _printers; }
            set { this.RaiseAndSetIfChanged(ref _printers, value); }
        }


    }
}
