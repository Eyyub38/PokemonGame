using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

public interface ISavable{
    object CaptureState();
    void RestoreState(object state);
}
