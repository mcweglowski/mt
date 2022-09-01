using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Contracts
{
    public interface OrderAccepted
    {
        Guid OrderId { get; }
        DateTime Timestamp { get; }
    }
}
