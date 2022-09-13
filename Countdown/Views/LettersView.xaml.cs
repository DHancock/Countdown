﻿using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class LettersView : Page
{
    public LettersView()
    {
        this.InitializeComponent();
    }

    public LettersViewModel? ViewModel { get; set; }

    private void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        StringBuilder sb = new StringBuilder();

        foreach (WordItem wordItem in WordList.SelectedItems.Cast<WordItem>())
            sb.AppendLine(wordItem.ToString());
         
        if (sb.Length > 0)
        {
            DataPackage dp = new();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }
    }

    private void CopyCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        args.CanExecute = WordList.SelectedItems.Any();
    }
    

}
