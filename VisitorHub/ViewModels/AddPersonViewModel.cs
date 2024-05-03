using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Models;

namespace SwipeDesktop.ViewModels
{
    public class AddPersonViewModel : ReactiveObject, IViewModel
    {
        private LocalStorage Storage;
        private RemoteStorage RemoteStorage;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddPersonViewModel));


        public AddPersonViewModel(LocalStorage localDbStorage, RemoteStorage remoteStorage)
        {
            Storage = localDbStorage;
            RemoteStorage = remoteStorage;

            PeopleTypes = new List<string> { "Student", "Staff" };
            PersonType = "Student";
            PersonToAdd = new PersonModel();

            this.WhenAnyValue(x => x.PersonType).Where(x => x == "Student").Subscribe(x =>
            {
               AdditionalDetails = new StudentDetails();
            });

            this.WhenAnyValue(x => x.PersonType).Where(x => x == "Teacher").Subscribe(x =>
            {
                AdditionalDetails = null;
            });

            this.WhenAnyValue(x => x.PersonType).Where(x => x == "Staff").Subscribe(x =>
            {
                AdditionalDetails = new StaffDetails();
            });

            this.WhenAnyValue(x => x.PersonToAdd.Error).Subscribe(x =>
            {
                SaveErrors = !string.IsNullOrEmpty(x)
                    ? "There was a problem adding this person."
                    : string.Empty;
            });

            this.WhenAnyValue(x => x.SaveErrors).ToProperty(this, x => x.DisplayErrors, out _displayErrors);

            this.WhenAnyValue(x => x.PersonToAdd.LastNameError, x=>x.PersonToAdd.FirstNameError, x=>x.PersonToAdd.IdNumberError, x=>x.PersonToAdd.DateOfBirthError).Subscribe(x =>
            {
                ValidationErrors = !string.IsNullOrEmpty(x.Item1) || !string.IsNullOrEmpty(x.Item2) || !string.IsNullOrEmpty(x.Item3) || !string.IsNullOrEmpty(x.Item4)
                    ? "Please fill out all required fields."
                    : string.Empty;
            });

        }
        public List<string> PeopleTypes { get; set; }

        private PersonModel _personToAdd;
        public PersonModel PersonToAdd
        {
            get { return _personToAdd; }
            set { this.RaiseAndSetIfChanged(ref _personToAdd, value); }
        }
   

        private IViewModel _details;
        public IViewModel AdditionalDetails
        {
            get { return _details; }
            set { this.RaiseAndSetIfChanged(ref _details, value); }
        }


        private string _personType;
        public string PersonType
        {
            get { return _personType; }
            set { this.RaiseAndSetIfChanged(ref _personType, value); }
        }
  
        public IObservable<Tuple<object, AddPersonViewModel>> AddPerson()
        {
            if (AdditionalDetails.GetType() == typeof(StudentDetails))
            {
                if (Storage.CheckStudentPerson(this.PersonToAdd.IdNumber))
                {
                    SaveErrors = "This Student Number already exists.";
                }
                else
                {
                    try
                    {
                       
                        //var task = Task.Run(() => RemoteStorage.SendStudent(this));
                        //var rslt = task.Result;
                        //if (rslt != null)
                        //{
                        //    Storage.InsertStudent(PersonToAdd, AdditionalDetails as StudentDetails, rslt.PersonRecord, rslt.StudentRecord);
                        //}

                        var rslt = Storage.InsertStudent(PersonToAdd, AdditionalDetails as StudentDetails);
                        return Observable.Return(new Tuple<object, AddPersonViewModel>(rslt, this));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        SaveErrors = ex.Message;
                    }
                }
            }

            if (AdditionalDetails.GetType() == typeof(StaffDetails))
            {
                if (Storage.CheckStaffPerson(this.PersonToAdd.IdNumber))
                {
                    SaveErrors = "This Staff Number already exists.";
                }
                else
                {
                    try
                    {
                       
                        /*var task = Task.Run(() => RemoteStorage.SendStaff(this));
                        var rslt = task.Result;

                        if (rslt != null)
                        {
                            Storage.InsertStaff(PersonToAdd, AdditionalDetails as StaffDetails, rslt.PersonRecord);
                        }*/

                        var rslt = Storage.InsertStaff(PersonToAdd, AdditionalDetails as StaffDetails);
                        return Observable.Return(new Tuple<object, AddPersonViewModel>(rslt, this));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        SaveErrors = ex.Message;
                    }
                }
            }
            
            return Observable.Return(new Tuple<object, AddPersonViewModel>(null, this));
        }

        string _errors;
        public string ValidationErrors
        {
            get { return _errors; }
            set { this.RaiseAndSetIfChanged(ref _errors, value); }
        }


        string _saveErrors;
        public string SaveErrors
        {
            get { return _saveErrors; }
            set { this.RaiseAndSetIfChanged(ref _saveErrors, value); }
        }

        private ObservableAsPropertyHelper<string> _displayErrors;
        public string DisplayErrors
        {
            get { return _displayErrors.Value; }
        }


        public bool ValidateInput()
        {
            if (ValidationErrors.Length > 0)
                return false;

            return true;
        }
    }
}
