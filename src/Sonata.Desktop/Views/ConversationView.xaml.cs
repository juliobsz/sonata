using System.Windows;
using System.Windows.Input;
using Sonata.Desktop.ViewModels;

namespace Sonata.Desktop.Views;

public partial class ConversationView : Window
{
    public ConversationView()
    {
        InitializeComponent();
        
        DataContext = new ConversationViewModel();
    }

    private void CommandInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers == ModifierKeys.Shift) return;

        var command = SendButton.Command;
        var parameter = SendButton.CommandParameter;

        if (command?.CanExecute(parameter) == true) command.Execute(parameter);
        
        e.Handled = true;
    }
}

