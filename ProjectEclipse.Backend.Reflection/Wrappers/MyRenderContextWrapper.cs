using ProjectEclipse.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEclipse.Backend.Reflection.Wrappers
{
    public struct MyRenderContextWrapper
    {
        private readonly object _myRenderContextInstance;

        public MyRenderContextWrapper(object myRenderContextInstance)
        {
            if (!myRenderContextInstance.GetType().IsAssignableTo(MyRenderContextAccessor.Type_MyRenderContext))
                throw new ArgumentException();
            _myRenderContextInstance = myRenderContextInstance;
        }

        public void ClearState() => MyRenderContextAccessor.ClearState(_myRenderContextInstance);
    }
}
