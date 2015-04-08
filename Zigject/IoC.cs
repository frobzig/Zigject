/* 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 Nicholas Barrett
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

namespace Zigject
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    public class IoC
    {
        private static IoC _default;

        public static IoC Default
        {
            get
            {
                if (_default == null)
                    _default = new IoC();

                return _default;
            }
        }

        private readonly Dictionary<Type, object> _map = new Dictionary<Type, object>();
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public void Clear()
        {
            this._rwLock.EnterWriteLock();

            try
            {
                this._map.Clear();
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        public void Register<T1>(object obj)
        {
            this._rwLock.EnterWriteLock();

            try
            {
                Type type = typeof(T1);

                if (this._map.ContainsKey(type))
                    this._map[type] = obj;
                else
                    this._map.Add(typeof(T1), obj);
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        public T1 Get<T1>()
        {
            return GetOrDefault<T1>(null, null);
        }

        public T1 Get<T1>(Action<T1> initialize)
        {
            return GetOrDefault(null, initialize);
        }

        public T1 Get<T1>(Func<T1> getDefault)
        {
            return GetOrDefault(getDefault);
        }

        public T1 GetOrDefault<T1>(Func<T1> getDefault = null, Action<T1> initialize = null)
        {
            this._rwLock.EnterReadLock();

            try
            {
                T1 result;

                if (!this._map.ContainsKey(typeof(T1)) && getDefault != null)
                    return getDefault();

                Type valueType = _map[typeof(T1)] as Type;

                if (valueType != null)
                    result = (T1)Activator.CreateInstance(valueType);
                else
                    result = (T1)this._map[typeof(T1)];

                if (initialize != null)
                    initialize(result);

                return result;
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }
        }
    }
}
