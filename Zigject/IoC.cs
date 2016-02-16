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
    using System.Threading.Tasks;
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
        private readonly SemaphoreSlim _semLock = new SemaphoreSlim(1);

        public async Task ClearAsync()
        {
            await this._semLock.WaitAsync();

            try
            {
                this._map.Clear();
            }
            finally
            {
                this._semLock.Release();
            }
        }

        public async Task RegisterAsync<T1>(object obj, InjectionBehavior behavior = InjectionBehavior.Standard)
        {
            await this._semLock.WaitAsync();

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
                this._semLock.Release();
            }
        }

        public async Task<T1> GetAsync<T1>(params object[] args)
        {
            return await GetOrDefaultAsync<T1>(null, null, args);
        }

        public async Task<T1> GetAsync<T1>(Action<T1> initialize, params object[] args)
        {
            return await GetOrDefaultAsync(null, initialize, args);
        }

        public async Task<T1> GetAsync<T1>(Func<T1> getDefault, params object[] args)
        {
            return await GetOrDefaultAsync(getDefault, null, args);
        }

        public async Task<T1> GetOrDefaultAsync<T1>(Func<T1> getDefault = null, Action<T1> initialize = null, params object[] args)
        {
            await this._semLock.WaitAsync();

            try
            {
                T1 result;

                if (!this._map.ContainsKey(typeof(T1)) && getDefault != null)
                    return getDefault();

                object value = _map[typeof(T1)];

                InjectionTypeDecorator<T1> decoratorValue = value as InjectionTypeDecorator<T1>;

                if (decoratorValue != null)
                {
                    result = await decoratorValue.GetAsync(initialize, args);
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
                            CultureInfo.InvariantCulture);
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
                this._semLock.Release();
            }
        }

        public void Clear()
        {
            Task.Run(() => { return this.ClearAsync(); });
        }

        public void Register<T1>(object obj, InjectionBehavior behavior = InjectionBehavior.Standard)
        {
            Task.Run(() => { return this.RegisterAsync<T1>(obj, behavior); }).Wait();
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
            return Task.Run(() => { return GetOrDefaultAsync(getDefault, initialize, args); }).Result;
        }

        #region InjectionBehavior
        [Flags]
        public enum InjectionBehavior
        {
            Standard = 0,
            LazySingleton = 1,
            CreateMethod = 2,
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
                Type type = this.Target as Type;

                if (this.Behavior.HasFlag(InjectionBehavior.LazySingleton) && type == null)
                    throw new InjectionException($"Only types can be registered with {nameof(InjectionBehavior.LazySingleton)}");

                if (this.Behavior.HasFlag(InjectionBehavior.CreateMethod))
                {
                    if (type == null)
                        throw new InjectionException($"Only types can be registered with {nameof(InjectionBehavior.CreateMethod)}");

                    if (type.GetMethod("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod) == null)
                        throw new InjectionException($"Cannot find Create method on type {type.Name}");
                }

                return this;
            }

            public async Task<object> CallCreateMethod(Type type, object[] args)
            {
                object result = type.InvokeMember("Create", BindingFlags.Public | BindingFlags.Static | BindingFlags.OptionalParamBinding | BindingFlags.InvokeMethod, null, null, args);

                Task task = result as Task;
                await task;

                if (task != null)
                    result = result.GetType().GetProperty("Result").GetValue(task);

                return result;
            }

            public async Task<T1> GetAsync(Action<T1> initialize = null, params object[] args)
            {
                Type typeTarget = this.Target as Type;
                object result = default(T1);

                if (typeTarget != null)
                {
                    if (this.Behavior.HasFlag(InjectionBehavior.CreateMethod))
                    {
                        result = await this.CallCreateMethod(typeTarget, args);
                    }
                    else
                    {
                        result = Activator.CreateInstance(
                            typeTarget,
                            BindingFlags.OptionalParamBinding | BindingFlags.Public |
                                BindingFlags.Instance | BindingFlags.CreateInstance,
                            null,
                            args,
                            CultureInfo.InvariantCulture);
                    }

                    if (this.Behavior.HasFlag(InjectionBehavior.LazySingleton))
                        this.Target = result;

                    if (initialize != null)
                        initialize((T1)result);
                }
                else if (!this.Behavior.HasFlag(InjectionBehavior.LazySingleton))
                    throw new InjectionException($"Invalid configuration of {nameof(InjectionTypeDecorator<T1>)}");
                else
                    result = this.Target;

                return (T1)result;
            }
        }
        #endregion
    }
}
