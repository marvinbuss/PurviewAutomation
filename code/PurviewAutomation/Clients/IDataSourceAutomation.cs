using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurviewAutomation.Clients
{
    internal interface IDataSourceAutomation
    {
        internal void AddDataSource();
        internal void RemoveDataSource();
        internal void CreateScan();
        internal void CreateTrigger();
        internal void CreateFilter();
    }
}
