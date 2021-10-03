using Countdown.Models;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

namespace Countdown.ViewModels
{
    internal sealed class ConundrumViewModel : PropertyChangedBase
    {
        private const char space_char = ' ';

        private Model Model { get; }
        public StopwatchController StopwatchController { get; }
        public ObservableCollection<ConundrumItem> SolutionList { get; } = new ObservableCollection<ConundrumItem>();

        // property names for change events when generating data 
        private static readonly string[] propertyNames = { nameof(Conundrum_0),
                                                            nameof(Conundrum_1),
                                                            nameof(Conundrum_2),
                                                            nameof(Conundrum_3),
                                                            nameof(Conundrum_4),
                                                            nameof(Conundrum_5),
                                                            nameof(Conundrum_6),
                                                            nameof(Conundrum_7),
                                                            nameof(Conundrum_8)};
        // property backing store
        private string solution = "         ";

        public ICommand ChooseCommand { get; }
        public RelayCommand SolveCommand { get; }
        public StandardUICommand CopyCommand { get; }
        public ICommand GoToDefinitionCommand { get; }


        public ConundrumViewModel(Model model, StopwatchController sc)
        {
            Model = model;
            StopwatchController = sc;

            SolveCommand = new RelayCommand(ExecuteSolve, CanSolve);
            ChooseCommand = new RelayCommand(ExecuteChoose, CanChoose);

            GoToDefinitionCommand = new RelayCommand(ExecuteGoToDefinition, CanGoToDefinition);


            ExecuteChoose(null);
        }

        /// <summary>
        /// Conundrum properties. 
        /// The char type cannot an have empty value so use strings.
        /// </summary>
        public string Conundrum_0
        {
            get { return Model.Conundrum[0]; }
            set { SetConundrum(value, ref Model.Conundrum[0], nameof(Conundrum_0)); }
        }

        public string Conundrum_1
        {
            get { return Model.Conundrum[1]; }
            set { SetConundrum(value, ref Model.Conundrum[1], nameof(Conundrum_1)); }
        }

        public string Conundrum_2
        {
            get { return Model.Conundrum[2]; }
            set { SetConundrum(value, ref Model.Conundrum[2], nameof(Conundrum_2)); }
        }

        public string Conundrum_3
        {
            get { return Model.Conundrum[3]; }
            set { SetConundrum(value, ref Model.Conundrum[3], nameof(Conundrum_3)); }
        }

        public string Conundrum_4
        {
            get { return Model.Conundrum[4]; }
            set { SetConundrum(value, ref Model.Conundrum[4], nameof(Conundrum_4)); }
        }

        public string Conundrum_5
        {
            get { return Model.Conundrum[5]; }
            set { SetConundrum(value, ref Model.Conundrum[5], nameof(Conundrum_5)); }
        }

        public string Conundrum_6
        {
            get { return Model.Conundrum[6]; }
            set { SetConundrum(value, ref Model.Conundrum[6], nameof(Conundrum_6)); }
        }

        public string Conundrum_7
        {
            get { return Model.Conundrum[7]; }
            set { SetConundrum(value, ref Model.Conundrum[7], nameof(Conundrum_7)); }
        }

        public string Conundrum_8
        {
            get { return Model.Conundrum[8]; }
            set { SetConundrum(value, ref Model.Conundrum[8], nameof(Conundrum_8)); }
        }


        private void SetConundrum(string newValue, ref string existing, string propertyName)
        {
            if (newValue != existing)
            {
                existing = newValue;
                RaisePropertyChanged(propertyName);
                SolveCommand.RaiseCanExecuteChanged();
            }
        }


        public string Solution
        {
            get { return solution; }
            set
            {
                solution = value;
                RaisePropertyChanged(nameof(Solution));
            }
        }

        //public object ScrollToItem
        //{
        //    get { return scrollToItem; }
        //    set
        //    {
        //        scrollToItem = value;
        //        RaisePropertyChanged();
        //    }
        //}




        private void ExecuteChoose(object? _)
        {
            Solution = new string(space_char, Model.cLetterCount);

            Model.GenerateConundrum();

            foreach (string property in propertyNames)
                RaisePropertyChanged(property);

            SolveCommand.RaiseCanExecuteChanged();
        }



        private bool CanChoose(object? _) => Model.HasConundrums;



        private void ExecuteSolve(object? _)
        {
            string word = Model.Solve();

            if (word.Length > 0)
            {
                char[] conundrum = new char[Model.cLetterCount];

                for (int index = 0; index < Model.cLetterCount; ++index)
                    conundrum[index] = Model.Conundrum[index][0];

                Solution = word;

                SolutionList.Insert(0, new ConundrumItem(new string(conundrum), word));

                SolveCommand.RaiseCanExecuteChanged();
            }
        }


        private bool CanSolve(object? _)
        {
            return string.IsNullOrWhiteSpace(Solution) && !Model.Conundrum.Any(s => string.IsNullOrEmpty(s)) && (Model.Solve() != null);
        }




        private void ExecuteGoToDefinition(object? p)
        {
            if (p is string formatStr)
            {
                try
                {
                    foreach (ConundrumItem e in SolutionList)
                    {
                        if (true)
                        {
                            ProcessStartInfo psi = new()
                            {
                                UseShellExecute = true,
                                FileName = string.Format(CultureInfo.InvariantCulture, formatStr, e.Solution),
                            };

                            _ = Process.Start(psi);
                        }
                    }
                }
                catch
                {
                    // fail silently...
                }
            }
        }

        private bool CanGoToDefinition(object? _) => false; // SolutionList.Count(e => e.IsSelected) == 1;
    }
}