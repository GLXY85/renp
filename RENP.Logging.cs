using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RENP;

public partial class RENP
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetCurrentMethod()
    {
        var st = new StackTrace();
        var sf = st.GetFrame(1);

        return $"{GetType().Name}.{sf.GetMethod().Name}";
    }
}