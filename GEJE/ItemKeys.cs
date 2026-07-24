using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    
    public class ItemKeys
    {
        Dictionary<int, Action> KeyToFunc = new Dictionary<int, Action>();
        public ISet<int> keys = new HashSet<int>();
        public ItemKeys() { }
        public void AddKey(int key, Action A)
        {
            KeyToFunc.Add(key, A);
            keys.Add(key);
        }
        public void RemoveKey(int key)
        {
            KeyToFunc.Remove(key);
            keys.Remove(key);
        }
        public void Use(int key)
        {
            KeyToFunc[key].Invoke();
        }
    }
}
