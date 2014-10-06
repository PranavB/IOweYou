using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using IOU.Commands;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text;
using Windows.Globalization.NumberFormatting;
using Windows.System.UserProfile;
using Windows.Globalization;

namespace IOU
{
    public class Store : INotifyPropertyChanged
    {
        public int id { get; set; }
        public string name { get; set; }
        public double amount { get; set; }
        public string currency { get; set; }

        [IgnoreDataMember]
        public ICommand deleteCommand { get; set; }

        public Store()
        {
            deleteCommand = new deleteButtonClick();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }


    // ------------------------------------------------------------------------------------------------------


    public class StoreDataSource
    {
        const string storageFile = "ioufile.json";
        private ObservableCollection<Store> _debts;

        public StoreDataSource()
        {
            _debts = new ObservableCollection<Store>();
        }

        public async Task initialLoad()
        {
            await readJSON();
            if (_debts == null)
                _debts = new ObservableCollection<Store>();
        }

        private async Task readJSON()
        {
            if (_debts == null) return;
            var jsonSerializer = new DataContractJsonSerializer(typeof(ObservableCollection<Store>));

            try
            {
                using (var stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(storageFile))
                {
                    _debts = (ObservableCollection<Store>)jsonSerializer.ReadObject(stream);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Whoops: {0}", e);
            }
        }

        public async void addToOCandStore(string n, double a)
        {
            double amt = a;
            
            // look for existing debt
            foreach (Store item in _debts)
            {
                if (n == item.name)
                {
                    amt += item.amount;
                    _debts.Remove(item);
                    break;
                }
            }

            Store st = new Store();
            if (_debts.Count == 0)
                st.id = 0;
            else
                st.id = _debts.Last().id + 1;
            st.name = n;
            st.amount = amt;

            string currency = GlobalizationPreferences.Currencies[0];
            StringBuilder amountAsCurrency = new StringBuilder();
            CurrencyFormatter defaultCurrencyFormatter = new CurrencyFormatter(currency);
            defaultCurrencyFormatter.IsGrouped = true;
            amountAsCurrency.AppendLine(defaultCurrencyFormatter.Format(st.amount));
            st.currency = amountAsCurrency.ToString();

            _debts.Add(st);

            await storeJSON();
        }

        private async Task storeJSON()
        {
            var jsonSerializer = new DataContractJsonSerializer(typeof(ObservableCollection<Store>));
            try
            {
                using (var stream = await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(storageFile,
                    CreationCollisionOption.ReplaceExisting))
                {
                    jsonSerializer.WriteObject(stream, _debts);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Whoops becauase: {0}", e);
            }
        }

        public async void deleteDebt(Store s)
        {
            //_debts.Remove(s);
            short i = 0, flag = 0;
            foreach (Store item in _debts)
            {
                if (item.id == s.id)
                {
                    flag = 1;
                    break;
                }
                i++;
            }
            if (flag == 1)
            {
                _debts.RemoveAt(i);
            }
            await storeJSON();
        }

        public ObservableCollection<Store> getObservableItems()
        {
            return _debts;
        }

    }

    //-----------------------------------------------------------------------------------------------------

    public class deleteButtonClick : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            App.sds.deleteDebt((Store)parameter);
        }
    }

}
