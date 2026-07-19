using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Sonata.Desktop.Models;
using Sonata.Desktop.Services;

namespace Sonata.Desktop.ViewModels;

public sealed class ConversationViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private string _currentInput = string.Empty;
    private string _memoryText = string.Empty;
    private string _selectedMemoryType = "ProjectContext";
    private string _statusMessage = "Loading Sonata...";
    private bool _isBusy;
    private bool _isArchiveConfirmationPending;
    private Conversation? _currentConversation;
    private Message? _selectedMessage;
    private MemoryItem? _selectedMemory;
    private long _conversationLoadVersion;

    public ConversationViewModel(ApiService? apiService = null)
    {
        _apiService = apiService ?? new ApiService();

        SendCommand = new AsyncRelayCommand(
            SendAsync,
            () => !IsBusy && !string.IsNullOrWhiteSpace(CurrentInput));
        NewConversationCommand = new AsyncRelayCommand(
            NewConversationAsync,
            () => !IsBusy);
        RefreshMemoriesCommand = new AsyncRelayCommand(
            RefreshMemoriesAsync,
            () => !IsBusy);
        SaveMemoryCommand = new AsyncRelayCommand(
            SaveMemoryAsync,
            CanSaveMemory);
        RequestArchiveCommand = new RelayCommand(
            RequestArchive,
            CanRequestArchive);
        CancelArchiveCommand = new RelayCommand(
            CancelArchive,
            () => IsArchiveConfirmationPending && !IsBusy);
        ConfirmArchiveCommand = new AsyncRelayCommand(
            ArchiveMemoryAsync,
            () => IsArchiveConfirmationPending &&
                  CanRequestArchive());

        _ = InitializeAsync();
    }

    public ObservableCollection<Message> Messages { get; } = [];
    public ObservableCollection<Conversation> Conversations { get; } = [];
    public ObservableCollection<MemoryItem> Memories { get; } = [];
    public ObservableCollection<MemoryDiffItem> LatestMemoryDiff { get; } = [];

    public IReadOnlyList<string> MemoryTypes { get; } =
        ["Preference", "Decision", "ProjectContext"];

    public string MovementName => PrototypeMovement.Name;

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

    public string MemoryText
    {
        get => _memoryText;
        set
        {
            if (_memoryText == value) return;
            _memoryText = value;
            OnPropertyChanged();
            SaveMemoryCommand.RaiseCanExecuteChanged();
        }
    }

    public string SelectedMemoryType
    {
        get => _selectedMemoryType;
        set
        {
            if (_selectedMemoryType == value) return;
            _selectedMemoryType = value;
            OnPropertyChanged();
            SaveMemoryCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            RaiseAllCanExecuteChanged();
        }
    }

    public bool IsArchiveConfirmationPending
    {
        get => _isArchiveConfirmationPending;
        private set
        {
            if (_isArchiveConfirmationPending == value) return;
            _isArchiveConfirmationPending = value;
            OnPropertyChanged();
            RequestArchiveCommand.RaiseCanExecuteChanged();
            CancelArchiveCommand.RaiseCanExecuteChanged();
            ConfirmArchiveCommand.RaiseCanExecuteChanged();
        }
    }

    public Conversation? CurrentConversation
    {
        get => _currentConversation;
        set
        {
            if (ReferenceEquals(_currentConversation, value)) return;
            SetCurrentConversation(value, loadMessages: true);
        }
    }

    public Message? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            if (ReferenceEquals(_selectedMessage, value)) return;
            _selectedMessage = value;
            OnPropertyChanged();

            if (value is { Id: > 0, Role: "user" })
            {
                MemoryText = value.Content;
                StatusMessage =
                    "Edit this user Message into a concise Memory claim.";
            }

            SaveMemoryCommand.RaiseCanExecuteChanged();
        }
    }

    public MemoryItem? SelectedMemory
    {
        get => _selectedMemory;
        set
        {
            if (ReferenceEquals(_selectedMemory, value)) return;
            _selectedMemory = value;
            IsArchiveConfirmationPending = false;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSourceExcerpt));
            RequestArchiveCommand.RaiseCanExecuteChanged();
            ConfirmArchiveCommand.RaiseCanExecuteChanged();
        }
    }

    public string SelectedSourceExcerpt =>
        SelectedMemory?.SourceNote.Excerpt ??
        "Select a Memory to inspect its Source Note.";

    public string MemoryDiffStatus => LatestMemoryDiff.Count == 0
        ? "No Memories informed the latest response."
        : $"{LatestMemoryDiff.Count} Memory item(s) informed the latest response.";

    public AsyncRelayCommand SendCommand { get; }
    public AsyncRelayCommand NewConversationCommand { get; }
    public AsyncRelayCommand RefreshMemoriesCommand { get; }
    public AsyncRelayCommand SaveMemoryCommand { get; }
    public RelayCommand RequestArchiveCommand { get; }
    public RelayCommand CancelArchiveCommand { get; }
    public AsyncRelayCommand ConfirmArchiveCommand { get; }

    private async Task InitializeAsync()
    {
        IsBusy = true;

        try
        {
            await LoadConversationsAsync();
            await LoadMemoriesAsync();
            StatusMessage = "Ready.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not load Sonata: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadConversationsAsync()
    {
        var conversations = await _apiService.GetAllConversationsAsync();
        Conversations.Clear();

        foreach (var conversation in conversations)
        {
            Conversations.Add(conversation);
        }
    }

    private async Task LoadMemoriesAsync()
    {
        var selectedMemoryId = SelectedMemory?.Id;
        var memories = await _apiService.GetMemoriesAsync(
            PrototypeMovement.Id);

        Memories.Clear();
        foreach (var memory in memories)
        {
            Memories.Add(memory);
        }

        SelectedMemory = selectedMemoryId is null
            ? null
            : Memories.FirstOrDefault(memory =>
                memory.Id == selectedMemoryId.Value);
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput) || IsBusy) return;

        IsBusy = true;
        var userText = CurrentInput.Trim();
        CurrentInput = string.Empty;
        ReplaceMemoryDiff([]);

        var conversation = CurrentConversation ?? new Conversation
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (CurrentConversation is null)
        {
            Conversations.Insert(0, conversation);
            SetCurrentConversation(conversation, loadMessages: false);
        }

        Messages.Add(new Message
        {
            Id = 0,
            ConversationId = conversation.Id,
            Sequence = Messages.Count + 1,
            Content = userText,
            Role = "user",
            CreatedAt = DateTimeOffset.UtcNow
        });

        StatusMessage = "Qwen is responding...";

        try
        {
            var response = await _apiService.SendMessageAsync(
                userText,
                conversation.Id);

            await ReloadMessagesAsync(conversation);
            ReplaceMemoryDiff(response.MemoryDiff);
            StatusMessage = LatestMemoryDiff.Count == 0
                ? "Response complete. No Memory was used."
                : "Response complete. Memory Diff is ready.";
        }
        catch (Exception exception)
        {
            await ReloadMessagesAfterFailureAsync(conversation);
            StatusMessage = $"The response failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveMemoryAsync()
    {
        var sourceMessage = SelectedMessage;
        if (sourceMessage is null || !CanSaveMemory()) return;

        IsBusy = true;
        StatusMessage = "Saving sourced Memory...";

        try
        {
            var created = await _apiService.CreateMemoryAsync(
                sourceMessage.Id,
                PrototypeMovement.Id,
                MemoryText,
                SelectedMemoryType);

            await LoadMemoriesAsync();
            SelectedMemory = Memories.FirstOrDefault(memory =>
                memory.Id == created.Id);
            StatusMessage = "Memory saved with its Source Note.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not save Memory: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshMemoriesAsync()
    {
        IsBusy = true;

        try
        {
            await LoadMemoriesAsync();
            StatusMessage = "Memory list refreshed.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not refresh Memories: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RequestArchive()
    {
        if (!CanRequestArchive()) return;
        IsArchiveConfirmationPending = true;
        StatusMessage =
            "Confirm archive. This Memory will stop influencing normal retrieval.";
    }

    private void CancelArchive()
    {
        IsArchiveConfirmationPending = false;
        StatusMessage = "Archive cancelled.";
    }

    private async Task ArchiveMemoryAsync()
    {
        var memory = SelectedMemory;
        if (memory is null || !CanRequestArchive()) return;

        IsBusy = true;
        StatusMessage = "Archiving Memory...";

        try
        {
            await _apiService.ArchiveMemoryAsync(memory.Id);
            await LoadMemoriesAsync();
            StatusMessage =
                "Memory archived. It is excluded from the next retrieval.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not archive Memory: {exception.Message}";
        }
        finally
        {
            IsArchiveConfirmationPending = false;
            IsBusy = false;
        }
    }

    private Task NewConversationAsync()
    {
        ++_conversationLoadVersion;
        _currentConversation = null;
        OnPropertyChanged(nameof(CurrentConversation));
        Messages.Clear();
        SelectedMessage = null;
        ReplaceMemoryDiff([]);
        StatusMessage = "New Conversation ready.";
        return Task.CompletedTask;
    }

    private void SetCurrentConversation(
        Conversation? conversation,
        bool loadMessages)
    {
        _currentConversation = conversation;
        OnPropertyChanged(nameof(CurrentConversation));
        SelectedMessage = null;
        ReplaceMemoryDiff([]);

        if (loadMessages)
        {
            _ = LoadSelectedConversationAsync(conversation);
        }
    }

    private async Task LoadSelectedConversationAsync(
        Conversation? conversation)
    {
        var loadVersion = ++_conversationLoadVersion;
        Messages.Clear();

        if (conversation is null) return;

        try
        {
            var messages = await _apiService.GetMessagesAsync(
                conversation.Id);

            if (loadVersion != _conversationLoadVersion ||
                !ReferenceEquals(CurrentConversation, conversation))
            {
                return;
            }

            ReplaceMessages(messages);
            StatusMessage = "Conversation loaded.";
        }
        catch (Exception exception)
        {
            if (loadVersion == _conversationLoadVersion)
            {
                StatusMessage =
                    $"Could not load Messages: {exception.Message}";
            }
        }
    }

    private async Task ReloadMessagesAsync(Conversation conversation)
    {
        var messages = await _apiService.GetMessagesAsync(conversation.Id);

        if (ReferenceEquals(CurrentConversation, conversation))
        {
            ReplaceMessages(messages);
        }
    }

    private async Task ReloadMessagesAfterFailureAsync(
        Conversation conversation)
    {
        try
        {
            await ReloadMessagesAsync(conversation);
        }
        catch
        {
            Messages.Clear();
        }
    }

    private void ReplaceMessages(IEnumerable<Message> messages)
    {
        SelectedMessage = null;
        Messages.Clear();

        foreach (var message in messages.OrderBy(message =>
                     message.Sequence))
        {
            Messages.Add(message);
        }
    }

    private void ReplaceMemoryDiff(
        IEnumerable<MemoryDiffItem> memoryDiff)
    {
        LatestMemoryDiff.Clear();

        foreach (var item in memoryDiff.OrderBy(item => item.Rank))
        {
            LatestMemoryDiff.Add(item);
        }

        OnPropertyChanged(nameof(MemoryDiffStatus));
    }

    private bool CanSaveMemory()
    {
        return !IsBusy &&
               SelectedMessage is { Id: > 0, Role: "user" } &&
               MemoryText.Trim().Length is > 0 and <= 500 &&
               MemoryTypes.Contains(SelectedMemoryType);
    }

    private bool CanRequestArchive()
    {
        return !IsBusy && SelectedMemory?.IsActive == true;
    }

    private void RaiseAllCanExecuteChanged()
    {
        SendCommand.RaiseCanExecuteChanged();
        NewConversationCommand.RaiseCanExecuteChanged();
        RefreshMemoriesCommand.RaiseCanExecuteChanged();
        SaveMemoryCommand.RaiseCanExecuteChanged();
        RequestArchiveCommand.RaiseCanExecuteChanged();
        CancelArchiveCommand.RaiseCanExecuteChanged();
        ConfirmArchiveCommand.RaiseCanExecuteChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class RelayCommand(
    Action execute,
    Func<bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) =>
        canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter)) execute();
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class AsyncRelayCommand(
    Func<Task> execute,
    Func<bool>? canExecute = null) : ICommand
{
    private bool _isExecuting;

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (canExecute?.Invoke() ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await execute();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}