using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Sonata.Desktop.Models;
using Sonata.Desktop.Services;

namespace Sonata.Desktop.ViewModels;

public class ConversationViewModel : INotifyPropertyChanged
{
    private string _currentInput = string.Empty;
    private bool _isBusy;
    private Conversation? _currentConversation;
    private readonly ApiService _apiService;
    private long _conversationLoadVersion;

    public ObservableCollection<Message> Messages { get; } = new();
    public ObservableCollection<Conversation> Conversations { get; } = new();

    public string CurrentInput
    {
        get => _currentInput;
        set
        {
            if (_currentInput == value) return;
            _currentInput = value;
            OnPropertyChanged();
            SendCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            SendCommand.RaiseCanExecuteChanged();
        }
    }

    public Conversation? CurrentConversation
    {
        get => _currentConversation;
        set
        {
            if (_currentConversation == value) return;
            SetCurrentConversation(value, loadMessages: true);
        }
    }

    public AsyncRelayCommand SendCommand { get; }
    public AsyncRelayCommand NewChatCommand { get; }

    public ConversationViewModel()
    {
        _apiService = new ApiService();
        SendCommand = new AsyncRelayCommand(SendAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(CurrentInput));
        NewChatCommand = new AsyncRelayCommand(NewChatAsync);
        _ = LoadConversationsAsync();
    }

    private async Task LoadConversationsAsync()
    {
        try
        {
            var conversations = await _apiService.GetAllConversationsAsync();

            foreach (var conversation in conversations)
            {
                Conversations.Add(conversation);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to load conversations.");
        }
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput) || IsBusy) return;
        IsBusy = true;

        var userText = CurrentInput;
        CurrentInput = string.Empty;
        var conversation = CurrentConversation ?? new Conversation
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var conversationId = conversation.Id;

        if (CurrentConversation is null)
        {
            Conversations.Add(conversation);
            SetCurrentConversation(conversation, loadMessages: false);
        }

        Messages.Add(new Message
        {
            ConversationId = conversationId,
            Content = userText,
            Role = "user",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        try
        {
            var reply = await _apiService.SendMessageAsync(userText, conversationId);

            Messages.Add(new Message
            {
                ConversationId = conversationId,
                Content = reply,
                Role = "assistant",
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
        catch (Exception)
        {
            Messages.Add(new Message
            {
                ConversationId = conversationId,
                Content = "The API request failed.",
                Role = "assistant",
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task NewChatAsync()
    {
        Messages.Clear();
        CurrentConversation = null;
        return Task.CompletedTask;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetCurrentConversation(Conversation? conversation, bool loadMessages)
    {
        if (_currentConversation == conversation) return;

        _currentConversation = conversation;
        OnPropertyChanged(nameof(CurrentConversation));
        SendCommand.RaiseCanExecuteChanged();

        if (loadMessages)
        {
            _ = LoadMessagesAsync(conversation);
        }
    }

    private async Task LoadMessagesAsync(Conversation? conversation)
    {
        var loadVersion = ++_conversationLoadVersion;
        Messages.Clear();

        if (conversation == null) return;

        try
        {
            var messages = await _apiService.GetMessagesAsync(conversation.Id);

            if (loadVersion != _conversationLoadVersion || !ReferenceEquals(CurrentConversation, conversation)) return;

            foreach (var message in messages)
            {
                Messages.Add(message);
            }
        }
        catch (Exception)
        {
            if (loadVersion == _conversationLoadVersion) Console.WriteLine("Failed to load messages.");
        }
    }
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter)
    {
        try
        {
            if (!CanExecute(parameter)) return;
            await _execute();
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to execute.");
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
