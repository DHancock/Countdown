using System.Windows.Input;
using Countdown.Models;


namespace Countdown.ViewModels
{
    internal sealed class SettingsViewModel 
    {
        private readonly LetterList previousVowelList = UserSettings.Vowels.DeepClone();
        private readonly LetterList previousConsonantList = UserSettings.Consonants.DeepClone();

        public ICommand RevertVowelsCommand { get; }
        public ICommand RevertConsonantsCommand { get; }
        public ICommand DefaultVowelsCommand { get; }
        public ICommand DefaultConsonantsCommand { get; }


        public SettingsViewModel()
        {
            RevertVowelsCommand = new RelayCommand(RevertVowelsExecute, RevertVowelsValid);
            RevertConsonantsCommand = new RelayCommand(RevertConsonantsExecute, RevertConsonantsValid);

            DefaultVowelsCommand = new RelayCommand(DefaultVowelsExecute);
            DefaultConsonantsCommand = new RelayCommand(DefaultConsonantsExecute);
        }



        public static LetterList VowelList
        {
            get { return UserSettings.Vowels; } 
        }


        public static LetterList ConsonantList
        {
            get { return UserSettings.Consonants; }
        }
        

        private void RevertVowelsExecute(object p)
        {
            UserSettings.Vowels.ResetTo(previousVowelList);
        }

        private bool RevertVowelsValid(object p)
        {
            return UserSettings.Vowels != previousVowelList; 
        }
      

        private void DefaultVowelsExecute(object p)
        {
            UserSettings.Vowels.ResetTo(UserSettings.DefaultVowels);
        }

        

        private void RevertConsonantsExecute(object p)
        {
            UserSettings.Consonants.ResetTo(previousConsonantList);
        }

        private bool RevertConsonantsValid(object p)
        {
            return UserSettings.Consonants != previousConsonantList;
        }


        private void DefaultConsonantsExecute(object p)
        {
            UserSettings.Consonants.ResetTo(UserSettings.DefaultConsonants);
        }
    }
}

