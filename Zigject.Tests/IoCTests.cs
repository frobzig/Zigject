using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Zigject.Tests
{
    [TestClass]
    public class IoCTests
    {
        public interface IVehicle
        {
            int Capacity { get; set; }
        }

        public class VehicleBase : IVehicle
        {
            public int Capacity { get; set; }
        }

        public class Unicycle : VehicleBase
        {
            public Unicycle()
            {
                this.Capacity = 1; //// unless you are clowns
            }
        }

        public class Car : VehicleBase
        {
            public Car(int capacity = 5)
            {
                this.Capacity = capacity; 
            }
        }

        public class Jet : VehicleBase
        {
            public static async Task<Jet> Create(int capacity = 10)
            {
                return await Task.FromResult<Jet>(new Jet() { Capacity = capacity });
            }
        }

        [TestMethod]
        public void TypeActivatorTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Car));

            IVehicle vehicle = container.Get<IVehicle>();

            Assert.IsTrue(vehicle is Car);
            Assert.AreEqual<int>(5, vehicle.Capacity);
        }

        [TestMethod]
        public void TypeActivatorWithParamsTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Car));

            IVehicle vehicle = container.Get<IVehicle>(17);

            Assert.IsTrue(vehicle is Car);
            Assert.AreEqual<int>(17, vehicle.Capacity);
        }

        [TestMethod]
        public void SingletonTest()
        {
            IoC container = new IoC();
            
            IVehicle circusCycle = new Unicycle() { Capacity = 4 };
            container.Register<IVehicle>(circusCycle);

            IVehicle vehicle = container.Get<IVehicle>();

            Assert.IsTrue(vehicle is Unicycle);
            Assert.AreSame(circusCycle, vehicle);
            Assert.AreEqual<int>(4, vehicle.Capacity);
        }

        [TestMethod]
        public void LazySingletonTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Car), IoC.InjectionBehavior.LazySingleton);

            IVehicle vehicle1 = container.Get<IVehicle>(6);
            IVehicle vehicle2 = container.Get<IVehicle>(10);
            IVehicle vehicle3 = container.Get<IVehicle>();

            Assert.IsTrue(vehicle1 is Car);
            Assert.IsTrue(vehicle2 is Car);
            Assert.IsTrue(vehicle3 is Car);
            Assert.AreSame(vehicle1, vehicle2);
            Assert.AreSame(vehicle1, vehicle3);
            Assert.AreEqual<int>(6, vehicle1.Capacity);

            //// should be 6 because it was only instantiated the first time
            Assert.AreEqual<int>(6, vehicle2.Capacity);
            Assert.AreEqual<int>(6, vehicle3.Capacity);
        }

        [TestMethod]
        public void RegisterOverwritesTest()
        {
            IoC container = new IoC();

            IVehicle cycle = new Unicycle() { Capacity = 4 };

            container.Register<IVehicle>(cycle);
            Assert.IsInstanceOfType(container.Get<IVehicle>(), typeof(Unicycle));

            IVehicle car = new Car() { Capacity = 1 };
            container.Register<IVehicle>(car);
            Assert.IsInstanceOfType(container.Get<IVehicle>(), typeof(Car));
        }

        [TestMethod]
        public void CreateMethodTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Jet), IoC.InjectionBehavior.CreateMethod);

            IVehicle vehicle1 = container.Get<IVehicle>(2);
            IVehicle vehicle2 = container.Get<IVehicle>(16);
            IVehicle vehicle3 = container.Get<IVehicle>(300);
            IVehicle vehicle4 = container.Get<IVehicle>();

            Assert.AreNotSame(vehicle2, vehicle1);
            Assert.AreNotSame(vehicle3, vehicle2);
            Assert.AreNotSame(vehicle3, vehicle1);

            Assert.AreEqual<int>(2, vehicle1.Capacity);
            Assert.AreEqual<int>(16, vehicle2.Capacity);
            Assert.AreEqual<int>(300, vehicle3.Capacity);
            Assert.AreEqual<int>(10, vehicle4.Capacity);
        }

        [TestMethod]
        public async Task CreateMethodAsyncTest()
        {
            IoC container = new IoC();

            await container.RegisterAsync<IVehicle>(typeof(Jet), IoC.InjectionBehavior.CreateMethod);

            IVehicle vehicle1 = await container.GetAsync<IVehicle>(2);
            IVehicle vehicle2 = await container.GetAsync<IVehicle>(16);
            IVehicle vehicle3 = await container.GetAsync<IVehicle>(300);

            Assert.AreNotSame(vehicle2, vehicle1);
            Assert.AreNotSame(vehicle3, vehicle2);
            Assert.AreNotSame(vehicle3, vehicle1);

            Assert.AreEqual<int>(2, vehicle1.Capacity);
            Assert.AreEqual<int>(16, vehicle2.Capacity);
            Assert.AreEqual<int>(300, vehicle3.Capacity);
        }

        [TestMethod]
        public async Task CreateMethodLazyAsyncTest()
        {
            IoC container = new IoC();

            await container.RegisterAsync<IVehicle>(typeof(Jet),
                IoC.InjectionBehavior.CreateMethod | IoC.InjectionBehavior.LazySingleton);

            IVehicle vehicle1 = await container.GetAsync<IVehicle>(2);
            IVehicle vehicle2 = await container.GetAsync<IVehicle>(16);
            IVehicle vehicle3 = await container.GetAsync<IVehicle>(300);

            Assert.AreSame(vehicle2, vehicle1);
            Assert.AreSame(vehicle3, vehicle2);
            Assert.AreSame(vehicle3, vehicle1);

            Assert.AreEqual<int>(2, vehicle1.Capacity);
            Assert.AreEqual<int>(2, vehicle2.Capacity);
            Assert.AreEqual<int>(2, vehicle3.Capacity);
        }

        [TestMethod]
        public void CreateMethodLazyTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Jet),
                IoC.InjectionBehavior.CreateMethod | IoC.InjectionBehavior.LazySingleton);

            IVehicle vehicle1 = container.Get<IVehicle>(2);
            IVehicle vehicle2 = container.Get<IVehicle>(16);
            IVehicle vehicle3 = container.Get<IVehicle>(300);

            Assert.AreSame(vehicle2, vehicle1);
            Assert.AreSame(vehicle3, vehicle2);
            Assert.AreSame(vehicle3, vehicle1);

            Assert.AreEqual<int>(2, vehicle1.Capacity);
            Assert.AreEqual<int>(2, vehicle2.Capacity);
            Assert.AreEqual<int>(2, vehicle3.Capacity);
        }
    }
}
