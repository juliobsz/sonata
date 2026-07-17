using Microsoft.AspNetCore.Mvc;
using Sonata.Server.Models;
using Sonata.Server.Repositories;
using Sonata.Server.ModelProviders;

namespace Sonata.Server.Controllers;

[ApiController]
[Route("v1/")]
public class QwenController(IModelProvider modelProvider, ISessionRepository sessionRepository, IMessageRepository messageRepository) : Controller
{
    [HttpPost("responses")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest data, CancellationToken cancellationToken)
    {
        var content = data.Content;
        var sessionId = data.SessionId ?? "";
        if (content == null || !Guid.TryParse(sessionId, out var sessionGuid)) return BadRequest();
        
        var session = await sessionRepository.GetSessionAsync(sessionGuid) ??
                      await sessionRepository.AddSessionAsync(new Session() 
                      {
                          Id = sessionGuid,
                          StartedAt = DateTimeOffset.UtcNow,
                      });
        
        await messageRepository.AddMessageAsync(new Message()
        {
            SessionId = session.Id,
            Content = content,
            Role = "user",
            CreatedAt = DateTimeOffset.UtcNow
        });
        
        var messages = await messageRepository.GetMessagesBySessionId(session.Id);

        GeneratedResponse generated;

        try
        {
            generated = await modelProvider.GenerateResponseAsync(new GenerateResponseRequest(
                messages.Select(message => new ModelMessage(Role: message.Role, Content: message.Content))
                    .ToArray()), cancellationToken);
        }
        catch (ModelProviderException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Error = "The model provider couldn't complete the response."
            });
        }
        
        await messageRepository.AddMessageAsync(new Message()
        {
            SessionId = session.Id,
            Content = generated.Text,
            Role = generated.Role,
            CreatedAt = DateTimeOffset.UtcNow
        });
        
        return Ok(new
        {
            Content = generated.Text,
            SessionId = session.Id
        });
    }
    
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var sessions = await sessionRepository.GetSessionsAsync();
        
        return Ok(new
        {
            Sessions = sessions
        });
    }
    
    [HttpGet("messages/{sessionId}")]
    public async Task<IActionResult> GetMessages([FromRoute] string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid)) return BadRequest();
        var messages = await messageRepository.GetMessagesBySessionId(sessionGuid);
        
        return Ok(new
        {
            Messages = messages
        });
    }
}
