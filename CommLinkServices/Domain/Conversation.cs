using Common;

namespace CommLinkServices.Domain;

public class Conversation : BaseEntity
{
    public required Guid FirstUserId { get; set; }
    public required Guid SecondUserId { get; set; }

    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Call> Calls { get; set; } = [];

    public bool Contains(Guid userId) => userId == FirstUserId || userId == SecondUserId;

    public Guid Other(Guid userId) => userId == FirstUserId ? SecondUserId : FirstUserId;
}
