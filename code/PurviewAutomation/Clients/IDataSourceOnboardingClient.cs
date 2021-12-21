using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients
{
    internal interface IDataSourceOnboardingClient
    {
        internal Task AddDataSourceAsync();
        internal Task RemoveDataSourceAsync();
        internal Task AddScanAsync();
        internal Task OnboardDataSource();
    }
}
