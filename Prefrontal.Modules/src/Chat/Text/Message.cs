namespace Prefrontal.Modules.Chat.Text;

public class Message
(
	string role,
	string content,
	string? name = null
)
{
	public string Role = role;
	public string Content = content;
	public string? Name = name;

	public static Message FromSystem(string content)
		=> new(SystemRole, content);

	public static Message FromUser(string content)
		=> new(UserRole, content);

	public static Message FromAssistant(string content)
		=> new(AssistantRole, content);

	public const string SystemRole = "system";
	public const string UserRole = "user";
	public const string AssistantRole = "assistant";
}
