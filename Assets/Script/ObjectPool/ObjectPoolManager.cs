using System;
using System.Collections.Generic;

public class ObjectPoolManager
{
    public Dictionary<Type, Queue<ObjeckBase>> pool;
    private static ObjectPoolManager _inst;

    public static ObjectPoolManager Inst
    {
        get
        {
            if (_inst == null)
            {
                _inst = new ObjectPoolManager();
            }

            return _inst;
        }
    }

    private ObjectPoolManager()
    {
        pool = new Dictionary<Type, Queue<ObjeckBase>>();
    }

    public T GetObject<T>() where T : ObjeckBase
    {
        var t = typeof(T);
        if (pool.ContainsKey(t) && pool[t].Count > 0)
        {
            return (T) pool[t].Dequeue();
        }
        else
        {
            var retult = Activator.CreateInstance<T>();
            return retult;
        }
    }

    public void Recycle(ObjeckBase o)
    {
        var t = o.GetType();
        o.ReSet();
        if (pool.ContainsKey(t))
        {
            pool[t].Enqueue(o);
        }
        else
        {
            var que = new Queue<ObjeckBase>();
            que.Enqueue(o);
            pool[t] = que;
        }
    }
}