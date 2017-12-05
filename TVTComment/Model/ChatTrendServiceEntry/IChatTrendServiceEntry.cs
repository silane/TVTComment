using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
