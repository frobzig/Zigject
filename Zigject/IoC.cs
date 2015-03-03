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

namespace ZigJect
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

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
