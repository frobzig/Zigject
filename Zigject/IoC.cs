using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZigJect
{
    public static class IoC
    {
        private static readonly Dictionary<Type, object> _map = new Dictionary<Type, object>();
        private static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public static void Register<T1, T2>(T2 obj) where T2 : class, T1
        {
            _rwLock.EnterWriteLock();

            try
            {
                //// best not add it twice
                _map.Add(typeof(T1), obj);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public static T1 Get<T1, T2>(Func<T2> getDefault = null) where T2 : class, T1
        {
            _rwLock.EnterReadLock();

            try
            {
                if (!_map.ContainsKey(typeof(T1)) && getDefault != null)
                    return getDefault();

                object value = _map[typeof(T1)];

                if (value is Type)
                    return Activator.CreateInstance((Type)value) as T2;

                return _map[typeof(T1)] as T2;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }
    }
}
