using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Test.Application
{
    public class Person : INotifyPropertyChanged
    {

        private int personId = 0;
        private string firstName = string.Empty;
        private string lastName = string.Empty;
        private DateTime dob = DateTime.UtcNow;
        private string phoneNumber = string.Empty;
        private bool isLocked = false;

        public int PersonId { 
            get { return this.personId; }
            set {
                if (value != this.personId)
                {
                    this.personId = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        public string FirstName { 
            get { return this.firstName; }
            set {
                if (value != this.firstName)
                {
                    this.firstName = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        public string LastName { 
            get { return this.lastName; }
            set {
                if (value != this.lastName)
                {
                    this.lastName = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        public DateTime Dob { 
            get { return this.dob; }
            set {
                if (value != this.dob)
                {
                    this.dob = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        public string PhoneNumber { 
            get { return this.phoneNumber; }
            set {
                if (value != this.phoneNumber)
                {
                    this.phoneNumber = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        public bool IsLocked { 
            get { return this.isLocked; }
            set {
                if (value != this.isLocked)
                {
                    this.isLocked = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}