using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCli
{
    public interface iState
    {
        void RegisterMainPackageStates(iState s);

        bool SetState(string line);
        bool Loop();
    }
}
