﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Countdown.Models;

namespace Countdown.ViewModels
{
    internal sealed class ConundrumViewModel : PropertyChangedBase
    {
        private const char space_char = ' ';

        private Model Model { get; }
        public StopwatchController StopwatchController { get; }
        public ObservableCollection<ConundrumItem> SolutionList { get; } = new ObservableCollection<ConundrumItem>();
        private object scrollToItem;

        // flag to see if this vm's view has been loaded  
        private bool notLoadedYet = true;

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
        private string solution;

        public ICommand ChooseCommand { get; }
        public ICommand SolveCommand { get; }
        public ICommand ListCopyCommand { get; }
        public ICommand GoToDefinitionCommand { get; }
        public ICommand ClearCommand { get; }


        public ConundrumViewModel(Model model, StopwatchController sc)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            StopwatchController = sc ?? throw new ArgumentNullException(nameof(sc));

            SolveCommand = new RelayCommand(ExecuteSolve, CanSolve);
            ChooseCommand = new RelayCommand(ExecuteChoose, CanChoose);

            ListCopyCommand = new RelayCommand(ExecuteCopy, CanCopy);
            GoToDefinitionCommand = new RelayCommand(ExecuteGoToDefinition, CanGoToDefinition);
            ClearCommand = new RelayCommand(ExecuteClear, CanCopy);
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
                ClearSolution();
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

        public object ScrollToItem
        {
            get { return scrollToItem; }
            set
            {
                scrollToItem = value;
                RaisePropertyChanged();
            }
        }

        public bool IsSelected
        {
            set
            {
                // Bound to the parent tabs IsSelected property. Delay picking
                // a conundrum, the dictionary is loaded in a background task 
                // and this method could block until its completed.
                if (value && notLoadedYet)
                {
                    notLoadedYet = false;

                    if (CanChoose(null))
                        ExecuteChoose(null);
                }
            }
        }



        private void ClearSolution()
        {
            if ((Solution is null) || Solution.Any(c => c != space_char))
                Solution = new string(space_char, Model.cLetterCount);
        }
        


        private void ExecuteChoose(object p)
        {
            ClearSolution();

            Model.GenerateConundrum();

            foreach (string property in propertyNames)
                RaisePropertyChanged(property);
        }


   
        private bool CanChoose(object p)
        {
            if (notLoadedYet)
                return false;

            return Model.HasConundrums ;
        }



        private void ExecuteSolve(object p)
        {
            string word = Model.Solve();

            if (word != null)
            {
                char[] conundrum = new char[Model.cLetterCount];

                for (int index = 0; index < Model.cLetterCount; ++index)
                    conundrum[index] = Model.Conundrum[index][0]; 

                Solution = word;
                SolutionList.Add(new ConundrumItem(new string(conundrum), word));
                ScrollToItem = SolutionList[^1];
            }
        }


        private bool CanSolve(object p)
        {
            if (notLoadedYet)
                return false;

            return (Solution[0] == space_char) && Model.Conundrum.Any(s => !string.IsNullOrEmpty(s)) && (Model.Solve() != null);
        }

   

        private void ExecuteCopy(object p)
        {
            if (SolutionList != null)
            {
                StringBuilder sb = new StringBuilder();

                foreach (ConundrumItem e in SolutionList)
                {
                    if (e.IsSelected)
                    {
                        if (sb.Length > 0)
                            sb.Append(Environment.NewLine);
                        
                        sb.Append(e.ToString());
                    }
                }

                if (sb.Length > 0)
                    Clipboard.SetText(sb.ToString());
            }
        }


        private bool CanCopy(object p) => SolutionList.Any(e => e.IsSelected);
    
        private void ExecuteGoToDefinition(object p)
        {
            try
            {
                foreach (ConundrumItem e in SolutionList)
                {
                    if (e.IsSelected)
                    {
                        ProcessStartInfo psi = new()
                        {
                            UseShellExecute = true,
                            FileName = string.Format(CultureInfo.InvariantCulture, p.ToString(), e.Solution),
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

        private bool CanGoToDefinition(object p) => SolutionList.Count(e => e.IsSelected) is > 0 and < 11;

        private void ExecuteClear(object p)
        {
            for (int index = SolutionList.Count - 1; index >= 0; --index)
            {
                if (SolutionList[index].IsSelected)
                    SolutionList.RemoveAt(index);
            }
        }
    }
}