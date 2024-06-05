using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    internal interface RequestHandler
    {
        void Handle(HttpListenerContext context);

        string GetName();
    }
}
