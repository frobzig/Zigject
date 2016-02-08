/* 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016 Nicholas Barrett
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
    using System.Globalization;
    using System.Reflection;
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

        public void Register<T1>(object obj, InjectionBehavior behavior = InjectionBehavior.Standard)
        {
            this._rwLock.EnterWriteLock();

            try
            {
                Type type = typeof(T1);

                if (behavior != InjectionBehavior.Standard)
                    obj = new InjectionTypeDecorator<T1>(obj, behavior).Validated();

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

        public T1 Get<T1>(params object[] args)
        {
            return GetOrDefault<T1>(null, null, args);
        }

        public T1 Get<T1>(Action<T1> initialize, params object[] args)
        {
            return GetOrDefault(null, initialize, args);
        }

        public T1 Get<T1>(Func<T1> getDefault, params object[] args)
        {
            return GetOrDefault(getDefault, null, args);
        }

        public T1 GetOrDefault<T1>(Func<T1> getDefault = null, Action<T1> initialize = null, params object[] args)
        {
            this._rwLock.EnterReadLock();

            try
            {
                T1 result;

                if (!this._map.ContainsKey(typeof(T1)) && getDefault != null)
                    return getDefault();

                object value = _map[typeof(T1)];

                InjectionTypeDecorator<T1> decoratorValue = value as InjectionTypeDecorator<T1>;

                if (decoratorValue != null)
                {
                    result = decoratorValue.Get(initialize, args);
                }
                else
                {
                    Type typeValue = value as Type;

                    if (typeValue != null)
                    {
                        result = (T1)Activator.CreateInstance(
                            typeValue,
                            BindingFlags.OptionalParamBinding | BindingFlags.Public |
                                BindingFlags.Instance | BindingFlags.CreateInstance,
                            null,
                            args,
                            CultureInfo.CurrentCulture);
                    }
                else
                        result = (T1)value;

                if (initialize != null)
                    initialize(result);
                }

                return result;
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }
        }

        #region InjectionBehavior
        [Flags]
        public enum InjectionBehavior
        {
            Standard = 0,
            Lazy = 1,
        }
        #endregion

        #region InjectionTypeDecorator
        private class InjectionTypeDecorator<T1>
        {
            public InjectionTypeDecorator(object obj, InjectionBehavior behavior)
            {
                this.Target = obj;
                this.Behavior = behavior;
            }

            public InjectionBehavior Behavior { get; private set; }
            public object Target { get; private set; }

            public InjectionTypeDecorator<T1> Validated()
            {
                if (this.Behavior.HasFlag(InjectionBehavior.Lazy) && !(this.Target is Type))
                    throw new InjectionException("Only types can be registered with InjectionBehavior.Lazy");

                return this;
            }

            public T1 Get(Action<T1> initialize = null, params object[] args)
            {
                Type typeTarget = this.Target as Type;

                if (this.Behavior.HasFlag(InjectionBehavior.Lazy))
                {
                    if (typeTarget != null)
                    {
                        this.Target = Activator.CreateInstance(
                            typeTarget,
                            BindingFlags.OptionalParamBinding | BindingFlags.Public |
                                BindingFlags.Instance | BindingFlags.CreateInstance,
                            null,
                            args,
                            CultureInfo.CurrentCulture);
                    }

                    if (initialize != null)
                        initialize((T1)this.Target);

                    return (T1)this.Target;
                }
                else
                    throw new InjectionException($"Invalid configuration of {nameof(InjectionTypeDecorator<T1>)}");
            }
        }
        #endregion
    }
}
