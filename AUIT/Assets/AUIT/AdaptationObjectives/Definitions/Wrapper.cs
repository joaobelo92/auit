using System;
using System.Linq;

namespace AUIT.AdaptationObjectives.Definitions
{
    [Serializable]
    public class Wrapper<T>
    {
        public string manager_id;
        public T[] items;
    }
    
    
}