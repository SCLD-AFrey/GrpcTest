using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Data;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Google.Protobuf.WellKnownTypes;
using GrpcBroadcast.Client.Core;

namespace Test.Application.ViewModels
{
    [MetadataType(typeof(MetaData))]
    public class MainViewModel
    {
        public class MetaData : IMetadataProvider<MainViewModel>
        {
            void IMetadataProvider<MainViewModel>.BuildMetadata
                (MetadataBuilder<MainViewModel> p_builder)
            {                
                p_builder.CommandFromMethod(p_x => p_x.OnLockPersonScriptCommand())
                    .CommandName("LockPersonScriptCommand");
                p_builder.CommandFromMethod(p_x => p_x.OnCommitPersonScriptCommand())
                    .CommandName("CommitPersonScriptCommand");
                p_builder.Property(p_x => p_x.SelectedPerson)
                    .OnPropertyChangedCall(p_x => p_x.OnSelectedPersonChanged());
                
            }
        }

        #region Constructors

        protected MainViewModel()
        {
            uow = new UnitOfWork()
            {
                ConnectionString =
                    "XpoProvider=MSSqlServer;data source=(localdb)\\MSSQLLocalDB;integrated security=SSPI;initial catalog=SampleData",
                AutoCreateOption = AutoCreateOption.DatabaseAndSchema
            };
            PersonCollectionDb = new XPCollection<SampleData.Person>(uow);
            PersonCollection = new ObservableCollection<Person>();
            IsReadOnly = false;
            CheckTime = DateTime.UtcNow;
            LoadData();
            BindingOperations.EnableCollectionSynchronization(BroadcastHistory, m_broadcastHistoryLockObject);
            StartReadingBroadcastServer();
            
        }

        public static MainViewModel Create()
        {
            return ViewModelSource.Create(() => new MainViewModel());
        }

        #endregion

        #region Fields and Properties

        public virtual UnitOfWork uow { get; set; }
        public virtual XPCollection<SampleData.Person> PersonCollectionDb { get; set; } 
        public virtual ObservableCollection<Person> PersonCollection { get; set; }
        public virtual Person SelectedPerson { get; set; }
        public virtual bool IsReadOnly { get; set; }
        public virtual DateTime CheckTime { get; set; }
        public virtual string LockButton { get; set; } = String.Empty;
        
        public virtual ObservableCollection<string> BroadcastHistory { get; } = new ObservableCollection<string>();
        private readonly object m_broadcastHistoryLockObject = new();
        private static readonly BroadcastServiceClient m_broadcastService = new();
        private static string m_originId = Guid.NewGuid().ToString();
        private bool StartReadingBroadcastServer()
        {
            try
            {
                var cts = new CancellationTokenSource();
                _ = m_broadcastService.BroadcastLogs()
                    .ForEachAsync(
                        p_x => ProcessUpdates(p_x.At, p_x.OriginId, p_x.Content),
                        cts.Token);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }

        private void ProcessUpdates(Timestamp p_at, string p_originId, string p_content)
        {

            if (CheckTime < p_at.ToDateTime() && p_originId != m_originId)
            {

                try
                {
                    var personID = int.Parse(p_content.Split(' ')[1]);
                    var p = PersonCollection.Single(x => x.PersonId == personID);
                    
                    switch (p_content.Split(' ')[0].ToUpper())
                    {
                        case "LOCK":
                            p.IsLocked = true;
                            break;
                        case "UNLOCK":
                            p.IsLocked = false;
                            break;
                        case "UPDATE":
                            switch (p_content.Split(' ')[2].ToUpper())
                            {
                                case "FIRSTNAME":
                                    p.FirstName = p_content.Split(' ')[3];
                                    break;
                                case "LASTNAME":
                                    p.LastName = p_content.Split(' ')[3];
                                    break;
                                case "DOB":
                                    p.Dob = DateTime.Parse(p_content.Split(' ')[3]);
                                    break;
                                case "PHONE": case "PHONENUMBER":
                                    p.PhoneNumber = p_content.Split(' ')[3];
                                    break;
                            }
                            break;
                    }
                    uow.CommitChanges();

                    BroadcastHistory.Add(
                        $"{p_at.ToDateTime().ToString("HH:mm:ss")} {p_originId} : {p_content.Split(' ')[0].ToUpper()}-{p_content.Split(' ')[1]}");


                }
                catch (Exception e)
                {
                    //BroadcastHistory.Add($"{p_at.ToDateTime().ToString("HH:mm:ss")} {p_originId} : {p_content} - {e.Message}");
                }


            }
            

            CheckTime = DateTime.UtcNow;
        }

        #endregion

        #region Methods

        public void OnSelectedPersonChanged()
        {
            IsReadOnly = !SelectedPerson.IsLocked;
            LockButton = SelectedPerson.IsLocked ? "UNLOCK" : "LOCK";
        }
        public void OnCommitPersonScriptCommand()
        {
            var person = PersonCollectionDb.Single(x => x.Oid == SelectedPerson.PersonId);
            person.FirstName = SelectedPerson.FirstName;
            person.LastName = SelectedPerson.LastName;
            person.PhoneNumber = SelectedPerson.PhoneNumber;
            person.DOB = SelectedPerson.Dob;
            uow.CommitChanges();
            m_broadcastService.WriteCommandExecute($"UPDATE {JsonSerializer.Serialize(SelectedPerson)}", m_originId);
        }
        public void OnLockPersonScriptCommand()
        {
            SelectedPerson.IsLocked = !SelectedPerson.IsLocked;
            IsReadOnly = !SelectedPerson.IsLocked;

            var person = PersonCollectionDb.Single(x => x.Oid == SelectedPerson.PersonId);
            person.IsLocked = SelectedPerson.IsLocked;
            uow.CommitChanges();

            m_broadcastService.WriteCommandExecute($"{(SelectedPerson.IsLocked ? "LOCK" : "UNLOCK")} {SelectedPerson.PersonId}", m_originId);
            LockButton = SelectedPerson.IsLocked ? "UNLOCK" : "LOCK";
        }        
        private void LoadData()
        {
            foreach (var p in PersonCollectionDb)
            {
                PersonCollection.Add(new Person()
                {
                    PersonId = p.Oid,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Dob = p.DOB,
                    PhoneNumber = p.PhoneNumber,
                    IsLocked = p.IsLocked
                }); 
            }
        }
        
        #endregion
    }
}