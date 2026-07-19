using Sonata.Server.Repositories;
using Sonata.Server.ModelProviders;
using Sonata.Server.Models;

namespace Sonata.Server.Conversations;

public sealed class ConversationService(
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IModelProvider modelProvider) : IConversationService
{
    public async Task<ConversationTurn> ContinueAsync(ContinueConversationCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Content))
            throw new ArgumentException("Conversation content can't be empty.", nameof(command));

        var conversation = await conversationRepository.GetConversationAsync(command.ConversationId)
                      ?? await conversationRepository.AddConversationAsync(new Conversation
                      {
                          Id = command.ConversationId,
                          CreatedAt = DateTimeOffset.UtcNow,
                      });

        var userMessage = await messageRepository.AddMessageAsync(new Message
        {
            ConversationId = conversation.Id,
            Content = command.Content,
            Role = "user",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        
        var history = await messageRepository.GetMessagesByConversationId(conversation.Id);

        var generated = await modelProvider.GenerateResponseAsync(new GenerateResponseRequest(
                history.Select(message => new ModelMessage(
                    message.Role,
                    message.Content
                )).ToArray()),
            cancellationToken);

        var assistantMessage = await messageRepository.AddMessageAsync(new Message
        {
            ConversationId = conversation.Id,
            Content = generated.Text,
            Role = generated.Role,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        
        return new ConversationTurn(conversation.Id, ToContract(userMessage), ToContract(assistantMessage));
    }
    
    private static ConversationMessage ToContract(Message message)
    {
        return new ConversationMessage(
            message.Id,
            message.Sequence,
            message.Role,
            message.Content,
            message.CreatedAt
        );
    }
}