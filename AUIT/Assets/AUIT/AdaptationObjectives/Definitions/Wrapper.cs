using System;
using System.Linq;

namespace AUIT.AdaptationObjectives.Definitions
{
    [Serializable]
    public class Wrapper<T>
    {
        public T[] items;

        // public override string ToString()
        // {
        //     return string.Join(",", array);
        // }
    }
    
    
}