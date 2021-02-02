using System.Threading.Tasks;

namespace TVTComment.Model
{
    public interface IChatTrendServiceEntry
    {
        string Name { get; }
        string Description { get; }
        Task<IChatTrendService> GetNewService();
    }
}
